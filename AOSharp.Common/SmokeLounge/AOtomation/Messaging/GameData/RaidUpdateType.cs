// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RaidUpdateType.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the RaidUpdateType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (GameData\RaidUpdateType.cs); no changes.

    /// <summary>
    /// Which of the two things a Raid message is.
    /// </summary>
    /// <remarks>
    /// The client reads an int16 and widens it, and its writer narrows the same
    /// field back, so two bytes is what travels. RaidIIR_c's dispatcher at
    /// Gamecode 0x100A40C0 tells the two apart and nothing else reaches a body.
    /// </remarks>
    public enum RaidUpdateType : short
    {
        /// <summary>
        /// The raid has been formed, and nothing follows.
        /// </summary>
        /// <remarks>
        /// The dispatcher clears the team subsystem's +0x38 - the member
        /// N3Msg_GetRaidTeamIndex reads - raises "Feedback_RaidCreated", and
        /// emits GlobalSignals_c + 0x13C, which GUI.dll connects at 0x10103609
        /// to the handler at 0x10102D5E that greys the raid window's create
        /// button out.
        /// </remarks>
        Created = 0,

        /// <summary>
        /// The raid's instance locks, in answer to N3Msg_RequestRaidLocks.
        /// </summary>
        Locks = 1
    }
}
