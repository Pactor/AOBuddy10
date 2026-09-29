// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OrgServerMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the OrgServerMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Layout, names and doc comments ported from OmniCell's AOtomation.Messaging (OrgServerMessage and
    // OrgServerMessages\*). OmniCell models the nine kinds as subclasses; this SDK keeps its flag-selected
    // body (IOrgServerMessage) and gives each kind OmniCell's layout. The reader at Gamecode 0x10126D95
    // reads the kind byte and refuses the whole message unless it is 1 to 9.
    [AoContract((int)N3MessageType.OrgServer)]
    public class OrgServerMessage : N3Message
    {
        #region Constructors and Destructors

        protected OrgServerMessage()
        {
            this.N3MessageType = N3MessageType.OrgServer;
        }

        #endregion

        #region AoMember Properties

        [AoFlags("orgmessagetype")]
        [AoMember(0)]
        public OrgServerMessageType OrgServerMessageType { get; set; }

        /// <summary>
        /// An Identity, empty in all 130 captured copies, that the client reads
        /// and writes and never once looks at.
        /// </summary>
        /// <remarks>
        /// It was two int32s here (Unknown1, Unknown2), which is the same eight bytes and the wrong
        /// shape: the reader at Gamecode.dll 0x10126DD2 takes it with the
        /// standard Identity reader, in the same call that takes the
        /// organization after it, and the writer at 0x10126C6C puts it back the
        /// same way.
        /// </remarks>
        [AoMember(1)]
        public Identity Unknown1 { get; set; }

        /// <summary>
        /// Which organization the message is about.
        /// </summary>
        [AoMember(2)]
        public Identity Organization { get; set; }

        /// <summary>
        /// Only two of the message types carry this: OrgInfo (2) and OrgInvite (5).
        /// </summary>
        /// <remarks>
        /// The reader's branch for type 6 at Gamecode 0x10126E13 takes
        /// three int32s and a byte and no string at all. The types that do read a string into this
        /// member are 5, whose branch at 0x10126E67 takes one and then an int32, and 2, whose branch at
        /// 0x10126E87 takes eight of them and this is the first. AOSharp read OrgInvite's int from
        /// this string's position.
        /// </remarks>
        [AoUsesFlags("orgmessagetype", typeof(string), FlagsCriteria.EqualsToAny,
            new[] { (int)OrgServerMessageType.OrgInfo, (int)OrgServerMessageType.OrgInvite })]
        [AoMember(3, SerializeSize = ArraySizeType.Int16)]
        public string OrganizationName { get; set; }

        /// <summary>
        /// The kind's own body; null for kinds 3, 4 and 9, which carry none (the switch at
        /// 0x10126E05 sends them straight to the success exit).
        /// </summary>
        [AoUsesFlags("orgmessagetype", typeof(OrgKind1), FlagsCriteria.EqualsToAny, new[] { (int)OrgServerMessageType.OrgKind1 })]
        [AoUsesFlags("orgmessagetype", typeof(OrganizationInfo), FlagsCriteria.EqualsToAny, new[] { (int)OrgServerMessageType.OrgInfo })]
        [AoUsesFlags("orgmessagetype", typeof(OrgInvite), FlagsCriteria.EqualsToAny, new[] { (int)OrgServerMessageType.OrgInvite })]
        [AoUsesFlags("orgmessagetype", typeof(OrgContract), FlagsCriteria.EqualsToAny, new[] { (int)OrgServerMessageType.OrgContract })]
        [AoUsesFlags("orgmessagetype", typeof(OrgKind7), FlagsCriteria.EqualsToAny, new[] { (int)OrgServerMessageType.OrgKind7, (int)OrgServerMessageType.OrgKind8 })]
        [AoMember(4)]
        public IOrgServerMessage IOrgServerMessage { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        /// <summary>Old second half of the empty Identity AOSharp read as two ints.</summary>
        [Obsolete("Wire field is Unknown1 (an Identity); this is its instance half.")]
        public int Unknown2 { get => this.Unknown1.Instance; set => this.Unknown1 = new Identity(this.Unknown1.Type, value); }

        #endregion
    }

    /// <summary>
    /// OrgServer kind 1: a container and then a flag byte.
    /// </summary>
    /// <remarks>
    /// The branch at 0x10126EF8 reads the shared container reader (an X3F1 count and then, per
    /// entry, a placement, two int16s, an Identity and a GameData::ACGItem_t - InventorySlot)
    /// for container page 0x1E of that Identity, which is what an organization's bank would look
    /// like. The byte after it goes through 0x100A0138, the helper that normalises a byte to 0 or 1.
    /// AOSharp modelled this kind as "OrgContract" with OrgContractSlot entries (the same 32 bytes)
    /// and no flag byte.
    /// </remarks>
    public class OrgKind1 : IOrgServerMessage
    {
        /// <summary>
        /// The container's contents, X3F1 counted.
        /// </summary>
        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public InventorySlot[] Contents { get; set; }

        /// <summary>
        /// A flag, normalised to 0 or 1 by 0x100A0138 and kept at + 0xF4.
        /// </summary>
        [AoMember(1)]
        public byte Flag { get; set; }
    }

    /// <summary>OrgServer kind 5: after the organization name, one int32.</summary>
    public class OrgInvite : IOrgServerMessage
    {
        [AoMember(0)]
        public int Unknown3 { get; set; }
    }

    /// <summary>
    /// OrgServer kind 6, and the only kind ever captured (all 404 OmniCell copies).
    /// </summary>
    public class OrgContract : IOrgServerMessage
    {
        /// <summary>
        /// The contract item's quality.
        /// </summary>
        /// <remarks>
        /// The branch at Gamecode 0x10126E13 reads three int32s in the order quality, low id,
        /// high id and hands them to GameData::ACGItem_t::ACGItem_t(unsigned
        /// int, unsigned int, int) as (low, high, quality), which is the same
        /// item record MarketSend and Mail carry.
        /// </remarks>
        [AoMember(0)]
        public int Quality { get; set; }

        [AoMember(1)]
        public int ItemLowId { get; set; }

        [AoMember(2)]
        public int ItemHighId { get; set; }

        /// <summary>
        /// A byte the reader normalises to 0 or 1.
        /// </summary>
        /// <remarks>
        /// Read through 0x100A0138, the same helper DoorStatusUpdate's flags go
        /// through, and stored at the message's +0x108.
        /// </remarks>
        [AoMember(3)]
        public byte Active { get; set; }
    }

    /// <summary>
    /// OrgServer kinds 7 and 8: one int32, into the message's + 0xD4. The switch subtracts 7 and
    /// takes anything at or below 1, so kinds 7 and 8 share the branch at 0x10126E73.
    /// </summary>
    public class OrgKind7 : IOrgServerMessage
    {
        [AoMember(0)]
        public int Unknown1 { get; set; }
    }

    /// <summary>
    /// AOSharp's model of kind 1 (which it called OrgContract). Kind 1 is <see cref="OrgKind1"/>;
    /// this class is no longer produced by the reader.
    /// </summary>
    [Obsolete("Kind 1 is OrgKind1 (InventorySlot[] + flag); kind 6 (the real OrgContract) is OrgContract.")]
    public class ContractsInfo : IOrgServerMessage
    {
        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public OrgContractSlot[] Contracts { get; set; }
    }

    /// <summary>
    /// OrgServer kind 2. The organization name (the first of the eight strings the branch at
    /// Gamecode 0x10126E87 reads) is on the message: <see cref="OrgServerMessage.OrganizationName"/>.
    /// </summary>
    public class OrganizationInfo : IOrgServerMessage
    {
        [AoMember(0, SerializeSize = ArraySizeType.Int16)]
        public string Description { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string Objective { get; set; }

        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public string History { get; set; }

        [AoMember(3, SerializeSize = ArraySizeType.Int16)]
        public string GoverningForm { get; set; }

        [AoMember(4, SerializeSize = ArraySizeType.Int16)]
        public string LeaderName { get; set; }

        [AoMember(5, SerializeSize = ArraySizeType.Int16)]
        public string Rank { get; set; }

        /// <summary>
        /// The eighth string, which AOSharp did not read.
        /// </summary>
        /// <remarks>
        /// The branch for this type at Gamecode 0x10126E87 pushes eight
        /// addresses and calls the counted-string reader eight times. The
        /// organization name on the message is the first of the eight and
        /// the six above are the next six; this is the last, into the message's
        /// +0x10C.
        /// </remarks>
        [AoMember(6, SerializeSize = ArraySizeType.Int16)]
        public string Unknown4 { get; set; }

        /// <summary>
        /// The X3F1 list the kind 2 branch reads after its eight strings (OmniCell: Destinations,
        /// GridDestination[]).
        /// </summary>
        /// <remarks>
        /// Each entry goes through 0x1012905A, which reads an int32, an Identity through the shared
        /// reader, a counted string through the int16 helper at 0x10038AF8, and two more int32s.
        /// That is GridDestination, field for field (ControlledArea has the same shape).
        /// </remarks>
        [AoMember(7, SerializeSize = ArraySizeType.X3F1)]
        public ControlledArea[] ControlledAreas { get; set; }
    }

    public interface IOrgServerMessage { }

    /// <summary>
    /// One entry of OrganizationInfo's list: OmniCell's GridDestination (PlayfieldId, Identity,
    /// Name, AreaLevel, AreaType).
    /// </summary>
    public class ControlledArea
    {
        [AoMember(0)]
        public PlayfieldId PlayfieldId { get; set; }

        [AoMember(1)]
        public Identity Identity { get; set; }

        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public string Area { get; set; }

        [AoMember(3)]
        public int Level { get; set; }

        [AoMember(4)]
        public int Type { get; set; }
    }
}
