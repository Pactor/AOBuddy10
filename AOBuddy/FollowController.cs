using System;
using System.Collections.Generic;
using AOSharp.Clientless;
using AOSharp.Common.GameData;

namespace AOBuddy
{
    /// <summary>
    /// FOLLOW — the only system that walks the bot toward the owner. It is a DIRECT SEEK, in the same
    /// three beats a player uses:
    ///
    ///     1. TURN TO OWNER  — rotate in place (real TurnLeft/TurnRight packets) until we face him,
    ///                         instead of sliding sideways while pointing the wrong way.
    ///     2. MOVE TO OWNER  — run straight at his CURRENT position (interpolated, fed every frame by
    ///                         Main's PredictOwnerPos) until we're inside FollowDistance.
    ///     3. TURN HIS WAY   — once parked, adopt the OWNER'S OWN heading, so the instant he runs off
    ///                         we're already pointing where he's going and start moving immediately
    ///                         instead of burning the first metres turning.
    ///
    /// The old breadcrumb trail is GONE. Replaying a queue of stale crumbs was the source of the choppy
    /// stop-start gait (arrive at crumb -> dequeue -> re-aim -> step) and of the "owner left view -> trail
    /// empties -> bot stops" freeze. Instead the target is always a live point, movement is continuous,
    /// and losing sight of the owner does NOT stop the bot: it keeps running to his last known spot and
    /// then leans one push further along his last direction, which is also what carries it onto a zone
    /// line. Only after that does it hand over (HasWork goes false) to Main's zone-sweep / NAV fallback.
    ///
    /// Hysteresis (FollowDistance to stop, +FollowResumeSlack to start again) keeps it from stuttering
    /// between "close enough" and "catch up" while the owner jogs at the edge of the leash.
    ///
    /// Still owned here and unchanged: the zone sweep, saved-path recording, and the REPLAY walker that
    /// NAV hands recorded routes to (the wall-safe fallback on ground we've already walked — that's what
    /// covers ramps, where a straight line at the owner can be rejected by the server's terrain check).
    /// </summary>
    public class FollowController
    {
        private readonly BotContext _ctx;
        private readonly Movement _move;

        // The owner's position as of this frame (interpolated — fed by Main every frame he's visible),
        // and a short history of it, kept for his travel DIRECTION (zone sweep + the lost-sight push).
        private Vector3? _ownerPos;
        private readonly Queue<Vector3> _recentOwner = new Queue<Vector3>();
        private Vector3? _lastSample;

        // Is he actually going somewhere? A sample moved => he's moving, and stays "moving" for a short
        // grace (his position reaches us in bursts, so a gap between samples is not a stop).
        private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
        private double _ownerMovingUntil;
        private bool OwnerMoving => _clock.Elapsed.TotalSeconds < _ownerMovingUntil;
        private const double OwnerMovingGrace = 0.6;
        private double _lastSampleTime;
        private double _ownerSpeed;      // u/s, smoothed — measured off his own position samples
        private double _outrunAccum;     // throttle for the "he's faster than me" log

        // Lost sight of him: the spot to keep running to, then one push past it along his last heading.
        private Vector3? _lostTarget;
        private bool _lostPushed;

        // Catch-up hysteresis: true while we're closing the gap, false while parked at his side.
        private bool _closing;
        private bool _aligning;   // parked and mid-turn onto the owner's facing

        // MIRROR LOCK (stack mode): we're on his spot with his facing, so Main re-sends his own movement
        // packets as ours (Movement.Mirror) and this controller sends nothing. Re-earned every WalkTick, so
        // any frame that doesn't reach StackTick (owner lost, cast, rest, travel, replay...) drops it.
        private bool _mirrorLock;
        public bool MirrorLocked => _mirrorLock;
        public void BreakMirror() => _mirrorLock = false;

        // A one-off manual destination ('come' = to the owner; 'zone'/'forward' = a push in a heading).
        private Vector3? _manualTarget;

        // Saved-path record / replay.
        private bool _recording;
        private readonly List<Vector3> _recordBuf = new List<Vector3>();
        private Vector3? _lastRecord;
        private readonly Queue<Vector3> _replay = new Queue<Vector3>();
        private Vector3? _lastReplayDir;
        private bool _replayPushed;
        private bool _replayZonePush = true;   // lean one push past the end (for a saved path that ends on a zone line)

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

        // Stuck detection for the current replay point.
        private double _crumbStuck;
        private float _lastCrumbDist = float.MaxValue;

        // Diagnostics: how many owner samples we're holding (the heartbeat's old "trail=" figure).
        public int TrailCount => _recentOwner.Count;
        public int ReplayCount => _replay.Count;
        public bool ZoneCrossing => _lostPushed;
        public bool Recording => _recording;

        // Does the bot have somewhere to walk right now? (drives the "Following" vs "Idle" label, and
        // gates Main's zone-sweep — which must only arm once the lost-sight chase is spent.)
        public bool HasWork => _manualTarget.HasValue || _replay.Count > 0 || _lostTarget.HasValue;

        // A one-off manual destination is set (a queued cast must wait for it to finish).
        public bool ManualActive => _manualTarget.HasValue;

        // The point the bot is walking toward right now (manual > replay > lost-chase), or null if it's
        // simply following the live owner. Lets Main notice a queued path that leads AWAY from him.
        public Vector3? CurrentTarget()
        {
            if (_manualTarget.HasValue) return _manualTarget;
            if (_replay.Count > 0) return _replay.Peek();
            if (_lostTarget.HasValue) return _lostTarget;
            return null;
        }

        public FollowController(BotContext ctx, Movement move)
        {
            _ctx = ctx;
            _move = move;
        }

        // ---- Tracking the owner --------------------------------------------------

        public void Record(PlayerChar owner) => RecordAt(owner.Transform.Position);

        // Called every frame the owner is visible, with his INTERPOLATED position (Main's PredictOwnerPos),
        // so the follow target moves smoothly between the server's sparse (~1/s) owner keyframes. Every
        // call refreshes the live target; the history behind it is thinned to BreadcrumbSpacing, because
        // it only exists to derive his travel direction.
        public void RecordAt(Vector3 op)
        {
            _ownerPos = op;
            _lostTarget = null; _lostPushed = false;   // he's visible — nothing to chase blind

            if (!_lastSample.HasValue || Vector3.Distance(op, _lastSample.Value) >= _ctx.Config.BreadcrumbSpacing)
            {
                double now = _clock.Elapsed.TotalSeconds;
                if (_lastSample.HasValue)
                {
                    _ownerMovingUntil = now + OwnerMovingGrace;
                    double gap = now - _lastSampleTime;
                    if (gap > 0.02)
                    {
                        double v = Vector3.Distance(op, _lastSample.Value) / gap;
                        if (v < 40) _ownerSpeed = _ownerSpeed <= 0 ? v : _ownerSpeed * 0.7 + v * 0.3;   // ignore teleports
                    }
                }
                _lastSampleTime = now;
                _lastSample = op;
                _recentOwner.Enqueue(op);
                while (_recentOwner.Count > 8) _recentOwner.Dequeue();
            }
        }

        // Owner reappeared after being out of view. Drop the blind chase and, on a far reacquire (he
        // teleported / we zoned in behind him), drop the stale direction history too.
        public void OnReacquired(PlayerChar owner)
        {
            CancelZoneSweep();   // he's back in view — stop sweeping the line, follow normally
            _lostTarget = null; _lostPushed = false;
            if (!_lastSample.HasValue) return;
            float gap = Vector3.Distance(owner.Transform.Position, _lastSample.Value);
            if (gap > 40f)
            {
                _ctx.Log($"Owner reacquired {gap:0}m from last seen spot — new area, cleared owner history.");
                _recentOwner.Clear();
                _lastSample = null;
            }
        }

        // Recording a named path the owner walks (the 'record' command).
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

        // His most recent travel direction, flattened (null if he wasn't really moving).
        private Vector3? OwnerDir()
        {
            if (_recentOwner.Count < 2) return null;
            Vector3[] pts = _recentOwner.ToArray();
            Vector3 d = pts[pts.Length - 1] - pts[0];
            Vector3 flat = new Vector3(d.X, 0f, d.Z);
            return flat.Magnitude < 0.3f ? (Vector3?)null : flat.Normalize();
        }

        // Begin the zone sweep across the owner's vanish spot, along his real recent travel direction.
        // Returns false if we don't have enough path history to know his direction.
        public bool StartZoneSweep(Vector3 vanishSpot)
        {
            Vector3? dir = OwnerDir();
            if (!dir.HasValue) return false;
            BeginSweep(vanishSpot, dir.Value, "owner vanished ahead while following (not combat) — sweeping his crossing spot");
            return true;
        }

        // Manual sweep (the 'zone'/'forward' command): sweep across a line in an explicit direction, so
        // the owner can point the bot at a line and have it work the crossing back and forth.
        public void StartManualSweep(Vector3 fromPos, Vector3 dir)
        {
            if (dir.Magnitude < 0.1f) return;
            _manualTarget = null;
            BeginSweep(fromPos, dir.Normalize(), "manual 'zone' command");
        }

        private void BeginSweep(Vector3 center, Vector3 dir, string why)
        {
            // A walked zone line is tested on X,Z ONLY (Y is ignored by the wall check). Any Y in the
            // sweep axis is wasted motion that doesn't help cross the X,Z band, so flatten the axis to the
            // horizontal plane and re-normalise. If his recent path was almost purely vertical, keep raw.
            Vector3 flat = new Vector3(dir.X, 0f, dir.Z);
            _sweepAxis = flat.Magnitude > 0.05f ? flat.Normalize() : dir;

            _sweepCenter = center;
            _sweepForward = true;
            _sweepElapsed = 0;
            _zoneSweep = true;
            _lostTarget = null; _lostPushed = false;   // sweep owns movement now
            _ctx.Log($"ZONE SWEEP: {why} — sweeping back and forth {SweepSpan:0}m each side along dir ({_sweepAxis.X:0.0},{_sweepAxis.Y:0.0},{_sweepAxis.Z:0.0}) until it takes.");
        }

        public void CancelZoneSweep()
        {
            if (_zoneSweep) { _zoneSweep = false; _ctx.Log("ZONE SWEEP: cancelled (owner back / reset)."); }
        }

        // One frame of the sweep: walk slowly toward the current end of the sweep; on reaching it,
        // reverse. A real zone line teleports him mid-sweep (POSITION JUMP -> ClearNav clears _zoneSweep);
        // a timeout gives up and holds for reacquire.
        private void SweepTick(LocalPlayer me, double dt)
        {
            _sweepElapsed += dt;
            if (_sweepElapsed > SweepTimeout)
            {
                _zoneSweep = false;
                _move.Stop(me, _ctx.Config.SendIntervalMs);
                _ctx.Log($"ZONE SWEEP: no cross after {SweepTimeout:0}s — holding for reacquire.");
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
            _lostTarget = null; _lostPushed = false;   // an explicit route supersedes the blind chase
            _replay.Clear(); _replayPushed = false; _lastReplayDir = null; _replayZonePush = zonePush;
            foreach (Vector3 p in pts) _replay.Enqueue(p);
        }

        public void StopReplay() { _replay.Clear(); _replayPushed = false; _lastReplayDir = null; }

        // ---- Walking (per frame, small steps, real run, never teleport) ----------

        // The bot runs at ITS OWN run speed, using the game client's EXACT velocity formula:
        //     velocity (u/s) = 5.5 + RunSpeed / 230        (RunSpeed = Exploring->Run Speed skill, Stat 156)
        // Base 5.5 u/s at 0 skill; +1 u/s per 230 skill; the client's own hard cap is 15.5 u/s (RunSpeed
        // 2300). This is exactly what the real client moves at, so the server accepts it — no guessed
        // coefficient. Fall back to config only if the stat can't be read.
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

        public void WalkTick(LocalPlayer me, double dt) => WalkTick(me, null, dt);

        /// <summary>
        /// One frame of movement. Priority: zone sweep > manual target > recorded replay (NAV / saved
        /// path) > live owner follow > blind chase after losing him. Called by Main's movement arbiter
        /// only when no higher-priority mover (travel) is active and the bot isn't casting/resting.
        /// </summary>
        public void WalkTick(LocalPlayer me, PlayerChar owner, double dt)
        {
            bool wasLocked = _mirrorLock;
            _mirrorLock = false;
            if (wasLocked && owner != null && !_zoneSweep && !_manualTarget.HasValue && _replay.Count == 0 && _ctx.Config.Follow
                && _ctx.Config.FollowDistance <= 0.05f && MirrorStillOn(me, owner))
            {
                _mirrorLock = true;
                return;
            }

            if (_zoneSweep) { SweepTick(me, dt); return; }

            if (_manualTarget.HasValue) { ManualTick(me, dt); return; }
            if (_replay.Count > 0) { ReplayTick(me, dt); return; }

            if (!_ctx.Config.Follow)
            {
                _ctx.WalkState = "idle (follow=off)";
                Hold(me);
                return;
            }

            if (owner != null) { FollowOwnerTick(me, owner, dt); return; }

            // He's out of view. ARM the blind chase once, from the last spot we saw him at — the bot must
            // NOT stop the moment he leaves view (that was the old freeze); it runs to where he was, then
            // leans one push further along his heading, which also carries it onto a zone line.
            if (!_lostTarget.HasValue && !_lostPushed && _ownerPos.HasValue)
            {
                _lostTarget = _ownerPos;
                _ctx.Log($"FOLLOW: lost sight — running to his last spot ({_ownerPos.Value.X:0},{_ownerPos.Value.Y:0},{_ownerPos.Value.Z:0}), then a {_ctx.Config.FollowLostPushMeters:0}m push along his heading.");
            }

            if (_lostTarget.HasValue) { LostTick(me, dt); return; }

            _ctx.WalkState = "idle (owner not visible, chase spent)";
            Hold(me);
        }

        // ---- The three beats: turn to him, move to him, then take his heading ----

        private void FollowOwnerTick(LocalPlayer me, PlayerChar owner, double dt)
        {
            Vector3 pos = me.MovementComponent.Position;
            Vector3 tgt = _ownerPos ?? owner.Transform.Position;   // interpolated when Main is feeding it
            float dist = Vector3.Distance(pos, tgt);

            if (_ctx.Config.FollowDistance <= 0.05f) { StackTick(me, owner, pos, tgt, dist, dt); return; }

            // Hysteresis, but ONLY against a STANDING owner. Parked next to a man who is standing still,
            // we don't want to twitch after every centimetre he shuffles — hence the slack. But while he
            // is actually RUNNING, waiting for him to open up the slack before setting off is exactly the
            // stutter (and the reason he pulls away): the moment he's outside FollowDistance, go.
            float resumeAt = _ctx.Config.FollowDistance + (OwnerMoving ? 0f : _ctx.Config.FollowResumeSlack);
            if (_closing) { if (dist <= _ctx.Config.FollowDistance) _closing = false; }
            else if (dist >= resumeAt) _closing = true;

            if (!_closing)
            {
                // BEAT 3 — parked at his side. Stand still, and while he's STANDING too, swing round to
                // HIS facing so that when he runs we're already pointing the right way. We do NOT turn
                // while he's moving: the turn would be obsolete the instant we set off, and burning
                // frames on it is what left the bot pirouetting while he walked away.
                Hold(me);
                if (OwnerMoving)
                {
                    _aligning = false;
                    _ctx.WalkState = $"parked (he's moving) d={dist:0.0}";
                    return;
                }
                // Deadzone + latch: only START aligning once his facing is meaningfully off ours, then
                // finish the turn. Otherwise his idle heading jitter would spam turn packets forever.
                float faceOff = Movement.HeadingOffsetDeg(me.MovementComponent.Heading, owner.Transform.Heading);
                if (!_aligning && faceOff > _ctx.Config.FollowFaceDeadzoneDeg) _aligning = true;
                if (_aligning)
                    _aligning = _move.Face(me, owner.Transform.Heading, _ctx.Config.FollowTurnDegPerSec, dt, _ctx.Config.SendIntervalMs);
                _ctx.WalkState = $"{(_aligning ? "align-owner-facing" : "parked")} d={dist:0.0} off={faceOff:0}";
                return;
            }
            _aligning = false;

            Step(me, tgt, dist, dt, "follow");
        }

        /// <summary>
        /// STACK / MIRROR follow (FollowDistance = 0): stand on the owner's exact spot and from then on
        /// simply BE where he is — each frame the bot's pose is set to his position and his heading, so
        /// its outgoing movement packets are a copy of his. No target-chasing, no arrive-radius, no
        /// turning: while it can keep up, the gap is exactly zero and there is nothing left to be choppy.
        /// (Player characters don't collide in AO, so sharing his coordinates is legal, and standing on
        /// the zone line he stands on means his crossing is our crossing.)
        ///
        /// THE ONE HARD LIMIT: mirroring cannot beat physics. Our step per frame is capped at OUR run
        /// speed (5.5 + RunSpeed/230) because the server rejects a faster move and snaps us back — see
        /// Config.FollowSpeed. So while the owner is genuinely faster than us we fall behind and this
        /// degrades to a normal top-speed chase, re-locking onto him the moment he's within one step
        /// again (he stops, turns a corner, fights). The log tells you which case you're in.
        /// </summary>
        private void StackTick(LocalPlayer me, PlayerChar owner, Vector3 pos, Vector3 tgt, float dist, double dt)
        {
            float maxStep = Math.Min((float)(MoveSpeed(me) * dt), _ctx.Config.MaxStep);

            if (dist > maxStep)
            {
                // Can't reach his spot this frame — run flat out at it and report the speed deficit, which
                // is the ONLY reason a stack-follow ever lags. Throttled so it can't spam the log.
                WarnIfOutrun(me, dt);
                if (dist > _ctx.Config.FollowStackSlideMeters)
                {
                    Step(me, tgt, dist, dt, "stack-chase");
                    return;
                }
                // Close: slide onto his spot while keeping HIS facing. If we ended up a metre past him when he
                // stopped, we back onto his spot still pointing his way — no about-face to walk back, and no
                // second about-face when he sets off again.
                Vector3 d = (tgt - pos).Normalize();
                _ctx.WalkState = $"stack-slide d={dist:0.00} step={maxStep:0.00}";
                _move.Advance(me, pos + d * maxStep, owner.Transform.Heading, run: true, dt, _ctx.Config.SendIntervalMs);
                return;
            }

            // On him. Copy his pose outright: his position, his facing.
            if (!OwnerMoving && dist < 0.15f)
            {
                // He's standing and we're on his spot — stop the movement stream entirely (no packets to
                // send while nothing changes) and just keep our facing matched to his.
                Hold(me);
                _move.Face(me, owner.Transform.Heading, _ctx.Config.FollowTurnDegPerSec, dt, _ctx.Config.SendIntervalMs);
                // On his spot, facing his way, both standing: from here on just replay his packets.
                if (_ctx.Config.FollowMirror && Movement.HeadingOffsetDeg(me.MovementComponent.Heading, owner.Transform.Heading) < 2f)
                {
                    _mirrorLock = true;
                    _ctx.Log($"FOLLOW: stacked on owner — mirroring his movement packets.");
                }
                _ctx.WalkState = $"stacked (he's still) d={dist:0.00}{(_mirrorLock ? " -> mirror" : "")}";
                return;
            }

            // Caught him on the move: take his predicted spot and facing, then lock — his next packet puts
            // us exactly on his reported position and from then on we move as he does.
            _ctx.WalkState = $"stack-join d={dist:0.00}";
            _move.Advance(me, tgt, owner.Transform.Heading, run: true, dt, _ctx.Config.SendIntervalMs);
            if (_ctx.Config.FollowMirror && OwnerMoving && !_mirrorLock)
            {
                _mirrorLock = true;
                _ctx.Log($"FOLLOW: joined owner on the move — mirroring his movement packets.");
            }
        }

        // Is the mirror still holding? We sit either on his last REPORTED spot (after a replayed packet) or on
        // his PREDICTED one (locked mid-run, before his next packet), so accept whichever is nearer. Past
        // FollowMirrorBreakMeters the server has moved us (correction / rejected step) or he did something we
        // couldn't copy — fall back to the normal stack catch-up, which re-locks once we're on him again.
        private bool MirrorStillOn(LocalPlayer me, PlayerChar owner)
        {
            if (!_ctx.Config.FollowMirror) return false;
            Vector3 pos = me.MovementComponent.Position;
            float d = Vector3.Distance(pos, owner.Transform.Position);
            if (_ownerPos.HasValue) d = Math.Min(d, Vector3.Distance(pos, _ownerPos.Value));
            if (d <= _ctx.Config.FollowMirrorBreakMeters)
            {
                _ctx.WalkState = $"mirror d={d:0.00}";
                return true;
            }
            _ctx.Log($"FOLLOW: mirror broke — {d:0.0}m off him (server moved us / couldn't copy a move). Re-stacking.");
            return false;
        }

        // Log (at most every 5s) when the owner is simply faster than this character — no follow algorithm
        // can fix that, only run-speed buffs or a lower-speed owner can.
        private void WarnIfOutrun(LocalPlayer me, double dt)
        {
            _outrunAccum += dt;
            if (_outrunAccum < 5.0 || !OwnerMoving || _ownerSpeed <= 0.1) return;
            _outrunAccum = 0;
            float mine = MoveSpeed(me);
            if (_ownerSpeed > mine + 0.5)
                _ctx.Log($"FOLLOW: he's faster than me — his {_ownerSpeed:0.0} u/s vs my {mine:0.0} u/s (RunSpeed {_lastRunSpeed}). Can't close while he runs; I re-stack when he slows.");
        }

        // Blind chase after he leaves view: same seek, toward his last spot, then one push past it.
        private void LostTick(LocalPlayer me, double dt)
        {
            Vector3 pos = me.MovementComponent.Position;
            Vector3 tgt = _lostTarget.Value;
            float dist = Vector3.Distance(pos, tgt);

            if (dist <= _ctx.Config.CrumbArrive)
            {
                Vector3? dir = OwnerDir();
                if (!_lostPushed && dir.HasValue)
                {
                    _lostTarget = pos + dir.Value * _ctx.Config.FollowLostPushMeters;
                    _lostPushed = true;
                    _ctx.Log($"FOLLOW: reached his last spot — pushing {_ctx.Config.FollowLostPushMeters:0}m along his heading (rounds a corner / leans onto a zone line).");
                    return;
                }
                _lostTarget = null;   // chase spent — Main's zone-sweep / NAV fallback takes it from here
                Hold(me);
                return;
            }

            Step(me, tgt, dist, dt, _lostPushed ? "lost-push" : "lost-chase");
        }

        private void ManualTick(LocalPlayer me, double dt)
        {
            Vector3 pos = me.MovementComponent.Position;
            float dist = Vector3.Distance(pos, _manualTarget.Value);
            if (dist <= 1.5f) { _manualTarget = null; Hold(me); return; }
            Step(me, _manualTarget.Value, dist, dt, "manual");
        }

        /// <summary>
        /// BEATS 1 + 2 for any destination: face it (rotating in place while it's well off our nose), then
        /// run at it in one capped step. One shared mover, so manual/lost/follow all move identically.
        /// </summary>
        private void Step(LocalPlayer me, Vector3 target, float dist, double dt, string what)
        {
            Vector3 pos = me.MovementComponent.Position;
            Vector3 delta = target - pos;
            if (delta.Magnitude < 0.05f) { Hold(me); return; }

            Vector3 dir = delta.Normalize();
            Quaternion want = Movement.SafeLook(dir, me.MovementComponent.Heading);
            float off = Movement.HeadingOffsetDeg(me.MovementComponent.Heading, want);
            bool instantTurn = _ctx.Config.FollowTurnDegPerSec <= 0f;   // mouse-look: face it in one packet

            // BEAT 1 — he's well off our nose (he came up behind us, or we just rounded a corner): turn
            // on the spot first, so we don't run sideways while slowly swinging round. Skipped entirely
            // in mouse-look mode — there the turn costs one packet, so there is nothing to wait for and
            // we face him and set off in the SAME frame.
            if (!instantTurn && off > _ctx.Config.FollowTurnFirstDeg)
            {
                Hold(me);
                _move.Face(me, want, _ctx.Config.FollowTurnDegPerSec, dt, _ctx.Config.SendIntervalMs);
                _ctx.WalkState = $"{what} turn-to d={dist:0.0} off={off:0}";
                return;
            }

            // BEAT 2 — run at him. Mouse-look faces him outright; keyboard mode eases the heading round
            // at the turn rate as it runs. Never step farther than MaxStep, so a lag spike can't warp us.
            Quaternion heading = instantTurn
                ? want
                : Movement.RotateToward(me.MovementComponent.Heading, want, (float)(_ctx.Config.FollowTurnDegPerSec * dt));
            float step = Math.Min((float)(MoveSpeed(me) * dt), _ctx.Config.MaxStep);
            step = Math.Min(step, dist);
            _ctx.WalkState = $"{what} tgt=({target.X:0},{target.Y:0},{target.Z:0}) d={dist:0.0} off={off:0} step={step:0.00}";
            _move.Advance(me, pos + dir * step, heading, run: true, dt, _ctx.Config.SendIntervalMs);
        }

        private void Hold(LocalPlayer me)
        {
            if (_move.Moving) _move.Stop(me, _ctx.Config.SendIntervalMs);
        }

        // ---- Recorded-route replay (NAV catch-up and saved paths) ----------------

        // Walks an exact list of recorded points in order. This is the wall-safe walker: every point was
        // actually walked by the owner, so it carries real ramp Y and never cuts a corner into geometry.
        private void ReplayTick(LocalPlayer me, double dt)
        {
            Vector3 pos = me.MovementComponent.Position;
            Vector3 target = _replay.Peek();
            float dist = Vector3.Distance(pos, target);

            if (dist <= _ctx.Config.CrumbArrive)
            {
                _replay.Dequeue(); _crumbStuck = 0; _lastCrumbDist = float.MaxValue;
                // End of a saved path: it ends at the doorway (we lost sight of the owner the instant he
                // zoned), so lean one push forward onto the trigger. A real zone line crosses en route.
                if (_replay.Count == 0 && !_replayPushed && _replayZonePush && _lastReplayDir.HasValue)
                {
                    _replay.Enqueue(pos + _lastReplayDir.Value * _ctx.Config.ZoneChaseMeters);
                    _replayPushed = true;
                }
                return;
            }

            // Skip a point we can't make progress toward (blocked by a wall).
            if (dist < _lastCrumbDist - 0.03f) _crumbStuck = 0; else _crumbStuck += dt;
            _lastCrumbDist = dist;
            if (_crumbStuck > _ctx.Config.StuckSeconds)
            {
                _ctx.Log($"STUCK-SKIP replay point at dist={dist:0.0} (blocked {_ctx.Config.StuckSeconds:0.0}s), skipping. remaining={_replay.Count - 1}");
                _replay.Dequeue(); _crumbStuck = 0; _lastCrumbDist = float.MaxValue;
                return;
            }

            _lastReplayDir = (target - pos).Normalize();
            Step(me, target, dist, dt, "replay");
        }

        // Full reset on a detected zone/teleport — drop old-playfield coordinates.
        public void Reset()
        {
            _replay.Clear(); _recentOwner.Clear();
            _zoneSweep = false; _replayPushed = false; _lastReplayDir = null;
            _manualTarget = null; _ownerPos = null; _lastSample = null; _lastRecord = null;
            _lostTarget = null; _lostPushed = false; _closing = false; _aligning = false; _mirrorLock = false;
            _crumbStuck = 0; _lastCrumbDist = float.MaxValue;
        }

        // 'stop'/'idle' clears active movement but keeps recording state.
        public void ClearMovement()
        {
            _manualTarget = null; _replay.Clear();
            _lostTarget = null; _lostPushed = false; _closing = false; _mirrorLock = false;
            _replayPushed = false; _lastReplayDir = null;
        }
    }
}