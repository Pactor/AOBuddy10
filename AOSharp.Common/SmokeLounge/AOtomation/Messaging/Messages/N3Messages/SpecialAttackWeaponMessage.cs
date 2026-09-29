// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpecialAttackWeaponMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SpecialAttackWeaponMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Layout and names ported from OmniCell's AOtomation.Messaging (verified against every recorded packet).
    // After the specials list the client reads CharacterStat 118, 119, 120, 149 and 51 into the five
    // trailing fields (OmniCell special-attack-weapon.html). AOSharp's names were shifted by one:
    // its Unknown1 was CC init and its NanoProwessInitiative was AggDef. Nothing in the SDK or plugin
    // read these fields, so they now carry OmniCell's names; Unknown1 is kept as an alias.
    [AoContract((int)N3MessageType.SpecialAttackWeapon)]
    public class SpecialAttackWeaponMessage : N3Message
    {
        #region Constructors and Destructors

        public SpecialAttackWeaponMessage()
        {
            this.N3MessageType = N3MessageType.SpecialAttackWeapon;
            this.CloseCombatInitiative = 0x00000007;
            this.DistanceWeaponInitiative = 0x00000007;
            this.PhysicalProwessInitiative = 0x00000007;
            this.NanoProwessInitiative = 0x0000000E;
            this.AggDef = 0x00000064;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The special attacks the character's weapons carry (template ids, selector, 4-char code).
        /// </summary>
        /// <remarks>
        /// Not a complete list of usable specials: for the bot it only ever held the carried items'
        /// innate BRAW/MAAT/DIIT, never FastAttack or SneakAttack.
        /// </remarks>
        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public SpecialAttackInfo[] Specials { get; set; }

        /// <summary>CharacterStat 118, closecombatinitiative.</summary>
        [AoMember(1)]
        public int CloseCombatInitiative { get; set; }

        /// <summary>CharacterStat 119, distanceweaponinitiative.</summary>
        [AoMember(2)]
        public int DistanceWeaponInitiative { get; set; }

        /// <summary>CharacterStat 120, physicalprowessinitiative.</summary>
        [AoMember(3)]
        public int PhysicalProwessInitiative { get; set; }

        /// <summary>CharacterStat 149, nanoprowessinitiative.</summary>
        [AoMember(4)]
        public int NanoProwessInitiative { get; set; }

        /// <summary>CharacterStat 51, aggdef (the aggressive/defensive slider). 100 on every bot copy.</summary>
        [AoMember(5)]
        public int AggDef { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is CloseCombatInitiative (stat 118).")]
        public int Unknown1 { get => this.CloseCombatInitiative; set => this.CloseCombatInitiative = value; }

        #endregion
    }
}
