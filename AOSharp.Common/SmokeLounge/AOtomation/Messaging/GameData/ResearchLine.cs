// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResearchLine.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ResearchLine type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using System;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One research line of a ResearchUpdate (OmniCell: ResearchUpdateEntry).
    /// </summary>
    public class ResearchLine
    {
        #region AoMember Properties

        /// <summary>Which research this entry is about, and the terminator when zero.</summary>
        [AoMember(0)]
        public int ResearchId { get; set; }

        /// <summary>Neutral-side research XP remaining.</summary>
        [AoMember(1)]
        public int NeutralResearchXpRemaining { get; set; }

        /// <summary>Clan-side research XP remaining (manager/Side index 1).</summary>
        [AoMember(2)]
        public int ClanResearchXpRemaining { get; set; }

        /// <summary>Omni-side research XP remaining (manager/Side index 2).</summary>
        [AoMember(3)]
        public int OmniResearchXpRemaining { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [System.Obsolete("Wire field is NeutralResearchXpRemaining.")]
        public int Unknown1 { get => this.NeutralResearchXpRemaining; set => this.NeutralResearchXpRemaining = value; }

        [System.Obsolete("Wire field is ClanResearchXpRemaining.")]
        public int Unknown2 { get => this.ClanResearchXpRemaining; set => this.ClanResearchXpRemaining = value; }

        [System.Obsolete("Wire field is OmniResearchXpRemaining.")]
        public int Unknown3 { get => this.OmniResearchXpRemaining; set => this.OmniResearchXpRemaining = value; }

        #endregion
    }
}
