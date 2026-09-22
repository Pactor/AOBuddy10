using System;

namespace AOBuddy
{
    /// <summary>
    /// The small shared blackboard every system reads: config + logging + a couple of
    /// cross-system status values (the current walk-state string for the heartbeat, and the
    /// behaviour label). It carries NO behaviour of its own — it exists so the isolated
    /// controllers don't each need their own copy of the config or a back-reference to Main.
    /// </summary>
    public class BotContext
    {
        public BuddyConfig Config;
        public Action<string> Log;

        // What the movement arbiter decided this frame (shown in the heartbeat). Written by
        // whichever mover ran; read only by the heartbeat log.
        public string WalkState = "";

        // Cached owner char id so the bot can still tell the owner things after he's zoned off.
        public int OwnerCharId;

        // Timestamped HP/nano of the owner and teammates (see VitalsTracker). Heal decisions read it.
        public VitalsTracker Vitals;

        private string _behavior = "";
        public string Behavior => _behavior;

        // Log a behaviour transition once, when it changes.
        public void SetBehavior(string b)
        {
            if (b != _behavior) { Log($"STATE {_behavior} -> {b}"); _behavior = b; }
        }

        public BotContext(BuddyConfig config, Action<string> log)
        {
            Config = config;
            Log = log;
        }
    }
}
