using System;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOBuddy
{
    /// <summary>
    /// The ONE place the bot's body moves. Every mover (follow, travel, manual) calls Advance()
    /// to take a single per-frame step and Stop() to halt. This centralises the exact packet
    /// sequence a real client sends when it walks/runs, so movement mechanics live in one file and
    /// changing how a system decides WHERE to go never changes HOW the body moves.
    ///
    /// Why it works like the client: ForwardStart begins the run/walk (animation + a "moving"
    /// state so the server registers position, including zone-line crossings); periodic Update
    /// packets keep it going; ForwardStop ends it. The DeltaTime must be a plausible
    /// ms-since-last-move or the live server ignores the move. Position is driven by SetPose
    /// (never a teleport) so the server terrain-validates each small step.
    /// </summary>
    public class Movement
    {
        public static readonly Vector3 Up = new Vector3(0f, 1f, 0f);

        // A movement heading from a travel direction, safe when the direction is (near) vertical. LookRotation
        // divides by zero if forward is parallel to Up (a straight up/down move, e.g. stacked points on a
        // lift/ramp) — the "Can not normalize a Vector with no direction" spam. We only need horizontal facing,
        // so build the heading from the flattened direction; if there's no horizontal component, keep facing.
        public static Quaternion SafeLook(Vector3 dir, Quaternion fallback)
        {
            Vector3 flat = new Vector3(dir.X, 0f, dir.Z);
            return flat.Magnitude > 0.001f ? Quaternion.LookRotation(flat.Normalize(), Up) : fallback;
        }

        // Horizontal (X/Z) distance — walkers steer on the ground plane; the Y gap to a goal (a ramp, a
        // lift, a stacked floor) is not distance to walk. THE one definition (R1.1): five private copies
        // used to live across the walkers and could drift apart.
        public static float Flat(Vector3 a, Vector3 b) { float dx = a.X - b.X, dz = a.Z - b.Z; return (float)Math.Sqrt(dx * dx + dz * dz); }

        // The same measure against bare coordinates (a configured spot, not a dynel position).
        public static float Flat(Vector3 a, float x, float z) { float dx = a.X - x, dz = a.Z - z; return (float)Math.Sqrt(dx * dx + dz * dz); }

        // The capped step EVERY walker takes (R1.4): never more than speed*dt, never more than MaxStep
        // (a lag spike must not warp us across the world), never more than what remains to the goal.
        public static float CappedStep(float speed, double dt, float maxStep, float remain)
            => Math.Min(Math.Min((float)(speed * dt), maxStep), remain);

        // ---- Heading math: YAW ONLY, and NEVER Quaternion.Slerp -------------------
        // A character heading in AO is a pure yaw (LookRotation of a flattened direction returns a
        // quaternion with X=Z=0), so every turn is a scalar angle problem. We do it in yaw degrees and
        // rebuild the quaternion with LookRotation — the same call that already produces the headings the
        // server accepts while running.
        //
        // AOSharp's Quaternion.Slerp is BROKEN and must not be used: measured against this SDK build,
        // Slerp(a,b,0) does not return a (it returns a quaternion ~49 degrees off for a 90-degree turn),
        // increasing t moves AWAY from b, and t=1 throws DivideByZeroException ("Can not normalize a
        // Vector with no direction") out of its internal AngleAxis ctor. Feeding a per-frame turn through
        // it froze the bot's heading — it sat at a constant offset and never faced anything.

        // Heading as a yaw angle in degrees, measured from +Z, turning toward +X. Range (-180, 180].
        public static float YawDeg(Quaternion q)
        {
            Vector3 f = q.Forward;
            if (Math.Abs(f.X) < 1e-6f && Math.Abs(f.Z) < 1e-6f) return 0f;
            return (float)(Math.Atan2(f.X, f.Z) * 180.0 / Math.PI);
        }

        // A heading from a yaw angle (the inverse of YawDeg).
        public static Quaternion FromYawDeg(float yawDeg)
        {
            double r = yawDeg * Math.PI / 180.0;
            return Quaternion.LookRotation(new Vector3((float)Math.Sin(r), 0f, (float)Math.Cos(r)), Up);
        }

        // Shortest signed turn from -> to, in degrees: positive = one way, negative = the other.
        public static float SignedOffsetDeg(Quaternion from, Quaternion to)
        {
            float d = YawDeg(to) - YawDeg(from);
            while (d > 180f) d -= 360f;
            while (d < -180f) d += 360f;
            return d;
        }

        // Angle (degrees) between two headings, measured on the horizontal plane only — the bot never
        // pitches, and a Y difference in the look vector would otherwise report a turn that doesn't exist.
        public static float HeadingOffsetDeg(Quaternion from, Quaternion to) => Math.Abs(SignedOffsetDeg(from, to));

        // Which way round is shorter, for the TurnLeft/TurnRight packet (the resulting heading is set
        // explicitly either way, so this only picks the animation the server plays).
        public static bool TurnsRight(Quaternion from, Quaternion to) => SignedOffsetDeg(from, to) >= 0f;

        // Rotate `from` toward `to` by at most maxDeg — a real, rate-limited turn instead of a snap.
        public static Quaternion RotateToward(Quaternion from, Quaternion to, float maxDeg)
        {
            float off = SignedOffsetDeg(from, to);
            if (Math.Abs(off) <= maxDeg) return to;
            return FromYawDeg(YawDeg(from) + (off > 0 ? maxDeg : -maxDeg));
        }

        private bool _moving;
        private bool _run;          // last gait, so a run<->walk switch re-issues the start packet
        private double _sendAccum;
        private int _turning;       // 0 = not turning, +1 = TurnRight in progress, -1 = TurnLeft
        private double _turnAccum;

        // LOCKSTEP LEASH: don't let the body's dictated (SetPose) position run more than _leashLead metres
        // ahead of the last position the SERVER confirmed. When the server is actively correcting us (a
        // desync/rejection storm), racing the local position far ahead just gets it violently snapped back
        // (the catch-up rubberband); leashing it makes the bot take gradual, server-paced steps and hold at
        // the leash instead of overshooting. Main only sets the anchor while corrections are RECENT, so
        // free movement (server happy, no corrections) is never clamped, and the lead is kept above the
        // normal ramp Y-jitter so ordinary ramp-follow isn't affected.
        private Vector3? _leashAnchor;
        private float _leashLead;
        public bool Leashed { get; private set; }

        public void SetLeash(Vector3? anchor, float lead) { _leashAnchor = anchor; _leashLead = lead; }

        public bool Moving => _moving;

        // Keep the sent transform (MovementComponent) and the read transform (Dynel.Transform,
        // used by DistanceFrom) in sync — they're separate objects on the LocalPlayer.
        public static void SetPose(LocalPlayer me, Vector3 pos, Quaternion heading)
        {
            me.MovementComponent.Position = pos;
            me.MovementComponent.Heading = heading;
            me.Transform.Position = pos;
            me.Transform.Heading = heading;
        }

        private void SendMove(LocalPlayer me, MovementAction moveType, int deltaMs)
        {
            Client.Send(new CharDCMoveMessage
            {
                Identity = me.Identity,
                MoveType = moveType,
                Heading = me.MovementComponent.Heading,
                Position = me.MovementComponent.Position,
                DeltaTime = Math.Max(1, deltaMs),
            });
        }

        // ---- MIRROR: replay the owner's own movement packets as ours ----------------
        // Once the bot stands on his spot with his facing, the cheapest perfect follow is to say exactly
        // what his client says: every CharDCMove he sends (start/stop, strafe, turn, jump, updates) is re-sent
        // with OUR identity and HIS position/heading/delta. The server then simulates both of us identically,
        // so we stay on his spot with no prediction, no chasing and no overshoot.
        private bool _mirrored;
        public bool Mirrored => _mirrored;

        // Movement types worth copying. Postures (sit, sleep, lounge, fly, frozen) are NOT: the bot has its
        // own rest logic and must never be locked into one of those by the owner's keyboard.
        public static bool IsMirrorable(MovementAction a)
        {
            switch (a)
            {
                case MovementAction.SwitchToSit: case MovementAction.LeaveSit:
                case MovementAction.SwitchToSleep: case MovementAction.LeaveSleep:
                case MovementAction.SwitchToLounge: case MovementAction.LeaveLounge:
                case MovementAction.SwitchToFly: case MovementAction.LeaveFly:
                case MovementAction.SwitchToFrozen: case MovementAction.LeaveFrozen:
                case MovementAction.Unknown0x1f: case MovementAction.Unknown0x20:
                    return false;
                default:
                    return true;
            }
        }

        public void Mirror(LocalPlayer me, CharDCMoveMessage m)
        {
            SetPose(me, m.Position, m.Heading);
            me.MovementComponent.SetFlags(m.MoveType);
            Client.Send(new CharDCMoveMessage
            {
                Identity = me.Identity,
                MoveType = m.MoveType,
                Heading = m.Heading,
                Position = m.Position,
                DeltaTime = m.DeltaTime,
                Unknown2 = m.Unknown2,
                Unknown3 = m.Unknown3,
            });
            if (m.MoveType == MovementAction.SwitchToRun) _run = true;
            else if (m.MoveType == MovementAction.SwitchToWalk) _run = false;
            _moving = (me.MovementComponent.Flags & ~(MovementFlags.TurningLeft | MovementFlags.TurningRight)) != MovementFlags.None;
            _turning = 0; _turnAccum = 0; _sendAccum = 0;
            _mirrored = true;
        }

        // Hand the body back to the bot's own movers. The owner may have left us strafing/backpedalling/
        // turning, which ForwardStop alone wouldn't end — so one FullStop clears every movement state.
        public void LeaveMirror(LocalPlayer me, int sendIntervalMs)
        {
            if (!_mirrored) return;
            _mirrored = false;
            if (me.MovementComponent.Flags != MovementFlags.None || _moving)
            {
                me.MovementComponent.SetFlags(MovementAction.FullStop);
                SendMove(me, MovementAction.FullStop, sendIntervalMs);
            }
            _moving = false; _turning = 0; _turnAccum = 0; _sendAccum = 0;
        }

        /// <summary>Move the body one frame: set the new pose, then send the right movement packet.</summary>
        public void Advance(LocalPlayer me, Vector3 newPos, Quaternion heading, bool run, double dt, int sendIntervalMs)
        {
            LeaveMirror(me, sendIntervalMs);

            // Leash: never dictate a position more than _leashLead ahead of the server's last-confirmed one.
            Leashed = false;
            if (_leashAnchor.HasValue && _leashLead > 0f)
            {
                Vector3 fromAnchor = newPos - _leashAnchor.Value;
                if (fromAnchor.Magnitude > _leashLead)
                {
                    newPos = _leashAnchor.Value + fromAnchor.Normalize() * _leashLead;
                    Leashed = true;
                }
            }

            SetPose(me, newPos, heading);

            StopTurn(me, sendIntervalMs);   // a turn-in-place and a run are different client states

            if (!_moving || _run != run)
            {
                me.MovementComponent.ChangeMovement(run ? MovementAction.SwitchToRun : MovementAction.SwitchToWalk);
                SendMove(me, MovementAction.ForwardStart, sendIntervalMs);
                _moving = true; _run = run; _sendAccum = 0;
            }
            else
            {
                _sendAccum += dt;
                if (_sendAccum >= sendIntervalMs / 1000.0)
                {
                    SendMove(me, MovementAction.Update, (int)(_sendAccum * 1000));
                    _sendAccum = 0;
                }
            }
        }

        /// <summary>
        /// Turn the body IN PLACE toward a heading. TWO MODES, matching the two the real client has:
        ///
        ///   degPerSec &lt;= 0  — MOUSE-LOOK. Right-click-drag in the client does not use the turn keys at
        ///     all: it writes the new heading straight into the movement packet, so the character faces
        ///     wherever you point in one update. That is why dragging spins you far faster than A/D. The
        ///     bot is clientless, so this mode is free: set the heading, send one Update, done — no turn
        ///     animation state, no waiting. (Proven legal here: the old trail follower snapped its
        ///     heading every frame this way and the server never rejected it.)
        ///
        ///   degPerSec &gt; 0   — KEYBOARD. TurnLeft/TurnRightStart, Update while turning, TurnStop when
        ///     aligned, rotating at the given rate. Slower, but it's what a watching player expects to
        ///     see, and it's the only mode that produces a turn animation.
        ///
        /// Returns true while still turning (mouse-look is never "still turning" — it arrives at once).
        /// </summary>
        public bool Face(LocalPlayer me, Quaternion want, float degPerSec, double dt, int sendIntervalMs)
        {
            LeaveMirror(me, sendIntervalMs);
            Quaternion cur = me.MovementComponent.Heading;
            float off = HeadingOffsetDeg(cur, want);
            if (off < 1.0f) { StopTurn(me, sendIntervalMs); return false; }

            if (degPerSec <= 0f)
            {
                StopTurn(me, sendIntervalMs);
                SetPose(me, me.MovementComponent.Position, want);
                SendMove(me, MovementAction.Update, sendIntervalMs);   // tell the server the new facing now
                return false;
            }

            int sign = TurnsRight(cur, want) ? 1 : -1;
            Quaternion next = RotateToward(cur, want, (float)(degPerSec * dt));
            SetPose(me, me.MovementComponent.Position, next);

            if (_turning != sign)
            {
                if (_turning != 0) SendMove(me, _turning > 0 ? MovementAction.TurnRightStop : MovementAction.TurnLeftStop, sendIntervalMs);
                SendMove(me, sign > 0 ? MovementAction.TurnRightStart : MovementAction.TurnLeftStart, sendIntervalMs);
                _turning = sign; _turnAccum = 0;
            }
            else
            {
                _turnAccum += dt;
                if (_turnAccum >= sendIntervalMs / 1000.0)
                {
                    SendMove(me, MovementAction.Update, (int)(_turnAccum * 1000));
                    _turnAccum = 0;
                }
            }
            return true;
        }

        public void StopTurn(LocalPlayer me, int sendIntervalMs)
        {
            if (_turning == 0) return;
            SendMove(me, _turning > 0 ? MovementAction.TurnRightStop : MovementAction.TurnLeftStop, sendIntervalMs);
            _turning = 0; _turnAccum = 0;
        }

        /// <summary>Halt the body (ForwardStop) if it was moving.</summary>
        public void Stop(LocalPlayer me, int sendIntervalMs)
        {
            LeaveMirror(me, sendIntervalMs);
            StopTurn(me, sendIntervalMs);
            if (_moving)
            {
                SendMove(me, MovementAction.ForwardStop, sendIntervalMs);
                _moving = false;
            }
        }

        /// <summary>Halt the body if it was moving — the "hold position" every walker issues on a frame
        /// with nowhere to go this tick. Guarded so holding every frame sends no ForwardStop spam (R1.3).</summary>
        public void Hold(LocalPlayer me, int sendIntervalMs)
        {
            if (_moving) Stop(me, sendIntervalMs);
        }

        /// <summary>
        /// Watch for "not making progress toward a target" (R1.5): remembers the CLOSEST we have been,
        /// and accumulates time whenever no new closest appears. Reset on progress and on retarget.
        /// Each walker keeps its OWN instance, margin and threshold — those are behaviour, not plumbing.
        /// Two look-alikes are deliberately NOT this: FollowController's replay walker compares
        /// per-frame deltas (not best-ever), and ResupplyController's watch uses absolute phase time.
        /// </summary>
        public struct StuckWatch
        {
            private float _best;
            private double _stuckFor;
            public void Reset() { _best = float.MaxValue; _stuckFor = 0; }
            /// <summary>One distance observation. True when stuck longer than stuckSec. margin = how much
            /// closer than the best-ever distance still counts as progress.</summary>
            public bool Tick(float dist, double dt, float margin, double stuckSec)
            {
                if (dist < _best - margin) { _best = dist; _stuckFor = 0; }
                else _stuckFor += dt;
                return _stuckFor > stuckSec;
            }
        }

        public void Reset()
        {
            _mirrored = false;
            _moving = false;
            _sendAccum = 0;
            _turning = 0;
            _turnAccum = 0;
        }
    }
}
