// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AttackInfoMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the AttackInfoMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Names ported from OmniCell's AOtomation.Messaging. Old AOSharp names kept as [Obsolete] aliases.
    [AoContract((int)N3MessageType.AttackInfo)]
    public class AttackInfoMessage : N3Message
    {
        #region Constructors and Destructors

        public AttackInfoMessage()
        {
            this.N3MessageType = N3MessageType.AttackInfo;
        }

        #endregion

        #region AoMember Properties

        /// <summary>The damage the blow did.</summary>
        [AoMember(0)]
        public int Damage { get; set; }

        /// <summary>The weapon's energy (ammo) after the blow; -1 = no ammo update (every recorded copy).</summary>
        [AoMember(1)]
        public int WeaponEnergy { get; set; }

        /// <summary>The equip slot of the weapon that struck (6 right hand, 8 left hand, 0/1/2 ...).</summary>
        [AoMember(2)]
        public int WeaponSlot { get; set; }

        /// <summary>The character that was hit. The attacker is the message Identity.</summary>
        [AoMember(3)]
        public Identity Target { get; set; }

        /// <summary>A visual effect id (0 or 4 on the wire).</summary>
        [AoMember(4)]
        public int VisualEffectId { get; set; }

        /// <summary>
        /// OmniCell calls this DamageType and its server always fills 3. On the wire it is 3, 4 or 2,
        /// which are this SDK's HitType Normal, Critical and Glancing, so the SDK's name and type stay.
        /// </summary>
        [AoMember(5)]
        public HitType HitType { get; set; }

        /// <summary>The instance of the weapon that struck (0 when none).</summary>
        [AoMember(6)]
        public int WeaponInstance { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is Damage.")]
        public int Amount { get => this.Damage; set => this.Damage = value; }

        [Obsolete("Wire field is WeaponEnergy (-1 = no ammo update).")]
        public int AmmoCount { get => this.WeaponEnergy; set => this.WeaponEnergy = value; }

        [Obsolete("Wire field is VisualEffectId.")]
        public int Unk1 { get => this.VisualEffectId; set => this.VisualEffectId = value; }

        #endregion
    }
}
