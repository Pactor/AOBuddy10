using System;
using System.Collections.Generic;
using System.Text;

namespace AONavExtractor
{
    /// <summary>
    /// The client's reflective object format (RDB types 1010001, 1010026, and the object inside
    /// a 'CHGA' ground record). Header: i32 version, i32, i32, i32 1, i32 nameCount, u8 0, then a
    /// NUL-separated pool: root class name, then (code, field) pairs, an empty string closing a
    /// class. Every class and field name is numbered in pool order; that number is the 1-byte tag
    /// in the data. Then i32 objectCount and objectCount+1 objects (object 0 is a wrapper whose
    /// 'obj' points at the root): i32 byteSize, i32 1, i32 entryCount, entries of
    /// u8 tag, i32 valueType, i32 elemSize, i32 totalSize, u8[totalSize]. Pointer fields
    /// (type 17) are 0-based indices into the object list, not counting the wrapper.
    /// </summary>
    public sealed class ReflectiveRecord
    {
        public const int T_BOOL = 0, T_INT = 3, T_STR = 6, T_RAW = 9, T_FLOAT = 10, T_VEC3 = 12, T_QUAT = 13, T_MAT = 15, T_RGB = 16, T_REF = 17;

        public sealed class Entry
        {
            public int Tag; public string Name; public int ValueType; public int ElemSize; public byte[] Raw;
            public int Int => BitConverter.ToInt32(Raw, 0);
            public int[] Ints { get { var a = new int[Raw.Length / 4]; for (int i = 0; i < a.Length; i++) a[i] = BitConverter.ToInt32(Raw, i * 4); return a; } }
            public float Float => BitConverter.ToSingle(Raw, 0);
            public string Str { get { int n = BitConverter.ToInt32(Raw, 0); return Encoding.Latin1.GetString(Raw, 4, Math.Min(n, Raw.Length - 4)); } }
            /// <summary>Length-prefixed blob (elemSize 0) without its prefix.</summary>
            public byte[] Blob { get { int n = BitConverter.ToInt32(Raw, 0); var b = new byte[Math.Min(n, Raw.Length - 4)]; Buffer.BlockCopy(Raw, 4, b, 0, b.Length); return b; } }
        }

        public sealed class Obj
        {
            public int Index; public string Class; public int Offset; public List<Entry> Entries = new List<Entry>();
            public Entry Get(string name) { foreach (var e in Entries) if (e.Name == name) return e; return null; }
            public int GetInt(string name, int dflt = 0) { var e = Get(name); return e != null && e.Raw.Length >= 4 ? e.Int : dflt; }
        }

        public int NameCount;
        public List<string> Names = new List<string>();
        public List<Obj> Objects = new List<Obj>();   // includes the wrapper at 0
        public int ObjectCount, PoolEnd, DataEnd;
        public Obj Root;
        public List<Obj> Nodes => Objects.GetRange(1, Objects.Count - 1);
        public Obj Ref(int idx) => idx >= 0 && idx + 1 < Objects.Count ? Objects[idx + 1] : null;

        public ReflectiveRecord(byte[] b, int start = 0)
        {
            NameCount = BitConverter.ToInt32(b, start + 16);
            int p = start + 21;
            bool inClass = false; string pendingCode = null;
            while (Names.Count < NameCount)
            {
                int e = Array.IndexOf(b, (byte)0, p);
                string s = Encoding.Latin1.GetString(b, p, e - p);
                p = e + 1;
                if (!inClass) { inClass = true; Names.Add(s); }
                else if (s.Length == 0) inClass = false;
                else if (pendingCode == null) pendingCode = s;
                else { Names.Add(s); pendingCode = null; }
            }
            if (p < b.Length && b[p] == 0) p++;
            PoolEnd = p;
            ObjectCount = BitConverter.ToInt32(b, p); p += 4;
            for (int n = 0; n < ObjectCount + 1 && p + 12 <= b.Length; n++)
            {
                var o = new Obj { Index = n - 1, Offset = p };
                int size = BitConverter.ToInt32(b, p), one = BitConverter.ToInt32(b, p + 4), count = BitConverter.ToInt32(b, p + 8);
                p += 12;
                if (one != 1) throw new InvalidDataException(string.Format("object {0} at 0x{1:X}: expected 1, got {2}", n, o.Offset, one));
                for (int k = 0; k < count; k++)
                {
                    var en = new Entry { Tag = b[p], ValueType = BitConverter.ToInt32(b, p + 1), ElemSize = BitConverter.ToInt32(b, p + 5) };
                    int total = BitConverter.ToInt32(b, p + 9);
                    p += 13;
                    en.Raw = new byte[total]; Buffer.BlockCopy(b, p, en.Raw, 0, total); p += total;
                    en.Name = en.Tag < Names.Count ? Names[en.Tag] : "?" + en.Tag;
                    o.Entries.Add(en);
                    if (en.Name == "__class_id__" && en.Raw.Length >= 4) { int c = en.Int; o.Class = c >= 0 && c < Names.Count ? Names[c] : null; }
                }
                Objects.Add(o);
            }
            DataEnd = p;
            if (Objects.Count > 0)
            {
                var w = Objects[0].Get("obj");
                Root = w != null ? Ref(w.Int) : null;
            }
        }
    }

    public sealed class InvalidDataException : Exception { public InvalidDataException(string m) : base(m) { } }
}
