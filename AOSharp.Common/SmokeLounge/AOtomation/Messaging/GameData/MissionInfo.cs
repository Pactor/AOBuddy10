// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MissionInfo.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the MissionInfo type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class MissionInfo
    {
        #region AoMember Properties

        [AoMember(0)]
        public Identity MissionIdentity { get; set; }

        [AoMember(1, FixedSizeLength = 16, IsFixedSize = true)]
        public byte[] UnkChunk1 { get; set; }

        [AoMember(2, SerializeSize=ArraySizeType.NullTerminated)]
        public string Title { get; set; }

        [AoMember(3, SerializeSize=ArraySizeType.Int32)]
        public string Description { get; set; }

        [AoMember(4)]
        public Identity TerminalIdentity { get; set; }

        [AoMember(5)]
        public int RewardDescriptorVersion { get; set; }

        [AoMember(6)]
        public int Credits { get; set; }

        [AoMember(7)]
        public int Unk1 { get; set; }

        [AoMember(8)]
        public int XpReward { get; set; }

        [AoMember(9, FixedSizeLength = 8, IsFixedSize = true)]
        public byte[] UnkChunk2 { get; set; }

        [AoMember(10, SerializeSize = ArraySizeType.X3F1)]
        public MissionItemReward[] MissionItemData { get; set; }

        [AoMember(11, FixedSizeLength = 44, IsFixedSize = true)]
        public byte[] UnkChunk3 { get; set; }

        [AoMember(12)]
        public int MissionIcon { get; set; }

        [AoMember(13, FixedSizeLength = 120, IsFixedSize = true)]
        public byte[] UnkChunk4 { get; set; }

        [AoMember(14)]
        public Identity Playfield{ get; set; }

        [AoMember(15, FixedSizeLength = 8, IsFixedSize = true)]
        public byte[] UnkChunk5 { get; set; }

        [AoMember(16)]
        public Vector3 Location { get; set; }

        [AoMember(17, FixedSizeLength = 61, IsFixedSize = true)]
        public byte[] UnkChunk6 { get; set; }

        #endregion
    }
}