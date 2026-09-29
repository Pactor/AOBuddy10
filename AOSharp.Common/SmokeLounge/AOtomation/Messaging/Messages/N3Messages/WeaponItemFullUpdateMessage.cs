// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WeaponItemFullUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the WeaponItemFullUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A weapon item: byte-for-byte a SimpleItemFullUpdate.
    /// </summary>
    /// <remarks>
    /// Layout and names ported from OmniCell's AOtomation.Messaging (WeaponItemFullUpdateMessage), which
    /// reads it exactly as SimpleItemFullUpdate, including the position block that is present only when
    /// the owner's instance is 0 (a weapon lying on the ground). AOSharp had no position branch, read the
    /// inventory-id/body-location pair as one short, and read the Name length as "Playfield". OmniCell
    /// calls the playfield int "Playfield"; this SDK keeps its name PlayfieldId because its old
    /// "Playfield" (the name length) is kept as an alias. Old names are [Obsolete] aliases.
    /// </remarks>
    [AoContract((int)N3MessageType.WeaponItemFullUpdate)]
    public class WeaponItemFullUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public WeaponItemFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.WeaponItemFullUpdate;
        }

        #endregion

        #region AoMember Properties

        /// <summary>The item-message version, 11 (the client refuses anything else).</summary>
        [AoMember(0)]
        public int MsgVersion { get; set; }

        /// <summary>Type half of the owner (holder) identity; see <see cref="Owner"/>.</summary>
        [AoMember(1)]
        public int OwnerType { get; set; }

        /// <summary>Instance half of the owner. Coordinate and Heading are on the wire only when this is 0.</summary>
        [AoFlags("wifuowner")]
        [AoMember(2)]
        public int OwnerInstance { get; set; }

        /// <summary>Where the weapon lies; present only when the owner instance is 0.</summary>
        [AoUsesFlags("wifuowner", typeof(Vector3), FlagsCriteria.EqualsToAny, new[] { 0 })]
        [AoMember(3)]
        public Vector3? Coordinate { get; set; }

        /// <summary>Its heading; present only when the owner instance is 0.</summary>
        [AoUsesFlags("wifuowner", typeof(Quaternion), FlagsCriteria.EqualsToAny, new[] { 0 })]
        [AoMember(4)]
        public Quaternion? Heading { get; set; }

        /// <summary>The playfield (OmniCell: Playfield).</summary>
        [AoMember(5)]
        public int PlayfieldId { get; set; }

        /// <summary>The shared item-message marker, 1000015:0.</summary>
        [AoMember(6)]
        public Identity StateMachine { get; set; }

        /// <summary>Stat 55, inventoryid.</summary>
        [AoMember(7)]
        public byte InventoryId { get; set; }

        /// <summary>Stat 220, currbodylocation (e.g. 6 = right hand, 8 = left hand).</summary>
        [AoMember(8)]
        public byte BodyLocation { get; set; }

        [AoMember(9, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<Stat, int>[] Stats { get; set; }

        /// <summary>
        /// The item's name (OmniCell reads it Int32Terminated; this reader's ReadString trims the NUL,
        /// so Int32 reads the same bytes). Empty in every recorded copy.
        /// </summary>
        [AoMember(10, SerializeSize = ArraySizeType.Int32)]
        public string Name { get; set; }

        #endregion

        #region Convenience and old AOSharp names (not on the wire)

        /// <summary>The owner (holder) identity: OwnerType:OwnerInstance. Same value AOSharp's Owner member read.</summary>
        public Identity Owner
        {
            get => new Identity((IdentityType)this.OwnerType, this.OwnerInstance);
            set { this.OwnerType = (int)value.Type; this.OwnerInstance = value.Instance; }
        }

        [Obsolete("Wire field is MsgVersion.")]
        public int Unknown1 { get => this.MsgVersion; set => this.MsgVersion = value; }

        /// <summary>Old two-byte view: InventoryId in the high byte, BodyLocation in the low byte (wire order).</summary>
        [Obsolete("Wire fields are InventoryId and BodyLocation.")]
        public short Unknown2
        {
            get => (short)((this.InventoryId << 8) | this.BodyLocation);
            set { this.InventoryId = (byte)((value >> 8) & 0xFF); this.BodyLocation = (byte)(value & 0xFF); }
        }

        /// <summary>AOSharp's "Playfield" was the length prefix of <see cref="Name"/> (0 = empty).</summary>
        [Obsolete("This AOSharp name was the Name length prefix. The playfield is PlayfieldId.")]
        public int Playfield
        {
            get => string.IsNullOrEmpty(this.Name) ? 0 : this.Name.Length + 1;
            set { if (value == 0) this.Name = string.Empty; }
        }

        #endregion
    }
}
