// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NewLevelMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the NewLevelMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
    using System;

    // Layout, names and doc comments ported from OmniCell's AOtomation.Messaging (verified against
    // every recorded packet). Old AOSharp names are kept as [Obsolete] aliases where they differed.
    /// <summary>
    /// Sent when a character gains a level.
    /// </summary>
    /// <remarks>
    /// N3MessageType.NewLevel was in the enum with no class behind it, so these
    /// never deserialised. Reconstructed from 7 captured across several 18.8.x
    /// sessions, all 61 bytes:
    ///
    ///   Level  IP        Experience  ThisLevel  NextLevel  Title KillRange Award
    ///       2      4662        1450       1450       4050   0   4   145
    ///       3      6471        4310       4050       7150   0   4   520
    ///       3      6631        4050       4050       7150   0   4   260
    ///       2      4444        1450       1450       4050   0   4   145
    ///       3      6515        4310       4050       7150   0   4   520
    ///       4      7798        7410       7150      11150   0   4   310
    ///       5      8400       11825      11150      15650   0   4   800
    ///
    /// The three experience fields identify themselves. ThisLevel for a given
    /// level always equals NextLevel for the level below - 4050 at level 3 is
    /// 4050 at level 2, 7150 at level 4 is 7150 at level 3, and so on - which
    /// fixes those two as the thresholds either side. Experience then falls
    /// within that band in all seven, which fixes it as the running total.
    ///
    /// The other four were named later from the client's dispatcher: improvement
    /// points (stat 53), title level (stat 37), experience kill range (stat 275) and
    /// the experience award the level-up feedback quotes. See each property.
    /// </remarks>
    [AoContract((int)N3MessageType.NewLevel)]
    public class NewLevelMessage : N3Message
    {
        #region Constructors and Destructors

        public NewLevelMessage()
        {
            this.N3MessageType = N3MessageType.NewLevel;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The level just reached.
        /// </summary>
        [AoMember(0)]
        public int Level { get; set; }

        /// <summary>
        /// The character's improvement point balance after the level.
        /// </summary>
        /// <remarks>
        /// Named on 2026-09-12 from the dispatcher rather than from the
        /// captures, which is why it stayed anonymous: 0x10075FB4 pushes the
        /// message's + 0x1C with stat 0x35 - 53, IP - into the setter on the
        /// character's stat table. The client writes this straight into the
        /// player's IP, so a server that sends zero here zeroes their
        /// improvement points on every level.
        /// </remarks>
        [AoMember(1)]
        public int ImprovementPoints { get; set; }

        /// <summary>
        /// Total experience, which sits between <see cref="ExperienceThisLevel"/>
        /// and <see cref="ExperienceNextLevel"/>.
        /// </summary>
        [AoMember(2)]
        public int Experience { get; set; }

        /// <summary>
        /// Experience needed for the level just reached.
        /// </summary>
        [AoMember(3)]
        public int ExperienceThisLevel { get; set; }

        /// <summary>
        /// Experience needed for the next level.
        /// </summary>
        [AoMember(4)]
        public int ExperienceNextLevel { get; set; }

        /// <summary>
        /// The character's title level, written only when positive.
        /// </summary>
        /// <remarks>
        /// The message's + 0x2C. The dispatcher writes it to stat 37,
        /// titlelevel, on a branch of its own rather than in the run of
        /// unconditional stat writes, which is why it is not beside the others
        /// at 0x10075FA1 onward.
        /// </remarks>
        [AoMember(5)]
        public int TitleLevel { get; set; }

        /// <summary>
        /// The experience kill range.
        /// </summary>
        /// <remarks>
        /// The message's + 0x30, pushed at 0x10075FDB with stat 0x113 - 275,
        /// xpkillrange. 4 to level 7 and 9 by level 31 across the retail
        /// recordings; see ZoneEngine XML Data\Experience.xml.
        /// </remarks>
        [AoMember(6)]
        public int ExperienceKillRange { get; set; }

        /// <summary>
        /// The experience the event that caused the level awarded.
        /// </summary>
        /// <remarks>
        /// The message's + 0x34, and the one field of the eight that does not
        /// go into a stat. The dispatcher reads it at 0x10075FFA, skips the
        /// rest when it is zero, and otherwise hands it to the Feedback_NewLevel
        /// string - so it is what the player is told they gained. The captures
        /// agree arithmetically: previous total plus this equals the total in
        /// the same message.
        /// </remarks>
        [AoMember(7)]
        public int ExperienceAward { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is ImprovementPoints (stat 53).")]
        public int AvailableIp { get => this.ImprovementPoints; set => this.ImprovementPoints = value; }

        [Obsolete("Wire field is Experience (total XP).")]
        public int CurrentXp { get => this.Experience; set => this.Experience = value; }

        [Obsolete("Wire field is ExperienceThisLevel.")]
        public int LastLevelXp { get => this.ExperienceThisLevel; set => this.ExperienceThisLevel = value; }

        [Obsolete("Wire field is ExperienceNextLevel.")]
        public int NextLevelXp { get => this.ExperienceNextLevel; set => this.ExperienceNextLevel = value; }

        /// <summary>Misnamed in AOSharp: this has always been the title level (stat 37).</summary>
        [Obsolete("Wire field is TitleLevel (stat 37), not IP reset points.")]
        public int IpResetPointsGained { get => this.TitleLevel; set => this.TitleLevel = value; }

        [Obsolete("Wire field is ExperienceKillRange (stat 275).")]
        public int XpKillRange { get => this.ExperienceKillRange; set => this.ExperienceKillRange = value; }

        [Obsolete("Wire field is ExperienceAward.")]
        public int LastAwardedXp { get => this.ExperienceAward; set => this.ExperienceAward = value; }

        #endregion
    }
}
