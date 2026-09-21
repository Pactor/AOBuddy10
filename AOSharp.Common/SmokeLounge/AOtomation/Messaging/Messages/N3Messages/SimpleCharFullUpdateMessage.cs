// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleCharFullUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SimpleCharFullUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
    using System;
    using System.Collections.Generic;

    [AoContract((int)N3MessageType.SimpleCharFullUpdate)]
    public class SimpleCharFullUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public SimpleCharFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.SimpleCharFullUpdate;
            this.Unknown = 0x00;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public byte Version { get; set; }

        [AoMember(1)]
        public SimpleCharFullUpdateFlags Flags { get; set; }

        [AoMember(2)]
        public int? PlayfieldId { get; set; }

        [AoMember(3)]
        public Identity? FightingTarget { get; set; }

        [AoMember(4)]
        public Vector3 Position { get; set; }

        [AoMember(5)]
        public Quaternion Heading { get; set; }

        [AoMember(6)]
        public Appearance Appearance { get; set; }

        [AoMember(7)]
        public string Name { get; set; }

        [AoMember(8)]
        public CharacterFlags CharacterFlags { get; set; }

        [AoMember(9)]
        public short AccountFlags { get; set; }

        [AoMember(10)]
        public short Expansions { get; set; }

        [AoMember(11)]
        public SimpleCharInfo CharacterInfo { get; set; }

        [AoMember(12)]
        public short Level { get; set; }

        [AoMember(13)]
        public int Health { get; set; }

        [AoMember(14)]
        public int HealthDamage { get; set; }

        [AoMember(15)]
        public uint MonsterData { get; set; }

        [AoMember(16)]
        public short MonsterScale { get; set; }

        [AoMember(17)]
        public short VisualFlags { get; set; }

        [AoMember(18)]
        public byte VisibleTitle { get; set; }

        [AoMember(19, SerializeSize = ArraySizeType.Int32)]
        public byte[] ScfuUnk1 { get; set; }

        [AoMember(20)]
        public int? HeadMesh { get; set; }

        [AoMember(21)]
        public short RunSpeedBase { get; set; }

        [AoMember(22, SerializeSize = ArraySizeType.X3F1)]
        public SimpleCharInfo.ActiveNano[] ActiveNanos { get; set; }

        [AoMember(23, SerializeSize = ArraySizeType.X3F1)]
        public Texture[] Textures { get; set; }

        [AoMember(24, SerializeSize = ArraySizeType.X3F1)]
        public Mesh[] Meshes { get; set; }

        [AoMember(25)]
        public ScfuFlags2 Flags2 { get; set; }

        [AoMember(26)]
        public SimpleCharInfo.SpecialAttackData[] SpecialAttacks { get; set; }

        [AoMember(27)]
        public byte ScfuUnk2 { get; set; }

        [AoMember(28)]
        public float ScfuUnk3 { get; set; }

        [AoMember(29)]
        public byte ScfuUnk4 { get; set; }

        [AoMember(30)]
        public SimpleCharInfo.TextureOverride[] TextureOverrides { get; set; }

        [AoMember(31)]
        public List<Vector3> Waypoints { get; set; }

        [AoMember(32)]
        public Identity? Owner { get; set; }

        [AoMember(33)]
        public byte ScfuTowerUnk { get; set; }

        #endregion

        public class MovementInfo
        {
            [AoMember(0)]
            public float Unk1 { get; set; }

            [AoMember(1)]
            public float Unk2 { get; set; }

            [AoMember(2)]
            public float Unk3 { get; set; }

            [AoMember(3)]
            public MovementState State { get; set; }
        }
    }

    [Flags]
    public enum ScfuFlags2
    {
        Unknown1 = 0x2,
        HasOwner = 0x4,
        Unknown3 = 0x40,
        Unknown4 = 0x80,
        Unknown5 = 0x100,
        Unknown6 = 0x200,
        Unknown7 = 0x400,
        Unknown8 = 0x800,
    };
}