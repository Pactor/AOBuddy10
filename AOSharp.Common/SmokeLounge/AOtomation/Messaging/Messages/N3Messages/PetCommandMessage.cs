// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PetCommandMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PetCommandMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.PetCommand)]
    public class PetCommandMessage : N3Message
    {
        #region Constructors and Destructors

        public PetCommandMessage()
        {
            this.N3MessageType = N3MessageType.PetCommand;
            this.Pets = new PetBase[0];
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public int Unk1 { get; set; }

        [AoMember(1)]
        public PetCommand Command { get; set; }

        [AoMember(2)]
        public int Unk2 { get; set; }

        [AoMember(3, SerializeSize = ArraySizeType.X3F1)]
        public PetBase[] Pets { get; set; }

        [AoMember(4)]
        public int Unk3 { get; set; }

        [AoMember(5)]
        public int Unk4 { get; set; }

        #endregion
    }

    public class PetBase
    {
        [AoMember(0)]
        public Identity Identity { get; set; }

        public PetBase()
        {
        }

        public PetBase(Identity identity)
        {
            Identity = identity;
        }
    }
}