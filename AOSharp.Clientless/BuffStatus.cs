using AOSharp.Common.GameData;
using AOSharp.Common.SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;

namespace AOSharp.Clientless
{
    public static class BuffStatus
    {
        public static EventHandler<BuffChangedArgs> BuffChanged;

        internal static void OnBuffMessage(SimpleChar simpleChar, int buffId)
        {
            BuffState buffStatus = simpleChar.Buffs.Find(buffId, out Buff buff) && buff.Cooldown.RemainingTime > 1f ? BuffState.Refreshed : BuffState.Removed;
            BuffChangedArgs buffChangedArgs = new BuffChangedArgs(simpleChar.Identity, buffStatus, buffId);
            BuffChanged?.Invoke(null, buffChangedArgs);
        }
    }

    public class BuffChangedArgs : EventArgs
    {
        public Identity Identity { get; }
        public int Id { get; }
        public BuffState Status { get; set; }

        public BuffChangedArgs(Identity character, BuffState status, int id)
        {
            Identity = character;
            Status = status;
            Id = id;
        }
    }

    public enum BuffState
    {
        Removed,
        Refreshed,
    }
}