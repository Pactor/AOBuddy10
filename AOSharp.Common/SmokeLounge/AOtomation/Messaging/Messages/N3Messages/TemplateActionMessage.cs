// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TemplateActionMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TemplateActionMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.TemplateAction)]
    public class TemplateActionMessage : N3Message
    {
        #region Constructors and Destructors

        public TemplateActionMessage()
        {
            this.N3MessageType = N3MessageType.TemplateAction;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public int ItemLowId { get; set; }

        [AoMember(1)]
        public int ItemHighId { get; set; }

        [AoMember(2)]
        public int Quality { get; set; }

        /// <summary>The stack size (OmniCell: the stack size for overflow deliveries); 1 in every recorded copy.</summary>
        [AoMember(3)]
        public int Amount { get; set; }

        /// <summary>What happened to the template: 3 = used (Placement = the inventory slot consumed), 87 = delivered to the overflow window (recorded values).</summary>
        [AoMember(4)]
        public int Action { get; set; }

        [AoMember(5)]
        public Identity Placement { get; set; }

        /// <summary>Type half of the second identity: 50000 = used on a character, 0 = none, 51005 on some copies (not in OmniCell's page).</summary>
        [AoMember(6)]
        public int TargetType { get; set; }

        /// <summary>Instance half of the second identity (see TargetType).</summary>
        [AoMember(7)]
        public int TargetInstance { get; set; }

        #endregion
    }
}