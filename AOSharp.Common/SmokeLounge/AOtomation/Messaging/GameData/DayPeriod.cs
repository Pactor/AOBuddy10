// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DayPeriod.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the DayPeriod type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// Which part of the day it is, as GameTime carries it.
    /// </summary>
    /// <remarks>
    /// The client's own DayPeriod_e. GameTime's dispatcher at Gamecode
    /// 0x10039800 hands the second of the message's four fields to
    /// GameTime_t::Update(float, DayPeriod_e, int, int) as that parameter, and
    /// the four values below are what the enum holds.
    /// </remarks>
    public enum DayPeriod
    {
        /// <summary>
        /// Dawn.
        /// </summary>
        Dawn = 0,

        /// <summary>
        /// Day.
        /// </summary>
        Day = 1,

        /// <summary>
        /// Dusk.
        /// </summary>
        Dusk = 2,

        /// <summary>
        /// Night.
        /// </summary>
        Night = 3
    }
}
