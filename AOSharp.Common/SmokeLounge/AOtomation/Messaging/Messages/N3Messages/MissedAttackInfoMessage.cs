// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MissedAttackInfoMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the MissedAttackInfoMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
    using System;

    // Layout, names and doc comments ported from OmniCell's AOtomation.Messaging (verified against
    // every recorded packet). Old AOSharp names are kept as [Obsolete] aliases where they differed.
    [AoContract((int)N3MessageType.MissedAttackInfo)]
    public class MissedAttackInfoMessage : N3Message
    {
        public MissedAttackInfoMessage()
        {
            this.N3MessageType = N3MessageType.MissedAttackInfo;
        }

        /// <summary>The weapon's energy (ammo) after the attack; -1 means no ammo update.</summary>
        [AoMember(1)]
        public int WeaponEnergy { get; set; }

        /// <summary>The equip slot of the weapon that swung (e.g. 6 right hand, 8 left hand, 0/1 unarmed).</summary>
        [AoMember(2)]
        public int WeaponSlot { get; set; }

        [AoMember(3)]
        public Identity Attacker { get; set; }

        /// <summary>The character that was missed (AOSharp: Defender).</summary>
        [AoMember(4)]
        public Identity Target { get; set; }

        /// <summary>The skill stat of the attack (0 for a normal swing, 142 Brawl, 147 FastAttack ...).</summary>
        [AoMember(5)]
        public int AttackSkillStat { get; set; }

        #region Old AOSharp names (aliases, not on the wire)

        /// <summary>
        /// AOSharp called the first int Unknown1: it is <see cref="WeaponEnergy"/> (-1 = no ammo update).
        /// AOSharp's own "WeaponEnergy" was the second int, <see cref="WeaponSlot"/>; nothing read it, so
        /// the name now carries its real meaning.
        /// </summary>
        [Obsolete("Wire field is WeaponEnergy (-1 = no ammo update).")]
        public int Unknown1 { get => this.WeaponEnergy; set => this.WeaponEnergy = value; }

        [Obsolete("Wire field is Target (the character that was missed).")]
        public Identity Defender { get => this.Target; set => this.Target = value; }

        [Obsolete("Wire field is AttackSkillStat (the skill stat of the attack, e.g. 142 Brawl).")]
        public int Unknown3 { get => this.AttackSkillStat; set => this.AttackSkillStat = value; }

        #endregion
    }
}
