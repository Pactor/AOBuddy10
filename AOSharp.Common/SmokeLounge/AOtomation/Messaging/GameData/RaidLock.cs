// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RaidLock.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the RaidLock type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (GameData\RaidLock.cs). Changes for this
    // library only: AOSharp's Identity/Vector3/Quaternion types.

    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One instance a raid is locked out of, and when the lock lifts.
    /// </summary>
    /// <remarks>
    /// Twenty bytes, read by Gamecode 0x10057277: two Identities and an int32.
    /// The names come from the one thing that reads the list back, the
    /// renderer at 0x100430C2, which walks it at stride 0x14 and prints a line
    /// per entry under the heading "Raid locks:".
    ///
    /// That line is built as name + "(" + the first Identity's instance +
    /// "): " + the second Identity's instance + " - " + hours + "h " +
    /// minutes + "m " + seconds + "s". The name is the return of the lookup at
    /// 0x10036982, which is the whole body of the export
    /// n3EngineClientAnarchy_t::N3Msg_GetPFName(unsigned int) at 0x10016AE6 -
    /// so the first Identity is a playfield, and when the table has no entry
    /// for it the client prints "Unknown". The hours, minutes and seconds are
    /// the int32 minus the client's current time, divided by 0xE10 and then by
    /// 0x3C, which makes the int32 an absolute time rather than a duration.
    /// </remarks>
    public class RaidLock
    {
        #region AoMember Properties

        /// <summary>
        /// The playfield that is locked.
        /// </summary>
        /// <remarks>
        /// Its instance half is what the renderer passes to N3Msg_GetPFName.
        /// </remarks>
        [AoMember(0)]
        public Identity Playfield { get; set; }

        /// <summary>
        /// Which instance of it.
        /// </summary>
        /// <remarks>
        /// Printed as a bare number beside the playfield's name; nothing in
        /// the client resolves it further.
        /// </remarks>
        [AoMember(1)]
        public Identity Instance { get; set; }

        /// <summary>
        /// When the lock lifts, as an absolute time.
        /// </summary>
        [AoMember(2)]
        public int ExpiresAt { get; set; }

        #endregion
    }
}
