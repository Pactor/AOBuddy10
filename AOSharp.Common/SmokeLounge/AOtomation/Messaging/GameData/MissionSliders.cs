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
    using System;
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The difficulty and the six mission sliders of a QuestAlternative, in wire order.
    /// </summary>
    /// <remarks>
    /// Names and docs from OmniCell's QuestAlternativeMessage, which reads these inline. The six
    /// sliders are one GameData::DimensionValues_t, read at 0x100C9FD3 through GameData's own stream
    /// operator; it takes six bytes and refuses any of them outside -100 to 100. The names are
    /// GameData's (GetDimensionName at GameData.dll 0x1000219B). They are signed on the wire and
    /// bytes here, so the sign has to be applied by whoever reads them.
    /// </remarks>
    public class MissionSliders
    {
        #region AoMember Properties

        /// <summary>
        /// How hard the missions on offer are, from 1 to 11. Read before the sliders, at 0x100C9FCC,
        /// which is what makes it not one of them.
        /// </summary>
        [AoMember(0)]
        public byte Difficulty { get; set; }

        /// <summary>"good vs bad (GB)", from -100 to 100.</summary>
        [AoMember(1)]
        public byte GoodBad { get; set; }

        /// <summary>"controlled vs lacking control (CL)", from -100 to 100.</summary>
        [AoMember(2)]
        public byte ControlledLackingControl { get; set; }

        /// <summary>"open vs hidden (OH)", from -100 to 100.</summary>
        [AoMember(3)]
        public byte OpenHidden { get; set; }

        /// <summary>"physical vs mystical (PM)", from -100 to 100.</summary>
        [AoMember(4)]
        public byte PhysicalMystical { get; set; }

        /// <summary>"explosive vs patient (EP)", from -100 to 100.</summary>
        [AoMember(5)]
        public byte ExplosivePatient { get; set; }

        /// <summary>"money reward vs experience reward (ME)", from -100 to 100.</summary>
        [AoMember(6)]
        public byte MoneyExperience { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is ControlledLackingControl (GameData's name).")]
        public byte OrderChaos { get => this.ControlledLackingControl; set => this.ControlledLackingControl = value; }

        [Obsolete("Wire field is ExplosivePatient (GameData's name).")]
        public byte HeadonStealth { get => this.ExplosivePatient; set => this.ExplosivePatient = value; }

        [Obsolete("Wire field is MoneyExperience (GameData's name).")]
        public byte CreditsXp { get => this.MoneyExperience; set => this.MoneyExperience = value; }

        #endregion
    }
}
