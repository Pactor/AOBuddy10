"""Turn a decoded 1010001/1010026 record into triangle geometry in the record's root frame.

Scene graph: RRefFrame_t / RTriMesh_t nodes carry anim_matrix (4x4 row-major, translation
in row 3, row-vector convention: world = local * M) and a chld list. RTriMesh_t.data points
at FAFTriMeshData_t, whose mesh list points at SimpleMesh objects; each SimpleMesh has a
TriList (u16 triangle indices) and an FVF vertex buffer whose first 12 bytes per vertex are
the position.
"""
import struct, sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from meshdecode import Record, load


def fvf_stride(fvf):
    pos = {0x2: 12, 0x4: 16, 0x6: 16, 0x8: 20, 0xA: 24, 0xC: 28, 0xE: 32}.get(fvf & 0xE, 0)
    s = pos
    if fvf & 0x10: s += 12
    if fvf & 0x20: s += 4
    if fvf & 0x40: s += 4
    if fvf & 0x80: s += 4
    for i in range((fvf >> 8) & 0xF):
        s += {0: 8, 1: 12, 2: 16, 3: 4}[(fvf >> (16 + i * 2)) & 3]
    return s


def mat_mul(a, b):
    return [[sum(a[i][k] * b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def xform(m, v):
    x, y, z = v
    return (x * m[0][0] + y * m[1][0] + z * m[2][0] + m[3][0],
            x * m[0][1] + y * m[1][1] + z * m[2][1] + m[3][1],
            x * m[0][2] + y * m[1][2] + z * m[2][2] + m[3][2])


IDENT = [[1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]]


def simplemesh_geometry(rec, sm):
    """(positions, triangles) in mesh-local space for one SimpleMesh object."""
    desc = sm.get("vb_desc")
    verts = sm.get("vertices")
    tl = rec.ref(sm.get("trilist"))
    if desc is None or verts is None or tl is None:
        return [], []
    _, _, fvf, n = struct.unpack("<4i", desc)
    st = fvf_stride(fvf)
    pos = [struct.unpack_from("<3f", verts, i * st) for i in range(n)]
    idx = tl.get("triangles") or b""
    tris = [struct.unpack_from("<3H", idx, i * 6) for i in range(len(idx) // 6)]
    return pos, tris


def walk(rec, node, parent_m, out, depth=0):
    """Depth-first over the node graph, accumulating anim_matrix. Appends
    (node, world_matrix, positions, triangles) per SimpleMesh."""
    m = node.get("anim_matrix")
    m = mat_mul([list(r) for r in m], parent_m) if m is not None else parent_m
    data = rec.ref(node.get("data")) if node.cls == "RTriMesh_t" else None
    if data is not None:
        meshes = data.get("mesh")
        if isinstance(meshes, int):
            meshes = [meshes]
        for mi in meshes or []:
            sm = rec.ref(mi)
            if sm is None or sm.cls != "SimpleMesh":
                continue
            pos, tris = simplemesh_geometry(rec, sm)
            out.append((node, data, m, pos, tris))
    ch = node.get("chld")
    if ch is None:
        ch = []
    elif isinstance(ch, int):
        ch = [ch]
    for c in ch:
        cn = rec.ref(c)
        if cn is not None and depth < 64:
            walk(rec, cn, m, out, depth + 1)


def geometry(rec):
    """World-space triangle soup for a record: list of (x,y,z) triples per triangle,
    plus the per-mesh detail list."""
    out = []
    if rec.root is not None:
        walk(rec, rec.root, IDENT, out)
    tris = []
    for node, data, m, pos, tl in out:
        wp = [xform(m, p) for p in pos]
        for a, b, c in tl:
            if a < len(wp) and b < len(wp) and c < len(wp):
                tris.append((wp[a], wp[b], wp[c]))
    return tris, out


def bounds(tris):
    xs = [p[0] for t in tris for p in t]; ys = [p[1] for t in tris for p in t]; zs = [p[2] for t in tris for p in t]
    if not xs:
        return None
    return (min(xs), min(ys), min(zs)), (max(xs), max(ys), max(zs))


if __name__ == "__main__":
    rec = load(int(sys.argv[1]), int(sys.argv[2]))
    tris, detail = geometry(rec)
    print("root=%s nodes=%d meshes=%d triangles=%d" % (rec.root, len(rec.nodes), len(detail), len(tris)))
    for node, data, m, pos, tl in detail:
        b = bounds([(pos[a], pos[b_], pos[c]) for a, b_, c in tl if max(a, b_, c) < len(pos)])
        bv = rec.ref(data.get("bvol"))
        print("  %-24s verts=%-6d tris=%-6d local %s  bvol min=%s max=%s  T=%s"
              % (data.get("name"), len(pos), len(tl), b,
                 tuple(round(v, 2) for v in (bv.get("min_pos") or ())) if bv else None,
                 tuple(round(v, 2) for v in (bv.get("max_pos") or ())) if bv else None,
                 tuple(round(v, 2) for v in m[3][:3])))
    print("world bounds:", bounds(tris))
