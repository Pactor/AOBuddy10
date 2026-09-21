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

        private bool _moving;
        private bool _run;          // last gait, so a run<->walk switch re-issues the start packet
        private double _sendAccum;

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

        /// <summary>Move the body one frame: set the new pose, then send the right movement packet.</summary>
        public void Advance(LocalPlayer me, Vector3 newPos, Quaternion heading, bool run, double dt, int sendIntervalMs)
        {
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

        /// <summary>Halt the body (ForwardStop) if it was moving.</summary>
        public void Stop(LocalPlayer me, int sendIntervalMs)
        {
            if (_moving)
            {
                SendMove(me, MovementAction.ForwardStop, sendIntervalMs);
                _moving = false;
            }
        }

        public void Reset()
        {
            _moving = false;
            _sendAccum = 0;
        }
    }
}
