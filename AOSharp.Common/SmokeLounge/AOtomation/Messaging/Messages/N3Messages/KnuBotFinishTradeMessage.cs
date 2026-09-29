// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KnuBotFinishTradeMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the KnuBotFinishTradeMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.KnubotFinishTrade)]
    public class KnuBotFinishTradeMessage : N3Message
    {
        #region Constructors and Destructors

        public KnuBotFinishTradeMessage()
        {
            this.N3MessageType = N3MessageType.KnubotFinishTrade;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public short ProtocolVersion { get; set; }

        [AoMember(1)]
        public Identity Target { get; set; }

        /// <summary>Non-zero when the trade was declined (OmniCell: Declined).</summary>
        [AoMember(2)]
        public int Declined { get; set; }

        /// <summary>Credits in the trade (OmniCell: Credits).</summary>
        [AoMember(3)]
        public int Credits { get; set; }

        [System.Obsolete("Wire field is Declined.")]
        public int Decline { get => this.Declined; set => this.Declined = value; }

        [System.Obsolete("Wire field is Credits.")]
        public int Amount { get => this.Credits; set => this.Credits = value; }
        #endregion
    }
}