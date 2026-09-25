using AOSharp.Clientless;
using AOSharp.Common.GameData;

namespace AOBuddy
{
    /// <summary>
    /// ZONE-ARRIVAL WATCH (R3.2, scope adjusted) — the surviving half of the old owner-loss "zone
    /// episode": after WE cross a zone line or get teleported, count the seconds until the owner
    /// appears next to us. Crossing a line SEPARATELY from him is normal for a second or two while
    /// he loads, but if he never appears we have crossed into somewhere he is not — say so rather
    /// than standing in an empty zone waiting.
    ///
    /// The OTHER half — the automatic zone-sweep/give-up ladder (sweep a vanished-close-and-moving
    /// owner's crossing spot, up to 3 sweeps / 75 s, then tell him we can't get across) — was found
    /// DEAD during Phase 0: its gate read two fields that are never assigned anywhere, so it was
    /// unreachable on HEAD; only the manual 'zone'/'forward' commands and nav replay ever crossed
    /// lines. It was DELETED rather than silently re-enabled (a restore is a behaviour change and
    /// belongs in its own deliberate commit). For the record it was doubly broken: the 75 s episode
    /// clock was never incremented, and the sideways retry offset (0/+3m/-3m) was computed but
    /// never passed to the sweep. Crossing a line today: the owner sends 'zone'/'forward', or
    /// travel/overland does it on purpose.
    /// </summary>
    public class ZoneEpisode
    {
        private const double ArrivedAloneSeconds = 12.0;

        private readonly BotContext _ctx;
        private double _alone;

        public ZoneEpisode(BotContext ctx) { _ctx = ctx; }

        /// <summary>
        /// Per-frame watch. soloTravel: an overland route or a mission run crosses zones ON PURPOSE
        /// (the owner told it to), so arriving somewhere he isn't is not a loss to report.
        /// </summary>
        public void Tick(LocalPlayer me, bool ownerVisible, bool soloTravel, double dt)
        {
            if (ownerVisible || soloTravel) _alone = 0;
            else if (_alone > 0)
            {
                _alone += dt;
                if (_alone > ArrivedAloneSeconds)
                {
                    _alone = 0;   // once per arrival
                    Vector3 ap = me.MovementComponent.Position;
                    _ctx.Log($"ZONE: arrived in {Playfield.Name} at ({ap.X:0},{ap.Y:0},{ap.Z:0}) and you are not here after {ArrivedAloneSeconds:0}s.");
                    _ctx.TellOwner($"I zoned into {Playfield.Name} at ({ap.X:0},{ap.Y:0},{ap.Z:0}) but you're not here. Holding.");
                }
            }
        }

        /// <summary>Main's ClearNav on a detected zone/teleport: start the "did he follow me here?"
        /// clock (0.001, not 0 — 0 means "he's here / nothing to watch").</summary>
        public void ResetOnZone() { _alone = 0.001; }
    }
}