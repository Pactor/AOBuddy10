// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QuestAlternativeMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the QuestAlternativeMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Layout, names and doc comments ported from OmniCell's AOtomation.Messaging. OmniCell reads the six
    // sliders and the difficulty inline; this SDK keeps them grouped in MissionSliders (same bytes).
    // Old AOSharp names are kept as [Obsolete] aliases. MissionDetails (MissionInfo, fixed-size chunks) is
    // unchanged: OmniCell's QuestAlternativeEntry = QuestInfo + a trailer byte, owned with the Quest classes.
    [AoContract((int)N3MessageType.QuestAlternative)]
    public class QuestAlternativeMessage : N3Message
    {
        #region Constructors and Destructors

        public QuestAlternativeMessage()
        {
            this.N3MessageType = N3MessageType.QuestAlternative;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The record's version. 4 in every captured copy.
        /// </summary>
        /// <remarks>
        /// Read at 0x100C9F88 and checked against the class's own version
        /// through its vtable; a mismatch is printed and the message dropped.
        /// </remarks>
        [AoMember(0)]
        public byte VersionId { get; set; }

        /// <summary>
        /// The difficulty (1 to 11) and the six GameData::DimensionValues_t sliders (-100..100, signed
        /// bytes), in wire order. See <see cref="MissionSliders"/>.
        /// </summary>
        [AoMember(1)]
        public MissionSliders MissionSliders { get; set; }

        /// <summary>
        /// What the missions on offer were generated from.
        /// </summary>
        /// <remarks>
        /// The error message calls it the seed. A different large value in each
        /// of the four captured terminals, which is what a seed looks like.
        /// </remarks>
        [AoMember(2)]
        public int Seed { get; set; }

        /// <summary>
        /// What kind of place is offering them.
        /// </summary>
        /// <remarks>
        /// The error message gives its range as [0, 8], which is the span of
        /// GameData's QuestOriginator_e exactly (the reader refuses anything outside 1 to 8), and every
        /// captured copy carries 1 - a quest from a neutral booth - beside a mission terminal
        /// identity. AOSharp modelled it as MissionScope (Solo 1 / Team 2), which are only the two
        /// neutral-booth values.
        /// </remarks>
        [AoMember(3)]
        public QuestOriginator Originator { get; set; }

        /// <summary>
        /// The mission terminal (OmniCell: MissionTerminalIdentity).
        /// </summary>
        [AoMember(4)]
        public Identity Terminal { get; set; }

        /// <summary>
        /// The quests on offer, each followed by a byte.
        /// </summary>
        /// <remarks>
        /// A byte count, then that many quests - and the client takes a byte
        /// after every one of them, inside the loop at 0x100CB2EE, rather than
        /// once after the list the way QuestFullUpdate does.
        /// </remarks>
        [AoMember(5, SerializeSize = ArraySizeType.Byte)]
        public MissionInfo[] MissionDetails { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is Seed.")]
        public int Unknown2 { get => this.Seed; set => this.Seed = value; }

        /// <summary>
        /// Old name for <see cref="Originator"/>. MissionScope Solo (1) / Team (2) are
        /// QuestOriginator NeutralBooth / NeutralBoothTeam; Omni, Clan and NPC booths are 3-8.
        /// </summary>
        [Obsolete("Wire field is Originator (QuestOriginator 1-8: booth side + team). MissionScope covers only the neutral booths.")]
        public MissionScope Scope { get => (MissionScope)(byte)this.Originator; set => this.Originator = (QuestOriginator)(byte)value; }

        #endregion
    }
}
