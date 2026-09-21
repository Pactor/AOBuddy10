// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TowerInfo.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TowerInfo type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class MissionSliders
    {
        #region AoMember Properties

        [AoMember(0)]
        public byte Difficulty { get; set; }

        [AoMember(1)]
        public byte GoodBad { get; set; }

        [AoMember(2)]
        public byte OrderChaos { get; set; }

        [AoMember(3)]
        public byte OpenHidden { get; set; }

        [AoMember(4)]
        public byte PhysicalMystical { get; set; }

        [AoMember(5)]
        public byte HeadonStealth { get; set; }

        [AoMember(6)]
        public byte CreditsXp { get; set; }

        #endregion
    }
}