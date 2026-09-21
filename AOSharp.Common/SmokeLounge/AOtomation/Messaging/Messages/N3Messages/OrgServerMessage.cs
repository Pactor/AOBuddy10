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
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

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

        [AoMember(1)]
        public int Unknown1 { get; set; }

        [AoMember(2)]
        public int Unknown2 { get; set; }

        [AoMember(3)]
        public Identity Organization { get; set; }

        [AoUsesFlags("orgmessagetype", typeof(OrgInvite), FlagsCriteria.EqualsToAny, new[] { (int)OrgServerMessageType.OrgInvite })]
        [AoUsesFlags("orgmessagetype", typeof(OrganizationInfo), FlagsCriteria.EqualsToAny, new[] { (int)OrgServerMessageType.OrgInfo })]
        [AoUsesFlags("orgmessagetype", typeof(ContractsInfo), FlagsCriteria.EqualsToAny, new[] { (int)OrgServerMessageType.OrgContract })]
        [AoMember(4)]
        public IOrgServerMessage IOrgServerMessage { get; set; }

        #endregion
    }

    public class OrgInvite : IOrgServerMessage
    {
        [AoMember(0)]
        public int Unknown3 { get; set; }
    }

    public class ContractsInfo : IOrgServerMessage
    {
        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public OrgContractSlot[] Contracts { get; set; }
    }

    public class OrganizationInfo : IOrgServerMessage
    {
        [AoMember(0, SerializeSize = ArraySizeType.Int16)]
        public string OrganizationName { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string Description { get; set; }

        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public string Objective { get; set; }

        [AoMember(3, SerializeSize = ArraySizeType.Int16)]
        public string History { get; set; }

        [AoMember(4, SerializeSize = ArraySizeType.Int16)]
        public string GoverningForm { get; set; }

        [AoMember(5, SerializeSize = ArraySizeType.Int16)]
        public string LeaderName { get; set; }

        [AoMember(6, SerializeSize = ArraySizeType.Int16)]
        public string Rank { get; set; }

        [AoMember(7, SerializeSize = ArraySizeType.X3F1)]
        public ControlledArea[] ControlledAreas { get; set; }
    }

    public interface IOrgServerMessage { }

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