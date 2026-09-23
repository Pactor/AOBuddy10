"""Which world->template mapping is right? Score each convention by how many walked points
land inside the room rect on a populated tile-id pixel (png 0)."""
import sys, os, json, struct, math, collections
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
f = open(os.path.join(os.path.dirname(__file__), "out", "%d.tri" % pf), "rb").read()
n = struct.unpack_from("<i", f, 12)[0]; p = 16; foot = []
for i in range(n):
    inst, vc = struct.unpack_from("<2i", f, p); p += 20
    xs = struct.unpack_from("<%df" % (vc * 3), f, p); p += vc * 12
    foot.append(((min(xs[0::3]), max(xs[0::3])), (min(xs[2::3]), max(xs[2::3]))))

# assign each walked point to the containing room whose base height is nearest
assign = collections.defaultdict(list)
for (x, y, z) in pts:
    cands = [r for r in rooms if foot[r.index][0][0] <= x <= foot[r.index][0][1] and foot[r.index][1][0] <= z <= foot[r.index][1][1]]
    if cands:
        r = min(cands, key=lambda r: abs(r.pos[1] - y))
        assign[r.index].append((x, y, z))

def mapping(r, x, z, sign, swap, mirror):
    dx, dz = x - r.pos[0], z - r.pos[2]
    k = (r.rot * sign) % 4
    for _ in range(k):
        dx, dz = dz, -dx
    if swap: dx, dz = dz, dx
    if mirror: dx = -dx
    x1, z1, x2, z2 = r.rect
    return (x1 + x2 + 1) / 2.0 + dx / cell, (z1 + z2 + 1) / 2.0 + dz / cell

convs = [(s, sw, m) for s in (1, -1) for sw in (0, 1) for m in (0, 1)]
tot = collections.Counter(); inrect = collections.Counter(); onfloor = collections.Counter()
per_room = {}
for r in rooms:
    P = assign.get(r.index, [])
    if len(P) < 5: continue
    x1, z1, x2, z2 = r.rect
    best = None
    for c in convs:
        ir = of = 0
        for (x, y, z) in P:
            px, pz = mapping(r, x, z, *c)
            ix, iz = int(math.floor(px)), int(math.floor(pz))
            if x1 <= ix <= x2 and z1 <= iz <= z2:
                ir += 1
                if ims[0][ix, iz]: of += 1
        tot[c] += len(P); inrect[c] += ir; onfloor[c] += of
        if best is None or of > best[1]: best = (c, of)
    per_room[r.index] = (len(P), r.rot, best)
for c in convs:
    print("conv sign=%+d swap=%d mirror=%d: in-rect %.1f%%  on-floor-tile %.1f%%" % (c[0], c[1], c[2], 100.0 * inrect[c] / tot[c], 100.0 * onfloor[c] / tot[c]))
print("per-room winner (points, rot, (conv, hits)):")
for k, v in sorted(per_room.items()):
    print("  room %2d %-28r %s" % (k, rooms[k].name, v))
