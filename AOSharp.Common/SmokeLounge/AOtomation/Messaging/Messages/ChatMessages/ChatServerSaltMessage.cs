namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)ChatMessageType.ServerSalt)]
    public class ChatServerSaltMessage : ChatMessageBody
    {
        #region Public Properties

        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.ServerSalt;
            }
        }

        #endregion

        [AoMember(0, SerializeSize = ArraySizeType.Int16)]
        public byte[] ServerSalt { get; set; }
    }
}