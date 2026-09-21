namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)ChatMessageType.Ping)]
    public class ChatPingMessage : ChatMessageBody
    {
        #region Public Properties

        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.Ping;
            }
        }

        #endregion

        public ChatPingMessage()
        {
            Data = new byte[] { 0, 1, 2 };
        }

        [AoMember(0)]
        public byte[] Data { get; set; }
    }
}