// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpecialAttackInfoMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SpecialAttackInfoMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Names ported from OmniCell's AOtomation.Messaging (N3Messages\SpecialAttackInfo.cs). Old AOSharp
    // names kept as [Obsolete] aliases.
    [AoContract((int)N3MessageType.SpecialAttackInfo)]
    public class SpecialAttackInfoMessage : N3Message
    {
        #region Constructors and Destructors

        public SpecialAttackInfoMessage()
        {
            this.N3MessageType = N3MessageType.SpecialAttackInfo;
        }

        #endregion

        #region AoMember Properties

        /// <summary>The equip slot of the weapon the special used (6 = right hand, 0 ...).</summary>
        [AoMember(0)]
        public int WeaponSlot { get; set; }

        /// <summary>The damage the special did.</summary>
        [AoMember(1)]
        public int Damage { get; set; }

        /// <summary>The weapon's energy (ammo) after the special; -1 = no ammo update.</summary>
        [AoMember(2)]
        public int WeaponEnergy { get; set; }

        /// <summary>The character it landed on. The attacker is the message Identity.</summary>
        [AoMember(3)]
        public Identity Target { get; set; }

        /// <summary>Which special landed, as its skill stat (147 FastAttack, 142 Brawl, 144 Dimach, 146 SneakAttack).</summary>
        [AoMember(4)]
        public int SpecialAttackSkill { get; set; }

        /// <summary>A visual effect id (0 or 4 on the wire).</summary>
        [AoMember(5)]
        public int VisualEffectId { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is WeaponSlot.")]
        public EquipSlot EquipSlot { get => (EquipSlot)this.WeaponSlot; set => this.WeaponSlot = (int)value; }

        [Obsolete("Wire field is Damage.")]
        public int Amount { get => this.Damage; set => this.Damage = value; }

        [Obsolete("Wire field is WeaponEnergy (-1 = no ammo update).")]
        public int AmmoCount { get => this.WeaponEnergy; set => this.WeaponEnergy = value; }

        [Obsolete("Wire field is SpecialAttackSkill.")]
        public Stat Stat { get => (Stat)this.SpecialAttackSkill; set => this.SpecialAttackSkill = (int)value; }

        [Obsolete("Wire field is VisualEffectId.")]
        public int Unk1 { get => this.VisualEffectId; set => this.VisualEffectId = value; }

        #endregion
    }
}
