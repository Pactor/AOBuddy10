using System;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOBuddy
{
    /// <summary>
    /// OWNER TRACKING — everything the bot knows about one character: who he is, where he is, and
    /// where he is GOING between the server's sparse movement keyframes (R3.1). Main owns the
    /// orchestration on the edges (follow reacquire, travel arming, the zone ladder); this is the
    /// bookkeeping underneath it, in three parts:
    ///   * identity  — the configured owner by name (Find/IsOwner), plus the two ids that still
    ///                 identify him when the name map is empty: his dynel id (ChatId, cached while
    ///                 he is in view) and the tell-sender id proven once by name (_tellId).
    ///   * visibility — Visible/LastPos/LostSeconds, advanced once per frame by UpdateVisible; the
    ///                 Visible it left from LAST frame is what tells Main "just reacquired"/"just lost".
    ///   * interpolation — his movement keyframes from CharDCMove (OnKeyframe) and the position
    ///                 prediction built on them (PredictedPos), so the trail between the ~1/s
    ///                 keyframes is smooth and the bot keeps up.
    /// </summary>
    public class OwnerTracker
    {
        private readonly BotContext _ctx;
        private BuddyConfig Config => _ctx.Config;

        // Owner-interpolation state: his latest movement keyframe + derived velocity, so we can predict his
        // position between the server's sparse (~1/s) keyframes (see PredictedPos). Keyframe times are
        // ctx.Clock seconds (the one bot clock, R2.1).
        private Vector3? _keyPos;
        private double _keyTime;
        private Vector3 _vel;
        private Quaternion _keyHeading;
        private bool _movingKey;

        // Visibility bookkeeping (drives follow record/reacquire and travel arming). Visible holds the
        // LAST frame's outcome until UpdateVisible overwrites it, so Main can read the pair as an edge.
        public bool Visible { get; private set; }
        public Vector3? LastPos { get; private set; }     // his last in-view position (kept after he's lost)
        public double LostSeconds { get; private set; }   // how long he has been continuously out of view

        // How far off he was / whether he was actually travelling at the moment he vanished — the
        // "he walked a zone line, not outran us" gate. FOUND DEAD during Phase 0 (see RESTRUCTURE R3.2's
        // note): never assigned anywhere, so the auto zone-sweep ladder that reads them is unreachable.
        // They live here so R3.2 can wire (or delete) them with the rest of the zone-episode state.
        public float LostDist { get; set; }
        public bool LostMoving { get; set; }

        // Cached owner dynel id (0 until first seen in view). Mirrored into ctx.OwnerCharId, the shared
        // slot other controllers read (ResupplyController's trade check). The tell id below is a
        // DIFFERENT id: proven by a name-matched tell, kept for when the chat name map is empty.
        public int ChatId { get; private set; }
        private uint _tellId;

        public OwnerTracker(BotContext ctx) { _ctx = ctx; }

        /// <summary>The configured owner while he is in view, else null (by name, case-insensitive).</summary>
        public PlayerChar Find() =>
            DynelManager.Players.FirstOrDefault(p => string.Equals(p.Name, Config.Owner, StringComparison.OrdinalIgnoreCase));

        private bool IsOwner(string name) =>
            !string.IsNullOrEmpty(name) && string.Equals(name, Config.Owner, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Owner check for an incoming tell. A tell's SenderName is looked up in ChatClient.IdToNameMap and is
        /// literally "&lt;Unknown&gt;" until that map knows the id — so a name-only check silently DROPS the
        /// owner's own commands. Fall back to the sender id: remembered once seen, else the owner's dynel id.
        /// </summary>
        public bool IsOwnerSender(string name, uint senderId)
        {
            if (IsOwner(name))
            {
                if (senderId != 0) _tellId = senderId;
                return true;
            }
            if (senderId != 0 && senderId == _tellId) return true;
            PlayerChar owner = Find();
            return owner != null && senderId != 0 && (uint)owner.Identity.Instance == senderId;
        }

        /// <summary>
        /// Per-frame visibility bookkeeping. Feed Find()'s result (null = out of view). Advances
        /// LostSeconds, keeps LastPos fresh and the id caches loaded while he is visible, and returns
        /// THIS frame's visibility — Visible still held last frame's when this is called, so Main
        /// reads the pair for the reacquire/lost edges.
        /// </summary>
        public bool UpdateVisible(PlayerChar owner, double dt)
        {
            bool visible = owner != null;
            if (visible)
            {
                LostSeconds = 0;
                LastPos = owner.Transform.Position;
                ChatId = owner.Identity.Instance;
                _ctx.OwnerCharId = ChatId;
            }
            else
            {
                LostSeconds += dt;
            }
            Visible = visible;
            return visible;
        }

        /// <summary>Zone/teleport reset (Main's ClearNav): stop counting him lost from the old playfield.
        /// Keyframes and LastPos are deliberately kept — exactly what the inline reset did before.</summary>
        public void ResetOnZone() { LostSeconds = 0; }

        /// <summary>
        /// Feed every CharDCMove whose identity matches ChatId (Main's message handler does that test and
        /// keeps the MIRROR forwarding — that is orchestration, not tracking). Captures the keyframe and
        /// derives his velocity for interpolation.
        /// </summary>
        public void OnKeyframe(CharDCMoveMessage cm)
        {
            double now = _ctx.Clock.Seconds;
            Vector3 p = cm.Position;
            // Speed is only measured between two MOVING keyframes: across a stop->start gap the
            // displacement/time is ~0 and would leave him unpredicted for his whole first second.
            if (_keyPos.HasValue && _movingKey)
            {
                double d = now - _keyTime;
                if (d > 0.03)
                {
                    Vector3 v = (p - _keyPos.Value) / (float)d;
                    if (v.Magnitude <= 20f) _vel = v;   // ignore teleport-sized jumps
                }
            }
            _keyPos = p; _keyTime = now;
            _keyHeading = cm.Heading;
            _movingKey = IsMovingMove(cm.MoveType);
        }

        // A movement keyframe that means the owner is still moving (vs. a stop/sit).
        private static bool IsMovingMove(MovementAction mt) =>
            mt == MovementAction.ForwardStart || mt == MovementAction.Update
            || mt == MovementAction.SwitchToWalk || mt == MovementAction.SwitchToRun;

        // The owner's position, PREDICTED forward from his latest keyframe using his own reported velocity —
        // fills the gap between the server's sparse (~1/s) owner keyframes so the trail is smooth and the bot
        // keeps up. Extrapolates only while his last keyframe was "moving", only horizontally, only on flat
        // ground (no vertical guess on ramps), and only for a capped time (bounds any stop-overshoot). Falls
        // back to his raw position otherwise. Feeds crumbs, which stay wall-safe (it's his own motion).
        public Vector3 PredictedPos(PlayerChar owner)
        {
            if (!Config.OwnerInterp || !_keyPos.HasValue) return owner.Transform.Position;
            double dtSince = _ctx.Clock.Seconds - _keyTime;
            if (_movingKey && dtSince > 0)
            {
                // Past the cap, HOLD at the capped point rather than snapping back to the keyframe — snapping
                // back put the target behind the bot, which then turned round to walk back to it.
                float t = (float)Math.Min(dtSince, Config.OwnerInterpMaxSec);
                Vector3 kp = _keyPos.Value;
                // Direction from the heading HE REPORTED in that keyframe (he runs where he faces), not from
                // the previous keyframe-to-keyframe delta: that one points along his OLD leg, so every turn he
                // made sent the prediction — and the bot — shooting off past the corner. Speed is his own
                // measured horizontal speed; Y keeps his measured climb rate so ramps stay on the surface.
                Vector3 fwd = _keyHeading.Forward;
                Vector3 flat = new Vector3(fwd.X, 0f, fwd.Z);
                Vector3 velFlat = new Vector3(_vel.X, 0f, _vel.Z);
                float speed = velFlat.Magnitude;
                if (flat.Magnitude < 0.001f || speed < 0.1f) return kp;
                // Backpedalling / strafing: he isn't moving where he faces, so trust his measured direction.
                Vector3 h = Vector3.Dot(flat.Normalize(), velFlat) > 0.5f * speed ? flat.Normalize() * speed : velFlat;
                return new Vector3(kp.X + h.X * t, kp.Y + _vel.Y * t, kp.Z + h.Z * t);
            }
            return owner.Transform.Position;
        }
    }
}