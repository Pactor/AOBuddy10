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
//   Defines the PrivateGroupMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)ChatMessageType.PrivateGroupMessage)]
    public class PrivateGroupMessage : ChatMessageBody
    {
        #region Public Properties

        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.PrivateGroupMessage;
            }
        }

        #endregion

        [AoMember(0)]
        public uint ChannelId { get; set; }

        [AoMember(1)]
        public uint Sender { get; set; }

        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public string Text { get; set; }

        [AoMember(3)]
        public byte Unk1 { get; set; }

        [AoMember(4)]
        public byte Unk2 { get; set; }
    }
}