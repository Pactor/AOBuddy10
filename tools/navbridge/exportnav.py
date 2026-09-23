"""Write every playfield's navigation data into <out>/<playfield id>/.

    python exportnav.py [<cd_image/data/db>] [<collision .tri dir>] [<out dir>] [pf,pf,...]

Sources (all read straight from ResourceDatabase.dat, nothing running):
  1000001  playfield record   - dungeon room list (version 10, room flags 0x05xx)
  1000009  ground data        - 'CHGA' outdoor heightfield, 'GNDA' dungeon template atlas
  1000013  collision surfaces - via navbridge.exe's .tri export (the client's own reader)

Per playfield folder:
  info.json       kind, name, sizes, the files present, and the verification result when
                  the owner has walked the zone (Build/Plugins/AOBuddy/nav/<pf>.json)
  ground.bin      outdoor heightfield + tile ids + building map (see README.md)
  rooms.json      dungeon rooms with placement and per-cell tile id / height / flags
  collision.bin   near-horizontal collision triangles, quantised (see README.md)
"""
import io, json, math, os, struct, sys, zlib
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from rdb import Rdb
from meshdecode import DB, Record
from playfield import parse_rooms
from tilemap import pngs

HERE = os.path.dirname(os.path.abspath(__file__))
P = 64                       # outdoor patch size in cells
WALK_NY = 0.5                # keep collision triangles whose |normal.y| exceeds this (<= 60 deg slope)


# ----------------------------------------------------------------------------- outdoor ground

def read_ground(rdb, gid):
    """Decode a 'CHGA' record: (w_samples, h_samples, cell, hscale, heights u16[h,w] (256 = one hscale unit),
    tiles u16[h-1,w-1], building u8[h-1,w-1], source bit depth)."""
    _, b = rdb.read(1000009, gid)
    if b[:4] != b"CHGA":
        return None
    i = b.find(b"AnarchyGroundDataDB_t")
    r = Record(b[i - 21:])
    g = r.nodes[0]
    w, h = g.get("map_width"), g.get("map_height")
    cell, hs = struct.unpack_from("<2f", b, 16)

    def blobs(name):
        raw = [e for e in g.entries if e.name == name][0].raw
        out, p = [], 0
        while p + 4 <= len(raw):
            n = struct.unpack_from("<I", raw, p)[0]; p += 4
            out.append(raw[p:p + n]); p += n
        return out

    hm = [zlib.decompress(x) for x in blobs("heightmap_compressed_data")]
    tm = [zlib.decompress(x) for x in blobs("tilemap_compressed_data")]
    bm = [zlib.decompress(x) for x in blobs("buildingmap_compressed_data")]
    # patch size and sample width come from the block itself: (P+1)^2 bytes, or twice that for
    # 16-bit heights (Patch_t::DecompressHeightMap has both paths)
    n0 = len(hm[0])
    P, bits = next((p, b) for p in (8, 16, 32, 64, 128, 256) for b in (8, 16) if (p + 1) * (p + 1) * (b // 8) == n0)
    nx, nz = (w - 1) // P, (h - 1) // P
    if len(hm) != nx * nz:
        raise ValueError("ground %d: %d patches for a %dx%d grid (P=%d)" % (gid, len(hm), nx, nz, P))
    heights = np.zeros((h, w), np.uint16)
    tiles = np.zeros((h - 1, w - 1), np.uint16)
    building = np.zeros((h - 1, w - 1), np.uint8)
    for bz in range(nz):
        for bx in range(nx):
            k = bz * nx + bx
            if bits == 8:
                a = np.frombuffer(hm[k], dtype=np.uint8).astype(np.int64).reshape(P + 1, P + 1)
                # Patch_t::DecompressHeightMap: cumulative sum down each column, then along
                # each row, modulo 256, then the byte becomes the high byte of a u16.
                a = ((np.cumsum(np.cumsum(a, axis=0), axis=1) % 256) * 256).astype(np.uint16)
            else:
                a = np.frombuffer(hm[k], dtype="<u2").astype(np.int64).reshape(P + 1, P + 1)
                a = (np.cumsum(np.cumsum(a, axis=0), axis=1) % 65536).astype(np.uint16)
            heights[bz * P:bz * P + P + 1, bx * P:bx * P + P + 1] = a
            if k < len(tm) and len(tm[k]) == P * P * 2:
                tiles[bz * P:bz * P + P, bx * P:bx * P + P] = np.frombuffer(tm[k], dtype="<u2").reshape(P, P)
            if k < len(bm) and len(bm[k]) == P * P // 2:
                nib = np.frombuffer(bm[k], dtype=np.uint8)
                pair = np.stack([nib & 15, nib >> 4], axis=1).reshape(P, P)   # nibble order OPEN
                building[bz * P:bz * P + P, bx * P:bx * P + P] = pair
    # 8-bit maps: height = byte * hscale (verified on walked zones), and the byte sits in the high
    # half of the u16, so the per-u16 scale is hscale/256. 16-bit maps store hscale already
    # divided down (e.g. 1/512 for a 128 m range): height = u16 * hscale. Unverified: no walked data.
    hs = hs / 256.0 if bits == 8 else hs
    return w, h, cell, hs, heights, tiles, building, bits


def write_ground(path, w, h, cell, hs, heights, tiles, building, bits):
    body = heights.astype("<u2").tobytes() + tiles.astype("<u2").tobytes() + building.tobytes()
    with open(path, "wb") as f:
        f.write(b"AONG")
        f.write(struct.pack("<iiiffi", 2, w, h, cell, hs, bits))
        z = zlib.compress(body, 9)
        f.write(struct.pack("<ii", len(body), len(z)))
        f.write(z)


def ground_height(heights, cell, hs, x, z):
    """Bilinear height at world (x, z); None outside the map."""
    fx, fz = x / cell, z / cell
    h, w = heights.shape
    ix, iz = int(math.floor(fx)), int(math.floor(fz))
    if ix < 0 or iz < 0 or ix + 1 >= w or iz + 1 >= h:
        return None
    tx, tz = fx - ix, fz - iz
    v = (heights[iz, ix] * (1 - tx) * (1 - tz) + heights[iz, ix + 1] * tx * (1 - tz)
         + heights[iz + 1, ix] * (1 - tx) * tz + heights[iz + 1, ix + 1] * tx * tz)
    return float(v) * hs


# ----------------------------------------------------------------------------- dungeon rooms

def read_dungeon(rdb, pf, blob):
    hdr, rooms, end = parse_rooms(blob)
    _, tb = rdb.read(1000009, hdr["tilemap"])
    if tb[:4] != b"GNDA":
        raise ValueError("pf %d tilemap %d is not GNDA" % (pf, hdr["tilemap"]))
    tw, th = struct.unpack_from("<2H", tb, 12)
    cell, hs = struct.unpack_from("<2f", tb, 16)
    ims = [np.array(im) for _, im in pngs(tb)]     # arrays are [z, x]
    out_rooms = []
    for rm in rooms:
        x1, z1, x2, z2 = rm.rect
        tile = ims[0][z1:z2 + 1, x1:x2 + 1]
        hgt = ims[1][z1:z2 + 1, x1:x2 + 1]
        flg = ims[3][z1:z2 + 1, x1:x2 + 1] if len(ims) > 3 else np.zeros_like(tile)
        floor = tile != 0
        kmin = int(hgt[floor].min()) if floor.any() else 0
        out_rooms.append(dict(
            index=rm.index, name=rm.name, flags=rm.flags, rot=rm.rot, rect=list(rm.rect),
            pos=[round(v, 3) for v in rm.pos], heightBase=kmin,
            doors=[list(d) for d in rm.doors],
            polys=[dict(verts=[[round(v, 3) for v in vv] for vv in verts], tris=[list(t) for t in tris])
                   for verts, tris, tail in rm.polys],
            tile=tile.astype(int).tolist(), height=hgt.astype(int).tolist(), flags3=flg.astype(int).tolist()))
    return dict(playfield=pf, name=hdr["name"], tilemap=hdr["tilemap"], cell=cell, heightScale=hs,
                atlas=[tw, th], rooms=out_rooms)


def room_floor_height(d, room, x, z):
    """World floor height of the tile under world (x, z) for one room, or None."""
    cell, hs = d["cell"], d["heightScale"]
    dx, dz = x - room["pos"][0], z - room["pos"][2]
    for _ in range((-room["rot"]) % 4):
        dx, dz = dz, -dx
    x1, z1, x2, z2 = room["rect"]
    a = int(math.floor((x1 + x2 + 1) / 2.0 + dx / cell))
    b = int(math.floor((z1 + z2 + 1) / 2.0 + dz / cell))
    if not (x1 <= a <= x2 and z1 <= b <= z2):
        return None
    t = room["tile"][b - z1][a - x1]
    if t == 0:
        return None
    return room["pos"][1] + (room["height"][b - z1][a - x1] - room["heightBase"]) * hs


# ----------------------------------------------------------------------------- collision

def read_tri(path):
    f = open(path, "rb").read()
    if f[:8] != b"AOTRI1\0\0":
        raise ValueError(path)
    pf, n = struct.unpack_from("<2i", f, 8)
    p = 16
    recs = []
    for _ in range(n):
        inst, vc, tp, lt, li = struct.unpack_from("<5i", f, p); p += 20
        v = np.frombuffer(f, dtype="<f4", count=vc * 3, offset=p).reshape(-1, 3, 3); p += vc * 12
        recs.append((inst, tp, lt, li, v))
    return pf, recs


def walkable(tris):
    if len(tris) == 0:
        return tris
    n = np.cross(tris[:, 1] - tris[:, 0], tris[:, 2] - tris[:, 0])
    l = np.linalg.norm(n, axis=1)
    ny = np.abs(n[:, 1]) / np.maximum(l, 1e-9)
    return tris[(l > 1e-9) & (ny > WALK_NY)]


def write_collision(path, recs):
    """AOCL: near-horizontal triangles, int16 centimetres relative to a per-chunk origin."""
    chunks = []
    kept = 0
    for inst, tp, lt, li, v in recs:
        w = walkable(v)
        if len(w) == 0:
            continue
        kept += len(w)
        for s in range(0, len(w), 2048):
            c = w[s:s + 2048]
            origin = c.reshape(-1, 3).min(axis=0)
            q = np.round((c - origin) * 100.0)
            if q.max() > 32767:                        # never seen; split finer if it happens
                for s2 in range(0, len(c), 64):
                    c2 = c[s2:s2 + 64]; o2 = c2.reshape(-1, 3).min(axis=0)
                    chunks.append((inst, tp, lt, li, o2, np.round((c2 - o2) * 100.0).astype("<i2")))
            else:
                chunks.append((inst, tp, lt, li, origin, q.astype("<i2")))
    body = io.BytesIO()
    for inst, tp, lt, li, origin, q in chunks:
        body.write(struct.pack("<iiiifffi", inst, tp, lt, li, float(origin[0]), float(origin[1]), float(origin[2]), len(q)))
        body.write(q.tobytes())
    raw = body.getvalue()
    z = zlib.compress(raw, 9)
    with open(path, "wb") as f:
        f.write(b"AOCL")
        f.write(struct.pack("<iiii", 1, len(chunks), len(raw), len(z)))
        f.write(z)
    return kept, len(chunks)


def collision_heights(recs, x, z, cache):
    """All triangle heights under world (x, z), from the unfiltered records (bucketed)."""
    key = (int(math.floor(x / 8)), int(math.floor(z / 8)))
    if "b" not in cache:
        buckets = {}
        for inst, tp, lt, li, v in recs:
            if len(v) == 0:
                continue
            mnx = np.floor(v[:, :, 0].min(1) / 8).astype(int); mxx = np.floor(v[:, :, 0].max(1) / 8).astype(int)
            mnz = np.floor(v[:, :, 2].min(1) / 8).astype(int); mxz = np.floor(v[:, :, 2].max(1) / 8).astype(int)
            for i in range(len(v)):
                for a in range(mnx[i], mxx[i] + 1):
                    for c in range(mnz[i], mxz[i] + 1):
                        buckets.setdefault((a, c), []).append(v[i])
        cache["b"] = buckets
    out = []
    for (p0, p1, p2) in cache["b"].get(key, []):
        d = (p1[0] - p0[0]) * (p2[2] - p0[2]) - (p2[0] - p0[0]) * (p1[2] - p0[2])
        if abs(d) < 1e-9:
            continue
        u = ((x - p0[0]) * (p2[2] - p0[2]) - (p2[0] - p0[0]) * (z - p0[2])) / d
        v = ((p1[0] - p0[0]) * (z - p0[2]) - (x - p0[0]) * (p1[2] - p0[2])) / d
        if u >= -1e-6 and v >= -1e-6 and u + v <= 1 + 1e-6:
            out.append(float(p0[1] + u * (p1[1] - p0[1]) + v * (p2[1] - p0[1])))
    return out


# ----------------------------------------------------------------------------- verification

def verify(pf, kind, ground, dungeon, recs, navdir, tol=1.0):
    navf = os.path.join(navdir, "%d.json" % pf)
    if not os.path.exists(navf):
        return None
    pts = [tuple(p) for seg in json.load(open(navf))["Segments"] for p in seg]
    if not pts:
        return None
    cache = {}
    n_floor = n_coll = n_none = 0
    worst = []
    for (x, y, z) in pts:
        ok = False
        if kind == "outdoor" and ground is not None:
            w, h, cell, hs, heights, tiles, building, bits = ground
            v = ground_height(heights, cell, hs, x, z)
            ok = v is not None and abs(v - y) <= tol
        elif kind == "dungeon" and dungeon is not None:
            for room in dungeon["rooms"]:
                v = room_floor_height(dungeon, room, x, z)
                if v is not None and abs(v - y) <= tol:
                    ok = True; break
        if ok:
            n_floor += 1; continue
        if recs is not None and any(abs(v - y) <= tol for v in collision_heights(recs, x, z, cache)):
            n_coll += 1; continue
        n_none += 1
        if len(worst) < 10:
            worst.append([round(x, 1), round(y, 2), round(z, 1)])
    n = len(pts)
    return dict(walkedPoints=n, toleranceM=tol, explainedByFloor=n_floor, explainedByCollision=n_coll,
                unexplained=n_none, explainedPct=round(100.0 * (n - n_none) / n, 1), unexplainedSamples=worst)


# ----------------------------------------------------------------------------- main

def main():
    db = sys.argv[1] if len(sys.argv) > 1 else DB
    tridir = sys.argv[2] if len(sys.argv) > 2 else os.path.join(HERE, "out_all")
    out = sys.argv[3] if len(sys.argv) > 3 else os.path.join(HERE, "..", "..", "AOBuddy", "GameData", "Nav")
    navdir = os.path.join(HERE, "..", "..", "Build", "Plugins", "AOBuddy", "nav")
    rdb = Rdb(db)
    os.makedirs(out, exist_ok=True)
    pfs = sorted(rdb.off[1000001])
    if len(sys.argv) > 4:                      # optional: comma-separated playfield ids
        only = set(int(v) for v in sys.argv[4].split(","))
        pfs = [p for p in pfs if p in only]
    index = []
    for pf in pfs:
        _, blob = rdb.read(1000001, pf)
        version = struct.unpack_from("<i", blob, 0)[0]
        name = blob[8:40].split(b"\0")[0].decode("latin1")
        kind = "none"
        ground = dungeon = None
        info = dict(playfield=pf, name=name, playfieldVersion=version, files=[])
        folder = os.path.join(out, str(pf))
        try:
            if version == 10 and len(blob) > 0x62 and (struct.unpack_from("<H", blob, 0x60)[0] & 0xFF00) == 0x0500:
                dungeon = read_dungeon(rdb, pf, blob)
                kind = "dungeon"
            elif pf in rdb.off[1000009]:
                ground = read_ground(rdb, pf)
                if ground is not None:
                    kind = "outdoor"
        except Exception as e:
            info["error"] = "%s: %s" % (type(e).__name__, e)
        tri = os.path.join(tridir, "%d.tri" % pf)
        recs = None
        if os.path.exists(tri):
            _, recs = read_tri(tri)
        if kind == "none" and recs is None:
            continue
        os.makedirs(folder, exist_ok=True)
        info["kind"] = kind
        if kind == "outdoor":
            w, h, cell, hs, heights, tiles, building, bits = ground
            write_ground(os.path.join(folder, "ground.bin"), w, h, cell, hs, heights, tiles, building, bits)
            info["files"].append("ground.bin")
            info["ground"] = dict(samplesX=w, samplesZ=h, cell=cell, heightScale=hs, sourceHeightBits=bits,
                                  worldSize=[(w - 1) * cell, (h - 1) * cell],
                                  heightRange=[round(float(heights.min()) * hs, 4), round(float(heights.max()) * hs, 4)],
                                  heightVerifiable=(bits == 8))
        elif kind == "dungeon":
            json.dump(dungeon, open(os.path.join(folder, "rooms.json"), "w"), separators=(",", ":"))
            info["files"].append("rooms.json")
            info["dungeon"] = dict(tilemap=dungeon["tilemap"], rooms=len(dungeon["rooms"]), cell=dungeon["cell"],
                                   heightScale=dungeon["heightScale"])
        if recs is not None:
            kept, chunks = write_collision(os.path.join(folder, "collision.bin"), recs)
            info["files"].append("collision.bin")
            info["collision"] = dict(records=len(recs), trianglesTotal=int(sum(len(v) for *_, v in recs)),
                                     trianglesWalkable=int(kept), chunks=chunks)
        v = verify(pf, kind, ground, dungeon, recs, navdir)
        if v is not None:
            info["verification"] = v
        json.dump(info, open(os.path.join(folder, "info.json"), "w"), indent=1)
        index.append(info)
        print("pf %-6d %-9s %-32s %s" % (pf, kind, name[:32], ("verified %.1f%%" % v["explainedPct"]) if v else ""))
    json.dump(index, open(os.path.join(out, "index.json"), "w"), indent=1)
    print("wrote %d playfields to %s" % (len(index), out))


if __name__ == "__main__":
    main()
