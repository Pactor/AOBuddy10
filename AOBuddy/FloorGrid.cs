using System;
using System.Collections.Generic;
using System.IO;
using AOSharp.Common.GameData;

namespace AOBuddy
{
    /// <summary>
    /// Walkable floors of a playfield WITHOUT a ground heightfield (dungeons, the Grid), for overland routing:
    /// layered 0.5 m cells built from the client's floor triangles (collision.bin), with the wall triangles
    /// (walls.bin) blocking at body height. A cell can hold several floors, one above the other; a step goes to
    /// the floor in the next cell within MaxStep of the one we are on, so the path stays on the level it
    /// started on and on the platform it is walking - the Grid's middle level is a web of walkways a metre or
    /// two wide over a 33 m drop (2026-09-24). Changing level is not walking: that is a lift beam, which the
    /// route planner treats as an exit. Dropping off an edge is never planned.
    ///
    /// Nothing here moves the body.
    /// </summary>
    public sealed class FloorGrid : IWalkGrid
    {
        public int Pf { get; }
        public const float Cell = 0.5f;
        private readonly int _x0, _z0, _w, _h;
        private readonly Dictionary<int, float[]> _floors = new Dictionary<int, float[]>();   // cell -> floor heights, ascending
        private readonly HashSet<long> _blocked = new HashSet<long>();                      // cell * 8 + floor index

        private const float Merge = 0.6f;             // surfaces this close are one floor (a deck's top and underside)
        private const float MaxStep = 0.4f;           // rise between neighbouring cells (0.5 m): ~40 degrees
        private const float BodyLow = 0.3f, BodyHigh = 1.9f;
        private const int MaxFloors = 8;
        private const int MaxExpand = 1_000_000;

        private FloorGrid(int pf, int x0, int z0, int w, int h) { Pf = pf; _x0 = x0; _z0 = z0; _w = w; _h = h; }

        public static FloorGrid Build(string pluginDir, int pf, AOBuddyNav nav, Action<string> log)
        {
            if (nav?.Collision == null || nav.Ground != null) return null;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
            foreach (var c in nav.Collision.Chunks)
                for (int i = 0; i + 2 < c.Verts.Length; i += 3)
                {
                    minX = Math.Min(minX, c.Verts[i]); maxX = Math.Max(maxX, c.Verts[i]);
                    minZ = Math.Min(minZ, c.Verts[i + 2]); maxZ = Math.Max(maxZ, c.Verts[i + 2]);
                }
            if (minX > maxX) return null;
            int x0 = (int)Math.Floor(minX / Cell) - 2, z0 = (int)Math.Floor(minZ / Cell) - 2;
            int w = (int)Math.Ceiling(maxX / Cell) + 2 - x0, h = (int)Math.Ceiling(maxZ / Cell) + 2 - z0;
            if ((long)w * h > 16_000_000) { log?.Invoke($"OVERLAND: pf {pf} is too big for a floor grid ({w}x{h})"); return null; }
            var grid = new FloorGrid(pf, x0, z0, w, h);
            grid.StampFloors(nav.Collision);
            string wp = Path.Combine(AOBuddyNav.FolderFor(pluginDir, pf), "walls.bin");
            bool walls = File.Exists(wp);
            if (walls) grid.StampWalls(NavCollision.Read(wp));
            grid.StampHeadroom();
            log?.Invoke($"OVERLAND: floor grid for pf {pf}: {w}x{h} cells of {Cell} m, {grid._floors.Count} with floor, {grid._blocked.Count} floor cells blocked, walls {(walls ? "yes" : "NONE")}, {sw.ElapsedMilliseconds} ms");
            return grid;
        }

        private void StampFloors(NavCollision col)
        {
            var raw = new Dictionary<int, List<float>>();
            foreach (var ch in col.Chunks)
            {
                float[] v = ch.Verts;
                for (int o = 0; o + 8 < v.Length; o += 9)
                {
                    float ax = v[o], ay = v[o + 1], az = v[o + 2], bx = v[o + 3], by = v[o + 4], bz = v[o + 5], cx = v[o + 6], cy = v[o + 7], cz = v[o + 8];
                    float d = (bx - ax) * (cz - az) - (cx - ax) * (bz - az);
                    if (Math.Abs(d) < 1e-6f) continue;
                    int i0 = CellX(Math.Min(ax, Math.Min(bx, cx))), i1 = CellX(Math.Max(ax, Math.Max(bx, cx)));
                    int j0 = CellZ(Math.Min(az, Math.Min(bz, cz))), j1 = CellZ(Math.Max(az, Math.Max(bz, cz)));
                    for (int j = j0; j <= j1; j++)
                        for (int i = i0; i <= i1; i++)
                        {
                            // The cell's centre inside the triangle, with a little slack so thin walkways are not lost.
                            float px = (i + _x0 + 0.5f) * Cell, pz = (j + _z0 + 0.5f) * Cell;
                            float u = ((px - ax) * (cz - az) - (cx - ax) * (pz - az)) / d;
                            float t = ((bx - ax) * (pz - az) - (px - ax) * (bz - az)) / d;
                            const float slack = -0.05f;
                            if (u < slack || t < slack || u + t > 1 - slack) continue;
                            if (!In(i, j)) continue;
                            int k = j * _w + i;
                            if (!raw.TryGetValue(k, out var l)) raw[k] = l = new List<float>();
                            l.Add(ay + u * (by - ay) + t * (cy - ay));
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
                if (merged.Count > MaxFloors) merged.RemoveRange(0, merged.Count - MaxFloors);
                _floors[kv.Key] = merged.ToArray();
            }
        }

        private void StampWalls(NavCollision walls)
        {
            foreach (var ch in walls.Chunks)
            {
                float[] v = ch.Verts;
                for (int o = 0; o + 8 < v.Length; o += 9)
                {
                    float ax = v[o], ay = v[o + 1], az = v[o + 2];
                    float ux = v[o + 3] - ax, uy = v[o + 4] - ay, uz = v[o + 5] - az;
                    float wx = v[o + 6] - ax, wy = v[o + 7] - ay, wz = v[o + 8] - az;
                    float longest = Math.Max(Len(ux, uy, uz), Math.Max(Len(wx, wy, wz), Len(wx - ux, wy - uy, wz - uz)));
                    int n = Math.Min(400, Math.Max(1, (int)Math.Ceiling(longest / 0.25f)));
                    for (int i = 0; i <= n; i++)
                        for (int j = 0; j <= n - i; j++)
                        {
                            float s = i / (float)n, t = j / (float)n;
                            float px = ax + ux * s + wx * t, py = ay + uy * s + wy * t, pz = az + uz * s + wz * t;
                            int k = Key(px, pz);
                            if (k < 0 || !_floors.TryGetValue(k, out var fl)) continue;
                            for (int f = 0; f < fl.Length; f++)
                            {
                                float above = py - fl[f];
                                if (above >= BodyLow && above <= BodyHigh) _blocked.Add((long)k * 8 + f);
                            }
                        }
                }
            }
        }

        // A floor with another one just above it (under a deck, inside a slab) is no place to stand.
        private void StampHeadroom()
        {
            foreach (var kv in _floors)
                for (int f = 0; f + 1 < kv.Value.Length; f++)
                    if (kv.Value[f + 1] - kv.Value[f] < BodyHigh) _blocked.Add((long)kv.Key * 8 + f);
        }

        private static float Len(float x, float y, float z) => (float)Math.Sqrt(x * x + y * y + z * z);

        // ---- queries ------------------------------------------------------------------------------------------

        private int CellX(float x) => (int)Math.Floor(x / Cell) - _x0;
        private int CellZ(float z) => (int)Math.Floor(z / Cell) - _z0;
        private bool In(int i, int j) => i >= 0 && j >= 0 && i < _w && j < _h;
        private int Key(float x, float z) { int i = CellX(x), j = CellZ(z); return In(i, j) ? j * _w + i : -1; }
        private Vector3 Centre(int k, float y) => new Vector3((k % _w + _x0 + 0.5f) * Cell, y, (k / _w + _z0 + 0.5f) * Cell);

        // The standable floor in cell k nearest height y, within tol; -1 when none.
        private int FloorAt(int k, float y, float tol, HashSet<int> extra)
        {
            if (k < 0 || (extra != null && extra.Contains(k)) || !_floors.TryGetValue(k, out var fl)) return -1;
            int best = -1; float bd = tol;
            for (int f = 0; f < fl.Length; f++)
            {
                float d = Math.Abs(fl[f] - y);
                if (d <= bd && !_blocked.Contains((long)k * 8 + f)) { bd = d; best = f; }
            }
            return best;
        }

        private float Height(long node) => _floors[(int)(node / 8)][(int)(node % 8)];

        /// <summary>Standable ground at p (a floor within 3 m of p.Y) — the front-ray test for doorway exits.</summary>
        public bool OpenAt(Vector3 p) => FloorAt(Key(p.X, p.Z), p.Y, 3f, null) >= 0;

        public HashSet<int> CellsAlong(Vector3 a, Vector3 b, float radius, HashSet<int> into = null)
        {
            into = into ?? new HashSet<int>();
            float len = (float)Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Z - a.Z) * (b.Z - a.Z));
            int n = Math.Max(1, (int)Math.Ceiling(len / (Cell / 2)));
            int r = (int)Math.Ceiling(radius / Cell);
            for (int s = 0; s <= n; s++)
            {
                float t = s / (float)n;
                int ci = CellX(a.X + (b.X - a.X) * t), cj = CellZ(a.Z + (b.Z - a.Z) * t);
                for (int dj = -r; dj <= r; dj++)
                    for (int di = -r; di <= r; di++)
                        if (In(ci + di, cj + dj) && di * di + dj * dj <= r * r) into.Add((cj + dj) * _w + ci + di);
            }
            return into;
        }

        /// <summary>
        /// A path over the floors from a (on the floor nearest a.Y) to within reach metres of b. b.Y NaN: any
        /// level; otherwise the end must be on b's level (within 1.5 m). Points carry their floor height.
        /// </summary>
        public List<Vector3> FindPath(Vector3 a, Vector3 b, HashSet<int> extra, float snap, float reach, out string why)
        {
            why = "";
            long start = NearestNode(a, snap, extra);
            if (start < 0) { why = $"no floor within {snap:0} m of me at height {a.Y:0}"; return null; }
            int bi = CellX(b.X), bj = CellZ(b.Z);
            float rc = Math.Max(reach, Cell) / Cell;
            bool anyLevel = float.IsNaN(b.Y);
            bool AtGoal(long node)
            {
                int k = (int)(node / 8);
                float di = k % _w - bi, dj = k / _w - bj;
                return di * di + dj * dj <= rc * rc && (anyLevel || Math.Abs(Height(node) - b.Y) <= 1.5f);
            }

            var gScore = new Dictionary<long, float> { [start] = 0 };
            var parent = new Dictionary<long, long>();
            var closed = new HashSet<long>();
            var open = new PriorityQueue<long, float>();
            float H(int k) { int di = Math.Abs(k % _w - bi), dj = Math.Abs(k / _w - bj); return 1.2f * Math.Max(0f, Math.Max(di, dj) + 0.4142f * Math.Min(di, dj) - rc); }
            open.Enqueue(start, H((int)(start / 8)));
            long goal = -1;
            while (open.TryDequeue(out long cur, out _))
            {
                if (!closed.Add(cur)) continue;
                if (AtGoal(cur)) { goal = cur; break; }
                if (closed.Count > MaxExpand) { why = $"searched {MaxExpand} floor cells without reaching it"; return null; }
                int k = (int)(cur / 8), i = k % _w, j = k / _w;
                float y = Height(cur), gc = gScore[cur];
                for (int dj = -1; dj <= 1; dj++)
                    for (int di = -1; di <= 1; di++)
                    {
                        if (di == 0 && dj == 0 || !In(i + di, j + dj)) continue;
                        int nk = (j + dj) * _w + i + di;
                        int f = FloorAt(nk, y, MaxStep, extra);
                        if (f < 0) continue;
                        if (di != 0 && dj != 0 && (FloorAt(j * _w + i + di, y, MaxStep, extra) < 0 || FloorAt((j + dj) * _w + i, y, MaxStep, extra) < 0)) continue;
                        long nn = (long)nk * 8 + f;
                        if (closed.Contains(nn)) continue;
                        float ng = gc + (di != 0 && dj != 0 ? 1.4142f : 1f);
                        if (gScore.TryGetValue(nn, out float old) && old <= ng) continue;
                        gScore[nn] = ng; parent[nn] = cur;
                        open.Enqueue(nn, ng + H(nk));
                    }
            }
            if (goal < 0) { why = $"no walkway from my level to within {reach:0.0} m of ({b.X:0},{(anyLevel ? "?" : b.Y.ToString("0"))},{b.Z:0})"; return null; }

            var nodes = new List<long>();
            for (long c = goal; ; c = parent[c]) { nodes.Add(c); if (c == start) break; }
            nodes.Reverse();

            var pts = new List<Vector3> { a };
            int i0 = 0;
            while (i0 < nodes.Count - 1)
            {
                int j = nodes.Count - 1;
                while (j > i0 + 1 && !Clear(nodes[i0], nodes[j], extra)) j--;
                pts.Add(Centre((int)(nodes[j] / 8), Height(nodes[j])));
                i0 = j;
            }
            int bk = Key(b.X, b.Z);
            if (bk >= 0 && goal / 8 == bk) pts[pts.Count - 1] = new Vector3(b.X, Height(goal), b.Z);
            return pts;
        }

        private long NearestNode(Vector3 p, float maxR, HashSet<int> extra)
        {
            int ci = CellX(p.X), cj = CellZ(p.Z);
            int R = (int)Math.Ceiling(maxR / Cell);
            for (int r = 0; r <= R; r++)
            {
                long best = -1; float bd = float.MaxValue;
                for (int dj = -r; dj <= r; dj++)
                    for (int di = -r; di <= r; di++)
                    {
                        if (Math.Max(Math.Abs(di), Math.Abs(dj)) != r || !In(ci + di, cj + dj)) continue;
                        int k = (cj + dj) * _w + ci + di;
                        int f = FloorAt(k, p.Y, 2.5f, extra);
                        if (f < 0) continue;
                        float d = di * di + dj * dj + Math.Abs(_floors[k][f] - p.Y);
                        if (d < bd) { bd = d; best = (long)k * 8 + f; }
                    }
                if (best >= 0) return best;
            }
            return -1;
        }

        // Walkable in a straight line: every sample (and a body's width either side) has a floor within MaxStep
        // of the one before it, so the line neither leaves the platform nor changes level.
        private bool Clear(long n0, long n1, HashSet<int> extra)
        {
            int k0 = (int)(n0 / 8), k1 = (int)(n1 / 8);
            float x0 = k0 % _w + 0.5f, z0 = k0 / _w + 0.5f, x1 = k1 % _w + 0.5f, z1 = k1 / _w + 0.5f;
            float len = (float)Math.Sqrt((x1 - x0) * (x1 - x0) + (z1 - z0) * (z1 - z0));
            if (len < 1e-3f) return true;
            float px = -(z1 - z0) / len * 0.6f, pz = (x1 - x0) / len * 0.6f;   // 0.3 m either side, in cells
            int n = Math.Max(1, (int)Math.Ceiling(len * 3));
            float y = Height(n0);
            for (int s = 1; s <= n; s++)
            {
                float t = s / (float)n, x = x0 + (x1 - x0) * t, z = z0 + (z1 - z0) * t;
                int f = FloorAt(Idx(x, z), y, MaxStep, extra);
                if (f < 0) return false;
                y = _floors[Idx(x, z)][f];
                if (FloorAt(Idx(x + px, z + pz), y, MaxStep, extra) < 0 || FloorAt(Idx(x - px, z - pz), y, MaxStep, extra) < 0) return false;
            }
            return true;
        }

        private int Idx(float ci, float cj) { int i = (int)Math.Floor(ci), j = (int)Math.Floor(cj); return In(i, j) ? j * _w + i : -1; }
    }
}