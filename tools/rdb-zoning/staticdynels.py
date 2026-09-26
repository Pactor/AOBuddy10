#!/usr/bin/env python3
"""
Regenerates StaticDynelData.bin from the client's resource database, with each dynel's
rotation (format v2).

The v1 bin shipped with the AOSharp SDK stores only (instance, position), so StaticDynel
was built with Quaternion.Identity and every terminal in the game "faced" (0,0,1)
(Newland City's east-facing mission terminal read exactly that, 2026-09-26). The rotation
was found in the statel record all along.

Source record: RDB 1000026 (Statel), one per playfield, the same layout extract.py walks:
  int entryCount, then entries of [int len][payload len bytes]:
    payload +0 identity type, +4 instance, +24 x,y,z, +36 rotation as (w,x,y,z),
    +56 templateId, +60 event-blob length (events at +64, not used here)
The rotation's (w,x,y,z) maps to AOSharp Quaternion's (x,y,z,w) fields; its Forward then
reads the facing directly (validated on Newland City: yaw 90 = east, yaw 224 = the
south-facing pair, 2026-09-26).

v2 bin layout (StaticDynelData.Deserialize reads this and falls back to v1 without the
magic):
  b'ASDD', int 2, int playfieldCount;
  per playfield: int pf, int typeCount;
  per type: int identityType, int templateCount;
  per template: int templateId, int dynelCount;
  per dynel: uint instance, 3f position, 4f rotation (x, y, z, w)

Run:  python staticdynels.py "<AO install>\\cd_image\\data\\db" [out.bin]
      (out defaults to AOSharp.Clientless/GameData/StaticDynelData.bin by the repo root)
Prints a parity report against the existing v1 bin before writing.
"""

import os
import struct
import sys

from extract import Rdb, T_STATEL

MAGIC = b"ASDD"


def parse_old(path):
    """v1 bin: the structure StaticDynelData.cs reads, minus rotations."""
    old = {}
    with open(path, "rb") as f:
        def i32():
            return struct.unpack("<i", f.read(4))[0]

        for _ in range(i32()):
            pf = i32()
            types = {}
            for _ in range(i32()):
                t = i32()
                tmpls = {}
                for _ in range(i32()):
                    tmpl = i32()
                    dynels = []
                    for _ in range(i32()):
                        inst = struct.unpack("<I", f.read(4))[0]
                        pos = struct.unpack("<3f", f.read(12))
                        dynels.append((inst, pos))
                    tmpls[tmpl] = dynels
                types[t] = tmpls
            old[pf] = types
    return old


def parse_statels(rdb):
    """Every statel entry as {pf: {(type, template): [(instance, pos, quat), ...]}}."""
    out = {}
    for pf in rdb.ids(T_STATEL):
        d = rdb.get(T_STATEL, pf)
        n, p = struct.unpack_from("<i", d, 0)[0], 4
        rows = {}
        for _ in range(n):
            ln = struct.unpack_from("<i", d, p)[0]
            b = d[p + 4:p + 4 + ln]
            p += 4 + ln
            if len(b) < 60:
                continue  # no template field: nothing for the loader to build it from
            itype, inst = struct.unpack_from("<ii", b, 0)
            pos = struct.unpack_from("<3f", b, 24)
            w, qx, qy, qz = struct.unpack_from("<4f", b, 36)
            tmpl = struct.unpack_from("<i", b, 56)[0]
            rows.setdefault((itype, tmpl), []).append((inst, pos, (qx, qy, qz, w)))
        out[pf] = rows
    return out


def parity_report(old, new):
    """Compare (pf, type, template, instance) sets and positions with the v1 bin."""
    old_keys = {(pf, t, tmpl, inst)
                for pf, types in old.items()
                for t, tmpls in types.items()
                for tmpl, ds in tmpls.items()
                for inst, _ in ds}
    new_keys = {(pf, t, tmpl, inst & 0xFFFFFFFF)
                for pf, rows in new.items()
                for (t, tmpl), ds in rows.items()
                for inst, _, _ in ds}
    old_pos = {(pf, t, tmpl, inst): tuple(round(v, 2) for v in pos)
               for pf, types in old.items()
               for t, tmpls in types.items()
               for tmpl, ds in tmpls.items()
               for inst, pos in ds}
    new_pos = {(pf, t, tmpl, inst & 0xFFFFFFFF): tuple(round(v, 2) for v in pos)
               for pf, rows in new.items()
               for (t, tmpl), ds in rows.items()
               for inst, pos, _ in ds}
    moved = [k for k in old_keys & new_keys if old_pos[k] != new_pos[k]]
    print(f"v1 bin: {len(old_keys)} dynels in {len(old)} playfields")
    print(f"rdb:    {len(new_keys)} dynels in {len(new)} playfields")
    print(f"only in v1: {len(old_keys - new_keys)}  only in rdb: {len(new_keys - old_keys)}  moved: {len(moved)}")
    for label, keys in (("v1-only", old_keys - new_keys), ("rdb-only", new_keys - old_keys)):
        for k in sorted(keys)[:5]:
            print(f"  {label}: pf {k[0]} type 0x{k[1]:X} tmpl {k[2]} inst {k[3]:08X}")
    for k in sorted(moved)[:5]:
        print(f"  moved: pf {k[0]} type 0x{k[1]:X} inst {k[3]:08X}: {old_pos[k]} -> {new_pos[k]}")


def write_v2(path, new):
    with open(path, "wb") as f:
        f.write(MAGIC)
        f.write(struct.pack("<i", 2))
        f.write(struct.pack("<i", len(new)))
        for pf in sorted(new):
            # regroup (type, template) rows into the bin's type -> template -> dynels nesting;
            # a type written twice would hit a duplicate-key Add in the C# reader
            by_type = {}
            for (t, tmpl), ds in new[pf].items():
                by_type.setdefault(t, {})[tmpl] = ds
            f.write(struct.pack("<i", pf))
            f.write(struct.pack("<i", len(by_type)))
            for t in sorted(by_type):
                tmpls = by_type[t]
                f.write(struct.pack("<i", t))
                f.write(struct.pack("<i", len(tmpls)))
                for tmpl in sorted(tmpls):
                    ds = tmpls[tmpl]
                    f.write(struct.pack("<i", tmpl))
                    f.write(struct.pack("<i", len(ds)))
                    for inst, pos, quat in ds:
                        f.write(struct.pack("<I", inst & 0xFFFFFFFF))   # parsed signed; 0xC0000320 etc.
                        f.write(struct.pack("<3f", *pos))
                        f.write(struct.pack("<4f", *quat))


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    here = os.path.dirname(os.path.abspath(__file__))
    default_out = os.path.join(here, "..", "..", "AOSharp.Clientless", "GameData", "StaticDynelData.bin")
    out = sys.argv[2] if len(sys.argv) > 2 else default_out

    rdb = Rdb(sys.argv[1])
    new = parse_statels(rdb)
    # parity always against the shipped bin, whatever out is
    if os.path.exists(default_out):
        try:
            with open(default_out, "rb") as f:
                if f.read(4) == MAGIC:
                    print("shipped bin is already v2; skipping the v1 parity report")
                else:
                    parity_report(parse_old(default_out), new)
        except struct.error:
            print("shipped bin unreadable as v1; writing without a parity report")
    write_v2(out, new)
    n = sum(len(ds) for rows in new.values() for ds in rows.values())
    print(f"wrote {n} dynels in {len(new)} playfields (v2, with rotations) to {os.path.normpath(out)}")


if __name__ == "__main__":
    main()