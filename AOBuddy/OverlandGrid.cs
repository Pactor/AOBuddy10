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
    ///   * the ground across it rises more than MaxRise (a cliff)
    ///   * it is off the map
    /// Wall cells are grown by one cell so the path keeps clear of corners. Zone lines are not in here: the
    /// caller passes the ones it must not cross per search (every line but the one it means to take).
    ///
    /// Nothing here moves the body.
    /// </summary>
    /// <summary>What overland routing needs from a playfield's walkable cells (OverlandGrid outdoors, FloorGrid indoors).</summary>
    public interface IWalkGrid
    {
        int Pf { get; }
        HashSet<int> CellsAlong(Vector3 a, Vector3 b, float radius, HashSet<int> into = null);
        List<Vector3> FindPath(Vector3 a, Vector3 b, HashSet<int> extra, float snap, float reach, out string why);
    }

    public sealed class OverlandGrid : IWalkGrid
    {
        public int Pf { get; }
        public readonly float Cell;
        private readonly int _w, _h;
        private readonly bool[] _blocked;
        private readonly NavGround _ground;
        public int BlockedCells { get; private set; }
        public bool HasWalls { get; private set; }

        private const float BodyLow = 0.3f, BodyHigh = 1.9f;
        private const float MaxRise = 1.2f;          // metres of rise per metre across a cell: ~50 degrees
        private const int MaxCells = 4_200_000;      // cell size grows on the biggest maps to stay under this: a 4 km map gets 2 m
        private const int MaxExpand = 1_500_000;

        private OverlandGrid(int pf, float cell, int w, int h, NavGround g)
        {
            Pf = pf; Cell = cell; _w = w; _h = h; _ground = g; _blocked = new bool[w * h];
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
            string wp = Path.Combine(AOBuddyNav.FolderFor(pluginDir, pf), "walls.bin");
            if (File.Exists(wp)) { grid.StampWalls(NavCollision.Read(wp)); grid.HasWalls = true; }
            for (int i = 0; i < grid._blocked.Length; i++) if (grid._blocked[i]) grid.BlockedCells++;
            log?.Invoke($"OVERLAND: grid for pf {pf}: {grid._w}x{grid._h} cells of {cell:0} m, {grid.BlockedCells} blocked, walls {(grid.HasWalls ? "yes" : "NONE (walls.bin missing: routing sees only cliffs)")}, {sw.ElapsedMilliseconds} ms");
            return grid;
        }

        private void StampGround()
        {
            for (int z = 0; z < _h; z++)
                for (int x = 0; x < _w; x++)
                {
                    double x0 = x * Cell, z0 = z * Cell, x1 = x0 + Cell, z1 = z0 + Cell;
                    double a = _ground.HeightAt(x0, z0), b = _ground.HeightAt(x1, z0), c = _ground.HeightAt(x0, z1), d = _ground.HeightAt(x1, z1);
                    if (double.IsNaN(a) || double.IsNaN(b) || double.IsNaN(c) || double.IsNaN(d)) { _blocked[z * _w + x] = true; continue; }
                    double rise = Math.Max(Math.Max(Math.Abs(a - b), Math.Abs(c - d)), Math.Max(Math.Abs(a - c), Math.Abs(b - d)));
                    if (rise > MaxRise * Cell) _blocked[z * _w + x] = true;
                }
        }

        private void StampWalls(NavCollision walls)
        {
            var hit = new bool[_blocked.Length];
            float step = Math.Min(0.5f, Cell / 2);
            foreach (var ch in walls.Chunks)
            {
                float[] v = ch.Verts;
                for (int o = 0; o + 8 < v.Length; o += 9)
                {
                    // Sample the triangle's surface; a sample at body height over the ground there blocks its cell.
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
                            if (hit[k]) continue;
                            double gh = _ground.HeightAt(px, pz);
                            if (double.IsNaN(gh)) continue;
                            double above = py - gh;
                            if (above >= BodyLow && above <= BodyHigh) hit[k] = true;
                        }
                }
            }
            // Not grown: a wall sample anywhere in a cell already blocks all of it, and the smoothed path's body-width
            // test (Clear) keeps off corners. Growing it by a cell sealed the Newland grid terminal's 2 m entrance.
            for (int k = 0; k < hit.Length; k++) if (hit[k]) _blocked[k] = true;
        }

        private static float Len(float x, float y, float z) => (float)Math.Sqrt(x * x + y * y + z * z);

        // ---- queries ------------------------------------------------------------------------------------------

        private int CellX(float x) => (int)Math.Floor(x / Cell);
        private int CellZ(float z) => (int)Math.Floor(z / Cell);
        private bool In(int x, int z) => x >= 0 && z >= 0 && x < _w && z < _h;
        private bool Open(int x, int z, HashSet<int> extra) => In(x, z) && !_blocked[z * _w + x] && (extra == null || !extra.Contains(z * _w + x));
        private Vector3 Centre(int x, int z)
        {
            float wx = (x + 0.5f) * Cell, wz = (z + 0.5f) * Cell;
            double h = _ground.HeightAt(wx, wz);
            return new Vector3(wx, double.IsNaN(h) ? 0f : (float)h, wz);
        }

        public bool IsOpen(Vector3 p, HashSet<int> extra = null) => Open(CellX(p.X), CellZ(p.Z), extra);

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
        {
            // In sight of b first; if that walks nowhere (b's pocket is closed off), on distance alone.
            return Search(a, b, extra, snap, reach, true, out why) ?? Search(a, b, extra, snap, reach, false, out _);
        }

        private List<Vector3> Search(Vector3 a, Vector3 b, HashSet<int> extra, float snap, float reach, bool sight, out string why)
        {
            why = "";
            var s = NearestOpen(a, snap, extra);
            if (s == null) { why = $"no open ground within {snap:0} m of me"; return null; }
            int start = s.Value.Item2 * _w + s.Value.Item1;
            int bx = CellX(b.X), bz = CellZ(b.Z);
            float reachCells = Math.Max(reach, Cell) / Cell;
            // Close enough AND in plain sight of b: "4 m from the terminal" was otherwise the far side of its kiosk
            // wall. When b stands in a closed box of walls nothing sees it, and then distance alone has to do.
            int bCell = bz * _w + bx;
            bool needSight = sight && SeenFromOutside(bx, bz, reachCells);
            bool AtGoal(int c)
            {
                float dx = c % _w - bx, dz = c / _w - bz;
                return dx * dx + dz * dz <= reachCells * reachCells && (!needSight || Sight(c, bCell));
            }
            int goal = -1;

            var gScore = new Dictionary<int, float> { [start] = 0 };
            var parent = new Dictionary<int, int>();
            var closed = new HashSet<int>();
            var open = new PriorityQueue<int, float>();
            float H(int x, int z) { int dx = Math.Abs(x - bx), dz = Math.Abs(z - bz); return 1.2f * Math.Max(0f, Math.Max(dx, dz) + 0.4142f * Math.Min(dx, dz) - reachCells); }
            open.Enqueue(start, H(s.Value.Item1, s.Value.Item2));
            bool found = false;
            while (open.TryDequeue(out int cur, out _))
            {
                if (!closed.Add(cur)) continue;
                if (AtGoal(cur)) { goal = cur; found = true; break; }
                if (closed.Count > MaxExpand) { why = $"searched {MaxExpand} cells without reaching it"; return null; }
                int x = cur % _w, z = cur / _w;
                float gc = gScore[cur];
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        int nx = x + dx, nz = z + dz;
                        if (!Open(nx, nz, extra)) continue;
                        if (dx != 0 && dz != 0 && (!Open(x + dx, z, extra) || !Open(x, z + dz, extra))) continue;   // no squeezing past a corner
                        int k = nz * _w + nx;
                        if (closed.Contains(k)) continue;
                        float ng = gc + (dx != 0 && dz != 0 ? 1.4142f : 1f);
                        if (gScore.TryGetValue(k, out float old) && old <= ng) continue;
                        gScore[k] = ng; parent[k] = cur;
                        open.Enqueue(k, ng + H(nx, nz));
                    }
            }
            if (!found) { why = $"walled off: no open ground within {reach:0} m of ({b.X:0},{b.Z:0}) that I can walk to"; return null; }

            var cells = new List<int>();
            for (int c = goal; ; c = parent[c]) { cells.Add(c); if (c == start) break; }
            cells.Reverse();

            // String-pull: from each kept cell, jump to the farthest one in plain sight.
            var pts = new List<Vector3> { a };
            int i0 = 0;
            while (i0 < cells.Count - 1)
            {
                int j = cells.Count - 1;
                while (j > i0 + 1 && !Clear(cells[i0], cells[j], extra)) j--;
                pts.Add(Centre(cells[j] % _w, cells[j] / _w));
                i0 = j;
            }
            if (pts.Count == 1) pts.Add(Centre(goal % _w, goal / _w));
            if (goal == bz * _w + bx || (IsOpen(b, extra) && Clear(goal, bz * _w + bx, extra))) pts[pts.Count - 1] = b;
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

        private bool Clear(int c0, int c1, HashSet<int> extra)
        {
            float x0 = c0 % _w + 0.5f, z0 = c0 / _w + 0.5f, x1 = c1 % _w + 0.5f, z1 = c1 / _w + 0.5f;
            float len = (float)Math.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0));
            int n = Math.Max(1, (int)Math.Ceiling(len * 3));
            // Test a body's width, not a line: a third of a cell either side, across the direction of travel.
            float px = len > 0 ? -(z1 - z0) / len * 0.35f : 0, pz = len > 0 ? (x1 - x0) / len * 0.35f : 0;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float x = x0 + (x1 - x0) * t, z = z0 + (z1 - z0) * t;
                if (!Open((int)Math.Floor(x), (int)Math.Floor(z), extra)) return false;
                if (!Open((int)Math.Floor(x + px), (int)Math.Floor(z + pz), extra) || !Open((int)Math.Floor(x - px), (int)Math.Floor(z - pz), extra)) return false;
            }
            return true;
        }
    }
}