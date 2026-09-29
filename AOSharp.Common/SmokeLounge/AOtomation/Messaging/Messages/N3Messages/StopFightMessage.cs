// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StopFightMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the StopFightMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Names ported from OmniCell's AOtomation.Messaging. Old AOSharp name kept as an [Obsolete] alias.
    [AoContract((int)N3MessageType.StopFight)]
    public class StopFightMessage : N3Message
    {
        #region Constructors and Destructors

        public StopFightMessage()
        {
            this.N3MessageType = N3MessageType.StopFight;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// OmniCell: StopFighting. The client normalises it and ignores it. The server sends 1 in every
        /// recorded copy, and OmniCell says to send 1 "because every captured copy does"; this SDK's
        /// constructor leaves it 0 (unchanged here, it only affects what the bot sends).
        /// </summary>
        [AoMember(0)]
        public int StopFighting { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is StopFighting.")]
        public int Unk { get => this.StopFighting; set => this.StopFighting = value; }

        #endregion
    }
}
