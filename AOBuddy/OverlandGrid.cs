using System;
using System.Collections.Generic;
using System.IO;
using AOSharp.Common.GameData;

namespace AOBuddy
{
    /// <summary>
    /// Walkable cells of an OUTDOOR playfield, for overland routing: the ground heightfield (ground.bin) with
    /// the zone's wall triangles (walls.bin, tools/AONavExtractor) stamped in. A cell is blocked when
    ///   * a wall triangle passes through it at body height (0.3-1.9 m over the ground there), so a gate arch
    ///     or a roof edge overhead does not close the way under it, while the city wall beside it does
    ///   * it is off the map
    /// Zone lines are not in here: the caller passes the ones it must not cross per search (every line but
    /// the one it means to take).
    ///
    /// SLOPES ARE DIRECTIONAL (owner, 2026-09-25): a slope too steep to climb is still walkable going down,
    /// and there is NO fall damage outdoors, so a drop of any height is routable — cliffs included. Steepness
    /// is therefore not a cell property but an EDGE one: the search may step from cell A to cell B only when
    /// the RISE A->B stays under MaxRise; descending is always allowed. (Indoors FloorGrid keeps its own,
    /// stricter rules.) The smoothing's corridor test checks the same one-way rule along its chords.
    ///
    /// Nothing here moves the body.
    /// </summary>
    /// <summary>What overland routing needs from a playfield's walkable cells (OverlandGrid outdoors, FloorGrid indoors).</summary>
    public interface IWalkGrid
    {
        int Pf { get; }
        HashSet<int> CellsAlong(Vector3 a, Vector3 b, float radius, HashSet<int> into = null);
        List<Vector3> FindPath(Vector3 a, Vector3 b, HashSet<int> extra, float snap, float reach, out string why);
        /// <summary>Standable ground at p — the front-ray test that finds a doorway exit's open side.</summary>
        bool OpenAt(Vector3 p);
    }

    public sealed class OverlandGrid : IWalkGrid
    {
        public int Pf { get; }
        public readonly float Cell;
        private readonly int _w, _h;
        private readonly bool[] _blocked;
        private readonly float[] _ch;
        private readonly NavGround _ground;
        // MULTI-LEVEL (owner, 2026-09-25): collision.bin's walkable surfaces — ramps, wall-tops, platforms,
        // the Stret West Bank wall you can walk up AND under — as extra floors per cell on top of the terrain.
        // Sparse: a cell with no structure on it has only the terrain floor and costs nothing extra.
        private Dictionary<int, float[]> _extra;
        private readonly HashSet<long> _blockedFl = new HashSet<long>();   // (cell << FloorShift) | floor
        private const float Merge = 0.6f;      // surfaces this close are one floor (a slab's top and underside)
        private const int MaxExtra = 15;       // + terrain = 16 floors per cell (4-bit floor index). The ICC wompa
                                             // tower stacks a deck every ~13 m to 152 m: 7 slots kept only the TOP decks and
                                             // threw away the door-sill and mezzanine floors near the ground (2026-09-25).
        private const int FloorShift = 4, FloorMask = 15;
        public int BlockedCells { get; private set; }
        public bool HasWalls { get; private set; }

        private const float BodyLow = 0.3f, BodyHigh = 1.9f;
        private const float MaxRise = 1.2f;          // metres of rise per metre of step: ~50 degrees — the climb limit (downhill is unlimited: no fall damage outdoors)
        private const int MaxCells = 6_000_000;      // cell size grows on the biggest maps to stay under this. 6 M (2026-09-25,
                                                     // ICC): Andromeda's wompa tower needs 2 m cells — at 4 m the doorway is one
                                                     // cell and its door-sill wall samples seal the tower shut.
        private const int MaxExpand = 1_500_000;

        // WALL CLEARANCE (owner, 2026-09-26: "the fastest route is hugging the walls, the safest route is to run the
        // middle, as most times that is where the road is"). _clear = cells to the nearest blocked cell, capped;
        // a step near a wall costs more (fading to nothing at ClearMetres), and the string-pull may not bring the
        // route closer to a wall than the search had it. Built from _blocked after a build or a cache read, so
        // the cache format is unchanged.
        private byte[] _clear;
        private const float ClearMetres = 5f, ClearWeight = 2f, ClearKeep = 2f;
        // WATER (Algorithman, 2026-09-26): swimming is no slower, but the server corrects the bot far more there.
        // Water deeper than WadeDepth costs WaterWeight extra per step, so he swims only when it saves a lot.
        private bool[] _water;
        private const float WaterWeight = 2f, WadeDepth = 1.0f;
        // LEARNED (LearnedGround, owner 2026-09-26): the owner's recorded roads cost RoadFactor of normal, the
        // server's remembered snap-back spots add up to SnapWeight x hits within SnapRadius, and a slope costs
        // SlopeWeight per unit of grade over SlopeFree (downhill half) - the hill loses to the longer road.
        private bool[] _road; private float[] _learn; private bool _anyRoad; private int _learnVer = -1;
        private const float RoadFactor = 0.5f, SnapWeight = 2f, SnapRadius = 6f, SlopeWeight = 4f, SlopeFree = 0.15f;

        private OverlandGrid(int pf, float cell, int w, int h, NavGround g)
        {
            Pf = pf; Cell = cell; _w = w; _h = h; _ground = g;
            _blocked = new bool[w * h];
            _ch = new float[w * h];   // centre heights, for the directional edge checks
        }

        /// <summary>The grid for an outdoor playfield; null when it has no ground data (dungeons, instances).</summary>
        public static OverlandGrid Build(string pluginDir, int pf, AOBuddyNav nav, Action<string> log)
        {
            var g = nav?.Ground;
            if (g == null) return null;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            float sizeX = (g.SamplesX - 1) * g.Cell, sizeZ = (g.SamplesZ - 1) * g.Cell;
            float cell = 1f;
            while ((sizeX / cell) * (sizeZ / cell) > MaxCells) cell *= 2;
            var grid = new OverlandGrid(pf, cell, (int)Math.Ceiling(sizeX / cell), (int)Math.Ceiling(sizeZ / cell), g);
            grid.StampGround();
            grid.FillCentreHeights();
            grid.StampFloors(nav?.Collision);   // structures first: the walls below block per floor
            string wp = Path.Combine(AOBuddyNav.FolderFor(pluginDir, pf), "walls.bin");
            if (File.Exists(wp)) { grid.StampWalls(NavCollision.Read(wp)); grid.HasWalls = true; }
            grid.StampHeadroom();
            grid.StampClearance();
            for (int i = 0; i < grid._blocked.Length; i++) if (grid._blocked[i]) grid.BlockedCells++;
            log?.Invoke($"OVERLAND: grid for pf {pf}: {grid._w}x{grid._h} cells of {cell:0} m, {grid.BlockedCells} blocked, {grid._extra?.Count ?? 0} multi-floor, walls {(grid.HasWalls ? "yes" : "NONE (walls.bin missing: routing sees only cliffs)")}, {sw.ElapsedMilliseconds} ms");
            return grid;
        }

        // Steepness does NOT block a cell anymore (owner, 2026-09-25: downhill is always walkable outdoors,
        // no fall damage — cliffs included); only off-map ground does. The climb limit lives on the edges
        // (StepOkay), which keeps steep ground one-way: down yes, up no.
        private void StampGround()
        {
            for (int z = 0; z < _h; z++)
                for (int x = 0; x < _w; x++)
                {
                    double x0 = x * Cell, z0 = z * Cell, x1 = x0 + Cell, z1 = z0 + Cell;
                    double a = _ground.HeightAt(x0, z0), b = _ground.HeightAt(x1, z0), c = _ground.HeightAt(x0, z1), d = _ground.HeightAt(x1, z1);
                    if (double.IsNaN(a) || double.IsNaN(b) || double.IsNaN(c) || double.IsNaN(d)) _blocked[z * _w + x] = true;
                }
        }

        private void FillCentreHeights()
        {
            for (int z = 0; z < _h; z++)
                for (int x = 0; x < _w; x++)
                {
                    double h = _ground.HeightAt((x + 0.5) * Cell, (z + 0.5) * Cell);
                    int k = z * _w + x;
                    if (double.IsNaN(h)) { _blocked[k] = true; _ch[k] = 0f; }
                    else _ch[k] = (float)h;
                }
        }

        // ---- GridCache persistence (deterministic from the zone's files; see GridCache) ----------------------

        internal void Write(System.IO.BinaryWriter bw)
        {
            bw.Write(Cell); bw.Write(_w); bw.Write(_h);
            bw.Write(HasWalls);
            // blocked as runs: terrain grids are mostly long stretches of open cells
            int i = 0;
            while (i < _blocked.Length)
            {
                bool v = _blocked[i];
                int run = 1;
                while (i + run < _blocked.Length && _blocked[i + run] == v) run++;
                bw.Write(v);
                bw.Write(run);
                i += run;
            }
            foreach (float h in _ch) bw.Write(h);
            // structure floors (sparse) and the per-floor blocks
            int ne = _extra?.Count ?? 0;
            bw.Write(ne);
            if (ne > 0)
                foreach (var kv in _extra)
                {
                    bw.Write(kv.Key);
                    bw.Write((byte)kv.Value.Length);
                    foreach (float f in kv.Value) bw.Write(f);
                }
            bw.Write(_blockedFl.Count);
            foreach (long b in _blockedFl) bw.Write(b);
        }

        internal static OverlandGrid Read(System.IO.BinaryReader br, NavGround g, int pf)
        {
            float cell = br.ReadSingle();
            int w = br.ReadInt32(), h = br.ReadInt32();
            var grid = new OverlandGrid(pf, cell, w, h, g);
            grid.HasWalls = br.ReadBoolean();
            int i = 0;
            while (i < grid._blocked.Length)
            {
                bool v = br.ReadBoolean();
                int run = br.ReadInt32();
                for (int k = 0; k < run && i < grid._blocked.Length; k++, i++) grid._blocked[i] = v;
                if (v) grid.BlockedCells += run;
            }
            for (int k = 0; k < grid._ch.Length; k++) grid._ch[k] = br.ReadSingle();
            int ne = br.ReadInt32();
            if (ne > 0)
            {
                grid._extra = new Dictionary<int, float[]>(ne);
                for (int e = 0; e < ne; e++)
                {
                    int key = br.ReadInt32();
                    int c = br.ReadByte();
                    var fl = new float[c];
                    for (int f = 0; f < c; f++) fl[f] = br.ReadSingle();
                    grid._extra[key] = fl;
                }
            }
            int nb = br.ReadInt32();
            for (int b = 0; b < nb; b++) grid._blockedFl.Add(br.ReadInt64());
            grid.StampClearance();
            return grid;
        }

        // The zone's STRUCTURE floors from collision.bin (the near-horizontal surfaces; walls.bin holds the
        // steep ones): each triangle stamps the height under the cells its footprint covers — FloorGrid's
        // sampling, on the terrain grid. Floors within Merge of the terrain (or of each other) fold together,
        // so a prop hugging the ground adds nothing; what is left is genuinely another level to walk on.
        private void StampFloors(NavCollision col)
        {
            if (col == null) return;
            var raw = new Dictionary<int, List<float>>();
            foreach (var ch in col.Chunks)
            {
                float[] v = ch.Verts;
                for (int o = 0; o + 8 < v.Length; o += 9)
                {
                    float ax = v[o], ay = v[o + 1], az = v[o + 2], bx = v[o + 3], by = v[o + 4], bz = v[o + 5], cx2 = v[o + 6], cy2 = v[o + 7], cz2 = v[o + 8];
                    float d = (bx - ax) * (cz2 - az) - (cx2 - ax) * (bz - az);
                    if (Math.Abs(d) < 1e-6f) continue;
                    int i0 = Math.Max(0, (int)Math.Floor(Math.Min(ax, Math.Min(bx, cx2)) / Cell)), i1 = Math.Min(_w - 1, (int)Math.Floor(Math.Max(ax, Math.Max(bx, cx2)) / Cell));
                    int j0 = Math.Max(0, (int)Math.Floor(Math.Min(az, Math.Min(bz, cz2)) / Cell)), j1 = Math.Min(_h - 1, (int)Math.Floor(Math.Max(az, Math.Max(bz, cz2)) / Cell));
                    for (int j = j0; j <= j1; j++)
                        for (int i = i0; i <= i1; i++)
                        {
                            float px = (i + 0.5f) * Cell, pz = (j + 0.5f) * Cell;
                            float u = ((px - ax) * (cz2 - az) - (cx2 - ax) * (pz - az)) / d;
                            float t = ((bx - ax) * (pz - az) - (px - ax) * (bz - az)) / d;
                            const float slack = -0.05f;
                            if (u < slack || t < slack || u + t > 1 - slack) continue;
                            int k = j * _w + i;
                            if (_blocked[k]) continue;   // off-map cell: no floor there either
                            if (!raw.TryGetValue(k, out var l)) raw[k] = l = new List<float>();
                            l.Add(ay + u * (by - ay) + t * (cy2 - ay));
                        }
                }
            }
            foreach (var kv in raw)
            {
                kv.Value.Sort();
                var merged = new List<float>();
                foreach (float y in kv.Value)
                {
                    if (merged.Count > 0 && y - merged[merged.Count - 1] <= Merge) merged[merged.Count - 1] = y;   // keep the top of a slab
                    else merged.Add(y);
                }
                merged.RemoveAll(y => Math.Abs(y - _ch[kv.Key]) <= Merge);   // the terrain already is this floor
                if (merged.Count > MaxExtra) merged.RemoveRange(MaxExtra, merged.Count - MaxExtra);   // over cap: keep the floors nearest the ground
                if (merged.Count > 0) (_extra ?? (_extra = new Dictionary<int, float[]>()))[kv.Key] = merged.ToArray();
            }
        }

        private void StampWalls(NavCollision walls)
        {
            float step = Math.Min(0.5f, Cell / 2);
            foreach (var ch in walls.Chunks)
            {
                float[] v = ch.Verts;
                for (int o = 0; o + 8 < v.Length; o += 9)
                {
                    // Sample the triangle's surface; a sample at body height over a FLOOR blocks that floor —
                    // so a doorway's lintel blocks nothing under it while the wall's face blocks the floor it
                    // stands on, at every level (ground beside it, walkway on top of it).
                    float ax = v[o], ay = v[o + 1], az = v[o + 2];
                    float ux = v[o + 3] - ax, uy = v[o + 4] - ay, uz = v[o + 5] - az;
                    float wx = v[o + 6] - ax, wy = v[o + 7] - ay, wz = v[o + 8] - az;
                    float longest = Math.Max(Len(ux, uy, uz), Math.Max(Len(wx, wy, wz), Len(wx - ux, wy - uy, wz - uz)));
                    int n = Math.Min(400, Math.Max(1, (int)Math.Ceiling(longest / step)));
                    for (int i = 0; i <= n; i++)
                        for (int j = 0; j <= n - i; j++)
                        {
                            float s = i / (float)n, t = j / (float)n;
                            float px = ax + ux * s + wx * t, py = ay + uy * s + wy * t, pz = az + uz * s + wz * t;
                            int cx = (int)Math.Floor(px / Cell), cz = (int)Math.Floor(pz / Cell);
                            if (cx < 0 || cz < 0 || cx >= _w || cz >= _h) continue;
                            int k = cz * _w + cx;
                            for (int f = 0; f < FloorCount(k); f++)
                            {
                                float above = py - FloorH(k, f);
                                if (above >= BodyLow && above <= BodyHigh) _blockedFl.Add(((long)k << FloorShift) | f);
                            }
                        }
                }
            }
        }

        // Two-pass chamfer distance (3 straight, 4 diagonal) from every blocked cell, in cells, capped.
        private void StampClearance()
        {
            int cap = Math.Min(250, (int)Math.Ceiling(ClearMetres / Cell) + 1);
            var d = new int[_w * _h];
            int inf = cap * 3;
            for (int i = 0; i < d.Length; i++) d[i] = _blocked[i] ? 0 : inf;
            for (int z = 0; z < _h; z++)
                for (int x = 0; x < _w; x++)
                {
                    int i = z * _w + x, v = d[i];
                    if (v == 0) continue;
                    if (x > 0) v = Math.Min(v, d[i - 1] + 3);
                    if (z > 0)
                    {
                        v = Math.Min(v, d[i - _w] + 3);
                        if (x > 0) v = Math.Min(v, d[i - _w - 1] + 4);
                        if (x < _w - 1) v = Math.Min(v, d[i - _w + 1] + 4);
                    }
                    d[i] = v;
                }
            for (int z = _h - 1; z >= 0; z--)
                for (int x = _w - 1; x >= 0; x--)
                {
                    int i = z * _w + x, v = d[i];
                    if (v == 0) continue;
                    if (x < _w - 1) v = Math.Min(v, d[i + 1] + 3);
                    if (z < _h - 1)
                    {
                        v = Math.Min(v, d[i + _w] + 3);
                        if (x < _w - 1) v = Math.Min(v, d[i + _w + 1] + 4);
                        if (x > 0) v = Math.Min(v, d[i + _w - 1] + 4);
                    }
                    d[i] = v;
                }
            _clear = new byte[d.Length];
            for (int i = 0; i < d.Length; i++) _clear[i] = (byte)Math.Min(cap, d[i] / 3);
            _learnVer = -1;
            if (_ground != null)
            {
                _water = new bool[_w * _h];
                for (int z = 0; z < _h; z++)
                    for (int x = 0; x < _w; x++)
                        if (!_blocked[z * _w + x]) _water[z * _w + x] = !double.IsNaN(_ground.SwimY((x + 0.5) * Cell, (z + 0.5) * Cell, WadeDepth));
            }
        }

        private void EnsureLearned()
        {
            LearnedGround.RefreshRoads();
            if (_learnVer == LearnedGround.Version) return;
            _learnVer = LearnedGround.Version;
            _road = new bool[_w * _h]; _learn = new float[_w * _h]; _anyRoad = false;
            bool OnFloor(Vector3 p)
            {
                int cx = CellX(p.X), cz = CellZ(p.Z);
                if (!In(cx, cz)) return false;
                int c = cz * _w + cx;
                for (int f = 0; f < FloorCount(c); f++) if (Math.Abs(FloorH(c, f) - p.Y) < 3f) return true;
                return false;
            }
            var cells = new HashSet<int>();
            foreach (var road in LearnedGround.Roads())
                for (int i = 0; i + 1 < road.Count; i++)
                {
                    Vector3 a = road[i], b = road[i + 1];
                    if (Vector3.Distance(a, b) > 40f || !OnFloor(a) || !OnFloor(b)) continue;   // a zone jump, or another zone's walk
                    CellsAlong(a, b, 1.5f, cells);
                }
            foreach (int c in cells) { _road[c] = true; _anyRoad = true; }
            int R = (int)Math.Ceiling(SnapRadius / Cell);
            foreach (var sp in LearnedGround.SnapsIn(Pf))
            {
                int sx = CellX(sp.X), sz = CellZ(sp.Z);
                for (int dz = -R; dz <= R; dz++)
                    for (int dx = -R; dx <= R; dx++)
                    {
                        if (!In(sx + dx, sz + dz)) continue;
                        float dist = (float)Math.Sqrt(dx * dx + dz * dz) * Cell;
                        if (dist > SnapRadius) continue;
                        _learn[(sz + dz) * _w + sx + dx] += SnapWeight * Math.Min(sp.N, 6) * (1f - dist / SnapRadius);
                    }
            }
        }

        private float ClearAt(int cell) => _clear == null ? ClearMetres : _clear[cell] * Cell;

        // 1 + ClearWeight right against a wall, falling off to 1 at ClearMetres.
        private float WallCost(int cell)
        {
            float d = ClearAt(cell);
            if (d >= ClearMetres) return 0f;
            float t = 1f - d / ClearMetres;
            return ClearWeight * t * t;
        }

        // A floor with another one just above it (under a deck, inside a slab — FloorGrid's rule) is no
        // place to stand. Considers the terrain floor as one of the stack.
        private void StampHeadroom()
        {
            if (_extra == null) return;
            foreach (var kv in _extra)
            {
                var all = new float[1 + kv.Value.Length];
                all[0] = _ch[kv.Key];
                Array.Copy(kv.Value, 0, all, 1, kv.Value.Length);
                Array.Sort(all);
                for (int f = 0; f + 1 < all.Length; f++)
                    if (all[f + 1] - all[f] < BodyHigh) _blockedFl.Add(((long)kv.Key << FloorShift) | FloorIndex(kv.Key, all[f]));
            }
        }

        // ---- floors (terrain = 0, structures above/below = 1..) ------------------------------------------------

        private int FloorCount(int cell) => (_extra != null && _extra.TryGetValue(cell, out var f)) ? 1 + f.Length : 1;
        private float FloorH(int cell, int i) => i == 0 ? _ch[cell] : _extra[cell][i - 1];

        private int FloorIndex(int cell, float h)
        {
            if (Math.Abs(h - _ch[cell]) < 0.01f) return 0;
            var f = _extra[cell];
            for (int i = 0; i < f.Length; i++) if (Math.Abs(h - f[i]) < 0.01f) return i + 1;
            return 0;
        }

        private bool FloorOpen(int cell, int i) => !_blockedFl.Contains(((long)cell << FloorShift) | i);

        /// <summary>Diagnostic: every floor of the cell at (x, z) with its open/blocked state.</summary>
        public string CellInfo(float x, float z)
        {
            int cx = CellX(x), cz = CellZ(z);
            if (!In(cx, cz)) return $"({x:0},{z:0}): off-grid";
            int cell = cz * _w + cx;
            var sb = new System.Text.StringBuilder($"cell ({cx},{cz}) [{cx * Cell:0}-{cx * Cell + Cell:0} x {cz * Cell:0}-{cz * Cell + Cell:0}]: {( _blocked[cell] ? "CELL BLOCKED (off-map)" : "cell open")}");
            for (int i = 0; i < FloorCount(cell); i++)
                sb.Append($"  floor {FloorH(cell, i):0.0} {(FloorOpen(cell, i) ? "open" : "BLOCKED")}");
            return sb.ToString();
        }

        // The open floor nearest height y (0 when y is unknown); -1 when every floor is blocked.
        private int NearestFloor(int cell, float y)
        {
            int best = -1; float bd = float.NaN;
            for (int i = 0; i < FloorCount(cell); i++)
            {
                if (!FloorOpen(cell, i)) continue;
                float d = Math.Abs(FloorH(cell, i) - y);
                if (best < 0 || d < bd) { bd = d; best = i; }
            }
            return best;
        }

        private static float Len(float x, float y, float z) => (float)Math.Sqrt(x * x + y * y + z * z);

        // ---- queries ------------------------------------------------------------------------------------------

        private int CellX(float x) => (int)Math.Floor(x / Cell);
        private int CellZ(float z) => (int)Math.Floor(z / Cell);
        private bool In(int x, int z) => x >= 0 && z >= 0 && x < _w && z < _h;
        private bool Open(int x, int z, HashSet<int> extra) => In(x, z) && !_blocked[z * _w + x] && (extra == null || !extra.Contains(z * _w + x));
        private Vector3 Centre(int x, int z, float h)
            => new Vector3((x + 0.5f) * Cell, h, (z + 0.5f) * Cell);

        public bool IsOpen(Vector3 p, HashSet<int> extra = null) => Open(CellX(p.X), CellZ(p.Z), extra);

        /// <summary>Open ground at p at ANY of its levels: the cell is open and one of its floors sits within 3 m of p.Y.</summary>
        public bool OpenAt(Vector3 p)
        {
            int x = CellX(p.X), z = CellZ(p.Z);
            if (!Open(x, z, null)) return false;
            if (float.IsNaN(p.Y)) return true;
            int cell = z * _w + x;
            for (int i = 0; i < FloorCount(cell); i++)
                if (Math.Abs(FloorH(cell, i) - p.Y) <= 3f && FloorOpen(cell, i)) return true;
            return false;
        }

        /// <summary>Cells within radius of the segment a-b, for the caller's per-search blocked set.</summary>
        public HashSet<int> CellsAlong(Vector3 a, Vector3 b, float radius, HashSet<int> into = null)
        {
            into = into ?? new HashSet<int>();
            float len = (float)Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Z - a.Z) * (b.Z - a.Z));
            int n = Math.Max(1, (int)Math.Ceiling(len / (Cell / 2)));
            int r = (int)Math.Ceiling(radius / Cell);
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                int cx = CellX(a.X + (b.X - a.X) * t), cz = CellZ(a.Z + (b.Z - a.Z) * t);
                for (int dz = -r; dz <= r; dz++)
                    for (int dx = -r; dx <= r; dx++)
                        if (In(cx + dx, cz + dz) && dx * dx + dz * dz <= r * r) into.Add((cz + dz) * _w + cx + dx);
            }
            return into;
        }

        /// <summary>The nearest open cell to p within maxR metres, ring by ring; null when there is none.</summary>
        private (int, int)? NearestOpen(Vector3 p, float maxR, HashSet<int> extra)
        {
            int cx = CellX(p.X), cz = CellZ(p.Z);
            if (Open(cx, cz, extra)) return (cx, cz);
            int R = (int)Math.Ceiling(maxR / Cell);
            for (int r = 1; r <= R; r++)
            {
                (int, int)? best = null; float bd = float.MaxValue;
                for (int dz = -r; dz <= r; dz++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dz)) != r || !Open(cx + dx, cz + dz, extra)) continue;
                        float d = dx * dx + dz * dz;
                        if (d < bd) { bd = d; best = (cx + dx, cz + dz); }
                    }
                if (best.HasValue) return best;
            }
            return null;
        }

        /// <summary>
        /// A walkable path from a to within reach metres of b (ground heights on the points), smoothed to its
        /// corners. The start snaps to the nearest open cell within snap metres. The path ends at the first open
        /// cell within reach of b, so an object standing in its own walls (a grid terminal's kiosk, whose empty
        /// inside is the nearest open ground to it) is walked up to from outside. The first point is a itself;
        /// the last is b when b's cell is open and reachable. Null (with why) when there is none.
        /// </summary>
        public List<Vector3> FindPath(Vector3 a, Vector3 b, HashSet<int> extra, float snap, float reach, out string why)
            => FindPath(a, b, extra, snap, reach, out why, float.NaN);

        /// <summary>Same, with a goal HEIGHT: the path must arrive on a floor within 2.5 m of goalY (a wall-top,
        /// a walkway) instead of on whichever level touches the goal cell first. NaN goalY = any level.</summary>
        public List<Vector3> FindPath(Vector3 a, Vector3 b, HashSet<int> extra, float snap, float reach, out string why, float goalY)
        {
            // In sight of b first; if that walks nowhere (b's pocket is closed off), on distance alone.
            return Search(a, b, extra, snap, reach, true, out why, goalY) ?? Search(a, b, extra, snap, reach, false, out _, goalY);
        }

        private List<Vector3> Search(Vector3 a, Vector3 b, HashSet<int> extra, float snap, float reach, bool sight, out string why, float goalY)
        {
            why = "";
            var s = NearestOpen(a, snap, extra);
            if (s == null) { why = $"no open ground within {snap:0} m of me"; return null; }
            int startCell = s.Value.Item2 * _w + s.Value.Item1;
            int startFloor = NearestFloor(startCell, a.Y);
            if (startFloor < 0) { why = "every floor under me is blocked"; return null; }
            long start = ((long)startCell << FloorShift) | startFloor;
            int bx = CellX(b.X), bz = CellZ(b.Z);
            float reachCells = Math.Max(reach, Cell) / Cell;
            // Close enough AND in plain sight of b: "4 m from the terminal" was otherwise the far side of its kiosk
            // wall. When b stands in a closed box of walls nothing sees it, and then distance alone has to do.
            int bCell = bz * _w + bx;
            bool needSight = sight && SeenFromOutside(bx, bz, reachCells);
            bool AtGoal(long node)
            {
                int c = (int)(node >> FloorShift);
                float dx = c % _w - bx, dz = c / _w - bz;
                if (dx * dx + dz * dz > reachCells * reachCells) return false;
                // A known goal height means THE level matters (the wall-top, a walkway): arrive on a floor
                // near it, not on whatever floor touches the goal cell first. NaN = any level, as before.
                if (!float.IsNaN(goalY) && Math.Abs(FloorH(c, (int)(node & FloorMask)) - goalY) > 2.5f) return false;
                return !needSight || Sight(c, bCell);
            }
            long goal = -1;

            var gScore = new Dictionary<long, float> { [start] = 0 };
            var parent = new Dictionary<long, long>();
            var closed = new HashSet<long>();
            var open = new PriorityQueue<long, float>();
            EnsureLearned();
            float hScale = _anyRoad ? RoadFactor : 1.2f;   // a road step costs less than its length: keep the estimate under it
            float H(int x, int z) { int dx = Math.Abs(x - bx), dz = Math.Abs(z - bz); return hScale * Math.Max(0f, Math.Max(dx, dz) + 0.4142f * Math.Min(dx, dz) - reachCells); }
            open.Enqueue(start, H(s.Value.Item1, s.Value.Item2));
            bool found = false;
            while (open.TryDequeue(out long cur, out _))
            {
                if (!closed.Add(cur)) continue;
                if (AtGoal(cur)) { goal = cur; found = true; break; }
                if (closed.Count > MaxExpand) { why = $"searched {MaxExpand} cells without reaching it"; return null; }
                int curCell = (int)(cur >> FloorShift);
                int x = curCell % _w, z = curCell / _w;
                int curFloor = (int)(cur & FloorMask);
                float fh = FloorH(curCell, curFloor);
                float gc = gScore[cur];
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        int nx = x + dx, nz = z + dz;
                        if (!Open(nx, nz, extra)) continue;
                        if (dx != 0 && dz != 0 && (!Open(x + dx, z, extra) || !Open(x, z + dz, extra))) continue;   // no squeezing past a corner
                        int ncell = nz * _w + nx;
                        float d = (dx != 0 && dz != 0 ? 1.4142f : 1f) * Cell;
                        float len1 = dx != 0 && dz != 0 ? 1.4142f : 1f;
                        float baseMul = 1f + WallCost(ncell) + (_water != null && _water[ncell] ? WaterWeight : 0f) + (_learn != null ? _learn[ncell] : 0f);
                        float roadMul = _road != null && _road[ncell] ? RoadFactor : 1f;
                        for (int j = 0; j < FloorCount(ncell); j++)
                        {
                            // The one-way rule per floor pair: climbing onto the next floor must stay under
                            // MaxRise; dropping onto it is free (no fall damage outdoors, owner 2026-09-25).
                            float rise = FloorH(ncell, j) - fh;
                            if (rise > MaxRise * d) continue;
                            float grade = Math.Abs(rise) / d;
                            float slope = grade > SlopeFree ? SlopeWeight * (grade - SlopeFree) * (rise < 0 ? 0.5f : 1f) : 0f;
                            float step = gc + len1 * (baseMul + slope) * roadMul;
                            long nn = ((long)ncell << FloorShift) | j;
                            if (closed.Contains(nn) || !FloorOpen(ncell, j)) continue;
                            if (gScore.TryGetValue(nn, out float old) && old <= step) continue;
                            gScore[nn] = step; parent[nn] = cur;
                            open.Enqueue(nn, step + H(nx, nz));
                        }
                    }
            }
            if (!found) { why = $"walled off: no open ground within {reach:0} m of ({b.X:0},{b.Z:0}) that I can walk to"; return null; }

            var nodes = new List<long>();
            for (long n = goal; ; n = parent[n]) { nodes.Add(n); if (n == start) break; }
            nodes.Reverse();

            // String-pull: from each kept node, jump to the farthest one on a clear line.
            var pts = new List<Vector3> { a };
            int i0 = 0;
            while (i0 < nodes.Count - 1)
            {
                int j = nodes.Count - 1;
                while (j > i0 + 1 && !Clear(nodes[i0], nodes[j], extra)) j--;
                int c = (int)(nodes[j] >> FloorShift);
                pts.Add(Centre(c % _w, c / _w, FloorH(c, (int)(nodes[j] & FloorMask))));
                i0 = j;
            }
            if (pts.Count == 1)
            {
                int c = (int)(goal >> FloorShift);
                pts.Add(Centre(c % _w, c / _w, FloorH(c, (int)(goal & FloorMask))));
            }
            int goalCell = (int)(goal >> FloorShift);
            if (goalCell != bCell)
            {
                int bf = NearestFloor(bCell, b.Y);
                if (bf >= 0 && IsOpen(b, extra) && Clear(goal, ((long)bCell << FloorShift) | bf, extra)) pts[pts.Count - 1] = b;
            }
            else pts[pts.Count - 1] = b;
            return pts;
        }

        // A thin line of cells with no wall or cliff on it (zone-line cells don't hide anything).
        private bool Sight(int c0, int c1)
        {
            float x0 = c0 % _w + 0.5f, z0 = c0 / _w + 0.5f, x1 = c1 % _w + 0.5f, z1 = c1 / _w + 0.5f;
            int n = Math.Max(1, (int)Math.Ceiling(Math.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0)) * 3));
            for (int i = 1; i < n; i++)
            {
                float t = i / (float)n;
                int x = (int)Math.Floor(x0 + (x1 - x0) * t), z = (int)Math.Floor(z0 + (z1 - z0) * t);
                if (!In(x, z) || _blocked[z * _w + x]) return false;
            }
            return true;
        }

        // Whether any open cell within r cells has sight of (bx, bz).
        private bool SeenFromOutside(int bx, int bz, float r)
        {
            int R = (int)Math.Ceiling(r), b = bz * _w + bx;
            for (int dz = -R; dz <= R; dz++)
                for (int dx = -R; dx <= R; dx++)
                {
                    int x = bx + dx, z = bz + dz;
                    if (dx * dx + dz * dz > r * r || !In(x, z) || _blocked[z * _w + x]) continue;
                    if ((dx != 0 || dz != 0) && Sight(z * _w + x, b)) return true;
                }
            return false;
        }

        // A thin line of cells with no wall or cliff on it (zone-line cells don't hide anything).
        private bool Clear(long n0, long n1, HashSet<int> extra)
        {
            int c0 = (int)(n0 >> FloorShift), c1 = (int)(n1 >> FloorShift);
            float h0 = FloorH(c0, (int)(n0 & FloorMask)), h1 = FloorH(c1, (int)(n1 & FloorMask));
            float x0 = c0 % _w + 0.5f, z0 = c0 / _w + 0.5f, x1 = c1 % _w + 0.5f, z1 = c1 / _w + 0.5f;
            float len = (float)Math.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0));
            int n = Math.Max(1, (int)Math.Ceiling(len * 3));
            // Test a body's width, not a line: a third of a cell either side, across the direction of travel.
            float px = len > 0 ? -(z1 - z0) / len * 0.35f : 0, pz = len > 0 ? (x1 - x0) / len * 0.35f : 0;
            // No closer to a wall than the ends are (up to ClearKeep): the pull would otherwise lay the line back
            // along the wall the search's wall cost kept it off. A narrow gate passes, its ends are narrow too.
            float keep = Math.Min(ClearKeep, Math.Min(ClearAt(c0), ClearAt(c1)));
            // ...and the chord must TRACK A FLOOR it can be walked on: at each sample, some OPEN floor of
            // the cell lies within 1.5 m of the chord's height. The floors follow a ramp, so a ramp chord
            // passes; a shortcut straight up a wall face passes through heights that have no floor near
            // them (the wall-top far above, the ground far below) and is refused; and a shortcut through
            // a building is refused because the wall blocks the floor it would run on. A chord that falls
            // away below the floors is likewise not collapsed — the raw route's own edges carry the drop.
            for (int i = 1; i < n; i++)
            {
                float t = i / (float)n;
                int cx = (int)Math.Floor(x0 + (x1 - x0) * t), cz = (int)Math.Floor(z0 + (z1 - z0) * t);
                if (!In(cx, cz)) return false;
                int cell = cz * _w + cx;
                double want = h0 + (h1 - h0) * t;
                bool near = false;
                for (int f = 0; f < FloorCount(cell); f++)
                    if (Math.Abs(FloorH(cell, f) - want) <= 1.5 && FloorOpen(cell, f)) { near = true; break; }
                if (!near) return false;
            }
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float x = x0 + (x1 - x0) * t, z = z0 + (z1 - z0) * t;
                if (!Open((int)Math.Floor(x), (int)Math.Floor(z), extra)) return false;
                if (ClearAt((int)Math.Floor(z) * _w + (int)Math.Floor(x)) < keep) return false;
                if (!Open((int)Math.Floor(x + px), (int)Math.Floor(z + pz), extra) || !Open((int)Math.Floor(x - px), (int)Math.Floor(z - pz), extra)) return false;
            }
            return true;
        }
    }
}