using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

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
                f.Write(2); f.Write(SamplesX); f.Write(SamplesZ); f.Write(Cell); f.Write(HeightScale); f.Write(SourceBits);
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
