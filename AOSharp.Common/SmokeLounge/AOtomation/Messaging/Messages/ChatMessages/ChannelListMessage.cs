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

        // Layout from OmniCell's chat server writer (Server\ChatEngine\Packets\ChannelJoin.cs): channel
        // type byte + uint32 channel id (the 5-byte group id), Int16 name, uint32 flags, other data.

        /// <summary>The channel type byte of the 5-byte group id.</summary>
        [AoMember(0)]
        public byte ChannelType { get; set; }

        [AoMember(1)]
        public int ChannelId { get; set; }

        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public string ChannelName { get; set; }

        /// <summary>The channel flags (uint32; AOSharp read it as two shorts, Unk2 high and Unk3 low).</summary>
        [AoMember(3)]
        public int Flags { get; set; }

        /// <summary>
        /// The Int16 length of the trailing "other data"; its contents are not read (the audit found no
        /// capture to settle them).
        /// </summary>
        [AoMember(4)]
        public short Unk4 { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [System.Obsolete("Wire field is ChannelType.")]
        public byte Unk1 { get => this.ChannelType; set => this.ChannelType = value; }

        [System.Obsolete("Wire field is Flags (uint32); this is its high half.")]
        public short Unk2 { get => (short)(this.Flags >> 16); set => this.Flags = (int)((this.Flags & 0x0000FFFF) | (value << 16)); }

        [System.Obsolete("Wire field is Flags (uint32); this is its low half.")]
        public short Unk3 { get => (short)this.Flags; set => this.Flags = (int)((this.Flags & 0xFFFF0000) | (ushort)value); }

        #endregion
    }
}