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
            // Must never be left null: the generated string serializer dereferences it and every pet
            // command carries an empty one, which is what made the first pet command of a session throw
            // an NRE inside Client.Send. Empty serialises to the same four zero bytes the field used to
            // be read as, verified by round-tripping a captured client packet.
            this.CommandText = string.Empty;
        }

        #endregion

        #region AoMember Properties

        // Wire-verified (captures/mp_203534_s37.csv, sniffs/20260913-154958_s18.csv):
        //   Scope=0 Command=7  CommandParameter=0 Pets=[1] IsPetType15=0 CommandText=   (attack, one pet)
        //   Scope=1 Command=10 CommandParameter=0 Pets=[0]                              (terminate, all pets)
        // Scope 0 means "the pets listed", scope 1 means "all of them" and the list is empty.
        [AoMember(0)]
        public int Scope { get; set; }

        [AoMember(1)]
        public PetCommand Command { get; set; }

        [AoMember(2)]
        public int CommandParameter { get; set; }

        [AoMember(3, SerializeSize = ArraySizeType.X3F1)]
        public PetBase[] Pets { get; set; }

        [AoMember(4)]
        public int IsPetType15 { get; set; }

        // Trailing command text, int32-counted. Round-tripping a captured client pet command
        // (sniffs/20260913-154958_s18.csv seq 50) reproduces that packet byte for byte, so this is the
        // real shape of the field rather than the plain int it used to be read as.
        [AoMember(5, SerializeSize = ArraySizeType.Int32)]
        public string CommandText { get; set; }

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