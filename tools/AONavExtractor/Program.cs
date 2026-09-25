using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace AONavExtractor
{
    /// <summary>
    /// AONavExtractor - writes one folder of navigation data per playfield from an Anarchy Online
    /// client install, the same layout AOBuddy ships in AOBuddy/GameData/Nav (README.md there).
    ///
    ///   AONavExtractor &lt;AO install dir&gt; &lt;out dir&gt; [--nav &lt;walked nav dir&gt;] [--only pf,pf,...]
    ///                  [--no-collision] [--tri &lt;dir of navbridge .tri files&gt;] [--keep-tri]
    ///
    /// Outdoor heightfields and dungeon rooms are decoded from ResourceDatabase.dat directly.
    /// Collision surfaces (RDB 1000013) are read by hosting the client's own N3.dll in a child
    /// process; a record that crashes the reader is skipped and the child restarted.
    /// </summary>
    static class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length >= 1 && args[0] == "--collision-worker") return CollisionWorker(args);
                if (args.Length >= 3 && args[0] == "--hostwater") return HostWater(args[1], int.Parse(args[2]));
                if (args.Length >= 4 && args[0] == "--dump") { string db0 = Path.Combine(args[1], "cd_image", "data", "db"); using (var r0 = new Rdb(db0)) { byte[] rec = r0.Read(int.Parse(args[2]), int.Parse(args[3])); Console.WriteLine("// length {0}", rec.Length); Dump(rec, 0, rec.Length, "record"); } return 0; }
                if (args.Length >= 3 && args[0] == "--probe") return Probe(args[1], int.Parse(args[2]));
                if (args.Length < 2) { Console.Error.WriteLine("usage: AONavExtractor <AO install dir> <out dir> [--nav <dir>] [--only pf,pf] [--no-collision] [--tri <dir>] [--keep-tri]"); return 2; }
                return Run(args);
            }
            catch (Exception e) { Console.Error.WriteLine(e); return 1; }
        }

        static int Run(string[] args)
        {
            string client = args[0], outDir = Path.GetFullPath(args[1]);
            string navDir = null, triDir = null; bool noCollision = false, keepTri = false;
            var only = new HashSet<int>();
            for (int i = 2; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--nav": navDir = args[++i]; break;
                    case "--tri": triDir = args[++i]; break;
                    case "--only": foreach (var s in args[++i].Split(',')) only.Add(int.Parse(s)); break;
                    case "--no-collision": noCollision = true; break;
                    case "--keep-tri": keepTri = true; break;
                    default: throw new ArgumentException("unknown option " + args[i]);
                }
            }
            string db = Path.Combine(client, "cd_image", "data", "db");
            if (!File.Exists(Path.Combine(db, "ResourceDatabase.idx"))) throw new FileNotFoundException("no ResourceDatabase.idx under " + db);
            Directory.CreateDirectory(outDir);
            var sw = Stopwatch.StartNew();

            // Phase 1: collision, in a child process per crash.
            bool ownTri = false;
            if (!noCollision && triDir == null)
            {
                triDir = Path.Combine(outDir, ".tri");
                Directory.CreateDirectory(triDir);
                CollisionPhase(client, triDir, only);
                ownTri = true;
            }

            // Phase 2: per playfield.
            var index = new List<string>();
            using (var rdb = new Rdb(db))
            {
                foreach (int pf in rdb.Instances(1000001))
                {
                    if (only.Count > 0 && !only.Contains(pf)) continue;
                    byte[] blob = rdb.Read(1000001, pf);
                    int version = blob.Length >= 4 ? BitConverter.ToInt32(blob, 0) : 0;
                    string name = Util.CStr(blob, 8, 32);
                    string kind = "none", error = null;
                    Ground ground = null; Dungeon dungeon = null;
                    try
                    {
                        if (Dungeon.IsDungeonRecord(blob) && rdb.Has(1000009, BitConverter.ToInt32(blob, 0x28))) { dungeon = Dungeon.Read(rdb, pf, blob); kind = "dungeon"; }
                        else if (rdb.Has(1000009, pf)) { ground = Ground.Read(rdb, pf); if (ground != null) kind = "outdoor"; }
                    }
                    catch (Exception e) { error = e.GetType().Name + ": " + e.Message; }
                    if (ground != null)
                    {
                        ground.WaterY = Ground.WaterPlanes(blob);
                        ground.Water.AddRange(Ground.WaterPolygons(blob));
                    }
                    List<SurfaceRecord> recs = null;
                    string tri = triDir != null ? Path.Combine(triDir, pf + ".tri") : null;
                    if (tri != null && File.Exists(tri)) recs = TriFile.Read(tri, out _);
                    if (kind == "none" && recs == null) continue;

                    string folder = Path.Combine(outDir, pf.ToString());
                    Directory.CreateDirectory(folder);
                    var info = new JsonOut(true);
                    info.Obj();
                    info.Key("playfield").Num(pf); info.Key("name").Str(name); info.Key("playfieldVersion").Num(version);
                    info.Key("files").Arr();
                    if (ground != null) info.Str("ground.bin");
                    if (dungeon != null) info.Str("rooms.json");
                    if (recs != null) info.Str("collision.bin");
                    if (recs != null && (dungeon != null || ground != null)) info.Str("walls.bin");
                    info.End();
                    if (error != null) info.Key("error").Str(error);
                    info.Key("kind").Str(kind);
                    if (ground != null)
                    {
                        ground.Write(Path.Combine(folder, "ground.bin"));
                        info.Key("ground").Obj();
                        info.Key("samplesX").Num(ground.SamplesX); info.Key("samplesZ").Num(ground.SamplesZ);
                        info.Key("cell").Num(ground.Cell); info.Key("heightScale").Num(ground.HeightScale); info.Key("sourceHeightBits").Num(ground.SourceBits);
                        info.Key("worldSize").Arr().Num((ground.SamplesX - 1) * (double)ground.Cell).Num((ground.SamplesZ - 1) * (double)ground.Cell).End();
                        info.Key("heightRange").Arr().Num(Math.Round(ground.MinHeight, 4)).Num(Math.Round(ground.MaxHeight, 4)).End();
                        info.Key("heightVerifiable").Bool(ground.SourceBits == 8);
                        if (ground.WaterY.Length > 0)
                        {
                            info.Key("waterPlanes").Arr();
                            foreach (float y in ground.WaterY) info.Num(Math.Round(y, 2));
                            info.End();
                        }
                        if (ground.Water.Count > 0)
                        {
                            info.Key("waterPolygons").Arr();
                            foreach (double[] w in ground.Water) info.Num(Math.Round(w[0], 2));
                            info.End();
                        }
                        info.End();
                    }
                    if (dungeon != null)
                    {
                        dungeon.WriteJson(Path.Combine(folder, "rooms.json"));
                        info.Key("dungeon").Obj();
                        info.Key("tilemap").Num(dungeon.Tilemap); info.Key("rooms").Num(dungeon.Rooms.Count);
                        info.Key("cell").Num(dungeon.Cell); info.Key("heightScale").Num(dungeon.HeightScale);
                        info.End();
                    }
                    if (recs != null)
                    {
                        var (kept, chunks) = TriFile.WriteCollision(Path.Combine(folder, "collision.bin"), recs);
                        long total = 0; foreach (var r in recs) total += r.Triangles;
                        info.Key("collision").Obj();
                        info.Key("records").Num(recs.Count); info.Key("trianglesTotal").Num(total); info.Key("trianglesWalkable").Num(kept); info.Key("chunks").Num(chunks);
                        info.End();
                        if (dungeon != null || ground != null)
                        {
                            // Dungeons for the mission room pools; outdoor zones for overland routing (city walls,
                            // buildings, fences - the heightfield alone walks straight through them).
                            var (wk, wc) = TriFile.WriteWalls(Path.Combine(folder, "walls.bin"), recs);
                            info.Key("walls").Obj();
                            info.Key("triangles").Num(wk); info.Key("chunks").Num(wc); info.Key("minHeight").Num(TriFile.WallMinHeight);
                            info.End();
                        }
                    }
                    Verification ver = null;
                    string navFile = navDir != null ? Path.Combine(navDir, pf + ".json") : null;
                    if (navFile != null && File.Exists(navFile))
                    {
                        var pts = Verification.LoadWalked(navFile);
                        if (pts.Count > 0) { ver = Verification.Run(pts, ground, dungeon, recs); info.Key("verification"); ver.WriteJson(info); }
                    }
                    info.End();
                    string infoText = info.ToString();
                    File.WriteAllText(Path.Combine(folder, "info.json"), infoText);
                    index.Add(infoText);
                    Console.WriteLine("pf {0,-6} {1,-9} {2,-32} {3}{4}", pf, kind, name.Length > 32 ? name.Substring(0, 32) : name,
                        ground != null && ground.WaterY.Length > 0 ? "water " + string.Join("/", ground.WaterY.Select(y => y.ToString("0.#"))) + " " : "",
                        ver != null ? string.Format("verified {0:F1}%", ver.ExplainedPct) : "");
                    if (ownTri && !keepTri && tri != null && File.Exists(tri)) File.Delete(tri);
                }
            }
            File.WriteAllText(Path.Combine(outDir, "index.json"), "[\n" + string.Join(",\n", index) + "\n]\n");
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("README.md"))
                if (s != null) using (var f = File.Create(Path.Combine(outDir, "README.md"))) s.CopyTo(f);
            if (ownTri && !keepTri && Directory.Exists(triDir) && Directory.GetFileSystemEntries(triDir).Length == 0) Directory.Delete(triDir);
            Console.WriteLine("wrote {0} playfields to {1} in {2:F0} s", index.Count, outDir, sw.Elapsed.TotalSeconds);
            return 0;
        }

        // ---------------------------------------------------------------- hostwater: the client's own playfield reader, water hunt 2026-09-24

        /// <summary>
        /// Constructs the client's RDBPlayfield_t over the framed 1000001 record (its ReadBlob is
        /// the only authoritative parse of that format) and dumps the object graph it produces,
        /// hunting the water polylines n3Room_t::ReadBlob builds into (room+0x88/+0x8c) and the
        /// 40-byte triangle entries (3x Vector3 + int level/320). x86 only, like the collision step.
        /// </summary>
        static unsafe int HostWater(string client, int pf)
        {
            if (IntPtr.Size != 4) { Console.Error.WriteLine("x86 build required"); return 1; }
            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool SetDllDirectoryW(string path);
            [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)] static extern IntPtr LoadLibraryExA(string path, IntPtr file, uint flags);
            [DllImport("kernel32", CharSet = CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr m, string name);
            const uint LOAD_WITH_ALTERED_SEARCH_PATH = 8;
            SetDllDirectoryW(client);
            IntPtr Load(string name) => LoadLibraryExA(Path.Combine(client, name), IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
            Load("msvcr100.dll"); Load("msvcp100.dll");
            IntPtr bs = Load("BinaryStream.dll");
            Load("InstanceManager.dll"); Load("DatabaseController.dll"); Load("ResourceManager.dll");
            Load("GameData.dll"); Load("Utils.dll"); Load("Collision.dll"); Load("PathFinder.dll");
            IntPtr n3 = Load("N3.dll");
            if (bs == IntPtr.Zero || n3 == IntPtr.Zero) { Console.Error.WriteLine("DLL load failed"); return 1; }
            IntPtr Sym(IntPtr m, string n) { var p = GetProcAddress(m, n); if (p == IntPtr.Zero) throw new EntryPointNotFoundException(n); return p; }
            delegate* unmanaged[Thiscall]<IntPtr, IntPtr, IntPtr> pfCtor = (delegate* unmanaged[Thiscall]<IntPtr, IntPtr, IntPtr>)Sym(n3, "??0RDBPlayfield_t@@IAE@ABVIdentity_t@@@Z");
            delegate* unmanaged[Thiscall]<IntPtr, IntPtr, byte> pfRead = (delegate* unmanaged[Thiscall]<IntPtr, IntPtr, byte>)Sym(n3, "?ReadBlob@RDBPlayfield_t@@UAE_NAAVBinaryStream@@@Z");
            delegate* unmanaged[Thiscall]<IntPtr, void> pfDtor = (delegate* unmanaged[Thiscall]<IntPtr, void>)Sym(n3, "??1RDBPlayfield_t@@MAE@XZ");
            delegate* unmanaged[Thiscall]<IntPtr, IntPtr, uint, IntPtr> lsCtor = (delegate* unmanaged[Thiscall]<IntPtr, IntPtr, uint, IntPtr>)Sym(bs, "??0BinaryLStream@@QAE@PAXI@Z");
            delegate* unmanaged[Thiscall]<IntPtr, void> lsDtor = (delegate* unmanaged[Thiscall]<IntPtr, void>)Sym(bs, "??1BinaryLStream@@QAE@XZ");

            string db = Path.Combine(client, "cd_image", "data", "db");
            using (var rdb = new Rdb(db))
            {
                byte[] blob = rdb.ReadFramed(1000001, pf);
                Console.WriteLine($"pf {pf}: framed record {blob.Length} bytes");
                IntPtr data = Marshal.AllocHGlobal(blob.Length);
                IntPtr ident = Marshal.AllocHGlobal(8);
                IntPtr obj = Marshal.AllocHGlobal(0x400);
                IntPtr strm = Marshal.AllocHGlobal(0x400);
                try
                {
                    Marshal.Copy(blob, 0, data, blob.Length);
                    Marshal.WriteInt32(ident, 0, 1000001); Marshal.WriteInt32(ident, 4, pf);
                    ZeroMem(obj, 0x400); ZeroMem(strm, 0x400);
                    pfCtor(obj, ident);
                    lsCtor(strm, data, (uint)blob.Length);
                    bool ok = (pfRead(obj, strm) & 1) != 0;
                    Console.WriteLine("ReadBlob -> " + ok);
                    lsDtor(strm);
                    if (ok)
                    {
                        Console.WriteLine("RDBPlayfield_t object dwords:");
                        for (int o = 0; o < 0xC0; o += 4)
                            Console.WriteLine($"  +0x{o:X2}: 0x{Marshal.ReadInt32(obj, o):X8}");
                        // the zone vector at +0x44 (populated by ReadBlob's outdoor branch); each 0x38-byte
                        // zone carries +0x20 = vector<VisualWaterInfo*> — the water boxes.
                        IntPtr groups = (IntPtr)Marshal.ReadInt32(obj, 0x44);
                        IntPtr zbegin = (IntPtr)Marshal.ReadInt32(groups, 0), zend = (IntPtr)Marshal.ReadInt32(groups, 4);
                        int zn = (int)((zend.ToInt64() - zbegin.ToInt64()) / 4);
                        Console.WriteLine($"zones: {zn}");
                        int wiTotal = 0;
                        foreach (int zi in new[] { 0, 900, zn - 1 })
                        {
                            IntPtr zone = (IntPtr)Marshal.ReadInt32(zbegin, zi * 4);
                            Console.WriteLine($"zone[{zi}] @0x{(zone == IntPtr.Zero ? 0 : zone.ToInt64()):X}:");
                            if (zone == IntPtr.Zero) continue;
                            for (int o = 0; o < 0x38; o += 4)
                            {
                                int dw = Marshal.ReadInt32(zone, o);
                                float fv = BitConverter.ToSingle(BitConverter.GetBytes(dw), 0);
                                string note = (Math.Abs(fv) > 0.001f && Math.Abs(fv) < 100000f) ? $" ({fv:0.00})" : "";
                                Console.WriteLine($"   +0x{o:X2}: 0x{dw:X8}{note}");
                            }
                        }
                        for (int zi = 0; zi < zn && zi < 3000; zi++)
                        {
                            IntPtr zone = (IntPtr)Marshal.ReadInt32(zbegin, zi * 4);
                            if (zone == IntPtr.Zero) continue;
                            IntPtr wi = (IntPtr)Marshal.ReadInt32(zone, 0x20);
                            if (wi == IntPtr.Zero) continue;
                            IntPtr wb = (IntPtr)Marshal.ReadInt32(wi, 0), we2 = (IntPtr)Marshal.ReadInt32(wi, 4);
                            long span = we2.ToInt64() - wb.ToInt64();
                            if (span <= 0 || span > 400000 || (span % 4) != 0) continue;
                            int wc2 = (int)(span / 4);
                            for (int k = 0; k < wc2; k++)
                            {
                                IntPtr w = (IntPtr)Marshal.ReadInt32(wb, k * 4);
                                if (w == IntPtr.Zero) continue;
                                wiTotal++;
                                if (wiTotal <= 6)
                                {
                                    var sb = new StringBuilder();
                                    for (int d = 0; d < 16; d++)
                                    {
                                        int dw = Marshal.ReadInt32(w, d * 4);
                                        float fv = BitConverter.ToSingle(BitConverter.GetBytes(dw), 0);
                                        sb.Append($" {d:00}:0x{dw:X8}");
                                        if (Math.Abs(fv) > 0.001f && Math.Abs(fv) < 100000f) sb.Append($"({fv:0.0})");
                                    }
                                    Console.WriteLine($"  zone {zi} WI[{k}] @0x{w.ToInt64():X}:{sb}");
                                }
                            }
                        }
                        Console.WriteLine($"total VisualWaterInfo entries: {wiTotal}");
                    }
                    pfDtor(obj);
                }
                finally
                {
                    Marshal.FreeHGlobal(data); Marshal.FreeHGlobal(ident); Marshal.FreeHGlobal(obj); Marshal.FreeHGlobal(strm);
                }
            }
            return 0;
        }

        static void ZeroMem(IntPtr p, int n) { for (int i = 0; i < n; i += 8) Marshal.WriteInt64(p, i, 0); }

        static float readF(IntPtr p, int off) { float[] f = new float[1]; Marshal.Copy(p + off, f, 0, 1); return f[0]; }

        // ---------------------------------------------------------------- probe: dump a playfield's records

        /// <summary>
        /// Exploration dump for one playfield: every field of the ground record's reflective tree,
        /// the playfield record's tail (where the water-plane table lives), and the RDB's type
        /// inventory. Water-region hunt, 2026-09-24.
        /// </summary>
        static int Probe(string client, int pf)
        {
            string db = Path.Combine(client, "cd_image", "data", "db");
            using (var rdb = new Rdb(db))
            {
                Console.WriteLine("== RDB types ==");
                foreach (var t in rdb.Types) Console.WriteLine("  {0,-8} {1} records", t, new List<int>(rdb.Instances(t)).Count);

                byte[] blob = rdb.Read(1000001, pf);
                Console.WriteLine("== playfield record 1000001/{0}: {1} bytes ==", pf, blob.Length);
                Console.WriteLine("  (water levels print with the ground record below — tile-derived)");

                if (rdb.HasType(1000010))
                {
                    foreach (int inst in rdb.Instances(1000010))
                    {
                        byte[] tt = rdb.Read(1000010, inst);
                        Console.WriteLine("== tile-type record 1000010/{0}: {1} bytes ==", inst, tt.Length);
                        int nt = Util.IndexOf(tt, "DB_t");
                        if (nt > 0)
                        {
                            var r2 = new ReflectiveRecord(tt, nt - 21);
                            foreach (var o in r2.Objects)
                            {
                                Console.WriteLine("object {0} class '{1}':", o.Index, o.Class);
                                foreach (var e in o.Entries)
                                {
                                    string preview = e.ValueType == ReflectiveRecord.T_STR ? "'" + e.Str + "'"
                                        : e.ValueType == ReflectiveRecord.T_INT && e.Raw.Length == 4 ? e.Int.ToString()
                                        : e.Raw.Length <= 48 ? Hex(e.Raw, 0, e.Raw.Length) : Hex(e.Raw, 0, 48) + " ...";
                                    Console.WriteLine("  {0,-36} vt {1,-3} es {2,-4} len {3,-9} {4}", e.Name, e.ValueType, e.ElemSize, e.Raw.Length, preview);
                                }
                            }
                        }
                        else Dump(tt, 0, Math.Min(tt.Length, 512), "raw head");
                    }
                }

                byte[] g = rdb.Read(1000009, pf);
                Console.WriteLine("== ground record 1000009/{0}: {1} bytes ==", pf, g.Length);
                Console.WriteLine("  water levels: " + string.Join(", ", Ground.WaterPlanes(blob).Select(y => y.ToString("0.###"))));
                int i = Util.IndexOf(g, "AnarchyGroundDataDB_t");
                var rec = new ReflectiveRecord(g, i - 21);
                foreach (var o in rec.Objects)
                {
                    Console.WriteLine("object {0} class '{1}' at 0x{2:X}:", o.Index, o.Class, o.Offset);
                    foreach (var e in o.Entries)
                    {
                        string preview;
                        if (e.ValueType == ReflectiveRecord.T_INT && e.Raw.Length == 4) preview = e.Int.ToString();
                        else if (e.ValueType == ReflectiveRecord.T_FLOAT && e.Raw.Length == 4) preview = e.Float.ToString("0.###");
                        else if (e.ValueType == ReflectiveRecord.T_STR) preview = "'" + e.Str + "'";
                        else if (e.Raw.Length <= 32) preview = Hex(e.Raw, 0, e.Raw.Length);
                        else preview = Hex(e.Raw, 0, 32) + " ...";
                        Console.WriteLine("  {0,-36} vt {1,-3} es {2,-4} len {3,-9} {4}", e.Name, e.ValueType, e.ElemSize, e.Raw.Length, preview);
                    }
                }
                int after = rec.DataEnd;
                Console.WriteLine("reflective data ends at 0x{0:X}; record has {1} trailing bytes", after, g.Length - after);
                if (g.Length - after > 0) Dump(g, after, Math.Min(g.Length - after, 256), "after-reflective");
            }
            return 0;
        }

        static string Hex(byte[] b, int at, int n)
        {
            var sb = new StringBuilder();
            for (int k = 0; k < n && at + k < b.Length; k++) sb.AppendFormat("{0:X2} ", b[at + k]);
            return sb.ToString();
        }

        static void Dump(byte[] b, int at, int n, string label)
        {
            Console.WriteLine("-- {0} from 0x{1:X} --", label, at);
            for (int r = 0; r < n; r += 16)
            {
                var sb = new StringBuilder();
                for (int k = 0; k < 16 && at + r + k < b.Length; k++) sb.AppendFormat("{0:X2} ", b[at + r + k]);
                Console.WriteLine("  0x{0:X6}  {1}", at + r, sb.ToString());
            }
        }

        // ---------------------------------------------------------------- collision, out of process

        static void CollisionPhase(string client, string triDir, HashSet<int> only)
        {
            string progress = Path.Combine(triDir, "progress.txt"), skips = Path.Combine(triDir, "skip.txt");
            var skipped = new List<string>();
            for (int attempt = 0; attempt < 200; attempt++)
            {
                var psi = new ProcessStartInfo(Environment.ProcessPath)
                {
                    UseShellExecute = false, RedirectStandardOutput = false, RedirectStandardError = false,
                };
                psi.ArgumentList.Add("--collision-worker"); psi.ArgumentList.Add(client); psi.ArgumentList.Add(triDir);
                psi.ArgumentList.Add(progress); psi.ArgumentList.Add(skips);
                psi.ArgumentList.Add(only.Count > 0 ? string.Join(",", only) : "all");
                using (var p = Process.Start(psi))
                {
                    p.WaitForExit();
                    if (p.ExitCode == 0) break;
                    string last = File.Exists(progress) ? File.ReadAllText(progress).Trim() : "";
                    if (last.Length == 0) throw new Exception("collision worker failed (exit " + p.ExitCode + ") before reading any record");
                    if (skipped.Contains(last)) throw new Exception("collision worker keeps failing on record " + last);
                    skipped.Add(last);
                    File.AppendAllText(skips, last + "\n");
                    Console.WriteLine("collision worker died on record {0} (exit {1}); restarting without it", last, p.ExitCode);
                }
            }
            if (skipped.Count > 0) Console.WriteLine("{0} collision record(s) skipped after crashing the client's reader: {1}", skipped.Count, string.Join(" ", skipped));
            if (File.Exists(progress)) File.Delete(progress);
            if (File.Exists(skips)) File.Delete(skips);
        }

        /// <summary>Child: read every type-1000013 record per playfield with the client's own reader, writing &lt;pf&gt;.tri files. Resumable.</summary>
        static int CollisionWorker(string[] a)
        {
            string client = a[1], triDir = a[2], progress = a[3], skipFile = a[4];
            var only = new HashSet<int>();
            if (a.Length > 5 && a[5] != "all") foreach (var s in a[5].Split(',')) only.Add(int.Parse(s));
            var skip = new HashSet<string>();
            if (File.Exists(skipFile)) foreach (var l in File.ReadAllLines(skipFile)) if (l.Trim().Length > 0) skip.Add(l.Trim());
            string db = Path.Combine(client, "cd_image", "data", "db");
            using (var rdb = new Rdb(db))
            using (var cs = new ClientSurfaces(client))
            {
                foreach (var line in cs.Log) Console.WriteLine("  " + line);
                var byPf = ClientSurfaces.ByPlayfield(rdb);
                int done = 0;
                foreach (var kv in byPf)
                {
                    int pf = kv.Key;
                    if (only.Count > 0 && !only.Contains(pf)) continue;
                    string path = Path.Combine(triDir, pf + ".tri");
                    if (File.Exists(path)) continue;                     // finished by an earlier attempt
                    var recs = new List<SurfaceRecord>();
                    long verts = 0;
                    foreach (int inst in kv.Value)
                    {
                        string key = pf + ":" + inst;
                        if (skip.Contains(key)) continue;
                        File.WriteAllText(progress, key);
                        var r = cs.Read(rdb, inst);
                        if (r != null && r.Verts.Length > 0) { recs.Add(r); verts += r.Verts.Length / 3; }
                    }
                    if (recs.Count > 0) TriFile.Write(path, pf, recs);
                    Console.WriteLine("pf {0,-6} {1,5} recs -> {2,5} parsed {3,10} verts", pf, kv.Value.Count, recs.Count, verts);
                    done++;
                }
                Console.WriteLine("collision: {0} playfields", done);
            }
            if (File.Exists(progress)) File.Delete(progress);
            return 0;
        }
    }

}
