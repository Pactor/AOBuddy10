// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FightModeChangeFlags.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the FightModeChangeFlags type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using System;

    /// <summary>
    /// What a fight mode change does to the district it names.
    /// </summary>
    /// <remarks>
    /// Three bits of one byte, unpacked by FightModeChange_t's reader at
    /// Gamecode.dll 0x1011FBB8 into three fields and packed back by its writer
    /// at 0x1011FC84. The client refuses any other bit.
    /// </remarks>
    [Flags]
    public enum FightModeChangeFlags : byte
    {
        /// <summary>
        /// Add the level to whatever the district already has.
        /// </summary>
        None = 0,

        /// <summary>
        /// Set the district's level rather than adding to it.
        /// </summary>
        /// <remarks>
        /// The routine that totals a district's changes, at 0x1011FDDD, assigns
        /// on this and adds without it. The reader ties the two together: a
        /// change that sets must carry a level of 0 to 4, and one that adds may
        /// carry -4 to 4, which is why the level is read signed.
        /// </remarks>
        Set = 1,

        /// <summary>
        /// Once this change is in the district's list, changes without it stop
        /// counting.
        /// </summary>
        /// <remarks>
        /// 0x1011FDDD walks the list in order and, the moment it meets one of
        /// these, skips every later change that is not also one.
        /// </remarks>
        Override = 2,

        /// <summary>
        /// Take a change out rather than putting one in.
        /// </summary>
        /// <remarks>
        /// The dispatcher at 0x101251A1 branches on this bit: without it the
        /// change is added to the district its name picks out, and with it the
        /// client goes looking for an existing change whose id matches this
        /// one, at 0x1011FE65.
        /// </remarks>
        ById = 4
    }
}
