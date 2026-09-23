using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOBuddy
{
    /// <summary>
    /// OVERLAND TRAVEL - 'travelto x y pf': plan a route across playfields with Zoning.Waypoints and walk it.
    /// Each leg is one exit: walk over a zone line, walk up to a teleport/door/grid object and use it, or send
    /// Scotty a tell. After each exit it waits for the zone, then takes the next leg; the last leg walks to
    /// (x, y). Off unless the owner starts it.
    ///
    /// Walking inside an outdoor playfield follows an OverlandGrid route (ground + wall triangles), which keeps
    /// off every zone line but the one the leg means to cross: a straight line from Newland's city gate to the
    /// grid terminal cut back through the gate into the city every time (log 2026-09-24 00:50). Arriving over
    /// a zone line puts you on the line back, so each leg first steps a few metres clear of it. Dungeons have
    /// no ground grid; there the walk is a straight line. Heights come from the client's floor data.
    ///
    /// When it lands somewhere the plan did not expect, or an exit will not take after three tries, it plans
    /// again from where it stands (the exit that failed is left out). Main.Walk gives it the frame after the
    /// mission blitz and before use-object travel and follow.
    /// </summary>
    public class OverlandController
    {
        private readonly BotContext _ctx;
        private readonly Movement _move;
        private readonly string _pluginDir;
        private readonly Action<string> _tell;

        private enum Phase { Off, Loading, Walk, Settle, Use, AwaitZone, Arrived }
        private Phase _phase = Phase.Off;
        private double _phaseTime;
        private string _why = "";

        // destination
        private int _destPf;
        private float? _destX, _destY;

        // plan
        private Queue<ZoneWaypoint> _legs;
        private ZoneWaypoint _leg;
        private int _legNo, _legCount;
        private readonly HashSet<ZoneExit> _failed = new HashSet<ZoneExit>();
        private int _tries, _replans;

        // walking
        private readonly List<Vector3> _path = new List<Vector3>();
        private int _pathIndex;
        private float _bestDist;
        private double _stuckTime;

        // zone detection
        private int _lastPf;
        private Vector3 _lastPos;

        // floor heights and the walkable grid for the playfield we are in
        private AOBuddyNav _ground;
        private IWalkGrid _grid;
        private Vector3? _padApproach;                // where we stepped onto a pad from, to step off and on again
        private int _groundPf = -1;
        private readonly HashSet<int> _stuckCells = new HashSet<int>();   // cells we got stuck walking into, this leg
        private int _stuckCount;

        private const float JumpMeters = 8f;          // a one-frame move this big was the server moving us
        private const float ObjectRange = 2.5f;       // how close to walk up to an object before using it
        private const float GoalRange = 3f;
        private const float UseReach = 4.5f;          // an object may stand in its own walls: stop this close, outside them
        private const float PadReach = 0.6f;          // a pad is walked onto, not used: stop on its centre
        private const int MaxTries = 3;               // per exit, before it is written off and we plan again
        private const double ScottyWait = 45;         // seconds per tell: the warp is a cast, not instant
        private const int ScottyTells = 2;            // don't pester him
        private const int MaxReplans = 6;
        private const double StuckSeconds = 4;
        private const int MaxStuck = 4;               // re-routes around a stuck spot per leg
        private const float ClearOfLine = 4f;         // how far to step off a zone line we arrived on

        public bool Active => _phase != Phase.Off;

        /// <summary>Standing on a pad (grid exit, lift beam) waiting for it to take us: Main logs what the server sends then.</summary>
        public bool OnPad => Active && _phase == Phase.AwaitZone && IsPad(_leg.Exit);

        // The server is carrying us (a lift beam's path): don't plan or walk until it has had time to finish.
        private double _rideUntil;
        private static double Now => Environment.TickCount64 / 1000.0;
        public void OnServerMoved(double seconds) { if (Active) _rideUntil = Math.Max(_rideUntil, Now + seconds); }

        // How far the server's floor sits above ours here. Our floor data can miss a surface (a collision record the
        // client's reader crashed on): in Rome Park the server walked us at y 16 over ground the data puts at 13.6, and
        // with its corrections ignored the bot sank back each step and was pulled back every 3 s (log 2026-09-24 01:30).
        private float _yBias;

        /// <summary>
        /// A server position correction while travelling: take it, like the mission blitz does, and carry its height
        /// against our floor data forward until the next one. Returns true when handled.
        /// </summary>
        public bool OnServerCorrection(LocalPlayer me, Vector3 serverPos)
        {
            if (!Active || me == null) return false;
            Vector3 local = me.MovementComponent.Position;
            float raw = RawFloorY(serverPos.X, serverPos.Y, serverPos.Z);
            float bias = float.IsNaN(raw) ? 0f : serverPos.Y - raw;
            if (Math.Abs(bias) > 4f) bias = 0f;
            if (Math.Abs(bias - _yBias) > 0.3f || Flat(local, serverPos) > 2f)
                _ctx.Log($"OVERLAND: server put me at ({serverPos.X:0},{serverPos.Y:0.0},{serverPos.Z:0}), {Vector3.Distance(local, serverPos):0.0} m from where I thought; its floor is {bias:+0.0;-0.0} m off our data here.");
            _yBias = bias;
            Movement.SetPose(me, serverPos, me.MovementComponent.Heading);
            _move.Reset();
            _lastPos = serverPos;
            return true;
        }

        public OverlandController(BotContext ctx, Movement move, string pluginDir, Action<string> tell)
        {
            _ctx = ctx; _move = move; _pluginDir = pluginDir; _tell = tell;
        }

        // =====================================================================================================
        // Commands
        // =====================================================================================================

        /// <summary>'travelto x y pf' | 'travelto pf' | 'travelto stop' | 'travelto status'. args: everything after the command.</summary>
        public void Command(string[] args, Action<string> reply)
        {
            var me = DynelManager.LocalPlayer;
            string first = args.Length > 0 ? args[0].ToLowerInvariant() : "";
            switch (first)
            {
                case "":
                    reply("Usage: travelto <x> <y> <playfield> | travelto <playfield> | travelto stop | travelto status");
                    return;
                case "stop":
                    if (Active) { Stop("owner said stop"); reply("Travel stopped."); } else reply("Not travelling.");
                    return;
                case "status":
                    reply(Status());
                    return;
            }
            if (me == null) { reply("Not in play yet."); return; }
            if (!Zoning.Loaded) { reply("No zoning data loaded (GameData/Zoning.json)."); return; }

            float? x = null, y = null;
            int pfStart = 0;
            var num = System.Globalization.NumberStyles.Float;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (args.Length >= 3 && float.TryParse(args[0], num, inv, out float ax) && float.TryParse(args[1], num, inv, out float ay)) { x = ax; y = ay; pfStart = 2; }
            string pfName = string.Join(" ", args.Skip(pfStart));
            int pf = Zoning.FindPlayfield(pfName);
            if (pf == 0) { reply($"No playfield '{pfName}'."); return; }

            Start(me, x, y, pf, reply);
        }

        private void Start(LocalPlayer me, float? x, float? y, int pf, Action<string> reply)
        {
            if (Active) Stop("new destination");
            _destPf = pf; _destX = x; _destY = y;
            _failed.Clear(); _replans = 0;
            if (!Plan(me, out string summary)) { reply($"No way to {Zoning.Name(pf)} from here."); _phase = Phase.Off; return; }
            if (!Active) return;   // already there; Done() has said so
            string where = x.HasValue ? $"({x:0},{y:0}) in {Zoning.Name(pf)}" : Zoning.Name(pf);
            reply($"Travelling to {where}: {summary}. 'travelto stop' cancels.");
        }

        public void Stop(string why)
        {
            if (_phase == Phase.Off) return;
            var me = DynelManager.LocalPlayer;
            if (me != null) _move.Stop(me, _ctx.Config.SendIntervalMs);
            _ctx.Log($"OVERLAND: stopped ({why}) in phase {_phase}.");
            _phase = Phase.Off; _legs = null; _path.Clear();
        }

        public string Status()
        {
            if (!Active) return "Not travelling.";
            string where = _destX.HasValue ? $"({_destX:0},{_destY:0}) in {Zoning.Name(_destPf)}" : Zoning.Name(_destPf);
            return $"Travelling to {where}. Leg {_legNo}/{_legCount}: {LegText(_leg)}. {_phase} ({_why}).";
        }

        private static string LegText(ZoneWaypoint w) =>
            w.Exit == null ? $"walk to ({w.X:0},{w.Y:0})"
            : w.Exit.Kind == ExitKind.Scotty ? $"tell scty {w.Exit.Label} to {Zoning.Name(w.Exit.ToPf)}"
            : w.Exit.ToString() + (w.HasPos ? $" at ({w.X:0},{w.Y:0})" : "");

        // =====================================================================================================
        // Planning
        // =====================================================================================================

        private ZoneRouteOptions Options(LocalPlayer me) => new ZoneRouteOptions
        {
            Stat = id => me.TryGetStat((Stat)id, out int v) ? v : (int?)null,
            UnknownPasses = true,
            // An object exit we cannot name can't be used; one that failed three times this trip is not tried again.
            Filter = e => !_failed.Contains(e) && (e.Kind == ExitKind.ZoneLine || e.Kind == ExitKind.Scotty || e.ObjInstance != 0),
        };

        private bool Plan(LocalPlayer me, out string summary)
        {
            summary = "";
            int pf = (int)Playfield.ModelId;
            Vector3 pos = me.MovementComponent.Position;
            var legs = Zoning.Waypoints(pf, pos, _destPf, _destX, _destY, Options(me));
            if (legs == null) { _ctx.Log($"OVERLAND: no route from {Zoning.Name(pf)} ({pos.X:0},{pos.Z:0}) to {Zoning.Name(_destPf)}."); return false; }
            _legs = legs;
            _legCount = legs.Count; _legNo = 0;
            summary = legs.Count == 0 ? "already there" : $"{legs.Count} leg(s): " + string.Join("; ", legs.Select(LegText));
            _ctx.Log($"OVERLAND: planned from {Zoning.Name(pf)} ({pos.X:0},{pos.Z:0}): {summary}");
            _lastPf = pf; _lastPos = pos;
            NextLeg(me);
            return true;
        }

        private void Replan(LocalPlayer me, string why)
        {
            _replans++;
            _ctx.Log($"OVERLAND: planning again ({why}), replan {_replans}.");
            if (_replans > MaxReplans) { Fail($"planned {MaxReplans} times and still didn't get there ({why})"); return; }
            if (!Plan(me, out _)) Fail($"no way to {Zoning.Name(_destPf)} from {Zoning.Name((int)Playfield.ModelId)} ({why})");
        }

        private void NextLeg(LocalPlayer me)
        {
            _path.Clear(); _tries = 0; _stuckCells.Clear(); _stuckCount = 0;
            if (_legs == null || _legs.Count == 0)
            {
                // Out of legs: with no coordinates, being in the playfield is the destination.
                if ((int)Playfield.ModelId == _destPf) { Done(me); return; }
                Replan(me, "ran out of legs in the wrong playfield");
                return;
            }
            _leg = _legs.Dequeue();
            _legNo++;
            int pf = (int)Playfield.ModelId;
            if (_leg.Pf != pf) { Replan(me, $"the next leg starts in {Zoning.Name(_leg.Pf)} but I'm in {Zoning.Name(pf)}"); return; }
            _ctx.Log($"OVERLAND: leg {_legNo}/{_legCount}: {LegText(_leg)}");
            BeginLeg(me);
        }

        private void BeginLeg(LocalPlayer me)
        {
            Vector3 pos = me.MovementComponent.Position;
            _path.Clear(); _pathIndex = 0;
            if (_leg.Exit?.Kind == ExitKind.Scotty) { Enter(Phase.Use, "telling Scotty"); return; }
            if (!EnsureNav()) { Hold(me); Enter(Phase.Loading, "loading this playfield's floor data"); return; }

            // Where this leg walks to, and what comes after the routed part.
            Vector3 goal;
            Vector3? across = null;
            string what;
            if (_leg.Exit == null) { goal = new Vector3(_leg.X, _grid is FloorGrid ? float.NaN : pos.Y, _leg.Y); what = "the destination"; }
            else if (_leg.Exit.Kind == ExitKind.ZoneLine)
            {
                // Cross where the line is nearest to where we actually stand (the plan may have guessed where we
                // would be): route to a point 3 m before it, then straight over. A retry after a crossing that did
                // not take starts from past the line, and the route brings us back over first.
                var (at, beyond, _) = Zoning.CrossLine(_leg.Exit, pos);
                goal = new Vector3(2 * at.X - beyond.X, at.Y, 2 * at.Z - beyond.Z);
                across = beyond;
                what = _tries > 0 ? $"the zone line, try {_tries + 1}" : "the zone line";
            }
            else
            {
                if (!_leg.HasPos) { FailExit(me, "don't know where the object is"); return; }
                goal = _leg.Exit.A;   // with its height: in the Grid the level matters
                what = IsPad(_leg.Exit) ? (_leg.Exit.ToPf == _leg.Pf ? "the lift beam" : "the exit pad") + (_tries > 0 ? $", try {_tries + 1}" : "")
                                        : "the " + _leg.Exit.Kind.ToString().ToLower();
            }

            EnsureNav();
            Vector3 from = pos;
            Vector3? off = StepOffLine(pos, _leg.Exit);
            if (off.HasValue) { _path.Add(off.Value); from = off.Value; }
            // A pad that did not take: step back off it the way we came, then on again.
            if (IsPad(_leg.Exit) && _tries > 0 && _padApproach.HasValue) { _path.Add(_padApproach.Value); from = _padApproach.Value; }

            // Standing on something the ground grid doesn't know (Harry's in Lush Fields, where Scotty drops you, is 8 m up on
            // a structure with no surfaces in our data): the grid doesn't describe where we are, so walk straight.
            double groundHere = _ground?.Ground?.HeightAt(from.X, from.Z) ?? double.NaN;
            bool offGrid = _grid is OverlandGrid && !double.IsNaN(groundHere) && Math.Abs(from.Y - groundHere) > 2;
            if (offGrid) _ctx.Log($"OVERLAND: I'm {from.Y - groundHere:0.0} m off the ground data here (on a structure it lacks); walking straight to {what}.");
            if (_grid != null && !offGrid)
            {
                var extra = new HashSet<int>(_stuckCells);
                foreach (var e in Zoning.ExitsFrom(_grid.Pf))
                    if (e.Kind == ExitKind.ZoneLine && e != _leg.Exit) _grid.CellsAlong(e.A, e.B, 2f, extra);
                float reach = _leg.Exit == null ? GoalRange : _leg.Exit.Kind == ExitKind.ZoneLine ? 1.5f : IsPad(_leg.Exit) ? PadReach : UseReach;
                var route = _grid.FindPath(from, goal, extra, 8f, reach, out string why);
                if (route == null && Flat(from, goal) <= 40f)
                {
                    // Close by, the data is more likely wrong than the way blocked (a gap in its walls, a missing surface).
                    _ctx.Log($"OVERLAND: no route to {what} on the data ({why}); it's {Flat(from, goal):0} m, walking straight.");
                    route = new List<Vector3> { from, goal };
                }
                if (route == null)
                {
                    if (_leg.Exit == null) Fail($"no way on foot to ({goal.X:0},{goal.Z:0}) in {Zoning.Name(_grid.Pf)}: {why}");
                    else FailExit(me, "no way on foot to it: " + why);
                    return;
                }
                _path.AddRange(route.Skip(1));
                _ctx.Log($"OVERLAND: route to {what}: {route.Count} points, {Length(route):0} m{(off.HasValue ? ", after stepping off the zone line" : "")}.");
            }
            else _path.Add(goal);   // no grid (or off it): straight there
            if (across.HasValue) _path.Add(across.Value);
            Enter(Phase.Walk, "walking to " + what);
        }

        // Arriving over a zone line leaves us standing on the line back. Step ClearOfLine metres into this playfield
        // off any line we are on (except the one this leg crosses) before routing, so the first step can't zone us back.
        private Vector3? StepOffLine(Vector3 pos, ZoneExit keep)
        {
            foreach (var e in Zoning.ExitsFrom((int)Playfield.ModelId))
            {
                if (e.Kind != ExitKind.ZoneLine || e == keep) continue;
                float dx = e.B.X - e.A.X, dz = e.B.Z - e.A.Z, len2 = dx * dx + dz * dz;
                if (len2 < 0.01f) continue;
                float t = Math.Max(0f, Math.Min(1f, ((pos.X - e.A.X) * dx + (pos.Z - e.A.Z) * dz) / len2));
                float cx = e.A.X + dx * t, cz = e.A.Z + dz * t;
                if (Flat(pos, new Vector3(cx, 0, cz)) > ClearOfLine) continue;
                float len = (float)Math.Sqrt(len2), nx = -dz / len, nz = dx / len;   // (-dz, dx) points into the playfield (Zoning)
                var p = new Vector3(cx + nx * ClearOfLine, pos.Y, cz + nz * ClearOfLine);
                _ctx.Log($"OVERLAND: on the zone line to {Zoning.Name(e.ToPf)}, stepping off it to ({p.X:0},{p.Z:0}).");
                return p;
            }
            return null;
        }

        private static float Length(List<Vector3> pts)
        {
            float l = 0;
            for (int i = 1; i < pts.Count; i++) l += Flat(pts[i - 1], pts[i]);
            return l;
        }

        // An exit did not take after MaxTries: leave it out and plan around it.
        private void FailExit(LocalPlayer me, string why)
        {
            _ctx.Log($"OVERLAND: giving up on {LegText(_leg)}: {why}.");
            if (_leg.Exit != null) _failed.Add(_leg.Exit);
            Replan(me, why);
        }

        private void Enter(Phase p, string why)
        {
            if (p != _phase || why != _why) _ctx.Log($"OVERLAND: {_phase} -> {p}: {why}");
            _phase = p; _why = why; _phaseTime = 0; _stuckTime = 0; _bestDist = float.MaxValue;
        }

        private void Fail(string why)
        {
            _ctx.Log("OVERLAND: giving up - " + why);
            _tell("Travel stopped: " + why);
            Stop(why);
        }

        private void Done(LocalPlayer me)
        {
            Vector3 p = me.MovementComponent.Position;
            _tell($"Arrived in {Zoning.Name((int)Playfield.ModelId)} at ({p.X:0},{p.Z:0}).");
            Stop("arrived");
        }

        // =====================================================================================================
        // Tick
        // =====================================================================================================

        /// <summary>One frame. Returns true when overland travel owns the body this frame.</summary>
        public bool Tick(LocalPlayer me, double dt)
        {
            if (!Active) return false;
            _phaseTime += dt;
            int pf = (int)Playfield.ModelId;
            Vector3 pos = me.MovementComponent.Position;

            // Any zone or teleport, whichever phase we were in: let it settle, then see where we are.
            bool zoned = pf != _lastPf || Vector3.Distance(pos, _lastPos) > JumpMeters;   // 3D: a lift beam moves us straight up
            _lastPf = pf; _lastPos = pos;
            if (zoned && _phase != Phase.Arrived)
            {
                _ctx.Log($"OVERLAND: now in {Zoning.Name(pf)} at ({pos.X:0},{pos.Y:0},{pos.Z:0}).");
                _move.Reset();
                Enter(Phase.Arrived, "zoned, letting it settle");
                return true;
            }

            switch (_phase)
            {
                case Phase.Arrived:
                    Hold(me);
                    if (_phaseTime < 1.0 || Now < _rideUntil) return true;
                    if (_leg.Exit != null && pf == _leg.Exit.ToPf)
                    {
                        // Where an exit lands is often a guess (the Grid's entrances have none), and the rest of the
                        // plan was made from that guess. Plan again from where we really are; it costs milliseconds.
                        if (!Plan(me, out _)) Fail($"no way on to {Zoning.Name(_destPf)} from {Zoning.Name(pf)}");
                    }
                    else Replan(me, $"expected {(_leg.Exit != null ? Zoning.Name(_leg.Exit.ToPf) : "to stay put")}, landed in {Zoning.Name(pf)}");
                    return true;

                case Phase.Loading:
                    Hold(me);
                    if (EnsureNav()) BeginLeg(me);
                    return true;

                case Phase.Walk:
                    WalkTick(me, dt);
                    return true;

                case Phase.Settle:
                    // Stand still a moment so the server has our stop before judging the use from where it has us.
                    Hold(me);
                    if (_phaseTime >= 0.6) Enter(Phase.Use, "using it");
                    return true;

                case Phase.Use:
                {
                    Hold(me);
                    _tries++;
                    var e = _leg.Exit;
                    if (e.Kind == ExitKind.Scotty)
                    {
                        // "scty ahanus" -> /tell scty ahanus
                        string tell = e.Tell ?? "";
                        int sp = tell.IndexOf(' ');
                        string to = sp > 0 ? tell.Substring(0, sp) : "scty", text = sp > 0 ? tell.Substring(sp + 1) : tell;
                        try { Client.Chat.SendPrivateMessage(to, text); } catch (Exception ex) { _ctx.Log("OVERLAND: tell failed: " + ex.Message); }
                        _ctx.Log($"OVERLAND: /tell {to} {text} (try {_tries}).");
                    }
                    else
                    {
                        SendUse(me, e);
                    }
                    Enter(Phase.AwaitZone, e.Kind == ExitKind.Scotty ? "waiting for Scotty" : "waiting for the zone");
                    return true;
                }

                case Phase.AwaitZone:
                {
                    Hold(me);
                    var e = _leg.Exit;
                    // A lift beam carries us up in everyone else's view, but the server may tell the bot nothing about it
                    // (2026-09-24 01:20: no teleport, no SetPos, no move; the owner saw it arrive on the next level). The
                    // beam's landing point is in the data, so after a moment on it with no word, assume the ride.
                    if (IsPad(e) && e.ToPf == pf && e.Arrival.HasValue && _phaseTime >= 3 && Now >= _rideUntil && _tries == 1)
                    {
                        Vector3 land = e.Arrival.Value;
                        _ctx.Log($"OVERLAND: no word from the server after 3 s on the lift beam; ASSUMING it carried me to ({land.X:0},{land.Y:0},{land.Z:0}).");
                        _move.Reset();
                        Movement.SetPose(me, land, me.MovementComponent.Heading);
                        return true;   // the jump is seen next frame: Arrived, then a fresh plan from there
                    }
                    // Scotty casts the warp: the zone came 26 s after the tell (log 2026-09-24 01:35:46 -> 01:36:12), so a
                    // 20 s wait sent a second tell into a warp already on its way. Wait long, and ask him twice at most.
                    double wait = e.Kind == ExitKind.Scotty ? ScottyWait : e.Kind == ExitKind.ZoneLine ? 6 : 8;
                    if (_phaseTime < wait) return true;
                    if (_tries >= (e.Kind == ExitKind.Scotty ? ScottyTells : MaxTries)) { FailExit(me, $"no zone after {_tries} tries"); return true; }
                    if (e.Kind == ExitKind.Scotty) Enter(Phase.Use, "no warp yet, asking again");
                    else BeginLeg(me);   // walk up / over again
                    return true;
                }
            }
            return false;
        }

        private void WalkTick(LocalPlayer me, double dt)
        {
            Vector3 pos = me.MovementComponent.Position;
            if (_pathIndex >= _path.Count) { ArriveWalk(me); return; }
            Vector3 wp = _path[_pathIndex];
            float d = Flat(pos, wp);
            bool last = _pathIndex == _path.Count - 1;
            float arrive = !last || _leg.Exit?.Kind == ExitKind.ZoneLine || IsPad(_leg.Exit) ? 0.5f : _leg.Exit == null ? GoalRange : ObjectRange;
            if (d <= arrive)
            {
                _pathIndex++;
                _bestDist = float.MaxValue; _stuckTime = 0;
                if (_pathIndex >= _path.Count) { ArriveWalk(me); return; }
                wp = _path[_pathIndex]; d = Flat(pos, wp);
            }

            if (d < _bestDist - 0.3f) { _bestDist = d; _stuckTime = 0; }
            else _stuckTime += dt;
            if (_stuckTime > StuckSeconds)
            {
                // Something the data does not show is in the way (a gap in the walls, a crate, a fence). Block the
                // few metres ahead and route around them; after MaxStuck of those, say where we are.
                _stuckCount++;
                if (_grid == null || _stuckCount > MaxStuck)
                {
                    Fail($"stuck at ({pos.X:0},{pos.Z:0}) in {Zoning.Name((int)Playfield.ModelId)}, {d:0} m short of ({wp.X:0},{wp.Z:0}) - something is in the way");
                    return;
                }
                Vector3 ahead = new Vector3(wp.X - pos.X, 0, wp.Z - pos.Z).Normalize();
                _grid.CellsAlong(new Vector3(pos.X + ahead.X, 0, pos.Z + ahead.Z), new Vector3(pos.X + ahead.X * 3, 0, pos.Z + ahead.Z * 3), 1f, _stuckCells);
                _ctx.Log($"OVERLAND: no progress toward ({wp.X:0},{wp.Z:0}) for {StuckSeconds:0} s at ({pos.X:0},{pos.Z:0}), routing round it ({_stuckCount}/{MaxStuck}).");
                Hold(me);
                BeginLeg(me);
                return;
            }

            if (d < 0.01f) return;
            Vector3 dir = new Vector3(wp.X - pos.X, 0, wp.Z - pos.Z).Normalize();
            float step = Math.Min((float)(_ctx.RunVelocity(me) * dt), _ctx.Config.MaxStep);
            step = Math.Min(step, d);
            float nx = pos.X + dir.X * step, nz = pos.Z + dir.Z * step;
            Vector3 next = new Vector3(nx, FloorY(nx, pos.Y, nz), nz);
            _ctx.WalkState = $"overland leg {_legNo}/{_legCount} wp {_pathIndex + 1}/{_path.Count} d={d:0}";
            _move.Advance(me, next, Movement.SafeLook(dir, me.MovementComponent.Heading), run: true, dt, _ctx.Config.SendIntervalMs);
            _lastPos = next;   // our own step, not a jump
        }

        private void ArriveWalk(LocalPlayer me)
        {
            Hold(me);
            if (_leg.Exit == null) { Done(me); return; }
            if (_leg.Exit.Kind == ExitKind.ZoneLine) { _tries++; Enter(Phase.AwaitZone, "over the line, waiting for the zone"); return; }
            if (IsPad(_leg.Exit))
            {
                _tries++;
                _padApproach = _path.Count >= 2 ? _path[_path.Count - 2] : (Vector3?)null;
                // Standing on it is what takes you. Only the last try also uses it, in case this one is a terminal after all.
                if (_tries >= MaxTries) SendUse(me, _leg.Exit);
                Enter(Phase.AwaitZone, "on the pad, waiting for it to take me");
                return;
            }
            Enter(Phase.Settle, "at the object");
        }

        // Grid exits and lift beams ('line' objects): pads you walk onto (owner, 2026-09-24), not terminals you use.
        private static bool IsPad(ZoneExit e) => e != null && e.Kind == ExitKind.Line;

        private void SendUse(LocalPlayer me, ZoneExit e)
        {
            var id = new Identity((IdentityType)e.ObjType, e.ObjInstance);
            Client.Send(new GenericCmdMessage { Action = GenericCmdAction.Use, User = me.Identity, Target = id, Count = 1, Temp4 = 1 });
            _ctx.Log($"OVERLAND: used {id} at ({e.A.X:0},{e.A.Y:0},{e.A.Z:0}) (try {_tries}).");
        }

        private void Hold(LocalPlayer me)
        {
            if (_move.Moving) _move.Stop(me, _ctx.Config.SendIntervalMs);
        }

        // The floor under (x, z) nearest our height, from the client's data for this playfield; our own height when there is none.
        private float FloorY(float x, float y, float z)
        {
            float h = RawFloorY(x, y - _yBias, z);
            if (float.IsNaN(h)) return y;
            h += _yBias;
            return Math.Abs(h - y) > 4 ? y : h;   // a jump of more than 4 m is a roof or a cave, not our floor
        }

        // Our data's floor under (x, z) nearest y; NaN when there is none.
        private float RawFloorY(float x, float y, float z)
        {
            if (!EnsureNav() || _ground == null) return float.NaN;   // still loading: the old playfield's floor is no answer
            double h = _ground.FloorNear(x, y, z, out _);
            return double.IsNaN(h) ? float.NaN : (float)h;
        }

        // The playfield's floor data and walkable grid, loaded once per playfield, off the update thread: Lush Fields'
        // grid took 6.3 s and froze the whole bot while it built (log 2026-09-24 01:36). True once it is ready.
        private System.Threading.Tasks.Task<(AOBuddyNav, IWalkGrid)> _navTask;
        private int _navTaskPf = -1;

        private bool EnsureNav()
        {
            int pf = (int)Playfield.ModelId;
            if (pf == _groundPf) return true;
            if (_navTask == null || _navTaskPf != pf)
            {
                string dir = _pluginDir;
                var log = _ctx.Log;
                _navTaskPf = pf;
                _navTask = System.Threading.Tasks.Task.Run(() =>
                {
                    var nav = AOBuddyNav.Load(dir, pf);
                    IWalkGrid grid = (IWalkGrid)OverlandGrid.Build(dir, pf, nav, log) ?? FloorGrid.Build(dir, pf, nav, log);
                    return (nav, grid);
                });
                return false;
            }
            if (!_navTask.IsCompleted) return false;
            var done = _navTask;
            _navTask = null;
            _groundPf = pf; _yBias = 0f; _ground = null; _grid = null;
            if (done.IsFaulted) _ctx.Log($"OVERLAND: no nav data for pf {pf}: {done.Exception?.GetBaseException().Message}");
            else (_ground, _grid) = done.Result;
            return true;
        }

        private static float Flat(Vector3 a, Vector3 b) { float dx = a.X - b.X, dz = a.Z - b.Z; return (float)Math.Sqrt(dx * dx + dz * dz); }
    }
}