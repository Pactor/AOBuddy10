"""Acceptance test: map the owner's walked points (nav/<pf>.json) into each room's template
rect and compare the tilemap PNG heights with the walked Y, for every rotation convention."""
import sys, os, json, struct, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from rdb import Rdb
from meshdecode import DB
from playfield import parse_rooms
from tilemap import pngs

pf = int(sys.argv[1]) if len(sys.argv) > 1 else 127
rdb = Rdb(DB)
_, pb = rdb.read(1000001, pf)
hdr, rooms, _ = parse_rooms(pb)
_, tb = rdb.read(1000009, hdr["tilemap"])
cell, hs = struct.unpack_from("<2f", tb, 16)
ims = [im.load() for _, im in pngs(tb)]
nav = json.load(open(os.path.join(os.path.dirname(__file__), "..", "..", "Build", "Plugins", "AOBuddy", "nav", "%d.json" % pf)))
pts = [tuple(p) for seg in nav["Segments"] for p in seg]
print("rooms %d, walked points %d, cell %g hscale %g" % (len(rooms), len(pts), cell, hs))

# per-room world footprint from the collision export (127.tri), index-aligned with the room list
f = open(os.path.join(os.path.dirname(__file__), "out", "%d.tri" % pf), "rb").read()
n = struct.unpack_from("<i", f, 12)[0]; p = 16; foot = []
for i in range(n):
    inst, vc = struct.unpack_from("<2i", f, p); p += 20
    xs = struct.unpack_from("<%df" % (vc * 3), f, p); p += vc * 12
    foot.append(((min(xs[0::3]), max(xs[0::3])), (min(xs[2::3]), max(xs[2::3]))))

def rotate(dx, dz, rot, conv):
    """template-local offset from world offset for rotation rot under convention conv"""
    k = rot if conv >= 0 else (4 - rot) % 4
    for _ in range(k):
        dx, dz = dz, -dx
    return dx, dz

for k, im in enumerate(ims[:5]):
    for conv in (1, -1):
        for order in ("xz", "zx"):
            err = []; miss = 0
            for r in rooms:
                (xa, xb), (za, zb) = foot[r.index]
                x1, z1, x2, z2 = r.rect
                cx, cz = (x1 + x2) / 2.0, (z1 + z2) / 2.0
                for (x, y, z) in pts:
                    if not (xa <= x <= xb and za <= z <= zb):
                        continue
                    dx, dz = x - r.pos[0], z - r.pos[2]
                    dx, dz = rotate(dx, dz, r.rot, conv)
                    if order == "zx": dx, dz = dz, dx
                    px, pz = int(math.floor(cx + dx / cell)), int(math.floor(cz + dz / cell))
                    if not (0 <= px < 280 and 0 <= pz < 280):
                        miss += 1; continue
                    v = im[px, pz]
                    v = v[0] if isinstance(v, tuple) else v
                    err.append(abs(r.pos[1] + v * hs - y))
            if err:
                err.sort()
                print("png %d conv %+d order %s: n=%d miss=%d median|dy|=%.2f p90=%.2f" % (k, conv, order, len(err), miss, err[len(err)//2], err[int(len(err)*0.9)]))
