// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TradeMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TradeMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Layout and names ported from OmniCell's AOtomation.Messaging: after Version and Action come two
    // Identities, Target and Container (OmniCell trade.html). AOSharp read them as four ints Param1-4;
    // those are kept as [Obsolete] aliases on the identity halves (same bytes, same values).
    [AoContract((int)N3MessageType.Trade)]
    public class TradeMessage : N3Message
    {
        #region Constructors and Destructors

        public TradeMessage()
        {
            this.N3MessageType = N3MessageType.Trade;
        }

        #endregion

        #region AoMember Properties

        /// <summary>The trade protocol version; always 2, and the client rejects anything else.</summary>
        [AoMember(0)]
        public int Version { get; set; }

        [AoMember(1)]
        public TradeAction Action { get; set; }

        /// <summary>The trade partner / the item the action is about (AOSharp Param1:Param2).</summary>
        [AoMember(2)]
        public Identity Target { get; set; }

        /// <summary>The container side of the action (AOSharp Param3:Param4).</summary>
        [AoMember(3)]
        public Identity Container { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is Target (Identity); this is its type half.")]
        public int Param1 { get => (int)this.Target.Type; set => this.Target = new Identity((IdentityType)value, this.Target.Instance); }

        [Obsolete("Wire field is Target (Identity); this is its instance half.")]
        public int Param2 { get => this.Target.Instance; set => this.Target = new Identity(this.Target.Type, value); }

        [Obsolete("Wire field is Container (Identity); this is its type half.")]
        public int Param3 { get => (int)this.Container.Type; set => this.Container = new Identity((IdentityType)value, this.Container.Instance); }

        [Obsolete("Wire field is Container (Identity); this is its instance half.")]
        public int Param4 { get => this.Container.Instance; set => this.Container = new Identity(this.Container.Type, value); }

        #endregion
    }
}
