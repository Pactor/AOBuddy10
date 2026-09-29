// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TilePos.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TilePos type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (GameData\TilePos.cs); no changes.

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A square on a city's grid.
    /// </summary>
    /// <remarks>
    /// city.dll's TilePos_c, and two int32s: its stream reader at 0x1001B7A3
    /// takes one into +0 and one into +4 with BinaryStream's int operator, and
    /// its constructor is TilePos_c(int, int). The city messages that carry a
    /// tile - ClientRequestBuild and ClientRequestDemolish - use that operator
    /// through the import at Gamecode 0x101553B0.
    /// </remarks>
    public class TilePos
    {
        #region AoMember Properties

        /// <summary>
        /// The first of the pair, at TilePos_c's +0.
        /// </summary>
        [AoMember(0)]
        public int X { get; set; }

        /// <summary>
        /// The second, at +4.
        /// </summary>
        [AoMember(1)]
        public int Z { get; set; }

        #endregion
    }
}
