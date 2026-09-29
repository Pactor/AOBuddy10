// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InfoPacketMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the InfoPacketMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The reply to an InfoRequest (examine): a flags byte and one <see cref="InfoPacket"/> record.
    /// </summary>
    /// <remarks>
    /// Since 2026-09-29 (after OmniCell, Messages\N3Messages\InfoPacketMessage.cs) the record is read for every
    /// flag combination by <see cref="Serialization.Serializers.Custom.InfoPacketSerializer"/>, registered for
    /// <see cref="InfoPacket"/>. The AOSharp original matched <see cref="Type"/> against seven whole values and read
    /// nothing at all for monsters (0x50) - every mission-NPC examine left its 43-byte record, HP included, unread.
    /// </remarks>
    [AoContract((int)N3MessageType.InfoPacket)]
    public class InfoPacketMessage : N3Message
    {
        #region Constructors and Destructors

        public InfoPacketMessage()
        {
            this.N3MessageType = N3MessageType.InfoPacket;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The flags byte. It is a set of bits (<see cref="Flags"/>), not seven whole values: any combination is a
        /// legal message.
        /// </summary>
        [AoMember(0)]
        [AoFlags(InfoPacketFlagsKey)]
        public InfoPacketType Type { get; set; }

        /// <summary>
        /// The record: a <see cref="CharacterInfoPacket"/> for players, <see cref="TowerInfoPacket"/> for towers,
        /// <see cref="MonsterInfoPacket"/> for every other non-player.
        /// </summary>
        [AoMember(1)]
        public InfoPacket Info { get; set; }

        #endregion

        /// <summary>
        /// <see cref="Type"/> as the bit set it really is.
        /// </summary>
        public InfoPacketFlags Flags { get => (InfoPacketFlags)this.Type; set => this.Type = (InfoPacketType)value; }

        /// <summary>
        /// The serialization-context key the flags byte is published under for the record serializer.
        /// </summary>
        public const string InfoPacketFlagsKey = "infoPacketFlags";
    }
}
