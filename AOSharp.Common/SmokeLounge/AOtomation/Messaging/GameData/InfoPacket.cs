// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InfoPacket.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the InfoPacket type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using AOSharp.Common.GameData;

    /// <summary>
    /// The contents of an examine window: one record with optional blocks, chosen by the flag bits on
    /// <see cref="Messages.N3Messages.InfoPacketMessage"/>.
    /// </summary>
    /// <remarks>
    /// Rewritten 2026-09-29 after OmniCell (GameData\InfoPacket.cs, Messages\N3Messages\InfoPacketFlags.cs), which
    /// walks every captured copy to the last byte. The client has one reader (Gamecode 0x10045F6C) and one
    /// structure, and tests the flags byte a bit at a time. The AOSharp original had three unrelated subclasses
    /// chosen by seven whole flag values; the monster one read nothing (43 bytes left on every mob examine), the
    /// character one had OrganizationId conditional and CityPlayfieldId unconditional (both wrong, cancelling only
    /// while a character had no org).
    ///
    /// The subclasses remain so existing <c>is CharacterInfoPacket</c> tests keep their meaning:
    /// <see cref="CharacterInfoPacket"/> when the NotAPlayer bit (0x10) is clear, <see cref="TowerInfoPacket"/> when it
    /// is set with HasAcgItems (0x04) or Suppression (0x08), otherwise <see cref="MonsterInfoPacket"/>. All three
    /// carry the same record, read by <see cref="Serialization.Serializers.Custom.InfoPacketSerializer"/>:
    ///
    ///   byte    Version, only when flags has 0x40 (Versioned), else 0
    ///   byte    Profession, Level, TitleLevel
    ///   byte    UnversionedPad, only when Version is 0
    ///   byte    VisualProfession
    ///   int16   SideXp
    ///   int32   Health, MaxHealth, BreedHostility, OrganizationId
    ///   string  FirstName, LastName, AuxiliaryName, DisplayText (each Int16-counted)
    ///   0x01:   Int16 string OrganizationRank;
    ///           0x02: X3F1 grid destinations (<see cref="TowerFields"/>);
    ///           int32 CityPlayfieldId
    ///   0x04:   X3F1 ACG items (<see cref="Towers"/>)
    ///   0x08:   int32 SuppressionTimer, byte SuppressionLevel
    ///   0x20:   twelve int32 faction standings (stats 561-572)
    ///   int32   InvadersKilled, KilledByInvaders, AiLevel
    ///   0x10 clear: eight int32 PvP figures
    /// </remarks>
    public abstract class InfoPacket
    {
        /// <summary>
        /// The record's version, present only with the Versioned flag (0x40). 1 in every capture.
        /// </summary>
        public byte Version { get; set; }

        public Profession Profession { get; set; }

        /// <summary>
        /// The examined dynel's level byte. On mission NPCs this was 1 while their SCFU said 42, so it is not
        /// always the level.
        /// </summary>
        public byte Level { get; set; }

        public byte TitleLevel { get; set; }

        /// <summary>
        /// One byte the client reads and drops (0x10045FB4), present only when <see cref="Version"/> is 0.
        /// </summary>
        public byte? UnversionedPad { get; set; }

        public Profession VisualProfession { get; set; }

        public short SideXp { get; set; }

        /// <summary>
        /// Current health of the examined dynel.
        /// </summary>
        public int Health { get; set; }

        /// <summary>
        /// Maximum health of the examined dynel.
        /// </summary>
        public int MaxHealth { get; set; }

        /// <summary>
        /// 60 on the captured mission NPCs.
        /// </summary>
        public int BreedHostility { get; set; }

        /// <summary>
        /// Unconditional (the fourth unconditional int32), not only with the organization flag.
        /// </summary>
        public int OrganizationId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        /// <summary>
        /// The third name string (the AOSharp original's LegacyTitle).
        /// </summary>
        public string AuxiliaryName { get; set; }

        /// <summary>
        /// The fourth string (a tower's formatted text).
        /// </summary>
        public string DisplayText { get; set; }

        /// <summary>
        /// Present with the Organization flag (0x01).
        /// </summary>
        public string OrganizationRank { get; set; }

        /// <summary>
        /// The organization's grid destinations (OmniCell GridDestinations: playfield, identity, name, area level,
        /// area type), present with Organization and OrganizationCities (0x01 | 0x02).
        /// </summary>
        public TowerField[] TowerFields { get; set; }

        /// <summary>
        /// Present with the Organization flag (0x01) only.
        /// </summary>
        public int? CityPlayfieldId { get; set; }

        /// <summary>
        /// ACG items (OmniCell AcgItems: low id, high id, quality, unused), present with HasAcgItems (0x04) -
        /// a tower's item.
        /// </summary>
        public Tower[] Towers { get; set; }

        /// <summary>
        /// Present with the Suppression flag (0x08).
        /// </summary>
        public int? SuppressionTimer { get; set; }

        /// <summary>
        /// Present with the Suppression flag (0x08).
        /// </summary>
        public byte? SuppressionLevel { get; set; }

        /// <summary>
        /// Twelve standings filed into stats 561-572 (ClanSentinels, OtMed, ClanGaia, OtTrans, ClanVanguards, Gos,
        /// OtFollowers, OtOperator, OtUnredeemed, ClanDevoted, ClanConserver, ClanRedeemed), present with
        /// HasFactionStandings (0x20); null otherwise. No capture has carried it.
        /// </summary>
        public int[] FactionStandings { get; set; }

        /// <summary>
        /// 1234567890 (the unset marker) on the captured NPCs.
        /// </summary>
        public int InvadersKilled { get; set; }

        public int KilledByInvaders { get; set; }

        public int AiLevel { get; set; }

        /// <summary>
        /// PvP figures, present only when NotAPlayer (0x10) is clear; 0 otherwise. OmniCell: PvpDuelKills.
        /// </summary>
        public int PvpDuelWins { get; set; }

        /// <summary>
        /// OmniCell: PvpDuelDeaths.
        /// </summary>
        public int PvpDuelLoses { get; set; }

        /// <summary>
        /// OmniCell: PvpProfessionDuelKills.
        /// </summary>
        public int PvpProfessionDuelLoses { get; set; }

        /// <summary>
        /// OmniCell: PvpRankedSoloKills.
        /// </summary>
        public int PvpSoloKills { get; set; }

        /// <summary>
        /// OmniCell: PvpRankedTeamKills.
        /// </summary>
        public int PvpTeamKills { get; set; }

        public int PvpSoloScore { get; set; }

        public int PvpTeamScore { get; set; }

        public int PvpDuelScore { get; set; }
    }
}
