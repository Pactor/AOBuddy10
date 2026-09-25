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

        // THE bot clock (R2.1): the one monotonic time base every controller reads. Created in Main.Init.
        public readonly IClock Clock;

        // Cross-system status read-model (R2.2), refreshed by Main once per tick just before Walk.
        public readonly BotStatus Status = new BotStatus();

        // What the movement arbiter decided this frame (shown in the heartbeat). Written by
        // whichever mover ran; read only by the heartbeat log.
        public string WalkState = "";

        // Cached owner char id so the bot can still tell the owner things after he's zoned off.
        public int OwnerCharId;

        // Timestamped HP/nano of the owner and teammates (see VitalsTracker). Heal decisions read it.
        public VitalsTracker Vitals;

        // RUN SPEED, the game client's exact formula: velocity (u/s) = 5.5 + RunSpeed / 230, capped at 15.5
        // (RunSpeed = Stat 156). The stat is not always readable: after a mission floor-button ride it read
        // -1 for the rest of the session (log 2026-09-23 22:14:43), and falling back to Config.FollowSpeed
        // (19 u/s) there made the server reject every step and snap the bot back to where it started. So
        // keep the last good reading; before the first one, the 0-skill base speed, which is always legal.
        public int LastRunSpeed = -1;
        private bool _runRead;
        public float RunVelocity(AOSharp.Clientless.LocalPlayer me)
        {
            // A snare drives the stat negative (-289 from 23:00:47, 2026-09-23, still -289 after a relog): that
            // is a real reading, and ignoring it kept the walker at full speed and the server snapped him back
            // every 3 s for minutes. Only -1 means unreadable. Floor 1.5 u/s (the formula below 0 is unverified).
            if (me != null && me.TryGetStat(AOSharp.Common.GameData.Stat.RunSpeed, out int rs) && rs != -1) { LastRunSpeed = rs; _runRead = true; }
            if (!_runRead) return 5.5f;
            return Math.Max(1.5f, Math.Min(15.5f, 5.5f + LastRunSpeed / 230f));
        }

        // SWIM: patch 18.7 derives swim speed from the run-speed stat (the owner's factor: 50%). Same
        // unknown/snares handling as RunVelocity — a snare slows the swim the same way.
        public float SwimVelocity(AOSharp.Clientless.LocalPlayer me)
            => Math.Max(1.0f, RunVelocity(me) * Config.SwimSpeedFactor);

        private string _behavior = "";
        public string Behavior => _behavior;

        // Log a behaviour transition once, when it changes.
        public void SetBehavior(string b)
        {
            if (b != _behavior) { Log($"STATE {_behavior} -> {b}"); _behavior = b; }
        }

        public BotContext(BuddyConfig config, Action<string> log, IClock clock)
        {
            Config = config;
            Log = log;
            Clock = clock;
        }

        /// <summary>
        /// Tell the owner BY NAME (via the chat server), so it reaches him wherever he is, even out of
        /// view. The ONE helper for saying something to him — controllers take this as their tell
        /// delegate instead of each hand-rolling the try/SendPrivateMessage/catch (R0.6).
        /// </summary>
        public void TellOwner(string text)
        {
            try { AOSharp.Clientless.Client.Chat.SendPrivateMessage(Config.Owner, text); } catch { }
        }
    }
}
