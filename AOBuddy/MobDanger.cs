using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AOSharp.Common.GameData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AOBuddy
{
    /// <summary>
    /// OUTDOOR MOB AVOIDANCE (owner, 2026-09-28). Outside a mission he never fights, he only runs, and he is squishy:
    /// 08:31-08:32 on the way out of a Longest Road mission door a pack of level 42-43 wildlife (Boneheaded Bloodcreeper,
    /// Borer Scorpiod, Young Scab Hyena) turned on him unprovoked, five on him, and killed him at (2834,1512). The walk
    /// grid knew walls, water, slopes and pull-backs, but not where the mobs are. This is where they are, for the grid:
    ///
    ///   * WHO IS HOSTILE is learned, never listed: a mob kind (its name) that turned on him or a pet OUTDOORS without one
    ///     of us fighting it first (Awareness' AGGRO with no ENGAGE on it before) - kept in hostile_mobs.json with how many
    ///     times and from how far (the farthest is its aggro radius). The first run seeds the file from aobuddy.prev.log +
    ///     aobuddy.log (same rule, read off the NAV/ENGAGE/AGGRO lines). Mission buildings don't count (everything in
    ///     there attacks).
    ///   * WHERE THEY ARE comes from the mob atlas (MobAtlas, mobs/&lt;pf&gt;.jsonl): each sighting's FIRST position - where it
    ///     stood when it came into view, before it could chase him (its track afterwards follows HIM when it aggroes, and
    ///     would paint his own path as mob country). Per 16 m bin: the hostile mobs seen there, one per (mob, 10-minute
    ///     window), divided by the windows he was near that bin at all (any NPC first seen within one bin of it) - so a
    ///     bin is "how many hostile mobs are standing here when he passes", not "how often he passed".
    ///   * WHERE THEY TURNED ON HIM: every such unprovoked outdoor AGGRO, at the spot he stood (the log's heartbeat position
    ///     when seeding, his own position live), one counted mob each - 08:23-08:24 on the way in, seven of them along
    ///     (2770,1408)-(2944,1524); 08:31 on the way out, five along (2941,1569)-(2876,1534). Kept in the same file.
    ///   * LIVE: the hostile kinds the server shows him now (Awareness), not the ones already on him or following him.
    ///
    /// Each mob counts by its level against his: 2^((mob - him)/10), 0.25..16 - a level-42 pack still killed him at 52
    /// (0.5 each), one ten levels up counts double, one 40 up counts 16 (in effect a no-go). The grid (OverlandGrid)
    /// turns a spot into step cost: crossing a spot's aggro circle through its middle costs DetourPerMob metres per
    /// counted mob, fading to nothing at its edge. Only cost, never a block; the owner's recorded roads get none.
    /// </summary>
    public static class MobDanger
    {
        public sealed class Hostile { public int N; public float MaxDist; public List<int> Pfs = new List<int>(); public DateTime Last; }
        /// <summary>One unprovoked outdoor aggro: where he stood, who turned on him, from how far.</summary>
        public sealed class AggroAt { public int Pf; public float X, Z, Dist; public string Name; public int Lvl; public DateTime At; }
        private sealed class Store
        {
            public Dictionary<string, Hostile> Kinds = new Dictionary<string, Hostile>(StringComparer.OrdinalIgnoreCase);
            public List<AggroAt> Aggros = new List<AggroAt>();
        }
        private const int MaxAggrosPerZone = 400;   // the newest are kept
        public readonly struct LiveMob
        {
            public readonly string Name; public readonly int Lvl; public readonly Vector3 Pos;
            public LiveMob(string name, int lvl, Vector3 pos) { Name = name; Lvl = lvl; Pos = pos; }
        }
        /// <summary>A place hostile mobs stand: W = counted mobs expected there (level-weighted), R = their aggro radius.</summary>
        public sealed class Spot { public float X, Z, W, R; public bool Live; public string Names; }

        /// <summary>Metres of detour worth one counted mob's aggro circle crossed through its middle.</summary>
        public const float DetourPerMob = 150f;
        private const float Bin = 16f;
        private const double WindowSecs = 600;
        private const float DefaultRadius = 15f;   // a kind with no aggro distance on record: the median of the log's AGGRO lines

        private static readonly object Gate = new object();
        private static string _dir, _file;
        private static Action<string> _log = _ => { };
        private static Dictionary<string, Hostile> _hostile = new Dictionary<string, Hostile>(StringComparer.OrdinalIgnoreCase);
        private static List<AggroAt> _aggros = new List<AggroAt>();
        private static int _hostileVer, _myLevel;
        private static float _medianDist = DefaultRadius;

        // atlas per zone: the raw bins (hostile (name, lvl) list + visit windows), re-read when the file grew
        private sealed class RawBin { public float SumX, SumZ; public int N; public List<(string name, int lvl)> Mobs = new List<(string, int)>(); public int Visits; }
        private sealed class Atlas { public long Len; public DateTime ReadAt; public Dictionary<(int, int), RawBin> Bins; public int Ver; }
        private static readonly Dictionary<int, Atlas> _atlas = new Dictionary<int, Atlas>();
        private static int _atlasVer;

        private static int _livePf = -1;
        private static List<LiveMob> _live = new List<LiveMob>();
        private static string _liveSig = "";
        public static int LiveVersion { get; private set; }

        public static void Init(string pluginDir, Action<string> log)
        {
            lock (Gate)
            {
                if (log != null) _log = log;
                if (_dir != null) return;
                _dir = pluginDir;
                _file = Path.Combine(pluginDir, "hostile_mobs.json");
                try
                {
                    if (File.Exists(_file))
                    {
                        var st = JsonConvert.DeserializeObject<Store>(File.ReadAllText(_file)) ?? new Store();
                        _hostile = new Dictionary<string, Hostile>(st.Kinds ?? new Dictionary<string, Hostile>(), StringComparer.OrdinalIgnoreCase);
                        _aggros = st.Aggros ?? new List<AggroAt>();
                    }
                    else
                    {
                        int n = 0;
                        foreach (var f in new[] { "aobuddy.prev.log", "aobuddy.log" }) n += SeedFromLog(Path.Combine(pluginDir, f));
                        Save();
                        _log($"MOBDANGER: seeded hostile_mobs.json from the log: {n} unprovoked outdoor AGGRO line(s), {_hostile.Count} kind(s): {string.Join(", ", _hostile.Select(kv => $"{kv.Key} (x{kv.Value.N}, {kv.Value.MaxDist:0} m)"))}.");
                    }
                }
                catch (Exception ex) { _log("MOBDANGER: hostile_mobs.json unreadable: " + ex.Message); }
                Median();
                _hostileVer++;
            }
        }

        private static readonly Regex NowIn = new Regex(@"NAV: now in pf (\d+)"), Aggro = new Regex(@"AGGRO: '(.+)' \((\d+) m\) on "),
                                      Engage = new Regex(@"ENGAGE: .* -> '(.+)' \("), HbPos = new Regex(@" hb \[.*? pos=\((-?\d+),(-?\d+),(-?\d+)\)");

        /// <summary>The log's unprovoked outdoor AGGRO lines: a mob turning on him/a pet in an outdoor zone with no ENGAGE on
        /// that name in the 60 s before. Returns how many it took.</summary>
        private static int SeedFromLog(string path)
        {
            if (!File.Exists(path)) return 0;
            int pf = -1, n = 0;
            float? hx = null, hz = null;                    // his last heartbeat position in this zone
            var pending = new List<AggroAt>();              // its level comes on the AWARE line right after
            var engaged = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var sr = new StreamReader(fs))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Length < 13) continue;
                    if (line.IndexOf("NAV: now in pf", StringComparison.Ordinal) >= 0) { var m = NowIn.Match(line); if (m.Success) { pf = int.Parse(m.Groups[1].Value); engaged.Clear(); hx = hz = null; pending.Clear(); } continue; }
                    if (line.IndexOf(" hb [", StringComparison.Ordinal) >= 0) { var m = HbPos.Match(line); if (m.Success) { hx = float.Parse(m.Groups[1].Value); hz = float.Parse(m.Groups[3].Value); } continue; }
                    if (pending.Count > 0 && line.IndexOf("AWARE: ", StringComparison.Ordinal) >= 0)
                    {
                        foreach (var p in pending)
                        {
                            var lm = Regex.Match(line, "'" + Regex.Escape(p.Name) + @"' lvl (\d+)");
                            if (lm.Success) p.Lvl = int.Parse(lm.Groups[1].Value);
                        }
                        pending.Clear();
                    }
                    double t = Secs(line);
                    if (line.IndexOf("ENGAGE: ", StringComparison.Ordinal) >= 0) { var m = Engage.Match(line); if (m.Success) engaged[m.Groups[1].Value] = t; continue; }
                    if (line.IndexOf("AGGRO: ", StringComparison.Ordinal) < 0) continue;
                    var a = Aggro.Match(line);
                    if (!a.Success || pf < 0 || pf >= 100000) continue;             // mission buildings: everything attacks
                    string name = a.Groups[1].Value;
                    if (engaged.TryGetValue(name, out double et) && t >= et && t - et < 60) continue;   // we opened on it
                    float dist = float.Parse(a.Groups[2].Value);
                    Note(name, pf, dist, DateTime.UtcNow);
                    if (hx.HasValue)
                    {
                        var at = new AggroAt { Pf = pf, X = hx.Value, Z = hz.Value, Dist = dist, Name = name, At = DateTime.UtcNow };
                        _aggros.Add(at); pending.Add(at);
                    }
                    n++;
                }
            }
            return n;
        }

        private static double Secs(string line)
        {
            // "08:31:23.952 ..." - time of day only; a wrap past midnight just looks like an old engage (not within 60 s)
            if (line.Length < 12 || line[2] != ':' || line[5] != ':') return -1;
            return int.Parse(line.Substring(0, 2)) * 3600 + int.Parse(line.Substring(3, 2)) * 60 + double.Parse(line.Substring(6, 6), System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void Note(string name, int pf, float dist, DateTime at)
        {
            if (!_hostile.TryGetValue(name, out var h)) _hostile[name] = h = new Hostile();
            h.N++; h.MaxDist = Math.Max(h.MaxDist, dist); h.Last = at;
            if (!h.Pfs.Contains(pf)) h.Pfs.Add(pf);
        }

        private static void Median()
        {
            var d = _hostile.Values.Select(h => h.MaxDist).Where(x => x > 0).OrderBy(x => x).ToList();
            _medianDist = d.Count > 0 ? d[d.Count / 2] : DefaultRadius;
        }

        private static void Save()
        {
            foreach (var g in _aggros.GroupBy(x => x.Pf).Where(g => g.Count() > MaxAggrosPerZone).ToList())
                foreach (var old in g.OrderByDescending(x => x.At).Skip(MaxAggrosPerZone).ToList()) _aggros.Remove(old);
            try { if (_file != null) File.WriteAllText(_file, JsonConvert.SerializeObject(new Store { Kinds = _hostile, Aggros = _aggros }, Formatting.Indented)); } catch { }
        }

        /// <summary>A mob turned on him or a pet outdoors, and none of us had fought it: its kind is hostile.</summary>
        public static void NoteAggro(int pf, string name, int lvl, float dist, Vector3 me)
        {
            if (string.IsNullOrEmpty(name) || pf < 0 || pf >= 100000) return;
            lock (Gate)
            {
                bool isNew = !_hostile.ContainsKey(name);
                float before = isNew ? 0 : _hostile[name].MaxDist;
                Note(name, pf, dist, DateTime.UtcNow);
                _aggros.Add(new AggroAt { Pf = pf, X = me.X, Z = me.Z, Dist = dist, Name = name, Lvl = lvl, At = DateTime.UtcNow });
                Save();
                Median();
                _hostileVer++;
                _log($"MOBDANGER: '{name}' lvl {lvl} turned on us unprovoked at {dist:0} m, at ({me.X:0},{me.Z:0}) in {Zoning.Name(pf)}; "
                     + (isNew ? "now a known hostile kind." : _hostile[name].MaxDist != before ? $"its aggro radius is now {_hostile[name].MaxDist:0} m." : $"{_hostile[name].N} times now."));
            }
        }

        public static void Log(string s) { Action<string> l; lock (Gate) l = _log; l?.Invoke(s); }

        public static bool IsHostile(string name) { lock (Gate) return name != null && _hostile.ContainsKey(name); }

        /// <summary>The aggro radius of a kind: the farthest it has turned on us from, else the median of the known kinds.</summary>
        public static float Radius(string name)
        {
            lock (Gate) return name != null && _hostile.TryGetValue(name, out var h) && h.MaxDist > 0 ? h.MaxDist : _medianDist;
        }

        /// <summary>His own level (outdoors the pets never fight, so not the pets').</summary>
        public static void SetMyLevel(int lvl) { lock (Gate) if (lvl > 0) _myLevel = lvl; }

        /// <summary>How much one mob of this level counts against him.</summary>
        public static float Weight(int mobLvl)
        {
            int me; lock (Gate) me = _myLevel;
            if (me <= 0 || mobLvl <= 0) return 1f;
            return (float)Math.Max(0.25, Math.Min(16.0, Math.Pow(2.0, (mobLvl - me) / 10.0)));
        }

        /// <summary>The hostile kinds the server shows now in zone pf (not the ones on him or following him).</summary>
        public static void SetLive(int pf, List<LiveMob> mobs)
        {
            lock (Gate)
            {
                // changes that matter to a route: a mob more or less, or one moved to another 8 m square
                string sig = pf + "|" + string.Join(";", mobs.Select(m => $"{m.Name}@{(int)Math.Floor(m.Pos.X / 8)},{(int)Math.Floor(m.Pos.Z / 8)}").OrderBy(s => s));
                if (sig == _liveSig) return;
                _liveSig = sig; _livePf = pf; _live = mobs.ToList();
                LiveVersion++;
            }
        }

        public static List<LiveMob> Live(int pf) { lock (Gate) return _livePf == pf ? _live.ToList() : new List<LiveMob>(); }

        /// <summary>Changes whenever the atlas layer for pf would come out different (hostile kinds, atlas file, his level).</summary>
        public static int Key(int pf)
        {
            lock (Gate)
            {
                LoadAtlas(pf);
                int av = _atlas.TryGetValue(pf, out var a) ? a.Ver : 0;
                return unchecked(((_hostileVer * 397) ^ av) * 397 ^ _myLevel);
            }
        }

        /// <summary>The places hostile mobs stand in zone pf, from the atlas, weighted for his level.</summary>
        public static List<Spot> AtlasSpots(int pf)
        {
            lock (Gate)
            {
                LoadAtlas(pf);
                var list = new List<Spot>();
                if (!_atlas.TryGetValue(pf, out var a) || a.Bins == null) return list;
                foreach (var b in a.Bins.Values)
                {
                    var mobs = b.Mobs.Where(m => _hostile.ContainsKey(m.name)).ToList();
                    if (mobs.Count == 0 || b.Visits <= 0) continue;
                    float w = mobs.Sum(m => Weight(m.lvl)) / b.Visits;
                    float r = mobs.Max(m => Radius(m.name));
                    list.Add(new Spot { X = b.SumX / b.N, Z = b.SumZ / b.N, W = w, R = r, Names = string.Join("/", mobs.Select(m => m.name).Distinct()) });
                }
                // ...and every place one turned on him: it stood up to Dist from him, and turns on whoever comes within its radius
                foreach (var g in _aggros.Where(x => x.Pf == pf))
                    list.Add(new Spot { X = g.X, Z = g.Z, W = Weight(g.Lvl), R = Radius(g.Name) + g.Dist, Names = g.Name });
                return list;
            }
        }

        /// <summary>The live hostiles of pf as spots (one each, its own level weight).</summary>
        public static List<Spot> LiveSpots(int pf)
            => Live(pf).Select(m => new Spot { X = m.Pos.X, Z = m.Pos.Z, W = Weight(m.Lvl), R = Radius(m.Name), Live = true, Names = m.Name }).ToList();

        // Re-read a zone's atlas when it has grown and the last read is over two minutes old (the atlas appends as he walks).
        private static void LoadAtlas(int pf)
        {
            if (_dir == null || pf < 0 || pf >= 100000) return;
            string path = Path.Combine(_dir, "mobs", pf + ".jsonl");
            long len;
            try { len = File.Exists(path) ? new FileInfo(path).Length : 0; } catch { return; }
            if (_atlas.TryGetValue(pf, out var a) && (a.Len == len || (DateTime.UtcNow - a.ReadAt).TotalSeconds < 120)) return;
            a = new Atlas { Len = len, ReadAt = DateTime.UtcNow, Bins = new Dictionary<(int, int), RawBin>(), Ver = ++_atlasVer };
            _atlas[pf] = a;
            if (len == 0) return;
            var visits = new Dictionary<(int, int), HashSet<long>>();
            var seen = new HashSet<(string, long)>();
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var sr = new StreamReader(fs))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Length < 10) continue;
                        JObject o;
                        try { o = JObject.Parse(line); } catch { continue; }
                        var first = o["first"] as JArray;
                        if (first == null || first.Count < 3) continue;
                        float x = (float)first[0], z = (float)first[2];
                        DateTime t = o["t"]?.Type == JTokenType.Date ? (DateTime)o["t"] : DateTime.TryParse((string)o["t"], null, System.Globalization.DateTimeStyles.RoundtripKind, out var tt) ? tt : DateTime.MinValue;
                        long win = (long)Math.Floor(t.ToUniversalTime().Subtract(DateTime.UnixEpoch).TotalSeconds / WindowSecs);
                        var key = ((int)Math.Floor(x / Bin), (int)Math.Floor(z / Bin));
                        for (int dx = -1; dx <= 1; dx++)
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                var k = (key.Item1 + dx, key.Item2 + dz);
                                if (!visits.TryGetValue(k, out var s)) visits[k] = s = new HashSet<long>();
                                s.Add(win);
                            }
                        string name = (string)o["name"];
                        // every sighting of an NPC in its bin (hostility is judged when the spots are made: a kind learned
                        // hostile later counts at once); one per mob per window - a mob in and out of range is one mob.
                        if (string.IsNullOrEmpty(name) || !seen.Add(((string)o["id"] ?? name + x + z, win))) continue;
                        if (!a.Bins.TryGetValue(key, out var b)) a.Bins[key] = b = new RawBin();
                        b.SumX += x; b.SumZ += z; b.N++;
                        b.Mobs.Add((name, (int?)o["lvl"] ?? 0));
                    }
                }
            }
            catch (Exception ex) { _log($"MOBDANGER: atlas {pf} unreadable: {ex.Message}"); return; }
            foreach (var kv in a.Bins) kv.Value.Visits = visits.TryGetValue(kv.Key, out var v) ? v.Count : 1;
        }

        /// <summary>
        /// A live hostile standing within its aggro radius of the path ahead (from pos along path[from..], up to 'ahead'
        /// metres), where the path is not one of the owner's recorded roads. who = what and where, for the log.
        /// </summary>
        public static bool ThreatAhead(int pf, IList<Vector3> path, int from, Vector3 pos, float ahead, out string who)
        {
            who = null;
            var live = Live(pf);
            if (live.Count == 0 || path == null || path.Count == 0 || from >= path.Count) return false;
            if (from < 0)
            {
                // where he is along it: the end of the segment nearest him
                float bd = float.MaxValue; from = 0;
                for (int i = 1; i < path.Count; i++)
                {
                    float d = Movement.Flat(Closest(path[i - 1], path[i], pos), pos);
                    if (d < bd) { bd = d; from = i; }
                }
            }
            var roads = LearnedGround.Roads();
            var hits = new List<string>();
            foreach (var m in live)
            {
                float r = Radius(m.Name), walked = 0;
                Vector3 a = pos;
                for (int i = Math.Max(0, from); i < path.Count && walked < ahead; i++)
                {
                    Vector3 b = path[i];
                    Vector3 q = Closest(a, b, m.Pos);
                    if (Movement.Flat(q, m.Pos) < r && !OnRoad(roads, q)) { hits.Add($"'{m.Name}' lvl {m.Lvl} at ({m.Pos.X:0},{m.Pos.Z:0})"); break; }
                    walked += Movement.Flat(a, b);
                    a = b;
                }
            }
            if (hits.Count == 0) return false;
            who = $"{hits.Count} hostile(s) on the way: {string.Join(", ", hits.Take(5))}";
            return true;
        }

        private static Vector3 Closest(Vector3 a, Vector3 b, Vector3 p)
        {
            float dx = b.X - a.X, dz = b.Z - a.Z, l2 = dx * dx + dz * dz;
            float t = l2 < 1e-6f ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Z - a.Z) * dz) / l2, 0f, 1f);
            return new Vector3(a.X + dx * t, a.Y + (b.Y - a.Y) * t, a.Z + dz * t);
        }

        // On one of the owner's recorded roads (3 m, same height band): those keep their priority.
        private static bool OnRoad(List<List<Vector3>> roads, Vector3 q)
        {
            foreach (var r in roads)
                for (int i = 1; i < r.Count; i++)
                {
                    var c = Closest(r[i - 1], r[i], q);
                    if (Movement.Flat(c, q) < 3f && Math.Abs(c.Y - q.Y) < 3f) return true;
                }
            return false;
        }

        /// <summary>The spots (atlas and live) of pf that a route passes within their aggro radius, and its closest approach
        /// to any of them. For the log.</summary>
        public static (int passes, int total, float closest) Along(int pf, IList<Vector3> route)
        {
            var spots = AtlasSpots(pf); spots.AddRange(LiveSpots(pf));
            int passes = 0; float closest = float.MaxValue;
            foreach (var s in spots)
            {
                var p = new Vector3(s.X, 0, s.Z);
                float best = float.MaxValue;
                for (int i = 1; i < route.Count; i++) best = Math.Min(best, Movement.Flat(Closest(route[i - 1], route[i], p), p));
                if (best < s.R) passes++;
                closest = Math.Min(closest, best);
            }
            return (passes, spots.Count, closest);
        }
    }
}
