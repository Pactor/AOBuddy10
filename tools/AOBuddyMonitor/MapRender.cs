using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AOBuddy;
using Newtonsoft.Json;

namespace AOBuddyMonitor
{
    /// <summary>
    /// The map background: one pixel per heightfield cell, coloured by the bot's own answers — the same
    /// ground.bin, the same SwimY verdict tools/navmap renders. When the extractor dropped a tilecolors.bin
    /// beside ground.bin (per-tile texture medians, pulled from the client's own ground record), the
    /// terrain paints in the zone's real colours × height shading; without it the height grey ramp stands
    /// in. Linked against AOBuddy.csproj on purpose so this can never drift from what the bot walks on
    /// (the RDB-sweep higher-resolution terrain, when it comes, lands in the same files and shows up here
    /// unchanged). The water sweep is not free, so renders happen on a background task and land in the
    /// cache when ready.
    /// </summary>
    public sealed class MapRender
    {
        public sealed class Terrain
        {
            public int Pf;
            public string Name = "";
            public int W, H;                     // cells (pixels)
            public float Cell;                  // metres per cell
            public byte[] Bgra;                 // W*H*4, for a WriteableBitmap
        }

        private readonly object _lock = new object();
        private readonly Dictionary<int, Terrain> _done = new Dictionary<int, Terrain>();
        private readonly HashSet<int> _loading = new HashSet<int>();
        private readonly List<int> _doneOrder = new List<int>();   // eviction order: terrain.png zones are
                                                                    // 8192x7168 BGRA (~235 MB) — keep few
        private readonly string _pluginDir;
        private DateTime _loadedAt;
        private Dictionary<int, List<List<float[]>>> _steps = new Dictionary<int, List<List<float[]>>>();

        public MapRender(string pluginDir) { _pluginDir = pluginDir; }

        /// <summary>The rendered terrain for a playfield, or null while it loads / when there is none
        /// (mission interiors and unextracted zones draw on the plain coordinate grid instead).</summary>
        public Terrain Get(int pf)
        {
            lock (_lock)
            {
                if (_done.TryGetValue(pf, out var t)) return t;
                if (!_loading.Contains(pf) && _pluginDir.Length > 0)
                {
                    _loading.Add(pf);
                    Task.Run(() =>
                    {
                        var rendered = Render(pf);
                        lock (_lock)
                        {
                            _loading.Remove(pf);
                            if (rendered != null)
                            {
                                _done[pf] = rendered;
                                _doneOrder.Remove(pf); _doneOrder.Add(pf);
                                while (_doneOrder.Count > 2) { _done.Remove(_doneOrder[0]); _doneOrder.RemoveAt(0); }
                            }
                        }
                        Rendered?.Invoke(pf);
                    });
                }
                return null;
            }
        }

        /// <summary>Fired on the background thread when a terrain finishes; the view invalidates itself.</summary>
        public event Action<int> Rendered;

        /// <summary>The owner's recorded footsteps (the bot's road network) for a playfield, as walked
        /// polylines. nav/&lt;pf&gt;.json is the bot's own file and may be mid-write — a failed read just
        /// yields no layer this round (re-read on the next playfield change).</summary>
        public List<List<float[]>> Footsteps(int pf)
        {
            if (_pluginDir.Length == 0) return null;
            string file = Path.Combine(_pluginDir, "nav", pf + ".json");
            try
            {
                var at = File.GetLastWriteTimeUtc(file);
                lock (_lock)
                    if (at == _loadedAt && _steps.TryGetValue(pf, out var cached))
                        return cached;
                var parsed = JsonConvert.DeserializeObject<WalkedFile>(File.ReadAllText(file));
                var segs = parsed?.Segments?.Where(s => s != null).ToList() ?? new List<List<float[]>>();
                lock (_lock) { _loadedAt = at; _steps[pf] = segs; }
                return segs;
            }
            catch { return null; }
        }

        private sealed class WalkedFile { public List<List<float[]>> Segments; }

        // ---- mission floor plans (2026-09-26) ------------------------------------------------------------
        // A mission building is not in the client data: the bot composes it from the zone-in placement and
        // the pool's rooms.json (AOBuddyNav.ComposeMission), and /nav carries just that placement. Here the
        // SAME composition runs against this machine's GameData, then each floor becomes a bitmap: one pixel
        // per walkable room cell, walls baked in as brighter lines (nav.Walls, world triangles filtered to
        // the floor's height band). All floors render on a background task; the plan pops in when ready.

        public sealed class MissionPlan
        {
            public int Instance;
            public float Cell;
            public float MinX, MinZ;                    // world bounds of the walkable cells (shared by all floors)
            public int W, H;                            // bitmap pixels
            public int PxPerCell = 1;                   // supersampling (10): crisp at dungeon zoom, no blur
            public int[] Floors = new int[0];
            public readonly List<RoomLabel> Rooms = new List<RoomLabel>();
            public float[] ExitXZ;                      // world [x, z] of the way out, on ExitFloor
            public int ExitFloor;
            public string Name = "";
            internal readonly Dictionary<int, byte[]> FloorBgra = new Dictionary<int, byte[]>();
        }

        public sealed class RoomLabel { public readonly string Name; public readonly int Floor; public readonly float X, Z; public RoomLabel(string n, int f, float x, float z) { Name = n; Floor = f; X = x; Z = z; } }

        private readonly object _mLock = new object();
        private readonly Dictionary<int, MissionPlan> _missions = new Dictionary<int, MissionPlan>();
        private readonly HashSet<int> _missionLoading = new HashSet<int>();

        /// <summary>The composed plan for a mission layout, or null while it builds / when composition fails.
        /// Fires <see cref="Rendered"/> (on the background thread) when a plan lands.</summary>
        public MissionPlan GetMission(BotClient.MissionLayout lay)
        {
            if (lay == null || _pluginDir.Length == 0) return null;
            lock (_mLock)
            {
                if (_missions.TryGetValue(lay.Instance, out var p)) return p;
                if (_missionLoading.Contains(lay.Instance)) return null;
                _missionLoading.Add(lay.Instance);
            }
            System.Threading.Tasks.Task.Run(() =>
            {
                var plan = BuildMission(lay);
                lock (_mLock)
                {
                    _missionLoading.Remove(lay.Instance);
                    if (plan != null) _missions[lay.Instance] = plan;
                }
                if (plan != null) Rendered?.Invoke(lay.Instance);
            });
            return null;
        }

        private MissionPlan BuildMission(BotClient.MissionLayout lay)
        {
            try
            {
                var ml = new AOBuddyNav.MissionLayout
                {
                    Instance = lay.Instance, TemplatePlayfield = lay.PoolPf,
                    Width = lay.Width, Height = lay.Height, WorldHeight = lay.WorldHeight,
                    LandX = lay.LandX, LandY = lay.LandY, LandZ = lay.LandZ,
                };
                foreach (var r in lay.Rooms) ml.Rooms.Add(r);
                var nav = AOBuddyNav.ComposeMission(_pluginDir, ml);
                var d = nav?.Dungeon;
                if (d == null || d.Rooms.Count == 0) return null;

                // cell → world, the exact inverse of NavDungeon.CellOf: local offset from the rect's centre,
                // turned back by the room's rotation (CellOf un-rotates world→local; this re-rotates local→world)
                float cell = d.Cell;
                void Walk(NavDungeon.Room rm, Action<int, int, double, double> cellAt)
                {
                    int turns = ((-rm.Rot) % 4 + 4) % 4;
                    double ccx = (rm.Rect[0] + rm.Rect[2] + 1) / 2.0, ccz = (rm.Rect[1] + rm.Rect[3] + 1) / 2.0;
                    for (int row = 0; row < rm.Tile.Length; row++)
                        for (int col = 0; col < rm.Tile[row].Length; col++)
                        {
                            if (rm.Tile[row][col] == 0) continue;
                            int a = rm.Rect[0] + col, b = rm.Rect[1] + row;
                            double dx = (a + 0.5 - ccx) * cell, dz = (b + 0.5 - ccz) * cell;
                            for (int i = 0; i < turns; i++) { double t = dx; dx = -dz; dz = t; }
                            cellAt(a, b, rm.Pos[0] + dx, rm.Pos[2] + dz);
                        }
                }

                // pass 1: world bounds over every walkable cell (all floors share the grid, so floors align)
                double minX = double.MaxValue, minZ = double.MaxValue, maxX = double.MinValue, maxZ = double.MinValue;
                foreach (var rm in d.Rooms)
                    Walk(rm, (a, b, x, z) => { if (x < minX) minX = x; if (x > maxX) maxX = x; if (z < minZ) minZ = z; if (z > maxZ) maxZ = z; });
                if (minX > maxX) return null;
                // ×10 supersampling: a building is ~100 m against a 4 km outdoor zone, so the view zooms
                // ~×10 on entry (MapView does that) — one pixel per 2 m cell would blur to mush there.
                // Back off only if a pathological pool would break the 4096-px bitmap cap.
                int cw = Math.Max(1, (int)Math.Ceiling((maxX - minX) / cell));
                int ch = Math.Max(1, (int)Math.Ceiling((maxZ - minZ) / cell));
                int s = 10;
                while (s > 1 && Math.Max(cw, ch) * s > 4096) s--;
                var plan = new MissionPlan
                {
                    Instance = lay.Instance, Cell = cell, Name = nav.Name,
                    MinX = (float)minX, MinZ = (float)minZ,
                    W = cw * s, H = ch * s, PxPerCell = s,
                    Floors = d.Rooms.Select(r => r.Floor).Distinct().OrderBy(f => f).ToArray(),
                };
                if (nav.Exit != null) { plan.ExitXZ = new[] { (float)nav.Exit.X, (float)nav.Exit.Z }; plan.ExitFloor = nav.Exit.Floor; }

                // per floor: paint the walkable cells (a shade per room, so rooms read as rooms)…
                foreach (int floor in plan.Floors)
                {
                    var img = new byte[plan.W * plan.H * 4];
                    foreach (var rm in d.Rooms)
                    {
                        if (rm.Floor != floor) continue;
                        int v = 56 + (rm.PoolIndex * 37 % 26);
                        Walk(rm, (a, b, x, z) =>
                        {
                            int px = (int)((x - minX) / cell * s), py = (int)((z - minZ) / cell * s);
                            for (int dz = 0; dz < s; dz++)
                                for (int dx = 0; dx < s; dx++)
                                {
                                    int qx = px + dx, qy = py + dz;
                                    if (qx < 0 || qy < 0 || qx >= plan.W || qy >= plan.H) continue;
                                    int i = (qy * plan.W + qx) * 4;
                                    img[i] = (byte)v; img[i + 1] = (byte)v; img[i + 2] = (byte)(v + 4); img[i + 3] = 255;
                                }
                        });
                        // the label at the room's walkable centroid, not its pivot — rotated rooms put the pivot oddly
                        double sx = 0, sz = 0; int n = 0;
                        Walk(rm, (a, b, x, z) => { sx += x; sz += z; n++; });
                        if (n > 0) plan.Rooms.Add(new RoomLabel(rm.PoolName, floor, (float)(sx / n), (float)(sz / n)));
                    }

                    // …then bake the walls: nav.Walls' triangles whose height sits in this floor's band
                    var onFloor = d.Rooms.Where(r => r.Floor == floor).ToList();
                    if (onFloor.Count > 0 && nav.Walls != null)
                    {
                        float y0 = onFloor.Min(r => r.Pos[1]) - 1f, y1 = onFloor.Min(r => r.Pos[1]) + Math.Max(3f, lay.WorldHeight * 0.8f);
                        void Line(double ax, double az, double bx, double bz)
                        {
                            int x0 = (int)Math.Round((ax - minX) / cell * s), z0 = (int)Math.Round((az - minZ) / cell * s);
                            int x1 = (int)Math.Round((bx - minX) / cell * s), z1 = (int)Math.Round((bz - minZ) / cell * s);
                            int steps = Math.Max(Math.Abs(x1 - x0), Math.Abs(z1 - z0));
                            if (steps > 20000) return;
                            int w = Math.Max(1, s / 4);                       // wall thickness scales with the supersampling
                            for (int st = 0; st <= steps; st++)
                            {
                                int px = x0 + (x1 - x0) * st / Math.Max(1, steps), py = z0 + (z1 - z0) * st / Math.Max(1, steps);
                                for (int dz = 0; dz < w; dz++)
                                    for (int dx = 0; dx < w; dx++)
                                    {
                                        int qx = px + dx, qy = py + dz;
                                        if (qx < 0 || qy < 0 || qx >= plan.W || qy >= plan.H) continue;
                                        int i = (qy * plan.W + qx) * 4;
                                        img[i] = 130; img[i + 1] = 130; img[i + 2] = 148; img[i + 3] = 255;
                                    }
                            }
                        }
                        for (int t = 0; t + 8 < nav.Walls.Length; t += 9)
                        {
                            bool inBand = true;
                            for (int k = 0; k < 3; k++) if (nav.Walls[t + k * 3 + 1] < y0 || nav.Walls[t + k * 3 + 1] > y1) { inBand = false; break; }
                            if (!inBand) continue;
                            for (int k = 0; k < 3; k++)
                            {
                                int a0 = t + k * 3, a1 = t + ((k + 1) % 3) * 3;
                                Line(nav.Walls[a0], nav.Walls[a0 + 2], nav.Walls[a1], nav.Walls[a1 + 2]);
                            }
                        }
                    }
                    lock (plan.FloorBgra) plan.FloorBgra[floor] = img;
                }
                return plan;
            }
            catch { return null; }
        }

        /// <summary>Decodes the extractor's terrain.png (8-bit truecolour, our own writer) to world-ordered
        /// BGRA — un-flipping the game-oriented rows on the way in. null when anything about it surprises us.</summary>
        private static (int w, int h, byte[] bgra)? DecodePng(byte[] all)
        {
            try
            {
                if (all.Length < 8 || all[0] != 0x89 || all[1] != (byte)'P') return null;
                int p = 8, w = 0, h = 0, bd = 0, ct = 0;
                var idat = new List<byte[]>();
                while (p + 12 <= all.Length)
                {
                    int len = (all[p] << 24) | (all[p + 1] << 16) | (all[p + 2] << 8) | all[p + 3];
                    string typ = System.Text.Encoding.ASCII.GetString(all, p + 4, 4);
                    if (typ == "IHDR") { w = Be32(all, p + 8); h = Be32(all, p + 12); bd = all[p + 16]; ct = all[p + 17]; }
                    else if (typ == "IDAT") { var c = new byte[len]; Buffer.BlockCopy(all, p + 8, c, 0, len); idat.Add(c); }
                    else if (typ == "IEND") break;
                    p += 12 + len;
                }
                if (w <= 0 || h <= 0 || bd != 8 || ct != 2 || idat.Count == 0) return null;
                int zlen = 0; foreach (var c in idat) zlen += c.Length;
                var z = new byte[zlen]; int zo = 0;
                foreach (var c in idat) { Buffer.BlockCopy(c, 0, z, zo, c.Length); zo += c.Length; }
                byte[] raw;
                using (var ds = new System.IO.Compression.DeflateStream(new MemoryStream(z, 2, zlen - 2), System.IO.Compression.CompressionMode.Decompress))
                using (var o = new MemoryStream()) { ds.CopyTo(o); raw = o.ToArray(); }
                int stride = w * 3;
                if (raw.Length < h * (stride + 1)) return null;
                // the extractor writes up to 16 K wide (texsheet-exact texels); halve those so the bitmap
                // stays ~235 MB instead of ~940
                int step = 1;
                while (w / step > 8192) step *= 2;
                int dw = w / step, dh = h / step;
                var bgra = new byte[dw * dh * 4];
                var prev = new byte[stride];
                var cur = new byte[stride];
                for (int y = 0; y < h; y++)
                {
                    int f = raw[y * (stride + 1)];
                    Buffer.BlockCopy(raw, y * (stride + 1) + 1, cur, 0, stride);
                    if (f != 0) Unfilter(f, cur, prev, stride);
                    if (y % step == 0)
                    {
                        int worldRow = dh - 1 - y / step;         // the extractor wrote game-oriented rows
                        for (int x = 0; x < dw; x++)
                        {
                            int si = x * step * 3, di = (worldRow * dw + x) * 4;
                            bgra[di] = cur[si + 2]; bgra[di + 1] = cur[si + 1]; bgra[di + 2] = cur[si]; bgra[di + 3] = 255;
                        }
                    }
                    var t = prev; prev = cur; cur = t;
                }
                return (dw, dh, bgra);
            }
            catch { return null; }
        }

        private static int Be32(byte[] b, int at) => (b[at] << 24) | (b[at + 1] << 16) | (b[at + 2] << 8) | b[at + 3];

        private static void Unfilter(int f, byte[] cur, byte[] prev, int n)
        {
            for (int i = 0; i < n; i++)
            {
                int a = i >= 3 ? cur[i - 3] : 0, b = prev[i], c = i >= 3 ? prev[i - 3] : 0;
                switch (f)
                {
                    case 1: cur[i] = (byte)(cur[i] + a); break;
                    case 2: cur[i] = (byte)(cur[i] + b); break;
                    case 3: cur[i] = (byte)(cur[i] + (a + b) / 2); break;
                    case 4:
                        int pp = a + b - c, pa = Math.Abs(pp - a), pb = Math.Abs(pp - b), pc = Math.Abs(pp - c);
                        cur[i] = (byte)(cur[i] + (pa <= pb && pa <= pc ? a : pb <= pc ? b : c));
                        break;
                }
            }
        }

        // ---- the render (navmap's colour logic: terrain grey, water blue, 256 m grid) ---------------------

        private Terrain Render(int pf)
        {
            try
            {
                var nav = AOBuddyNav.Load(_pluginDir, pf);
                var g = nav?.Ground;
                if (g == null) return null;
                int w = g.SamplesX - 1, h = g.SamplesZ - 1;
                if (w <= 0 || h <= 0) return null;
                float cell = g.Cell;

                // the extractor's terrain.png when it wrote one: the zone's per-patch ground textures
                // (one 16x16 per 256 m patch), water already baked blue. Its rows are game-oriented;
                // the decoder un-flips so MakeBitmap's world-order flip lands right.
                string terPath = Path.Combine(AOBuddyNav.FolderFor(_pluginDir, pf), "terrain.png");
                if (File.Exists(terPath))
                {
                    var dec = DecodePng(File.ReadAllBytes(terPath));
                    if (dec.HasValue)
                        return new Terrain { Pf = pf, Name = nav.Name, W = dec.Value.w, H = dec.Value.h,
                            Cell = w * cell / dec.Value.w, Bgra = dec.Value.bgra };
                }
                float Low(int iz, int ix) => g.Heights[iz * g.SamplesX + ix] * g.HeightScale;
                float CornerMin(int iz, int ix) => Math.Min(Math.Min(Low(iz, ix), Low(iz, ix + 1)), Math.Min(Low(iz + 1, ix), Low(iz + 1, ix + 1)));

                // ask SwimY once so its lazy water build happens on this thread, then everywhere
                g.SwimY(cell / 2, cell / 2, 0.3);

                float hmin = float.MaxValue, hmax = float.MinValue;
                foreach (ushort v in g.Heights) { float hv = v * g.HeightScale; if (hv < hmin) hmin = hv; if (hv > hmax) hmax = hv; }

                var img = new byte[w * h * 4];         // BGRA
                byte[] tc = g.TileColors;              // the ground's own colours when the extractor wrote tilecolors.bin
                for (int iz = 0; iz < h; iz++)
                    for (int ix = 0; ix < w; ix++)
                    {
                        int i = iz * w + ix;
                        float t = (CornerMin(iz, ix) - hmin) / Math.Max(0.1f, hmax - hmin);
                        int r, gr, b;
                        if (tc != null)
                        {
                            // the tile's texture median, shaded by height — dimmed overall, it is a background
                            int ti = (g.Tiles[i] & 0xFF) * 3;
                            double bright = 0.55 + 0.45 * t;
                            r = (int)Math.Clamp(tc[ti] * bright, 0, 255);
                            gr = (int)Math.Clamp(tc[ti + 1] * bright, 0, 255);
                            b = (int)Math.Clamp(tc[ti + 2] * bright, 0, 255);
                        }
                        else
                        {
                            int v = 38 + (int)(150 * t);   // a shade darker than navmap: it is a background here
                            r = v; gr = v; b = v;
                        }
                        double sw = g.SwimY((ix + 0.5f) * cell, (iz + 0.5f) * cell, 0.3);
                        if (!double.IsNaN(sw))
                        {
                            float depth = (float)(sw - g.HeightAt((ix + 0.5f) * cell, (iz + 0.5f) * cell));
                            int a = Math.Min(215, 110 + (int)(depth * 22));
                            r += (50 - r) * a / 255; gr += (100 - gr) * a / 255; b += (230 - b) * a / 255;
                        }
                        img[i * 4] = (byte)b; img[i * 4 + 1] = (byte)gr; img[i * 4 + 2] = (byte)r; img[i * 4 + 3] = 255;
                    }

                // a grid line every 256 m, lightened like navmap's, keeps the sense of scale when zoomed in
                int step = Math.Max(1, (int)Math.Round(256 / cell));
                for (int x = 0; x < w; x += step) for (int z = 0; z < h; z++) Lighten(z * w + x);
                for (int z = 0; z < h; z += step) for (int x = 0; x < w; x++) Lighten(z * w + x);
                void Lighten(int i)
                {
                    img[i * 4] = (byte)Math.Min(255, img[i * 4] + 20);
                    img[i * 4 + 1] = (byte)Math.Min(255, img[i * 4 + 1] + 20);
                    img[i * 4 + 2] = (byte)Math.Min(255, img[i * 4 + 2] + 20);
                }

                return new Terrain { Pf = pf, Name = nav.Name, W = w, H = h, Cell = cell, Bgra = img };
            }
            catch { return null; }
        }
    }
}
