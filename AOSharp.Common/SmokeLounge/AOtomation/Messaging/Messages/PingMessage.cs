// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PingMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PingMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)PacketType.PingMessage)]
    public class PingMessage : MessageBody
    {
        #region Public Properties

        public override PacketType PacketType
        {
            get
            {
                return PacketType.PingMessage;
            }
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public PingMessageType PingMessageType { get; set; }

        [AoMember(1)]
        public int Unk1 { get; set; }

        [AoMember(2)]
        public uint ServerTime { get; set; }

        [AoMember(3)]
        public uint UpTime1 { get; set; }

        [AoMember(4)]
        public uint UpTime2 { get; set; }

        [AoMember(5)]
        public uint Unk2 { get; set; }

        #endregion
    }
}