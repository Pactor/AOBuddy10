"""Validate exported collision meshes against the bot's own recorded walk data.

The .tri files come from the client's own collision reader. The nav JSONs are points the
owner actually walked. If the geometry is right, standing on a walked point and casting
straight down must land on a triangle within a few centimetres of the recorded Y.

Usage: python validate.py [out_dir] [nav_dir]
"""
import json
import os
import struct
import sys
from collections import defaultdict

OUT = sys.argv[1] if len(sys.argv) > 1 else "out"
NAV = sys.argv[2] if len(sys.argv) > 2 else r"E:\Funcom\AOBuddy10\Build\Plugins\AOBuddy\nav"
CELL = 8.0          # spatial hash cell, world units
MAX_DROP = 3.0      # how far below a walked point we still call it "the floor"
MAX_RISE = 2.5      # head height: a triangle this far above the feet is a ceiling, not a floor


def load_tri(path):
    """Yield (instance, teleport_dest, [(x,y,z), ...]) per record."""
    with open(path, "rb") as f:
        magic = f.read(8)
        if magic[:6] != b"AOTRI1":
            raise ValueError("bad magic in %s" % path)
        pf, nrec = struct.unpack("<ii", f.read(8))
        for _ in range(nrec):
            inst, vc, dest, ltype, linst = struct.unpack("<5i", f.read(20))
            raw = f.read(vc * 12)
            yield inst, dest, raw, vc
        return


def build_index(path):
    """Spatial hash of triangles by XZ cell. Returns (grid, tris) where tris is a flat
    list of 9 floats per triangle."""
    grid = defaultdict(list)
    tris = []
    teleports = 0
    for inst, dest, raw, vc in load_tri(path):
        if dest:
            teleports += 1
        v = struct.unpack("<%df" % (vc * 3), raw)
        for t in range(vc // 3):
            b = t * 9
            tri = v[b:b + 9]
            if len(tri) < 9:
                break
            idx = len(tris)
            tris.append(tri)
            xs = (tri[0], tri[3], tri[6])
            zs = (tri[2], tri[5], tri[8])
            for cx in range(int(min(xs) // CELL), int(max(xs) // CELL) + 1):
                for cz in range(int(min(zs) // CELL), int(max(zs) // CELL) + 1):
                    grid[(cx, cz)].append(idx)
    return grid, tris, teleports


def height_at(grid, tris, x, z):
    """Every triangle surface directly under/over (x, z), as a list of Y values."""
    hits = []
    for idx in grid.get((int(x // CELL), int(z // CELL)), ()):
        ax, ay, az, bx, by, bz, cx, cy, cz = tris[idx]
        # barycentric test in the XZ plane
        d = (bz - cz) * (ax - cx) + (cx - bx) * (az - cz)
        if abs(d) < 1e-9:
            continue
        l1 = ((bz - cz) * (x - cx) + (cx - bx) * (z - cz)) / d
        l2 = ((cz - az) * (x - cx) + (ax - cx) * (z - cz)) / d
        l3 = 1.0 - l1 - l2
        if l1 < -1e-4 or l2 < -1e-4 or l3 < -1e-4:
            continue
        hits.append(l1 * ay + l2 * by + l3 * cy)
    return hits


def main():
    print("%-6s %8s %9s %7s  %s" % ("pf", "tris", "navpts", "onfloor", "|dY| median / p90 / worst"))
    for name in sorted(os.listdir(OUT)):
        if not name.endswith(".tri"):
            continue
        pf = int(name[:-4])
        navpath = os.path.join(NAV, "%d.json" % pf)
        if not os.path.exists(navpath):
            continue
        grid, tris, teleports = build_index(os.path.join(OUT, name))
        zone = json.load(open(navpath))
        pts = [p for seg in zone.get("Segments") or [] for p in seg]
        if not pts:
            continue
        errs = []
        missed = 0
        for x, y, z in pts:
            hits = height_at(grid, tris, x, z)
            # the floor is the highest surface at or just below the feet
            below = [h for h in hits if h <= y + MAX_RISE]
            if not below:
                missed += 1
                continue
            floor = max(below)
            if y - floor > MAX_DROP:
                missed += 1
                continue
            errs.append(abs(y - floor))
        errs.sort()
        if errs:
            med = errs[len(errs) // 2]
            p90 = errs[int(len(errs) * 0.9)]
            worst = errs[-1]
            stat = "%.2f / %.2f / %.2f" % (med, p90, worst)
        else:
            stat = "n/a"
        print("%-6d %8d %9d %6.1f%%  %s   (%d teleport records)"
              % (pf, len(tris), len(pts), 100.0 * len(errs) / len(pts), stat, teleports))


if __name__ == "__main__":
    main()
