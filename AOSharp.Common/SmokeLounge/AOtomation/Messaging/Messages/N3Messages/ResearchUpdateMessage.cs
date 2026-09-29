// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResearchUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ResearchUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Layout and names ported from OmniCell's AOtomation.Messaging: the list is null-terminated (an entry
    // whose ResearchId is 0 ends it), not a fixed 54. AOSharp declared AoMember(1) twice and read the
    // terminator as Unknown2. Old names kept as [Obsolete] aliases.
    [AoContract((int)N3MessageType.ResearchUpdate)]
    public class ResearchUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public ResearchUpdateMessage()
        {
            this.N3MessageType = N3MessageType.ResearchUpdate;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Required research-update format marker. It is 1 in all captured
        /// copies; the current client reads and discards it for compatibility.
        /// </summary>
        [AoMember(0)]
        public byte FormatMarker { get; set; }

        /// <summary>
        /// The research lines, ended by a ResearchId of zero (the int32 0 is consumed as the terminator).
        /// </summary>
        [AoMember(1, SerializeSize = ArraySizeType.NullTerminated)]
        public ResearchLine[] Entries { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is FormatMarker.")]
        public byte Unknown1 { get => this.FormatMarker; set => this.FormatMarker = value; }

        [Obsolete("Wire field is Entries (null-terminated, not a fixed 54).")]
        public ResearchLine[] ResearchLine { get => this.Entries; set => this.Entries = value; }

        /// <summary>The terminator AOSharp read as a field; always 0.</summary>
        [Obsolete("This was the list terminator (ResearchId 0).")]
        public int Unknown2 { get => 0; set { } }

        #endregion
    }
}
