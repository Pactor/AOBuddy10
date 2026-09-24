using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AOSharp.Common.GameData;
using Newtonsoft.Json;

namespace AOBuddy
{
    public enum ExitKind
    {
        ZoneLine,
        Teleport,
        Line,
        Proxy,
        Scotty
    }

    /// <summary>
    /// One way out of a playfield. Zone lines are crossed by walking over A-B; the rest are objects at A
    /// (== B) that are used; Scotty warps are a tell to scottyboi and work from anywhere (FromPf 0).
    /// </summary>
    public sealed class ZoneExit
    {
        public ExitKind Kind;
        public int FromPf, ToPf;
        public Vector3 A, B;
        public Vector3? Arrival; // where you come out; null = not known
        public Vector3? ArrivalA, ArrivalB; // zone lines: the far side's arrival line, runs opposite to A-B
        public bool ArrivalGuessed; // Arrival is estimated from the exits leading back
        public int Idx, Flags;
        public int[][] Reqs; // [stat, operator, value], postfix; null = none
        public string ReqText;
        public int ObjType, ObjInstance, Template;
        public string Tell, Label; // Scotty only

        public override string ToString()
        {
            switch (Kind)
            {
                case ExitKind.ZoneLine: return $"zone line to {Zoning.Name(ToPf)}";
                case ExitKind.Scotty: return $"/tell scty {Tell} ({Label}, {Zoning.Name(ToPf)})";
                default: return $"{Kind.ToString().ToLower()} {ObjType}:{ObjInstance} to {Zoning.Name(ToPf)}";
            }
        }
    }

    /// <summary>One crossing: walk to WalkTo in FromPf (then on to CrossTo for zone lines), use Exit, come out at ArriveAt.</summary>
    public sealed class ZoneHop
    {
        public int FromPf;
        public ZoneExit Exit;
        public Vector3? WalkTo; // null for Scotty
        public Vector3? CrossTo; // zone lines: a few metres past the line
        public Vector3? ArriveAt; // estimate; null = not known
        public double Cost; // cumulative, up to and including this hop

        public override string ToString()
        {
            string at = WalkTo.HasValue ? $" at ({WalkTo.Value.X:0},{WalkTo.Value.Z:0})" : "";
            string arrive = ArriveAt.HasValue ? $" -> ({ArriveAt.Value.X:0},{ArriveAt.Value.Z:0}){(Exit.ArrivalGuessed ? "~" : "")}" : " -> (?)";
            return $"{Zoning.Name(FromPf)}: {Exit}{at}{arrive}";
        }
    }

    /// <summary>
    /// One leg for the navigation controller: get to (X, Y) in Pf (map x, y = world X, Z), then take Exit there.
    /// Zone lines: (X, Y) is just past the line, so walking to it crosses. Objects: (X, Y) is the object, use it.
    /// Scotty: (X, Y) is wherever the bot is at that point, send the tell. Exit null = the destination itself.
    /// HasPos is false when the point isn't known (after a proxy whose arrival couldn't be estimated).
    /// </summary>
    public readonly record struct ZoneWaypoint(float X, float Y, int Pf, ZoneExit Exit)
    {
        public bool HasPos => !float.IsNaN(X);
    }

    public sealed class ZoneRoute
    {
        public List<ZoneHop> Hops = new List<ZoneHop>();
        public double Cost; // includes the final walk to the goal
        public int Crossings => Hops.Count;

        public string Describe()
        {
            if (Hops.Count == 0) return $"already in the playfield, cost {Cost:0}";
            var sb = new StringBuilder($"{Crossings} crossing(s), cost {Cost:0}: ");
            sb.Append(string.Join("; ", Hops.Select((h, i) => $"{i + 1}. {h}")));
            return sb.ToString();
        }
    }

    public sealed class ZoneRouteOptions
    {
        // Costs are in metres of walking.
        public double ZoneLineCost = 20;
        public double TeleportCost = 30;
        public double ScottyCost = 400;
        public double UnknownWalk = 250; // walking from a point we don't know
        public bool UseScotty = true;

        // Playfields stacked in levels where walking never changes level (the Grid: decks joined only by lift
        // beams, which are exits). There a walk between points more than LevelGap apart in height is impossible.
        public Func<int, bool> SameLevelOnly = pf => pf == 152;
        public double LevelGap = 3;

        // An exit is usable when Filter (if set) allows it and, with Stat set (stat id -> value, null when
        // unreadable), its Reqs pass; terms the checker can't read count as UnknownPasses.
        public Func<ZoneExit, bool> Filter;
        public Func<int, int?> Stat;
        public bool UnknownPasses = true;
    }

    /// <summary>
    /// Route planning across playfields from GameData/Zoning.json (tools/rdb-zoning) and GameData/ScottyWarps.json.
    /// Dijkstra over exits: walking inside a playfield is flat straight-line distance, each crossing adds a fixed
    /// cost. Coordinates are AO world (x, y, z) with y up; a map's "x, y" is world X, Z.
    ///
    /// Arrival points: a zone line's arrival table entry on the far side is that side's own zone line back,
    /// wound the other way (514 of 518), so crossing at fraction t comes out at 1 - t along it. Wall polygons
    /// wind so that (-dz, dx) of a line points into its playfield. Proxies (grid, doors, apartments) carry no
    /// destination; they're estimated from the destination's exits back, or left unknown.
    ///
    /// Nothing here moves the body.
    /// </summary>
    public static class Zoning
    {
        private static Dictionary<int, string> _names = new Dictionary<int, string>();
        private static Dictionary<int, List<ZoneExit>> _exits = new Dictionary<int, List<ZoneExit>>();
        private static List<ZoneExit> _scotty = new List<ZoneExit>();
        private static List<ZoneExit> _all = new List<ZoneExit>();

        public static bool Loaded => _names.Count > 0;
        public static IReadOnlyList<ZoneExit> ScottyWarps => _scotty;

        public static string Name(int pf) => _names.TryGetValue(pf, out var n) ? $"{n} ({pf})" : pf.ToString();

        /// <summary>
        /// The options every route ask starts from (R1.10): read requirements live off the character
        /// (a stat the server hasn't sent reads as unknown, never as zero), keep the class defaults.
        /// Each asker then sets its own Filter — and UseScotty where it differs — for its exit policy.
        /// </summary>
        public static ZoneRouteOptions RouteOptions(AOSharp.Clientless.LocalPlayer me) => new ZoneRouteOptions
        {
            Stat = id => me.TryGetStat((Stat)id, out int v) ? v : (int?)null,
        };

        public static IReadOnlyList<ZoneExit> ExitsFrom(int pf) =>
            _exits.TryGetValue(pf, out var l) ? l : (IReadOnlyList<ZoneExit>)Array.Empty<ZoneExit>();

        public static void Load(string pluginDir, Action<string> log)
        {
            var names = new Dictionary<int, string>();
            var exits = new Dictionary<int, List<ZoneExit>>();
            var scotty = new List<ZoneExit>();
            string zf = Path.Combine(pluginDir, "GameData", "Zoning.json"), sf = Path.Combine(pluginDir, "GameData", "ScottyWarps.json");
            try
            {
                var z = JsonConvert.DeserializeObject<ZoningFile>(File.ReadAllText(zf));
                foreach (var kv in z.playfields)
                    names[int.Parse(kv.Key)] = kv.Value.name;
                foreach (var kv in z.playfields)
                {
                    int pf = int.Parse(kv.Key);
                    var list = new List<ZoneExit>();
                    foreach (var l in kv.Value.zoneLines ?? new List<ZlDto>())
                    {
                        if (!z.playfields.ContainsKey(l.to.ToString())) continue;
                        var e = new ZoneExit
                            { Kind = ExitKind.ZoneLine, FromPf = pf, ToPf = l.to, A = V(l.a), B = V(l.b), Idx = l.idx, Flags = l.flags };
                        if (z.playfields[l.to.ToString()].arrivals.TryGetValue(l.idx.ToString(), out var ar))
                        {
                            e.ArrivalA = V(ar[0]);
                            e.ArrivalB = V(ar[1]);
                            e.Arrival = Mid(e.ArrivalA.Value, e.ArrivalB.Value);
                        }

                        list.Add(e);
                    }

                    foreach (var t in kv.Value.teleports ?? new List<TpDto>())
                    {
                        int to = t.kind == "teleport" && t.to == 0 ? pf : t.to; // teleport to 0: same playfield (lifts)
                        if (!z.playfields.ContainsKey(to.ToString())) continue; // proxies to instances / garbage args
                        var e = new ZoneExit
                        {
                            FromPf = pf, ToPf = to, A = V(t.pos), B = V(t.pos), Idx = t.idx, Reqs = t.reqs, ReqText = t.reqText,
                            ObjType = t.id?[0] ?? 0, ObjInstance = t.id?[1] ?? 0, Template = t.template,
                        };
                        switch (t.kind)
                        {
                            case "teleport":
                                e.Kind = ExitKind.Teleport;
                                e.Arrival = V(t.dest);
                                break;
                            case "line":
                                e.Kind = ExitKind.Line;
                                if (z.playfields[to.ToString()].arrivals.TryGetValue(t.idx.ToString(), out var ar))
                                    e.Arrival = Mid(V(ar[0]), V(ar[1]));
                                break;
                            default: e.Kind = ExitKind.Proxy; break;
                        }

                        list.Add(e);
                    }

                    exits[pf] = list;
                }

                foreach (var e in exits.Values.SelectMany(l => l))
                    if (e.Arrival == null)
                    {
                        e.Arrival = GuessArrival(exits, e);
                        e.ArrivalGuessed = e.Arrival != null;
                    }
            }
            catch (Exception ex)
            {
                log($"ZONING: couldn't load {zf}: {ex.Message}");
                return;
            }

            try
            {
                if (File.Exists(sf))
                    foreach (var w in JsonConvert.DeserializeObject<ScottyFile>(File.ReadAllText(sf)).warps)
                    {
                        if (w.playfieldId == null) continue; // playfield not identified (BS Room)
                        Vector3? p = w.pos == null ? (Vector3?)null : new Vector3(w.pos.x, 0f, w.pos.z);
                        scotty.Add(new ZoneExit { Kind = ExitKind.Scotty, ToPf = w.playfieldId.Value, Arrival = p, Tell = w.tell, Label = w.label });
                    }
            }
            catch (Exception ex)
            {
                log($"ZONING: couldn't load {sf}, planning without Scotty: {ex.Message}");
                scotty.Clear();
            }

            _names = names;
            _exits = exits;
            _scotty = scotty;
            _all = exits.Values.SelectMany(l => l).Concat(scotty).ToList();
            log($"ZONING: {names.Count} playfields, {_all.Count - scotty.Count} exits, {scotty.Count} Scotty warps loaded.");
        }

        /// <summary>Playfield by id, exact name, then name prefix / substring (case-insensitive). 0 when none.</summary>
        public static int FindPlayfield(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Trim();
            if (int.TryParse(s, out int id)) return _names.ContainsKey(id) ? id : 0;
            foreach (Func<string, bool> m in new Func<string, bool>[]
                     {
                         n => n.Equals(s, StringComparison.OrdinalIgnoreCase),
                         n => n.StartsWith(s, StringComparison.OrdinalIgnoreCase),
                         n => n.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0
                     })
            {
                var hit = _names.Where(kv => m(kv.Value)).OrderBy(kv => kv.Key).ToList();
                if (hit.Count > 0) return hit[0].Key;
            }

            return 0;
        }

        /// <summary>The cheapest way from (fromPf, from) to (toPf, goal). goal null = anywhere in toPf. Null when there is none.</summary>
        public static ZoneRoute FindRoute(int fromPf, Vector3 from, int toPf, Vector3? goal, ZoneRouteOptions opt = null)
        {
            opt = opt ?? new ZoneRouteOptions();
            int n = _all.Count, start = n, end = n + 1;
            var dist = new double[n + 2];
            var prev = new int[n + 2];
            var pos = new Vector3?[n + 2];
            var walkTo = new Vector3?[n + 2];
            var crossTo = new Vector3?[n + 2];
            var done = new bool[n + 2];
            var usable = new bool?[n];
            var index = new Dictionary<ZoneExit, int>(n);
            for (int i = 0; i < n; i++) index[_all[i]] = i;
            for (int i = 0; i < n + 2; i++)
            {
                dist[i] = double.PositiveInfinity;
                prev[i] = -1;
            }

            dist[start] = 0;
            pos[start] = from;

            int PfOf(int node) => node == start ? fromPf : _all[node].ToPf;
            bool Usable(int i) => usable[i] ?? (usable[i] = CanUse(_all[i], opt)).Value;
            double Walk(Vector3? a, Vector3 b) => a.HasValue ? Flat(a.Value, b) : opt.UnknownWalk; // Scotty points and goals have no height
            bool OtherLevel(int pf, Vector3? a, Vector3 b) => a.HasValue && opt.SameLevelOnly != null && opt.SameLevelOnly(pf) && Math.Abs(a.Value.Y - b.Y) > opt.LevelGap;

            var queue = new PriorityQueue<int, double>();
            queue.Enqueue(start, 0);

            void Relax(int u, int v, double c, Vector3? p, Vector3? walk, Vector3? cross)
            {
                if (done[v] || c >= dist[v]) return;
                dist[v] = c;
                prev[v] = u;
                pos[v] = p;
                walkTo[v] = walk;
                crossTo[v] = cross;
                queue.Enqueue(v, c);
            }

            while (queue.TryDequeue(out int u, out double d))
            {
                if (done[u] || d > dist[u]) continue;
                done[u] = true;
                if (u == end) break;
                int pf = PfOf(u);
                Vector3? p = pos[u];
                if (pf == toPf) Relax(u, end, d + (goal.HasValue ? Walk(p, goal.Value) : 0), goal, goal, null);
                if (_exits.TryGetValue(pf, out var list))
                    foreach (var e in list)
                    {
                        int v = index[e];
                        if (done[v] || !Usable(v)) continue;
                        if (e.Kind == ExitKind.ZoneLine)
                        {
                            var (at, beyond, arrive) = CrossLine(e, p);
                            if (OtherLevel(pf, p, at)) continue;
                            Relax(u, v, d + Walk(p, at) + opt.ZoneLineCost, arrive, at, beyond);
                        }
                        else
                        {
                            if (OtherLevel(pf, p, e.A)) continue;
                            Relax(u, v, d + Walk(p, e.A) + opt.TeleportCost, e.Arrival, e.A, null);
                        }
                    }

                if (opt.UseScotty)
                    foreach (var e in _scotty)
                    {
                        int v = index[e];
                        if (!done[v] && Usable(v)) Relax(u, v, d + opt.ScottyCost, e.Arrival, null, null);
                    }
            }

            if (double.IsPositiveInfinity(dist[end])) return null;
            var route = new ZoneRoute { Cost = dist[end] };
            for (int v = prev[end]; v != start; v = prev[v])
                route.Hops.Add(new ZoneHop
                    { FromPf = PfOf(prev[v]), Exit = _all[v], WalkTo = walkTo[v], CrossTo = crossTo[v], ArriveAt = pos[v], Cost = dist[v] });
            route.Hops.Reverse();
            return route;
        }

        /// <summary>
        /// FindRoute flattened into legs for the navigation controller, ending with the destination itself
        /// (x, y = map coordinates; both null = anywhere in toPf, no final leg). Null when there is no way.
        /// </summary>
        /* Example usage:
  var opt = new ZoneRouteOptions
  {
      // Check exit requirements (CL for grid exits, Side, Level, ...) against the bot's own stats.
      // Return null for a stat you can't read; that term then counts as UnknownPasses.
      Stat = id => me.TryGetStat((Stat)id, out int v) ? v : (int?)null,
      UnknownPasses = true,      // e.g. "HasRunningNano" terms: assume they pass

      UseScotty = true,
      ScottyCost = 400,          // all costs are metres of walking
      ZoneLineCost = 20,
      TeleportCost = 30,
      UnknownWalk = 250,         // walking from a point we couldn't place

      // Optional extra filter, applied on top of the Stat check. Here: stay out of the grid.
      Filter = e => e.ToPf != 152 && e.FromPf != 152,
  };

  // Andromeda, near the mine: map x 3075, y 2113
  Queue<ZoneWaypoint> legs = Zoning.Waypoints(pf, me.MovementComponent.Position, 655, 3075, 2113, opt);
  if (legs == null) { _ctx.Log("No way there."); return; }

  while (legs.Count > 0)
  {
      ZoneWaypoint w = legs.Dequeue();
      if (w.Exit == null)                         { /* walk to (w.X, w.Y) in w.Pf: arrived * / }
      else if (w.Exit.Kind == ExitKind.Scotty)    { /* send w.Exit.Tell * / }
      else if (w.Exit.Kind == ExitKind.ZoneLine)  { /* walk to (w.X, w.Y); walking there crosses the line * / }    
  }                          
         */
        public static Queue<ZoneWaypoint> Waypoints(int fromPf, Vector3 from, int toPf, float? x, float? y, ZoneRouteOptions opt = null)
        {
            Vector3? goal = x.HasValue && y.HasValue ? new Vector3(x.Value, 0f, y.Value) : (Vector3?)null;
            var r = FindRoute(fromPf, from, toPf, goal, opt);
            if (r == null) return null;
            var q = new Queue<ZoneWaypoint>();
            Vector3? here = from;
            foreach (var h in r.Hops)
            {
                Vector3? at = h.CrossTo ?? h.WalkTo ?? here; // Scotty: tell from where we stand
                q.Enqueue(new ZoneWaypoint(at?.X ?? float.NaN, at?.Z ?? float.NaN, h.FromPf, h.Exit));
                here = h.ArriveAt;
            }

            if (goal.HasValue) q.Enqueue(new ZoneWaypoint(goal.Value.X, goal.Value.Z, toPf, null));
            return q;
        }

        /// <summary>
        /// Where to cross a zone line coming from p: the nearest point on it, kept 2 m off the ends, a point
        /// 3 m past it, and where that comes out on the far side.
        /// </summary>
        public static (Vector3 at, Vector3 beyond, Vector3? arrive) CrossLine(ZoneExit e, Vector3? p)
        {
            float dx = e.B.X - e.A.X, dz = e.B.Z - e.A.Z;
            float len = (float)Math.Sqrt(dx * dx + dz * dz);
            float t = 0.5f;
            if (p.HasValue && len > 0.01f)
            {
                t = ((p.Value.X - e.A.X) * dx + (p.Value.Z - e.A.Z) * dz) / (len * len);
                float margin = Math.Min(0.5f, 2f / len);
                t = Math.Max(margin, Math.Min(1 - margin, t));
            }

            Vector3 at = Lerp(e.A, e.B, t);
            Vector3 beyond = len > 0.01f ? new Vector3(at.X + dz / len * 3f, at.Y, at.Z - dx / len * 3f) : at; // outward = -(-dz, dx)
            Vector3? arrive = e.ArrivalA.HasValue ? Lerp(e.ArrivalA.Value, e.ArrivalB.Value, 1 - t) : e.Arrival;
            return (at, beyond, arrive);
        }

        public static bool CanUse(ZoneExit e, ZoneRouteOptions opt)
        {
            if (opt.Filter != null && !opt.Filter(e)) return false;
            if (opt.Stat == null || e.Reqs == null || e.Reqs.Length == 0) return true;
            return MeetsRequirements(e.Reqs, opt.Stat) ?? opt.UnknownPasses;
        }

        /// <summary>
        /// Evaluates postfix requirements ([stat, op, value]; op 3/4/42 = Or/And/Not joining the terms before it).
        /// Null when the answer hangs on a term that can't be read (an operator other than a plain stat compare,
        /// or a stat the getter returns null for). Terms left over at the end are And-ed.
        /// </summary>
        public static bool? MeetsRequirements(int[][] reqs, Func<int, int?> stat)
        {
            var stack = new Stack<bool?>();
            foreach (var r in reqs)
            {
                int s = r[0], op = r[1], v = r[2];
                switch ((UseCriteriaOperator)op)
                {
                    case UseCriteriaOperator.And:
                    case UseCriteriaOperator.Or:
                        if (stack.Count < 2) return null;
                        bool? y = stack.Pop(), x = stack.Pop();
                        stack.Push(op == (int)UseCriteriaOperator.And ? And(x, y) : Or(x, y));
                        break;
                    case UseCriteriaOperator.Not:
                        if (stack.Count < 1) return null;
                        bool? a = stack.Pop();
                        stack.Push(a.HasValue ? !a.Value : (bool?)null);
                        break;
                    default:
                        stack.Push(Compare(stat(s), (UseCriteriaOperator)op, v));
                        break;
                }
            }

            bool? all = true;
            foreach (var b in stack) all = And(all, b);
            return all;
        }

        private static bool? Compare(int? have, UseCriteriaOperator op, int v)
        {
            if (!have.HasValue) return null;
            int h = have.Value;
            switch (op)
            {
                case UseCriteriaOperator.EqualTo: return h == v;
                case UseCriteriaOperator.LessThan: return h < v;
                case UseCriteriaOperator.GreaterThan: return h > v;
                case UseCriteriaOperator.Unequal: return h != v;
                case UseCriteriaOperator.BitAnd: return (h & v) != 0;
                case UseCriteriaOperator.NotBitAnd: return (h & v) == 0;
                default: return null;
            }
        }

        private static bool? And(bool? a, bool? b) => a == false || b == false ? false : a == true && b == true ? true : (bool?)null;
        private static bool? Or(bool? a, bool? b) => a == true || b == true ? true : a == false && b == false ? false : (bool?)null;

        // A proxy names no destination point. The exits in the destination that lead back to where we came
        // from are usually beside the door we come out of: use their centre when they sit within 30 m of it.
        private static Vector3? GuessArrival(Dictionary<int, List<ZoneExit>> exits, ZoneExit e)
        {
            if (!exits.TryGetValue(e.ToPf, out var there)) return null;
            var back = there.Where(b => b.ToPf == e.FromPf && b.ToPf != b.FromPf).Select(b => Mid(b.A, b.B)).ToList();
            if (back.Count == 0) return null;
            var c = new Vector3(back.Average(b => b.X), back.Average(b => b.Y), back.Average(b => b.Z));
            return back.All(b => Vector3.Distance(b, c) <= 30f) ? c : (Vector3?)null;
        }

        private static double Flat(Vector3 a, Vector3 b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Z - b.Z) * (a.Z - b.Z));
        private static Vector3 V(float[] a) => new Vector3(a[0], a[1], a[2]);
        private static Vector3 Mid(Vector3 a, Vector3 b) => Lerp(a, b, 0.5f);

        private static Vector3 Lerp(Vector3 a, Vector3 b, float t) =>
            new Vector3(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);

        // ---- file shapes (filled by Json.NET) ----
#pragma warning disable CS0649
        sealed class ZoningFile
        {
            public int version;
            public Dictionary<string, PfDto> playfields;
        }

        sealed class PfDto
        {
            public string name;
            public List<ZlDto> zoneLines;
            public List<TpDto> teleports;
            public Dictionary<string, float[][]> arrivals;
        }

        sealed class ZlDto
        {
            public int to, idx, flags;
            public float[] a, b;
        }

        sealed class TpDto
        {
            public string kind;
            public int to, idx, template;
            public float[] pos, dest;
            public int[] id;
            public int[][] reqs;
            public string reqText;
        }

        sealed class ScottyFile
        {
            public int version;
            public List<ScottyDto> warps;
        }

        sealed class ScottyDto
        {
            public string tell, label, playfield;
            public int? playfieldId;
            public ScottyPos pos;
        }

        sealed class ScottyPos
        {
            public float x, z;
        }
#pragma warning restore CS0649
    }
}