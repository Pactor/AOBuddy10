// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayfieldAnarchyFMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PlayfieldAnarchyFMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.PlayfieldAnarchyF)]
    public class PlayfieldAnarchyFMessage : N3Message
    {
        #region Constructors and Destructors

        public PlayfieldAnarchyFMessage()
        {
            this.N3MessageType = N3MessageType.PlayfieldAnarchyF;
            this.Unknown = 0x00;
            this.Unknown1 = 0x00000004;
            this.Unknown2 = 0x61;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public int Unknown1 { get; set; }

        [AoMember(1)]
        public Vector3 CharacterCoordinates { get; set; }

        [AoMember(2)]
        public byte Unknown2 { get; set; }

        [AoMember(3)]
        public Identity PlayfieldId1 { get; set; }

        [AoMember(4)]
        public int Unknown3 { get; set; }

        [AoMember(5)]
        public int SG { get; set; }

        [AoMember(6)]
        public Identity ProxyId { get; set; }

        [AoFlags("flags")]
        [AoMember(7)]
        public int UnknownIdType { get; set; }

        [AoMember(8)]
        public int UnknownIdInstance { get; set; }

        [AoMember(9)]
        public int Unknown5 { get; set; }

        [AoMember(10)]
        public int Unknown6 { get; set; }

        [AoUsesFlags("flags", typeof(UnknownStruct1), FlagsCriteria.EqualsToAny, new[] { 0xC77B })]
        [AoMember(11)]
        public UnknownStruct1 Unknown7 { get; set; }

        [AoUsesFlags("flags", typeof(PlayfieldDynelInfo[]), FlagsCriteria.EqualsToAny, new[] { 0xC77B, 0xC77D })]
        [AoMember(12, SerializeSize=ArraySizeType.Int32)]
        public PlayfieldDynelInfo[] Dynels { get; set; }

        #endregion

        public class UnknownStruct1
        {
            [AoMember(0)]
            public int Unknown1 { get; set; }
            [AoMember(1)]
            public Identity Unknown2 { get; set; }
            [AoMember(2)]
            public int Unknown3 { get; set; }
            [AoMember(3)]
            public Vector3 Unknown4 { get; set; }
            [AoMember(4)]
            public int Unknown5 { get; set; }
            [AoMember(5)]
            public int Unknown6 { get; set; }
            [AoMember(6)]
            public float Unknown7 { get; set; }
            [AoMember(7)]
            public int Unknown8 { get; set; }
            [AoMember(8)]
            public int Unknown9 { get; set; }
        }

        public class PlayfieldDynelInfo
        {
            [AoMember(0)]
            public IdentityType IdentityType { get; set; }
            [AoMember(1)]
            public int Unknown1 { get; set; }
            [AoMember(2)]
            public int Unknown2 { get; set; }
            [AoMember(3)]
            public int Unknown3 { get; set; }
            [AoMember(4)]
            public int Instance { get; set; }
        }
    }
}