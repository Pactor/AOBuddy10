// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AbsorbMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the AbsorbMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Names ported from OmniCell's AOtomation.Messaging. Old AOSharp names kept as [Obsolete] aliases.
    [AoContract((int)N3MessageType.Absorb)]
    public class AbsorbMessage : N3Message
    {
        #region Constructors and Destructors

        public AbsorbMessage()
        {
            this.N3MessageType = N3MessageType.Absorb;
        }

        #endregion

        #region AoMember Properties

        /// <summary>Damage the shield/absorb soaked.</summary>
        [AoMember(0)]
        public int DamageAbsorbed { get; set; }

        /// <summary>The damage type as its armour-class stat id (e.g. 91 MeleeAC).</summary>
        [AoMember(1)]
        public int DamageTypeStat { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is DamageAbsorbed.")]
        public int Amount { get => this.DamageAbsorbed; set => this.DamageAbsorbed = value; }

        [Obsolete("Wire field is DamageTypeStat.")]
        public Stat DmgType { get => (Stat)this.DamageTypeStat; set => this.DamageTypeStat = (int)value; }

        #endregion
    }
}
