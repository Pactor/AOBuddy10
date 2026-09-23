using System;
using System.Collections.Generic;
using System.IO;

namespace AONavExtractor
{
    /// <summary>
    /// Read-only reader for the classic AO client's ResourceDatabase (cd_image/data/db).
    /// ResourceDatabase.idx is a chain of blocks of 16-byte entries (offset hi, offset lo,
    /// type, instance); offsets little-endian, ids big-endian. The .dat is one logical stream
    /// split into parts (.dat, .dat.001, ...), each part starting with a header of partHeader bytes.
    /// Files are opened with full sharing so a running game client is not disturbed.
    /// </summary>
    public sealed class Rdb : IDisposable
    {
        readonly List<FileStream> _parts = new List<FileStream>();
        readonly Dictionary<int, SortedDictionary<int, long>> _offsets = new Dictionary<int, SortedDictionary<int, long>>();
        readonly uint _partSize, _partHeader;

        public Rdb(string dir)
        {
            byte[] idx = File.ReadAllBytes(Path.Combine(dir, "ResourceDatabase.idx"));
            for (int n = -1; ; n++)
            {
                string name = n < 0 ? "ResourceDatabase.dat" : string.Format("ResourceDatabase.dat.{0:D3}", n + 1);
                string p = Path.Combine(dir, name);
                if (!File.Exists(p)) break;
                _parts.Add(new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
            }
            if (_parts.Count == 0) throw new FileNotFoundException("no ResourceDatabase.dat in " + dir);
            _partHeader = BitConverter.ToUInt32(idx, 12);
            _partSize = BitConverter.ToUInt32(idx, 184);
            uint block = BitConverter.ToUInt32(idx, 72);
            uint next = BitConverter.ToUInt32(idx, (int)block);
            while (next > 0)
            {
                int count = BitConverter.ToInt16(idx, (int)block + 8);
                int e = (int)block + 28;
                for (int i = 0; i < count; i++, e += 16)
                {
                    long off = ((long)BitConverter.ToUInt32(idx, e) << 32) | BitConverter.ToUInt32(idx, e + 4);
                    int type = Be32(idx, e + 8), inst = Be32(idx, e + 12);
                    if (!_offsets.TryGetValue(type, out var m)) _offsets[type] = m = new SortedDictionary<int, long>();
                    m[inst] = off;
                }
                block = next;
                next = BitConverter.ToUInt32(idx, (int)block);
            }
        }

        static int Be32(byte[] b, int at) => (b[at] << 24) | (b[at + 1] << 16) | (b[at + 2] << 8) | b[at + 3];

        public IEnumerable<int> Types => _offsets.Keys;
        public bool HasType(int type) => _offsets.ContainsKey(type);
        public bool Has(int type, int inst) => _offsets.TryGetValue(type, out var m) && m.ContainsKey(inst);
        public IEnumerable<int> Instances(int type) => _offsets.TryGetValue(type, out var m) ? m.Keys : (IEnumerable<int>)Array.Empty<int>();

        byte[] ReadAt(long logical, int len)
        {
            var outb = new byte[len];
            if (len == 0) return outb;
            int part = (int)(logical / _partSize);
            long pos = logical - (long)part * (_partSize - _partHeader);
            int filled = 0;
            while (filled < len)
            {
                if (part >= _parts.Count) throw new EndOfStreamException("record beyond the last .dat part");
                var f = _parts[part];
                f.Seek(pos, SeekOrigin.Begin);
                int got = f.Read(outb, filled, len - filled);
                if (got <= 0) throw new EndOfStreamException();
                filled += got;
                if (filled < len) { part++; pos = _partHeader; }
            }
            return outb;
        }

        /// <summary>The record payload. The 34-byte header's size field (at 18) counts 12 header bytes before the payload.</summary>
        public byte[] Read(int type, int inst)
        {
            long off = _offsets[type][inst];
            byte[] hdr = ReadAt(off, 34);
            int len = BitConverter.ToInt32(hdr, 18) - 12;
            return ReadAt(off + 34, len);
        }

        /// <summary>The record as the client's DbObject readers want it: header bytes 22.. plus the payload.</summary>
        public byte[] ReadFramed(int type, int inst)
        {
            long off = _offsets[type][inst];
            byte[] hdr = ReadAt(off, 34);
            int len = BitConverter.ToInt32(hdr, 18);
            return ReadAt(off + 22, len);
        }

        public void Dispose() { foreach (var f in _parts) f.Dispose(); }
    }
}
