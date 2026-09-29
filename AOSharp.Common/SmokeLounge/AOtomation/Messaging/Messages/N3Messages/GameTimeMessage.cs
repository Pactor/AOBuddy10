// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GameTimeMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the GameTimeMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Names ported from OmniCell's AOtomation.Messaging. AOSharp read SystemTimeReference (an int32) as a
    // float; the old names are kept as [Obsolete] aliases returning the same bits.
    [AoContract((int)N3MessageType.GameTime)]
    public class GameTimeMessage : N3Message
    {
        #region Constructors and Destructors

        public GameTimeMessage()
        {
            this.N3MessageType = N3MessageType.GameTime;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Where the clock is within the game day, in seconds.
        /// </summary>
        [AoMember(0)]
        public float CurrentGameTime { get; set; }

        /// <summary>
        /// Which part of the day it is (0 Dawn, 1 Day, 2 Dusk, 3 Night).
        /// </summary>
        [AoMember(1)]
        public SmokeLounge.AOtomation.Messaging.GameData.DayPeriod DayPeriod { get; set; }

        /// <summary>
        /// The game day number.
        /// </summary>
        [AoMember(2)]
        public int CurrentGameDay { get; set; }

        /// <summary>
        /// The system-time reference the client advances the clock against. An int32; what it is
        /// measured in is not settled (OmniCell GameTimeMessage).
        /// </summary>
        [AoMember(3)]
        public int SystemTimeReference { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is DayPeriod.")]
        public int Unknown2 { get => (int)this.DayPeriod; set => this.DayPeriod = (SmokeLounge.AOtomation.Messaging.GameData.DayPeriod)value; }

        /// <summary>The bits of <see cref="SystemTimeReference"/> read as a float, as AOSharp did.</summary>
        [Obsolete("Wire field is SystemTimeReference (int32), not a float.")]
        public float Unknown4 { get => BitConverter.Int32BitsToSingle(this.SystemTimeReference); set => this.SystemTimeReference = BitConverter.SingleToInt32Bits(value); }

        #endregion
    }
}
