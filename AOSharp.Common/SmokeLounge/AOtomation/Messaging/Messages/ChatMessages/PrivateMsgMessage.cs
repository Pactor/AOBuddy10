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
    /// A tell (chat packet 30).
    /// </summary>
    /// <remarks>
    /// Layout from OmniCell's chat server writer (Server\ChatEngine\Packets\MsgPrivateGroup.cs, which
    /// writes packet 30): uint32 sender, Int16 string message, Int16-counted blob. AOSharp read the
    /// blob as a short length (Unk1) and one content byte (Unk2), which is right only for a 1-byte
    /// blob; the old names are kept as [Obsolete] aliases on the blob.
    /// </remarks>
    [AoContract((int)ChatMessageType.PrivateMessage)]
    public class PrivateMsgMessage : ChatMessageBody
    {
        public PrivateMsgMessage()
        {
            this.Blob = new byte[0];
        }

        #region Public Properties

        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.PrivateMessage;
            }
        }

        #endregion

        /// <summary>The sender (incoming) or the recipient (outgoing).</summary>
        [AoMember(0)]
        public uint Sender { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string Text { get; set; }

        /// <summary>The trailing Int16-counted blob. Its meaning for tells is not established.</summary>
        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public byte[] Blob { get; set; }

        #region Old AOSharp names (aliases, not on the wire)

        /// <summary>Old name for the blob's length. Setting it resizes <see cref="Blob"/> (new bytes are 0).</summary>
        [System.Obsolete("Wire field is Blob (Int16-counted bytes); this is its length.")]
        public short Unk1
        {
            get => (short)(this.Blob?.Length ?? 0);
            set { var b = this.Blob ?? new byte[0]; System.Array.Resize(ref b, value); this.Blob = b; }
        }

        /// <summary>Old name for the blob's first byte.</summary>
        [System.Obsolete("Wire field is Blob; this is its first byte.")]
        public byte Unk2
        {
            get => this.Blob != null && this.Blob.Length > 0 ? this.Blob[0] : (byte)0;
            set { if (this.Blob == null || this.Blob.Length == 0) this.Blob = new byte[1]; this.Blob[0] = value; }
        }

        #endregion
    }
}
