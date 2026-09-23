"""Dungeon tilemap record (RDB 1000009, 'GNDA' variant): tagged sections carrying PNGs."""
import struct, zlib, io, sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image
from rdb import Rdb
from meshdecode import DB


def sections(blob):
    """[(tag, offset, size, version)] for the 4-char tags that head each section."""
    out = []
    p = 0
    while p + 12 <= len(blob):
        tag = blob[p:p + 4]
        size, ver = struct.unpack_from("<2I", blob, p + 4)
        if not all(65 <= c <= 90 for c in tag) or size <= 0 or p + size > len(blob):
            break
        out.append((tag.decode(), p, size, ver))
        p += size
    return out


def pngs(blob):
    out = []
    p = 0
    while True:
        i = blob.find(b"\x89PNG\r\n\x1a\n", p)
        if i < 0:
            break
        j = blob.find(b"IEND", i) + 8
        out.append((i, Image.open(io.BytesIO(blob[i:j]))))
        p = j
    return out


if __name__ == "__main__":
    inst = int(sys.argv[1]) if len(sys.argv) > 1 else 126
    rdb = Rdb(DB)
    _, b = rdb.read(1000009, inst)
    w, h = struct.unpack_from("<2H", b, 12)
    cell, hs = struct.unpack_from("<2f", b, 16)
    print("record %d: %d bytes, %dx%d cell=%g hscale=%g" % (inst, len(b), w, h, cell, hs))
    for tag, off, size, ver in sections(b):
        print("  section %s @0x%X size %d ver %d" % (tag, off, size, ver))
    for off, im in pngs(b):
        print("  png @0x%X %s %s" % (off, im.mode, im.size))
