// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VendingMachineFullUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the VendingMachineFullUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using System.Linq;
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Layout and names ported from OmniCell's AOtomation.Messaging (verified against every recorded packet).
    // AOSharp's tail names were one field off; the old names are kept below as aliases with the SAME data
    // they returned before (so AOSharp's TailVersion still returns LockDifficulty).
    [AoContract((int)N3MessageType.VendingMachineFullUpdate)]
    public class VendingMachineFullUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public VendingMachineFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.VendingMachineFullUpdate;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// A version, 11, that the client refuses the message without.
        /// </summary>
        /// <remarks>
        /// This message is a SimpleItemFullUpdate with a tail on it - its
        /// reader calls SimpleItemFullUpdateIIR_t::ReadSubClass at Gamecode.dll
        /// 0x100A1661 and then reads four more fields - so everything down to
        /// Name below is that message, field for field, and this is its version
        /// check against the static at 0x101C2160, which holds 11.
        /// </remarks>
        [AoMember(0)]
        public int TypeIdentifier { get; set; }

        /// <summary>
        /// Type half of the NPC (shopkeeper) identity that holds this inventory; see <see cref="NpcIdentity"/>.
        /// </summary>
        [AoMember(1)]
        public int OwnerType { get; set; }

        /// <summary>
        /// Instance half of the NPC identity. Coordinates and Heading are present only when this is zero.
        /// </summary>
        [AoFlags("OwnerInstance")]
        [AoMember(2)]
        public int OwnerInstance { get; set; }

        /// <summary>
        /// Present only when NpcIdentity.Instance is zero.
        /// </summary>
        /// <remarks>
        /// The instance, not the type beside it, and this message is where
        /// that was decided.
        ///
        /// Coordinates appear in exactly 281 of 690 copies, which is exactly
        /// the number whose NpcIdentity.Instance is zero. 362 have a zero type,
        /// and that is not the same set - so the two rules disagree about 81
        /// copies, and those 81 settle it. Read one of them with the type: a
        /// captured message with type 0 and instance 0x57371D is 143 bytes,
        /// which is this layout with no position in it and twenty eight bytes
        /// short of what it gives with one, and parsing it the other way puts
        /// 0xD8CC0000 where the marker constant has to be.
        /// </remarks>
        [AoUsesFlags("OwnerInstance", typeof(Vector3), FlagsCriteria.EqualsToAny, new[] { 0 })]
        [AoMember(3)]
        public Vector3? Position { get; set; }

        /// <summary>
        /// Heading; present only when NpcIdentity.Instance is zero (see <see cref="Position"/>).
        /// </summary>
        [AoUsesFlags("OwnerInstance", typeof(Quaternion), FlagsCriteria.EqualsToAny, new[] { 0 })]
        [AoMember(4)]
        public Quaternion? Rotation { get; set; }

        [AoMember(5)]
        public int PlayfieldId { get; set; }

        /// <summary>
        /// The shared item-message constant and its always-zero instance (1000015:0).
        /// </summary>
        /// <remarks>
        /// The same Identity SimpleItemFullUpdate carries. See OmniCell's
        /// ItemMessageConstants: the client reads it and never reads it back.
        /// </remarks>
        [AoMember(6)]
        public Identity StateMachine { get; set; }

        /// <summary>
        /// Two bytes: the inventory id and the body location.
        /// </summary>
        /// <remarks>
        /// Modelled as a short because that is how it was found, and it stays
        /// one because the two bytes have never been anything but 0 and a
        /// placement - 64, 65 and 111 across 690 captured copies, which is 0x40,
        /// 0x41 and 0x6F with a zero high byte.
        ///
        /// They are the same pair SimpleItemFullUpdate carries, and the same
        /// code reads them: Gamecode.dll 0x100A1B83 sign-extends the first into
        /// stat 55, inventoryid, and 0x100A1B94 the second into stat 220,
        /// currbodylocation.
        /// </remarks>
        [AoMember(7)]
        public short InventoryIdAndBodyLocation { get; set; }

        [AoMember(8, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<Stat, int>[] Stats { get; set; }

        /// <summary>
        /// The machine's name. Empty in every captured copy.
        /// </summary>
        /// <remarks>
        /// OmniCell reads it as Int32Terminated (the length counts a trailing NUL);
        /// this reader's ReadString trims the NUL, so Int32 reads the same bytes.
        /// </remarks>
        [AoMember(9, SerializeSize = ArraySizeType.Int32)]
        public string Name { get; set; }

        /// <summary>
        /// A second version, 2, checked against the static at 0x101C2084.
        /// OmniCell calls this TailVersion; AOSharp had put that name on the next field.
        /// </summary>
        /// <remarks>
        /// The tail this message adds to SimpleItemFullUpdate opens and closes
        /// with a version check. A mismatch here abandons the message.
        /// </remarks>
        [AoMember(10)]
        public int LockableVersion { get; set; }

        /// <summary>
        /// Stat 299, lockdifficulty. 50 in every captured copy.
        /// </summary>
        /// <remarks>
        /// Gamecode.dll 0x100A0CBA reads this int32 into
        /// **(message + 0x60) + 0x4AC. That address resolves: message + 0x60
        /// holds the stat table the message fills in, built at 0x10009D97 as a
        /// vector of 1,200 ints every one of which is seeded with 1234567890 -
        /// the stat unset sentinel. The table is indexed by stat id directly,
        /// so a byte offset of 0x4AC is element 299.
        ///
        /// Stat 299 is lockdifficulty, which is exactly what a container has,
        /// and 50 is exactly what one looks like.
        /// </remarks>
        [AoMember(11)]
        public int LockDifficulty { get; set; }

        /// <summary>
        /// Who can lock and unlock this. Empty in every captured copy.
        /// </summary>
        /// <remarks>
        /// The list belongs to the class this message inherits from, and the
        /// class says what it is for. LockableItemFullUpdateIIR_t::ReadSubClass
        /// at Gamecode.dll 0x100A0C87 reads it - the three fields it adds are a
        /// version, lockdifficulty and this - and its dispatcher at 0x100A0BEF
        /// casts the target to a LockableItem_t and adds every entry to the
        /// object's own list at +0x98.
        ///
        /// That list is read back at 0x10085890, which walks it comparing each
        /// entry against the identity of whatever is acting on the item, and on
        /// a match toggles the lock and prints Feedback_YouUnlockedTheItem or
        /// Feedback_YouLockedTheItem. So an entry here is an identity that may
        /// open the thing.
        ///
        /// Nothing has ever been seen in one, so the count is all the captures
        /// prove; the element width is the client's Identity reader at
        /// 0x1013D2F9, which is what 0x1002BAC0 calls for each entry.
        /// </remarks>
        [AoMember(12, SerializeSize = ArraySizeType.X3F1)]
        public Identity[] Keyholders { get; set; }

        /// <summary>
        /// A third version, 3, checked against the static at 0x101C1F80.
        /// </summary>
        /// <remarks>
        /// The last field of the message and the last of three version checks
        /// in it - 11 at the front, 2 opening the tail, and 3 closing it. Each
        /// one abandons the message on a mismatch.
        /// </remarks>
        [AoMember(13)]
        public int TailEndVersion { get; set; }

        #endregion

        #region Convenience (not on the wire)

        /// <summary>
        /// The NPC that holds this inventory (OmniCell's NpcIdentity): OwnerType:OwnerInstance.
        /// </summary>
        public Identity NpcIdentity
        {
            get => new Identity((IdentityType)this.OwnerType, this.OwnerInstance);
            set { this.OwnerType = (int)value.Type; this.OwnerInstance = value.Instance; }
        }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is TypeIdentifier (message version 11).")]
        public int Unknown1 { get => this.TypeIdentifier; set => this.TypeIdentifier = value; }

        [Obsolete("Wire field is InventoryIdAndBodyLocation.")]
        public short Unknown4 { get => this.InventoryIdAndBodyLocation; set => this.InventoryIdAndBodyLocation = value; }

        /// <summary>Old name for the Name length prefix (0 = empty name).</summary>
        [Obsolete("Wire field is the length prefix of Name; read Name.")]
        public int Unknown6
        {
            get => string.IsNullOrEmpty(this.Name) ? 0 : this.Name.Length + 1;
            set { if (value == 0) this.Name = string.Empty; }
        }

        [Obsolete("Wire field is LockableVersion (OmniCell: TailVersion, value 2).")]
        public int Unknown7 { get => this.LockableVersion; set => this.LockableVersion = value; }

        /// <summary>
        /// AOSharp's TailVersion was one field off: it has always held stat 299, lockdifficulty.
        /// Kept returning that value so existing readers keep their meaning.
        /// </summary>
        [Obsolete("This AOSharp name holds LockDifficulty (stat 299). The real tail version is LockableVersion.")]
        public int TailVersion { get => this.LockDifficulty; set => this.LockDifficulty = value; }

        /// <summary>Old int[] view of <see cref="Keyholders"/> (type, instance pairs flattened).</summary>
        [Obsolete("Wire field is Keyholders (Identity[]).")]
        public int[] UnknownArray
        {
            get => this.Keyholders?.SelectMany(k => new[] { (int)k.Type, k.Instance }).ToArray();
            set
            {
                if (value == null) { this.Keyholders = null; return; }
                this.Keyholders = Enumerable.Range(0, value.Length / 2)
                    .Select(i => new Identity((IdentityType)value[2 * i], value[2 * i + 1])).ToArray();
            }
        }

        [Obsolete("Wire field is TailEndVersion (value 3).")]
        public int Unknown9 { get => this.TailEndVersion; set => this.TailEndVersion = value; }

        #endregion
    }
}
