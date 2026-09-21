using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using Newtonsoft.Json;

namespace AOBuddy
{
    /// <summary>
    /// NAV — persistent, per-playfield walkable MEMORY. Records the OWNER's clean footsteps into
    /// nav/&lt;playfieldId&gt;.json so ground we've already walked is never guessed again, and, when the
    /// bot loses the owner, it can fall back to a known walked route instead of flailing into a wall.
    ///
    /// WALL-SAFETY BY CONSTRUCTION: we have no collision data, so the graph holds ONLY edges the owner
    /// actually walked (consecutive points in a segment). No synthetic shortcuts, no straightening across
    /// un-walked space — a recorded run cannot pass through a wall because the owner didn't. See NAV_DESIGN.md.
    ///
    /// This controller NEVER moves the body. It only supplies a route (a list of already-walked points)
    /// that FOLLOW replays through its proven LoadReplay walker — so nav can't break follow/combat/travel/zone.
    /// </summary>
    public class NavController
    {
        private readonly BotContext _ctx;
        private readonly string _navDir;

        private NavZone _zone;                 // the current playfield's memory (null until first SetPlayfield)
        private readonly List<float[]> _seg = new List<float[]>();   // the run being recorded right now
        private Vector3? _lastPt;              // last recorded owner point (segment tail)
        private bool _dirty;
        private double _saveAccum;
        private double _scanAccum;             // throttle for the interactable-object scan
        private readonly HashSet<int> _seenObj = new HashSet<int>();   // object instances already catalogued this pf

        public NavController(BotContext ctx, string pluginDir)
        {
            _ctx = ctx;
            _navDir = Path.Combine(pluginDir, "nav");
            try { Directory.CreateDirectory(_navDir); } catch { }
        }

        public int PlayfieldId => _zone?.Playfield ?? 0;

        // ---- Playfield switching -------------------------------------------------

        // Called every frame with the live playfield id (AOSharp Playfield.ModelId). No-ops unless it
        // changed. On a change: finalise the current run, record a transition from the old pf's exit spot
        // to the new pf, save the old file, then load the new pf's memory.
        public void SetPlayfield(int pf, string name, Vector3? crossPos, bool realCrossing)
        {
            if (pf <= 0) return;
            if (_zone != null && _zone.Playfield == pf) return;

            int oldPf = _zone != null ? _zone.Playfield : 0;
            EndSegment("zone");

            if (_zone != null)
            {
                // Record a transition ONLY for a REAL crossing — a genuine position teleport (POSITION JUMP)
                // happened this moment, and crossPos is the pre-teleport spot (the true zone line). A bare
                // playfield-id change with NO teleport is a desync/partial-zone and would log a PHANTOM
                // zone-out at wherever the bot last stood inside (seen: 127->655 at (351,103,271) instead of
                // the real line at (65,116,319)).
                if (realCrossing && oldPf > 0 && crossPos.HasValue)
                {
                    _zone.Transitions.Add(new NavTransition { X = crossPos.Value.X, Y = crossPos.Value.Y, Z = crossPos.Value.Z, ToPf = pf, Kind = "zone", Name = "" });
                    _ctx.Log($"NAV: transition pf {oldPf} -> {pf} at ({crossPos.Value.X:0},{crossPos.Value.Y:0},{crossPos.Value.Z:0}).");
                    _dirty = true;
                }
                else if (oldPf > 0)
                    _ctx.Log($"NAV: pf {oldPf} -> {pf} with NO teleport — not recording a zone transition (desync/partial zone).");
                Save();
            }

            Load(pf, name);
            _ctx.Log($"NAV: now in pf {pf} ({name}) — {_zone.Segments.Count} segments, {TotalPoints()} points, {_zone.Transitions.Count} transitions on file.");
        }

        private void Load(int pf, string name)
        {
            _seg.Clear();
            _lastPt = null;
            string file = FileFor(pf);
            try
            {
                if (File.Exists(file))
                {
                    _zone = JsonConvert.DeserializeObject<NavZone>(File.ReadAllText(file)) ?? NewZone(pf, name);
                    if (_zone.Segments == null) _zone.Segments = new List<List<float[]>>();
                    if (_zone.Transitions == null) _zone.Transitions = new List<NavTransition>();
                    _zone.Playfield = pf;
                    if (string.IsNullOrEmpty(_zone.Name)) _zone.Name = name;
                }
                else
                {
                    _zone = NewZone(pf, name);
                }
            }
            catch (Exception ex)
            {
                _ctx.Log($"NAV: load {file} failed ({ex.Message}) — starting fresh.");
                _zone = NewZone(pf, name);
            }
            _seenObj.Clear();
            if (_zone.Objects != null) foreach (var o in _zone.Objects) _seenObj.Add(o.Instance);
            _scanAccum = 0;
            _dirty = false;
        }

        private static NavZone NewZone(int pf, string name) =>
            new NavZone { Playfield = pf, Name = name, Segments = new List<List<float[]>>(), Transitions = new List<NavTransition>() };

        // ---- Recording -----------------------------------------------------------

        // Feed the owner's position each frame. clean = following properly (Assist, follow on, owner
        // visible, NOT combat/rest/sweep/travel). When not clean, the current run is closed so a break /
        // retrieve episode never becomes part of a segment.
        public void RecordOwner(Vector3 p, bool clean)
        {
            if (!_ctx.Config.NavRecord || _zone == null) return;
            if (!clean) { EndSegment("not-clean"); return; }

            if (_lastPt.HasValue)
            {
                float d = Vector3.Distance(p, _lastPt.Value);
                if (d < _ctx.Config.NavPointSpacing) return;                 // thin to a clean line
                if (d > _ctx.Config.NavSegmentBreakMeters) EndSegment("gap"); // owner blinked/teleported — split
            }

            _seg.Add(new[] { p.X, p.Y, p.Z });
            _lastPt = p;
            _dirty = true;
        }

        // Finalise the run in progress. Runs of a single point are dropped (no edge).
        private void EndSegment(string why)
        {
            if (_seg.Count >= 2 && _zone != null)
            {
                _zone.Segments.Add(new List<float[]>(_seg));
                _dirty = true;
                _ctx.Log($"NAV: segment closed ({why}) — {_seg.Count} pts; pf {_zone.Playfield} now {_zone.Segments.Count} segments.");
            }
            _seg.Clear();
            _lastPt = null;
        }

        public void Tick(double dt)
        {
            if (_zone != null && (_scanAccum += dt) >= 2.0) { _scanAccum = 0; ScanObjects(); ScanMobs(); }
            if (!_dirty) return;
            _saveAccum += dt;
            if (_saveAccum >= _ctx.Config.NavAutosaveSec) { _saveAccum = 0; Save(); }
        }

        // Catalog every interactable world object the bot can currently see (terminals — shop/bank/insurance/
        // mission —, doors, containers, ...), with its position and name. Skips characters (players/NPCs) and
        // transient drops (corpses/loot). Deduped by instance, so each is logged once per playfield.
        public void ScanObjects()
        {
            if (_zone == null) return;
            if (_zone.Objects == null) _zone.Objects = new List<NavObject>();
            foreach (var dyn in DynelManager.AllDynels)
            {
                if (dyn == null || dyn is SimpleChar) continue;               // not players/NPCs/self
                int inst = dyn.Identity.Instance;
                if (!_seenObj.Add(inst)) continue;                            // already have it
                string type = dyn.Identity.Type.ToString();
                if (type.IndexOf("Corpse", StringComparison.OrdinalIgnoreCase) >= 0
                    || type.IndexOf("Inventory", StringComparison.OrdinalIgnoreCase) >= 0) continue;  // transient
                Vector3 p = dyn.Transform.Position;
                _zone.Objects.Add(new NavObject { X = p.X, Y = p.Y, Z = p.Z, Type = (int)dyn.Identity.Type, Instance = inst, Name = dyn.Name ?? "" });
                _dirty = true;
                _ctx.Log($"NAV: object '{dyn.Name}' [{type}] at ({p.X:0},{p.Y:0},{p.Z:0}).");
            }
        }

        // Catalog mob SPAWNS (mob spawns are consistent per zone, so this tells the bot where to go to kill
        // a named mob). Deduped by name within a radius, so one entry per named spawn point (a mob wandering
        // its spawn area doesn't multiply, but the same name spawning elsewhere is a separate entry).
        public void ScanMobs()
        {
            if (_zone == null) return;
            if (_zone.Mobs == null) _zone.Mobs = new List<NavMob>();
            foreach (var npc in DynelManager.Npcs)
            {
                if (npc == null || string.IsNullOrEmpty(npc.Name)) continue;
                Vector3 p = npc.Transform.Position;
                bool known = false;
                foreach (var m in _zone.Mobs)
                    if (string.Equals(m.Name, npc.Name, StringComparison.OrdinalIgnoreCase)
                        && Vector3.Distance(new Vector3(m.X, m.Y, m.Z), p) < 20f) { known = true; break; }
                if (known) continue;
                int lvl = 0; npc.TryGetStat(Stat.Level, out lvl);
                _zone.Mobs.Add(new NavMob { X = p.X, Y = p.Y, Z = p.Z, Name = npc.Name, Level = lvl });
                _dirty = true;
                _ctx.Log($"NAV: mob spawn '{npc.Name}' (lvl {lvl}) at ({p.X:0},{p.Y:0},{p.Z:0}).");
            }
        }

        // Flush the current run into the file too, so a save never loses the last (still-open) run.
        public void Save()
        {
            if (_zone == null) return;
            try
            {
                // Persist the in-progress run without ending it (copy, don't clear).
                var snapshot = JsonConvert.DeserializeObject<NavZone>(JsonConvert.SerializeObject(_zone));
                if (_seg.Count >= 2) snapshot.Segments.Add(new List<float[]>(_seg));
                snapshot.Updated = DateTime.Now.ToString("s");
                File.WriteAllText(FileFor(_zone.Playfield), JsonConvert.SerializeObject(snapshot, Formatting.Indented));
                _dirty = false;
            }
            catch (Exception ex) { _ctx.Log($"NAV: save failed ({ex.Message})."); }
        }

        // ---- Use: a known route toward the owner's last-seen spot -----------------

        // Wall-safe first cut: only replay WITHIN one recorded run (guaranteed walked). Find the run point
        // nearest the bot; if the bot is within NavSnapMeters of it, return the ordered slice of that run
        // from the bot's nearest point to the point nearest the owner's last-seen position. Cross-segment
        // routing (junction graph) is a later iteration — never invent an edge between runs.
        public List<Vector3> RouteToward(Vector3 from, Vector3 toward)
        {
            if (_zone == null || _zone.Segments == null || _zone.Segments.Count == 0) return null;

            // Flatten every recorded point into one node set (recordings fragment into many short runs when
            // the owner briefly leaves view, so a single-run route rarely spans both endpoints).
            List<Vector3> nodes = new List<Vector3>();
            List<int> seg = new List<int>();
            List<int> idx = new List<int>();
            for (int s = 0; s < _zone.Segments.Count; s++)
            {
                var pts = _zone.Segments[s];
                for (int i = 0; i < pts.Count; i++) { nodes.Add(At(pts, i)); seg.Add(s); idx.Add(i); }
            }
            int n = nodes.Count;
            if (n < 2) return null;

            int src = Nearest(nodes, from, out float dFrom);
            int dst = Nearest(nodes, toward, out float dTo);
            if (src < 0 || dst < 0 || src == dst) return null;
            if (dFrom > _ctx.Config.NavSnapMeters || dTo > _ctx.Config.NavSnapMeters) return null;   // both must be ON a recorded run

            // Edges: consecutive points WITHIN a run (real walked steps), PLUS "welds" between points from
            // any runs that lie within NavWeldMeters — the owner stood at both (on different passes), so that
            // short hop is the same walkable junction. Both kinds are ground the owner actually walked; no
            // invented shortcuts across un-walked space, so the route stays wall-safe.
            List<KeyValuePair<int, float>>[] adj = new List<KeyValuePair<int, float>>[n];
            for (int k = 0; k < n; k++) adj[k] = new List<KeyValuePair<int, float>>();
            float weld = _ctx.Config.NavWeldMeters;
            for (int a = 0; a < n; a++)
                for (int b = a + 1; b < n; b++)
                {
                    bool sameRun = seg[a] == seg[b] && Math.Abs(idx[a] - idx[b]) == 1;
                    float d = Vector3.Distance(nodes[a], nodes[b]);
                    if (sameRun || d <= weld)
                    {
                        adj[a].Add(new KeyValuePair<int, float>(b, d));
                        adj[b].Add(new KeyValuePair<int, float>(a, d));
                    }
                }

            // Dijkstra src -> dst over the welded walked graph.
            float[] dist = new float[n];
            int[] prev = new int[n];
            bool[] done = new bool[n];
            for (int k = 0; k < n; k++) { dist[k] = float.MaxValue; prev[k] = -1; }
            dist[src] = 0;
            for (int it = 0; it < n; it++)
            {
                int u = -1; float best = float.MaxValue;
                for (int k = 0; k < n; k++) if (!done[k] && dist[k] < best) { best = dist[k]; u = k; }
                if (u < 0 || u == dst) break;
                done[u] = true;
                foreach (var e in adj[u])
                {
                    float nd = dist[u] + e.Value;
                    if (nd < dist[e.Key]) { dist[e.Key] = nd; prev[e.Key] = u; }
                }
            }
            if (prev[dst] == -1) return null;   // the walked graph doesn't connect the two — let live/sweep handle it

            List<Vector3> route = new List<Vector3>();
            for (int k = dst; k != -1; k = prev[k]) route.Add(nodes[k]);
            route.Reverse();
            return route.Count >= 2 ? route : null;
        }

        // The recorded spots where the owner crossed OUT of THIS playfield (stored in this zone's own
        // coords, NavController.cs SetPlayfield). Lets 'zone' HEAD to the real line we came in through
        // instead of sweeping along the bot's stale facing. Aim `from` at the owner's last-seen spot so
        // we pick the line he actually used when there are several (e.g. Andromeda's five).
        public Vector3? NearestTransition(Vector3 from)
        {
            if (_zone == null || _zone.Transitions == null || _zone.Transitions.Count == 0) return null;
            Vector3 best = default(Vector3); float bd = float.MaxValue; bool found = false;
            foreach (var t in _zone.Transitions)
            {
                Vector3 p = new Vector3(t.X, t.Y, t.Z);
                float d = Vector3.Distance(p, from);
                if (d < bd) { bd = d; best = p; found = true; }
            }
            return found ? (Vector3?)best : null;
        }

        // The spot we first stood on after zoning IN (the earliest recorded point) — right by the line we
        // came in through. Zoning back out is going to this spot, so it's the target when we have no
        // recorded out-transition yet (a mission we entered but never left before). "the zone out is just
        // the entry, backwards."
        public Vector3? EntryPoint()
        {
            if (_zone == null || _zone.Segments == null) return null;
            foreach (var s in _zone.Segments) if (s != null && s.Count > 0) return At(s, 0);
            return null;
        }

        private static int Nearest(List<Vector3> nodes, Vector3 p, out float d)
        {
            int best = -1; d = float.MaxValue;
            for (int i = 0; i < nodes.Count; i++) { float dd = Vector3.Distance(p, nodes[i]); if (dd < d) { d = dd; best = i; } }
            return best;
        }

        // ---- Status --------------------------------------------------------------

        // Learn a WARP: the owner used an object (floor button / lift / terminal) and teleported. Store the
        // button's spot, where it drops you, and which object to Use — so the bot can later navigate to it
        // and use it itself (e.g. change floors within a mission), not just ride it reactively behind you.
        // Same-playfield floor buttons are captured here (a plain pf-change transition misses them).
        public void RecordWarp(Vector3 from, Vector3 to, int toPf, int objType, int objInstance)
        {
            if (_zone == null) return;
            if (_zone.Warps == null) _zone.Warps = new List<NavWarp>();
            foreach (var w in _zone.Warps)
                if (Vector3.Distance(new Vector3(w.FromX, w.FromY, w.FromZ), from) < 3f) return;   // already known
            _zone.Warps.Add(new NavWarp { FromX = from.X, FromY = from.Y, FromZ = from.Z, ToX = to.X, ToY = to.Y, ToZ = to.Z, ToPf = toPf, ObjType = objType, ObjInstance = objInstance });
            _dirty = true;
            _ctx.Log($"NAV: learned WARP at ({from.X:0},{from.Y:0},{from.Z:0}) -> ({to.X:0},{to.Y:0},{to.Z:0}) pf{toPf} obj={objType}:{objInstance}.");
        }

        public string Status()
        {
            if (_zone == null) return "Nav: no playfield loaded yet.";
            int warps = _zone.Warps != null ? _zone.Warps.Count : 0;
            int objs = _zone.Objects != null ? _zone.Objects.Count : 0;
            int mobs = _zone.Mobs != null ? _zone.Mobs.Count : 0;
            return $"Nav pf={_zone.Playfield} ({_zone.Name}): {_zone.Segments.Count} segments, {TotalPoints()} points, " +
                   $"{_zone.Transitions.Count} transitions, {warps} warps, {objs} objects, {mobs} mobs. " +
                   $"recording={_ctx.Config.NavRecord} use={_ctx.Config.NavUse} curRun={_seg.Count}pts dirty={_dirty}.";
        }

        private int TotalPoints() => (_zone == null || _zone.Segments == null) ? 0 : _zone.Segments.Sum(s => s.Count);
        private string FileFor(int pf) => Path.Combine(_navDir, pf + ".json");
        private static Vector3 At(List<float[]> seg, int i) => new Vector3(seg[i][0], seg[i][1], seg[i][2]);
    }

    // ---- On-disk model (one file per playfield) ----------------------------------

    public class NavZone
    {
        public int Playfield;
        public string Name;
        public string Updated;
        public List<List<float[]>> Segments = new List<List<float[]>>();   // each = one walked run [ [x,y,z], ... ]
        public List<NavTransition> Transitions = new List<NavTransition>();
        public List<NavWarp> Warps = new List<NavWarp>();                   // floor buttons / lifts (used-object teleports)
        public List<NavObject> Objects = new List<NavObject>();             // interactables: terminals (shop/bank/insurance/mission), doors, containers...
        public List<NavMob> Mobs = new List<NavMob>();                      // mob spawn points (name + where it spawns), for "go kill X"
    }

    // A logged mob spawn: where a named mob is found (spawns are consistent per zone).
    public class NavMob
    {
        public float X, Y, Z;
        public string Name;
        public int Level;
    }

    // A logged interactable world object: its position, its identity type, and its NAME (which is what tells
    // a Shop from a Bank from an Insurance/Mission terminal — they're all "Terminal" type).
    public class NavObject
    {
        public float X, Y, Z;
        public int Type, Instance;
        public string Name;
    }

    // A learned "warp": at (From) in this playfield, Using object (ObjType:ObjInstance) teleports you to
    // (To) (in ToPf). Captures same-playfield floor buttons that a pf-change transition can't see.
    public class NavWarp
    {
        public float FromX, FromY, FromZ;
        public float ToX, ToY, ToZ;
        public int ToPf;
        public int ObjType, ObjInstance;
    }

    public class NavTransition
    {
        public float X, Y, Z;
        public int ToPf;         // playfield the owner ended up in
        public string Kind;      // "zone" (generic crossing); "mission" tagging comes later
        public string Name;      // optional label
    }
}
