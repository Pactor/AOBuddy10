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
                if (args.Length >= 3 && args[0] == "--grounddump") return HostGround(args[1], int.Parse(args[2]));
                if (args.Length >= 3 && args[0] == "--tilemapdump") return TileMapDump(args[1], int.Parse(args[2]));
                if (args.Length >= 4 && args[0] == "--tilecolors") return TileColors.Run(args[1], args[2], args[3], args.Length > 4 && args[4] == "--png");
                if (args.Length >= 6 && args[0] == "--offsettest") return TileColors.OffsetTest(args[1], args[2], int.Parse(args[3]), int.Parse(args[4]), int.Parse(args[5]));
                if (args.Length >= 6 && args[0] == "--optest") return TileColors.OpTest(args[1], args[2], int.Parse(args[3]), int.Parse(args[4]), int.Parse(args[5]));
                if (args.Length >= 5 && args[0] == "--mapwin") return TileColors.MapWin(args[1], args[2], int.Parse(args[3]), int.Parse(args[4]));
                if (args.Length >= 4 && args[0] == "--dump")
                {
                    string db0 = Path.Combine(args[1], "cd_image", "data", "db");
                    using (var r0 = new Rdb(db0))
                    {
                        if (!r0.Has(int.Parse(args[2]), int.Parse(args[3])))
                        {
                            Console.WriteLine("// no such record; instances of type {0}: {1}", args[2], string.Join(",", r0.Instances(int.Parse(args[2])).Take(40)));
                            return 1;
                        }
                        byte[] rec = r0.Read(int.Parse(args[2]), int.Parse(args[3]));
                        Console.WriteLine("// length {0}", rec.Length);
                        Dump(rec, 0, rec.Length, "record");
                    }
                    return 0;
                }
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

        // ---------------------------------------------------------------- tilemapdump: the client's own tile->texture answer

        /// <summary>
        /// The end of the tile-colour hunt (2026-09-26): hosts serialize.dll + Anarchy.dll, parses the
        /// CHGA ground record with the CLIENT'S OWN ObjectArchive, builds AnarchyGroundData_t, and asks
        /// its exported TileTexture(ushort) what RTexture_t each distinct tile value renders with — then
        /// matches those pointers against the archive's objects (ReadObject ids = the texture numbering
        /// the texsheet uses). Whatever mapping the client applies (DecompressTileMap included) is in
        /// that answer, no guessing left. x86 only.
        /// </summary>
        static unsafe int TileMapDump(string client, int pf)
        {
            if (IntPtr.Size != 4) { Console.Error.WriteLine("x86 build required"); return 1; }
            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool SetDllDirectoryW(string path);
            [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)] static extern IntPtr LoadLibraryExA(string path, IntPtr file, uint flags);
            [DllImport("kernel32", CharSet = CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr m, string name);
            const uint LOAD_WITH_ALTERED_SEARCH_PATH = 8;
            SetDllDirectoryW(client);
            IntPtr Load(string name) => LoadLibraryExA(Path.Combine(client, name), IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
            [DllImport("kernel32")] static extern uint GetLastError();
            Load("msvcr100.dll"); Load("msvcp100.dll");
            IntPtr ser = IntPtr.Zero, ds = IntPtr.Zero;
            foreach (string pre in new[] { "Utils.dll", "serialize.dll", "BinaryStream.dll", "InstanceManager.dll",
                                           "DatabaseController.dll", "ResourceManager.dll", "GameData.dll",
                                           "Collision.dll", "PathFinder.dll", "DisplaySystem.dll", "TideRandy.dll" })
            {
                IntPtr h = Load(pre);
                if (h == IntPtr.Zero)
                {
                    Console.Error.WriteLine($"load {pre} failed (err {GetLastError()})");
                    if (pre == "serialize.dll" || pre == "Anarchy.dll") return 1;
                }
                if (pre == "serialize.dll") ser = h;
                if (pre == "DisplaySystem.dll") ds = h;
            }
            if (ser == IntPtr.Zero || ds == IntPtr.Zero) { Console.Error.WriteLine("serialize/DisplaySystem load failed"); return 1; }
            IntPtr Sym(IntPtr m, string n) { var p = GetProcAddress(m, n); if (p == IntPtr.Zero) throw new EntryPointNotFoundException(n); return p; }

            delegate* unmanaged[Thiscall]<IntPtr, void*, uint, IntPtr> memCtor = (delegate* unmanaged[Thiscall]<IntPtr, void*, uint, IntPtr>)Sym(ser, "??0MemoryIO_t@fun@@QAE@PBXI@Z");
            delegate* unmanaged[Thiscall]<IntPtr, void> memDtor = (delegate* unmanaged[Thiscall]<IntPtr, void>)Sym(ser, "??1MemoryIO_t@fun@@UAE@XZ");
            delegate* unmanaged[Thiscall]<IntPtr, IntPtr, bool, IntPtr> arcCtor = (delegate* unmanaged[Thiscall]<IntPtr, IntPtr, bool, IntPtr>)Sym(ser, "??0ObjectArchive_c@fun@@QAE@PAVIO_t@1@_N@Z");
            delegate* unmanaged[Thiscall]<IntPtr, void> arcDtor = (delegate* unmanaged[Thiscall]<IntPtr, void>)Sym(ser, "??1ObjectArchive_c@fun@@UAE@XZ");
            delegate* unmanaged[Thiscall]<IntPtr, int> arcCurId = (delegate* unmanaged[Thiscall]<IntPtr, int>)Sym(ser, "?GetCurrentObjID@ObjectArchive_c@fun@@QBEHXZ");
            delegate* unmanaged[Thiscall]<IntPtr, int, IntPtr> arcReadObj = (delegate* unmanaged[Thiscall]<IntPtr, int, IntPtr>)Sym(ser, "?ReadObject@ObjectArchive_c@fun@@QAEPAVSerializable_c@2@H@Z");
            delegate* unmanaged[Thiscall]<IntPtr, IntPtr, IntPtr> gdCtor = (delegate* unmanaged[Thiscall]<IntPtr, IntPtr, IntPtr>)Sym(ds, "??0AnarchyGroundData_t@@QAE@PAVObjectArchive_c@fun@@@Z");
            delegate* unmanaged[Thiscall]<IntPtr, void> gdDtor = (delegate* unmanaged[Thiscall]<IntPtr, void>)Sym(ds, "??1AnarchyGroundData_t@@UAE@XZ");
            delegate* unmanaged[Thiscall]<IntPtr, ushort, IntPtr> tileTex = (delegate* unmanaged[Thiscall]<IntPtr, ushort, IntPtr>)Sym(ds, "?TileTexture@AnarchyGroundData_t@@QBEPBVRTexture_t@@G@Z");

            string db = Path.Combine(client, "cd_image", "data", "db");
            using (var rdb = new Rdb(db))
            {
                byte[] g = rdb.Read(1000009, pf);
                if (g[0] != 'C' || g[1] != 'H' || g[2] != 'G' || g[3] != 'A') { Console.WriteLine("pf {0}: no CHGA record", pf); return 1; }
                int at = Util.IndexOf(g, "AnarchyGroundDataDB_t") - 21;   // the reflective block's start
                Console.WriteLine($"pf {pf}: record {g.Length} bytes, reflective block at 0x{at:X}");
                IntPtr data = Marshal.AllocHGlobal(g.Length - at);
                IntPtr io = Marshal.AllocHGlobal(0x100);
                IntPtr arc = Marshal.AllocHGlobal(0x200);
                IntPtr gd = Marshal.AllocHGlobal(1 << 24);
                try
                {
                    Marshal.Copy(g, at, data, g.Length - at);
                    ZeroMem(io, 0x100); ZeroMem(arc, 0x200); ZeroMem(gd, 1 << 24);
                    memCtor(io, (void*)data, (uint)(g.Length - at));
                    arcCtor(arc, io, false);
                    delegate* unmanaged[Thiscall]<IntPtr, IntPtr, void> arcLoad = (delegate* unmanaged[Thiscall]<IntPtr, IntPtr, void>)Sym(ser, "?LoadFromStream@ObjectArchive_c@fun@@QAEXPAVIO_t@2@@Z");
                    arcLoad(arc, io);
                    int cur = arcCurId(arc);
                    Console.WriteLine("archive loaded, current obj id " + cur);

                    IntPtr ground = gdCtor(gd, arc);
                    Console.WriteLine("AnarchyGroundData_t @0x" + ((long)ground).ToString("X"));
                    int total = arcCurId(arc);
                    Console.WriteLine("archive objects: " + total);

                    // pointer -> archive object id (0-based; my texsheet numbering = the same order)
                    var byPtr = new Dictionary<long, int>();
                    for (int i = 0; i < Math.Min(total, 2000); i++)
                    {
                        IntPtr o = arcReadObj(arc, i);
                        if (o != IntPtr.Zero && !byPtr.ContainsKey((long)o)) byPtr[(long)o] = i;
                    }
                    Console.WriteLine(byPtr.Count + " mapped object pointer(s)");

                    // every distinct tile u16 from the shipped ground.bin
                    string gpath = Path.Combine("F:", "testcellao", "AOBuddy10", "AOBuddy", "GameData", "Nav", pf.ToString(), "ground.bin");
                    ushort[] tiles = null; int tw = 0, th = 0;
                    if (File.Exists(gpath))
                    {
                        var (t2, w2, h2) = ReadAongTiles(gpath);
                        tiles = t2; tw = w2; th = h2;
                        Console.WriteLine($"ground.bin: {tw}x{th} cells");
                    }
                    var distinct = new List<ushort>();
                    if (tiles != null) { var s = new HashSet<ushort>(tiles); distinct = s.OrderBy(v => v).ToList(); }
                    else for (int v = 0; v < 256; v++) distinct.Add((ushort)v);
                    Console.WriteLine(distinct.Count + " distinct tile value(s)");
                    foreach (ushort v in distinct)
                    {
                        IntPtr tex = tileTex(gd, v);
                        int id = tex != IntPtr.Zero && byPtr.TryGetValue((long)tex, out int i) ? i : -1;
                        Console.WriteLine($"  tile 0x{v:X4} (lo {v & 0xFF,3}, hi {v >> 8,3}) -> {(tex == IntPtr.Zero ? "NULL" : "obj " + id)}");
                    }
                    gdDtor(gd);
                    arcDtor(arc);
                    memDtor(io);
                }
                finally
                {
                    Marshal.FreeHGlobal(data); Marshal.FreeHGlobal(io); Marshal.FreeHGlobal(arc); Marshal.FreeHGlobal(gd);
                }
            }
            return 0;
        }

        /// <summary>ground.bin (AONG v2-4) → just the per-cell tile u16s.</summary>
        static (ushort[] tiles, int w, int h) ReadAongTiles(string path)
        {
            using (var r = new BinaryReader(File.OpenRead(path)))
            {
                if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "AONG") throw new InvalidDataException(path + ": not AONG");
                int ver = r.ReadInt32();
                int w = r.ReadInt32(), h = r.ReadInt32();
                r.ReadSingle(); r.ReadSingle(); r.ReadInt32();          // cell, heightScale, sourceBits
                if (ver >= 3) { int wc = r.ReadInt32(); r.BaseStream.Seek(wc * 4, SeekOrigin.Current); }
                if (ver >= 4)
                {
                    int qc = r.ReadInt32();
                    for (int q = 0; q < qc; q++)
                    {
                        int pts = r.ReadInt32(); r.ReadSingle();
                        r.BaseStream.Seek(pts * 8, SeekOrigin.Current);
                    }
                }
                int rawLen = r.ReadInt32(), zLen = r.ReadInt32();
                byte[] raw = Util.ZlibDecompress(r.ReadBytes(zLen));
                var tiles = new ushort[(w - 1) * (h - 1)];
                Buffer.BlockCopy(raw, w * h * 2, tiles, 0, tiles.Length * 2);
                return (tiles, w - 1, h - 1);
            }
        }

        // ---------------------------------------------------------------- grounddump: the client's own tilemap decompression

        /// <summary>
        /// Hosts N3.dll, runs the client's RDBPlayfield_t::ReadBlob over the framed playfield record,
        /// then scans committed memory for the u16 array DecompressTileMap must have produced: the
        /// ground's per-cell values, whatever transform the client applies. A run is any sequence of
        /// u16s whose high bytes stay in the observed flag set; runs over 64 Ki entries get dumped.
        /// 2026-09-26, the tile-color hunt: the record's own bytes render speckled under every direct
        /// reading, so the client's decode is the only remaining authority. x86 only.
        /// </summary>
        static unsafe int HostGround(string client, int pf)
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
                    var lsCtor2 = (delegate* unmanaged[Thiscall]<IntPtr, IntPtr, uint, IntPtr>)Sym(bs, "??0BinaryLStream@@QAE@PAXI@Z");
                    lsCtor2(strm, data, (uint)blob.Length);
                    bool ok = (pfRead(obj, strm) & 1) != 0;
                    Console.WriteLine("ReadBlob -> " + ok);
                    if (ok) ScanForTileArrays(pf);
                    pfDtor(obj);
                }
                finally
                {
                    Marshal.FreeHGlobal(data); Marshal.FreeHGlobal(ident); Marshal.FreeHGlobal(obj); Marshal.FreeHGlobal(strm);
                }
            }
            return 0;
        }

        [DllImport("kernel32")] static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32")] static extern int VirtualQueryEx(IntPtr proc, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION info, int len);

        [StructLayout(LayoutKind.Sequential)]
        struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress; public IntPtr AllocationBase; public uint AllocationProtect;
            public UIntPtr RegionSize; public uint State; public uint Protect; public uint Type;
        }

        static unsafe void ScanForTileArrays(int pf)
        {
            Console.WriteLine("scanning committed memory for tile u16 runs...");
            var mbi = new MEMORY_BASIC_INFORMATION();
            IntPtr addr = IntPtr.Zero;
            var hits = new List<(IntPtr at, int len)>();
            while (VirtualQueryEx(GetCurrentProcess(), addr, out mbi, sizeof(MEMORY_BASIC_INFORMATION)) == sizeof(MEMORY_BASIC_INFORMATION))
            {
                long size = (long)(uint)mbi.RegionSize;
                if (mbi.State == 0x1000 /* committed */ && (mbi.Protect & 0xFF) >= 0x02 && (mbi.Protect & 0x100) == 0 /* readable, no guard */
                    && size <= (64 << 20) && (long)mbi.BaseAddress > 0x10000)
                {
                    byte* p = (byte*)mbi.BaseAddress;
                    int n = (int)size / 2;
                    int run = 0;
                    for (int i = 0; i < n; i++)
                    {
                        ushort v = *(ushort*)(p + i * 2);
                        int hi = v >> 8;
                        bool okv = hi == 0 || hi == 1 || hi == 64 || hi == 65 || hi == 128 || hi == 129 || hi == 192 || hi == 193;
                        if (okv) run++;
                        else
                        {
                            if (run >= 65536) hits.Add(((IntPtr)(p + (i - run) * 2), run));
                            run = 0;
                        }
                    }
                    if (run >= 65536) hits.Add(((IntPtr)(p + (n - run) * 2), run));
                }
                addr = (IntPtr)((long)mbi.BaseAddress + size);
                if ((long)addr > 0x7FFFFFF0) break;
            }
            Console.WriteLine("{0} candidate run(s)", hits.Count);
            foreach (var (at, len) in hits.OrderByDescending(h => h.len).Take(8))
            {
                ushort* u = (ushort*)at;
                var hiC = new Dictionary<int, int>();
                var loC = new Dictionary<int, int>();
                for (int i = 0; i < len; i++) { int h = u[i] >> 8; hiC[h] = hiC.TryGetValue(h, out var a) ? a + 1 : 1; }
                for (int i = 0; i < Math.Min(len, 200000); i++) { int l = u[i] & 0xFF; loC[l] = loC.TryGetValue(l, out var a2) ? a2 + 1 : 1; }
                Console.WriteLine("  run @0x{0:X} len {1}  hi: {2}  distinct lo (first 200k): {3}", (long)at, len,
                    string.Join(" ", hiC.OrderByDescending(k => k.Value).Take(8).Select(k => $"{k.Key}x{k.Value}")),
                    loC.Count);
                Console.WriteLine("    first 32: " + string.Join(" ", Enumerable.Range(0, Math.Min(32, len)).Select(i => $"0x{u[i]:X4}")));
                string file = Path.Combine(Path.GetTempPath(), $"grounddump-{pf}-0x{(long)at:X}.bin");
                using (var f = File.Create(file))
                {
                    var buf = new byte[Math.Min(len, 1_200_000) * 2];
                    Marshal.Copy(at, buf, 0, buf.Length);
                    f.Write(buf, 0, buf.Length);
                }
                Console.WriteLine("    dumped " + file);
            }
        }

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
