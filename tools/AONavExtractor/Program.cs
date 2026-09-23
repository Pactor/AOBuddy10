using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
                    Console.WriteLine("pf {0,-6} {1,-9} {2,-32} {3}", pf, kind, name.Length > 32 ? name.Substring(0, 32) : name,
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
