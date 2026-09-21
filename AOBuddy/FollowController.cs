using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;

namespace AOBuddy
{
    /// <summary>
    /// FOLLOW — the only system that walks the bot toward the owner. It records the owner's actual
    /// footsteps (breadcrumbs) and retraces them, so it takes valid ground and the SAME route the
    /// owner did — up and down ramps (each crumb carries the owner's real X/Y/Z, so a change in his
    /// Y is followed), around obstacles, through doors, and onto a zone line (which zones the bot).
    /// It NEVER beelines to the owner's live position: the live server terrain-validates movement and
    /// rejects a straight line up a slope (the "bouncing on the ramp" bug).
    ///
    /// This controller owns its own trail/manual/replay state. Nothing outside it touches _trail, so
    /// a change to zone-crossing or path replay here can never reach into combat or travel.
    /// </summary>
    public class FollowController
    {
        private readonly BotContext _ctx;
        private readonly Movement _move;

        // Breadcrumb trail of the owner's route.
        private readonly Queue<Vector3> _trail = new Queue<Vector3>();
        private Vector3? _lastCrumb;

        // The owner's last few recorded footsteps, kept so that when he vanishes at a line we can
        // continue along his REAL recent path (its averaged direction, which follows his curve),
        // instead of a straight guess off one noisy sample.
        private readonly Queue<Vector3> _recentOwner = new Queue<Vector3>();

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

        // Zone-crossing marker: true while the final crumb is a push meant to carry the bot ONTO a
        // zone trigger. Keeps the stuck-skip from throwing that last crumb away.
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
        // re-acquire him when he simply walked out of sight — "follow my path"), but only a little BACK,
        // so each pass still re-crosses the loss point where a real zone line sits. Symmetric ±3 only
        // oscillated in place and never rounded corners once the trail emptied (which it now does, because
        // at real run speed he keeps up instead of lagging with a backlog of breadcrumbs).
        private const float SweepForward = 8.0f;    // how far ahead along his heading each forward pass goes
        private const float SweepBack = 3.0f;       // how far back past the loss point (recross the line)
        private const float SweepSpan = SweepForward; // (kept for the start log)
        private const float SweepSpeed = 3.0f;      // slow (and under the server's ~4u/s cap) so he dwells in
                                                     // the ~2-unit band long enough for a heartbeat tick to catch him.
        private const double SweepTimeout = 16.0;   // give up after this long and hold for reacquire

        public bool ZoneSweeping => _zoneSweep;

        // Stuck detection for the current crumb.
        private double _crumbStuck;
        private float _lastCrumbDist = float.MaxValue;

        // How far we've coasted (dead-reckoned along the owner's last direction) since his last real position
        // update. Bounded so we never coast far if he actually stopped; reset when a fresh crumb arrives.
        private double _coastAccum;

        public int TrailCount => _trail.Count;
        public int ReplayCount => _replay.Count;
        public bool ZoneCrossing => _zoneCrossing;
        public bool Recording => _recording;

        // Does the bot have somewhere to walk right now? (drives the "Following" vs "Idle" label)
        public bool HasWork => _manualTarget.HasValue || _replay.Count > 0 || _trail.Count > 0;

        // A one-off manual destination is set (a queued cast must wait for it to finish).
        public bool ManualActive => _manualTarget.HasValue;

        // The point the bot is walking toward right now (manual > replay > trail), or null if idle. Lets
        // Main notice when the queued path leads AWAY from where the owner now is (he walked back).
        public Vector3? CurrentTarget()
        {
            if (_manualTarget.HasValue) return _manualTarget;
            if (_replay.Count > 0) return _replay.Peek();
            if (_trail.Count > 0) return _trail.Peek();
            return null;
        }

        public FollowController(BotContext ctx, Movement move)
        {
            _ctx = ctx;
            _move = move;
        }

        // ---- Recording the owner's route ----------------------------------------

        // Owner is visible: drop a breadcrumb when he's moved far enough from the last one. His
        // recorded positions carry the correct terrain height, so retracing them stays server-legal.
        public void Record(PlayerChar owner) => RecordAt(owner.Transform.Position);

        // Drop a breadcrumb at a given position — used with the OWNER-INTERPOLATED position so the trail is
        // smooth between the server's sparse (~1/s) owner keyframes, instead of choppy raw keyframes.
        public void RecordAt(Vector3 op)
        {
            if (!_lastCrumb.HasValue || Vector3.Distance(op, _lastCrumb.Value) >= _ctx.Config.BreadcrumbSpacing)
            {
                _trail.Enqueue(op);
                _lastCrumb = op;
                _coastAccum = 0;   // a real owner update arrived — stop counting coast distance
                while (_trail.Count > _ctx.Config.MaxTrail) _trail.Dequeue();
                _recentOwner.Enqueue(op);
                while (_recentOwner.Count > 6) _recentOwner.Dequeue();
            }
        }

        // Owner reappeared after being out of view. If he's far from the last crumb it's a real
        // teleport / new area, so the old path is garbage — drop it. Otherwise keep his exact route.
        public void OnReacquired(PlayerChar owner)
        {
            CancelZoneSweep();   // he's back in view — stop sweeping the line, follow normally
            if (!_lastCrumb.HasValue) return;
            float gap = Vector3.Distance(owner.Transform.Position, _lastCrumb.Value);
            if (gap > 40f)
            {
                _ctx.Log($"Owner reacquired {gap:0}m from last crumb — new area, cleared trail({_trail.Count}).");
                _trail.Clear();
                _lastCrumb = null;
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

        // Begin the zone sweep across the owner's vanish spot, along his real recent travel direction.
        // Returns false if we don't have enough path history to know his direction. Main arms this ONLY
        // when the owner vanished close ahead while following and NOT in combat.
        public bool StartZoneSweep(Vector3 vanishSpot)
        {
            if (_recentOwner.Count < 2) return false;
            Vector3[] pts = _recentOwner.ToArray();
            Vector3 dir = pts[pts.Length - 1] - pts[0];
            if (dir.Length() < 0.1f) return false;
            BeginSweep(vanishSpot, dir.Normalize(), "owner vanished ahead while following (not combat) — sweeping his crossing spot");
            return true;
        }

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
            _trail.Clear();   // sweep owns movement now
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
            _trail.Clear(); _lastCrumb = null;   // an explicit replay overrides the live trail (drop stale/backwards crumbs)
            _replay.Clear(); _replayPushed = false; _lastReplayDir = null; _replayZonePush = zonePush;
            foreach (Vector3 p in pts) _replay.Enqueue(p);
        }

        public void StopReplay() { _replay.Clear(); _replayPushed = false; _lastReplayDir = null; }

        // ---- Walking (per frame, small steps, real run, never teleport) ----------

        // Move one frame. Priority: a manual target, then a saved-path replay, then the owner's
        // breadcrumb trail. If there is nothing to walk, hold. Called by Main's movement arbiter
        // only when no higher-priority mover (travel) is active and the bot isn't casting/resting.
        // The bot runs at ITS OWN run speed, using the game client's EXACT velocity formula:
        //     velocity (u/s) = 5.5 + RunSpeed / 230        (RunSpeed = Exploring->Run Speed skill, Stat 156)
        // Base 5.5 u/s at 0 skill; +1 u/s per 230 skill; the client's own hard cap is 15.5 u/s (RunSpeed
        // 2300). So RunSpeed 36 -> ~5.66 u/s, and training the skill raises it. This is exactly what the
        // real client moves at, so the server accepts it — no guessed coefficient. Fall back to config only
        // if the stat can't be read.
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
        // coasting a guessed height there is exactly the old rubber-band, so we defer to the recorded trail).
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

        public void WalkTick(LocalPlayer me, double dt)
        {
            if (_zoneSweep) { SweepTick(me, dt); return; }

            if (_trail.Count == 0) _zoneCrossing = false;

            Vector3 target;
            float arrive;
            bool coasting = false;
            Queue<Vector3> crumbs = null;   // the queue to advance when a point is reached

            if (_manualTarget.HasValue)
            {
                target = _manualTarget.Value; arrive = 1.5f;
            }
            else if (_replay.Count > 0)
            {
                crumbs = _replay; target = _replay.Peek(); arrive = _ctx.Config.CrumbArrive;
            }
            else if (_ctx.Config.Follow && _trail.Count > 0)
            {
                // Walk his recorded footsteps — oldest first, dequeued as we arrive. We walk the path
                // whether or not he's currently in view, so a brief disappearance (a door) doesn't stop
                // us, and a path that ends on a zone line walks us onto the trigger and zones us.
                crumbs = _trail; target = _trail.Peek(); arrive = _ctx.Config.CrumbArrive;
            }
            else if (_ctx.Config.FollowLiveLead && TryCoastTarget(me, out Vector3 coastTgt))
            {
                // Trail's used up but the owner was just moving and his next position update is delayed
                // (his position reaches us in bursts). Rather than idle and lurch, COAST a little along his
                // last direction so we're already moving when the update lands. Flat + bounded only (see
                // TryCoastTarget) so it can never coast a wrong height up a ramp.
                target = coastTgt; arrive = _ctx.Config.CrumbArrive; coasting = true;
            }
            else
            {
                _ctx.WalkState = $"idle (follow={_ctx.Config.Follow} trail={_trail.Count})";
                _move.Stop(me, _ctx.Config.SendIntervalMs);
                return;
            }

            Vector3 pos = me.MovementComponent.Position;
            float dist = Vector3.Distance(pos, target);

            if (dist <= arrive)
            {
                if (crumbs != null)
                {
                    crumbs.Dequeue(); _crumbStuck = 0; _lastCrumbDist = float.MaxValue;
                    // End of a saved path: it ends at the doorway (we lost sight of the owner the
                    // instant he zoned), so lean one push forward onto the trigger. A real zone line
                    // crosses en route (ClearNav fires on the position jump); otherwise we just stop.
                    if (crumbs == _replay && crumbs.Count == 0 && !_replayPushed && _replayZonePush && _lastReplayDir.HasValue)
                    {
                        _replay.Enqueue(pos + _lastReplayDir.Value * _ctx.Config.ZoneChaseMeters);
                        _replayPushed = true;
                    }
                    return;
                }
                _manualTarget = null;
                _move.Stop(me, _ctx.Config.SendIntervalMs);
                return;
            }

            // Skip a point we can't make progress toward (blocked by a wall) — but never skip the
            // final push onto a zone-line trigger; keep leaning into it.
            if (crumbs != null && !(crumbs == _trail && _zoneCrossing))
            {
                if (dist < _lastCrumbDist - 0.03f) _crumbStuck = 0; else _crumbStuck += dt;
                _lastCrumbDist = dist;
                if (_crumbStuck > _ctx.Config.StuckSeconds)
                {
                    _ctx.Log($"STUCK-SKIP {(crumbs == _replay ? "replay" : "trail")} crumb at dist={dist:0.0} (blocked {_ctx.Config.StuckSeconds:0.0}s), skipping. remaining={crumbs.Count - 1}");
                    crumbs.Dequeue(); _crumbStuck = 0; _lastCrumbDist = float.MaxValue; return;
                }
            }

            Vector3 dir = (target - pos).Normalize();
            if (crumbs == _replay) _lastReplayDir = dir;
            if (coasting) _coastAccum += Math.Min(dist - arrive, Math.Min((float)(MoveSpeed(me) * dt), _ctx.Config.MaxStep));
            Quaternion heading = Movement.SafeLook(dir, me.MovementComponent.Heading);
            float speed = MoveSpeed(me);   // capped at the char's run speed or the server rejects the move
            float step = Math.Min(dist - arrive, (float)(speed * dt));
            step = Math.Min(step, _ctx.Config.MaxStep);   // hard cap so a lag spike can never warp him
            _ctx.WalkState = $"{(coasting ? "coast" : crumbs == _replay ? "replay" : crumbs == _trail ? "follow" : "manual")} tgt=({target.X:0},{target.Y:0},{target.Z:0}) d={dist:0.0} step={step:0.00} coast={_coastAccum:0.0}";

            // Dead zone: he's a hair past 'arrive' so the step rounds to ~0. Treat it as reached
            // (advance the crumb / clear the manual target) and stop, so he doesn't freeze 1m short.
            if (step <= 0.05f)
            {
                if (crumbs != null) { crumbs.Dequeue(); _crumbStuck = 0; _lastCrumbDist = float.MaxValue; }
                else _manualTarget = null;
                if (_trail.Count == 0) _zoneCrossing = false;
                _move.Stop(me, _ctx.Config.SendIntervalMs);
                return;
            }

            _move.Advance(me, pos + dir * step, heading, run: true, dt, _ctx.Config.SendIntervalMs);
        }

        // Full reset on a detected zone/teleport — drop old-playfield coordinates.
        public void Reset()
        {
            _replay.Clear(); _trail.Clear(); _recentOwner.Clear();
            _zoneCrossing = false; _zoneSweep = false; _replayPushed = false; _lastReplayDir = null;
            _manualTarget = null; _lastCrumb = null; _lastRecord = null;
            _crumbStuck = 0; _lastCrumbDist = float.MaxValue;
        }

        // 'stop'/'idle' clears active movement but keeps recording state.
        public void ClearMovement()
        {
            _manualTarget = null; _trail.Clear(); _replay.Clear();
            _replayPushed = false; _lastReplayDir = null;
        }
    }
}
