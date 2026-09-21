namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)ChatMessageType.CharacterList)]
    public class ChatCharacterListMessage : ChatMessageBody
    {
        #region Public Properties

        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.CharacterList;
            }
        }

        #endregion

        [AoMember(0, SerializeSize = ArraySizeType.Int16)]
        public uint[] Ids { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string[] Names { get; set; }

        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public int[] Levels { get; set; }

        [AoMember(3, SerializeSize = ArraySizeType.Int16)]
        public bool[] Online { get; set; }

        public ChatCharacter[] Characters => ToCharacters();

        private ChatCharacter[] ToCharacters()
        {
            ChatCharacter[] characters = new ChatCharacter[Names.Length];

            for(int i = 0; i < Names.Length; i++)
            {
                characters[i] = new ChatCharacter
                {
                    Name = Names[i],
                    Id = Ids[i],
                    Level = Levels[i],
                    Online = Online[i]
                };
            }

            return characters;
        }
    }

    public class ChatCharacter
    {
        public string Name;
        public uint Id;
        public int Level;
        public bool Online;
    }
}