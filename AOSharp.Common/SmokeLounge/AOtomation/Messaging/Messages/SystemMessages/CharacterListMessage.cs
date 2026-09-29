// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterListMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CharacterListMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.SystemMessages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)SystemMessageType.CharacterList)]
    public class CharacterListMessage : SystemMessage
    {
        #region Constructors and Destructors

        public CharacterListMessage()
        {
            this.SystemMessageType = SystemMessageType.CharacterList;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0, SerializeSize = ArraySizeType.Int32)]
        public LoginCharacterInfo[] Characters { get; set; }

        [AoMember(1)]
        public int AllowedCharacters { get; set; }

        [AoMember(2)]
        public int Expansions { get; set; }

        /// <summary>
        /// A trailing int32, 0 in every captured copy.
        /// </summary>
        /// <remarks>
        /// Added 2026-09-29 from OmniCell (Messages\SystemMessages\CharacterListMessage.cs:57-66); the AOSharp
        /// original left these 4 bytes unread. The client does not require it. Meaning unknown.
        /// </remarks>
        [AoMember(3)]
        public int Unknown1 { get; set; }

        #endregion
    }
}