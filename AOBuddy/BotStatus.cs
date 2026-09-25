namespace AOBuddy
{
    /// <summary>
    /// Cross-system status read-model (R2.2): the live facts controllers used to get by reaching back
    /// into Main through constructor lambdas (MissionRun took seven of them). Main refreshes these once
    /// per tick, just before Walk — after ChewyBuffController has queued this frame's casts, so
    /// HasPendingCasts/SecondsSinceCast are current for the systems that yield to casting — and
    /// everything downstream reads plain fields off ctx.Status. Values are a snapshot as of the start
    /// of the tick; anything reading them mid-tick (a command handler) sees the same freshness the old
    /// lazy lambdas gave a few statements later. Fields are written on the update thread only.
    /// </summary>
    public class BotStatus
    {
        public bool Dead;                 // the reclaim wait (Main's death detector)
        public bool Resting;              // SupportController.Resting — actually sitting to rest
        public bool HasPendingCasts;      // SupportController's shared cast queue is non-empty
        public double SecondsSinceCast = 999;   // pre-first-cast value (SupportController starts _lastCastAt at -999)
        public bool InCombat;             // CombatController.InCombat OR HostilesEngaged(me, owner) — the assist condition
        public bool NeedsRecovery;        // SupportController.NeedsRecovery(me)
        public int SelfHpPct = SupportController.Unknown;
        public bool Casting;              // me.IsCasting
        public bool InMission;            // MissionController.InMission — blitz is walking a mission floor
        public bool OwnerVisible;         // owner dynel found this tick
        public float OwnerDistance;       // metres to him while visible (0 when not)
    }
}