// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShieldAttackMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ShieldAttackMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Names ported from OmniCell's AOtomation.Messaging. Old AOSharp names kept as [Obsolete] aliases.
    [AoContract((int)N3MessageType.ShieldAttack)]
    public class ShieldAttackMessage : N3Message
    {
        #region Constructors and Destructors

        public ShieldAttackMessage()
        {
            this.N3MessageType = N3MessageType.ShieldAttack;
        }

        #endregion

        #region AoMember Properties

        /// <summary>The damage-shield damage taken by the message Identity.</summary>
        [AoMember(0)]
        public int Damage { get; set; }

        /// <summary>Whose damage shield it was (the mob carrying it).</summary>
        [AoMember(1)]
        public Identity ShieldOwner { get; set; }

        /// <summary>A visual effect id; not a stat (0 in every recorded copy).</summary>
        [AoMember(2)]
        public int VisualEffectId { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is Damage.")]
        public int Amount { get => this.Damage; set => this.Damage = value; }

        [Obsolete("Wire field is ShieldOwner (the shield's owner, not a target).")]
        public Identity Target { get => this.ShieldOwner; set => this.ShieldOwner = value; }

        [Obsolete("Wire field is VisualEffectId, not a stat.")]
        public Stat Stat { get => (Stat)this.VisualEffectId; set => this.VisualEffectId = (int)value; }

        #endregion
    }
}
