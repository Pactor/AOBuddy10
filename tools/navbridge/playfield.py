"""RDB type 1000001 (RDBPlayfield_t) - dungeon room list. Layout measured on pf 127.

  header 0x60: i32 version(10) i32 playfield char name[32] i32 tilemapId i32 ? i32 roomCount
               zeros i32 ?(48) zeros
  room:  u16 flags (0x5xx; low 2 bits = rotation in 90-degree steps, bit 7 varies)
         u16 x1,z1,x2,z2  rect in the tilemap (RDB 1000009 'GNDA' record tilemapId), template space
         f32 x,y,z        world position of the room centre; y = floor base height
         u16 nDoors; nDoors x (u16 a, u16 b)
         char name[32]     only when flags bit 7 is set
         u32 lmBytes, u32 lmSamples; lmBytes of (zlib lightmap + u32 trailer)
         u32 ?; then 0..n polygon meshes in room-local coordinates: u32 nVerts, nVerts x vec3,
             u32 nTris, nTris x 3 u16, u32 0.  The lightmap trailer u32 equals 1009 * (1 + nPolys).
"""
import struct, zlib, sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from rdb import Rdb
from meshdecode import DB


class Room:
    __slots__ = ("index", "flags", "rot", "rect", "pos", "doors", "name", "lm_bytes", "lm_samples",
                 "trailer", "extra", "polys", "offset")


def parse_rooms(b):
    version, pf = struct.unpack_from("<2i", b, 0)
    name = b[8:40].split(b"\0")[0].decode("latin1")
    tilemap, f1, nrooms = struct.unpack_from("<3i", b, 0x28)
    p = 0x60
    rooms = []
    for r in range(nrooms):
        rm = Room()
        rm.index = r
        rm.offset = p
        rm.flags, x1, z1, x2, z2 = struct.unpack_from("<5H", b, p); p += 10
        rm.rot = rm.flags & 3
        rm.rect = (x1, z1, x2, z2)
        rm.pos = struct.unpack_from("<3f", b, p); p += 12
        n = struct.unpack_from("<H", b, p)[0]; p += 2
        rm.doors = [struct.unpack_from("<2H", b, p + 4 * i) for i in range(n)]; p += 4 * n
        if rm.flags & 0x80:               # bit 7: a 32-byte name field follows
            rm.name = b[p:p + 32].split(b"\0")[0].decode("latin1"); p += 32
        else:
            rm.name = ""
        rm.lm_bytes, rm.lm_samples = struct.unpack_from("<2I", b, p); p += 8
        rm.trailer = struct.unpack_from("<I", b, p + rm.lm_bytes - 4)[0]
        p += rm.lm_bytes
        rm.extra = struct.unpack_from("<I", b, p)[0]; p += 4
        rm.polys = []
        while p + 4 <= len(b):
            n = struct.unpack_from("<I", b, p)[0]
            if n == 0 or n > 64:          # a room's flags word is >= 0x500
                break
            verts = [struct.unpack_from("<3f", b, p + 4 + 12 * i) for i in range(n)]
            p += 4 + 12 * n
            ntri = struct.unpack_from("<I", b, p)[0]; p += 4
            tris = [struct.unpack_from("<3H", b, p + 6 * i) for i in range(ntri)]; p += 6 * ntri
            tail = struct.unpack_from("<I", b, p)[0]; p += 4
            rm.polys.append((verts, tris, tail))
        rooms.append(rm)
    return dict(version=version, playfield=pf, name=name, tilemap=tilemap, f1=f1, nrooms=nrooms), rooms, p


if __name__ == "__main__":
    pf = int(sys.argv[1]) if len(sys.argv) > 1 else 127
    rdb = Rdb(DB)
    _, b = rdb.read(1000001, pf)
    hdr, rooms, end = parse_rooms(b)
    print(hdr)
    for rm in rooms:
        print("room %2d @%05X rot=%d rect=%-20s pos=(%6.1f,%7.2f,%6.1f) doors=%2d %-30r lm=%d/%d tr=%d extra=%d polys=%d"
              % (rm.index, rm.offset, rm.rot, rm.rect, rm.pos[0], rm.pos[1], rm.pos[2], len(rm.doors), rm.name,
                 rm.lm_bytes, rm.lm_samples, rm.trailer, rm.extra, len(rm.polys)))
        for verts, tris, tail in rm.polys:
            print("      poly verts=%s tris=%s tail=%d" % ([tuple(round(v, 1) for v in vv) for vv in verts], tris, tail))
    print("rooms end at 0x%X of 0x%X" % (end, len(b)))
    for i in range(end, min(end + 256, len(b)), 16):
        row = b[i:i + 16]
        print("%05X  %-48s %s" % (i, row.hex(" "), "".join(chr(c) if 32 <= c < 127 else "." for c in row)))
