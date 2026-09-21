// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrivateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ChannelListMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)ChatMessageType.ChannelList)]
    public class ChannelListMessage : ChatMessageBody
    {
        #region Public Properties

        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.ChannelList;
            }
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public byte Unk1 { get; set; }

        [AoMember(1)]
        public int ChannelId { get; set; }

        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public string ChannelName { get; set; }

        [AoMember(3)]
        public short Unk2 { get; set; }

        [AoMember(4)]
        public short Unk3 { get; set; }

        [AoMember(5)]
        public short Unk4 { get; set; }

        #endregion
    }
}