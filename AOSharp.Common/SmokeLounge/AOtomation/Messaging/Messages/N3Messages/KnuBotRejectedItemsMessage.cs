// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KnuBotRejectedItemsMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the KnuBotRejectedItemsMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.KnubotRejectedItems)]
    public class KnuBotRejectedItemsMessage : N3Message
    {
        #region Constructors and Destructors

        public KnuBotRejectedItemsMessage()
        {
            this.N3MessageType = N3MessageType.KnubotRejectedItems;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The record's version. 2 in every captured copy.
        /// </summary>
        /// <remarks>
        /// Read at 0x10127DFB and compared against the class's own version
        /// before anything else; a mismatch drops the message.
        /// </remarks>
        [AoMember(0)]
        public short Version { get; set; }

        [AoMember(1)]
        public Identity Target { get; set; }

        /// <summary>
        /// The items the bot would not take.
        /// </summary>
        /// <remarks>
        /// Counted by a plain Int32 ahead of it, which nothing was reading -
        /// so the count was never consumed and every captured copy came out
        /// four bytes short (CashReturned read the count). All captured copies
        /// carry an empty list, so the entries themselves are still untested.
        /// </remarks>
        [AoMember(2, SerializeSize = ArraySizeType.Int32)]
        public KnuBotRejectedItem[] Items { get; set; }

        /// <summary>
        /// Credits handed back with the items.
        /// </summary>
        /// <remarks>
        /// The client adds it to the character's cash. The dispatcher reads it
        /// from the message's +0x30 at 0x10128833, and when it is above zero
        /// fetches stat 61 - cash - adds this to it at 0x10128851 and sets the
        /// stat back. So this is money returning to the player along with the
        /// items, which is what a refused trade gives back.
        /// </remarks>
        [AoMember(3)]
        public int CashReturned { get; set; }

        #endregion
    }
}