// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SuppressionLevel.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SuppressionLevel type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// How much of a district's suppression field is up.
    /// </summary>
    /// <remarks>
    /// The client's own names, and the order is worth reading twice: zero is
    /// the most suppression and four is none at all. Gamecode.dll 0x1003F4A3
    /// switches on the value and loads one of five strings for it -
    /// SuppressionField100, SuppressionField75, SuppressionField25,
    /// SuppressionField5, SuppressionField0 - and prints the line that goes
    /// with it.
    ///
    /// The range is enforced twice over. FightModeChange_t's reader refuses a
    /// level outside 0 to 4 on a change that sets one, and outside -4 to 4 on a
    /// change that adds one; and the routine that works a district's level out,
    /// at 0x1011FDDD, clamps the total to 0 through 4 whatever the changes say.
    /// </remarks>
    /// <remarks>
    /// One byte on the wire, and read signed by the client - a change that adds
    /// rather than sets may carry down to -4. The values named here are the
    /// five a district can end up at; an additive change's own value is a
    /// difference and will not be one of them.
    /// </remarks>
    public enum SuppressionLevel : byte
    {
        /// <summary>
        /// SuppressionField100 - the field is fully up.
        /// </summary>
        Full = 0,

        /// <summary>
        /// SuppressionField75
        /// </summary>
        SeventyFive = 1,

        /// <summary>
        /// SuppressionField25
        /// </summary>
        TwentyFive = 2,

        /// <summary>
        /// SuppressionField5
        /// </summary>
        Five = 3,

        /// <summary>
        /// SuppressionField0 - no field at all.
        /// </summary>
        None = 4
    }
}
