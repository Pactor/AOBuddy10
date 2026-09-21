namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)ChatMessageType.FriendStatus)]
    public class FriendStatusMessage : ChatMessageBody
    {
        #region Public Properties

        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.FriendStatus;
            }
        }

        #endregion

        [AoMember(0)]
        public uint Id { get; set; }

        [AoMember(1)]
        public bool Online { get; set; }
    }
}