using System;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOBuddy
{
    /// <summary>
    /// USE-OBJECT TRAVEL — the owner rode a lift / grid terminal / whompa / mission door-button.
    /// That is a GenericCmd 'Use' on the object (a right-click), NOT a walked zone line, so FOLLOW's
    /// breadcrumbs can't trace it — the owner simply vanishes to another zone server. We watch the
    /// owner use objects (DynelManager.DynelUsed, forwarded by Main) and, if he then zones right
    /// after, walk to that same object and Use() it so the bot rides it too, up or down with him.
    ///
    /// While a use-travel is pending this controller OWNS movement (Main gives it priority over
    /// follow); it hands movement back to follow the instant the owner is visible again.
    /// </summary>
    public class TravelController
    {
        private readonly BotContext _ctx;
        private readonly Movement _move;

        private Identity? _ownerUsedTarget;   // last object the owner used
        private Vector3? _ownerUsedPos;       // its position when used
        private double _ownerUsedAge;         // seconds since the owner used it

        private Identity? _pendingUseTravel;  // confirmed travel object the bot must go use
        private Vector3? _pendingUsePos;      // where that object is
        private double _useTravelElapsed;     // time spent trying to reach/use it (timeout guard)

        private const double UseTravelWindowSeconds = 5.0;  // owner-use counts as travel if he vanishes within this
        private const double UseTravelGrace = 2.5;          // owner must stay gone this long before we commit (ignores flickers)
        private const float UseTravelRange = 4.0f;          // how close the bot must be to Use() the object
        private const double UseTravelTimeout = 10.0;       // give up if the object can't be reached in this long
        private const float WalkSpeed = 9.0f;               // WALK (not run) up to the terminal so we don't overshoot it

        public bool Active => _pendingUseTravel.HasValue;

        public TravelController(BotContext ctx, Movement move)
        {
            _ctx = ctx;
            _move = move;
        }

        // The owner used an object. Remember what and where; if he then vanishes within the window,
        // OnOwnerLost turns this into a pending use-travel.
        public void OnOwnerUsed(Identity target, Vector3? pos)
        {
            _ownerUsedTarget = target;
            _ownerUsedPos = pos;
            _ownerUsedAge = 0;
            _ctx.Log($"OWNER used object {target}" + (pos.HasValue ? $" at ({pos.Value.X:0},{pos.Value.Y:0},{pos.Value.Z:0})" : "") + ".");
        }

        public void Age(double dt)
        {
            if (_ownerUsedTarget.HasValue) _ownerUsedAge += dt;
        }

        // Owner just dropped out of view. If he used a travel object within the window, arm the ride.
        public void OnOwnerLost(Vector3? lastOwnerPos)
        {
            if (_ownerUsedTarget.HasValue && _ownerUsedAge <= UseTravelWindowSeconds)
            {
                _pendingUseTravel = _ownerUsedTarget;
                _pendingUsePos = _ownerUsedPos ?? lastOwnerPos;
                _useTravelElapsed = 0;
                _ownerUsedTarget = null;   // consumed
                _ctx.Log($"OWNER LOST right after using an object — USE-TRAVEL: heading to {_pendingUseTravel.Value} to ride it.");
            }
        }

        // Move one frame toward the travel object and Use() it. Returns true if it consumed this
        // frame's movement (Main then skips follow). Aborts the instant the owner is visible again.
        public bool Tick(LocalPlayer me, double dt, bool ownerVisible, double ownerLostSeconds)
        {
            if (!_pendingUseTravel.HasValue) return false;

            if (ownerVisible)
            {
                _ctx.Log("USE-TRAVEL: owner visible again — aborting, following normally.");
                Clear();
                return false;
            }

            // Wait a short grace so a brief owner-visibility flicker near a terminal doesn't commit us.
            if (ownerLostSeconds < UseTravelGrace) return false;

            _useTravelElapsed += dt;
            if (_useTravelElapsed > UseTravelTimeout)
            {
                _ctx.Log($"USE-TRAVEL: gave up reaching {_pendingUseTravel} after {UseTravelTimeout:0}s.");
                Clear();
                _move.Stop(me, _ctx.Config.SendIntervalMs);
                return true;
            }

            Vector3 pos = me.MovementComponent.Position;
            // Prefer the object's live position if it's still spawned (it may have moved slightly).
            if (DynelManager.Find(_pendingUseTravel.Value, out Dynel found)) _pendingUsePos = found.Transform.Position;
            Vector3 goal = _pendingUsePos ?? pos;
            float dist = Vector3.Distance(pos, goal);

            if (dist <= UseTravelRange)
            {
                _move.Stop(me, _ctx.Config.SendIntervalMs);
                // Send the Use straight to the captured identity. Travel objects (mission floor
                // buttons/terminals, grid, whompas) aren't tracked as findable dynels, so a
                // lookup-based Dynel.Use() never fires — we command the exact identity we captured.
                Client.Send(new GenericCmdMessage
                {
                    Action = GenericCmdAction.Use,
                    User = me.Identity,
                    Target = _pendingUseTravel.Value,
                    Count = 1,
                    Temp4 = 1,
                });
                _ctx.Log($"USE-TRAVEL: sent Use to {_pendingUseTravel.Value} at ({goal.X:0},{goal.Y:0},{goal.Z:0}) — expecting to zone.");
                Clear();
                return true;
            }

            Vector3 dir = (goal - pos).Normalize();
            float step = Math.Min((float)(WalkSpeed * dt), _ctx.Config.MaxStep);
            step = Math.Min(step, dist);
            _ctx.WalkState = $"use-travel(walk) d={dist:0.0} -> ({goal.X:0},{goal.Y:0},{goal.Z:0}) t={_useTravelElapsed:0.0}";

            _move.Advance(me, pos + dir * step, Movement.SafeLook(dir, me.MovementComponent.Heading), run: false, dt, _ctx.Config.SendIntervalMs);
            return true;
        }

        private void Clear()
        {
            _pendingUseTravel = null; _pendingUsePos = null; _useTravelElapsed = 0;
        }

        public void Reset()
        {
            Clear();
            _ownerUsedTarget = null; _ownerUsedPos = null; _ownerUsedAge = 0;
        }
    }
}
