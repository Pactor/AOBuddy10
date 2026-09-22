#!/usr/bin/env python3
"""
Pulls every playfield's ways out of the client's resource database into AOBuddy/GameData/Zoning.json.

Run:  python extract.py "<AO install>\\cd_image\\data\\db" [out.json]
      (out defaults to AOBuddy/GameData/Zoning.json next to this tools/ folder)

Python 3 standard library only. The record layouts are the ones CellAO/OmniCell's Extractor
Serializer reads; the teleport arguments were matched against known whompahs and grid terminals.

Records used (type ids, keyed by playfield id):
  1000001 Playfield  name at +8; at the end a table of arrival lines, index -> start/end point
  1000021 Wall       wall polylines; a vertex tagged (destPf, destIdx) makes the segment
                     previous vertex -> this vertex a zone line into destPf, arriving on destPf's
                     arrival line destIdx (which is the matching zone line on the far side)
  1000026 Statel     static objects (doors, lifts, whompahs, grid terminals) with their event
                     scripts; the teleport functions in them name where using the object sends you
                     and carry the requirements for using it. Mission doors are statels of type
                     ACGEntrance (0xDAC6) with no teleport: the server makes the instance and the
                     mission key opens the door

Requirements are the function's criteria as stored, in postfix order: [stat, operator, value],
where operator 3/4/42 is Or/And/Not joining the terms before it (stat and value 0). reqText
spells them out with the names from AOSharp.Common's Stat and UseCriteriaOperator enums.

Coordinates are AO's (x, y, z) with y up.
"""

import json
import os
import re
import struct
import sys

T_PLAYFIELD, T_WALL, T_STATEL = 1000001, 1000021, 1000026

# Teleport function ids (OmniCell.Enums.FunctionType) and how their big-endian int args read.
F_TELEPORT, F_LINE, F_PROXY, F_PROXY2, F_PROXY_PET = 53016, 53059, 53082, 53083, 53165
TELEPORTS = {F_TELEPORT: "teleport", F_LINE: "line", F_PROXY: "proxy", F_PROXY2: "proxy", F_PROXY_PET: "proxy"}

ACG_ENTRANCE = 0xDAC6
OP_OR, OP_AND, OP_NOT = 3, 4, 42


def load_enum(path):
    names = {}
    if os.path.exists(path):
        for m in re.finditer(r"^\s*(\w+)\s*=\s*(-?(?:0x[0-9A-Fa-f]+|\d+))\s*,?", open(path, encoding="utf-8-sig").read(), re.M):
            names.setdefault(int(m.group(2), 0), m.group(1))
    return names


GAMEDATA = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "AOSharp.Common", "GameData")
STATS = load_enum(os.path.join(GAMEDATA, "Stat.cs"))
OPS = load_enum(os.path.join(GAMEDATA, "UseCriteriaOperator.cs"))


def req_text(reqs):
    words = []
    for stat, op, value in reqs:
        if op in (OP_OR, OP_AND, OP_NOT):
            words.append(OPS.get(op, str(op)))
        else:
            words.append(f"{STATS.get(stat, stat)} {OPS.get(op, f'op{op}')} {value}")
    return " ".join(words)


class Rdb:
    def __init__(self, path):
        idx = open(os.path.join(path, "ResourceDatabase.idx"), "rb").read()
        u32 = lambda o: struct.unpack_from("<I", idx, o)[0]
        self.block_offset, self.dat_size = u32(12), u32(184)
        names = sorted(f for f in os.listdir(path) if re.match(r"ResourceDatabase\.dat(\.\d{3})?$", f, re.I))
        self.dats = [open(os.path.join(path, f), "rb") for f in names]
        self.records = {}
        # Leaf blocks form a chain from the pointer at +72: [next][?][count:i16][18 bytes][entries].
        # The block whose next is 0 is the end marker. Entry: offset hi/lo (LE), type and id (BE).
        block = u32(72)
        while u32(block):
            count = struct.unpack_from("<h", idx, block + 8)[0]
            p = block + 28
            for _ in range(count):
                hi, lo = struct.unpack_from("<II", idx, p)
                rtype, rid = struct.unpack_from(">ii", idx, p + 8)
                self.records.setdefault(rtype, {})[rid] = (hi << 32) | lo
                p += 16
            block = u32(block)

    def _read(self, pos, n):
        df = pos // self.dat_size
        if df > 0:
            pos -= (self.dat_size - self.block_offset) * df
        f = self.dats[df]
        f.seek(pos)
        b = f.read(n)
        if len(b) < n:  # a record straddling two .dat files carries on past the next file's header
            f = self.dats[df + 1]
            f.seek(self.block_offset)
            b += f.read(n - len(b))
        return b

    def ids(self, rtype):
        return sorted(self.records.get(rtype, {}))

    def get(self, rtype, rid):
        pos = self.records[rtype][rid]
        head = self._read(pos, 34)
        t, i, length = struct.unpack_from("<iiI", head, 10)
        if (t, i) != (rtype, rid):
            raise ValueError(f"record {rtype}:{rid} header says {t}:{i}")
        return self._read(pos + 34, length - 12)


def r3(x, y, z):
    return [round(x, 2), round(y, 2), round(z, 2)]


def parse_playfield(d):
    name = d[8:d.index(b"\0", 8)].decode("latin1")
    # The arrival table is the record's tail: count, then count 28-byte entries. Its start isn't
    # stored, so walk back from the end until the int there equals the entries passed over.
    pos, k = len(d) - 4, 0
    while struct.unpack_from("<i", d, pos)[0] != k:
        k += 1
        pos -= 28
    pos += 4
    arrivals = {}
    for _ in range(k):
        _, key, _, sx, sy, sz, ex, ey, ez = struct.unpack_from("<hBB6f", d, pos)
        arrivals[key] = [r3(sx, sy, sz), r3(ex, ey, ez)]
        pos += 28
    return name, arrivals


def parse_zone_lines(d):
    lines = []
    polys, p = struct.unpack_from("<i", d, 0)[0], 4
    for _ in range(polys):
        _, n = struct.unpack_from("<ii", d, p)
        p += 8
        verts = []
        for _ in range(n):
            verts.append(struct.unpack_from("<hBB3f", d, p))
            p += 16
        for i, (dest, idx, flags, x, y, z) in enumerate(verts):
            if dest:
                px, py, pz = verts[i - 1][3:]
                lines.append({"to": dest, "idx": idx, "flags": flags, "a": r3(px, py, pz), "b": r3(x, y, z)})
    return lines


def teleport_functions(ev):
    """The event blob's functions have no length prefix and the per-function argument layout isn't
    to hand, so find teleport functions by shape: id, 8 bytes, a small requirement count, the
    requirements (12 bytes each), tick count/interval/target/pad, then the args."""
    found = []
    for o in range(0, len(ev) - 48):
        fid = struct.unpack_from(">i", ev, o)[0]
        if fid not in TELEPORTS:
            continue
        reqs = struct.unpack_from(">i", ev, o + 12)[0]
        a = o + 16 + reqs * 12 + 16
        if not 0 <= reqs < 20 or a + 16 > len(ev):
            continue
        # Stored as stat, value, operator; kept as [stat, operator, value] to read left to right.
        crit = [struct.unpack_from(">3i", ev, o + 16 + 12 * i) for i in range(reqs)]
        found.append((fid, struct.unpack_from(">4i", ev, a), [[s, op, v] for s, v, op in crit]))
    return found


def parse_statels(d):
    teleports, entrances = [], []
    n, p = struct.unpack_from("<i", d, 0)[0], 4
    for _ in range(n):
        ln = struct.unpack_from("<i", d, p)[0]
        b = d[p + 4:p + 4 + ln]
        p += 4 + ln
        itype, inst = struct.unpack_from("<ii", b, 0)
        x, y, z = struct.unpack_from("<3f", b, 24)
        tmpl, evlen = struct.unpack_from("<ii", b, 56)
        if itype == ACG_ENTRANCE:
            entrances.append({"pos": r3(x, y, z), "id": [itype, inst], "template": tmpl})
        seen = set()
        for fid, a, reqs in teleport_functions(b[64:64 + evlen]):
            if fid == F_TELEPORT:     # x, y, z, playfield
                t = {"kind": "teleport", "to": a[3], "dest": [a[0], a[1], a[2]]}
            elif fid == F_LINE:       # _, idx << 16 | playfield, ...
                t = {"kind": "line", "to": a[1] & 0xFFFF, "idx": a[1] >> 16}
            else:                     # 51102 (proxy type), playfield, _, instance
                t = {"kind": "proxy", "to": a[1]}
            if reqs:                  # a grid exit can hold the same jump twice: by CL, or by side
                t["reqs"] = reqs
                t["reqText"] = req_text(reqs)
            key = json.dumps(t, sort_keys=True)
            if key in seen:           # one object often repeats the same jump across events
                continue
            seen.add(key)
            t.update({"pos": r3(x, y, z), "id": [itype, inst], "template": tmpl})
            teleports.append(t)
    return teleports, entrances


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    here = os.path.dirname(os.path.abspath(__file__))
    out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(here, "..", "..", "AOBuddy", "GameData", "Zoning.json")
    rdb = Rdb(sys.argv[1])
    walls = set(rdb.ids(T_WALL))
    statels = set(rdb.ids(T_STATEL))
    zoning = {}
    for pf in rdb.ids(T_PLAYFIELD):
        name, arrivals = parse_playfield(rdb.get(T_PLAYFIELD, pf))
        teleports, entrances = parse_statels(rdb.get(T_STATEL, pf)) if pf in statels else ([], [])
        zoning[str(pf)] = {
            "name": name,
            "zoneLines": parse_zone_lines(rdb.get(T_WALL, pf)) if pf in walls else [],
            "teleports": teleports,
            "missionEntrances": entrances,
            "arrivals": {str(k): v for k, v in sorted(arrivals.items())},
        }
    with open(out, "w", encoding="utf-8") as f:
        json.dump({"version": 2, "playfields": zoning}, f, separators=(",", ":"))
    count = lambda key: sum(len(v[key]) for v in zoning.values())
    gated = sum(1 for v in zoning.values() for t in v["teleports"] if "reqs" in t)
    print(f"wrote {len(zoning)} playfields, {count('zoneLines')} zone lines, {count('teleports')} teleports "
          f"({gated} with requirements), {count('missionEntrances')} mission entrances to {os.path.normpath(out)}")


if __name__ == "__main__":
    main()