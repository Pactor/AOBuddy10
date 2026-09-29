// --------------------------------------------------------------------------------------------------------------------
// <copyright file="N3TeleportMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the N3TeleportMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.N3Teleport)]
    public class N3TeleportMessage : N3Message
    {
        #region Constructors and Destructors

        public N3TeleportMessage()
        {
            this.N3MessageType = N3MessageType.N3Teleport;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public Vector3 Destination { get; set; }

        [AoMember(1)]
        public Quaternion Heading { get; set; }

        [AoMember(2)]
        public byte Unknown1 { get; set; }

        [AoMember(3)]
        public Identity Playfield { get; set; }

        [AoMember(4)]
        public int GameServerId { get; set; }

        [AoMember(5)]
        public int SgId { get; set; }

        [AoMember(6)]
        public Identity ChangePlayfield { get; set; }

        [AoMember(7)]
        public int Unknown4 { get; set; }

        [AoMember(8)]
        public int Unknown5 { get; set; }

        [AoMember(9)]
        public Identity Playfield2 { get; set; }

        /// <summary>
        /// A trailing block behind its own int32 byte count.
        /// </summary>
        /// <remarks>
        /// Ported 2026-09-29 from OmniCell (Messages\N3Messages\N3TeleportMessage.cs:90-108). The AOSharp original
        /// read the count as <c>Unknown6</c> and dropped the bytes after it (16 or 12 on every server copy). The
        /// count always equals the bytes left in the message and is a multiple of four. OmniCell calls the
        /// contents "not settled"; the AOBuddy mission recordings show:
        ///   16 bytes (243 copies): three floats then int32 1 - the outdoor mission-entrance position on the exit
        ///      teleport (e.g. trailer (446.7, 1.2, 1502.5) for "Holes In the Wall (447,1503)"), or, on the two
        ///      in-mission teleports, the destination repeated then int32 0;
        ///   12 bytes (8 copies, pf 655): three floats (3231, 36, 915), not identified.
        /// See <see cref="TrailerPosition"/> and <see cref="TrailerFlag"/>.
        /// </remarks>
        [AoMember(10, SerializeSize = ArraySizeType.Int32)]
        public byte[] Trailer { get; set; } = new byte[0];

        #endregion

        #region Compatibility and convenience

        /// <summary>
        /// The old name of the trailer's byte count (what the AOSharp original read here). Setting it resizes
        /// <see cref="Trailer"/>.
        /// </summary>
        public int Unknown6
        {
            get => this.Trailer?.Length ?? 0;
            set
            {
                if (this.Trailer == null || this.Trailer.Length != value)
                {
                    var resized = new byte[value];
                    if (this.Trailer != null)
                    {
                        System.Array.Copy(this.Trailer, resized, System.Math.Min(value, this.Trailer.Length));
                    }

                    this.Trailer = resized;
                }
            }
        }

        /// <summary>
        /// The three big-endian floats at the start of <see cref="Trailer"/> when it holds at least 12 bytes -
        /// on a mission exit teleport, the outdoor mission-entrance position; null otherwise.
        /// </summary>
        public Vector3? TrailerPosition
        {
            get
            {
                if (this.Trailer == null || this.Trailer.Length < 12)
                {
                    return null;
                }

                return new Vector3(ReadFloat(this.Trailer, 0), ReadFloat(this.Trailer, 4), ReadFloat(this.Trailer, 8));
            }
        }

        /// <summary>
        /// The big-endian int32 after <see cref="TrailerPosition"/> in the 16-byte form (1 on exit teleports, 0 on
        /// in-mission ones); null when the trailer is shorter.
        /// </summary>
        public int? TrailerFlag
        {
            get
            {
                if (this.Trailer == null || this.Trailer.Length < 16)
                {
                    return null;
                }

                return (this.Trailer[12] << 24) | (this.Trailer[13] << 16) | (this.Trailer[14] << 8) | this.Trailer[15];
            }
        }

        private static float ReadFloat(byte[] bytes, int offset)
        {
            var b = new[] { bytes[offset + 3], bytes[offset + 2], bytes[offset + 1], bytes[offset] };
            return System.BitConverter.ToSingle(b, 0);
        }

        #endregion
    }
}