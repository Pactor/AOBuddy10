// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KnuBotTradeMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the KnuBotTradeMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.KnubotTrade)]
    public class KnuBotTradeMessage : N3Message
    {
        #region Constructors and Destructors

        public KnuBotTradeMessage()
        {
            this.N3MessageType = N3MessageType.KnubotTrade;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public short Version { get; set; }

        [AoMember(1)]
        public Identity Target { get; set; }

        [AoMember(2)]
        public KnuBotTradeAction Action { get; set; }

        /// <summary>
        /// An Identity both senders zero.
        /// </summary>
        /// <remarks>
        /// This was two int32s (Unknown3, Unknown4), which is the same eight bytes and the wrong
        /// shape: the reader takes it with the client's Identity reader at
        /// 0x10128D06, in the call before the one that takes the item. Both
        /// exported senders pass the address of two words they have just
        /// cleared, so the client cannot put anything else in it.
        /// </remarks>
        [AoMember(3)]
        public Identity Unknown1 { get; set; }

        /// <summary>
        /// The item being added or removed.
        /// </summary>
        /// <remarks>
        /// The third argument of both exported senders.
        /// </remarks>
        [AoMember(4)]
        public Identity Item { get; set; }

        [System.Obsolete("Wire field is Unknown1 (an Identity); this is its type half.")]
        public int Unknown3 { get => (int)this.Unknown1.Type; set => this.Unknown1 = new Identity((IdentityType)value, this.Unknown1.Instance); }

        [System.Obsolete("Wire field is Unknown1 (an Identity); this is its instance half.")]
        public int Unknown4 { get => this.Unknown1.Instance; set => this.Unknown1 = new Identity(this.Unknown1.Type, value); }

        [System.Obsolete("Wire field is Item.")]
        public Identity Slot { get => this.Item; set => this.Item = value; }

        #endregion
    }
}