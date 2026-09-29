// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleItemFullUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SimpleItemFullUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.SimpleItemFullUpdate)]
    public class SimpleItemFullUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public SimpleItemFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.SimpleItemFullUpdate;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public int Unknown1 { get; set; }

        [AoMember(1)]
        public int OwnerType { get; set; }

        /// <summary>
        /// The instance half of the holder identity, and the gate on the position below.
        /// </summary>
        /// <remarks>
        /// Gated on the instance, not the type, since 2026-09-29, as OmniCell does
        /// (Messages\N3Messages\SimpleItemFullUpdateMessage.cs:86-106): across 690 captured copies of its vending
        /// machine relative, 281 carry a position and exactly 281 have a zero instance, while 362 have a zero type.
        /// </remarks>
        [AoFlags("sifuHolderInstance")]
        [AoMember(2)]
        public int OwnerInstance { get; set; }

        [AoUsesFlags("sifuHolderInstance", typeof(Vector3), FlagsCriteria.HasNone, int.MaxValue)]
        [AoMember(3)]
        public Vector3? Position { get; set; }

        [AoUsesFlags("sifuHolderInstance", typeof(Quaternion), FlagsCriteria.HasNone, int.MaxValue)]
        [AoMember(4)]
        public Quaternion? Rotation { get; set; }

        [AoMember(5)]
        public int PlayfieldId { get; set; }

        [AoMember(6)]
        public Identity StateMachine { get; set; }

        /// <summary>
        /// InventoryId (high byte, stat 55) and BodyLocation (low byte, stat 220) as one short; see
        /// <see cref="InventoryId"/> and <see cref="BodyLocation"/>.
        /// </summary>
        [AoMember(7)]
        public short Unknown2 { get; set; }

        [AoMember(8, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<Stat, int>[] Stats { get; set; }

        /// <summary>
        /// The item's own name, Int32-counted including its terminator; empty (count 0) on most items.
        /// </summary>
        /// <remarks>
        /// Added 2026-09-29 from OmniCell (SimpleItemFullUpdateMessage.cs:161); the AOSharp original stopped at the
        /// stats and left these bytes unread on every item. Mission keys carry the door they open, e.g. "Mission
        /// key to an entrance to some underground tubes".
        /// </remarks>
        [AoMember(9, SerializeSize = ArraySizeType.Int32)]
        public string Name { get; set; }

        #endregion

        #region Convenience

        /// <summary>
        /// Stat 55, inventoryid: the high byte of <see cref="Unknown2"/>.
        /// </summary>
        public byte InventoryId => (byte)((ushort)this.Unknown2 >> 8);

        /// <summary>
        /// Stat 220, currbodylocation: the low byte of <see cref="Unknown2"/>.
        /// </summary>
        public byte BodyLocation => (byte)this.Unknown2;

        #endregion
    }
}