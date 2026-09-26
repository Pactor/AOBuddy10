using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace AONavExtractor
{
    /// <summary>
    /// The ground record's own tile textures, written out per playfield (2026-09-26, the owner's
    /// "it's only a heightmap" — and after a day of failed auto-composition, his "use the EXACT way you
    /// created the texsheet, I'll do the rest myself").
    ///
    /// The 'CHGA' record carries the textures itself: root.texture is a ref array of RTexture_t, each
    /// pointing at a GroundTextureCreator_t whose bitmap is a 16x16 RGB565 tile (pixelformat masks read
    /// from the record, 0xF800/0x07E0/0x001F on every zone probed). A heightfield cell's tile u16 (in
    /// ground.bin) carries a texture index in its low byte, rotation in the top two bits — but how the
    /// client turns those into the zone's look stayed unsolved, so the composition is the owner's now.
    ///
    /// Writes &lt;pf&gt;/terrain.png per playfield (the monitor's map background); --png additionally
    /// drops each texture as its own tile-NNN.png for eyeballing.
    /// </summary>
    public static class TileColors
    {
        public static int Run(string client, string outDir, string pfList, bool png)
        {
            string db = Path.Combine(client, "cd_image", "data", "db");
            using (var rdb = new Rdb(db))
                foreach (string s in pfList.Split(','))
                {
                    int pf = int.Parse(s.Trim());
                    byte[] g = rdb.Read(1000009, pf);
                    if (g[0] != 'C' || g[1] != 'H' || g[2] != 'G' || g[3] != 'A') { Console.WriteLine("pf {0}: no CHGA ground record, skipped", pf); continue; }
                    int at = Util.IndexOf(g, "AnarchyGroundDataDB_t");
                    var rec = new ReflectiveRecord(g, at - 21);
                    var root = rec.Root;

                    // the textures: root.texture's refs -> RTexture_t -> creator -> GroundTextureCreator_t bitmap
                    var texRgb = new List<byte[]>();      // the whole texture, w*h*3
                    var texW = new List<int>();
                    var texH = new List<int>();
                    var refs = root.Get("texture");
                    if (refs == null) { Console.WriteLine("pf {0}: no texture field", pf); continue; }
                    foreach (int ridx in refs.Ints)
                    {
                        var texR = rec.Ref(ridx);
                        var creatorRef = texR?.Get("creator");
                        var creator = creatorRef == null ? null : rec.Ref(creatorRef.Int);
                        var bmp = creator?.Get("bitmap");
                        int w = creator?.GetInt("width") ?? 0, h = creator?.GetInt("height") ?? 0;
                        if (bmp == null || w <= 0 || h <= 0 || bmp.Raw.Length < w * h * 2) { texRgb.Add(null); texW.Add(0); texH.Add(0); continue; }
                        texRgb.Add(DecodeRgb565(bmp.Raw, w, h, creator.Get("pixelformat")?.Raw));
                        texW.Add(w); texH.Add(h);
                    }
                    int n = texRgb.Count;

                    // texture -> type: tile_type_data is u32 byte-count + 255 u16s; values 0-21 are the
                    // base types, 22-254 run-length back into them (see the dump in the conversation log)
                    var typeTex = new int[0];
                    var ttd = root.Get("tile_type_data");
                    if (ttd != null)
                    {
                        byte[] blob = ttd.Raw;
                        int tn = Math.Min(blob.Length / 2, (blob.Length - 4) / 2);
                        typeTex = new int[tn];
                        for (int i = 0; i < tn; i++) typeTex[i] = BitConverter.ToUInt16(blob, 4 + i * 2);
                    }

                    string dir = Path.Combine(outDir, pf.ToString());
                    Directory.CreateDirectory(dir);

                    // --png: every texture as its own image (x8) - tile-<index>.png, for eyeballing
                    if (png)
                        for (int t = 0; t < n; t++)
                        {
                            byte[] src = texRgb[t];
                            if (src == null || texW[t] == 0 || texH[t] == 0) continue;
                            int w0 = texW[t], h0 = texH[t];
                            var one = new byte[w0 * sc * h0 * sc * 3];
                            for (int y = 0; y < h0; y++)
                                for (int x = 0; x < w0; x++)
                                    for (int dy = 0; dy < sc; dy++)
                                        for (int dx = 0; dx < sc; dx++)
                                        {
                                            int o = ((y * sc + dy) * w0 * sc + x * sc + dx) * 3, i3 = (y * w0 + x) * 3;
                                            one[o] = src[i3]; one[o + 1] = src[i3 + 1]; one[o + 2] = src[i3 + 2];
                                        }
                            File.WriteAllBytes(Path.Combine(dir, "tile-" + t.ToString("000") + ".png"), EncodePng(one, w0 * sc, h0 * sc));
                        }

                    // terrain.png — THE model, confirmed by the owner's eyes and the client's own code
                    // (2026-09-26): the texture array is PER-PATCH — one 16x16 texture per ground patch,
                    // patch-major (565: 224 = 16x14; ICC 655: 288 = 24x12 — "more wide than high").
                    // Each patch paints its texture stretched over its ground (nearest, 16 m per texel),
                    // the liquid polygons blue by per-metre depth. The per-cell tile u16s feed the client's
                    // compositor (GenerateTexture in DisplaySystem.dll), not the map's look. Rows are
                    // flipped into the game's orientation; the monitor's decoder un-flips on load.
                    var ground = Ground.Read(rdb, pf);
                    ground.Water.AddRange(Ground.WaterPolygons(rdb.Read(1000001, pf)));
                    int cw = ground.SamplesX - 1, ch = ground.SamplesZ - 1;
                    float Low(int iz, int ix) => ground.Heights[iz * ground.SamplesX + ix] * ground.HeightScale;
                    var levelAt = new float[cw * ch];               // water once per cell (see InLiquid)
                    for (int i = 0; i < cw * ch; i++)
                    {
                        int z = i / cw, x = i % cw;
                        float corner = Math.Min(Math.Min(Low(z, x), Low(z, x + 1)), Math.Min(Low(z + 1, x), Low(z + 1, x + 1)));
                        levelAt[i] = InLiquid(ground, (x + 0.5) * ground.Cell, (z + 0.5) * ground.Cell, out double lvl) && corner < lvl + 0.5 ? (float)lvl : float.NaN;
                    }

                    // the patch grid: pick P so the patch count matches the texture count
                    int P = 64;
                    foreach (int p in new[] { 64, 32, 16 })
                        if ((cw / p) * (ch / p) == n) { P = p; break; }
                    int nx = Math.Max(1, cw / P), nz = Math.Max(1, ch / P);

                    int pw = cw * 4, ph = ch * 4;                   // 1 m per px (4 m cells)
                    var ter = new byte[pw * ph * 3];
                    for (int z = 0; z < ph; z++)
                    {
                        int pz = z / (P * 4);                                           // patch row
                        for (int x = 0; x < pw; x++)
                        {
                            int px_ = x / (P * 4);
                            int t = pz * nx + px_;
                            byte[] src = t < n ? texRgb[t] : null;
                            int tw = t < n ? texW[t] : 16, th = t < n ? texH[t] : 16;  // some zones ship non-16 tiles
                            int tz = (z % (P * 4)) * Math.Max(1, th) / (P * 4), tx = (x % (P * 4)) * Math.Max(1, tw) / (P * 4);
                            int o = ((ph - 1 - z) * pw + x) * 3;   // the row flip
                            int cx = x / 4, cz = z / 4;
                            if (cx < cw && cz < ch)
                            {
                                float lvl = levelAt[cz * cw + cx];
                                if (!float.IsNaN(lvl))
                                {
                                    int ix0 = Math.Min(cx, ground.SamplesX - 2), iz0 = Math.Min(cz, ground.SamplesZ - 2);
                                    double u = (x % 4) / 4.0, v = (z % 4) / 4.0;
                                    float hv = (float)(Low(iz0, ix0) * (1 - u) * (1 - v) + Low(iz0, ix0 + 1) * u * (1 - v) + Low(iz0 + 1, ix0) * (1 - u) * v + Low(iz0 + 1, ix0 + 1) * u * v);
                                    if (hv < lvl + 0.3)
                                    {
                                        double deep = Math.Min(1, (lvl - hv) / 10);
                                        ter[o] = (byte)(48 - 14 * deep);
                                        ter[o + 1] = (byte)(86 - 22 * deep);
                                        ter[o + 2] = (byte)(140 - 40 * deep);
                                        continue;
                                    }
                                }
                            }
                            if (src != null)
                            {
                                int t3 = (Math.Min(th - 1, tz) * tw + Math.Min(tw - 1, tx)) * 3;
                                ter[o] = src[t3]; ter[o + 1] = src[t3 + 1]; ter[o + 2] = src[t3 + 2];
                            }
                            else { ter[o] = 64; ter[o + 1] = 64; ter[o + 2] = 64; }
                        }
                    }
                    File.WriteAllBytes(Path.Combine(dir, "terrain.png"), EncodePng(ter, pw, ph));
                    Console.WriteLine("pf {0}: {1} texture(s) in a {2}x{3} patch grid (P={4}) -> terrain.png {5}x{6} (1 m/px)", pf, n, nx, nz, P, pw, ph);
                }
            return 0;
        }


        /// <summary>
        /// The offset probe (2026-09-26, the owner's "are we just off by an offset?"): renders the same
        /// 512 m square around (wx,wz) for texture-index offsets -8..+8 (byte+off, then the family's
        /// representative, identity when the byte misses), stacked 6 per row into offsets-&lt;pf&gt;.png.
        /// The panel that matches the game names the offset; then tilemap.txt's "offset=N" bakes it in.
        /// </summary>
        public static int OffsetTest(string client, string outDir, int pf, int wx, int wz)
        {
            string db = Path.Combine(client, "cd_image", "data", "db");
            using (var rdb = new Rdb(db))
            {
                byte[] g = rdb.Read(1000009, pf);
                if (g[0] != 'C' || g[1] != 'H' || g[2] != 'G' || g[3] != 'A') { Console.WriteLine("pf {0}: no CHGA ground record", pf); return 1; }
                var rec = new ReflectiveRecord(g, Util.IndexOf(g, "AnarchyGroundDataDB_t") - 21);
                var root = rec.Root;
                var texRgb = new List<byte[]>();
                var texW = new List<int>(); var texH = new List<int>();
                foreach (int ridx in root.Get("texture").Ints)
                {
                    var texR = rec.Ref(ridx);
                    var cr = rec.Ref(texR?.Get("creator")?.Int ?? -1);
                    var bmp = cr?.Get("bitmap");
                    int w = cr?.GetInt("width") ?? 0, h = cr?.GetInt("height") ?? 0;
                    if (bmp == null || w <= 0 || h <= 0 || bmp.Raw.Length < w * h * 2) { texRgb.Add(null); texW.Add(0); texH.Add(0); continue; }
                    texRgb.Add(DecodeRgb565(bmp.Raw, w, h, cr.Get("pixelformat")?.Raw)); texW.Add(w); texH.Add(h);
                }
                int n = texRgb.Count;

                var ground = Ground.Read(rdb, pf);
                ground.Water.AddRange(Ground.WaterPolygons(rdb.Read(1000001, pf)));
                int cw = ground.SamplesX - 1, ch = ground.SamplesZ - 1;
                float Low(int iz, int ix) => ground.Heights[iz * ground.SamplesX + ix] * ground.HeightScale;
                var levelAt = new float[cw * ch];
                for (int i = 0; i < cw * ch; i++)
                {
                    int z = i / cw, x = i % cw;
                    float corner = Math.Min(Math.Min(Low(z, x), Low(z, x + 1)), Math.Min(Low(z + 1, x), Low(z + 1, x + 1)));
                    levelAt[i] = InLiquid(ground, (x + 0.5) * ground.Cell, (z + 0.5) * ground.Cell, out double lvl) && corner < lvl + 0.5 ? (float)lvl : float.NaN;
                }

                const int tex = 4;                                  // 1 m/px, like terrain.png
                const int cells = 128;                              // 512 m per panel
                int cx = Math.Max(0, Math.Min(cw - cells, wx / (int)ground.Cell - cells / 2));
                int cz = Math.Max(0, Math.Min(ch - cells, wz / (int)ground.Cell - cells / 2));
                var offsets = Enumerable.Range(-8, 17).ToArray();
                int cols = 6, rows = (offsets.Length + cols - 1) / cols;
                int pw = cells * tex, ph = cells * tex;
                var strip = new byte[rows * ph * cols * pw * 3];
                int sw = cols * pw;
                Console.WriteLine("offsets row-major, 6 per row:");
                for (int k = 0; k < offsets.Length; k++)
                {
                    int off = offsets[k], ox = (k % cols) * pw, oy = (k / cols) * ph;
                    Console.WriteLine("  panel {0} (row {1}, col {2}): offset {3:+0;-0;+0}", k, k / cols + 1, k % cols + 1, off);
                    for (int iz = cz; iz < cz + cells; iz++)
                        for (int ix = cx; ix < cx + cells; ix++)
                        {
                            int i = iz * cw + ix;
                            int b = ((ground.Tiles[i] & 0xFF) + off) & 0xFF;
                            byte[] src = b < n ? texRgb[b] : null;
                            int tw = b < n ? texW[b] : 0, th = b < n ? texH[b] : 0;
                            float h00 = Low(iz, ix), h10 = Low(iz, ix + 1), h01 = Low(iz + 1, ix), h11 = Low(iz + 1, ix + 1);
                            float lvl = levelAt[i];
                            for (int tz = 0; tz < tex; tz++)
                            {
                                double v = (tz + 0.5) / tex, w00 = 1 - v, w01 = v;
                                for (int tx = 0; tx < tex; tx++)
                                {
                                    double u = (tx + 0.5) / tex;
                                    float hv = (float)(h00 * (1 - u) * w00 + h10 * u * w00 + h01 * (1 - u) * w01 + h11 * u * w01);
                                    int pxo = ((oy + (cells - 1 - (iz - cz)) * tex + tz) * sw + ox + (ix - cx) * tex + tx) * 3;
                                    if (!float.IsNaN(lvl) && hv < lvl + 0.3)
                                    {
                                        double deep = Math.Min(1, (lvl - hv) / 10);
                                        strip[pxo] = (byte)(48 - 14 * deep); strip[pxo + 1] = (byte)(86 - 22 * deep); strip[pxo + 2] = (byte)(140 - 40 * deep);
                                        continue;
                                    }
                                    if (src != null)
                                    {
                                        int t3 = (Math.Min(th - 1, (int)(v * th)) * tw + Math.Min(tw - 1, (int)(u * tw))) * 3;
                                        strip[pxo] = src[t3]; strip[pxo + 1] = src[t3 + 1]; strip[pxo + 2] = src[t3 + 2];
                                    }
                                    else { strip[pxo] = 64; strip[pxo + 1] = 64; strip[pxo + 2] = 64; }
                                }
                            }
                        }
                }
                string outp = Path.Combine(outDir, "offsets-" + pf + ".png");
                File.WriteAllBytes(outp, EncodePng(strip, sw, rows * ph));
                Console.WriteLine("wrote {0} ({1}x{2}, {3} panels of {4} m around world {5},{6})", outp, sw, rows * ph, offsets.Length, cells * ground.Cell, wx, wz);
            }
            return 0;
        }

        /// <summary>
        /// The bit-op probe (2026-09-26, the owner's "some bit operation probably" after the +6 offset
        /// nailed the sand but not the shore): the same 512 m square rendered under 12 candidate decodings
        /// of the tile byte — identity, +6, low-6 bits, low-6 +6, >>2, rotates, nibble swap, bit reverse,
        /// two XORs, and tile_type_data itself — stacked 6 per row into optest-&lt;pf&gt;.png.
        /// </summary>
        public static int OpTest(string client, string outDir, int pf, int wx, int wz)
        {
            string db = Path.Combine(client, "cd_image", "data", "db");
            using (var rdb = new Rdb(db))
            {
                byte[] g = rdb.Read(1000009, pf);
                if (g[0] != 'C' || g[1] != 'H' || g[2] != 'G' || g[3] != 'A') { Console.WriteLine("pf {0}: no CHGA ground record", pf); return 1; }
                var rec = new ReflectiveRecord(g, Util.IndexOf(g, "AnarchyGroundDataDB_t") - 21);
                var root = rec.Root;
                var texRgb = new List<byte[]>();
                var texW = new List<int>(); var texH = new List<int>();
                foreach (int ridx in root.Get("texture").Ints)
                {
                    var texR = rec.Ref(ridx);
                    var cr = rec.Ref(texR?.Get("creator")?.Int ?? -1);
                    var bmp = cr?.Get("bitmap");
                    int w = cr?.GetInt("width") ?? 0, h = cr?.GetInt("height") ?? 0;
                    if (bmp == null || w <= 0 || h <= 0 || bmp.Raw.Length < w * h * 2) { texRgb.Add(null); texW.Add(0); texH.Add(0); continue; }
                    texRgb.Add(DecodeRgb565(bmp.Raw, w, h, cr.Get("pixelformat")?.Raw)); texW.Add(w); texH.Add(h);
                }
                int n = texRgb.Count;
                var ttd = root.Get("tile_type_data")?.Raw;
                var table = new int[256];
                for (int i = 0; i < 256 && ttd != null && 4 + i * 2 + 2 <= ttd.Length; i++) table[i] = BitConverter.ToUInt16(ttd, 4 + i * 2);

                var ground = Ground.Read(rdb, pf);
                ground.Water.AddRange(Ground.WaterPolygons(rdb.Read(1000001, pf)));
                int cw = ground.SamplesX - 1, ch = ground.SamplesZ - 1;
                float Low(int iz, int ix) => ground.Heights[iz * ground.SamplesX + ix] * ground.HeightScale;
                var levelAt = new float[cw * ch];
                for (int i = 0; i < cw * ch; i++)
                {
                    int z = i / cw, x = i % cw;
                    float corner = Math.Min(Math.Min(Low(z, x), Low(z, x + 1)), Math.Min(Low(z + 1, x), Low(z + 1, x + 1)));
                    levelAt[i] = InLiquid(ground, (x + 0.5) * ground.Cell, (z + 0.5) * ground.Cell, out double lvl) && corner < lvl + 0.5 ? (float)lvl : float.NaN;
                }

                int Rev(int b) { int r = 0; for (int k = 0; k < 8; k++) { r = (r << 1) | (b & 1); b >>= 1; } return r; }
                (string name, Func<int, int> op)[] ops =
                {
                    ("identity b",      b => b),
                    ("b + 6",           b => b + 6),
                    ("b & 0x3F",        b => b & 0x3F),
                    ("(b & 0x3F) + 6",  b => (b & 0x3F) + 6),
                    ("b >> 2",          b => b >> 2),
                    ("rol2",            b => ((b << 2) | (b >> 6)) & 0xFF),
                    ("ror2",            b => ((b >> 2) | ((b & 3) << 6)) & 0xFF),
                    ("nibble swap",     b => ((b & 15) << 4) | (b >> 4)),
                    ("bit reverse",     b => Rev(b)),
                    ("b ^ 30",          b => b ^ 30),
                    ("b ^ 0x3F",        b => b ^ 0x3F),
                    ("table[b]",        b => b < 256 ? table[b] : 0),
                    ("wrap(lo,hi)",     b => -1),      // special-cased below: DX8 window into the atlas
                };

                // the DX8 window hypothesis: the cell u16 is an 8.8 UV into a wrapped 256x256 atlas of
                // the tiles (16 per row, rows padded by tiling); each cell renders the 16x16 window at
                // (lo, hi) — v's four observed values pick the band, u breaks the tiling. 2026-09-26.
                int wrapRows = Math.Max(1, (n + 15) / 16);
                var atlas = new byte[256 * 256 * 3];
                for (int r16 = 0; r16 < 16; r16++)
                    for (int c = 0; c < 16; c++)
                    {
                        int t = (r16 % wrapRows) * 16 + c;
                        if (t >= n || texRgb[t] == null) continue;
                        for (int y = 0; y < 16; y++)
                            for (int x = 0; x < 16; x++)
                            {
                                int o = ((r16 * 16 + y) * 256 + c * 16 + x) * 3, i3 = (y * 16 + x) * 3;
                                atlas[o] = texRgb[t][i3]; atlas[o + 1] = texRgb[t][i3 + 1]; atlas[o + 2] = texRgb[t][i3 + 2];
                            }
                    }

                const int tex = 4, cells = 128;                        // 1 m/px, 512 m per panel
                int cx = Math.Max(0, Math.Min(cw - cells, wx / (int)ground.Cell - cells / 2));
                int cz = Math.Max(0, Math.Min(ch - cells, wz / (int)ground.Cell - cells / 2));
                int cols = 6, rows = (ops.Length + cols - 1) / cols;
                int pw = cells * tex, ph = cells * tex, sw = cols * pw;
                var strip = new byte[rows * ph * sw * 3];
                for (int k = 0; k < ops.Length; k++)
                {
                    int ox = (k % cols) * pw, oy = (k / cols) * ph;
                    Console.WriteLine("  panel {0} (row {1}, col {2}): {3}", k, k / cols + 1, k % cols + 1, ops[k].name);
                    for (int iz = cz; iz < cz + cells; iz++)
                        for (int ix = cx; ix < cx + cells; ix++)
                        {
                            int i = iz * cw + ix;
                            ushort cell16 = ground.Tiles[i];
                            int b = ops[k].op(cell16 & 0xFF) & 0xFF;
                            bool wrap = ops[k].name == "wrap(lo,hi)";
                            byte[] src = !wrap && b < n ? texRgb[b] : null;
                            int tw = !wrap && b < n ? texW[b] : 0, th = !wrap && b < n ? texH[b] : 0;
                            float h00 = Low(iz, ix), h10 = Low(iz, ix + 1), h01 = Low(iz + 1, ix), h11 = Low(iz + 1, ix + 1);
                            float lvl = levelAt[i];
                            for (int tz = 0; tz < tex; tz++)
                            {
                                double v = (tz + 0.5) / tex, w00 = 1 - v, w01 = v;
                                for (int tx = 0; tx < tex; tx++)
                                {
                                    double u = (tx + 0.5) / tex;
                                    float hv = (float)(h00 * (1 - u) * w00 + h10 * u * w00 + h01 * (1 - u) * w01 + h11 * u * w01);
                                    int pxo = ((oy + (cells - 1 - (iz - cz)) * tex + tz) * sw + ox + (ix - cx) * tex + tx) * 3;
                                    if (!float.IsNaN(lvl) && hv < lvl + 0.3)
                                    {
                                        double deep = Math.Min(1, (lvl - hv) / 10);
                                        strip[pxo] = (byte)(48 - 14 * deep); strip[pxo + 1] = (byte)(86 - 22 * deep); strip[pxo + 2] = (byte)(140 - 40 * deep);
                                        continue;
                                    }
                                    if (wrap)
                                    {
                                        int ax = ((cell16 & 0xFF) + (int)(u * 16)) & 255;
                                        int ay = (((cell16 >> 8) & 0xFF) + (int)(v * 16)) & 255;
                                        int ar = ay >> 4; if (ar >= wrapRows) ar -= wrapRows;
                                        int tile = ar * 16 + (ax >> 4);
                                        byte[] wsrc = tile < n ? texRgb[tile] : null;
                                        if (wsrc != null)
                                        {
                                            int t3 = ((ay & 15) * 16 + (ax & 15)) * 3;
                                            strip[pxo] = wsrc[t3]; strip[pxo + 1] = wsrc[t3 + 1]; strip[pxo + 2] = wsrc[t3 + 2];
                                        }
                                        else { strip[pxo] = 64; strip[pxo + 1] = 64; strip[pxo + 2] = 64; }
                                    }
                                    else if (src != null)
                                    {
                                        int t3 = (Math.Min(th - 1, (int)(v * th)) * tw + Math.Min(tw - 1, (int)(u * tw))) * 3;
                                        strip[pxo] = src[t3]; strip[pxo + 1] = src[t3 + 1]; strip[pxo + 2] = src[t3 + 2];
                                    }
                                    else { strip[pxo] = 64; strip[pxo + 1] = 64; strip[pxo + 2] = 64; }
                                }
                            }
                        }
                }
                string outp = Path.Combine(outDir, "optest-" + pf + ".png");
                File.WriteAllBytes(outp, EncodePng(strip, sw, rows * ph));
                Console.WriteLine("wrote {0} ({1}x{2}, {3} panels of {4} m around world {5},{6})", outp, sw, rows * ph, ops.Length, cells * ground.Cell, wx, wz);
            }
            return 0;
        }

        /// <summary>
        /// The sliding-window map (2026-09-26, the owner's ICC stride insight): the texture array is
        /// PATCH-SHAPED — one 16x16 texture per 256 m patch, laid out like the map (565: 16x14, ICC 655:
        /// 24x12 — "more wide than high"). Each cell's u16 is then an anchor into that atlas: the ground
        /// is one big texture scrolled by world position, re-anchored per cell — so neighbouring cells with
        /// different anchors still sample similar atlas regions and no speckle survives. Renders the full
        /// map under that model for a given atlas column count (16 = the accidental sheet layout, 24 =
        /// ICC's patch width) into mapwin-&lt;cols&gt;-&lt;pf&gt;.png.
        /// </summary>
        public static int MapWin(string client, string outDir, int pf, int cols)
        {
            string db = Path.Combine(client, "cd_image", "data", "db");
            using (var rdb = new Rdb(db))
            {
                byte[] g = rdb.Read(1000009, pf);
                if (g[0] != 'C' || g[1] != 'H' || g[2] != 'G' || g[3] != 'A') { Console.WriteLine("pf {0}: no CHGA ground record", pf); return 1; }
                var rec = new ReflectiveRecord(g, Util.IndexOf(g, "AnarchyGroundDataDB_t") - 21);
                var root = rec.Root;
                var texRgb = new List<byte[]>();
                foreach (int ridx in root.Get("texture").Ints)
                {
                    var texR = rec.Ref(ridx);
                    var cr = rec.Ref(texR?.Get("creator")?.Int ?? -1);
                    var bmp = cr?.Get("bitmap");
                    int w = cr?.GetInt("width") ?? 0, h = cr?.GetInt("height") ?? 0;
                    texRgb.Add(bmp != null && w == 16 && h == 16 && bmp.Raw.Length >= w * h * 2 ? DecodeRgb565(bmp.Raw, w, h, cr.Get("pixelformat")?.Raw) : null);
                }
                int n = texRgb.Count;
                int arows = (n + cols - 1) / cols;

                // the atlas: cols wide, texture t at (t % cols, t / cols), wrapped by tiling rows
                var atlas = new byte[256 * 256 * 3];
                for (int r16 = 0; r16 < 16; r16++)
                    for (int c = 0; c < 16; c++)
                    {
                        if (c >= cols) continue;
                        int t = (r16 % arows) * cols + c;
                        if (t >= n || texRgb[t] == null) continue;
                        for (int y = 0; y < 16; y++)
                            for (int x = 0; x < 16; x++)
                            {
                                int o = ((r16 * 16 + y) * 256 + c * 16 + x) * 3, i3 = (y * 16 + x) * 3;
                                atlas[o] = texRgb[t][i3]; atlas[o + 1] = texRgb[t][i3 + 1]; atlas[o + 2] = texRgb[t][i3 + 2];
                            }
                    }

                var ground = Ground.Read(rdb, pf);
                ground.Water.AddRange(Ground.WaterPolygons(rdb.Read(1000001, pf)));
                int cw = ground.SamplesX - 1, ch = ground.SamplesZ - 1;
                float Low(int iz, int ix) => ground.Heights[iz * ground.SamplesX + ix] * ground.HeightScale;
                var levelAt = new float[cw * ch];
                for (int i = 0; i < cw * ch; i++)
                {
                    int z = i / cw, x = i % cw;
                    float corner = Math.Min(Math.Min(Low(z, x), Low(z, x + 1)), Math.Min(Low(z + 1, x), Low(z + 1, x + 1)));
                    levelAt[i] = InLiquid(ground, (x + 0.5) * ground.Cell, (z + 0.5) * ground.Cell, out double lvl) && corner < lvl + 0.5 ? (float)lvl : float.NaN;
                }

                const int tex = 2;                                  // 2 m/px is plenty for a whole-map look
                int pw = cw * tex, ph = ch * tex;
                var ter = new byte[pw * ph * 3];
                for (int iz = 0; iz < ch; iz++)
                    for (int ix = 0; ix < cw; ix++)
                    {
                        int i = iz * cw + ix;
                        ushort cell16 = ground.Tiles[i];
                        float h00 = Low(iz, ix), h10 = Low(iz, ix + 1), h01 = Low(iz + 1, ix), h11 = Low(iz + 1, ix + 1);
                        float lvl = levelAt[i];
                        int y0 = (ch - 1 - iz) * tex;
                        for (int tz = 0; tz < tex; tz++)
                        {
                            double v = (tz + 0.5) / tex, w00 = 1 - v, w01 = v;
                            int o0 = ((y0 + tz) * pw + ix * tex) * 3;
                            for (int tx = 0; tx < tex; tx++)
                            {
                                double u = (tx + 0.5) / tex;
                                float hv = (float)(h00 * (1 - u) * w00 + h10 * u * w00 + h01 * (1 - u) * w01 + h11 * u * w01);
                                int o = o0 + tx * 3;
                                if (!float.IsNaN(lvl) && hv < lvl + 0.3)
                                {
                                    double deep = Math.Min(1, (lvl - hv) / 10);
                                    ter[o] = (byte)(48 - 14 * deep); ter[o + 1] = (byte)(86 - 22 * deep); ter[o + 2] = (byte)(140 - 40 * deep);
                                    continue;
                                }
                                // the anchor scrolls with world position: 4 cells per atlas texel
                                int ax = ((cell16 & 0xFF) + (int)((ix + u) / 4)) & 255;
                                int ay = (((cell16 >> 8) & 0xFF) + (int)((iz + v) / 4)) & 255;
                                int ac = ax >> 4, ar = ay >> 4;
                                int t = (ar % arows) * cols + ac;
                                if (ac < cols && t < n && texRgb[t] != null)
                                {
                                    int t3 = ((ay & 15) * 16 + (ax & 15)) * 3;
                                    ter[o] = texRgb[t][t3]; ter[o + 1] = texRgb[t][t3 + 1]; ter[o + 2] = texRgb[t][t3 + 2];
                                }
                                else { ter[o] = 64; ter[o + 1] = 64; ter[o + 2] = 64; }
                            }
                        }
                    }
                string outp = Path.Combine(outDir, string.Format("mapwin-{0}col-{1}.png", cols, pf));
                File.WriteAllBytes(outp, EncodePng(ter, pw, ph));
                Console.WriteLine("pf {0}: {1} texture(s) in a {2}x{3} atlas -> {4} ({5}x{6})", pf, n, cols, arows, outp, pw, ph);
            }
            return 0;
        }

        /// <summary>Median r,g,b of a decoded RGB triple array.</summary>
        private static byte[] MedianOf(byte[] rgb)
        {
            int n = rgb.Length / 3;
            var rs = new int[n]; var gs = new int[n]; var bs = new int[n];
            for (int i = 0; i < n; i++) { rs[i] = rgb[i * 3]; gs[i] = rgb[i * 3 + 1]; bs[i] = rgb[i * 3 + 2]; }
            Array.Sort(rs); Array.Sort(gs); Array.Sort(bs);
            return new[] { (byte)rs[n / 2], (byte)gs[n / 2], (byte)bs[n / 2] };
        }

        /// <summary>Point-in-polygon over the ground's liquid rings ([level, x,z, x,z, ...]).</summary>
        private static bool InLiquid(Ground ground, double x, double z, out double level)
        {
            foreach (double[] w in ground.Water)
            {
                int n = (w.Length - 1) / 2;
                bool inside = false;
                for (int a = 0, b = n - 1; a < n; b = a++)
                {
                    double ax = w[1 + 2 * a], az = w[2 + 2 * a], bx = w[1 + 2 * b], bz = w[2 + 2 * b];
                    if ((az > z) != (bz > z) && x < (bx - ax) * (z - az) / (bz - az) + ax) inside = !inside;
                }
                if (inside) { level = w[0]; return true; }
            }
            level = 0;
            return false;
        }

        /// <summary>A masked bitmap (RGB565 by default) as straight w*h*3 RGB, ranges stretched to 255.</summary>
        private static byte[] DecodeRgb565(byte[] raw, int w, int h, byte[] pixelformat)
        {
            uint rm = 0xF800, gm = 0x07E0, bm = 0x001F;
            if (pixelformat != null && pixelformat.Length >= 28)
            {
                uint r2 = BitConverter.ToUInt32(pixelformat, 16), g2 = BitConverter.ToUInt32(pixelformat, 20), b2 = BitConverter.ToUInt32(pixelformat, 24);
                if (r2 != 0 && g2 != 0 && b2 != 0) { rm = r2; gm = g2; bm = b2; }
            }
            var rgb = new byte[w * h * 3];
            for (int i = 0; i < w * h; i++)
            {
                uint v = BitConverter.ToUInt16(raw, i * 2);
                rgb[i * 3] = (byte)(Unmask(v, rm) * 255 / MaxOf(rm));
                rgb[i * 3 + 1] = (byte)(Unmask(v, gm) * 255 / MaxOf(gm));
                rgb[i * 3 + 2] = (byte)(Unmask(v, bm) * 255 / MaxOf(bm));
            }
            return rgb;
        }

        private static int Unmask(uint v, uint mask) => (int)((v & mask) >> BitPos(mask));
        private static int BitPos(uint mask) { int p = 0; while ((mask & 1u) == 0) { mask >>= 1; p++; } return p; }
        private static int MaxOf(uint mask) { int bits = 0; while (mask != 0) { bits += (int)(mask & 1u); mask >>= 1; } return (1 << bits) - 1; }

        // smallest PNG writer that could work (8-bit RGB, filter 0, zlib via the BCL) — navmap's, verbatim
        private static byte[] EncodePng(byte[] rgb, int w, int h)
        {
            var raw = new byte[h * (w * 3 + 1)];
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
            ihdr[0] = (byte)(w >> 24); ihdr[1] = (byte)(w >> 16); ihdr[2] = (byte)(w >> 8); ihdr[3] = (byte)w;   // PNG is big-endian
            ihdr[4] = (byte)(h >> 24); ihdr[5] = (byte)(h >> 16); ihdr[6] = (byte)(h >> 8); ihdr[7] = (byte)h;
            ihdr[8] = 8; ihdr[9] = 2;   // 8-bit, truecolour
            Chunk(outp, "IHDR", ihdr);
            Chunk(outp, "IDAT", z);
            Chunk(outp, "IEND", new byte[0]);
            return outp.ToArray();
        }

        private static void Chunk(Stream s, string type, byte[] data)
        {
            s.WriteByte((byte)(data.Length >> 24)); s.WriteByte((byte)(data.Length >> 16));   // big-endian length
            s.WriteByte((byte)(data.Length >> 8)); s.WriteByte((byte)data.Length);
            byte[] t = System.Text.Encoding.ASCII.GetBytes(type);
            s.Write(t, 0, 4);
            s.Write(data, 0, data.Length);
            uint crc = Crc32(t, 0, 4);
            // the running value is stored finalized; continuing a chunk CRC past the type means
            // un-finalizing it before feeding the data through. Written big-endian, like the length.
            crc = Crc32(data, 0, data.Length, crc ^ 0xFFFFFFFF);
            s.WriteByte((byte)(crc >> 24)); s.WriteByte((byte)(crc >> 16)); s.WriteByte((byte)(crc >> 8)); s.WriteByte((byte)crc);
        }

        private static uint[] _crcTable;
        private static uint Crc32(byte[] b, int at, int len, uint seed = 0xFFFFFFFF)
        {
            if (_crcTable == null)
            {
                _crcTable = new uint[256];
                for (int n = 0; n < 256; n++)
                {
                    uint c = (uint)n;
                    for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                    _crcTable[n] = c;
                }
            }
            uint crc = seed;
            for (int i = at; i < at + len; i++) crc = _crcTable[(crc ^ b[i]) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFF;
        }
    }
}
