using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOSharp.Clientless
{
    public class NpcChar : SimpleChar
    {
        public Identity? Owner;

        /// <summary>
        /// What this pet is for, as the server states it in its own character update. Zero for an
        /// ordinary NPC; only meaningful together with <see cref="Owner"/>. See
        /// <see cref="AOSharp.Common.GameData.PetType"/> for the values and where they were verified.
        /// </summary>
        public short PetTypeId;

        /// <summary>The NPC family from the same block; it tracks the role too (95-98 for pets).</summary>
        public short NpcFamily;

        /// <summary><see cref="PetTypeId"/> as the enum. An unknown value keeps its raw number.</summary>
        public PetType Role => (PetType)PetTypeId;

        /// <summary>One of the master's combat pets (attack, heal or mezz) rather than a vanity one.</summary>
        public bool IsCombatPet => Role == PetType.Attack || Role == PetType.Heal || Role == PetType.Support;

        public NpcChar(SimpleCharFullUpdateMessage simpleCharMsg) : base(simpleCharMsg)
        {
            Owner = simpleCharMsg.Owner;
            PetTypeId = simpleCharMsg.PetType;
            NpcFamily = simpleCharMsg.NpcFamily;
        }
    }
}
