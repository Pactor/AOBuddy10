"""Decoder for the AO client's reflective object format (RDB types 1010001 / 1010026).

Record layout, all little-endian, measured on type 1010001:
  i32 version(3)  i32 flags  i32 flags  i32 one  i32 nameCount  u8 zero
  string pool: NUL-separated. Root class name, then (code, field) pairs; an empty
      string closes a class and the next string opens one. Class and field names are
      numbered in pool order and that number is the 1-byte tag used in the data.
  i32 objectCount   (nodes; the wrapper object that follows is not counted)
  objects, sequential:  i32 byteSize  i32 one  i32 entryCount  then entries:
      u8 tag  i32 valueType  i32 elemSize  i32 totalSize  u8[totalSize]
  Pointer fields (valueType 17) hold the 0-based index of another object in the same
  record, -1 for none. Object 0 is a wrapper whose 'obj' points at the real root.
  Variable-length values (elemSize 0: strings, vertex/index arrays) carry their own
  i32 length as the first 4 bytes of the value.

valueType codes seen: 0 bool/u8, 3 i32, 6 string, 9 raw array, 10 f32, 12 vec3,
13 quat, 15 4x4 matrix (row-major, translation in row 3), 16 rgb f32, 17 object ref.
"""
import struct, sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from rdb import Rdb

DB = r"E:\Funcom\Anarchy Online\cd_image\data\db"

T_BOOL, T_INT, T_STR, T_RAW, T_FLOAT, T_VEC3, T_QUAT, T_MAT, T_RGB, T_REF = 0, 3, 6, 9, 10, 12, 13, 15, 16, 17


class Entry:
    __slots__ = ("tag", "name", "vtype", "elem", "raw")

    def __init__(self, tag, name, vtype, elem, raw):
        self.tag, self.name, self.vtype, self.elem, self.raw = tag, name, vtype, elem, raw

    @property
    def value(self):
        t, r = self.vtype, self.raw
        if t == T_INT or t == T_REF:
            return struct.unpack("<i", r)[0] if len(r) == 4 else list(struct.unpack("<%di" % (len(r) // 4), r))
        if t == T_FLOAT:
            return struct.unpack("<f", r)[0] if len(r) == 4 else list(struct.unpack("<%df" % (len(r) // 4), r))
        if t == T_BOOL:
            return r[0] if len(r) == 1 else list(r)
        if t == T_VEC3:
            return struct.unpack("<3f", r)
        if t == T_RGB:
            return struct.unpack("<3f", r)
        if t == T_QUAT:
            return struct.unpack("<4f", r)
        if t == T_MAT:
            return [struct.unpack_from("<4f", r, i * 16) for i in range(4)]
        if t == T_STR:
            n = struct.unpack_from("<i", r, 0)[0]
            return r[4:4 + n].decode("latin1")
        if t == T_RAW:
            if self.elem == 0:
                n = struct.unpack_from("<i", r, 0)[0]
                return r[4:4 + n]
            return r
        return r


class Obj:
    __slots__ = ("index", "cls", "entries", "offset")

    def __init__(self, index, cls, entries, offset):
        self.index, self.cls, self.entries, self.offset = index, cls, entries, offset

    def get(self, name, default=None):
        for e in self.entries:
            if e.name == name:
                return e.value
        return default

    def __repr__(self):
        return "Obj(%d %s)" % (self.index, self.cls)


class Record:
    def __init__(self, blob):
        self.blob = blob
        self.version, self.f1, self.f2, self.one, self.name_count = struct.unpack_from("<5i", blob, 0)
        p = 21
        self.names = []          # tag -> name
        self.classes = []        # [(class, [(code, field)])]
        cur = None
        pending_code = None
        while len(self.names) < self.name_count:
            e = blob.index(b"\0", p)
            s = blob[p:e].decode("latin1")
            p = e + 1
            if cur is None:
                cur = (s, [])
                self.classes.append(cur)
                self.names.append(s)
            elif s == "":
                cur = None
            elif pending_code is None:
                pending_code = s
            else:
                cur[1].append((pending_code, s))
                self.names.append(s)
                pending_code = None
        # a class may be closed by the empty string that follows the last field
        if p < len(blob) and blob[p] == 0:
            p += 1
        self.pool_end = p
        self.object_count = struct.unpack_from("<i", blob, p)[0]
        p += 4
        self.objects = []
        n = 0
        while n < self.object_count + 1 and p + 12 <= len(blob):
            start = p
            size, one, count = struct.unpack_from("<3i", blob, p)
            p += 12
            if one != 1:
                raise ValueError("object %d at 0x%X: expected 1, got %d" % (n, start, one))
            entries = []
            cls = None
            for _ in range(count):
                tag = blob[p]
                vtype, elem, total = struct.unpack_from("<3i", blob, p + 1)
                p += 13
                raw = blob[p:p + total]
                p += total
                name = self.names[tag] if tag < len(self.names) else "?%d" % tag
                en = Entry(tag, name, vtype, elem, raw)
                entries.append(en)
                if name == "__class_id__":
                    cls = self.names[en.value]
            self.objects.append(Obj(n, cls, entries, start))
            n += 1
        self.data_end = p
        # object 0 is a wrapper: its only field is 'obj' -> real root
        self.root = None
        self.wrapper = None
        self.nodes = []
        if self.objects:
            w = self.objects[0]
            r = w.get("obj")
            # the wrapper holds no __class_id__; objects are numbered from the first real one
            self.wrapper = w
            self.nodes = self.objects[1:]
            self.root = self.nodes[r] if r is not None and 0 <= r < len(self.nodes) else None

    def ref(self, idx):
        return self.nodes[idx] if idx is not None and 0 <= idx < len(self.nodes) else None


def load(rtype, inst, rdb=None):
    rdb = rdb or Rdb(DB)
    _, blob = rdb.read(rtype, inst)
    return Record(blob)


def dump(rec, maxraw=48):
    print("version=%d flags=%08x/%08x names=%d objects=%d (parsed %d) pool_end=0x%X data_end=0x%X of 0x%X"
          % (rec.version, rec.f1, rec.f2, rec.name_count, rec.object_count, len(rec.objects),
             rec.pool_end, rec.data_end, len(rec.blob)))
    for cls, fields in rec.classes:
        print("  class %-22s %s" % (cls, " ".join("%s:%s" % (c, f) for c, f in fields)))
    for o in rec.objects:
        print("OBJ %d %s @0x%X" % (o.index - 1, o.cls, o.offset))
        for e in o.entries:
            v = e.value
            if isinstance(v, (bytes, bytearray)):
                v = "%d bytes %s%s" % (len(v), v[:maxraw].hex(" "), " ..." if len(v) > maxraw else "")
            print("   %02X %-20s t=%-2d el=%-3d %s" % (e.tag, e.name, e.vtype, e.elem, v))


if __name__ == "__main__":
    rec = load(int(sys.argv[1]), int(sys.argv[2]))
    dump(rec)
