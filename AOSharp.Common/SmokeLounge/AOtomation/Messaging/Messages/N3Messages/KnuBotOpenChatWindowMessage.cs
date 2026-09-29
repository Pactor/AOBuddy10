// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KnuBotOpenChatWindowMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the KnuBotOpenChatWindowMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.KnubotOpenChatWindow)]
    public class KnuBotOpenChatWindowMessage : N3Message
    {
        #region Constructors and Destructors

        public KnuBotOpenChatWindowMessage()
        {
            this.N3MessageType = N3MessageType.KnubotOpenChatWindow;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public short Version { get; set; }

        [AoMember(1)]
        public Identity Target { get; set; }

        /// <summary>
        /// A flag. 1 in 56 of the 175 captured copies.
        /// </summary>
        /// <remarks>
        /// An int32 on the wire and a bool in the client: the reader at
        /// 0x10128375 compares it against 1 and stores the result as a byte, so
        /// anything that is not 1 is false. What it turns on is not established.
        /// </remarks>
        [AoMember(2)]
        public int Unknown2 { get; set; }

        /// <summary>
        /// The second flag, and zero in every captured copy.
        /// </summary>
        [AoMember(3)]
        public int Unknown3 { get; set; }

        #endregion
    }
}