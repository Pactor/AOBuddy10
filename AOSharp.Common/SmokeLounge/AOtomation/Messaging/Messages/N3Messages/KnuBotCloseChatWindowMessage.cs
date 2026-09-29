// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KnuBotCloseChatWindowMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the KnuBotCloseChatWindowMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.KnubotCloseChatWindow)]
    public class KnuBotCloseChatWindowMessage : N3Message
    {
        #region Constructors and Destructors

        public KnuBotCloseChatWindowMessage()
        {
            this.N3MessageType = N3MessageType.KnubotCloseChatWindow;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The KnuBot protocol version, 2 in every captured copy.
        /// </summary>
        /// <remarks>
        /// Not this message's own field: KnubotBaseIIR_c reads and checks it,
        /// so every KnuBot message carries it in front of its own body.
        /// </remarks>
        [AoMember(0)]
        public short Version { get; set; }

        [AoMember(1)]
        public Identity Target { get; set; }

        [AoMember(2)]
        public int Seconds { get; set; }

        /// <summary>
        /// Why the window is closing, when the server says why.
        /// </summary>
        /// <remarks>
        /// This was Unknown3, an integer, and it was the length of a string
        /// nothing read: "You are too far away from ICC Immigration Officer Bill
        /// to continue this conversation." is eighty five characters and the
        /// integer is eighty five. Sixty of the eighty two captured copies
        /// carry no message and a length of zero, which is why the missing
        /// field went unnoticed.
        ///
        /// No terminator here: the text ends on its full stop and the length
        /// counts exactly the characters.
        /// </remarks>
        [AoMember(3, SerializeSize = ArraySizeType.Int32)]
        public string Reason { get; set; }

        /// <summary>Old name: it was the length prefix of <see cref="Reason"/>.</summary>
        [System.Obsolete("Wire field is Reason (Int32-length string); this returns its length.")]
        public int Unknown3 { get => this.Reason?.Length ?? 0; set { if (value == 0) this.Reason = string.Empty; } }

        #endregion
    }
}