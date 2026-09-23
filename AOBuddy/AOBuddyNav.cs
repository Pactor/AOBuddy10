using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using AOSharp.Common.GameData;
using Newtonsoft.Json;

namespace AOBuddy
{
    /// <summary>
    /// Reader for the per-playfield navigation data in GameData/Nav/&lt;pf&gt;/ (formats: GameData/Nav/README.md,
    /// how they were found: NAV-CLIENTDATA.md). Loads a playfield's outdoor heightfield, dungeon room list
    /// and near-horizontal collision triangles, and answers "where is the floor under (x, z)?".
    ///
    /// NOT wired into any controller. The only caller today is the 'navdata' chat command in Main, which
    /// exists so the data can be checked against the live character before anything relies on it.
    /// Nothing here moves the body.
    /// </summary>
    public sealed class AOBuddyNav
    {
        public readonly int Playfield;
        public readonly string Kind;               // outdoor / dungeon / none
        public readonly string Name;
        public readonly NavGround Ground;          // outdoor only
        public readonly NavDungeon Dungeon;        // dungeon only
        public readonly NavCollision Collision;    // whenever the client has surfaces for the zone

        AOBuddyNav(int pf, string kind, string name, NavGround g, NavDungeon d, NavCollision c)
        { Playfield = pf; Kind = kind; Name = name; Ground = g; Dungeon = d; Collision = c; }

        public static string FolderFor(string pluginDir, int pf) => Path.Combine(pluginDir, "GameData", "Nav", pf.ToString());

        /// <summary>Load one playfield's folder. Returns null when there is no folder for it.</summary>
        public static AOBuddyNav Load(string pluginDir, int pf)
        {
            string dir = FolderFor(pluginDir, pf);
            string infoPath = Path.Combine(dir, "info.json");
            if (!File.Exists(infoPath)) return null;
            var info = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(infoPath));
            string kind = info.TryGetValue("kind", out var k) ? k as string : "none";
            string name = info.TryGetValue("name", out var n) ? n as string : "";
            NavGround g = null; NavDungeon d = null; NavCollision c = null;
            string gp = Path.Combine(dir, "ground.bin"), rp = Path.Combine(dir, "rooms.json"), cp = Path.Combine(dir, "collision.bin");
            if (File.Exists(gp)) g = NavGround.Read(gp);
            if (File.Exists(rp)) d = NavDungeon.Read(rp);
            if (File.Exists(cp)) c = NavCollision.Read(cp);
            return new AOBuddyNav(pf, kind, name, g, d, c);
        }

        /// <summary>
        /// The floor height under (x, z) nearest to a known height y (the character's own Y, say):
        /// tiles/heightfield first, then collision triangles. NaN when nothing is under the point.
        /// </summary>
        public double FloorNear(double x, double y, double z, out string source)
        {
            double best = double.NaN; string src = "none";
            void Consider(double h, string s) { if (!double.IsNaN(h) && (double.IsNaN(best) || Math.Abs(h - y) < Math.Abs(best - y))) { best = h; src = s; } }
            if (Ground != null) Consider(Ground.HeightAt(x, z), "ground");
            if (Dungeon != null)
                foreach (var rm in Dungeon.Rooms)
                {
                    double h = Dungeon.FloorHeight(rm, x, z);
                    if (!double.IsNaN(h)) Consider(h, "room " + rm.Index + (rm.Name.Length > 0 ? " " + rm.Name : ""));
                }
            if (Collision != null)
                foreach (double h in Collision.HeightsUnder(x, z)) Consider(h, "collision");
            source = src;
            return best;
        }

        /// <summary>Everything the data says about a point, for the 'navdata' command.</summary>
        public string Explain(double x, double y, double z)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("pf {0} {1} ({2}): ", Playfield, Name, Kind);
            if (Ground != null)
            {
                double h = Ground.HeightAt(x, z);
                sb.AppendFormat("ground {0} tile {1} bld {2}; ", double.IsNaN(h) ? "outside" : h.ToString("0.00"), Ground.TileAt(x, z), Ground.BuildingAt(x, z));
            }
            if (Dungeon != null)
            {
                int hits = 0;
                foreach (var rm in Dungeon.Rooms)
                {
                    double h = Dungeon.FloorHeight(rm, x, z);
                    if (double.IsNaN(h)) continue;
                    hits++;
                    if (hits <= 3) sb.AppendFormat("room {0} '{1}' floor {2:0.00}; ", rm.Index, rm.Name, h);
                }
                if (hits == 0) sb.Append("no room tile here; ");
            }
            if (Collision != null)
            {
                var hs = new List<double>(Collision.HeightsUnder(x, z));
                hs.Sort();
                sb.AppendFormat("collision {0} surface(s)", hs.Count);
                for (int i = 0; i < hs.Count && i < 6; i++) sb.AppendFormat(" {0:0.00}", hs[i]);
                sb.Append("; ");
            }
            double best = FloorNear(x, y, z, out string src);
            sb.AppendFormat("nearest floor to y={0:0.00}: {1} ({2})", y, double.IsNaN(best) ? "none" : best.ToString("0.00"), src);
            return sb.ToString();
        }

        /// <summary>
        /// The acceptance test the formats were built with: every point in the bot's own walked record for
        /// this playfield (nav/&lt;pf&gt;.json) against the data, within tol metres.
        /// </summary>
        public string SelfTest(string navFile, double tol = 1.0)
        {
            if (!File.Exists(navFile)) return "no walked record " + navFile;
            var zone = JsonConvert.DeserializeObject<WalkedFile>(File.ReadAllText(navFile));
            int total = 0, floor = 0, coll = 0, none = 0;
            var samples = new List<string>();
            if (zone?.Segments != null)
                foreach (var seg in zone.Segments)
                    foreach (var p in seg)
                    {
                        if (p == null || p.Length < 3) continue;
                        total++;
                        double x = p[0], y = p[1], z = p[2];
                        bool ok = false;
                        if (Ground != null) { double h = Ground.HeightAt(x, z); ok = !double.IsNaN(h) && Math.Abs(h - y) <= tol; }
                        else if (Dungeon != null)
                            foreach (var rm in Dungeon.Rooms) { double h = Dungeon.FloorHeight(rm, x, z); if (!double.IsNaN(h) && Math.Abs(h - y) <= tol) { ok = true; break; } }
                        if (ok) { floor++; continue; }
                        if (Collision != null) foreach (double h in Collision.HeightsUnder(x, z)) if (Math.Abs(h - y) <= tol) { ok = true; break; }
                        if (ok) { coll++; continue; }
                        none++;
                        if (samples.Count < 5) samples.Add(string.Format("({0:0},{1:0.0},{2:0})", x, y, z));
                    }
            if (total == 0) return "walked record has no points";
            return string.Format("{0} walked points: {1} on floor data, {2} on collision, {3} unexplained ({4:0.0}% explained){5}",
                total, floor, coll, none, 100.0 * (total - none) / total, samples.Count > 0 ? " e.g. " + string.Join(" ", samples) : "");
        }

        sealed class WalkedFile { public List<List<float[]>> Segments; }

        // ---- missions ----------------------------------------------------------------------------------------
        // A mission instance is not in the client data. The zone-in packet (PlayfieldAnarchyF, version 4)
        // carries BuildingGeneratorData: the template playfield (a pool such as 320 Midtech), the building's
        // slot grid, the floor height, and one (room, floor, x, z, rotation) per placed room. Verified on three
        // pools against 425 walked points (NAV-CLIENTDATA.md): a placed room is the pool room with its stored
        // rotation, x origin = X * 10 m, far z edge = (gridHeight - Z) * 10 m, y = pool height + floor * worldHeight.

        public sealed class MissionLayout
        {
            public int Instance, TemplatePlayfield, Width, Height, WorldHeight;
            public List<int[]> Rooms = new List<int[]>();   // room, floor, x, z, rotation
        }

        /// <summary>Decode the building block of a raw zone-in packet; null when the packet has none (outdoor, static dungeon).</summary>
        public static MissionLayout DecodeZoneIn(byte[] b)
        {
            if (b == null || b.Length < 0x60) return null;
            int p = 0x10;
            int I32() { int v = (b[p] << 24) | (b[p + 1] << 16) | (b[p + 2] << 8) | b[p + 3]; p += 4; return v; }
            short I16() { short v = (short)((b[p] << 8) | b[p + 1]); p += 2; return v; }
            I32(); I32(); I32(); p++;                                   // message type, identity, unknown
            int version = I32(); p += 12;                                // version, landing coordinates
            if (version <= 3) return null;
            p++; int modelType = I32(), modelInst = I32(); I32(); I32(); I32(); I32();
            int genType = I32(), genInst = I32();
            if (genType != 51103) return null;                           // 0xC79F ACGBuildingGeneratorData
            I32();                                                       // revision
            var m = new MissionLayout { Instance = modelInst, TemplatePlayfield = 0 };
            I16(); m.Width = I16(); m.Height = I16(); m.WorldHeight = I16(); m.TemplatePlayfield = I32(); p += 3;
            int n = I32();
            if (n < 0 || n > 512 || p + n * 6 > b.Length) return null;
            for (int i = 0; i < n; i++)
            {
                short room = I16(); sbyte floor = (sbyte)b[p++]; int x = b[p++], z = b[p++], rot = b[p++];
                m.Rooms.Add(new int[] { room, floor, x, z, rot });
            }
            return m;
        }

        /// <summary>Compose a mission instance's rooms from its zone-in packet and the template pool's rooms.json.</summary>
        public static AOBuddyNav LoadMission(string pluginDir, byte[] zoneInPacket)
        {
            var m = DecodeZoneIn(zoneInPacket);
            if (m == null) return null;
            string poolPath = Path.Combine(FolderFor(pluginDir, m.TemplatePlayfield), "rooms.json");
            if (!File.Exists(poolPath)) return null;
            NavDungeon pool = NavDungeon.Read(poolPath);
            const double slot = 10.0;
            var d = new NavDungeon { Playfield = m.Instance, Name = "mission from " + pool.Name, Tilemap = pool.Tilemap, Cell = pool.Cell, HeightScale = pool.HeightScale, Atlas = pool.Atlas, Rooms = new List<NavDungeon.Room>() };
            foreach (var pr in m.Rooms)
            {
                int idx = pr[0], floor = pr[1], X = pr[2], Z = pr[3], rot = pr[4];
                if (idx < 0 || idx >= pool.Rooms.Count) continue;
                var src = pool.Rooms[idx];
                int w = src.Rect[2] - src.Rect[0] + 1, h = src.Rect[3] - src.Rect[1] + 1;
                int tw = rot % 2 == 0 ? w : h, th = rot % 2 == 0 ? h : w;   // footprint after rotation
                double ox = X * slot, oz = (m.Height - Z) * slot - th * pool.Cell;
                d.Rooms.Add(new NavDungeon.Room
                {
                    Index = d.Rooms.Count, Name = src.Name + " f" + floor, Flags = src.Flags, Rot = rot, Rect = src.Rect,
                    Pos = new[] { (float)(ox + tw * pool.Cell / 2.0), src.Pos[1] + floor * m.WorldHeight, (float)(oz + th * pool.Cell / 2.0) },
                    HeightBase = src.HeightBase, Doors = src.Doors, Polys = src.Polys, Tile = src.Tile, Height = src.Height, Flags3 = src.Flags3
                });
            }
            return new AOBuddyNav(m.Instance, "mission", d.Name, null, d, null);
        }
    }

    /// <summary>ground.bin (AONG v2): outdoor heightfield + tile ids + building nibbles.</summary>
    public sealed class NavGround
    {
        public int SamplesX, SamplesZ, SourceBits;
        public float Cell, HeightScale;
        public ushort[] Heights;     // [z * SamplesX + x], height = value * HeightScale
        public ushort[] Tiles;       // [(SamplesZ-1) * (SamplesX-1)]
        public byte[] Building;

        public static NavGround Read(string path)
        {
            using (var r = new BinaryReader(File.OpenRead(path)))
            {
                if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "AONG") throw new InvalidDataException(path + ": not AONG");
                int version = r.ReadInt32();
                if (version != 2) throw new InvalidDataException(path + ": AONG version " + version);
                var g = new NavGround { SamplesX = r.ReadInt32(), SamplesZ = r.ReadInt32(), Cell = r.ReadSingle(), HeightScale = r.ReadSingle(), SourceBits = r.ReadInt32() };
                int rawLen = r.ReadInt32(), zLen = r.ReadInt32();
                byte[] raw = Inflate(r.ReadBytes(zLen), rawLen);
                int w = g.SamplesX, h = g.SamplesZ, p = 0;
                g.Heights = new ushort[w * h];
                Buffer.BlockCopy(raw, p, g.Heights, 0, w * h * 2); p += w * h * 2;
                g.Tiles = new ushort[(w - 1) * (h - 1)];
                Buffer.BlockCopy(raw, p, g.Tiles, 0, g.Tiles.Length * 2); p += g.Tiles.Length * 2;
                g.Building = new byte[(w - 1) * (h - 1)];
                Buffer.BlockCopy(raw, p, g.Building, 0, g.Building.Length);
                return g;
            }
        }

        internal static byte[] Inflate(byte[] z, int rawLen)
        {
            using (var ms = new MemoryStream(z))
            using (var zs = new ZLibStream(ms, CompressionMode.Decompress))
            {
                var raw = new byte[rawLen]; int got = 0;
                while (got < rawLen) { int n = zs.Read(raw, got, rawLen - got); if (n <= 0) break; got += n; }
                if (got != rawLen) throw new InvalidDataException("zlib block short: " + got + " of " + rawLen);
                return raw;
            }
        }

        /// <summary>Bilinear height at world (x, z); NaN outside the map.</summary>
        public double HeightAt(double x, double z)
        {
            double fx = x / Cell, fz = z / Cell;
            int ix = (int)Math.Floor(fx), iz = (int)Math.Floor(fz);
            if (ix < 0 || iz < 0 || ix + 1 >= SamplesX || iz + 1 >= SamplesZ) return double.NaN;
            double tx = fx - ix, tz = fz - iz;
            double v = Heights[iz * SamplesX + ix] * (1 - tx) * (1 - tz) + Heights[iz * SamplesX + ix + 1] * tx * (1 - tz)
                     + Heights[(iz + 1) * SamplesX + ix] * (1 - tx) * tz + Heights[(iz + 1) * SamplesX + ix + 1] * tx * tz;
            return v * HeightScale;
        }

        public int TileAt(double x, double z)
        {
            int ix = (int)Math.Floor(x / Cell), iz = (int)Math.Floor(z / Cell);
            return ix < 0 || iz < 0 || ix >= SamplesX - 1 || iz >= SamplesZ - 1 ? -1 : Tiles[iz * (SamplesX - 1) + ix];
        }

        public int BuildingAt(double x, double z)
        {
            int ix = (int)Math.Floor(x / Cell), iz = (int)Math.Floor(z / Cell);
            return ix < 0 || iz < 0 || ix >= SamplesX - 1 || iz >= SamplesZ - 1 ? -1 : Building[iz * (SamplesX - 1) + ix];
        }
    }

    /// <summary>rooms.json: dungeon rooms placed in the world, each with its template cells.</summary>
    public sealed class NavDungeon
    {
        public sealed class Poly { public List<float[]> verts; public List<int[]> tris; }
        public sealed class Room
        {
            [JsonProperty("index")] public int Index;
            [JsonProperty("name")] public string Name = "";
            [JsonProperty("flags")] public int Flags;
            [JsonProperty("rot")] public int Rot;
            [JsonProperty("rect")] public int[] Rect;
            [JsonProperty("pos")] public float[] Pos;
            [JsonProperty("heightBase")] public int HeightBase;
            [JsonProperty("doors")] public List<int[]> Doors;
            [JsonProperty("polys")] public List<Poly> Polys;
            [JsonProperty("tile")] public int[][] Tile;
            [JsonProperty("height")] public int[][] Height;
            [JsonProperty("flags3")] public int[][] Flags3;
        }

        [JsonProperty("playfield")] public int Playfield;
        [JsonProperty("name")] public string Name;
        [JsonProperty("tilemap")] public int Tilemap;
        [JsonProperty("cell")] public float Cell;
        [JsonProperty("heightScale")] public float HeightScale;
        [JsonProperty("atlas")] public int[] Atlas;
        [JsonProperty("rooms")] public List<Room> Rooms;

        public static NavDungeon Read(string path) => JsonConvert.DeserializeObject<NavDungeon>(File.ReadAllText(path));

        /// <summary>Template cell of a room under world (x, z), or false when outside its rect.</summary>
        public bool CellOf(Room rm, double x, double z, out int col, out int row)
        {
            double dx = x - rm.Pos[0], dz = z - rm.Pos[2];
            for (int i = 0; i < ((-rm.Rot) % 4 + 4) % 4; i++) { double t = dx; dx = dz; dz = -t; }
            int x1 = rm.Rect[0], z1 = rm.Rect[1], x2 = rm.Rect[2], z2 = rm.Rect[3];
            int a = (int)Math.Floor((x1 + x2 + 1) / 2.0 + dx / Cell), b = (int)Math.Floor((z1 + z2 + 1) / 2.0 + dz / Cell);
            col = a - x1; row = b - z1;
            return a >= x1 && a <= x2 && b >= z1 && b <= z2;
        }

        /// <summary>World floor height of the tile under (x, z) in one room; NaN when outside or no tile.</summary>
        public double FloorHeight(Room rm, double x, double z)
        {
            if (!CellOf(rm, x, z, out int c, out int r)) return double.NaN;
            if (rm.Tile[r][c] == 0) return double.NaN;
            return rm.Pos[1] + (rm.Height[r][c] - rm.HeightBase) * HeightScale;
        }

        /// <summary>Rooms whose floor tiles cover (x, z).</summary>
        public IEnumerable<Room> RoomsAt(double x, double z)
        {
            foreach (var rm in Rooms) if (!double.IsNaN(FloorHeight(rm, x, z))) yield return rm;
        }
    }

    /// <summary>collision.bin (AOCL v1): near-horizontal triangles, bucketed on an 8 m grid for point-under queries.</summary>
    public sealed class NavCollision
    {
        public sealed class Chunk { public int Instance, TeleportDestPf, LocalizerType, LocalizerInstance; public float[] Verts; }   // 9 floats per triangle, world
        public readonly List<Chunk> Chunks = new List<Chunk>();
        readonly Dictionary<long, List<int>> _buckets = new Dictionary<long, List<int>>();   // -> (chunk << 20 | tri)
        public int Triangles;

        static long Key(int a, int c) => ((long)a << 32) ^ (uint)c;

        public static NavCollision Read(string path)
        {
            var nc = new NavCollision();
            using (var r = new BinaryReader(File.OpenRead(path)))
            {
                if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "AOCL") throw new InvalidDataException(path + ": not AOCL");
                int version = r.ReadInt32();
                if (version != 1) throw new InvalidDataException(path + ": AOCL version " + version);
                int chunks = r.ReadInt32(), rawLen = r.ReadInt32(), zLen = r.ReadInt32();
                byte[] raw = NavGround.Inflate(r.ReadBytes(zLen), rawLen);
                int p = 0;
                for (int ci = 0; ci < chunks; ci++)
                {
                    var c = new Chunk
                    {
                        Instance = BitConverter.ToInt32(raw, p), TeleportDestPf = BitConverter.ToInt32(raw, p + 4),
                        LocalizerType = BitConverter.ToInt32(raw, p + 8), LocalizerInstance = BitConverter.ToInt32(raw, p + 12)
                    };
                    float ox = BitConverter.ToSingle(raw, p + 16), oy = BitConverter.ToSingle(raw, p + 20), oz = BitConverter.ToSingle(raw, p + 24);
                    int n = BitConverter.ToInt32(raw, p + 28); p += 32;
                    c.Verts = new float[n * 9];
                    for (int i = 0; i < n * 9; i += 3)
                    {
                        c.Verts[i] = ox + BitConverter.ToInt16(raw, p) / 100f;
                        c.Verts[i + 1] = oy + BitConverter.ToInt16(raw, p + 2) / 100f;
                        c.Verts[i + 2] = oz + BitConverter.ToInt16(raw, p + 4) / 100f;
                        p += 6;
                    }
                    nc.Chunks.Add(c);
                    nc.Triangles += n;
                }
            }
            nc.Index();
            return nc;
        }

        void Index()
        {
            for (int ci = 0; ci < Chunks.Count; ci++)
            {
                float[] v = Chunks[ci].Verts;
                for (int t = 0; t * 9 < v.Length; t++)
                {
                    int o = t * 9;
                    int x0 = (int)Math.Floor(Math.Min(v[o], Math.Min(v[o + 3], v[o + 6])) / 8), x1 = (int)Math.Floor(Math.Max(v[o], Math.Max(v[o + 3], v[o + 6])) / 8);
                    int z0 = (int)Math.Floor(Math.Min(v[o + 2], Math.Min(v[o + 5], v[o + 8])) / 8), z1 = (int)Math.Floor(Math.Max(v[o + 2], Math.Max(v[o + 5], v[o + 8])) / 8);
                    for (int a = x0; a <= x1; a++)
                        for (int c = z0; c <= z1; c++)
                        {
                            if (!_buckets.TryGetValue(Key(a, c), out var l)) _buckets[Key(a, c)] = l = new List<int>();
                            l.Add((ci << 20) | t);
                        }
                }
            }
        }

        /// <summary>Heights of every kept triangle directly under world (x, z).</summary>
        public IEnumerable<double> HeightsUnder(double x, double z)
        {
            if (!_buckets.TryGetValue(Key((int)Math.Floor(x / 8), (int)Math.Floor(z / 8)), out var l)) yield break;
            foreach (int id in l)
            {
                float[] v = Chunks[id >> 20].Verts; int o = (id & 0xFFFFF) * 9;
                double x0 = v[o], y0 = v[o + 1], z0 = v[o + 2], x1 = v[o + 3], y1 = v[o + 4], z1 = v[o + 5], x2 = v[o + 6], y2 = v[o + 7], z2 = v[o + 8];
                double d = (x1 - x0) * (z2 - z0) - (x2 - x0) * (z1 - z0);
                if (Math.Abs(d) < 1e-9) continue;
                double u = ((x - x0) * (z2 - z0) - (x2 - x0) * (z - z0)) / d;
                double w = ((x1 - x0) * (z - z0) - (x - x0) * (z1 - z0)) / d;
                if (u >= -1e-6 && w >= -1e-6 && u + w <= 1 + 1e-6) yield return y0 + u * (y1 - y0) + w * (y2 - y0);
            }
        }
    }
}
