namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Anonymous vicinity text (chat packet 35), e.g. NPC speech.
    /// </summary>
    /// <remarks>
    /// Layout from OmniCell's chat server writer (Server\ChatEngine\Packets\MsgAnonymousVicinity.cs):
    /// three Int16 strings - the sender's name, the message and a blob. AOSharp's shape (short, string,
    /// short) read the sender-name length as Unk1 and then misread the rest.
    /// </remarks>
    [AoContract((int)ChatMessageType.NpcMessage)]
    public class NpcMessage : ChatMessageBody
    {
        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.NpcMessage;
            }
        }

        /// <summary>Who is speaking (a name, not an id).</summary>
        [AoMember(0, SerializeSize = ArraySizeType.Int16)]
        public string SenderName { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string Text { get; set; }

        /// <summary>The trailing blob string.</summary>
        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public string Blob { get; set; }

        #region Old AOSharp names (aliases, not on the wire)

        [System.Obsolete("Wire field is SenderName; this is its length.")]
        public short Unk1 { get => (short)(this.SenderName?.Length ?? 0); set { } }

        [System.Obsolete("Wire field is Blob; this is its length.")]
        public short Unk2 { get => (short)(this.Blob?.Length ?? 0); set { } }

        #endregion
    }
}
