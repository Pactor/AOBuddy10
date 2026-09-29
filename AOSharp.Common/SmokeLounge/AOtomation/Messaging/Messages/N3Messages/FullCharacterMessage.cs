// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FullCharacterMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the FullCharacterMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.FullCharacter)]
    public class FullCharacterMessage : N3Message
    {
        #region Constructors and Destructors

        public FullCharacterMessage()
        {
            this.N3MessageType = N3MessageType.FullCharacter;
            this.Unknown = 0x00;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public int Version { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.X3F1)]
        public InventorySlot[] InventorySlots { get; set; }

        [AoMember(2, SerializeSize = ArraySizeType.X3F1)]
        public int[] UploadedNanoIds { get; set; }

        [AoMember(3, SerializeSize = ArraySizeType.X3F1)]
        public UnknownDataType1[] Unknown2 { get; set; }

        [AoMember(4)]
        public int SkillEntriesVersion { get; set; }

        [AoMember(5, SerializeSize = ArraySizeType.Int32)]
        public UnknownDataType2[] Unknown4 { get; set; }

        [AoMember(6)]
        public int Unknown5 { get; set; }

        [AoMember(7, SerializeSize = ArraySizeType.Int32)]
        public UnknownDataType2[] Unknown6 { get; set; }

        [AoMember(8)]
        public int Unknown7 { get; set; }

        [AoMember(9, SerializeSize = ArraySizeType.Int32)]
        public UnknownDataType2[] Unknown8 { get; set; }

        [AoMember(10, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<int, int>[] Stats1 { get; set; }

        [AoMember(11, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<int, int>[] Stats2 { get; set; }

        [AoMember(12, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<byte, byte>[] Stats3 { get; set; }

        [AoMember(13, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<byte, short>[] Stats4 { get; set; }

        [AoMember(14, SerializeSize = ArraySizeType.Int32)]
        public GameTuple<int, int>[] AbsorbStats { get; set; }

        [AoMember(15, SerializeSize = ArraySizeType.Int32)]
        public Identity[] UnknownIdentities { get; set; }

        [AoMember(16, SerializeSize = ArraySizeType.X3F1)]
        public TeamMember[] TeamMembers { get; set; }

        [AoMember(17, SerializeSize = ArraySizeType.X3F1)]
        public UnknownDataType4[] Unknown12 { get; set; }

        [AoMember(18, SerializeSize = ArraySizeType.X3F1)]
        public Perk[] Perks { get; set; }

        // NOT AoMembers from here on. The AoMember layout above is wrong from UnknownIdentities on: the wire
        // (OmniCell's FullCharacterMessage, which decodes all 530 recorded copies with nothing left over) carries
        //   TeamFlags; TeamIdentity if 1 or 3; RaidTeamIndex if 3; Team (1) or six raid teams (3);
        //   Pets; Buffs; ResearchGoals.
        // The stock layout read TeamFlags as a count, Pets as TeamMembers, Buffs as Unknown12 and ResearchGoals
        // as Perks - right only while solo with no pet; teamed it loaded the empty buff list as Perks and wiped
        // the trained perks. AOSharp.Clientless.Net.FullCharacterReader reads the real layout into the fields
        // below (and Perks = ResearchGoals). Unknown4/6/8 are the skill, perk and nano LOCK timers (Identity,
        // lock duration, remaining), e.g. First Aid 40 s with 36 left.
        public int TeamFlags { get; set; }
        public Identity? TeamIdentity { get; set; }
        public int? RaidTeamIndex { get; set; }
        public TeamMember[][] RaidTeams { get; set; }
        /// <summary>The character's pets (the AUTHORITATIVE pet list, used to set pet ownership).</summary>
        public Identity[] Pets { get; set; }
        /// <summary>The nano effects running on the character: each effect's function identity, hits, amount, spell list.</summary>
        public BuffEntry[] Buffs { get; set; }

        public class BuffEntry
        {
            public Identity Effect { get; set; }
            public int Hits { get; set; }
            public int Amount { get; set; }
            public int Target { get; set; }
            public int SpellList { get; set; }
        }

        #endregion

        public class TeamMember
        {
            [AoMember(0)]
            public Identity Identity { get; set; }

            [AoMember(1, SerializeSize = ArraySizeType.Int16)]
            public string Name { get; set; }

            [AoMember(2)]
            public int Unknown1 { get; set; }

            [AoMember(3)]
            public byte Unknown2 { get; set; }

            [AoMember(4)]
            public short Level { get; set; }

            [AoMember(5)]
            public short Profession { get; set; }
        }

        public class UnknownDataType1
        {
            [AoMember(0)]
            public byte Unknown1 { get; set; }

            [AoMember(1)]
            public byte Unknown2 { get; set; }

            [AoMember(2)]
            public byte Unknown3 { get; set; }
        }

        public class UnknownDataType2
        {
            [AoMember(0)]
            public int MsgVersion { get; set; }

            [AoMember(1)]
            public Identity Unknown2 { get; set; }

            [AoMember(2)]
            public int Unknown3 { get; set; }

            [AoMember(3)]
            public int Unknown4 { get; set; }
        }

        public class UnknownDataType4
        {
            [AoMember(0)]
            public int MsgVersion { get; set; }

            [AoMember(1)]
            public int Unknown2 { get; set; }

            [AoMember(2)]
            public int Unknown3 { get; set; }

            [AoMember(3)]
            public int Unknown4 { get; set; }

            [AoMember(4)]
            public int SkillEntriesVersion { get; set; }

            [AoMember(5)]
            public int Unknown6 { get; set; }

            [AoMember(6)]
            public int Unknown7 { get; set; }

            [AoMember(7)]
            public int PerkEntriesVersion { get; set; }

            [AoMember(8)]
            public int Unknown9 { get; set; }

            [AoMember(9)]
            public int NanoEntriesVersion { get; set; }

        }

        public class Perk
        {
            [AoMember(0)]
            public int SkillId { get; set; }

            [AoMember(1)]
            public int Unknown1 { get; set; }

            [AoMember(2)]
            public int Unknown2 { get; set; }

            [AoMember(3)]
            public int Unknown3 { get; set; }
        }
    }
}