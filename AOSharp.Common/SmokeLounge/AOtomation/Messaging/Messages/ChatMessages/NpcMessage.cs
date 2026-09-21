namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

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

        [AoMember(0)]
        public short Unk1 { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string Text { get; set; }

        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public short Unk2 { get; set; }
    }
}
