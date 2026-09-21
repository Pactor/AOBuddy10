namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)ChatMessageType.LoginRequest)]
    public class ChatLoginRequestMessage : ChatMessageBody
    {
        #region Public Properties

        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.LoginRequest;
            }
        }

        #endregion

        [AoMember(0)]
        public int Unk { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string Username { get; set; }


        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public string Credentials { get; set; }
    }
}