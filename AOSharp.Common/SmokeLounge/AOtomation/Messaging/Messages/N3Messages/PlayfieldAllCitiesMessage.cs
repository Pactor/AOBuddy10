// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlayfieldAllTowersMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PlayfieldAllTowersMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Sent on entering a playfield, listing the player cities in it.
    /// </summary>
    /// <remarks>
    /// Ported from OmniCell. On the wire the body is a two-byte count of payload BYTES, not of
    /// houses, followed by that many bytes. Zero means nothing follows at all - not a list of no
    /// houses, but no list: the uint32 house count inside the payload is absent too. The message
    /// reader never looks inside the payload; it hands the bytes to
    /// PlayfieldCityHolderClient_c::UpdateNewHouses (city.dll 0x10016317): a uint32 count, then a
    /// 25-byte CityHouse that many times. OmniCell reads it with a custom serializer; here the byte
    /// count gates the house list. AOSharp read only the short (Unknown1), which breaks in any
    /// playfield that has a city. Every recorded copy (525+) carries 0.
    /// </remarks>
    [AoContract((int)N3MessageType.PlayfieldAllCities)]
    public class PlayfieldAllCitiesMessage : N3Message
    {
        #region Constructors and Destructors

        public PlayfieldAllCitiesMessage()
        {
            this.N3MessageType = N3MessageType.PlayfieldAllCities;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Byte length of the city payload that follows (4 + 25 per house), or 0 for none.
        /// </summary>
        [AoFlags("citypayload")]
        [AoMember(0)]
        public short PayloadLength { get; set; }

        /// <summary>
        /// The houses standing in this playfield, or null when there are none.
        /// </summary>
        [AoUsesFlags("citypayload", typeof(CityHouse[]), FlagsCriteria.HasAny, new[] { 0xFFFF })]
        [AoMember(1, SerializeSize = ArraySizeType.Int32)]
        public CityHouse[] Houses { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is PayloadLength (byte count of the Houses payload).")]
        public short Unknown1 { get => this.PayloadLength; set => this.PayloadLength = value; }

        #endregion
    }
}
