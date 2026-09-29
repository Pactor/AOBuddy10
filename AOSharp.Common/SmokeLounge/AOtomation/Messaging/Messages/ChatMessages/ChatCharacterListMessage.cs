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

        /// <summary>
        /// Per character, whether it is online, as a uint32 (OmniCell's chat server writer
        /// AccountCharacterList.cs writes WriteUInt32 per character). AOSharp read it as one byte each.
        /// </summary>
        [AoMember(3, SerializeSize = ArraySizeType.Int16)]
        public int[] OnlineStatus { get; set; }

        /// <summary>Old bool view of <see cref="OnlineStatus"/> (non-zero = online).</summary>
        [System.Obsolete("Wire field is OnlineStatus (uint32 per character).")]
        public bool[] Online
        {
            get => this.OnlineStatus == null ? null : System.Array.ConvertAll(this.OnlineStatus, v => v != 0);
            set => this.OnlineStatus = value == null ? null : System.Array.ConvertAll(value, v => v ? 1 : 0);
        }

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