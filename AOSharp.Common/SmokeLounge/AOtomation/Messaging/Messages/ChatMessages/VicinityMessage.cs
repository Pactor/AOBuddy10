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
//   Defines the PrivateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Vicinity chat (chat packet 34).
    /// </summary>
    /// <remarks>
    /// Layout from OmniCell's chat server writer (Server\ChatEngine\Packets\MsgVicinity.cs): uint32
    /// sender, Int16 string message, then a blob written as UInt16 1 and one byte, the chatType
    /// (say/whisper/shout). The blob is modelled as Int16-counted bytes; AOSharp read it as a short
    /// (Unk1) and a byte (Unk2), kept as [Obsolete] aliases.
    /// </remarks>
    [AoContract((int)ChatMessageType.VicinityMessage)]
    public class VicinityMessage : ChatMessageBody
    {
        public VicinityMessage()
        {
            this.Blob = new byte[0];
        }

        #region Public Properties

        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.VicinityMessage;
            }
        }

        #endregion

        [AoMember(0)]
        public uint Sender { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string Text { get; set; }

        /// <summary>The trailing Int16-counted blob; OmniCell writes one byte, the chat type (say/whisper/shout).</summary>
        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public byte[] Blob { get; set; }

        /// <summary>The chat type byte (the blob's first byte), or 0 when the blob is empty.</summary>
        public byte ChatType => this.Blob != null && this.Blob.Length > 0 ? this.Blob[0] : (byte)0;

        #region Old AOSharp names (aliases, not on the wire)

        [System.Obsolete("Wire field is Blob (Int16-counted bytes); this is its length.")]
        public short Unk1
        {
            get => (short)(this.Blob?.Length ?? 0);
            set { var b = this.Blob ?? new byte[0]; System.Array.Resize(ref b, value); this.Blob = b; }
        }

        [System.Obsolete("Wire field is Blob; this is its first byte (the chat type).")]
        public byte Unk2
        {
            get => this.ChatType;
            set { if (this.Blob == null || this.Blob.Length == 0) this.Blob = new byte[1]; this.Blob[0] = value; }
        }

        #endregion
    }
}
