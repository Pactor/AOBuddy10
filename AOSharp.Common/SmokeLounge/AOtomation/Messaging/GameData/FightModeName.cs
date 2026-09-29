// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FightModeName.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the FightModeName type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using System;
    /// <summary>
    /// One change to one district's suppression field (OmniCell: FightModeUpdateEntry).
    /// </summary>
    /// <remarks>
    /// The client calls it FightModeChange_t, in its own words: "Invalid
    /// FightModeChange_t stream" is what its reader prints when it refuses one.
    /// </remarks>
    public class FightModeName
    {
        #region AoMember Properties

        /// <summary>
        /// The change's own id, so it can be taken out again later.
        /// </summary>
        /// <remarks>
        /// A change carrying <see cref="FightModeChangeFlags.ById"/> is matched
        /// against the district's existing changes on this value - 0x1011FE65
        /// walks the list comparing it - rather than being added by district
        /// name.
        /// </remarks>
        [AoMember(0)]
        public int Id { get; set; }

        /// <summary>
        /// Which district, by name (e.g. "Aprils Rock Offense").
        /// </summary>
        /// <remarks>
        /// A name and not a number: the dispatcher hands it straight to
        /// GameData's PlayfieldDistrictInfo_t::GetDistrictData, the overload
        /// that takes a string.
        /// </remarks>
        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string District { get; set; }

        /// <summary>
        /// Whether the change sets or adds, whether it overrides, and whether
        /// it is putting a change in or taking one out.
        /// </summary>
        [AoMember(2)]
        public FightModeChangeFlags Flags { get; set; }

        /// <summary>
        /// How much suppression, or how much to add to it (0 = full field ... 4 = none).
        /// </summary>
        /// <remarks>
        /// Signed, and one byte wide. A change that sets carries 0 to 4; one
        /// that adds may carry -4 to 4, and the reader enforces both.
        /// </remarks>
        [AoMember(3)]
        public SuppressionLevel Level { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [System.Obsolete("Wire field is Id.")]
        public int Unknown1 { get => this.Id; set => this.Id = value; }

        [System.Obsolete("Wire field is District.")]
        public string Text { get => this.District; set => this.District = value; }

        [System.Obsolete("Wire field is Flags (FightModeChangeFlags).")]
        public byte Unknown2 { get => (byte)this.Flags; set => this.Flags = (FightModeChangeFlags)value; }

        [System.Obsolete("Wire field is Level (SuppressionLevel).")]
        public byte Unknown3 { get => (byte)this.Level; set => this.Level = (SuppressionLevel)value; }

        #endregion
    }
}
