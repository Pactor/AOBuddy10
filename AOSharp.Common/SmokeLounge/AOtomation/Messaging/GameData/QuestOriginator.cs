// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QuestOriginator.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the QuestOriginator type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// Where a mission came from.
    /// </summary>
    /// <remarks>
    /// GameData's own enum, and its own words: GetQuestOriginatorName at
    /// GameData.dll 0x10002D0A is a table of nine strings indexed by this, and
    /// the names below are those strings. IsTeamOriginator at 0x10002D23
    /// answers yes to 2, 4, 6 and 8 - the even ones - which is what pairs each
    /// solo value with its team version.
    ///
    /// The reader refuses anything outside 1 to 8, and says so: the error it
    /// prints when a mission arrives with a bad one reads "Originator = %u:%u
    /// %u (valid is [%d, %d])", and the bounds it fills in are the constants
    /// pushed at 0x100CA062 - one and eight. So Unknown is a value this enum
    /// needs and the wire will never carry.
    /// </remarks>
    public enum QuestOriginator : byte
    {
        /// <summary>
        /// "&lt;unknown originator&gt;".
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A quest from a neutral booth.
        /// </summary>
        NeutralBooth = 1,

        /// <summary>
        /// A team quest from a neutral booth.
        /// </summary>
        NeutralBoothTeam = 2,

        /// <summary>
        /// A quest from an Omni-Tek booth.
        /// </summary>
        OmniBooth = 3,

        /// <summary>
        /// A team quest from an Omni-Tek booth.
        /// </summary>
        OmniBoothTeam = 4,

        /// <summary>
        /// A quest from a clan booth.
        /// </summary>
        ClanBooth = 5,

        /// <summary>
        /// A team quest from a clan booth.
        /// </summary>
        ClanBoothTeam = 6,

        /// <summary>
        /// A quest from an NPC.
        /// </summary>
        Npc = 7,

        /// <summary>
        /// A team quest from an NPC.
        /// </summary>
        NpcTeam = 8
    }
}
