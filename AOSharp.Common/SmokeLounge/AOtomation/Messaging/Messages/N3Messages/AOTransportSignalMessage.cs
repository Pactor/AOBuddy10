// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QuestMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the QuestMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.AOTransportSignal)]
    public class AOTransportSignalMessage : N3Message
    {
        #region Constructors and Destructors

        public AOTransportSignalMessage()
        {
            this.N3MessageType = N3MessageType.AOTransportSignal;
        }

        #endregion

        #region AoMember Properties

        [AoFlags("action")]
        [AoMember(0)]
        public AOSignalAction Action { get; set; }

        [AoUsesFlags("action", typeof(CityInfo), FlagsCriteria.EqualsToAny, new[] { (int)AOSignalAction.CityInfo })]
        [AoUsesFlags("action", typeof(CityCreditsUpkeep), FlagsCriteria.EqualsToAny, new[] { (int)AOSignalAction.CreditsUpkeepInfo })]
        [AoUsesFlags("action", typeof(CloakInfo), FlagsCriteria.EqualsToAny, new[] { (int)AOSignalAction.CloakInfo })]
        [AoUsesFlags("action", typeof(CityNextUpkeep), FlagsCriteria.EqualsToAny, new[] { (int)AOSignalAction.UpkeepInfo })]
        [AoUsesFlags("action", typeof(CityCharge), FlagsCriteria.EqualsToAny, new[] { (int)AOSignalAction.ChargeInfo })]

        [AoMember(1)]
        public IAOTransportSignalMessage TransportSignalMessage { get; set; }

        #endregion
    }

    public interface IAOTransportSignalMessage
    {
    }

    public class CityInfo : IAOTransportSignalMessage
    {
        #region AoMember Properties

        [AoMember(0)]
        public Identity UnknownIdentity1 { get; set; }

        [AoMember(1)]
        public int Unknown1 { get; set; }

        [AoMember(2)]
        public Identity UnknownIdentity2 { get; set; }

        [AoMember(3)]
        public Identity User { get; set; }

        [AoMember(4)]
        public int Unknown2 { get; set; }

        [AoMember(5)]
        public int Unknown3 { get; set; }

        [AoMember(6, SerializeSize = ArraySizeType.Int32)]
        public string OrgName { get; set; }

        #endregion
    }

    public class CityCreditsUpkeep : IAOTransportSignalMessage
    {
        [AoMember(0)]
        public int CreditsUpkeep { get; set; }
    }

    public class CloakInfo : IAOTransportSignalMessage
    {
        #region AoMember Properties

        [AoMember(0)]
        public CloakStatus CloakState { get; set; }

        [AoMember(1)]
        public int ShieldTimerInSeconds { get; set; }

        #endregion
    }

    public class CityNextUpkeep : IAOTransportSignalMessage
    {
        #region AoMember Properties

        [AoMember(0)]
        public int NextUpkeepPaymentInSeconds { get; set; }

        #endregion
    }


    public class CityCharge : IAOTransportSignalMessage
    {
        #region AoMember Properties

        [AoMember(0)]
        public float CityControllerCharge { get; set; }

        #endregion
    }
}

public enum CloakStatus
{
    Unknown = 0,
    Disabled = -1,
    Enabled = 1,
}