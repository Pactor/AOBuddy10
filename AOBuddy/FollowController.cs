using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;

namespace AOBuddy
{
    /// <summary>
    /// FOLLOW — the only system that walks the bot toward the owner. It records the owner's route as a
    /// WAYPOINT QUEUE and walks that queue in order, so it takes valid ground and the SAME route the owner
    /// did — up and down ramps (each waypoint carries his real X/Y/Z), around obstacles, through doors, and
    /// onto a zone line (which zones the bot). It NEVER beelines to the owner's live position: the live
    /// server terrain-validates movement and rejects a straight line up a slope (the "bouncing on the ramp"
    /// bug).
    ///
    /// The queue is the bot's own plan, not a live sighting. Once a waypoint is queued the bot walks it
    /// whether or not the owner is still in view, so ducking behind a hill or around a corner does not stop
    /// the bot — it keeps walking B -> C -> D -> E. Only when the queue runs dry AND the owner is still gone
    /// does it fall back to his last known position, and only after reaching THAT does the zone sweep arm.
    ///
    /// Waypoints are sparse: a point is queued when the owner has moved WaypointSpacing along a straight,
    /// OR turned more than ~WaypointTurnCos, OR changed height by WaypointHeightDelta. Straights cost
    /// almost nothing (so the bot never builds a backlog of old positions to retrace — the old 0.3m
    /// breadcrumb trail was exactly that), while corners, doorways and ramps still get a point each, which
    /// is what keeps the retraced route server-legal on terrain.
    ///
    /// This controller owns its own queue/manual/replay state. Nothing outside it touches _waypoints, so a
    /// change to zone-crossing or path replay here can never reach into combat or travel.
    /// </summary>
    public class FollowController
    {
        private readonly BotContext _ctx;
        private readonly Movement _move;

        // ---- Waypoint tuning ----------------------------------------------------
        // Derived from how the game actually moves, not from user config: the owner runs at 5.5-15.5 u/s,
        // so ~7m is about one second of his travel — dense enough that the route still reads as his route,
        // sparse enough that the bot is never retracing a queue of stale positions.
        private const float WaypointSpacing = 7.0f;       // straight-line spacing, metres
        private const float WaypointTurnCos = 0.94f;      // record on a turn of ~20 deg or more
        // Height granularity is NOT a taste call: a real client crossing a ramped zone line moves in
        // ~0.35m Y steps (captures/newchar_s16.csv: Y 14.01 -> 14.37 -> 14.72 -> 15.135). Anything coarser
        // than that lets the bot draw a straight line across a curve the server terrain-validates, which is
        // the ramp-bounce this whole design exists to avoid. So: sparse on the flat, client-dense on slopes.
        private const float WaypointHeightDelta = 0.4f;   // ...or a height change this big (ramp / stairs / lift)
        private const float WaypointNoiseFloor = 0.35f;   // ignore jitter smaller than this
        private const int MaxWaypoints = 250;             // hard queue cap (~1.7km of route) — bounds memory

        // Catch-up tiers, by how far behind the bot is (live distance to the owner while he is visible,
        // otherwise the remaining queued path length).
        //   0 .. CatchupRun     normal follow  — exact path, small step cap, tight arrive
        //   .. CatchupMax       run            — larger step cap, wider arrive (stop braking into every point)
        //   beyond              maximum        — widest arrive + corner cutting on flat ground
        // These do NOT raise velocity. The live server speed-validates every move against the client's run
        // formula (5.5 + RunSpeed/230); anything above it is rejected and snaps the bot back. Catching up
        // therefore means losing less ground to per-frame clipping and to braking at each point — the gait
        // is run in every tier, because walk gait would put the bot behind instantly and flap the tiers.
        private const float CatchupRun = 10.0f;
        private const float CatchupMax = 25.0f;
        private const float ArriveRun = 1.8f;
        private const float ArriveMax = 3.0f;
        private const float StepCapRun = 2.5f;
        private const float StepCapMax = 4.0f;
        private const float CornerCutFlatY = 1.5f;        // only cut a corner when the points are this level
        private const float LastKnownArrive = 3.0f;       // close enough to the owner's last known spot

        // The owner's route, oldest first. A LinkedList (not a Queue) so we can look one point ahead and
        // discard points the bot has already passed.
        private readonly LinkedList<Vector3> _waypoints = new LinkedList<Vector3>();
        private Vector3? _lastCrumb;      // position the last waypoint was queued at
        private Vector3? _lastLegDir;     // horizontal direction of the leg into that waypoint (turn test)

        // The owner's last few queued waypoints, kept so that when he vanishes at a line we can continue
        // along his REAL recent path (its averaged direction, which follows his curve), instead of a
        // straight guess off one noisy sample.
        private readonly Queue<Vector3> _recentOwner = new Queue<Vector3>();

        // Where he was standing the last time we saw him, and whether we still owe him a walk to it. Armed
        // when he drops out of view; consumed once the queue is empty (queue empty + owner missing -> head
        // for his last known position instead of stopping).
        private Vector3? _lastKnownOwner;
        private bool _lastKnownPending;

        // How far behind the bot is right now (metres). Main feeds the live owner distance while he is
        // visible; while he is not, we fall back to the queued path length.
        private float? _lagToOwner;

        // A one-off manual destination ('come' = to the owner; 'zone'/'forward' = a push in a heading).
        private Vector3? _manualTarget;

        // Saved-path record / replay.
        private bool _recording;
        private readonly List<Vector3> _recordBuf = new List<Vector3>();
        private Vector3? _lastRecord;
        private readonly LinkedList<Vector3> _replay = new LinkedList<Vector3>();
        private Vector3? _lastReplayDir;
        private bool _replayPushed;
        private bool _replayZonePush = true;   // lean one push past the end (for a saved path that ends on a zone line)

        // Zone-crossing marker: true while the final waypoint is a push meant to carry the bot ONTO a
        // zone trigger. Keeps the stuck-skip from throwing that last point away.
        private bool _zoneCrossing;

        // ZONE SWEEP: on losing the owner at a line, sweep back and forth across his vanish spot along
        // his travel axis — exactly what a player does when a zone line won't take on the first step.
        // A walked zone line (server side, from OmniCell's WallCollision + heartbeat) is a wall segment
        // the server tests on a ~0.25s HEARTBEAT: it fires only if your X,Z is within ~2 units of the
        // segment AT A TICK (Y ignored). Gliding through crosses that narrow band between ticks and
        // misses; sweeping slowly back and forth keeps re-entering the band so a tick catches it.
        // Crossing fires a POSITION JUMP -> ClearNav which ends it; so does the owner returning, or a
        // timeout. No per-zone coordinates — works for entering and exiting any zone line.
        private bool _zoneSweep;
        private Vector3 _sweepCenter;  // his vanish spot (the line is near here)
        private Vector3 _sweepAxis;    // his travel direction (crosses the line)
        private bool _sweepForward = true;
        private double _sweepElapsed;
        // Asymmetric sweep: reach FORWARD along the owner's last direction (to round a corner/door and
        // re-acquire him when he simply walked out of sight), but only a little BACK, so each pass still
        // re-crosses the loss point where a real zone line sits.
        private const float SweepForward = 8.0f;    // how far ahead along his heading each forward pass goes
        private const float SweepBack = 3.0f;       // how far back past the loss point (recross the line)
        private const float SweepSpan = SweepForward; // (kept for the start log)
        private const float SweepSpeed = 3.0f;      // slow (and under the server's ~4u/s cap) so he dwells in
                                                     // the ~2-unit band long enough for a heartbeat tick to catch him.
        private const double SweepTimeout = 16.0;   // give up after this long and hold for reacquire

        public bool ZoneSweeping => _zoneSweep;

        // Stuck detection for the current waypoint.
        private double _crumbStuck;
        private float _lastCrumbDist = float.MaxValue;

        // How far we've coasted (dead-reckoned along the owner's last direction) since his last real position
        // update. Bounded so we never coast far if he actually stopped; reset when a fresh waypoint arrives.
        private double _coastAccum;

        public int TrailCount => _waypoints.Count;
        public int ReplayCount => _replay.Count;
        public bool ZoneCrossing => _zoneCrossing;
        public bool Recording => _recording;
        public bool LastKnownPending => _lastKnownPending;

        /// <summary>Remaining queued path length in metres (from the bot's position through every point).</summary>
        public float PathRemaining(Vector3 from)
        {
            float total = 0f;
            Vector3 prev = from;
            foreach (Vector3 p in _waypoints) { total += Vector3.Distance(prev, p); prev = p; }
            return total;
        }

        /// <summary>How far behind the bot is, in metres — 0 when it has nothing to chase.</summary>
        private float Lag(Vector3 pos)
        {
            float pathLag = _waypoints.Count > 0 ? PathRemaining(pos) : 0f;
            // While he is visible, being 8m away in a straight line but 40m of PATH behind (a U-bend, a
            // switchback ramp) still means 40m of running to do — take the worse of the two.
            if (_lagToOwner.HasValue) return Math.Max(_lagToOwner.Value, pathLag);
            if (_waypoints.Count > 0) return pathLag;
            if (_lastKnownPending && _lastKnownOwner.HasValue) return Vector3.Distance(pos, _lastKnownOwner.Value);
            return 0f;
        }

        // Does the bot have somewhere to walk right now? (drives the "Following" vs "Idle" label, and gates
        // the zone sweep in Main — the pending last-known walk counts, so the sweep can't pre-empt it.)
        public bool HasWork => _manualTarget.HasValue || _replay.Count > 0 || _waypoints.Count > 0 || _lastKnownPending;

        // A one-off manual destination is set (a queued cast must wait for it to finish).
        public bool ManualActive => _manualTarget.HasValue;

        // The point the bot is walking toward right now (manual > replay > waypoints > last known), or null
        // if idle. Lets Main notice when the queued path leads AWAY from where the owner now is (he walked back).
        public Vector3? CurrentTarget()
        {
            if (_manualTarget.HasValue) return _manualTarget;
            if (_replay.Count > 0) return _replay.First.Value;
            if (_waypoints.Count > 0) return _waypoints.First.Value;
            if (_lastKnownPending) return _lastKnownOwner;
            return null;
        }

        public FollowController(BotContext ctx, Movement move)
        {
            _ctx = ctx;
            _move = move;
        }

        // ---- Recording the owner's route ----------------------------------------

        // Owner is visible: queue a waypoint when his path has actually changed shape — he has covered
        // WaypointSpacing on a straight, or turned, or changed height. His recorded positions carry the
        // correct terrain height, so retracing them stays server-legal.
        public void Record(PlayerChar owner) => RecordAt(owner.Transform.Position);

        // Queue a waypoint from a given position — used with the OWNER-INTERPOLATED position so the route is
        // smooth between the server's sparse (~1/s) owner keyframes, instead of choppy raw keyframes.
        public void RecordAt(Vector3 op)
        {
            _lastKnownOwner = op;
            _coastAccum = 0;   // a real owner update arrived — stop counting coast distance

            if (!_lastCrumb.HasValue) { Enqueue(op, null); return; }

            Vector3 d = op - _lastCrumb.Value;
            float dist = d.Magnitude;
            if (dist < WaypointNoiseFloor) return;

            Vector3 flat = new Vector3(d.X, 0f, d.Z);
            Vector3? legDir = flat.Magnitude > 0.2f ? (Vector3?)flat.Normalize() : null;

            bool far = dist >= WaypointSpacing;
            bool climbed = Math.Abs(d.Y) >= WaypointHeightDelta;
            bool turned = legDir.HasValue && _lastLegDir.HasValue
                          && Vector3.Dot(legDir.Value, _lastLegDir.Value) < WaypointTurnCos;
            if (!far && !climbed && !turned) return;

            Enqueue(op, legDir);
        }

        private void Enqueue(Vector3 op, Vector3? legDir)
        {
            _waypoints.AddLast(op);
            _lastCrumb = op;
            if (legDir.HasValue) _lastLegDir = legDir;
            // Hard cap. Dropping the head skips ground the bot has not walked, so say so rather than
            // silently teleporting the plan forward.
            while (_waypoints.Count > MaxWaypoints)
            {
                _waypoints.RemoveFirst();
                _ctx.Log($"FOLLOW: waypoint queue hit the {MaxWaypoints} cap — dropped the oldest point (bot is very far behind).");
            }
            _recentOwner.Enqueue(op);
            while (_recentOwner.Count > 6) _recentOwner.Dequeue();
        }

        /// <summary>Main feeds the live owner distance each frame he is visible; null when he is not.</summary>
        public void SetOwnerDistance(float? metres) => _lagToOwner = metres;

        /// <summary>The owner just dropped out of view. Keep the queue — it is the plan — but remember where
        /// he was, so that when the queue runs dry we head there instead of stopping.</summary>
        public void OnOwnerLost(Vector3? lastKnown)
        {
            if (lastKnown.HasValue) _lastKnownOwner = lastKnown;
            _lagToOwner = null;
            _lastKnownPending = _lastKnownOwner.HasValue;
        }

        // Owner reappeared after being out of view. If he's far from the last waypoint it's a real
        // teleport / new area, so the old route is garbage — drop it. Otherwise keep his exact route, but
        // discard any queued points that now lead away from him (he doubled back while we couldn't see him).
        public void OnReacquired(PlayerChar owner)
        {
            CancelZoneSweep();   // he's back in view — stop sweeping the line, follow normally
            _lastKnownPending = false;
            if (!_lastCrumb.HasValue) return;

            Vector3 op = owner.Transform.Position;
            float gap = Vector3.Distance(op, _lastCrumb.Value);
            if (gap > 40f)
            {
                _ctx.Log($"Owner reacquired {gap:0}m from last waypoint — new area, cleared queue({_waypoints.Count}).");
                _waypoints.Clear();
                _lastCrumb = null;
                _lastLegDir = null;
                return;
            }

            // Rebuild: trim the TAIL of the queue back to the queued point nearest him. Anything past that
            // was recorded on a leg he has since walked back along, and walking it would run the bot past him.
            int before = _waypoints.Count;
            float bestD = float.MaxValue;
            LinkedListNode<Vector3> best = null;
            for (LinkedListNode<Vector3> n = _waypoints.First; n != null; n = n.Next)
            {
                float d = Vector3.Distance(n.Value, op);
                if (d <= bestD) { bestD = d; best = n; }
            }
            if (best != null && bestD <= WaypointSpacing * 2f)
            {
                while (_waypoints.Last != best) _waypoints.RemoveLast();
                if (_waypoints.Count != before)
                    _ctx.Log($"FOLLOW: owner back in view — trimmed queue {before} -> {_waypoints.Count} (dropped points past him).");
                _lastCrumb = _waypoints.Last.Value;
            }
        }

        // Recording a named path the owner walks (the 'record' command). Saved paths stay dense: they are
        // replayed blind later, so fidelity matters more than queue length there.
        public void RecordPathPoint(PlayerChar owner)
        {
            Vector3 p = owner.Transform.Position;
            if (!_lastRecord.HasValue || Vector3.Distance(p, _lastRecord.Value) >= _ctx.Config.BreadcrumbSpacing)
            {
                _recordBuf.Add(p);
                _lastRecord = p;
            }
        }

        public void StartRecording() { _recording = true; _recordBuf.Clear(); _lastRecord = null; }
        public void StopRecording() { _recording = false; }
        public int RecordCount => _recordBuf.Count;
        public List<Vector3> RecordBuffer => _recordBuf;

        // ---- Manual / replay control (from chat commands) ------------------------

        public void SetManualTarget(Vector3 t) => _manualTarget = t;
        public void ClearManual() => _manualTarget = null;

        // Begin the zone sweep across the owner's vanish spot, along his real recent travel direction.
        // Returns false if we don't have enough path history to know his direction. Main arms this ONLY
        // when the owner vanished close ahead while following and NOT in combat.
        public bool StartZoneSweep(Vector3 vanishSpot) => StartZoneSweep(vanishSpot, 0f, "owner vanished ahead while following (not combat)");

        /// <summary>
        /// Sweep across a crossing point along the owner's real recent travel direction.
        /// <paramref name="lateralOffset"/> shifts the whole sweep sideways, perpendicular to that
        /// direction: a walked zone line is a wall SEGMENT with ends, so a sweep that keeps missing may
        /// simply be running back and forth past one of them, and stepping sideways for the next attempt
        /// covers that without needing to know where the segment actually is.
        /// Returns false when we have too little of his path to know which way he was going.
        /// </summary>
        public bool StartZoneSweep(Vector3 center, float lateralOffset, string why)
        {
            if (_recentOwner.Count < 2) return false;
            Vector3[] pts = _recentOwner.ToArray();
            Vector3 dir = pts[pts.Length - 1] - pts[0];
            if (dir.Length() < 0.1f) return false;
            dir = dir.Normalize();

            if (Math.Abs(lateralOffset) > 0.01f)
            {
                // Perpendicular in the horizontal plane; the wall check ignores Y entirely.
                Vector3 side = new Vector3(-dir.Z, 0f, dir.X);
                if (side.Magnitude > 0.05f) center = center + side.Normalize() * lateralOffset;
                why += $" — offset {lateralOffset:0.0}m sideways";
            }

            BeginSweep(center, dir, why);
            return true;
        }

        /// <summary>True once a sweep has run its course without crossing, until the next one starts.</summary>
        public bool SweepTimedOut { get; private set; }

        // Manual sweep (the 'zone'/'forward' command): sweep across a line in an explicit direction, so
        // the owner can point the bot at a line and have it work the crossing back and forth.
        public void StartManualSweep(Vector3 fromPos, Vector3 dir)
        {
            if (dir.Length() < 0.1f) return;
            _manualTarget = null;
            BeginSweep(fromPos, dir.Normalize(), "manual 'zone' command");
        }

        private void BeginSweep(Vector3 center, Vector3 dir, string why)
        {
            // A walked zone line is tested on X,Z ONLY (Y is ignored by the wall check). Any Y in the
            // sweep axis is wasted motion that doesn't help cross the X,Z band, so flatten the axis to the
            // horizontal plane and re-normalise — all of SweepSpan then goes into crossing the line. If the
            // owner's recent path was almost purely vertical (no usable X,Z direction), keep the raw dir.
            Vector3 flat = new Vector3(dir.X, 0f, dir.Z);
            _sweepAxis = flat.Magnitude > 0.05f ? flat.Normalize() : dir;

            _sweepCenter = center;
            _sweepForward = true;
            _sweepElapsed = 0;
            _zoneSweep = true;
            SweepTimedOut = false;
            _waypoints.Clear();   // sweep owns movement now
            _lastKnownPending = false;
            _ctx.Log($"ZONE SWEEP: {why} — sweeping back and forth {SweepSpan:0}m each side along dir ({_sweepAxis.X:0.0},{_sweepAxis.Y:0.0},{_sweepAxis.Z:0.0}) until it takes.");
        }

        public void CancelZoneSweep()
        {
            if (_zoneSweep) { _zoneSweep = false; _ctx.Log("ZONE SWEEP: cancelled (owner back / reset)."); }
        }

        // One frame of the sweep: walk slowly toward the current end of the sweep; on reaching it,
        // reverse. Slow so he dwells in the ~2-unit band long enough for a server heartbeat to catch
        // him. A real zone line teleports him mid-sweep (POSITION JUMP -> ClearNav clears _zoneSweep);
        // a timeout gives up and holds for reacquire.
        private void SweepTick(LocalPlayer me, double dt)
        {
            _sweepElapsed += dt;
            if (_sweepElapsed > SweepTimeout)
            {
                _zoneSweep = false;
                SweepTimedOut = true;   // Main decides whether another attempt is worth making
                _move.Stop(me, _ctx.Config.SendIntervalMs);
                _ctx.Log($"ZONE SWEEP: no cross after {SweepTimeout:0}s.");
                return;
            }

            Vector3 pos = me.MovementComponent.Position;
            Vector3 target = _sweepForward ? _sweepCenter + _sweepAxis * SweepForward : _sweepCenter - _sweepAxis * SweepBack;
            float dist = Vector3.Distance(pos, target);

            if (dist <= 1.0f)
            {
                _sweepForward = !_sweepForward;   // reached an end — reverse back across the line
                _ctx.WalkState = $"zone-sweep flip t={_sweepElapsed:0.0}";
                return;
            }

            Vector3 d = (target - pos).Normalize();
            float step = Math.Min((float)(SweepSpeed * dt), dist);
            step = Math.Min(step, _ctx.Config.MaxStep);
            _ctx.WalkState = $"zone-sweep {(_sweepForward ? "fwd" : "back")} d={dist:0.0} t={_sweepElapsed:0.0}";
            _move.Advance(me, pos + d * step, Movement.SafeLook(d, me.MovementComponent.Heading), run: false, dt, _ctx.Config.SendIntervalMs);
        }

        public void LoadReplay(IEnumerable<Vector3> pts) => LoadReplay(pts, true);

        // zonePush=false for a nav catch-up (walk exactly the recorded points, no +ZoneChase lunge at the
        // end — that would overshoot the owner, maybe into a wall). true for a saved path meant to zone.
        public void LoadReplay(IEnumerable<Vector3> pts, bool zonePush)
        {
            _manualTarget = null;
            _waypoints.Clear(); _lastCrumb = null; _lastLegDir = null;   // an explicit replay overrides the live queue
            _replay.Clear(); _replayPushed = false; _lastReplayDir = null; _replayZonePush = zonePush;
            foreach (Vector3 p in pts) _replay.AddLast(p);
        }

        public void StopReplay() { _replay.Clear(); _replayPushed = false; _lastReplayDir = null; }

        // ---- Walking (per frame, small steps, real run, never teleport) ----------

        // The bot runs at ITS OWN run speed, using the game client's EXACT velocity formula:
        //     velocity (u/s) = 5.5 + RunSpeed / 230        (RunSpeed = Exploring->Run Speed skill, Stat 156)
        // Base 5.5 u/s at 0 skill; +1 u/s per 230 skill; the client's own hard cap is 15.5 u/s (RunSpeed
        // 2300). This is exactly what the real client moves at, so the server accepts it — no guessed
        // coefficient, and no catch-up tier may exceed it. Fall back to config only if the stat can't be read.
        private const float RunSpeedBaseVelocity = 5.5f;
        private const float RunSpeedDivisor = 230f;
        private const float MaxGroundSpeed = 15.5f;   // client cap (reached at RunSpeed 2300)
        private int _lastRunSpeed = -1;
        public int LastRunSpeed => _lastRunSpeed;

        private float MoveSpeed(LocalPlayer me)
        {
            if (me.TryGetStat(Stat.RunSpeed, out int rs) && rs >= 0)
            {
                _lastRunSpeed = rs;
                return Math.Min(MaxGroundSpeed, RunSpeedBaseVelocity + rs / RunSpeedDivisor);
            }
            return _ctx.Config.FollowSpeed;
        }

        // A short coast target along the owner's most-recent HORIZONTAL direction, to keep moving during a
        // gap in his position updates. Returns false (so we idle instead) when: he wasn't really moving, we've
        // already coasted the cap (he may have stopped), or his recent path isn't flat/level with us (a ramp —
        // coasting a guessed height there is exactly the old rubber-band, so we defer to the recorded route).
        private bool TryCoastTarget(LocalPlayer me, out Vector3 target)
        {
            target = default(Vector3);
            if (_recentOwner.Count < 2 || _coastAccum >= _ctx.Config.FollowCoastMeters) return false;
            Vector3[] pts = _recentOwner.ToArray();
            Vector3 dir = pts[pts.Length - 1] - pts[0];
            Vector3 flat = new Vector3(dir.X, 0f, dir.Z);
            if (flat.Magnitude < 0.3f) return false;                                   // he wasn't moving
            Vector3 pos = me.MovementComponent.Position;
            if (Math.Abs(pts[pts.Length - 1].Y - pos.Y) > _ctx.Config.FollowFlatThreshold) return false;   // ramp — don't guess height
            target = pos + flat.Normalize() * 3f;                                       // a few metres ahead along his path
            return true;
        }

        // Discard leading waypoints the bot has effectively already reached, so the queue always points
        // forward instead of dragging the bot back through positions it has passed.
        //   - always: the head is inside `arrive` and the next point is no farther off (we are past it)
        //   - max tier only: cut a corner — drop the head when the next point is already closer AND both
        //     are level with us. Gated on flat ground because cutting across a ramp or a doorway is
        //     exactly the straight line the server rejects.
        private void TrimPassed(Vector3 pos, float arrive, bool cornerCut)
        {
            while (_waypoints.Count >= 2)
            {
                Vector3 head = _waypoints.First.Value;
                Vector3 next = _waypoints.First.Next.Value;
                float dHead = Vector3.Distance(pos, head);
                float dNext = Vector3.Distance(pos, next);

                bool passed = dHead <= arrive && dNext <= dHead + arrive;
                bool cut = cornerCut && dNext < dHead
                           && Math.Abs(head.Y - pos.Y) <= CornerCutFlatY
                           && Math.Abs(next.Y - pos.Y) <= CornerCutFlatY;
                if (!passed && !cut) break;

                _waypoints.RemoveFirst();
                _crumbStuck = 0; _lastCrumbDist = float.MaxValue;
            }
        }

        public void WalkTick(LocalPlayer me, double dt)
        {
            if (_zoneSweep) { SweepTick(me, dt); return; }

            if (_waypoints.Count == 0) _zoneCrossing = false;

            Vector3 pos = me.MovementComponent.Position;

            // Catch-up tier from how far behind we are. A tier only widens the arrive radius and the
            // per-frame step cap (and, at the top tier, allows flat corner cutting) — never the velocity,
            // which the server validates against the run-speed formula.
            float lag = Lag(pos);
            int tier = lag >= CatchupMax ? 2 : lag >= CatchupRun ? 1 : 0;
            float tierArrive = tier == 2 ? ArriveMax : tier == 1 ? ArriveRun : _ctx.Config.CrumbArrive;
            float tierStepCap = tier == 2 ? StepCapMax : tier == 1 ? StepCapRun : _ctx.Config.MaxStep;

            if (_waypoints.Count > 0 && !_manualTarget.HasValue && _replay.Count == 0)
                TrimPassed(pos, tierArrive, cornerCut: tier == 2 && !_zoneCrossing);

            Vector3 target;
            float arrive;
            bool coasting = false;
            bool lastKnownRun = false;
            LinkedList<Vector3> crumbs = null;   // the queue to advance when a point is reached

            if (_manualTarget.HasValue)
            {
                target = _manualTarget.Value; arrive = 1.5f;
            }
            else if (_replay.Count > 0)
            {
                crumbs = _replay; target = _replay.First.Value; arrive = tierArrive;
            }
            else if (_ctx.Config.Follow && _waypoints.Count > 0)
            {
                // Walk his queued route — oldest first, removed as we arrive. We walk the queue whether or
                // not he's currently in view, so a disappearance (a hill, a corner, a door) doesn't stop us,
                // and a route that ends on a zone line walks us onto the trigger and zones us.
                crumbs = _waypoints; target = _waypoints.First.Value; arrive = tierArrive;
            }
            else if (_ctx.Config.Follow && _lastKnownPending && _lastKnownOwner.HasValue)
            {
                // Queue is empty and he is still missing: head for where we last saw him. Only after
                // reaching THAT does Main get to arm the zone sweep (HasWork covers this state).
                target = _lastKnownOwner.Value; arrive = LastKnownArrive; lastKnownRun = true;
            }
            else if (_ctx.Config.FollowLiveLead && TryCoastTarget(me, out Vector3 coastTgt))
            {
                // Queue used up but the owner was just moving and his next position update is delayed (his
                // position reaches us in bursts). Rather than idle and lurch, COAST a little along his last
                // direction so we're already moving when the update lands. Flat + bounded only (see
                // TryCoastTarget) so it can never coast a wrong height up a ramp.
                target = coastTgt; arrive = tierArrive; coasting = true;
            }
            else
            {
                _ctx.WalkState = $"idle (follow={_ctx.Config.Follow} wp={_waypoints.Count})";
                _move.Stop(me, _ctx.Config.SendIntervalMs);
                return;
            }

            float dist = Vector3.Distance(pos, target);

            if (dist <= arrive)
            {
                if (crumbs != null)
                {
                    crumbs.RemoveFirst(); _crumbStuck = 0; _lastCrumbDist = float.MaxValue;
                    // End of a saved path: it ends at the doorway (we lost sight of the owner the
                    // instant he zoned), so lean one push forward onto the trigger. A real zone line
                    // crosses en route (ClearNav fires on the position jump); otherwise we just stop.
                    if (crumbs == _replay && crumbs.Count == 0 && !_replayPushed && _replayZonePush && _lastReplayDir.HasValue)
                    {
                        _replay.AddLast(pos + _lastReplayDir.Value * _ctx.Config.ZoneChaseMeters);
                        _replayPushed = true;
                    }
                    return;
                }
                if (lastKnownRun)
                {
                    _lastKnownPending = false;
                    _ctx.Log("FOLLOW: reached the owner's last known position and he is still out of sight — handing off.");
                }
                else _manualTarget = null;
                _move.Stop(me, _ctx.Config.SendIntervalMs);
                return;
            }

            // Skip a point we can't make progress toward (blocked by a wall) — but never skip the
            // final push onto a zone-line trigger; keep leaning into it.
            if (crumbs != null && !(crumbs == _waypoints && _zoneCrossing))
            {
                if (dist < _lastCrumbDist - 0.03f) _crumbStuck = 0; else _crumbStuck += dt;
                _lastCrumbDist = dist;
                if (_crumbStuck > _ctx.Config.StuckSeconds)
                {
                    _ctx.Log($"STUCK-SKIP {(crumbs == _replay ? "replay" : "waypoint")} at dist={dist:0.0} (blocked {_ctx.Config.StuckSeconds:0.0}s), skipping. remaining={crumbs.Count - 1}");
                    crumbs.RemoveFirst(); _crumbStuck = 0; _lastCrumbDist = float.MaxValue; return;
                }
            }
            else if (lastKnownRun)
            {
                // The last-known walk is a straight line into ground we never recorded. Give it the same
                // stuck budget (x3, it can be a long leg), then stop owing it so the sweep / nav fallback
                // can take over instead of grinding into a wall forever.
                if (dist < _lastCrumbDist - 0.03f) _crumbStuck = 0; else _crumbStuck += dt;
                _lastCrumbDist = dist;
                if (_crumbStuck > _ctx.Config.StuckSeconds * 3)
                {
                    _ctx.Log($"FOLLOW: blocked {_ctx.Config.StuckSeconds * 3:0.0}s heading to the owner's last known position — giving up on it.");
                    _lastKnownPending = false; _crumbStuck = 0; _lastCrumbDist = float.MaxValue; return;
                }
            }

            Vector3 dir = (target - pos).Normalize();
            if (crumbs == _replay) _lastReplayDir = dir;
            if (coasting) _coastAccum += Math.Min(dist - arrive, Math.Min((float)(MoveSpeed(me) * dt), tierStepCap));
            Quaternion heading = Movement.SafeLook(dir, me.MovementComponent.Heading);
            float speed = MoveSpeed(me);   // capped at the char's run speed or the server rejects the move
            float step = Math.Min(dist - arrive, (float)(speed * dt));
            step = Math.Min(step, tierStepCap);   // hard cap so a lag spike can never warp him
            string what = coasting ? "coast"
                        : crumbs == _replay ? "replay"
                        : crumbs == _waypoints ? "follow"
                        : lastKnownRun ? "lastknown" : "manual";
            _ctx.WalkState = $"{what} t{tier} tgt=({target.X:0},{target.Y:0},{target.Z:0}) d={dist:0.0} lag={lag:0.0} wp={_waypoints.Count} step={step:0.00} coast={_coastAccum:0.0}";

            // Dead zone: he's a hair past 'arrive' so the step rounds to ~0. Treat it as reached
            // (advance the queue / clear the manual target) and stop, so he doesn't freeze 1m short.
            if (step <= 0.05f)
            {
                if (crumbs != null) { crumbs.RemoveFirst(); _crumbStuck = 0; _lastCrumbDist = float.MaxValue; }
                else if (lastKnownRun) _lastKnownPending = false;
                else _manualTarget = null;
                if (_waypoints.Count == 0) _zoneCrossing = false;
                _move.Stop(me, _ctx.Config.SendIntervalMs);
                return;
            }

            _move.Advance(me, pos + dir * step, heading, run: true, dt, _ctx.Config.SendIntervalMs);
        }

        // Full reset on a detected zone/teleport — drop old-playfield coordinates.
        public void Reset()
        {
            _replay.Clear(); _waypoints.Clear(); _recentOwner.Clear();
            _zoneCrossing = false; _zoneSweep = false; _replayPushed = false; _lastReplayDir = null;
            _manualTarget = null; _lastCrumb = null; _lastLegDir = null; _lastRecord = null;
            _lastKnownOwner = null; _lastKnownPending = false; _lagToOwner = null;
            _crumbStuck = 0; _lastCrumbDist = float.MaxValue;
        }

        // 'stop'/'idle' clears active movement but keeps recording state.
        public void ClearMovement()
        {
            _manualTarget = null; _waypoints.Clear(); _replay.Clear();
            _lastKnownPending = false;
            _replayPushed = false; _lastReplayDir = null;
        }
    }
}
