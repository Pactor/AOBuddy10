namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)ChatMessageType.LoginError)]
    public class ChatLoginErrorMessage : ChatMessageBody
    {
        #region Public Properties

        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.LoginError;
            }
        }

        #endregion

        [AoMember(0, SerializeSize = ArraySizeType.Int16)]
        public string Message { get; set; }
    }
}