"""composemission.py [zone-in packet ...] - compose a mission instance from its zone-in packet + the pool's rooms.json using the SAME room
model as the static dungeons (rect, rot, centre pos, y = pool y + floor * worldHeight), then grade
the recorded walk: fraction of points on a floor tile within 1 m of the composed height.

Placement (from the first two missions): slot = 10 m; room origin x = X * 10; the room's far z
edge sits at (H - Z) * 10; rooms are (5k + 1) cells wide so they overlap the next slot by the
shared door cell. Rotation: same convention as static rooms (world -> template rotates by -rot
around the footprint centre), tested here against all four rot->k assignments."""
import struct, json, os, sys, math, collections
sys.path.insert(0, r"E:\Funcom\AOBuddy10\tools\navbridge")
PLUGIN = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Build", "Plugins", "AOBuddy")   # packets in missions/, walks in nav/, pools in GameData/Nav/

def decode(pkt):
    b = open(pkt, "rb").read(); p = [0x10]
    def i32(): v = struct.unpack_from(">i", b, p[0])[0]; p[0] += 4; return v
    def i16(): v = struct.unpack_from(">h", b, p[0])[0]; p[0] += 2; return v
    def u8(): v = b[p[0]]; p[0] += 1; return v
    i32(); i32(); i32(); u8(); i32(); land = struct.unpack_from(">3f", b, p[0]); p[0] += 12
    u8(); model = (i32(), i32()); i32(); i32(); i32(); i32(); i32(); i32(); i32()
    i16(); W, H, WH = i16(), i16(), i16(); tpl = i32(); u8(); u8(); u8(); n = i32()
    rooms = []
    for _ in range(n):
        r = i16(); fl = struct.unpack_from(">b", b, p[0])[0]; p[0] += 1; x = u8(); z = u8(); rot = u8(); rooms.append((r, fl, x, z, rot))
    return model[1], tpl, W, H, WH, land, rooms

SLOT = 10.0

def place(pool, placed, W, H, WH, rotmap):
    """Synthetic static-style rooms for one placement list. rotmap: placed rot -> static rot."""
    cell = pool["cell"]; out = []
    for r, fl, X, Z, rot in placed:
        src = pool["rooms"][r]; x1, z1, x2, z2 = src["rect"]; w, h = x2 - x1 + 1, z2 - z1 + 1
        srot = rotmap[rot]
        tw, th = (w, h) if srot % 2 == 0 else (h, w)          # footprint after rotation
        ox = X * SLOT; oz = (H - Z) * SLOT - th * cell
        cx = ox + tw * cell / 2.0; cz = oz + th * cell / 2.0
        out.append(dict(src=src, name=src["name"], rot=srot, rect=src["rect"], tile=src["tile"], height=src["height"],
                        heightBase=src["heightBase"], pos=[cx, src["pos"][1] + fl * WH, cz], floor=fl, slot=(X, Z), prot=rot))
    return out

def floor_height(room, x, z, cell, hs):
    dx, dz = x - room["pos"][0], z - room["pos"][2]
    for _ in range((-room["rot"]) % 4): dx, dz = dz, -dx
    x1, z1, x2, z2 = room["rect"]
    a = int(math.floor((x1 + x2 + 1) / 2.0 + dx / cell)); b = int(math.floor((z1 + z2 + 1) / 2.0 + dz / cell))
    if not (x1 <= a <= x2 and z1 <= b <= z2): return None
    if room["tile"][b - z1][a - x1] == 0: return None
    return room["pos"][1] + (room["height"][b - z1][a - x1] - room["heightBase"]) * hs

def grade(rooms, pts, cell, hs, tol=1.0):
    ok = 0; misses = []
    for (x, y, z) in pts:
        hit = False
        for rm in rooms:
            h = floor_height(rm, x, z, cell, hs)
            if h is not None and abs(h - y) <= tol: hit = True; break
        if hit: ok += 1
        elif len(misses) < 6: misses.append((round(x, 1), round(y, 1), round(z, 1)))
    return ok, misses

def main():
    pkts = sys.argv[1:] or sorted(os.path.join(PLUGIN, "missions", f) for f in os.listdir(os.path.join(PLUGIN, "missions")) if f.startswith("zonein-pf2"))
    for pkt in pkts:
        inst, tpl, W, H, WH, land, placed = decode(pkt)
        pool = json.load(open(os.path.join(PLUGIN, "GameData", "Nav", str(tpl), "rooms.json")))
        nav = os.path.join(PLUGIN, "nav", "%d.json" % inst)
        pts = [tuple(q) for s in json.load(open(nav))["Segments"] for q in s] if os.path.exists(nav) else []
        print("\ninstance %d template %d (%s) %dx%d WH %d rooms %d, walk %d points" % (inst, tpl, pool["name"], W, H, WH, len(placed), len(pts)))
        if not pts: continue
        results = []
        for k in range(4):               # static rot = (prot * s + k) % 4 for s in (1, -1)
            for s in (1, -1):
                rotmap = {r: (r * s + k) % 4 for r in range(4)}
                rooms = place(pool, placed, W, H, WH, rotmap)
                ok, misses = grade(rooms, pts, pool["cell"], pool["heightScale"])
                results.append((ok, s, k, rotmap, misses))
        results.sort(key=lambda t: -t[0])
        for ok, s, k, rotmap, misses in results[:4]:
            print("  rotmap %s: %d/%d = %.1f%%  misses %s" % (rotmap, ok, len(pts), 100.0 * ok / len(pts), misses[:4]))
        # per-room breakdown for the best
        ok, s, k, rotmap, _ = results[0]
        rooms = place(pool, placed, W, H, WH, rotmap)
        per = collections.Counter(); tot = 0
        for (x, y, z) in pts:
            for rm in rooms:
                h = floor_height(rm, x, z, pool["cell"], pool["heightScale"])
                if h is not None and abs(h - y) <= 1.0: per[(rm["name"], rm["floor"], rm["prot"])] += 1; break
        print("  points per room:", per.most_common())

main()
