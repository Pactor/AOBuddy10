// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PingMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PingMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)PacketType.PingMessage)]
    public class PingMessage : MessageBody
    {
        #region Public Properties

        public override PacketType PacketType
        {
            get
            {
                return PacketType.PingMessage;
            }
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public PingMessageType PingMessageType { get; set; }

        /// <summary>Hop count; 0 in every recorded copy.</summary>
        [AoMember(1)]
        public int HopCount { get; set; }

        /// <summary>The originator's time stamp (OmniCell OriginatorStamp; names from MessageProtocol.dll exports).</summary>
        [AoMember(2)]
        public uint OriginatorStamp { get; set; }

        /// <summary>When the reply's sender received the request (OmniCell ReceiveStamp). A real client's reply carries it set, equal to TransmitStamp; the server's request carries 0.</summary>
        [AoMember(3)]
        public uint ReceiveStamp { get; set; }

        /// <summary>When the reply was sent (OmniCell TransmitStamp).</summary>
        [AoMember(4)]
        public uint TransmitStamp { get; set; }

        /// <summary>The ping sequence number (OmniCell Sequence).</summary>
        [AoMember(5)]
        public uint Sequence { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [System.Obsolete("Wire field is OriginatorStamp.")]
        public uint ServerTime { get => this.OriginatorStamp; set => this.OriginatorStamp = value; }

        [System.Obsolete("Wire field is ReceiveStamp.")]
        public uint UpTime1 { get => this.ReceiveStamp; set => this.ReceiveStamp = value; }

        [System.Obsolete("Wire field is TransmitStamp.")]
        public uint UpTime2 { get => this.TransmitStamp; set => this.TransmitStamp = value; }

        [System.Obsolete("Wire field is Sequence.")]
        public uint Unk2 { get => this.Sequence; set => this.Sequence = value; }

        #endregion
    }
}