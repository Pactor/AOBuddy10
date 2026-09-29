// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharSecSpecAttackMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CharSecSpecAttackMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Names ported from OmniCell's AOtomation.Messaging. Old AOSharp names kept as [Obsolete] aliases.
    [AoContract((int)N3MessageType.CharSecSpecAttack)]
    public class CharSecSpecAttackMessage : N3Message
    {
        #region Constructors and Destructors

        public CharSecSpecAttackMessage()
        {
            this.N3MessageType = N3MessageType.CharSecSpecAttack;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public Identity Target { get; set; }

        /// <summary>
        /// The special attack, as its skill stat (147 FastAttack, 142 Brawl, 144 Dimach, 146 SneakAttack ...).
        /// </summary>
        [AoMember(1)]
        public int SpecialAttackSkill { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is SpecialAttackSkill.")]
        public Stat Stat { get => (Stat)this.SpecialAttackSkill; set => this.SpecialAttackSkill = (int)value; }

        #endregion
    }
}
