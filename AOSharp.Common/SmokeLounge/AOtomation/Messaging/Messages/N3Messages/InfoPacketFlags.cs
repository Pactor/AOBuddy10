// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InfoPacketFlags.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the InfoPacketFlags type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;

    /// <summary>
    /// The bits of InfoPacketMessage's flags byte, each switching one optional block of the record on (ported
    /// 2026-09-29 from OmniCell's Messages\N3Messages\InfoPacketFlags.cs). <see cref="InfoPacketType"/>'s seven
    /// values are combinations of these.
    /// </summary>
    [Flags]
    public enum InfoPacketFlags : byte
    {
        None = 0x00,

        /// <summary>OrganizationRank and CityPlayfieldId follow.</summary>
        Organization = 0x01,

        /// <summary>With Organization: the X3F1 grid-destination list follows.</summary>
        OrganizationCities = 0x02,

        /// <summary>An X3F1 ACG item list follows (towers).</summary>
        HasAcgItems = 0x04,

        /// <summary>A suppression timer and level follow (control towers).</summary>
        Suppression = 0x08,

        /// <summary>Not a player: the eight PvP figures are absent.</summary>
        NotAPlayer = 0x10,

        /// <summary>Twelve faction standings follow.</summary>
        HasFactionStandings = 0x20,

        /// <summary>The record starts with a version byte.</summary>
        Versioned = 0x40
    }
}
