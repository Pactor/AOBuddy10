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
//   Defines the GroupMsgMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)ChatMessageType.GroupMessage)]
    public class GroupMsgMessage : ChatMessageBody
    {
        #region Public Properties

        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.GroupMessage;
            }
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public GroupMessageType MessageType { get; set; }

        [AoMember(1)]
        public int ChannelId { get; set; }

        [AoMember(3)]
        public uint SenderId { get; set; }

        [AoMember(4, SerializeSize = ArraySizeType.Int16)]
        public string Text { get; set; }

        #endregion
    }
}