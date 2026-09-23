"""Parse the AO client's reflective record format (RDB types 1010001, 1010026, 1000009).

These records carry their own schema: a header, a NUL-separated string pool listing every
class and field with a type code, then a tagged binary section holding the values.

The pool is read as: the root class name, then (type code, field name) pairs, with an empty
string ending a class and the next string naming the next one. Verified against type
1010001 instance 1445, where it yields RTriMesh_t / FAFTriMeshData_t / TriList / BVolume_t
with exactly the fields the mangled names in N3.dll and Collision.dll imply.

Usage: python meshschema.py <type> <instance>
"""
import struct
import sys

sys.path.insert(0, __import__("os").path.dirname(__import__("os").path.abspath(__file__)))
from rdb import Rdb  # noqa: E402

DB = r"E:\Funcom\Anarchy Online\cd_image\data\db"
POOL_START = 21          # after the 5 header ints plus one byte


def read_pool(blob):
    """Every NUL-terminated printable string at the head of the record."""
    out = []
    p = POOL_START
    while p < len(blob):
        e = blob.find(b"\0", p)
        if e < 0:
            break
        s = blob[p:e]
        if s and not all(32 <= c < 127 for c in s):
            break
        out.append(s.decode("latin1"))
        p = e + 1
        if len(out) > 4000:
            break
    return out, p


def parse_schema(strings):
    """[(class name, [(type code, field name), ...]), ...]"""
    classes = []
    cur = None
    i = 0
    while i < len(strings):
        s = strings[i]
        if cur is None:
            cur = (s, [])
            i += 1
            continue
        if s == "":
            classes.append(cur)
            cur = None
            i += 1
            continue
        # a type code is a single digit; it precedes the field it describes
        if len(s) == 1 and s.isdigit() and i + 1 < len(strings):
            cur[1].append((s, strings[i + 1]))
            i += 2
            continue
        i += 1
    if cur is not None:
        classes.append(cur)
    return classes


def main():
    rtype = int(sys.argv[1]) if len(sys.argv) > 1 else 1010001
    inst = int(sys.argv[2]) if len(sys.argv) > 2 else 1445
    rdb = Rdb(DB)
    _, blob = rdb.read(rtype, inst)
    head = struct.unpack_from("<5i", blob, 0)
    strings, pool_end = read_pool(blob)
    print("type %d instance %d: %d bytes" % (rtype, inst, len(blob)))
    print("header ints: %s" % (head,))
    print("pool: %d strings, ends at 0x%X, %d bytes of tagged data follow"
          % (len(strings), pool_end, len(blob) - pool_end))
    for name, fields in parse_schema(strings):
        print("  class %s" % name)
        for code, field in fields:
            print("      %s  %s" % (code, field))
    print("first tagged bytes: %s" % blob[pool_end:pool_end + 64].hex(" "))


if __name__ == "__main__":
    main()
