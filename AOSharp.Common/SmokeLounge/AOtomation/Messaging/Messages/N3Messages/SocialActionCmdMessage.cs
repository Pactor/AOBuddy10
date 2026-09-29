// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SocialActionCmdMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SocialActionCmdMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Names ported from OmniCell's AOtomation.Messaging (social-action-cmd.html): AOSharp's four bytes
    // Unknown1-4 are one int32, the n3Command_t Verification; Unknown5 is CommandReference. Old names are
    // kept as [Obsolete] aliases on the same bytes (Unknown1 is the most significant byte).
    [AoContract((int)N3MessageType.SocialActionCmd)]
    public class SocialActionCmdMessage : N3Message
    {
        #region Constructors and Destructors

        public SocialActionCmdMessage()
        {
            this.N3MessageType = N3MessageType.SocialActionCmd;
        }

        #endregion

        #region AoMember Properties

        /// <summary>The n3Command_t verification state.</summary>
        [AoMember(0)]
        public int Verification { get; set; }

        [AoMember(1)]
        public int CommandReference { get; set; }

        /// <summary>The emote (the client accepts ids 1-71).</summary>
        [AoMember(2)]
        public SocialAction Action { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [System.Obsolete("Wire field is Verification (int32); this is its first (most significant) byte.")]
        public byte Unknown1 { get => (byte)(this.Verification >> 24); set => this.Verification = (this.Verification & 0x00FFFFFF) | (value << 24); }

        [System.Obsolete("Wire field is Verification (int32); this is its second byte.")]
        public byte Unknown2 { get => (byte)(this.Verification >> 16); set => this.Verification = (int)((this.Verification & 0xFF00FFFF) | (uint)(value << 16)); }

        [System.Obsolete("Wire field is Verification (int32); this is its third byte.")]
        public byte Unknown3 { get => (byte)(this.Verification >> 8); set => this.Verification = (int)((this.Verification & 0xFFFF00FF) | (uint)(value << 8)); }

        [System.Obsolete("Wire field is Verification (int32); this is its last byte.")]
        public byte Unknown4 { get => (byte)this.Verification; set => this.Verification = (int)((this.Verification & 0xFFFFFF00) | value); }

        [System.Obsolete("Wire field is CommandReference.")]
        public int Unknown5 { get => this.CommandReference; set => this.CommandReference = value; }

        #endregion
    }
}
