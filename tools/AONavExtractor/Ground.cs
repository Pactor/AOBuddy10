using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace AONavExtractor
{
    /// <summary>
    /// Outdoor heightfield: the 'CHGA' variant of RDB type 1000009. The record wraps a
    /// reflective AnarchyGroundDataDB_t object whose heightmap_compressed_data field holds one
    /// length-prefixed zlib blob per patch. A patch inflates to (P+1)^2 bytes (or twice that for
    /// 16-bit zones); the client's AnarchyGroundData_t::Patch_t::DecompressHeightMap then takes a
    /// cumulative sum down each column and along each row modulo 256 (65536), i.e. the block is
    /// an integral image, and the byte is the height in hscale units. Patches are laid out
    /// row-major with x fastest, overlapping by one sample.
    /// </summary>
    public sealed class Ground
    {
        public int SamplesX, SamplesZ, SourceBits;
        public float Cell, HeightScale;          // HeightScale is per u16 unit (already /256 for 8-bit zones)
        public ushort[] Heights;                 // [z * SamplesX + x]
        public ushort[] Tiles;                   // [(SamplesZ-1) * (SamplesX-1)]
        public byte[] Building;                  // same shape, nibble per cell (order OPEN)
        public float[] WaterY = Array.Empty<float>();   // the plane table's LEVELS (see WaterPlanes). Superseded by
                                                        // Water (the client's own liquid polygons) — kept for information
        public readonly List<double[]> Water = new List<double[]>();   // per liquid: [level, x1,z1, x2,z2, ...] — the
                                                        // playfield record's own water polygons (see WaterPolygons)

        /// <summary>
        /// The playfield record's water-level table: four 12-byte entries ([f32 levelY][f32][f32]),
        /// a fixed 48-byte block ending 50 bytes before the arrival table (which is itself found by
        /// walking back from the record end until the int there equals the entries passed). Found
        /// 2026-09-24 hunting why the bot could not cross Newland's lake: Newland (567) and Newland
        /// City (566) both say 32.1 — matching the lake surface exactly (the capture swam 32.09).
        /// 2026-09-25 correction, after the bot swam 7 m over Newland City's dry pit: these are
        /// LEVELS, not regions — applying one playfield-wide puts water over any dry ground below
        /// it (ICC's four entries, 10.5-15.4, sit UNDER its 17.1 tile-12 basin and are likely
        /// arrivals). Where water actually is: the tilemap's band of tile type 12 painted over the
        /// shore, flooded through ground under the level — that is the bot's NavGround.SwimY. This
        /// table is kept as the level source only. Distinct values only.
        /// </summary>
        public static float[] WaterPlanes(byte[] playfieldBlob)
        {
            // Walk back over the arrival table: count 28-byte entries until the int names them.
            // That int sits AT the table's head, so the body (everything before it) ends at pos.
            int pos = playfieldBlob.Length - 4, k = 0;
            while (pos >= 4 && BitConverter.ToInt32(playfieldBlob, pos) != k) { k++; pos -= 28; }
            int bodyEnd = pos;
            var planes = new List<float>();
            for (int e = 0; e < 4; e++)
            {
                int at = bodyEnd - 98 + e * 12;
                if (at < 0 || at + 4 > bodyEnd - 50) break;
                float y = BitConverter.ToSingle(playfieldBlob, at);
                if (float.IsNaN(y) || Math.Abs(y) > 2000f) continue;
                if (!planes.Any(p => Math.Abs(p - y) < 0.05f)) planes.Add(y);   // the table's copies differ in low bits
            }
            return planes.ToArray();
        }

        /// <summary>
        /// The playfield record's LIQUID POLYGONS — where water actually is, found 2026-09-25 after the
        /// owner called it ("the game is from 2000, they wouldn't flood-fill; water has to be a quad or
        /// a volume") and baited the hunt with a lake he stood in: level 18.3 at (2349,1127) in Newland
        /// Desert, a level no stored plane has. Each liquid is a flat ring of 3-12 (x,z) points at one
        /// level, serialized in 12-byte rows just before the arrival table:
        ///
        ///   [count][x1][level] [z1][x2][level] ... [z(n-1)][xn][level] [zn][2][flags]
        ///
        /// i.e. a flat ring whose consecutive points are (row.slot1, nextRow.slot0), bracketed by rows
        /// of small flag ints. Walked backwards from the last ring's closer row; the count in the opener
        /// row names the ring's size, which is what makes the walk unambiguous.
        ///
        /// Verified against ground truth on both installs: 565's quad at 18.3 spans (2267..2416,
        /// 1052..1180) — the owner's lake exactly; 567 Newland's one quad at 32.1 is the captured swim
        /// (32.09); 656 Coast of Tranquility's ocean is ONE quad spanning its whole map (its fan map is
        /// 51% water); 696 Wailing Wastes is a whole-map water table at 5.0; 655 ICC's canal at 15.41;
        /// 695 Lush Fields' "river" is one small 9.4 quad. The acid river/pools of 565 are in here too
        /// (the 11.9/12.4 rings in the south-east) — the record lists liquids, not just water; a ring's
        /// level can slope slightly along a river (±0.35 m), and the max is kept.
        /// </summary>
        public static List<double[]> WaterPolygons(byte[] playfieldBlob)
        {
            var outl = new List<double[]>();
            int pos = playfieldBlob.Length - 4, k = 0;
            while (pos >= 4 && BitConverter.ToInt32(playfieldBlob, pos) != k) { k++; pos -= 28; }
            int e = pos - 58;                                   // the last ring's closer row
            while (e >= 24)
            {
                float closerZ = BitConverter.ToSingle(playfieldBlob, e);
                if (BitConverter.ToInt32(playfieldBlob, e + 4) != 2
                    || Math.Abs(BitConverter.ToInt32(playfieldBlob, e + 8)) > 1 << 21
                    || Math.Abs(closerZ) >= 9000f) break;
                int op = -1, C = 0;
                for (int c = 3; c <= 12; c++)
                {
                    int at = e - c * 12;
                    if (at < 12 || BitConverter.ToInt32(playfieldBlob, at) != c) continue;
                    float ox = BitConverter.ToSingle(playfieldBlob, at + 4), lv = BitConverter.ToSingle(playfieldBlob, at + 8);
                    if (!(lv > 0.2f && lv < 300f) || Math.Abs(ox) >= 9000f) continue;
                    bool ok = true;
                    for (int r = 1; r < c && ok; r++)
                        if (Math.Abs(BitConverter.ToSingle(playfieldBlob, at + r * 12)) >= 9000f
                            || Math.Abs(BitConverter.ToSingle(playfieldBlob, at + r * 12 + 4)) >= 9000f) ok = false;
                    if (ok) { op = at; C = c; break; }
                }
                if (op < 0) break;
                float level = 0;
                for (int r = 0; r < C; r++) level = Math.Max(level, BitConverter.ToSingle(playfieldBlob, op + r * 12 + 8));
                var ring = new double[1 + 2 * C];
                ring[0] = level;
                ring[1] = BitConverter.ToSingle(playfieldBlob, op + 4);
                ring[2] = BitConverter.ToSingle(playfieldBlob, op + 12);
                for (int r = 1; r < C - 1; r++)
                {
                    ring[1 + 2 * r] = BitConverter.ToSingle(playfieldBlob, op + r * 12 + 4);
                    ring[2 + 2 * r] = BitConverter.ToSingle(playfieldBlob, op + (r + 1) * 12);
                }
                ring[1 + 2 * (C - 1)] = BitConverter.ToSingle(playfieldBlob, op + (C - 1) * 12 + 4);
                ring[2 + 2 * (C - 1)] = closerZ;
                outl.Add(ring);
                int prev = op - 12;                              // a flag-ints row brackets each liquid
                if (prev >= 12 && Math.Abs(BitConverter.ToInt32(playfieldBlob, prev)) < 1 << 21
                    && Math.Abs(BitConverter.ToInt32(playfieldBlob, prev + 4)) < 1 << 21
                    && Math.Abs(BitConverter.ToInt32(playfieldBlob, prev + 8)) < 1 << 21) prev -= 12;
                e = prev;
            }
            outl.Reverse();
            return outl;
        }

        public static Ground Read(Rdb rdb, int gid)
        {
            byte[] b = rdb.Read(1000009, gid);
            if (b.Length < 24 || b[0] != 'C' || b[1] != 'H' || b[2] != 'G' || b[3] != 'A') return null;
            int i = Util.IndexOf(b, "AnarchyGroundDataDB_t");
            if (i < 21) throw new InvalidDataException("ground " + gid + ": no AnarchyGroundDataDB_t");
            var rec = new ReflectiveRecord(b, i - 21);
            var g = rec.Nodes[0];
            var gr = new Ground
            {
                SamplesX = g.GetInt("map_width"), SamplesZ = g.GetInt("map_height"),
                Cell = BitConverter.ToSingle(b, 16), HeightScale = BitConverter.ToSingle(b, 20)
            };
            List<byte[]> hm = Inflate(Blobs(g.Get("heightmap_compressed_data")));
            List<byte[]> tm = Inflate(Blobs(g.Get("tilemap_compressed_data")));
            List<byte[]> bm = Inflate(Blobs(g.Get("buildingmap_compressed_data")));
            if (hm.Count == 0) throw new InvalidDataException("ground " + gid + ": no heightmap patches");
            int n0 = hm[0].Length, P = 0, bits = 0;
            foreach (int p in new[] { 8, 16, 32, 64, 128, 256 })
                foreach (int bt in new[] { 8, 16 })
                    if (P == 0 && (p + 1) * (p + 1) * (bt / 8) == n0) { P = p; bits = bt; }
            if (P == 0) throw new InvalidDataException("ground " + gid + ": patch of " + n0 + " bytes");
            int w = gr.SamplesX, h = gr.SamplesZ, nx = (w - 1) / P, nz = (h - 1) / P;
            if (hm.Count != nx * nz) throw new InvalidDataException(string.Format("ground {0}: {1} patches for a {2}x{3} grid (P={4})", gid, hm.Count, nx, nz, P));
            gr.SourceBits = bits;
            gr.Heights = new ushort[w * h];
            gr.Tiles = new ushort[(w - 1) * (h - 1)];
            gr.Building = new byte[(w - 1) * (h - 1)];
            int S = P + 1;
            var acc = new long[S * S];
            for (int bz = 0; bz < nz; bz++)
                for (int bx = 0; bx < nx; bx++)
                {
                    int k = bz * nx + bx;
                    byte[] blk = hm[k];
                    // integral image: sum down columns then along rows
                    for (int r = 0; r < S; r++)
                        for (int c = 0; c < S; c++)
                        {
                            long v = bits == 8 ? blk[r * S + c] : BitConverter.ToUInt16(blk, (r * S + c) * 2);
                            if (r > 0) v += acc[(r - 1) * S + c];
                            acc[r * S + c] = v;
                        }
                    for (int r = 0; r < S; r++)
                        for (int c = 1; c < S; c++)
                            acc[r * S + c] += acc[r * S + c - 1];
                    for (int r = 0; r < S; r++)
                        for (int c = 0; c < S; c++)
                        {
                            ushort v = bits == 8 ? (ushort)((acc[r * S + c] % 256) * 256) : (ushort)(acc[r * S + c] % 65536);
                            gr.Heights[(bz * P + r) * w + bx * P + c] = v;
                        }
                    if (k < tm.Count && tm[k].Length == P * P * 2)
                        for (int r = 0; r < P; r++)
                            for (int c = 0; c < P; c++)
                                gr.Tiles[(bz * P + r) * (w - 1) + bx * P + c] = BitConverter.ToUInt16(tm[k], (r * P + c) * 2);
                    if (k < bm.Count && bm[k].Length == P * P / 2)
                        for (int r = 0; r < P; r++)
                            for (int c = 0; c < P; c++)
                            {
                                int idx = r * P + c; byte nib = bm[k][idx / 2];
                                gr.Building[(bz * P + r) * (w - 1) + bx * P + c] = (byte)((idx & 1) == 0 ? (nib & 15) : (nib >> 4));
                            }
                }
            if (bits == 8) gr.HeightScale /= 256f;
            return gr;
        }

        static List<byte[]> Blobs(ReflectiveRecord.Entry e)
        {
            var outl = new List<byte[]>();
            if (e == null) return outl;
            byte[] raw = e.Raw; int p = 0;
            while (p + 4 <= raw.Length)
            {
                int n = BitConverter.ToInt32(raw, p); p += 4;
                var b = new byte[n]; Buffer.BlockCopy(raw, p, b, 0, n); p += n;
                outl.Add(b);
            }
            return outl;
        }

        static List<byte[]> Inflate(List<byte[]> zs)
        {
            var outl = new List<byte[]>();
            foreach (var z in zs) outl.Add(Util.ZlibDecompress(z));
            return outl;
        }

        public double HeightAt(double x, double z)
        {
            double fx = x / Cell, fz = z / Cell;
            int ix = (int)Math.Floor(fx), iz = (int)Math.Floor(fz);
            if (ix < 0 || iz < 0 || ix + 1 >= SamplesX || iz + 1 >= SamplesZ) return double.NaN;
            double tx = fx - ix, tz = fz - iz;
            double v = Heights[iz * SamplesX + ix] * (1 - tx) * (1 - tz) + Heights[iz * SamplesX + ix + 1] * tx * (1 - tz)
                     + Heights[(iz + 1) * SamplesX + ix] * (1 - tx) * tz + Heights[(iz + 1) * SamplesX + ix + 1] * tx * tz;
            return v * HeightScale;
        }

        public void Write(string path)
        {
            var body = new MemoryStream();
            var bw = new BinaryWriter(body);
            foreach (ushort v in Heights) bw.Write(v);
            foreach (ushort v in Tiles) bw.Write(v);
            bw.Write(Building);
            byte[] raw = body.ToArray();
            byte[] z = Util.ZlibCompress(raw);
            using (var f = new BinaryWriter(File.Create(path)))
            {
                f.Write(new byte[] { (byte)'A', (byte)'O', (byte)'N', (byte)'G' });
                // v3 = v2 + the water-plane list (WaterY) after SourceBits. v4 = v3 + the client's own
                // liquid polygons (Water): per liquid the point count, the level, then (x, z) per point.
                f.Write(4); f.Write(SamplesX); f.Write(SamplesZ); f.Write(Cell); f.Write(HeightScale); f.Write(SourceBits);
                f.Write(WaterY.Length); foreach (float y in WaterY) f.Write(y);
                f.Write(Water.Count);
                foreach (double[] w in Water)
                {
                    f.Write((w.Length - 1) / 2);
                    f.Write((float)w[0]);
                    for (int i = 1; i < w.Length; i++) f.Write((float)w[i]);
                }
                f.Write(raw.Length); f.Write(z.Length); f.Write(z);
            }
        }

        public double MinHeight { get { ushort m = ushort.MaxValue; foreach (var v in Heights) if (v < m) m = v; return m * HeightScale; } }
        public double MaxHeight { get { ushort m = 0; foreach (var v in Heights) if (v > m) m = v; return m * HeightScale; } }
    }

    public static class Util
    {
        /// <summary>NUL-terminated Latin-1 string in a fixed field.</summary>
        public static string CStr(byte[] b, int at, int max)
        {
            int n = 0;
            while (n < max && at + n < b.Length && b[at + n] != 0) n++;
            return System.Text.Encoding.Latin1.GetString(b, at, n);
        }

        public static int IndexOf(byte[] hay, string needle)
        {
            byte[] n = System.Text.Encoding.ASCII.GetBytes(needle);
            for (int i = 0; i + n.Length <= hay.Length; i++)
            {
                int k = 0;
                while (k < n.Length && hay[i + k] == n[k]) k++;
                if (k == n.Length) return i;
            }
            return -1;
        }

        public static byte[] ZlibDecompress(byte[] z)
        {
            using (var ms = new MemoryStream(z))
            using (var zs = new ZLibStream(ms, CompressionMode.Decompress))
            using (var o = new MemoryStream()) { zs.CopyTo(o); return o.ToArray(); }
        }

        public static byte[] ZlibCompress(byte[] raw)
        {
            using (var o = new MemoryStream())
            {
                using (var zs = new ZLibStream(o, CompressionLevel.SmallestSize, true)) zs.Write(raw, 0, raw.Length);
                return o.ToArray();
            }
        }
    }
}
