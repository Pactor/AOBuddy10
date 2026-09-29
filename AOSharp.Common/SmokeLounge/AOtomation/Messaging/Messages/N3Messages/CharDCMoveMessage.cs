// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharDCMoveMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CharDCMoveMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Layout, names and doc comments ported from OmniCell's AOtomation.Messaging. OmniCell's MoveType is a
    // byte; this SDK keeps its MovementAction enum (same byte). Old AOSharp names kept as [Obsolete] aliases.
    [AoContract((int)N3MessageType.CharDCMove)]
    public class CharDCMoveMessage : N3Message
    {
        #region Constructors and Destructors

        public CharDCMoveMessage()
        {
            this.N3MessageType = N3MessageType.CharDCMove;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public MovementAction MoveType { get; set; }

        [AoMember(1)]
        public Quaternion Heading { get; set; }

        [AoMember(2)]
        public Vector3 Coordinates { get; set; }

        /// <summary>
        /// Milliseconds since the client last sent a movement message.
        /// </summary>
        /// <remarks>
        /// Measured, not guessed. Across 2,665 client movement packets in one
        /// captured session, this value matched the wall-clock gap since the
        /// previous movement packet to within sixty milliseconds in 97% of
        /// cases; the rest are where another message arrived in between and
        /// moved the reference. It is zero in a little over half of all copies,
        /// which is the client saying nothing has elapsed worth reporting.
        ///
        /// It is why a stop carries a smaller number than a start: a stop
        /// follows its own start within a few hundred milliseconds, and a start
        /// follows however long the player stood still.
        /// </remarks>
        [AoMember(3)]
        public int MillisecondsSincePreviousMove { get; set; }

        /// <summary>
        /// Radians about the world's Z axis, turned onto
        /// <see cref="Heading"/> before it reaches the character.
        /// </summary>
        /// <remarks>
        /// It is a float, and the client insists on it: the reader at
        /// 0x1006BEB8 takes this and <see cref="TiltLocalX"/> with the float
        /// operator at 0x101540CC and puts each through MSVCR100 _finite,
        /// failing the whole message if either is not a finite float.
        ///
        /// For move types 9 to 14 the dispatcher calls
        /// n3Dynel_t::VehicleForwardUpdate(const Vector3&amp;, const
        /// Quaternion&amp;, float, float) at 0x1006BE3D, which treats both as
        /// angles composed onto Heading. With both zero the composition is the
        /// heading itself, which is why the captures (all zero) look like a field
        /// that does nothing.
        /// </remarks>
        [AoMember(4)]
        public float TiltWorldZ { get; set; }

        /// <summary>
        /// Radians about the (1, 0, 0) axis rotated by <see cref="Heading"/>.
        /// See <see cref="TiltWorldZ"/>, which is read and used the same way.
        /// </summary>
        [AoMember(5)]
        public float TiltLocalX { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is Coordinates.")]
        public Vector3 Position { get => this.Coordinates; set => this.Coordinates = value; }

        [Obsolete("Wire field is MillisecondsSincePreviousMove.")]
        public int DeltaTime { get => this.MillisecondsSincePreviousMove; set => this.MillisecondsSincePreviousMove = value; }

        /// <summary>The raw 32 bits of <see cref="TiltWorldZ"/> (AOSharp read the float as an int).</summary>
        [Obsolete("Wire field is TiltWorldZ (float); this is its bit pattern.")]
        public int Unknown2 { get => BitConverter.SingleToInt32Bits(this.TiltWorldZ); set => this.TiltWorldZ = BitConverter.Int32BitsToSingle(value); }

        /// <summary>The raw 32 bits of <see cref="TiltLocalX"/> (AOSharp read the float as an int).</summary>
        [Obsolete("Wire field is TiltLocalX (float); this is its bit pattern.")]
        public int Unknown3 { get => BitConverter.SingleToInt32Bits(this.TiltLocalX); set => this.TiltLocalX = BitConverter.Int32BitsToSingle(value); }

        #endregion
    }
}
