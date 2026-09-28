using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Clientless.Logging;
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

        // FAILED TRIPS, REMEMBERED (TravelFailures, travelfails.json): one attempt = one Start(). The first step the
        // latest plan took from the start zone is what gets recorded when the attempt fails, and a new attempt at the
        // same goal leaves out the first steps that failed before (20:11-20:14, 2026-09-27: the same six legs three times).
        public TravelFailures Failures { get; }
        private int _attemptFromPf;
        private string _attemptFirst, _attemptFirstText;
        private bool _attemptRecorded, _ignoreRecord;
        private HashSet<string> _recordedFails = new HashSet<string>();

        // SAME ZONE, ON FOOT FIRST (owner, 2026-09-27): _walkFirst = this plan is the walk tried before any exit;
        // _noWalk = it had no way on foot, plan through the exits. _roadOut = the path is a recorded road out of a
        // walled place; at its far end the leg is planned again from there. _roadOutFrom: where each road out began,
        // so the road back in is never taken as the next way "out".
        private bool _walkFirst, _noWalk, _roadOut;
        private Vector3 _noWalkAt; private int _noWalkPf = -1;
        private readonly List<Vector3> _roadOutFrom = new List<Vector3>();

        // walking
        private readonly List<Vector3> _path = new List<Vector3>();
        private int _pathIndex;
        private Movement.StuckWatch _stuck;

        // zone detection
        private int _lastPf;
        private Vector3 _lastPos;

        // floor heights and the walkable grid for the playfield we are in
        private AOBuddyNav _ground;
        private IWalkGrid _grid;
        private Vector3? _padApproach;                // where we stepped onto a pad from, to step off and on again
        private bool _frontal;                        // this object leg crosses head-on: staging spot, then straight through
        private int _groundPf = -1;
        private readonly HashSet<int> _stuckCells = new HashSet<int>();   // cells we got stuck walking into, this leg
        private int _stuckCount;
        // hostile mobs on the way (MobDanger): the live picture the route was planned with, and the replans for it this leg
        private int _dangerVer = -1, _dangerReplans;
        private double _dangerAt = -99;
        private const int MaxDangerReplans = 3;

        private const float JumpMeters = 8f;          // a one-frame move this big was the server moving us
        private const float ObjectRange = 2.5f;       // how close to walk up to an object before using it
        private const float GoalRange = 3f;
        private const float WideGoalRange = 8f;   // a goal in a wall: the nearest reachable ground within this
        private const float UseReach = 4.5f;          // an object may stand in its own walls: stop this close, outside them
        private const float PadReach = 0.6f;          // a pad is walked onto, not used: stop on its centre
        private const int MaxTries = 3;               // per exit, before it is written off and we plan again
        private const double ScottyWait = 45;         // seconds per tell: the warp is a cast, not instant
        private const int ScottyTells = 2;            // don't pester him
        private const int MaxReplans = 6;
        private const double StuckSeconds = 4;
        public bool Swimming => _inWater;          // in the water band (no wire mode anymore — see WalkTick);
                                                   // MissionRun stands down its pull-back counter while we're wet
        private bool _inWater;
        // The server's own vertical for our wet body, from its last correction: if it starts floating us
        // where the bottom drops away, this is the surface Y we ride.
        private float _wetY;
        private double _wetYAt = -999;
        private const int MaxStuck = 4;               // re-routes around a stuck spot per leg
        private const float ClearOfLine = 4f;         // how far to step off a zone line we arrived on
        private const float StraightMaxDrop = 2f;     // an off-grid straight walk is LEVEL: the goal may sit at most this
                                                      // far above/below us. More and there is no ramp in the data — walking
                                                      // it walks on air (Newland City, 2026-09-25 16:15: the wompah station
                                                      // sits at the bowl's floor, y 27.6, under an elevated street at y 32.4
                                                      // the heightfield can't see; the straight walk crossed the plaza at
                                                      // street height, mounted the booth's roof, and stood 4.7 m ABOVE the
                                                      // stand-trigger until it was stopped by hand).

        public bool Active => _phase != Phase.Off;

        /// <summary>Standing on a pad (grid exit, lift beam) waiting for it to take us: Main logs what the server sends then.</summary>
        public bool OnPad => Active && _phase == Phase.AwaitZone && IsPad(_leg.Exit);

        // The server is carrying us (a lift beam's path): don't plan or walk until it has had time to finish.
        private double _rideUntil;
        private double Now => _ctx.Clock.Seconds;
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
            if (Math.Abs(bias) > 4f || _move.Swimming) bias = 0f;   // over water the floor says nothing about the bias
            if (Math.Abs(bias - _yBias) > 0.3f || Movement.Flat(local, serverPos) > 2f)
                _ctx.Log($"OVERLAND: server put me at ({serverPos.X:0},{serverPos.Y:0.0},{serverPos.Z:0}), {Vector3.Distance(local, serverPos):0.0} m from where I thought; its floor is {bias:+0.0;-0.0} m off our data here.");
            _yBias = bias;
            _wetY = serverPos.Y; _wetYAt = Now;   // its vertical for us is wet truth while it is fresh
            Movement.SetPose(me, serverPos, me.MovementComponent.Heading);
            _move.ResetKeepGait();   // a correction stops the packet stream, not the swim mode
            _lastPos = serverPos;
            return true;
        }

        public OverlandController(BotContext ctx, Movement move, string pluginDir, Action<string> tell)
        {
            _ctx = ctx; _move = move; _pluginDir = pluginDir; _tell = tell;
            Failures = new TravelFailures(pluginDir, ctx.Log);
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
            _attemptFromPf = (int)Playfield.ModelId; _attemptFirst = null; _attemptFirstText = null; _attemptRecorded = false; _ignoreRecord = false;
            _noWalk = false; _walkFirst = false; _roadOut = false; _roadOutFrom.Clear();
            _recordedFails = Failures.FailedFirsts(pf, DestGoal);
            if (_recordedFails.Count > 0)
                _ctx.Log($"OVERLAND: {Failures.Count(pf, DestGoal)} failed trip(s) to this goal in the last {TravelFailures.KeepHours:0} h; leaving out their first step(s): {string.Join(", ", _recordedFails)}.");
            if (!Plan(me, out string summary)) { reply($"No way to {Zoning.Name(pf)} from here."); _phase = Phase.Off; return; }
            if (!Active) return;   // already there; Done() has said so
            string where = x.HasValue ? $"({x:0},{y:0}) in {Zoning.Name(pf)}" : Zoning.Name(pf);
            reply($"Travelling to {where}: {summary}. 'travelto stop' cancels.");
        }

        private Vector3? DestGoal => _destX.HasValue && _destY.HasValue ? new Vector3(_destX.Value, 0f, _destY.Value) : (Vector3?)null;

        /// <summary>
        /// This attempt failed (called by Fail, and by the mission run when it stops travel for a reason that is the
        /// plan's fault: a route through the other side's city, too long). Recorded once per attempt.
        /// </summary>
        public void RecordFailure(string why)
        {
            if (_phase == Phase.Off || _attemptRecorded) return;
            _attemptRecorded = true;
            Failures.Record(_attemptFromPf, _destPf, DestGoal, _attemptFirst ?? TravelFailures.Walk, _attemptFirstText, why);
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

        // An object exit we cannot name can't be used; one that failed three times this trip is not tried again.
        // NOTHING ONTO THE OTHER SIDE'S GROUND (FactionMap, the map the mission run's own routes use): 20:11-20:14
        // (2026-09-27) this planned Jobe Platform -> Old Athen (540, Clan city) -> the Bliss (Clan) whompa for an Omni.
        // The avoid list ('mission run avoid') is left out too, except as the destination itself.
        // First steps that failed toward this goal before (TravelFailures) are left out unless nothing else goes.
        private ZoneRouteOptions Options(LocalPlayer me)
        {
            var o = Zoning.RouteOptions(me);
            int side = me.TryGetStat(Stat.Side, out int sd) ? sd : 0;
            var avoid = _ctx.Config.MissionAvoidZones;
            int dest = _destPf;
            var recorded = _ignoreRecord ? null : _recordedFails;
            o.Filter = e => !_failed.Contains(e) && (e.Kind == ExitKind.ZoneLine || e.Kind == ExitKind.Scotty || e.ObjInstance != 0)
                            && (recorded == null || !recorded.Contains(TravelFailures.Key(e)))
                            && !FactionMap.HostileExit(_pluginDir, _ctx.Log, side, e, z => z != dest && avoid != null && avoid.Contains(z));
            // The walk found no way on foot (or failed before): through the exits means through at least one
            // (21:00, 2026-09-27: Stret West Bank, the fallback planned the same walk again, four times).
            // Only from the spot the walk failed at: from any other landing the walk gets its own try.
            o.NoDirectWalk = (int)Playfield.ModelId == _destPf
                && ((_noWalk && _noWalkPf == _destPf && Movement.Flat(me.MovementComponent.Position, _noWalkAt) < 30f)
                    || (recorded != null && recorded.Contains(TravelFailures.Walk) && (int)Playfield.ModelId == _attemptFromPf));
            return o;
        }

        private bool Plan(LocalPlayer me, out string summary)
        {
            summary = "";
            int pf = (int)Playfield.ModelId;
            Vector3 pos = me.MovementComponent.Position;
            Queue<ZoneWaypoint> legs = null;
            _walkFirst = false;
            // SAME ZONE: WALK IT when the ground allows (owner, 2026-09-27: "prefer walking within the same playfield").
            // Zoning.FindRoute prices a walk as straight-line metres and a crossing as 20-30, so from the Longest Road
            // town to a door 1.7 km away in the same zone it planned five crossings out through the booths (cost 357).
            // BeginLeg finds the way on foot (by the recorded road out when the grid has none); only when there is no
            // way on foot does the plan fall back to the exits (_noWalk). Not in the Grid: its decks join only by lifts.
            if (pf == _destPf && _destX.HasValue && pf != 152 && !_noWalk && (_ignoreRecord || !_recordedFails.Contains(TravelFailures.Walk)))
            {
                legs = new Queue<ZoneWaypoint>();
                legs.Enqueue(new ZoneWaypoint(_destX.Value, _destY.Value, pf, null));
                _walkFirst = true;
                _ctx.Log($"OVERLAND: same playfield, {Movement.Flat(pos, new Vector3(_destX.Value, 0f, _destY.Value)):0} m: walking it; the exits only if there is no way on foot.");
            }
            if (legs == null) legs = Zoning.Waypoints(pf, pos, _destPf, _destX, _destY, Options(me));
            if (legs == null && !_ignoreRecord && _recordedFails.Count > 0)
            {
                _ctx.Log($"OVERLAND: no route without the first step(s) that failed before ({string.Join(", ", _recordedFails)}); planning with them.");
                _ignoreRecord = true;
                legs = Zoning.Waypoints(pf, pos, _destPf, _destX, _destY, Options(me));
            }
            if (legs == null) { _ctx.Log($"OVERLAND: no route from {Zoning.Name(pf)} ({pos.X:0},{pos.Z:0}) to {Zoning.Name(_destPf)} (the other side's ground and the avoid list left out)."); return false; }
            if (pf == _attemptFromPf && legs.Count > 0)
            {
                var first = legs.Peek();
                _attemptFirst = TravelFailures.Key(first.Exit);
                _attemptFirstText = LegText(first);
            }
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
            _path.Clear(); _tries = 0; _stuckCells.Clear(); _stuckCount = 0; _frontal = false; _dangerReplans = 0;
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
            _path.Clear(); _pathIndex = 0; _roadOut = false;
            if (_leg.Exit?.Kind == ExitKind.Scotty) { Enter(Phase.Use, "telling Scotty"); return; }
            if (!EnsureNav()) { _move.Hold(me, _ctx.Config.SendIntervalMs); Enter(Phase.Loading, "loading this playfield's floor data"); return; }

            // Where this leg walks to, and what comes after the routed part.
            Vector3 goal;
            Vector3? across = null;
            string what;
            if (_leg.Exit == null && _walkFirst && !(_grid is OverlandGrid))
            {
                // The on-foot-first plan is for outdoor ground; indoors (lifts, levels) the exits plan is the way.
                _walkFirst = false; _noWalk = true;
                Replan(me, "no outdoor walk grid here for the walk; planning through the exits");
                return;
            }
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
                // A WHOMPA BOOTH (ExitKind.Line, 51016) takes when you stand ON it — and exactly on it: the route's
                // own reach can end a cell or two short, which in the ICC tower's ring of side-by-side booths lands
                // you in the neighbour's alcove or the gap between them, and nothing triggers (2026-09-25 13:17:
                // stood 1.6 m off the centre for 8 s, no zone; the hike's stand 2.3 m the right way took in 0.7 s).
                // Append the centre itself so the walk finishes standing on it.
                if (_leg.Exit.Kind == ExitKind.Line) across = _leg.Exit.A;
                // ENTER HEAD-ON (owner, 2026-09-25): running into a wompa booth or a doorway from the side
                // doesn't take. The zone's walls data knows which side of the object is open ground — its
                // front — so route to a spot 3.5 m out on that side and then walk straight through the
                // centre, the same staged crossing the zone-line legs already use.
                if ((_leg.Exit.Kind == ExitKind.Teleport || _leg.Exit.Kind == ExitKind.Proxy)
                    && !IsPad(_leg.Exit) && FrontDir(goal, pos, out Vector3 front))
                {
                    goal = new Vector3(_leg.Exit.A.X + front.X * 3.5f, _leg.Exit.A.Y, _leg.Exit.A.Z + front.Z * 3.5f);
                    across = new Vector3(_leg.Exit.A.X - front.X * 1.2f, _leg.Exit.A.Y, _leg.Exit.A.Z - front.Z * 1.2f);
                    _frontal = true;
                    what += ", head-on";
                }
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
            // Off the ground data and the goal is far: a recorded road out, when one starts here, before a straight walk.
            if (offGrid && Movement.Flat(from, goal) > 40f && TryRoadOut(from, what, "off the ground data here")) return;
            if (offGrid)
            {
                _ctx.Log($"OVERLAND: I'm {from.Y - groundHere:0.0} m off the ground data here (on a structure it lacks); walking straight to {what}.");
                // LEVEL OR NOT AT ALL: the straight walk exists for pads on the SAME structure we were
                // dropped on (Harry's, ICC). When the goal sits a storey below or above us, the data has
                // no ramp to it and the walk goes over rooftops and thin air — say so and fail the leg;
                // the replan then takes a route that works (this morning's grid refusal of the same exit
                // was right; 16:15's bypass walked over the wompah instead).
                if (!float.IsNaN(goal.Y) && goal.Y - from.Y > StraightMaxDrop)
                {
                    string drop = $"the {what} is {Math.Abs(goal.Y - from.Y):0.0} m {(goal.Y < from.Y ? "below" : "above")} me and the ground data has no way {(goal.Y < from.Y ? "down" : "up")} to it";
                    if (_leg.Exit == null) Fail($"no way on foot to ({goal.X:0},{goal.Z:0}): {drop}");
                    else FailExit(me, "no way on foot to it: " + drop);
                    return;
                }
            }
            if (_grid != null && !offGrid)
            {
                var extra = new HashSet<int>(_stuckCells);
                foreach (var e in Zoning.ExitsFrom(_grid.Pf))
                {
                    if (e.Kind == ExitKind.ZoneLine && e != _leg.Exit) _grid.CellsAlong(e.A, e.B, 2f, extra);
                    // Whompa booths, proxies and teleporters take you on contact: keep 3 m off every one but the leg's own
                    // (owner, 2026-09-27: 20:11:53 travelto to a Longest Road mission door stepped onto the Broken Shores
                    // booth 4 m from the whompa landing, then Rome Park's Jobe proxy - a loop through three zones).
                    // Not one he stands in already (the first step must be able to leave it).
                    else if ((e.Kind == ExitKind.Line || e.Kind == ExitKind.Proxy || e.Kind == ExitKind.Teleport) && e != _leg.Exit
                             && Movement.Flat(e.A, goal) > 1.5f && Movement.Flat(e.A, from) > 3.5f)
                        _grid.CellsAlong(e.A, e.A, 3f, extra);
                }
                float reach = _leg.Exit == null ? GoalRange : _leg.Exit.Kind == ExitKind.ZoneLine ? 1.5f : IsPad(_leg.Exit) ? PadReach : UseReach;
                var route = _grid.FindPath(from, goal, extra, 8f, reach, out string why);
                // A door set in its building's wall has no open ground within GoalRange (21:00, 2026-09-27: Stret West
                // Bank (681,1367), floors under it blocked; open ground 5-7 m out). End the walk near it instead; the
                // mission run walks the last metres to the door itself.
                if (route == null && _leg.Exit == null)
                {
                    var near = _grid.FindPath(from, goal, extra, 8f, WideGoalRange, out _);
                    if (near != null)
                    {
                        _ctx.Log($"OVERLAND: no open ground within {reach:0} m of ({goal.X:0},{goal.Z:0}); walking to the nearest reachable spot, {Movement.Flat(near[near.Count - 1], goal):0.0} m from it.");
                        route = near;
                    }
                }
                // WALK OUT BY THE ENTRANCE (owner, 2026-09-27): no way on the grid from where he stands - or the grid's way
                // runs where the server has pulled him back (the Longest Road town: the grid climbs the ridge at
                // (1971,877)-(2013,911), 20+ snap-backs in snapbacks.json) - and a recorded road starts within 30 m: walk it
                // to its far end, then plan this leg again from there.
                if (Movement.Flat(from, goal) > 40f)
                {
                    int snaps = route != null ? LearnedGround.SnapHitsAlong(_grid.Pf, route) : 0;
                    if ((route == null || snaps >= RoadOutSnaps)
                        && TryRoadOut(from, what, route == null ? $"no route to {what} on the data ({why})" : $"the grid's way to {what} runs over {snaps} server pull-back(s)"))
                        return;
                    if (route == null) _ctx.Log($"OVERLAND: no route to {what} on the data ({why}).");
                }
                if (route == null && Movement.Flat(from, goal) <= 40f)
                {
                    // Close by, the data is more likely wrong than the way blocked (a gap in its walls, a missing surface).
                    _ctx.Log($"OVERLAND: no route to {what} on the data ({why}); it's {Movement.Flat(from, goal):0} m, walking straight.");
                    route = new List<Vector3> { from, goal };
                }
                // No way on foot from where an exit put him (21:00, 2026-09-27: the Stret East Bank teleport lands in
                // Stret West Bank at (1143,541), cut off from the door): leave out every exit landing here, so the next
                // plan doesn't bring him straight back to this spot.
                int landed = 0;
                if (route == null && _leg.Exit == null)
                    foreach (var x in Zoning.ExitsInto(_grid.Pf))
                        if (x.Arrival.HasValue && Movement.Flat(x.Arrival.Value, from) < 30f && _failed.Add(x)) landed++;
                if (landed > 0) _ctx.Log($"OVERLAND: no way on foot from ({from.X:0},{from.Z:0}); leaving out the {landed} exit(s) that land here.");
                if (route == null && _leg.Exit == null && (_walkFirst || landed > 0))
                {
                    // The same-zone walk has no way on foot from here: plan through the exits, from here.
                    _walkFirst = false; _noWalk = true; _noWalkAt = from; _noWalkPf = _grid.Pf;
                    Replan(me, $"no way on foot to ({goal.X:0},{goal.Z:0}) ({why}); planning through the exits");
                    return;
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
            _dangerVer = MobDanger.LiveVersion;   // the plan saw the live hostiles as they are now
            Enter(Phase.Walk, "walking to " + what);
        }

        // The direction of open ground at a doorway-style object — its front. Sixteen rays out from the
        // object on this zone's grid; the front is the longest open run, preferring the side we approach
        // from (it is usually the front too). False when there is no grid or no 3 m of open ground on any
        // side (an object in a wall pocket): the approach then stays as it was.
        private bool FrontDir(Vector3 obj, Vector3 from, out Vector3 dir)
        {
            dir = Vector3.Zero;
            if (_grid == null || _grid.Pf != (int)Playfield.ModelId) return false;   // a grid from another zone would ray the wrong walls
            float fx = from.X - obj.X, fz = from.Z - obj.Z;
            float fl = (float)Math.Sqrt(fx * fx + fz * fz);
            if (fl > 0.01f) { fx /= fl; fz /= fl; } else { fx = 1; fz = 0; }
            bool any = false; float best = float.MinValue;
            for (int k = 0; k < 16; k++)
            {
                double t = k * Math.PI / 8;
                float dx = (float)Math.Cos(t), dz = (float)Math.Sin(t);
                float open = 0;
                for (float d = 1; d <= 4; d += 1f)
                    if (_grid.OpenAt(new Vector3(obj.X + dx * d, obj.Y, obj.Z + dz * d))) open = d; else break;
                if (open < 3f) continue;                                   // needs room for the staging spot
                float score = open + (dx * fx + dz * fz) * 2f;             // open run first, then nearest our approach
                if (score > best) { best = score; any = true; dir = new Vector3(dx, 0, dz); }
            }
            return any;
        }

        /// <summary>The object's front direction for callers outside (MissionRun's door sweep starts on it); null without a grid.</summary>
        public Vector3? FrontOf(Vector3 obj, Vector3 from) => FrontDir(obj, from, out Vector3 d) ? d : (Vector3?)null;

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
                if (Movement.Flat(pos, new Vector3(cx, 0, cz)) > ClearOfLine) continue;
                float len = (float)Math.Sqrt(len2), nx = -dz / len, nz = dx / len;   // (-dz, dx) points into the playfield (Zoning)
                var p = new Vector3(cx + nx * ClearOfLine, pos.Y, cz + nz * ClearOfLine);
                _ctx.Log($"OVERLAND: on the zone line to {Zoning.Name(e.ToPf)}, stepping off it to ({p.X:0},{p.Z:0}).");
                return p;
            }
            return null;
        }

        // A recorded road out of where we stand (LearnedGround.RoadOut: starts within 30 m, leads away, far end on this
        // zone's ground), never one that ends where an earlier road out of this attempt began (that is the road back in).
        // Loads it as the path: a straight step to its start, then the road. True when taken.
        private const int RoadOutSnaps = 2;   // a grid way over this many remembered pull-backs is no way out when a road is

        private bool TryRoadOut(Vector3 from, string what, string reason)
        {
            if (_grid == null) return false;
            var ground = _ground?.Ground;
            var road = LearnedGround.RoadOut(from, 30f, end =>
                (ground == null || double.IsNaN(ground.HeightAt(end.X, end.Z)) || Math.Abs(ground.HeightAt(end.X, end.Z) - end.Y) < 3)
                && _roadOutFrom.All(b => Movement.Flat(b, end) > 30f));
            if (road == null) return false;
            _roadOutFrom.Add(from);
            _path.AddRange(road);
            _roadOut = true;
            var end0 = road[road.Count - 1];
            _ctx.Log($"OVERLAND: {reason}; walking out by the recorded road: {road.Count} points, {Length(road):0} m, from ({road[0].X:0},{road[0].Z:0}) {Movement.Flat(from, road[0]):0} m away to ({end0.X:0},{end0.Z:0}); then on to {what} from there.");
            Enter(Phase.Walk, "walking out by the recorded road");
            return true;
        }

        private static float Length(List<Vector3> pts)
        {
            float l = 0;
            for (int i = 1; i < pts.Count; i++) l += Movement.Flat(pts[i - 1], pts[i]);
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
            _phase = p; _why = why; _phaseTime = 0; _stuck.Reset();
        }

        private void Fail(string why)
        {
            _ctx.Log("OVERLAND: giving up - " + why);
            RecordFailure(why);
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
            if (pf != _lastPf) { _noWalk = false; _roadOutFrom.Clear(); }   // 'no way on foot' was about the zone we left
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
                    _move.Hold(me, _ctx.Config.SendIntervalMs);
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
                    _move.Hold(me, _ctx.Config.SendIntervalMs);
                    if (EnsureNav()) BeginLeg(me);
                    return true;

                case Phase.Walk:
                    WalkTick(me, dt);
                    return true;

                case Phase.Settle:
                    // Stand still a moment so the server has our stop before judging the use from where it has us.
                    _move.Hold(me, _ctx.Config.SendIntervalMs);
                    if (_phaseTime >= 0.6) Enter(Phase.Use, "using it");
                    return true;

                case Phase.Use:
                {
                    _move.Hold(me, _ctx.Config.SendIntervalMs);
                    _tries++;
                    var e = _leg.Exit;
                    if (e.Kind == ExitKind.Scotty)
                    {
                        // "scty ahanus" -> /tell scty ahanus. The chat client logs the BARE wire text ("tarden")
                        // to the console; suppress that and say where the warp is going instead — label from
                        // ScottyWarps.json plus the destination playfield's name.
                        string tell = e.Tell ?? "";
                        int sp = tell.IndexOf(' ');
                        string to = sp > 0 ? tell.Substring(0, sp) : "scty", text = sp > 0 ? tell.Substring(sp + 1) : tell;
                        try { Client.Chat.SendPrivateMessage(to, text, false); } catch (Exception ex) { _ctx.Log("OVERLAND: tell failed: " + ex.Message); }
                        Logger.Information($"Initiate Scottywarp - {Zoning.Name(e.ToPf)} {e.Label}");
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
                    _move.Hold(me, _ctx.Config.SendIntervalMs);
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
                    double wait = e.Kind == ExitKind.Scotty ? ScottyWait
                        : e.Kind == ExitKind.Line ? 10        // a whompa booth: standing right on it takes a moment
                        : e.Kind == ExitKind.ZoneLine ? 6 : 8;
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
            float d = Movement.Flat(pos, wp);
            bool last = _pathIndex == _path.Count - 1;
            float arrive = !last || _leg.Exit?.Kind == ExitKind.ZoneLine || IsPad(_leg.Exit) || _frontal ? 0.5f : _leg.Exit == null ? GoalRange : ObjectRange;
            if (d <= arrive)
            {
                _pathIndex++;
                _stuck.Reset();
                if (_pathIndex >= _path.Count) { ArriveWalk(me); return; }
                wp = _path[_pathIndex]; d = Movement.Flat(pos, wp);
            }

            // A PACK ON THE WAY (owner, 2026-09-28: outside he never fights, he only runs): the live hostiles changed and one
            // stands within its aggro range of the next 150 m - plan the leg again, the grid now prices them. Not on a recorded
            // road out (the owner's roads keep their priority), at most MaxDangerReplans a leg, 5 s apart.
            if (_grid is OverlandGrid && !_roadOut && MobDanger.LiveVersion != _dangerVer && Now - _dangerAt > 5 && _dangerReplans < MaxDangerReplans)
            {
                _dangerVer = MobDanger.LiveVersion;
                if (MobDanger.ThreatAhead(_grid.Pf, _path, _pathIndex, pos, 150f, out string who))
                {
                    _dangerAt = Now; _dangerReplans++;
                    _ctx.Log($"OVERLAND: {who} - planning this leg again round them ({_dangerReplans}/{MaxDangerReplans}).");
                    _move.Hold(me, _ctx.Config.SendIntervalMs);
                    BeginLeg(me);
                    return;
                }
            }

            if (_stuck.Tick(d, dt, 0.3f, StuckSeconds))
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
                _move.Hold(me, _ctx.Config.SendIntervalMs);
                BeginLeg(me);
                return;
            }

            if (d < 0.01f) return;
            Vector3 dir = new Vector3(wp.X - pos.X, 0, wp.Z - pos.Z).Normalize();

            // WATER — the captured client's exact contract (20260924-215811 s116, this very shore):
            // NO swim-mode packet ever; Y = THE WATER SURFACE while the bottom is deeper than a wade
            // (it sent 32.09 over our floor of 24.8 — swimming at the surface), Y = THE BOTTOM once
            // it rises inside wading range (30.95, 31.67, 31.90 up the sandbar), plain Update
            // packets throughout at ~5.5 u/s. Our four failures each sent one wrong leg of that
            // triangle plus the mode packet the client never sends.
            float probe = Math.Min(d, 0.5f);
            double plane = _ground?.Ground != null
                ? _ground.Ground.SwimY(pos.X + dir.X * probe, pos.Z + dir.Z * probe, 0.3) : double.NaN;
            _inWater = !double.IsNaN(plane);

            float speed = _inWater ? _ctx.SwimVelocity(me) : _ctx.RunVelocity(me);
            float step = Movement.CappedStep(speed, dt, _ctx.Config.MaxStep, d);
            float nx = pos.X + dir.X * step, nz = pos.Z + dir.Z * step;
            float floorY2 = FloorY(nx, pos.Y, nz);
            // UPHILL THE SERVER IS STRICTER THAN ON THE FLAT (capture 20260925-141138, Wailing Wastes:
            // a 0.1/m rise refused every 1.5 m step — 15 u/s on the wire — while the captured client walks
            // the same slope in 0.1-0.2 m moves every 10-30 ms, i.e. run speed, accepted without one
            // correction). Climb in steps small enough to be under run speed per send interval.
            if (!_inWater && floorY2 > pos.Y)
            {
                step = Math.Min(step, Math.Max(0.3f, speed * _ctx.Config.SendIntervalMs / 1000f * 0.6f));
                nx = pos.X + dir.X * step;
                nz = pos.Z + dir.Z * step;
                floorY2 = FloorY(nx, pos.Y, nz);
            }
            bool floating = _inWater && Now - _wetYAt < 3 && _wetY > floorY2 + 0.4f && _wetY <= plane + 0.3f;
            float nextY = floating ? _wetY                       // the server's own surface Y, while fresh
                : _inWater && floorY2 < (float)plane - _ctx.Config.SwimWadeMeters ? (float)plane   // swim at the surface
                : floorY2;                                      // wade the bottom / walk the shore
            // NEVER BELOW THE TERRAIN (2026-09-25, the Wailing Wastes rubberband, finally understood):
            // FloorNear sticks to the nearest surface — off a wompah that is the PAD's collision floor
            // (23.1), and walking into rising ground while claiming the pad's height puts us INSIDE the
            // hill; the server rejects every step into terrain and pins us at the last valid spot
            // (downhill worked, uphill did not, diagonals were just the routes that crossed the pad).
            // The heightfield is solid ground outdoors: the step we claim can never be under it.
            double terr = _ground?.Ground != null ? _ground.Ground.HeightAt(nx, nz) : double.NaN;
            if (!double.IsNaN(terr) && terr > nextY) nextY = (float)terr;
            if (!_inWater)
            {
                // ...AND STEP UP ONTO WHAT WE WALK INTO (the ICC steps, same day): under a staircase
                // FloorNear's "nearest" floor is the TERRAIN BENEATH THE STAIRS, so the walk claimed the
                // plaza's height while stepping into the rising treads, and the server pinned the bot at
                // the foot of the steps. The surface we stand on at the next position is the HIGHEST floor
                // at most a step above us (a riser or two — anything higher is a wall, anything the terrain
                // clamp already covered is below); big drops keep the old fall behaviour.
                float sf = StepFloor(nx, pos.Y, nz);
                if (!float.IsNaN(sf) && sf >= pos.Y - 4f && sf > nextY) nextY = sf;
            }
            Vector3 next = new Vector3(nx, nextY, nz);
            _ctx.WalkState = $"overland leg {_legNo}/{_legCount} wp {_pathIndex + 1}/{_path.Count} d={d:0}{(_inWater ? (floating ? " float" : nextY == floorY2 ? " wade" : " swim") : "")}";
            _move.Advance(me, next, Movement.SafeLook(dir, me.MovementComponent.Heading), run: true, dt, _ctx.Config.SendIntervalMs);
            _lastPos = next;   // our own step, not a jump
        }

        private void ArriveWalk(LocalPlayer me)
        {
            _move.Hold(me, _ctx.Config.SendIntervalMs);
            if (_roadOut)
            {
                // Out by the road: the same leg again from its far end (the grid sees open ground from here).
                _roadOut = false;
                _ctx.Log("OVERLAND: at the end of the recorded road; planning the leg again from here.");
                BeginLeg(me);
                return;
            }
            if (_leg.Exit == null) { Done(me); return; }
            if (_leg.Exit.Kind == ExitKind.ZoneLine)
            {
                _tries++;
                Enter(Phase.AwaitZone, "over the line, waiting for the zone");
                return;
            }
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
            GameCommands.UseObject(me, id);
            _ctx.Log($"OVERLAND: used {id} at ({e.A.X:0},{e.A.Y:0},{e.A.Z:0}) (try {_tries}).");
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

        // The HIGHEST surface at the next position that is at most a step above y (stairs, kerbs, sills) —
        // the surface we would walk ONTO. NaN when nothing qualifies.
        private float StepFloor(float x, float y, float z)
        {
            float best = float.NaN;
            void Consider(double h)
            {
                if (double.IsNaN(h) || h > y + 0.8f) return;
                if (float.IsNaN(best) || h > best) best = (float)h;
            }
            if (_ground?.Ground != null) Consider(_ground.Ground.HeightAt(x, z));
            if (_ground?.Collision != null) foreach (double h in _ground.Collision.HeightsUnder(x, z)) Consider(h);
            return best;
        }

        // The playfield's floor data and walkable grid, built off the update thread by the ONE shared
        // NavGridCache (Lush Fields' grid took 6.3 s and froze the whole bot while it built; log
        // 2026-09-24 01:36) — shared with the mission hike since both only ever ask for the playfield
        // they stand in, so a zone builds once per visit, not once per consumer.
        private NavGridCache _nav => _ctx.NavGrid;

        private bool EnsureNav()
        {
            int pf = (int)Playfield.ModelId;
            if (!_nav.Request(pf, _pluginDir, _ctx.Log, "OVERLAND")) return false;
            if (_groundPf != pf)
            {
                _groundPf = pf; _yBias = 0f;
                _ground = _nav.Nav; _grid = _nav.Grid;
            }
            return true;
        }
    }
}