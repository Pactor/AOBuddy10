using System;
using System.Collections.Generic;
using System.IO;

namespace AONavExtractor
{
    /// <summary>Just enough PNG to read the dungeon tilemap layers: 8-bit greyscale or RGB, filters 0-4, no interlace.</summary>
    public sealed class Png
    {
        public int Width, Height, Channels;
        public byte[] Pixels;   // row-major, Channels bytes per pixel
        public int At(int x, int y, int channel = 0) => Pixels[(y * Width + x) * Channels + channel];

        public static Png Parse(byte[] data, int start, out int end)
        {
            int q = start + 8;
            var idat = new MemoryStream();
            int w = 0, h = 0, depth = 0, ctype = 0;
            end = data.Length;
            while (q + 8 <= data.Length)
            {
                int len = (data[q] << 24) | (data[q + 1] << 16) | (data[q + 2] << 8) | data[q + 3];
                string tag = System.Text.Encoding.ASCII.GetString(data, q + 4, 4);
                int body = q + 8;
                if (tag == "IHDR")
                {
                    w = (data[body] << 24) | (data[body + 1] << 16) | (data[body + 2] << 8) | data[body + 3];
                    h = (data[body + 4] << 24) | (data[body + 5] << 16) | (data[body + 6] << 8) | data[body + 7];
                    depth = data[body + 8]; ctype = data[body + 9];
                }
                else if (tag == "IDAT") idat.Write(data, body, len);
                q = body + len + 4;
                if (tag == "IEND") { end = q; break; }
            }
            if (depth != 8) throw new InvalidDataException("PNG depth " + depth + " unsupported");
            int ch = ctype == 0 ? 1 : ctype == 2 ? 3 : ctype == 4 ? 2 : ctype == 6 ? 4 : 0;
            if (ch == 0) throw new InvalidDataException("PNG colour type " + ctype + " unsupported");
            byte[] raw = Util.ZlibDecompress(idat.ToArray());
            int stride = w * ch;
            var px = new byte[stride * h];
            var prev = new byte[stride];
            int p = 0;
            for (int y = 0; y < h; y++)
            {
                int f = raw[p++];
                var row = new byte[stride];
                Buffer.BlockCopy(raw, p, row, 0, stride); p += stride;
                for (int x = 0; x < stride; x++)
                {
                    int a = x >= ch ? row[x - ch] : 0, b = prev[x], c = x >= ch ? prev[x - ch] : 0;
                    switch (f)
                    {
                        case 1: row[x] = (byte)(row[x] + a); break;
                        case 2: row[x] = (byte)(row[x] + b); break;
                        case 3: row[x] = (byte)(row[x] + ((a + b) >> 1)); break;
                        case 4:
                            int pa = Math.Abs(b - c), pb = Math.Abs(a - c), pc = Math.Abs(a + b - 2 * c);
                            int pr = (pa <= pb && pa <= pc) ? a : (pb <= pc ? b : c);
                            row[x] = (byte)(row[x] + pr); break;
                    }
                }
                Buffer.BlockCopy(row, 0, px, y * stride, stride);
                prev = row;
            }
            return new Png { Width = w, Height = h, Channels = ch, Pixels = px };
        }

        /// <summary>Every PNG embedded in a blob, in order.</summary>
        public static List<Png> FindAll(byte[] data)
        {
            var sig = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var outl = new List<Png>();
            int p = 0;
            while (true)
            {
                int i = -1;
                for (int k = p; k + 8 <= data.Length; k++)
                {
                    int m = 0; while (m < 8 && data[k + m] == sig[m]) m++;
                    if (m == 8) { i = k; break; }
                }
                if (i < 0) break;
                outl.Add(Parse(data, i, out int end));
                p = end;
            }
            return outl;
        }
    }
}
