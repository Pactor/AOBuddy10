namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)ChatMessageType.SelectCharacter)]
    public class ChatSelectCharacterMessage : ChatMessageBody
    {
        #region Public Properties

        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.SelectCharacter;
            }
        }

        #endregion

        [AoMember(0)]
        public uint CharacterId { get; set; }
    }
}