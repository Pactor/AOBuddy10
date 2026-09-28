using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AONavExtractor
{
    /// <summary>
    /// --saavick: crops each playfield's region out of Saavick's Map of Rubi-Ka (shipped with the client in
    /// cd_image/textures/PlanetMap/SaavicksMap) and writes &lt;pf&gt;/map.png + &lt;pf&gt;/map.json into a Nav folder,
    /// so the monitor never needs the client's PlanetMap folder at run time.
    ///
    ///   AONavExtractor --saavick &lt;AO install dir&gt; &lt;Nav dir&gt; [--only pf,pf] [--outdoor]
    ///
    /// By default only static dungeons (a rooms.json and no ground.bin in the Nav folder) get a map; --outdoor
    /// adds the outdoor zones too (the monitor keeps terrain.png as the outdoor default either way).
    ///
    /// Format (verified 2026-09-28): saavicksmap.txt lists levels (File / TextureSize 128 / Size W H /
    /// Tiles TX TY / MapRect x0 y0 x1 y1 / one FilePos per tile). Each tile is a 128x128 8-bit RGB PNG at
    /// its FilePos in the .bin; tiles are stored COLUMN-major (index = col*TY + row) — row-major assembles
    /// scrambled. Of the two 9728x9216 levels, saavicksmap3-2x is the labelled colour map (shops, "ENTRANCE",
    /// bosses); saavicksmap4-2x is the grey monster layer. We take the first largest level in the file = 3-2x.
    ///
    /// World -> pixel on that level (px right, py DOWN; x/z = playfield-local world metres):
    ///   px = RX + KX * xscale * (pfX + x - X0)
    ///   py = RZ - KZ * zscale * (pfZ + z - Z0)
    /// with pfX/pfZ/xscale/zscale from coords.xml. Every playfield is scaled about ONE fixed world point
    /// (X0, Z0) ~ the MapRect's bottom-left corner. The constants are fitted, not guessed:
    ///   - KX, KZ and the xscale=1 intercepts: normalized cross-correlation of 15 outdoor zones' own terrain.png
    ///     against the map (residuals within +-4 px at 1x);
    ///   - the xscale=7.9 intercepts: the walkable cells of 8 dungeons' rooms.json (127,1931,1933,1941,4805,
    ///     125,120,1943) correlated against their insets (92-99% of cells land on drawn floor); with the slope
    ///     fixed to KX*7.9, all 8 intercepts agree to +-4 px — one formula, not per-dungeon offsets;
    ///   - two intercepts per axis give (RX, X0) and (RZ, Z0). Independent check at xscale 2.63333:
    ///     Montroyal City lands within ~1 px.
    /// </summary>
    static class SaavicksMap
    {
        // level-3 2x pixel frame (9728x9216, MapRect 440 0 7560 9164)
        const double KX = 0.380304, KZ = 0.380132;          // px per world unit
        const double RX = 431.47, X0 = 31022.0;
        const double RZ = 9165.73, Z0 = 24879.85;
        const int RefRectW = 7120;                          // MapRect width the constants were fitted on
        const double DungeonMarginM = 30;                   // labels ("ENTRANCE", boss names) sit beside the rooms

        sealed class Level { public string File; public int W, H, TX, TY; public int[] Rect; public List<long> Pos = new List<long>(); }

        public static int Run(string[] a)
        {
            string client = a[1], navDir = Path.GetFullPath(a[2]);
            bool outdoor = false; var only = new HashSet<int>();
            for (int i = 3; i < a.Length; i++)
            {
                if (a[i] == "--outdoor") outdoor = true;
                else if (a[i] == "--only") foreach (var s in a[++i].Split(',')) only.Add(int.Parse(s));
                else throw new ArgumentException("unknown option " + a[i]);
            }
            string dir = Path.Combine(client, "cd_image", "textures", "PlanetMap", "SaavicksMap");
            var levels = ReadLevels(Path.Combine(dir, "saavicksmap.txt"));
            var lv = levels.OrderByDescending(l => (long)l.W * l.H).First();   // stable: first of the largest = 3-2x
            if (lv.Pos.Count != lv.TX * lv.TY) throw new InvalidDataException($"{lv.File}: {lv.Pos.Count} FilePos for {lv.TX}x{lv.TY} tiles");
            double f = (lv.Rect[2] - lv.Rect[0]) / (double)RefRectW;          // 1.0 on 3-2x
            byte[] bin = File.ReadAllBytes(Path.Combine(dir, Path.GetFileName(lv.File)));
            Console.WriteLine("Saavick level {0}: {1}x{2}, {3}x{4} tiles, scale {5}", lv.File, lv.W, lv.H, lv.TX, lv.TY, f);
            var tileCache = new Dictionary<int, Png>();

            string xml = File.ReadAllText(Path.Combine(dir, "coords.xml"));
            int written = 0;
            foreach (Match m in Regex.Matches(xml, "<Playfield\\s+id=\"(\\d+)\"\\s+name=\"([^\"]*)\"\\s+x=\"([-\\d.]+)\"\\s+xscale=\"([-\\d.]+)\"\\s+z=\"([-\\d.]+)\"\\s+zscale=\"([-\\d.]+)\""))
            {
                int pf = int.Parse(m.Groups[1].Value);
                if (only.Count > 0 && !only.Contains(pf)) continue;
                string name = m.Groups[2].Value;
                double pfX = D(m.Groups[3].Value), xs = D(m.Groups[4].Value), pfZ = D(m.Groups[5].Value), zs = D(m.Groups[6].Value);
                if (xs < 0.01 || zs < 0.01) continue;                          // the Grid / Fixer Grid legend entries
                string folder = Path.Combine(navDir, pf.ToString());
                bool hasGround = File.Exists(Path.Combine(folder, "ground.bin")), hasRooms = File.Exists(Path.Combine(folder, "rooms.json"));
                double wx0, wz0, wx1, wz1;
                if (hasRooms && !hasGround)
                {
                    if (!RoomBounds(Path.Combine(folder, "rooms.json"), out wx0, out wz0, out wx1, out wz1)) continue;
                    wx0 -= DungeonMarginM; wz0 -= DungeonMarginM; wx1 += DungeonMarginM; wz1 += DungeonMarginM;
                }
                else if (hasGround && outdoor)
                {
                    using var info = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "info.json")));
                    var ws = info.RootElement.GetProperty("ground").GetProperty("worldSize");
                    wx0 = 0; wz0 = 0; wx1 = ws[0].GetDouble(); wz1 = ws[1].GetDouble();
                }
                else continue;

                double sx = KX * f * xs, sz = KZ * f * zs;
                double ox = RX * f + sx * (pfX - X0), oz = RZ * f - sz * (pfZ - Z0);   // full-image pixel of world (0,0)
                int cx0 = Math.Max(0, (int)Math.Floor(ox + sx * wx0)), cx1 = Math.Min(lv.W, (int)Math.Ceiling(ox + sx * wx1));
                int cy0 = Math.Max(0, (int)Math.Floor(oz - sz * wz1)), cy1 = Math.Min(lv.H, (int)Math.Ceiling(oz - sz * wz0));
                if (cx1 - cx0 < 8 || cy1 - cy0 < 8) { Console.WriteLine("pf {0}: region off the map, skipped", pf); continue; }

                int w = cx1 - cx0, h = cy1 - cy0;
                var rgb = new byte[w * h * 3];
                for (int tc = cx0 / 128; tc <= (cx1 - 1) / 128; tc++)
                    for (int tr = cy0 / 128; tr <= (cy1 - 1) / 128; tr++)
                    {
                        int idx = tc * lv.TY + tr;                               // column-major
                        if (!tileCache.TryGetValue(idx, out var t))
                        {
                            t = Png.Parse(bin, (int)lv.Pos[idx], out _);
                            if (t.Width != 128 || t.Height != 128 || t.Channels < 3) throw new InvalidDataException($"tile {idx}: {t.Width}x{t.Height}x{t.Channels}");
                            tileCache[idx] = t;
                        }
                        for (int y = 0; y < 128; y++)
                        {
                            int iy = tr * 128 + y - cy0; if (iy < 0 || iy >= h) continue;
                            for (int x = 0; x < 128; x++)
                            {
                                int ix = tc * 128 + x - cx0; if (ix < 0 || ix >= w) continue;
                                int s = (y * 128 + x) * t.Channels, d = (iy * w + ix) * 3;
                                rgb[d] = t.Pixels[s]; rgb[d + 1] = t.Pixels[s + 1]; rgb[d + 2] = t.Pixels[s + 2];
                            }
                        }
                    }
                if (tileCache.Count > 4000) tileCache.Clear();
                File.WriteAllBytes(Path.Combine(folder, "map.png"), TileColors.EncodePng(rgb, w, h));

                var j = new StringBuilder();
                j.Append("{\n");
                j.AppendFormat(CultureInfo.InvariantCulture, " \"playfield\": {0},\n \"name\": \"{1}\",\n", pf, name.Replace("\"", "'"));
                j.AppendFormat(" \"source\": \"Saavick's Map of Rubi-Ka 3.3, {0}, crop {1},{2} {3}x{4}\",\n", Path.GetFileName(lv.File), cx0, cy0, w, h);
                j.Append(" \"transform\": \"px = originX + x * pxPerMetreX; py = originZ - z * pxPerMetreZ (image pixels, y down; x,z playfield world metres)\",\n");
                j.AppendFormat(CultureInfo.InvariantCulture, " \"width\": {0},\n \"height\": {1},\n", w, h);
                j.AppendFormat(CultureInfo.InvariantCulture, " \"originX\": {0:0.###},\n \"originZ\": {1:0.###},\n", ox - cx0, oz - cy0);
                j.AppendFormat(CultureInfo.InvariantCulture, " \"pxPerMetreX\": {0:0.######},\n \"pxPerMetreZ\": {1:0.######},\n", sx, sz);
                j.AppendFormat(CultureInfo.InvariantCulture, " \"coords\": {{ \"x\": {0}, \"xscale\": {1}, \"z\": {2}, \"zscale\": {3} }}\n", pfX, xs, pfZ, zs);
                j.Append("}\n");
                File.WriteAllText(Path.Combine(folder, "map.json"), j.ToString());
                Console.WriteLine("pf {0,-5} {1,-32} map.png {2}x{3} ({4:0.##} px/m)", pf, name, w, h, sx);
                written++;
            }
            Console.WriteLine("wrote {0} map(s) to {1}", written, navDir);
            return 0;
        }

        static double D(string s) => double.Parse(s, CultureInfo.InvariantCulture);

        static List<Level> ReadLevels(string txt)
        {
            var list = new List<Level>(); Level cur = null;
            foreach (var raw in File.ReadAllLines(txt))
            {
                var p = raw.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length < 2) continue;
                switch (p[0])
                {
                    case "File": cur = new Level { File = p[1] }; list.Add(cur); break;
                    case "Size": cur.W = int.Parse(p[1]); cur.H = int.Parse(p[2]); break;
                    case "Tiles": cur.TX = int.Parse(p[1]); cur.TY = int.Parse(p[2]); break;
                    case "MapRect": cur.Rect = p.Skip(1).Take(4).Select(int.Parse).ToArray(); break;
                    case "FilePos": cur.Pos.Add(long.Parse(p[1])); break;
                }
            }
            return list;
        }

        /// <summary>World bounds of every walkable room cell — the same cell→world walk as the monitor's plan
        /// (MapRender.BuildPlan) and NavDungeon.CellOf's inverse.</summary>
        static bool RoomBounds(string path, out double x0, out double z0, out double x1, out double z1)
        {
            x0 = z0 = double.MaxValue; x1 = z1 = double.MinValue;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            double cell = doc.RootElement.GetProperty("cell").GetDouble();
            foreach (var rm in doc.RootElement.GetProperty("rooms").EnumerateArray())
            {
                int rot = rm.GetProperty("rot").GetInt32();
                var r = rm.GetProperty("rect").EnumerateArray().Select(e => e.GetInt32()).ToArray();
                var pos = rm.GetProperty("pos").EnumerateArray().Select(e => e.GetDouble()).ToArray();
                int turns = ((-rot) % 4 + 4) % 4;
                double ccx = (r[0] + r[2] + 1) / 2.0, ccz = (r[1] + r[3] + 1) / 2.0;
                int row = 0;
                foreach (var line in rm.GetProperty("tile").EnumerateArray())
                {
                    int col = 0;
                    foreach (var v in line.EnumerateArray())
                    {
                        if (v.GetInt32() != 0)
                        {
                            double dx = (r[0] + col + 0.5 - ccx) * cell, dz = (r[1] + row + 0.5 - ccz) * cell;
                            for (int i = 0; i < turns; i++) { double t = dx; dx = -dz; dz = t; }
                            double wx = pos[0] + dx, wz = pos[2] + dz;
                            if (wx < x0) x0 = wx; if (wx > x1) x1 = wx; if (wz < z0) z0 = wz; if (wz > z1) z1 = wz;
                        }
                        col++;
                    }
                    row++;
                }
            }
            return x0 <= x1;
        }
    }
}
