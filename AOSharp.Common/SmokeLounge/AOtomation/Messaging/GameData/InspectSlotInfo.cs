// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NanoEffect.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the NanoEffect type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    public class InspectSlotInfo
    {
        /// <summary>The placement (slot) of the entry: OmniCell models Inspect's list as the standard container record, InventorySlot (placement, int16 flags, int16 count, Identity, ACGItem).</summary>
        [AoMember(0)]
        public EquipSlot EquipSlot { get; set; }

        /// <summary>Two int16s, the entry's Flags (high half) and Count (low half), read as one int.</summary>
        [AoMember(1)]
        public int Unk1 { get; set; }

        /// <summary>The item (OmniCell InventorySlot.Identity).</summary>
        [AoMember(2)]
        public Identity Identity { get; set; }

        /// <summary>The item's low template id.</summary>
        [AoMember(3)]
        public int ItemLowId { get; set; }

        /// <summary>The item's high template id.</summary>
        [AoMember(4)]
        public int ItemHighId { get; set; }

        /// <summary>The item's quality level.</summary>
        [AoMember(5)]
        public int Quality { get; set; }

        /// <summary>ACGItem's unused fourth word.</summary>
        [AoMember(6)]
        public int Unused { get; set; }

        #region Old AOSharp names (aliases, not on the wire)

        [System.Obsolete("Wire field is Identity (OmniCell InventorySlot).")]
        public Identity UniqueIdentity { get => this.Identity; set => this.Identity = value; }

        [System.Obsolete("Wire field is ItemLowId.")]
        public int LowId { get => this.ItemLowId; set => this.ItemLowId = value; }

        [System.Obsolete("Wire field is ItemHighId.")]
        public int HighId { get => this.ItemHighId; set => this.ItemHighId = value; }

        [System.Obsolete("Wire field is Quality.")]
        public int Ql { get => this.Quality; set => this.Quality = value; }

        [System.Obsolete("Wire field is Unused (ACGItem's fourth word).")]
        public int Unk2 { get => this.Unused; set => this.Unused = value; }

        #endregion
    }
}