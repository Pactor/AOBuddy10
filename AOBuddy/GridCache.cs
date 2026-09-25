using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace AOBuddy
{
    /// <summary>
    /// Disk cache for built walk grids (2026-09-25; the owner asked for navdata to be created once, not
    /// per entry). Building a zone's OverlandGrid/FloorGrid takes 0.6-3.8 s of stamping every time it is
    /// entered — and each zone used to build TWICE per trip (R1.9 gave every consumer its own NavGridCache).
    /// The build is deterministic from the zone's data files, so the finished grid is serialized next to the
    /// plugin as GameData/gridcache/&lt;pf&gt;.bin and reloaded in a fraction of the time. A cache file is valid
    /// only when CodeVersion (bumped whenever the stamping/search data layout changes) and a content hash of
    /// the zone's ground/walls/collision inputs match; anything else rebuilds and rewrites. Mission-instance
    /// layouts are never cached (instanced, tiny, built per mission).
    /// </summary>
    public static class GridCache
    {
        public const int CodeVersion = 5;   // 5: 16 floors per cell (ICC tower stacks) + finer cells

        private static readonly byte[] Magic = { (byte)'A', (byte)'O', (byte)'G', (byte)'C' };

        private static string PathFor(string pluginDir, int pf)
            => System.IO.Path.Combine(pluginDir, "GameData", "gridcache", pf + ".bin");

        /// <summary>The zone's cached grid when it is current, else null (the caller builds and saves).</summary>
        public static IWalkGrid TryLoad(string pluginDir, int pf, AOBuddyNav nav, Action<string> log)
        {
            string path = PathFor(pluginDir, pf);
            try
            {
                if (!File.Exists(path)) return null;
                using (var fs = File.OpenRead(path))
                using (var br = new BinaryReader(fs))
                {
                    byte[] m = br.ReadBytes(4);
                    if (m.Length != 4 || m[0] != Magic[0] || m[1] != Magic[1] || m[2] != Magic[2] || m[3] != Magic[3]) return null;
                    if (br.ReadInt32() != CodeVersion) return null;
                    if (br.ReadInt64() != InputHash(pluginDir, pf)) return null;
                    if (br.ReadInt32() != pf) return null;
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    using (var zs = new ZLibStream(fs, CompressionMode.Decompress, leaveOpen: true))
                    using (var br2 = new BinaryReader(zs))
                    {
                        IWalkGrid g = br2.ReadByte() == 1
                            ? (IWalkGrid)OverlandGrid.Read(br2, nav?.Ground, pf)
                            : FloorGrid.Read(br2, pf);
                        log?.Invoke($"GRIDCACHE: pf {pf} loaded from cache ({sw.ElapsedMilliseconds} ms).");
                        return g;
                    }
                }
            }
            catch { return null; }   // unreadable/corrupt: rebuild is the fallback, never a failure
        }

        /// <summary>Write the grid atomically (tmp + replace, the JsonStore pattern); never throws.</summary>
        public static void Save(string pluginDir, int pf, IWalkGrid grid, Action<string> log)
        {
            try
            {
                string path = PathFor(pluginDir, pf);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                string tmp = path + ".tmp";
                using (var fs = File.Create(tmp))
                {
                    using (var bw = new BinaryWriter(fs, System.Text.Encoding.UTF8, leaveOpen: true))
                    {
                        bw.Write(Magic);
                        bw.Write(CodeVersion);
                        bw.Write(InputHash(pluginDir, pf));
                        bw.Write(pf);
                    }
                    using (var zs = new ZLibStream(fs, CompressionLevel.Optimal, leaveOpen: true))
                    using (var bw2 = new BinaryWriter(zs))
                    {
                        if (grid is OverlandGrid og) { bw2.Write((byte)1); og.Write(bw2); }
                        else if (grid is FloorGrid fg) { bw2.Write((byte)2); fg.Write(bw2); }
                        else return;   // nothing else to cache
                    }
                }
                if (File.Exists(path)) File.Replace(tmp, path, null);
                else File.Move(tmp, path);
                log?.Invoke($"GRIDCACHE: pf {pf} saved ({new FileInfo(path).Length / 1024} KiB).");
            }
            catch (Exception ex) { log?.Invoke($"GRIDCACHE: couldn't save pf {pf}: {ex.Message}"); }
        }

        // FNV-1a over the zone's input files plus the code version: any change to ground/walls/collision
        // (or to the stamping logic, via CodeVersion) invalidates the cache.
        private static long InputHash(string pluginDir, int pf)
        {
            string folder = AOBuddyNav.FolderFor(pluginDir, pf);
            long h = unchecked((long)0xcbf29ce484222325L) ^ CodeVersion;
            foreach (string name in new[] { "ground.bin", "walls.bin", "collision.bin" })
            {
                string f = System.IO.Path.Combine(folder, name);
                if (!File.Exists(f)) { h = Mix(h, 0); continue; }
                using (var fs = File.OpenRead(f))
                {
                    var buf = new byte[1 << 16];
                    int n;
                    while ((n = fs.Read(buf, 0, buf.Length)) > 0)
                        for (int i = 0; i < n; i++) h = Mix(h, buf[i]);
                }
            }
            return h;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long Mix(long h, byte b)
        {
            h ^= b;
            h = (long)((ulong)h * 0x100000001b3UL);
            return h;
        }
    }
}