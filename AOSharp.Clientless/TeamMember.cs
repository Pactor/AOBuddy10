using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Clientless
{
    public class TeamMember
    {
        public Identity Identity;
        public int Level;
        public string Name;

        /// <summary>Profession id from TeamMemberMessage (8 = Bureaucrat, 14 = Keeper, ...).</summary>
        public short Profession;

        /// <summary>Raid group index, or -1 when the team is not part of a raid.</summary>
        public int RaidGroup = -1;

        // The team window's live vitals, from TeamMemberInfoMessage. Unlike a dynel's stats these
        // keep arriving while the member is out of render range - they stop only when he leaves the
        // playfield - so they are the reliable way to know how the owner is actually doing.
        // -1 means "not reported yet", so a caller can tell that from a genuine zero.
        public int CurrentHealth = -1;
        public int MaxHealth = -1;
        public int CurrentNano = -1;
        public int MaxNano = -1;

        /// <summary>Seconds-resolution timestamp of the last vitals update, or default if never.</summary>
        public DateTime VitalsUpdated;

        public bool HasVitals => CurrentHealth >= 0 && MaxHealth > 0;

        /// <summary>Health as a percentage, or -1 when the server has not reported it yet.</summary>
        public int HealthPercent => HasVitals ? (int)(100L * CurrentHealth / MaxHealth) : -1;

        /// <summary>Nano as a percentage, or -1 when the server has not reported it yet.</summary>
        public int NanoPercent => (CurrentNano >= 0 && MaxNano > 0) ? (int)(100L * CurrentNano / MaxNano) : -1;
    }
}
