// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InventoryUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the InventoryUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Names ported from OmniCell's AOtomation.Messaging. OmniCell's entry type is InventoryEntry; this SDK
    // keeps InventorySlot (same 32 bytes). Old names kept as [Obsolete] aliases.
    [AoContract((int)N3MessageType.InventoryUpdate)]
    public class InventoryUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public InventoryUpdateMessage()
        {
            this.N3MessageType = N3MessageType.InventoryUpdate;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// How many slots the container has.
        /// </summary>
        [AoMember(0)]
        public int NumberOfSlots { get; set; }

        /// <summary>
        /// Whether things may be put into this container and taken out of it (1 CanAdd, 2 CanRemove).
        /// </summary>
        [AoMember(1)]
        public InventoryAccess Access { get; set; }

        /// <summary>
        /// The container's contents.
        /// </summary>
        [AoMember(2, SerializeSize = ArraySizeType.X3F1)]
        public InventorySlot[] Entries { get; set; }

        /// <summary>
        /// The bag.
        /// </summary>
        [AoMember(3)]
        public Identity BagIdentity { get; set; }

        /// <summary>
        /// Which slot of the owner's inventory the bag sits in.
        /// </summary>
        [AoMember(4)]
        public int SlotnumberInMainInventory { get; set; }

        /// <summary>
        /// Anything but zero opens the container's window.
        /// </summary>
        [AoMember(5)]
        public int Open { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is Access (InventoryAccess).")]
        public int Unknown2 { get => (int)this.Access; set => this.Access = (InventoryAccess)value; }

        [Obsolete("Wire field is Entries.")]
        public InventorySlot[] Items { get => this.Entries; set => this.Entries = value; }

        [Obsolete("Wire field is BagIdentity.")]
        public Identity InventoryIdentity { get => this.BagIdentity; set => this.BagIdentity = value; }

        /// <summary>Old name: the bag's slot number in the owner's main inventory.</summary>
        [Obsolete("Wire field is SlotnumberInMainInventory.")]
        public int Handle { get => this.SlotnumberInMainInventory; set => this.SlotnumberInMainInventory = value; }

        #endregion
    }
}
