// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CityHouse.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CityHouse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One house in the city payload PlayfieldAllCities carries.
    /// </summary>
    /// <remarks>
    /// This is BaseClientHouseData_t. The message itself only counts the payload
    /// bytes and hands them to
    /// PlayfieldCityHolderClient_c::UpdateNewHouses(BinaryStream&amp;, bool) at
    /// city.dll 0x10016317, which is where the shape below comes from: a uint32
    /// count, then this record that many times.
    ///
    /// Twenty five bytes each, and none of it conditional.
    /// </remarks>
    public class CityHouse
    {
        #region AoMember Properties

        /// <summary>
        /// Where the house stands, as a TilePos_c.
        /// </summary>
        /// <remarks>
        /// Three floats, read by city.dll 0x1000DE03. The client divides two of
        /// them down to work out which tile they fall in.
        /// </remarks>
        [AoMember(0)]
        public Vector3 Position { get; set; }

        /// <summary>
        /// Which house to build there.
        /// </summary>
        /// <remarks>
        /// AddNewHouse passes this and Identity together to the template lookup
        /// and gets back a CityHouseTemplate_c, which it then asks for its
        /// number of visuals. So it selects the house, not an instance of one.
        /// </remarks>
        [AoMember(1)]
        public int Template { get; set; }

        /// <summary>
        /// Whether this house is already being torn down.
        /// </summary>
        /// <remarks>
        /// One byte on the wire, kept as a boolean - city.dll 0x1000DDAE reads
        /// it and stores whether it equals 1.
        ///
        /// What it means is settled by the only other place that writes the same
        /// field: PlayfieldCityHolderClient_c::SlotPlayfieldHouseDemolishStarted
        /// at 0x10016AB7 finds the house standing on a tile and sets this byte
        /// to 1. So it is carried here for the sake of a client arriving in a
        /// playfield where a demolition is already under way.
        /// </remarks>
        [AoMember(2)]
        public bool DemolitionStarted { get; set; }

        /// <summary>
        /// The house itself, as a thing that can be interacted with.
        /// </summary>
        [AoMember(3)]
        public Identity Identity { get; set; }

        #endregion
    }
}
