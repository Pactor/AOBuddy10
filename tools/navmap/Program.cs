using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using AOBuddy;

// NAVMAP — renders what the bot's nav data sees for one playfield (2026-09-25, the Newland Desert
// phantom lake: the owner wanted to SEE the ground.bin the bot walks on). One pixel per heightfield
// cell, coloured by the bot's own answers, so the picture cannot drift from the bot's logic:
//
//   terrain     grey, dark = low, bright = high
//   band        magenta tint — the tile-12 cells SwimY's region flood starts from
//   water       blue tint, darker = deeper — SwimY's verdict after BuildWater's gates
//   basins      green tint, plus a number at each big bowl's deepest point — every interior
//               depression filled to its rim (priority flood): the candidate lakes the terrain
//               alone cannot confirm or deny (Newland Desert: the real lake at (2349,1127) is
//               one of ~50; the fan map says a handful hold water)
//   --mark      red cross at x,z
//
//   navmap [pluginDir] [pf] [--mark x,z[:label]]... [-o out.png]
//
// The console legend carries the numbers (planes, band %, verdict, the basins' area/rim/bed) —
// the picture carries the shape.

internal static class Program
{
    private static int Main(string[] args)
    {
        string pluginDir = null; int pf = 0; string outPath = null;
        var marks = new List<(float x, float z, string label)>();
        var asks = new List<(float x, float z)>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--ask" && i + 1 < args.Length)
            {
                var xy = args[++i].Split(',');
                if (xy.Length == 2 && float.TryParse(xy[0], System.Globalization.CultureInfo.InvariantCulture, out float ax)
                                   && float.TryParse(xy[1], System.Globalization.CultureInfo.InvariantCulture, out float az))
                    asks.Add((ax, az));
            }
            else if (args[i] == "--mark" && i + 1 < args.Length)
            {
                string s = args[++i]; string lbl = null;
                int c = s.IndexOf(':');
                if (c >= 0) { lbl = s.Substring(c + 1); s = s.Substring(0, c); }
                var xy = s.Split(',');
                if (xy.Length == 2 && float.TryParse(xy[0], System.Globalization.CultureInfo.InvariantCulture, out float mx)
                                   && float.TryParse(xy[1], System.Globalization.CultureInfo.InvariantCulture, out float mz))
                    marks.Add((mx, mz, lbl ?? ("M" + (marks.Count + 1))));
                else { Console.WriteLine($"--mark '{args[i]}' not x,z[:label]"); return 2; }
            }
            else if (args[i] == "-o" && i + 1 < args.Length) outPath = args[++i];
            else if (pf == 0 && int.TryParse(args[i], out int p)) pf = p;
            else if (pluginDir == null) pluginDir = args[i];
        }
        pluginDir ??= Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Build", "Plugins", "AOBuddy"));
        if (pf == 0) pf = 565;
        outPath ??= $"navmap-{pf}.png";

        var nav = AOBuddyNav.Load(pluginDir, pf);
        if (nav?.Ground == null) { Console.WriteLine($"no ground data for pf {pf} under {pluginDir}"); return 1; }
        var g = nav.Ground;
        foreach (var (ax, az) in asks)
        {
            double terrain = g.HeightAt(ax, az);
            double sw = g.SwimY(ax, az, 0.3);
            Console.WriteLine($"ask ({ax:0.#},{az:0.#}): terrain {(double.IsNaN(terrain) ? "outside" : terrain.ToString("0.00"))}  tile {g.TileAt(ax, az)}  SwimY={(double.IsNaN(sw) ? "dry" : sw.ToString("0.00"))}");
        }
        if (asks.Count > 0) return 0;

        // ---- the bot's own answers, per cell ------------------------------------------------------
        int w = g.SamplesX - 1, h = g.SamplesZ - 1, n = w * h;
        float cell = g.Cell;
        float Low(int iz, int ix) => g.Heights[iz * g.SamplesX + ix] * g.HeightScale;   // a sample corner
        float CornerMin(int iz, int ix) => Math.Min(Math.Min(Low(iz, ix), Low(iz, ix + 1)), Math.Min(Low(iz + 1, ix), Low(iz + 1, ix + 1)));

        bool[] band = new bool[n];
        int seeds = 0;
        for (int i = 0; i < n; i++) if ((g.Tiles[i] & 0xFF) == 12) { band[i] = true; seeds++; }

        // force BuildWater once (SwimY is lazy), then ask it everywhere
        g.SwimY(cell / 2, cell / 2, 0.3);
        bool[] wet = new bool[n]; float[] depth = new float[n];
        for (int iz = 0; iz < h; iz++)
            for (int ix = 0; ix < w; ix++)
            {
                double sw = g.SwimY((ix + 0.5f) * cell, (iz + 0.5f) * cell, 0.3);
                if (double.IsNaN(sw)) continue;
                wet[iz * w + ix] = true;
                depth[iz * w + ix] = (float)(sw - g.HeightAt((ix + 0.5f) * cell, (iz + 0.5f) * cell));
            }

        // ---- interior basins: the filled-to-rim DEM (priority flood) -------------------------------
        float[] spill = new float[n]; bool[] done = new bool[n];
        var heap = new Heap(n);
        for (int x = 0; x < w; x++) { Push(x); Push((h - 1) * w + x); }
        for (int z = 0; z < h; z++) { Push(z * w); Push(z * w + w - 1); }
        void Push(int c) { if (!done[c]) { done[c] = true; spill[c] = CornerMin(c / w, c % w); heap.Add(spill[c], c); } }
        while (heap.Count > 0)
        {
            var (lvl, c) = heap.Pop();
            int cx = c % w, cz = c / w;
            for (int k = 0; k < 4; k++)
            {
                int nx = cx + (k == 0 ? -1 : k == 1 ? 1 : 0), nz = cz + (k == 2 ? -1 : k == 3 ? 1 : 0);
                if (nx < 0 || nz < 0 || nx >= w || nz >= h) continue;
                int nc = nz * w + nx;
                if (done[nc]) continue;
                done[nc] = true;
                float l = Math.Max(lvl, CornerMin(nz, nx));   // water entering this cell spills at the higher of rim and floor
                spill[nc] = l;
                heap.Add(l, nc);
            }
        }
        bool[] bowl = new bool[n];
        for (int i = 0; i < n; i++) bowl[i] = spill[i] - CornerMin(i / w, i % w) > 0.5f;

        // bowls as components: area, rim, bed, deepest point
        var bowls = new List<(int cells, float rim, float bed, int deepest)>();
        int[] bowlNo = new int[n];
        var q = new Queue<int>();
        for (int i = 0; i < n; i++)
        {
            if (!bowl[i] || bowlNo[i] != 0) continue;
            int no = bowls.Count + 1; bowlNo[i] = no; q.Clear(); q.Enqueue(i);
            int cnt = 0, deep = i; float rim = float.MinValue, bed = float.MaxValue;
            while (q.Count > 0)
            {
                int c = q.Dequeue(); cnt++;
                int cx = c % w, cz = c / w;
                float low = CornerMin(cz, cx);
                if (low < bed) { bed = low; deep = c; }
                rim = Math.Max(rim, spill[c]);
                for (int k = 0; k < 4; k++)
                {
                    int nx = cx + (k == 0 ? -1 : k == 1 ? 1 : 0), nz = cz + (k == 2 ? -1 : k == 3 ? 1 : 0);
                    if (nx < 0 || nz < 0 || nx >= w || nz >= h) continue;
                    int nc = nz * w + nx;
                    if (!bowl[nc] || bowlNo[nc] != 0) continue;
                    bowlNo[nc] = no; q.Enqueue(nc);
                }
            }
            bowls.Add((cnt, rim, bed, deep));
        }
        bowls.Sort((a, b) => b.cells.CompareTo(a.cells));
        var rankOf = new Dictionary<int, int>();   // deepest cell -> rank (1 = biggest bowl)
        for (int r = 0; r < bowls.Count; r++) rankOf[bowls[r].deepest] = r + 1;

        // ---- render --------------------------------------------------------------------------------
        float hmin = float.MaxValue, hmax = float.MinValue;
        foreach (ushort v in g.Heights) { float hv = v * g.HeightScale; if (hv < hmin) hmin = hv; if (hv > hmax) hmax = hv; }
        byte[] img = new byte[n * 3];
        for (int i = 0; i < n; i++)
        {
            float t = (CornerMin(i / w, i % w) - hmin) / Math.Max(0.1f, hmax - hmin);
            int v = 38 + (int)(190 * t);
            int r = v, gr = v, b = v;
            if (band[i]) Blend(255, 0, 255, 70, ref r, ref gr, ref b);
            if (bowl[i]) Blend(70, 230, 70, 60, ref r, ref gr, ref b);
            if (wet[i]) Blend(50, 100, 230, (int)Math.Min(210, 90 + depth[i] * 22), ref r, ref gr, ref b);
            img[i * 3] = (byte)r; img[i * 3 + 1] = (byte)gr; img[i * 3 + 2] = (byte)b;
        }
        static void Blend(int br, int bg, int bb, int a, ref int r, ref int gr, ref int b)
        { r += (br - r) * a / 255; gr += (bg - gr) * a / 255; b += (bb - b) * a / 255; }

        // grid every 256 m
        int step = Math.Max(1, (int)Math.Round(256 / cell));
        for (int x = 0; x < w; x += step) for (int z = 0; z < h; z++) Lighten(z * w + x);
        for (int z = 0; z < h; z += step) for (int x = 0; x < w; x++) Lighten(z * w + x);
        void Lighten(int i) { img[i * 3] = (byte)Math.Min(255, img[i * 3] + 28); img[i * 3 + 1] = (byte)Math.Min(255, img[i * 3 + 1] + 28); img[i * 3 + 2] = (byte)Math.Min(255, img[i * 3 + 2] + 28); }

        // bowl numbers at their deepest point (the big ones only)
        foreach (var kvp in rankOf.Where(k => bowls[k.Value - 1].cells * cell * cell >= 1200))
            DrawNumber(img, w, h, kvp.Key % w, kvp.Key / w, kvp.Value);
        // markers
        foreach (var (mx, mz, _) in marks)
        {
            int ix = (int)(mx / cell), iz = (int)(mz / cell);
            for (int d = -5; d <= 5; d++)
            {
                Set(ix + d, iz, 255, 70, 70); Set(ix, iz + d, 255, 70, 70);
                Set(ix + d, iz - 1, 255, 70, 70); Set(ix + d, iz + 1, 255, 70, 70);
            }
        }
        void Set(int x, int z, int r, int gr, int b)
        {
            if (x < 0 || z < 0 || x >= w || z >= h) return;
            img[(z * w + x) * 3] = (byte)r; img[(z * w + x) * 3 + 1] = (byte)gr; img[(z * w + x) * 3 + 2] = (byte)b;
        }

        File.WriteAllBytes(outPath, Png(img, w, h));

        // ---- console legend ------------------------------------------------------------------------
        Console.WriteLine($"== pf {pf} {nav.Name}: what the bot's nav data sees ==");
        Console.WriteLine($"  heightfield {g.SamplesX}x{g.SamplesZ} samples, cell {cell:0} m (image {w}x{h} px, 1 px = 1 cell); heights {hmin:0.0}..{hmax:0.0}");
        Console.WriteLine($"  stored water planes: {(g.WaterY.Length == 0 ? "none" : string.Join(", ", g.WaterY.Select(y => y.ToString("0.0"))))}");
        Console.WriteLine($"  tile-12 band: {seeds} cells ({100.0 * seeds / n:0.0}% of the map) [magenta]");
        Console.WriteLine($"  water verdict: {g.WaterInfo()} [blue]");
        var bowlsBig = bowls.Where(b => b.cells * cell * cell >= 1200).ToList();
        Console.WriteLine($"  interior basins (filled to rim, >= 1200 m²): {bowlsBig.Count} of {bowls.Count}, {bowlsBig.Sum(b => b.cells) * cell * cell / 10000.0:0.0} ha total [green, number = rank]");
        foreach (var b in bowlsBig.Take(25).Select((b, k) => (b, k: k + 1)))
            Console.WriteLine($"     #{b.k,-2} {b.b.cells * cell * cell,9:0} m²  rim {b.b.rim,5:0.0}  bed {b.b.bed,5:0.0}  deepest at ({b.b.deepest % w * cell},{b.b.deepest / w * cell})");
        if (marks.Count > 0)
            foreach (var m in marks) Console.WriteLine($"  mark {m.label}: ({m.x:0},{m.z:0})");
        Console.WriteLine($"  wrote {Path.GetFullPath(outPath)}");
        return 0;
    }

    // ---- a 3x5 digit font, black outline + white, so the numbers survive any background -----------
    private static readonly string[] Digits =
    {
        "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
        "111100111001111", "111100111101111", "111001010010010", "111101111101111", "111101111001111",
    };
    private static void DrawNumber(byte[] img, int w, int h, int x, int z, int number)
    {
        string s = number.ToString();
        int width = s.Length * 4 - 1;
        int x0 = Math.Max(1, Math.Min(w - width - 2, x - width / 2)), z0 = Math.Max(1, Math.Min(h - 7, z - 2));
        void Glyph(int ox, int oy, int r, int g, int b)
        {
            for (int ci = 0; ci < s.Length; ci++)
            {
                string glyph = Digits[s[ci] - '0'];
                for (int gy = 0; gy < 5; gy++) for (int gx = 0; gx < 3; gx++)
                    if (glyph[gy * 3 + gx] == '1') Pix(x0 + ci * 4 + gx + ox, z0 + gy + oy, r, g, b);
            }
        }
        // black at the eight neighbours, white in place: readable on any background
        for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
            if (dx != 0 || dy != 0) Glyph(dx, dy, 0, 0, 0);
        Glyph(0, 0, 255, 255, 255);
        void Pix(int px, int pz, int r, int gr, int b)
        {
            if (px < 0 || pz < 0 || px >= w || pz >= h) return;
            img[(pz * w + px) * 3] = (byte)r; img[(pz * w + px) * 3 + 1] = (byte)gr; img[(pz * w + px) * 3 + 2] = (byte)b;
        }
    }

    // ---- smallest PNG writer that could work: 8-bit RGB, filter 0, zlib via BCL -------------------
    private static byte[] Png(byte[] rgb, int w, int h)
    {
        byte[] raw = new byte[h * (w * 3 + 1)];
        for (int y = 0; y < h; y++)
        {
            raw[y * (w * 3 + 1)] = 0;
            Buffer.BlockCopy(rgb, y * w * 3, raw, y * (w * 3 + 1) + 1, w * 3);
        }
        byte[] z;
        using (var ms = new MemoryStream())
        {
            using (var zs = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true)) zs.Write(raw);
            z = ms.ToArray();
        }
        var outp = new MemoryStream();
        outp.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var ihdr = new byte[13];
        int bw = w, bh = h;
        for (int i = 3; i >= 0; i--) { ihdr[i] = (byte)bw; bw >>= 8; }
        for (int i = 7; i >= 4; i--) { ihdr[i] = (byte)bh; bh >>= 8; }
        ihdr[8] = 8; ihdr[9] = 2;   // 8-bit truecolour
        Chunk(outp, "IHDR", ihdr);
        Chunk(outp, "IDAT", z);
        Chunk(outp, "IEND", new byte[0]);
        return outp.ToArray();
    }
    private static void Chunk(Stream s, string tag, byte[] body)
    {
        var len = new byte[4]; int v = body.Length; for (int i = 3; i >= 0; i--) { len[i] = (byte)v; v >>= 8; }
        s.Write(len);
        var tb = System.Text.Encoding.ASCII.GetBytes(tag);
        s.Write(tb);
        s.Write(body);
        uint crc = Crc32(tb, body);
        var cb = new byte[4]; for (int i = 3; i >= 0; i--) { cb[i] = (byte)crc; crc >>= 8; }
        s.Write(cb);
    }
    private static uint[] _crcTable;
    private static uint Crc32(byte[] a, byte[] b)
    {
        if (_crcTable == null)
        {
            _crcTable = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                uint c = (uint)i;
                for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                _crcTable[i] = c;
            }
        }
        uint crc = 0xFFFFFFFFu;
        foreach (byte x in a) crc = _crcTable[(crc ^ x) & 0xFF] ^ (crc >> 8);
        foreach (byte x in b) crc = _crcTable[(crc ^ x) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }
}

/// <summary>A binary heap of (float, int) sized up front — the priority flood's queue.</summary>
internal sealed class Heap
{
    private float[] _k; private int[] _v; private int _n;
    public Heap(int cap) { _k = new float[Math.Max(16, cap / 4)]; _v = new int[_k.Length]; }
    public int Count => _n;
    public void Add(float k, int v)
    {
        if (_n == _k.Length) { Array.Resize(ref _k, _n * 2); Array.Resize(ref _v, _n * 2); }
        int i = _n++; _k[i] = k; _v[i] = v;
        while (i > 0) { int p = (i - 1) / 2; if (_k[p] <= _k[i]) break; Swap(p, i); i = p; }
    }
    public (float, int) Pop()
    {
        float k = _k[0]; int v = _v[0];
        _n--; _k[0] = _k[_n]; _v[0] = _v[_n];
        int i = 0;
        for (; ; )
        {
            int l = 2 * i + 1, r = l + 1, m = i;
            if (l < _n && _k[l] < _k[m]) m = l;
            if (r < _n && _k[r] < _k[m]) m = r;
            if (m == i) break;
            Swap(m, i); i = m;
        }
        return (k, v);
    }
    private void Swap(int a, int b) { float tk = _k[a]; _k[a] = _k[b]; _k[b] = tk; int tv = _v[a]; _v[a] = _v[b]; _v[b] = tv; }
}