"""Sequential dump of the tagged value stream that follows the reflective schema pool."""
import struct, sys, os
sys.path.insert(0, os.path.dirname(__file__))
from rdb import Rdb
from meshschema import read_pool, DB

def names_from_pool(strings):
    """Global tag table: every pool string that is not a type digit or empty, in order."""
    out = []
    i = 0
    while i < len(strings):
        s = strings[i]
        if s == "":
            i += 1; continue
        if len(s) == 1 and s.isdigit():
            out.append(strings[i+1]); i += 2; continue
        out.append(s); i += 1      # class name
    return out

def fmt(t, data):
    if t == 3 or t == 0x11:
        return struct.unpack('<i', data)[0] if len(data)==4 else data.hex()
    if t == 0x0a: return struct.unpack('<f', data)[0]
    if t == 0x0c: return struct.unpack('<3f', data)
    if t == 0x0d: return struct.unpack('<4f', data)
    if t == 0x0f: return [tuple(round(v,4) for v in struct.unpack_from('<4f', data, r*16)) for r in range(4)]
    if t == 0: return data[0] if len(data)==1 else data.hex()
    if t == 6: return data
    return data[:32].hex(' ') + (' ...' if len(data)>32 else '')

def dump(blob, pool_end, names, limit=200):
    d = blob; p = pool_end
    print("prefix int: %d" % struct.unpack_from('<i', d, p)[0]); p += 4
    n = 0
    while p < len(d) and n < limit:
        size, one, count = struct.unpack_from('<3i', d, p); p += 12
        print("OBJ @%05X size=%d x=%d entries=%d" % (p-12, size, one, count))
        for _ in range(count):
            if p >= len(d): break
            tag = d[p]; t, sz, el = struct.unpack_from('<3i', d, p+1); p += 13
            if sz == 0:
                blen = struct.unpack_from('<i', d, p)[0]; p += 4
                data = d[p:p+blen]; p += blen
                extra = " (blob %d)" % blen
            else:
                data = d[p:p+sz]; p += sz; extra = ""
            nm = names[tag] if tag < len(names) else "?%d" % tag
            print("   %02X %-20s type=%-3d size=%-6d el=%-4d%s  %s" % (tag, nm, t, sz, el, extra, fmt(t, data) if sz or blen<=64 else data[:48].hex(' ')+' ...'))
        n += 1
    print("ended at %X of %X" % (p, len(d)))

if __name__ == "__main__":
    rtype = int(sys.argv[1]); inst = int(sys.argv[2])
    rdb = Rdb(DB); _, blob = rdb.read(rtype, inst)
    strings, pe = read_pool(blob)
    names = names_from_pool(strings)
    print("tag table (%d): %s" % (len(names), names))
    dump(blob, pe, names)
