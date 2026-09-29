// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OperatorMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the OperatorMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Server statistics, sent unasked (packet type 0x0E).
    /// </summary>
    /// <remarks>
    /// The AOSharp original had no fields and left the whole body unread. Since 2026-09-29 the body is carried as
    /// <see cref="Payload"/>, after OmniCell (Messages\OperatorMessage.cs:21-83). Every server copy is 532 bytes of
    /// body: version 11, the sender again, then three groups of [a counted array of ten int32s, another of ten
    /// int32s, ten floats around 6-10, a float Unix timestamp, a seven-dword record] and an eight-dword tail -
    /// sampled server-load series. Nothing a player does depends on it, so it is carried rather than decoded.
    ///
    /// Read by <see cref="Serialization.Serializers.Custom.OperatorMessageSerializer"/>, which takes every byte left
    /// in the packet rather than OmniCell's fixed 532: the client's own OperatorMessage is 2 bytes of body, and a
    /// fixed length would throw on it.
    /// </remarks>
    [AoContract((int)PacketType.OperatorMessage)]
    public class OperatorMessage : MessageBody
    {
        #region Public Properties

        public override PacketType PacketType
        {
            get
            {
                return PacketType.OperatorMessage;
            }
        }

        /// <summary>
        /// The whole body, carried rather than read (532 bytes on every server copy).
        /// </summary>
        public byte[] Payload { get; set; } = new byte[0];

        #endregion
    }
}
