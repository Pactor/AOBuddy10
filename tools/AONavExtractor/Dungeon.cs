using System;
using System.Collections.Generic;
using System.Text;

namespace AONavExtractor
{
    /// <summary>
    /// A dungeon playfield: the RDB 1000001 record's room list (versions 8, 9, 10) joined with its 'GNDA'
    /// tilemap (RDB 1000009), a 2 m-cell template atlas stored as PNG layers. Layout of the
    /// playfield record (measured on pf 127):
    ///   header: i32 version, i32 playfield, char name[32], i32 tilemapId, i32 10, i32 roomCount; rooms at 0x34 (v8) or 0x60
    ///   room: u16 flags (0x05xx, bits 0-1 rotation, bit 7 name present), u16 x1,z1,x2,z2 atlas rect,
    ///         f32 x,y,z world centre (y = lowest floor cell), u16 nDoors, nDoors x (u16,u16),
    ///         char name[32] if flags&0x80, u32 lmBytes, u32 lmSamples, lmBytes of lightmap,
    ///         nPolys (= lightmap trailer/1009 - 1) x (u32 id, u32 nVerts, vec3[], u32 nTris, u16[3*nTris]),
    ///         u32 nObjects, nObjects x 44 bytes (pos, quat, point, radius).
    /// </summary>
    public sealed class Dungeon
    {
        public sealed class Poly { public int Id; public List<float[]> Verts = new List<float[]>(); public List<int[]> Tris = new List<int[]>(); }

        public sealed class Room
        {
            public int Index, Flags, Rot; public int[] Rect; public float[] Pos; public string Name = "";
            public List<int[]> Doors = new List<int[]>(); public List<Poly> Polys = new List<Poly>(); public List<float[]> Objects = new List<float[]>();
            public int HeightBase;
            public int[][] Tile, Height, Flags3;     // rows z1..z2 of cols x1..x2
        }

        public int Playfield, Tilemap, AtlasW, AtlasH;
        public string Name;
        public float Cell, HeightScale;
        public List<Room> Rooms = new List<Room>();

        /// <summary>Rooms start at 0x34 in version 8 records and at 0x60 (zero padded) in 9 and 10.</summary>
        public static int RoomsStart(byte[] b) => BitConverter.ToInt32(b, 0) == 8 ? 0x34 : 0x60;

        public static bool IsDungeonRecord(byte[] b)
        {
            if (b.Length < 0x40) return false;
            int v = BitConverter.ToInt32(b, 0);
            if (v != 8 && v != 9 && v != 10) return false;
            int start = RoomsStart(b);
            return b.Length > start + 2 && (BitConverter.ToUInt16(b, start) & 0xFF00) == 0x0500;
        }

        public static Dungeon Read(Rdb rdb, int pf, byte[] b)
        {
            var d = new Dungeon { Playfield = pf, Name = Util.CStr(b, 8, 32), Tilemap = BitConverter.ToInt32(b, 0x28) };
            int nrooms = BitConverter.ToInt32(b, 0x30);
            byte[] tb = rdb.Read(1000009, d.Tilemap);
            if (tb.Length < 24 || tb[0] != 'G' || tb[1] != 'N' || tb[2] != 'D' || tb[3] != 'A')
                throw new InvalidDataException("pf " + pf + ": tilemap " + d.Tilemap + " is not GNDA");
            d.AtlasW = BitConverter.ToUInt16(tb, 12); d.AtlasH = BitConverter.ToUInt16(tb, 14);
            d.Cell = BitConverter.ToSingle(tb, 16); d.HeightScale = BitConverter.ToSingle(tb, 20);
            List<Png> layers = Png.FindAll(tb);
            if (layers.Count < 2) throw new InvalidDataException("pf " + pf + ": tilemap has " + layers.Count + " layers");
            Png tileL = layers[0], hgtL = layers[1], flgL = layers.Count > 3 ? layers[3] : null;

            int p = RoomsStart(b);
            for (int r = 0; r < nrooms; r++)
            {
                var rm = new Room { Index = r };
                rm.Flags = BitConverter.ToUInt16(b, p); rm.Rot = rm.Flags & 3;
                rm.Rect = new int[] { BitConverter.ToUInt16(b, p + 2), BitConverter.ToUInt16(b, p + 4), BitConverter.ToUInt16(b, p + 6), BitConverter.ToUInt16(b, p + 8) };
                p += 10;
                rm.Pos = new[] { BitConverter.ToSingle(b, p), BitConverter.ToSingle(b, p + 4), BitConverter.ToSingle(b, p + 8) }; p += 12;
                int nd = BitConverter.ToUInt16(b, p); p += 2;
                for (int i = 0; i < nd; i++) { rm.Doors.Add(new int[] { BitConverter.ToUInt16(b, p), BitConverter.ToUInt16(b, p + 2) }); p += 4; }
                if ((rm.Flags & 0x80) != 0) { rm.Name = Util.CStr(b, p, 32); p += 32; }
                int lmBytes = BitConverter.ToInt32(b, p); p += 8;
                uint trailer = BitConverter.ToUInt32(b, p + lmBytes - 4);
                p += lmBytes;
                // nPolys = trailer / 1009 - 1; each: u32 id, u32 nVerts, vec3[], u32 nTris, u16[3*nTris] (room-local)
                int npolys = trailer >= 1009 && trailer % 1009 == 0 ? (int)(trailer / 1009) - 1 : 0;
                for (int k = 0; k < npolys; k++)
                {
                    var poly = new Poly { Id = BitConverter.ToInt32(b, p) }; p += 4;
                    int n = BitConverter.ToInt32(b, p); p += 4;
                    for (int i = 0; i < n; i++) { poly.Verts.Add(new[] { BitConverter.ToSingle(b, p), BitConverter.ToSingle(b, p + 4), BitConverter.ToSingle(b, p + 8) }); p += 12; }
                    int nt = BitConverter.ToInt32(b, p); p += 4;
                    for (int i = 0; i < nt; i++) { poly.Tris.Add(new int[] { BitConverter.ToUInt16(b, p), BitConverter.ToUInt16(b, p + 2), BitConverter.ToUInt16(b, p + 4) }); p += 6; }
                    rm.Polys.Add(poly);
                }
                // then u32 nObjects x (pos, quaternion, point, radius) in world coordinates - meaning OPEN
                int nobj = BitConverter.ToInt32(b, p); p += 4;
                for (int i = 0; i < nobj; i++)
                {
                    var o = new float[11];
                    for (int j = 0; j < 11; j++) o[j] = BitConverter.ToSingle(b, p + 4 * j);
                    rm.Objects.Add(o); p += 44;
                }
                int x1 = rm.Rect[0], z1 = rm.Rect[1], x2 = rm.Rect[2], z2 = rm.Rect[3];
                int rows = z2 - z1 + 1, cols = x2 - x1 + 1;
                rm.Tile = new int[rows][]; rm.Height = new int[rows][]; rm.Flags3 = new int[rows][];
                int kmin = int.MaxValue;
                for (int zz = 0; zz < rows; zz++)
                {
                    rm.Tile[zz] = new int[cols]; rm.Height[zz] = new int[cols]; rm.Flags3[zz] = new int[cols];
                    for (int xx = 0; xx < cols; xx++)
                    {
                        int ax = x1 + xx, az = z1 + zz;
                        bool inside = ax < tileL.Width && az < tileL.Height;
                        rm.Tile[zz][xx] = inside ? tileL.At(ax, az) : 0;
                        rm.Height[zz][xx] = inside && ax < hgtL.Width && az < hgtL.Height ? hgtL.At(ax, az) : 0;
                        rm.Flags3[zz][xx] = flgL != null && inside && ax < flgL.Width && az < flgL.Height ? flgL.At(ax, az) : 0;
                        if (rm.Tile[zz][xx] != 0 && rm.Height[zz][xx] < kmin) kmin = rm.Height[zz][xx];
                    }
                }
                rm.HeightBase = kmin == int.MaxValue ? 0 : kmin;
                d.Rooms.Add(rm);
            }
            return d;
        }

        /// <summary>World floor height of the tile under (x, z) in one room, or NaN.</summary>
        public double FloorHeight(Room rm, double x, double z)
        {
            double dx = x - rm.Pos[0], dz = z - rm.Pos[2];
            for (int i = 0; i < ((-rm.Rot) % 4 + 4) % 4; i++) { double t = dx; dx = dz; dz = -t; }
            int x1 = rm.Rect[0], z1 = rm.Rect[1], x2 = rm.Rect[2], z2 = rm.Rect[3];
            int a = (int)Math.Floor((x1 + x2 + 1) / 2.0 + dx / Cell), bb = (int)Math.Floor((z1 + z2 + 1) / 2.0 + dz / Cell);
            if (a < x1 || a > x2 || bb < z1 || bb > z2) return double.NaN;
            if (rm.Tile[bb - z1][a - x1] == 0) return double.NaN;
            return rm.Pos[1] + (rm.Height[bb - z1][a - x1] - rm.HeightBase) * HeightScale;
        }

        public void WriteJson(string path)
        {
            var w = new JsonOut();
            w.Obj(); w.Key("playfield").Num(Playfield); w.Key("name").Str(Name); w.Key("tilemap").Num(Tilemap);
            w.Key("cell").Num(Cell); w.Key("heightScale").Num(HeightScale);
            w.Key("atlas").Arr().Num(AtlasW).Num(AtlasH).End();
            w.Key("rooms").Arr();
            foreach (var rm in Rooms)
            {
                w.Obj();
                w.Key("index").Num(rm.Index); w.Key("name").Str(rm.Name); w.Key("flags").Num(rm.Flags); w.Key("rot").Num(rm.Rot);
                w.Key("rect").Arr(); foreach (var v in rm.Rect) w.Num(v); w.End();
                w.Key("pos").Arr(); foreach (var v in rm.Pos) w.Num(Math.Round(v, 3)); w.End();
                w.Key("heightBase").Num(rm.HeightBase);
                w.Key("doors").Arr(); foreach (var dd in rm.Doors) { w.Arr().Num(dd[0]).Num(dd[1]).End(); } w.End();
                w.Key("polys").Arr();
                foreach (var pl in rm.Polys)
                {
                    w.Obj();
                    w.Key("id").Num(pl.Id);
                    w.Key("verts").Arr(); foreach (var v in pl.Verts) { w.Arr(); foreach (var c in v) w.Num(Math.Round(c, 3)); w.End(); } w.End();
                    w.Key("tris").Arr(); foreach (var t in pl.Tris) { w.Arr().Num(t[0]).Num(t[1]).Num(t[2]).End(); } w.End();
                    w.End();
                }
                w.End();
                w.Key("objects").Arr();
                foreach (var o in rm.Objects)
                {
                    w.Obj();
                    w.Key("pos").Arr(); for (int j = 0; j < 3; j++) w.Num(Math.Round(o[j], 3)); w.End();
                    w.Key("rot").Arr(); for (int j = 3; j < 7; j++) w.Num(Math.Round(o[j], 4)); w.End();
                    w.Key("point").Arr(); for (int j = 7; j < 10; j++) w.Num(Math.Round(o[j], 3)); w.End();
                    w.Key("radius").Num(Math.Round(o[10], 3));
                    w.End();
                }
                w.End();
                w.Key("tile").Grid(rm.Tile); w.Key("height").Grid(rm.Height); w.Key("flags3").Grid(rm.Flags3);
                w.End();
            }
            w.End(); w.End();
            System.IO.File.WriteAllText(path, w.ToString());
        }
    }

    /// <summary>A small JSON writer: nested arrays and objects, compact or indented, invariant numbers.</summary>
    public sealed class JsonOut
    {
        readonly StringBuilder _sb = new StringBuilder();
        readonly Stack<char> _open = new Stack<char>();     // '[' or '{' per open container
        readonly Stack<bool> _empty = new Stack<bool>();    // nothing written yet in that container
        bool _afterKey;
        readonly bool _indent;

        public JsonOut(bool indent = false) { _indent = indent; }

        void BeforeValue()
        {
            if (_afterKey) { _afterKey = false; return; }
            if (_open.Count == 0) return;
            if (!_empty.Peek()) _sb.Append(',');
            _empty.Pop(); _empty.Push(false);
            if (_indent) _sb.Append('\n').Append(' ', _open.Count);
        }

        public JsonOut Obj() { BeforeValue(); _sb.Append('{'); _open.Push('{'); _empty.Push(true); return this; }
        public JsonOut Arr() { BeforeValue(); _sb.Append('['); _open.Push('['); _empty.Push(true); return this; }
        public JsonOut End()
        {
            char c = _open.Pop(); bool empty = _empty.Pop();
            if (_indent && !empty) _sb.Append('\n').Append(' ', _open.Count);
            _sb.Append(c == '[' ? ']' : '}');
            return this;
        }
        public JsonOut Key(string k) { BeforeValue(); _sb.Append(Quote(k)).Append(_indent ? ": " : ":"); _afterKey = true; return this; }
        JsonOut Raw(string text) { BeforeValue(); _sb.Append(text); return this; }
        public JsonOut Num(long v) => Raw(v.ToString(System.Globalization.CultureInfo.InvariantCulture));
        public JsonOut Num(double v) => Raw(double.IsNaN(v) ? "null" : v.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        public JsonOut Bool(bool v) => Raw(v ? "true" : "false");
        public JsonOut Null() => Raw("null");
        public JsonOut Str(string s) => Raw(Quote(s));
        public JsonOut Grid(int[][] g) { Arr(); foreach (var row in g) { Arr(); foreach (var v in row) Num(v); End(); } End(); return this; }

        static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in s ?? "")
            {
                if (c == '"') sb.Append("\\\""); else if (c == '\\') sb.Append("\\\\");
                else if (c < 0x20) sb.AppendFormat("\\u{0:x4}", (int)c); else sb.Append(c);
            }
            return sb.Append('"').ToString();
        }
        public override string ToString() => _sb.ToString();
    }
}
