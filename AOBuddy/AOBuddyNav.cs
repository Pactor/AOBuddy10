using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
        public MissionLayout Layout;               // a mission instance only: the zone-in placement it was composed from

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
                sb.AppendFormat("water {0}; ", Ground.WaterInfo());
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
        // rotation, x origin = X * 10 m, far z edge = (gridHeight - Z) * 10 m, y = pool height + (floor - lowest floor) * worldHeight.

        public sealed class MissionLayout
        {
            public int Instance, TemplatePlayfield, Width, Height, WorldHeight;
            public float LandX, LandY, LandZ;               // where the server put us on zone-in (the entrance)
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
            int version = I32();                                         // version, then the landing coordinates
            float F32() { int v = I32(); return BitConverter.Int32BitsToSingle(v); }
            float lx = F32(), ly = F32(), lz = F32();
            if (version <= 3) return null;
            p++; int modelType = I32(), modelInst = I32(); I32(); I32(); I32(); I32();
            int genType = I32(), genInst = I32();
            if (genType != 51103) return null;                           // 0xC79F ACGBuildingGeneratorData
            I32();                                                       // revision
            var m = new MissionLayout { Instance = modelInst, TemplatePlayfield = 0, LandX = lx, LandY = ly, LandZ = lz };
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
            // Floors are numbered as the server sends them, and a building can sit below its entrance:
            // the Grey Caves mission on 2026-09-23 had floors 0, -1, -2 walked at y 133, 69 and 0. The
            // heights count up from the lowest floor, not from floor 0 (all eight saved missions fit).
            int lowestFloor = int.MaxValue;
            foreach (var pr in m.Rooms) lowestFloor = Math.Min(lowestFloor, pr[1]);
            if (lowestFloor == int.MaxValue) lowestFloor = 0;
            var d = new NavDungeon { Playfield = m.Instance, Name = "mission from " + pool.Name, Tilemap = pool.Tilemap, Cell = pool.Cell, HeightScale = pool.HeightScale, Atlas = pool.Atlas, Rooms = new List<NavDungeon.Room>() };
            foreach (var pr in m.Rooms)
            {
                int idx = pr[0], floor = pr[1], X = pr[2], Z = pr[3], rot = pr[4];
                if (idx < 0 || idx >= pool.Rooms.Count) continue;
                var src = pool.Rooms[idx];
                int w = src.Rect[2] - src.Rect[0] + 1, h = src.Rect[3] - src.Rect[1] + 1;
                int tw = rot % 2 == 0 ? w : h, th = rot % 2 == 0 ? h : w;   // footprint after rotation
                double ox = X * slot, oz = (m.Height - Z) * slot - th * pool.Cell;
                // A room's floor is (w-1) x (h-1) cells: the last column and row of its rect are the cell it
                // shares with its neighbour. The room turns about its FLOOR's centre, and in the mission the
                // shared cell stays on the slot's +x / -z side (world z counts down from the grid's far edge).
                // Turning about the rect's centre instead swung the shared cell round with the room and put
                // rooms 1-2 m off by rotation (Ventil mission 2026-09-23, floor -1: rot 1 right, rot 2 off
                // (-2,0), rot 3 off (-2,+2), the odd-sized SmallB5 (-1,+1)). With this every doorway on all four
                // floors meets its neighbour's, and of the owner's 226 walked steps there one crosses a wall.
                // In the pool's geometry (walls, doors) the floor's centre is the stored centre minus 1 m on an
                // even-sized axis and the stored centre itself on an odd one; the tiles (CellOf) put it 1 m
                // lower on the odd axis too. So each room gets two pivots, one each for geometry and tiles:
                // pivot = slot rect centre - (1, -1) + turned(stored centre - floor centre in that frame).
                int turnsT = ((-rot) % 4 + 4) % 4;
                (double, double) Turned(double x, double z) { for (int i = 0; i < turnsT; i++) { double t = x; x = -z; z = t; } return (x, z); }
                var (gx, gz) = Turned(w % 2 == 0 ? 1 : 0, h % 2 == 0 ? 1 : 0);
                var (tx, tz) = Turned(1, 1);
                double cx = ox + tw * pool.Cell / 2.0 - 1, cz = oz + th * pool.Cell / 2.0 + 1;
                float y = src.Pos[1] + (floor - lowestFloor) * m.WorldHeight;
                d.Rooms.Add(new NavDungeon.Room
                {
                    Index = d.Rooms.Count, Name = src.Name + " f" + floor, PoolName = src.Name, PoolIndex = idx, Floor = floor, Flags = src.Flags, Rot = rot, Rect = src.Rect,
                    Pos = new[] { (float)(cx + tx), y, (float)(cz + tz) },
                    GeomPos = new[] { (float)(cx + gx), y, (float)(cz + gz) },
                    HeightBase = src.HeightBase, Doors = src.Doors, Polys = src.Polys, Tile = src.Tile, Height = src.Height, Flags3 = src.Flags3
                });
            }
            var nav = new AOBuddyNav(m.Instance, "mission", d.Name, null, d, null) { Layout = m, Walls = PlaceWalls(pluginDir, m.TemplatePlayfield, pool, d) };
            var doors = PlaceDoors(pool, d);
            DoorCheck = CheckDoorways(d, doors);
            nav.Exit = FindExit(d, doors);
            return nav;
        }

        /// <summary>
        /// A mission only: the door out of the building. The zone-in's landing point is the entrance only
        /// when you come in from outside; a bot (re)started inside a running mission lands wherever it is
        /// (2026-09-23 23:54: on floor -1), so the exit comes from the building instead. The rooms arrive
        /// entrance first (the Ventil mission lists Subway_Ent_5 first, and a fresh entry landed in it), and
        /// the way out is the entrance room's doorway that opens onto no neighbour.
        /// </summary>
        public Doorway Exit;
        public int ExitFloor => Exit == null ? 0 : Exit.Floor;

        static Doorway FindExit(NavDungeon mission, List<Doorway>[] doors)
        {
            if (mission.Rooms.Count == 0) return null;
            Doorway best = null;
            foreach (var da in doors[0])
            {
                bool faces = false;
                for (int b = 1; b < doors.Length && !faces; b++)
                {
                    if (mission.Rooms[b].Floor != mission.Rooms[0].Floor) continue;
                    foreach (var db in doors[b])
                        if (da.Nx * db.Nx + da.Nz * db.Nz < -0.9 && Math.Sqrt((da.X - db.X) * (da.X - db.X) + (da.Z - db.Z) * (da.Z - db.Z)) < 3.5) { faces = true; break; }
                }
                if (!faces) { best = da; break; }
            }
            return best;
        }

        /// <summary>A mission only: every placed room's wall triangles (walls.bin of the pool), world coordinates, 9 floats each. Null when the pool has no walls.bin.</summary>
        public float[] Walls;

        // The pool's walls.bin holds each pool room's wall triangles where the pool itself places the room
        // (record index = room index). Move them to where the mission put the room: the offset from the pool
        // room's centre, turned from the pool room's rotation to the mission's (the same turn NavDungeon.CellOf
        // uses), then added to the mission room's geometry pivot; heights shift with the floor.
        //
        static float[] PlaceWalls(string pluginDir, int poolPf, NavDungeon pool, NavDungeon mission)
        {
            string wp = Path.Combine(FolderFor(pluginDir, poolPf), "walls.bin");
            if (!File.Exists(wp)) return null;
            var byRoom = new Dictionary<int, List<float[]>>();
            foreach (var c in NavCollision.Read(wp).Chunks)
            {
                int idx = c.Instance & 0xFFFF;
                if (!byRoom.TryGetValue(idx, out var l)) byRoom[idx] = l = new List<float[]>();
                l.Add(c.Verts);
            }
            int n = mission.Rooms.Count;
            var walls = new List<float>();
            for (int ri = 0; ri < n; ri++)
            {
                var mr = mission.Rooms[ri];
                if (mr.PoolIndex < 0) continue;
                var pr = pool.Rooms[mr.PoolIndex];
                var g = mr.GeomPos ?? mr.Pos;
                int turns = ((((-pr.Rot) % 4 + 4) % 4) - (((-mr.Rot) % 4 + 4) % 4) + 4) % 4;
                void Turn(ref double x, ref double z) { for (int t = 0; t < turns; t++) { double s = x; x = z; z = -s; } }
                if (byRoom.TryGetValue(mr.PoolIndex, out var chunks))
                    foreach (var v in chunks)
                        for (int i = 0; i + 2 < v.Length; i += 3)
                        {
                            double dx = v[i] - pr.Pos[0], dz = v[i + 2] - pr.Pos[2];
                            Turn(ref dx, ref dz);
                            walls.Add((float)(g[0] + dx)); walls.Add(v[i + 1] - pr.Pos[1] + g[1]); walls.Add((float)(g[2] + dz));
                        }
            }
            return walls.ToArray();
        }

        /// <summary>Every placed room's doorways to neighbours, in world coordinates (rooms.json 'doors', see DoorwaysFromField).</summary>
        static List<Doorway>[] PlaceDoors(NavDungeon pool, NavDungeon mission)
        {
            var doors = new List<Doorway>[mission.Rooms.Count];
            for (int ri = 0; ri < doors.Length; ri++)
            {
                var mr = mission.Rooms[ri];
                doors[ri] = new List<Doorway>();
                if (mr.PoolIndex < 0) continue;
                var pr = pool.Rooms[mr.PoolIndex];
                var g = mr.GeomPos ?? mr.Pos;
                int turns = ((((-pr.Rot) % 4 + 4) % 4) - (((-mr.Rot) % 4 + 4) % 4) + 4) % 4;
                void Turn(ref double x, ref double z) { for (int t = 0; t < turns; t++) { double s = x; x = z; z = -s; } }
                foreach (var dw in DoorwaysFromField(pr))
                {
                    double cx = dw.X - pr.Pos[0], cz = dw.Z - pr.Pos[2], nx = dw.Nx, nz = dw.Nz;
                    Turn(ref cx, ref cz); Turn(ref nx, ref nz);
                    doors[ri].Add(new Doorway { X = g[0] + cx, Y = g[1], Z = g[2] + cz, Nx = nx, Nz = nz, Floor = mr.Floor });
                }
            }
            return doors;
        }

        // The placement is checked by the doorways (the owner's idea, 2026-09-23): every doorway of a room
        // that faces a neighbour should meet that neighbour's doorway. Rooms are not moved; the log says how
        // many meet and names any that miss, which would mean the placement rule is off for that pool.
        /// <summary>The doorway check of the last mission composed, for the log.</summary>
        public static string DoorCheck = "";

        static string CheckDoorways(NavDungeon mission, List<Doorway>[] doors)
        {
            int total = 0, met = 0; var off = new List<string>();
            for (int a = 0; a < doors.Length; a++)
                foreach (var da in doors[a])
                {
                    double best = double.MaxValue;
                    for (int b = 0; b < doors.Length; b++)
                    {
                        if (b == a || mission.Rooms[b].Floor != mission.Rooms[a].Floor) continue;
                        foreach (var db in doors[b])
                            if (da.Nx * db.Nx + da.Nz * db.Nz < -0.9)
                                best = Math.Min(best, Math.Sqrt((da.X - db.X) * (da.X - db.X) + (da.Z - db.Z) * (da.Z - db.Z)));
                    }
                    if (best > 3.5) continue;   // a doorway onto nothing (the building's edge): nothing to check
                    total++;
                    if (best < 0.3) met++;
                    else off.Add($"{mission.Rooms[a].PoolName} f{mission.Rooms[a].Floor} ({da.X:0.0},{da.Z:0.0}) {best:0.0} m off");
                }
            return $"{met}/{total} doorways meet their neighbour's" + (off.Count > 0 ? "; OFF: " + string.Join(", ", off.Take(6)) : "");
        }

        public sealed class Doorway { public double X, Y, Z, Nx, Nz; public int Floor; }   // N: out of the room

        /// <summary>
        /// A pool room's doorways from its room record, in pool coordinates (pool rooms are unrotated).
        /// Each entry is (link, code): link 65535 is a doorway to a neighbour, the room's own index an inner
        /// door (skipped). code = row * 4(W-1) + col: row is the door's 2 m cell row in the room's floor (the
        /// floor is W-1 by H-1 cells; the last row and column are the cell shared with the neighbour), col
        /// its x in half metres. Row 0 is the south side, row H-2 the north; otherwise col 3 is the west side
        /// and 4(W-1)-3 the east. Worked out on pool 351 (Subway - Ventil), where every entry matched a wall
        /// opening: 6x6 [10 43 57 88] = S x5, W z5, E z5, N; 16x16 BigA3 [10 177 723 777] = S x5, E z5,
        /// W z25, E z25. North doors decode 1 m short in x on every size (x4 for an opening at 5), corrected
        /// here. In the room's geometry the floor starts at the stored centre minus W metres, plus 1 m on an
        /// odd-sized axis: that is where the walls put the openings (SmallA2 11x11: the code's x15 is the
        /// opening at 16).
        /// </summary>
        static List<Doorway> DoorwaysFromField(NavDungeon.Room pr)
        {
            var outp = new List<Doorway>();
            if (pr.Doors == null) return outp;
            int W = pr.Rect[2] - pr.Rect[0] + 1, H = pr.Rect[3] - pr.Rect[1] + 1;
            int stride = 4 * (W - 1);
            if (stride <= 0) return outp;
            double ox = pr.Pos[0] - W + (W % 2 == 1 ? 1 : 0), oz = pr.Pos[2] - H + (H % 2 == 1 ? 1 : 0);
            double fw = 2.0 * (W - 1), fh = 2.0 * (H - 1);
            foreach (var d in pr.Doors)
            {
                if (d == null || d.Length < 2 || d[0] != 65535) continue;
                int row = d[1] / stride, col = d[1] % stride;
                double lx, lz, nx = 0, nz = 0;
                if (row == 0) { lx = col / 2.0; lz = 0; nz = -1; }
                else if (row >= H - 2) { lx = col / 2.0 + 1; lz = fh; nz = 1; }
                else if (col == 3) { lx = 0; lz = row * 2 + 1; nx = -1; }
                else if (col == stride - 3) { lx = fw; lz = row * 2 + 1; nx = 1; }
                else continue;   // a door inside the room (the MH halls' side rooms: MH_5 330 = row 5, col 30), not to a neighbour
                outp.Add(new Doorway { X = ox + lx, Z = oz + lz, Nx = nx, Nz = nz });
            }
            return outp;
        }

    }

    /// <summary>ground.bin (AONG v2-v4): outdoor heightfield + tile ids + building nibbles; v3 adds the water
    /// planes, v4 the client's own liquid polygons (the playfield record's water rings — see SwimY).</summary>
    public sealed class NavGround
    {
        public int SamplesX, SamplesZ, SourceBits;
        public float Cell, HeightScale;
        public ushort[] Heights;     // [z * SamplesX + x], height = value * HeightScale
        public ushort[] Tiles;       // [(SamplesZ-1) * (SamplesX-1)]
        public byte[] Building;
        public float[] WaterY = new float[0];   // v3 legacy: the playfield record's plane-table levels (the LAST liquid's
                                                // levels — see WaterPolygons). Informational only; v4's rings are the truth

        public static NavGround Read(string path)
        {
            using (var r = new BinaryReader(File.OpenRead(path)))
            {
                if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "AONG") throw new InvalidDataException(path + ": not AONG");
                int version = r.ReadInt32();
                if (version < 2 || version > 4) throw new InvalidDataException(path + ": AONG version " + version);
                var g = new NavGround { SamplesX = r.ReadInt32(), SamplesZ = r.ReadInt32(), Cell = r.ReadSingle(), HeightScale = r.ReadSingle(), SourceBits = r.ReadInt32() };
                if (version >= 3)
                {
                    int wc = r.ReadInt32();
                    g.WaterY = new float[wc];
                    for (int i = 0; i < wc; i++) g.WaterY[i] = r.ReadSingle();
                }
                if (version >= 4)
                {
                    // the client's own liquid polygons (see SwimY): per liquid the ring size, the level, (x, z) pairs
                    int qc = r.ReadInt32();
                    var lakes = new List<Lake>(qc);
                    for (int i = 0; i < qc; i++)
                    {
                        int pts = r.ReadInt32();
                        var lk = new Lake { Level = r.ReadSingle(), X = new float[pts], Z = new float[pts] };
                        for (int pt = 0; pt < pts; pt++) { lk.X[pt] = r.ReadSingle(); lk.Z[pt] = r.ReadSingle(); }
                        lk.MinX = lk.X.Min(); lk.MaxX = lk.X.Max(); lk.MinZ = lk.Z.Min(); lk.MaxZ = lk.Z.Max();
                        lakes.Add(lk);
                    }
                    g._lakes = lakes.ToArray();
                }
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

        /// <summary>One liquid polygon from the playfield record: a flat ring of points at one level.</summary>
        private struct Lake { public float Level; public float[] X, Z; public float MinX, MaxX, MinZ, MaxZ; }
        private Lake[] _lakes;   // v4 ground.bin: the client's own liquid polygons — SwimY's whole truth.
                                 // Null on v3 data, which falls back to the tile-12 band model below.

        /// <summary>
        /// The water surface to swim on at (x, z): NaN = dry ground. V4 DATA (2026-09-25): the client's
        /// own water is a list of flat POLYGONS in the playfield record — the owner's call ("the game is
        /// from 2000, they wouldn't flood-fill; water has to be a quad or a volume"), baited with a lake
        /// he stood in (level 18.3 at (2349,1127) in Newland Desert — a level no stored plane has) and
        /// found in the record that evening: per liquid one ring of 3-12 points at one level. SwimY is a
        /// point-in-ring test: 565's lake quad at 18.3 spans (2267..2416, 1052..1180) — his lake exactly;
        /// 567 Newland's 32.1 is the captured swim (32.09); Coast of Tranquility's ocean is ONE quad over
        /// its whole map; Wailing Wastes is a whole-map water table at 5.0; ICC's canal at 15.4; the acid
        /// river of 565 is in the same list (a liquid is a liquid). Ground poking through the surface
        /// (an island, a shore) leaves nothing to swim on when it is within wadeDepth of the level.
        ///
        /// V3 DATA (2026-09-24/25, superseded — still the fallback for un-regenerated files): the RDB
        /// "water planes" (WaterY) applied playfield-wide put the bot 7 m over Newland City's dry pit, so
        /// the region was guessed from the tilemap instead: cells whose tile id's low byte is 12 form a
        /// band the designers paint over the shore, the body is the flood from it through ground under
        /// the level, and the level is the stored plane above the band's core. Gates against its two
        /// failure modes (Newland Desert's 47% texture 'band' and its runaway 86% flood, log 2026-09-25
        /// 19:01) are below in BuildWater.
        /// </summary>
        public double SwimY(double x, double z, double wadeDepth)
        {
            if (_lakes != null)
            {
                double lakeFloor = HeightAt(x, z);
                if (double.IsNaN(lakeFloor)) return double.NaN;
                foreach (Lake lk in _lakes)
                {
                    if (x < lk.MinX || x > lk.MaxX || z < lk.MinZ || z > lk.MaxZ) continue;
                    if (!InRing(lk, x, z)) continue;
                    return lk.Level - lakeFloor > wadeDepth ? lk.Level : double.NaN;
                }
                return double.NaN;
            }
            if (_wet == null) BuildWater();
            if (float.IsNaN(_waterLevel)) return double.NaN;
            int ix = (int)Math.Floor(x / Cell), iz = (int)Math.Floor(z / Cell);
            if (ix < 0 || iz < 0 || ix >= SamplesX - 1 || iz >= SamplesZ - 1) return double.NaN;
            if (!_wet[iz * (SamplesX - 1) + ix]) return double.NaN;
            double floor = HeightAt(x, z);
            if (double.IsNaN(floor)) return double.NaN;
            return _waterLevel - floor > wadeDepth ? _waterLevel : double.NaN;
        }

        // Even-odd point-in-polygon over the ring. There are a handful of liquids per playfield and
        // this runs once per walk step at most — no index needed.
        private static bool InRing(Lake lk, double x, double z)
        {
            bool inside = false;
            int n = lk.X.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
                if ((lk.Z[i] > z) != (lk.Z[j] > z) &&
                    x < (lk.X[j] - lk.X[i]) * (z - lk.Z[i]) / (lk.Z[j] - lk.Z[i]) + lk.X[i])
                    inside = !inside;
            return inside;
        }

        private bool[] _wet;                 // [(SamplesZ-1)*(SamplesX-1)]: the flooded water body
        private float _waterLevel = float.NaN;

        /// <summary>
        /// Flood the water body from the tile-12 band at the stored plane that caps it. Two gates keep a
        /// stored plane off land it was never meant for, both learned in Newland Desert (565) on
        /// 2026-09-25 19:01: its only stored plane is 49.2 while a mission door sits on ground at 20, its
        /// tile-12 'band' is a desert texture over 47% of the map, and the flood from it read a phantom
        /// lake over 86% of the playfield — every step of the walk to the door claimed the plane 29 m up,
        /// the position jump read as a zone, and the planner replanned itself out after six tries
        /// ("expected to stay put, landed in Newland Desert").
        /// </summary>
        private void BuildWater()
        {
            int w = SamplesX - 1, h = SamplesZ - 1;
            _wet = new bool[w * h];
            // the band: tile low byte 12. A handful of strays (Newland City has 7 on high ground) is not a body.
            int seeds = 0;
            for (int i = 0; i < w * h; i++) if ((Tiles[i] & 0xFF) == 12) seeds++;
            if (seeds < 8) return;
            // A BAND, NOT THE MAP: tile ids index each playfield's own terrain set, so low byte 12 is the
            // water-shore tile only where it happens to be (Newland 567: 3.6% of the cells; Lush Fields'
            // river system: 16%). Newland Desert paints a desert texture there instead — 47% of the map —
            // which is what let the flood below wet 86% of the playfield. Water itself can cover half a
            // map (Coast of Tranquility's is 51% by its map), but no playfield like that decodes a band
            // this big: 656's band is 0.2% of its cells and already dry by the plane check above, its sea
            // beyond the reach of this data. Where the band IS this big (565, Penumbra 4006/4320) the
            // tile has meant a texture every time it could be checked — so: dry, as far as this data knows.
            if (seeds > _wet.Length / 4) return;
            // the band's core height (p10 of the cells' lowest corner — the band slopes into the depths)
            var band = new List<float>(seeds);
            for (int z = 0; z < h; z++)
                for (int x = 0; x < w; x++)
                    if ((Tiles[z * w + x] & 0xFF) == 12)
                        band.Add(Math.Min(Math.Min(Corner(z, x), Corner(z, x + 1)), Math.Min(Corner(z + 1, x), Corner(z + 1, x + 1))));
            band.Sort();
            float core = band[band.Count / 10];
            float level = float.NaN;
            foreach (float y in WaterY) if (y > core && (float.IsNaN(level) || y > level)) level = y;
            if (float.IsNaN(level)) return;
            // CONTAINED: flood with no distance limit as the test of the level itself. A real lake sits in
            // a basin that dry land closes above the plane, so the unbounded flood stays in it (Newland's
            // lake: 11.6% of 567; Lush Fields' rivers: 22% of 695). A plane let loose on the lowlands
            // floods most of the playfield instead (Newland Desert's 49.2: 90%) — no shoreline exists at
            // that level, nobody swims there, and claiming one put the walk on the plane's height over dry
            // ground (see the class note above). An open sea would flood past half legitimately (Coast of
            // Tranquility again) — but this model cannot express a sea anyway: one plane per playfield,
            // and the flood below reaches 150 m of shore at most. Abort at half the map; it only ever
            // takes away runaways (565's 90%, Penumbra Forest's 82%).
            for (int i = 0; i < w * h; i++)
                if ((Tiles[i] & 0xFF) == 12) _wet[i] = true;
            if (!Flood((bool[])_wet.Clone(), level, int.MaxValue, _wet.Length / 2)) return;
            // flood from the band through cells whose lowest corner is under the surface — but only so far:
            // a real shore is a few cells of shallows around the band, while an unbounded flood spills through
            // any lowland below the plane into basins that have no water at all (Wailing Wastes, 2026-09-25:
            // the wompah station, 335 m from the nearest band cell and bone dry, read as a 4 m deep pool and
            // the bot waded at the phantom surface until the server dropped it every step).
            Flood(_wet, level, Math.Max(4, (int)(150 / Cell)), int.MaxValue);
            _waterLevel = level;
        }

        // Flood from the already-marked seed cells through cells whose lowest corner is under `level`, at
        // most `maxShore` cells beyond a seed. Returns false the moment more than `abortAt` cells are wet
        // (the containment test above); int.MaxValue disables either limit.
        private bool Flood(bool[] wet, float level, int maxShore, int abortAt)
        {
            int w = SamplesX - 1, h = SamplesZ - 1;
            var queue = new Queue<(int cell, int dist)>();
            for (int i = 0; i < wet.Length; i++) if (wet[i]) queue.Enqueue((i, 0));
            int count = queue.Count;
            while (queue.Count > 0)
            {
                var (c, dist) = queue.Dequeue();
                if (dist >= maxShore) continue;
                int cx = c % w, cz = c / w;
                for (int n = 0; n < 4; n++)
                {
                    int nx = cx + (n == 0 ? -1 : n == 1 ? 1 : 0), nz = cz + (n == 2 ? -1 : n == 3 ? 1 : 0);
                    if (nx < 0 || nz < 0 || nx >= w || nz >= h) continue;
                    int nc = nz * w + nx;
                    if (wet[nc]) continue;
                    if (Math.Min(Math.Min(Corner(nz, nx), Corner(nz, nx + 1)), Math.Min(Corner(nz + 1, nx), Corner(nz + 1, nx + 1))) < level)
                    {
                        wet[nc] = true;
                        if (++count > abortAt) return false;
                        queue.Enqueue((nc, dist + 1));
                    }
                }
            }
            return true;
        }

        private float Corner(int sz, int sx) => Heights[sz * SamplesX + sx] * HeightScale;

        /// <summary>The water verdict for 'navdata': the liquids the record names, or the v3 model's guess.</summary>
        public string WaterInfo()
        {
            if (_lakes != null)
                return _lakes.Length == 0 ? "dry (no liquids in the record)"
                    : $"{_lakes.Length} liquid(s) at " + string.Join(", ", _lakes.Select(l => l.Level.ToString("0.0")).Distinct());
            if (_wet == null) BuildWater();
            int wet = 0;
            foreach (bool b in _wet) if (b) wet++;
            return float.IsNaN(_waterLevel) ? "dry" : $"{_waterLevel:0.0} over {100.0 * wet / _wet.Length:0}% of the map";
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
            [JsonIgnore] public int Floor;         // mission rooms only: the floor as the server numbered it
            [JsonIgnore] public string PoolName = "";   // mission rooms only: the pool room's own name
            [JsonIgnore] public int PoolIndex = -1;     // mission rooms only: the pool room's index (= its collision record)
            [JsonIgnore] public float[] GeomPos;         // mission rooms only: the point its walls and doors turn about (Pos: the tiles')
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
        // -> (chunk << TriBits | tri). Was chunk << 20: past 2047 chunks that went negative and HeightsUnder threw on
        // every frame in Lush Fields (695), which has more (log 2026-09-24 01:36).
        const int TriBits = 11;
        readonly Dictionary<long, List<int>> _buckets = new Dictionary<long, List<int>>();
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
                            l.Add((ci << TriBits) | t);   // a chunk holds at most 2048 triangles (the extractor splits records there)
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
                float[] v = Chunks[id >> TriBits].Verts; int o = (id & ((1 << TriBits) - 1)) * 9;
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
