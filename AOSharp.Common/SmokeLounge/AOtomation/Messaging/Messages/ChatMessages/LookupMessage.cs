namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)ChatMessageType.LookupMessage)]
    public class LookupMessage : ChatMessageBody
    {
        #region Public Properties

        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.LookupMessage;
            }
        }

        #endregion

        [AoMember(0)]
        public uint Id { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string Name { get; set; }
    }
}