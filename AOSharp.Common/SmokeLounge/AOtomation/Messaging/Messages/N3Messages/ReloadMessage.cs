// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReloadMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ReloadMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
    using System;

    // Layout, names and doc comments ported from OmniCell's AOtomation.Messaging (verified against
    // every recorded packet). Old AOSharp names are kept as [Obsolete] aliases where they differed.
    /// <summary>
    /// A weapon has been reloaded: what came out of the clip, and what went
    /// into the gun.
    /// </summary>
    /// <remarks>
    /// Five fields, and one call names all five. Reader 0x10076C2A, writer
    /// 0x10076C83, dispatcher 0x10076CC9, vtable 0x10162280; the reader takes
    /// two Identities and three int32s - the last of which it keeps as nothing
    /// but zero-or-not - and the writer emits the same five.
    ///
    /// The dispatcher resolves the message identity to a character, takes the
    /// fight handler at character +0x1D4 - the member N3Msg_GetAttackingID,
    /// N3Msg_CanAttack and N3Msg_GetSpecialAttackWeaponName all work through -
    /// and hands it the five fields at 0x100697BD. That function splits into
    /// two halves.
    ///
    /// The ammunition half works on the character's inventory. When the flag is
    /// set it calls 0x10049391 with a slot of -1, which ends at 0x1002AB07:
    /// fetch the entry, free it, and clear the slot. When the flag is clear it
    /// calls 0x10049354, which ends at 0x1002AF06: fetch the entry at that slot
    /// and set its +2 - the count int16 every container record carries - to the
    /// int32, clamped to 0 to 0xFFFF, marking the entry changed if it moved.
    /// Either way the Identity it works from is an inventory location and not
    /// an item: both paths check that its type is between 0x65 and 0xF9, which
    /// is the range of inventory pages, and use its instance as the slot.
    ///
    /// The weapon half resolves the second Identity to a WeaponItem_t - the
    /// cast target at 0x101BFCC8 says so - reads stat 0x1A, Energy, refuses the
    /// message if it comes back negative, and sets that stat to the second
    /// int32.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.Reload)]
    public class ReloadMessage : N3Message
    {
        #region Constructors and Destructors

        public ReloadMessage()
        {
            this.N3MessageType = N3MessageType.Reload;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Where the ammunition was: an inventory page as the type, and the
        /// slot within it as the instance.
        /// </summary>
        /// <remarks>
        /// Read at 0x10076C3A. Both paths through 0x100697BD check that the
        /// type is an inventory page - 0x65 to 0xF9 - before doing anything,
        /// and type 0x6B has its instance masked to sixteen bits first.
        /// </remarks>
        [AoMember(0)]
        public Identity Ammo { get; set; }

        /// <summary>
        /// The weapon that was loaded.
        /// </summary>
        /// <remarks>
        /// Read at 0x10076C44 and resolved as a WeaponItem_t.
        /// </remarks>
        [AoMember(1)]
        public Identity Weapon { get; set; }

        /// <summary>
        /// What is left of the ammunition stack.
        /// </summary>
        /// <remarks>
        /// Read at 0x10076C58. Written to the container record's count int16 -
        /// the same field InventorySlot calls Count - clamped to 0 to 0xFFFF.
        /// Only used when <see cref="AmmoUsedUp"/> is zero; when it is not, the
        /// entry goes rather than changing.
        /// </remarks>
        [AoMember(2)]
        public int AmmoCount { get; set; }

        /// <summary>
        /// What is now in the gun.
        /// </summary>
        /// <remarks>
        /// Read at 0x10076C60 and set on the weapon as stat 0x1A, Energy, at
        /// 0x1006982F. The client reads that stat first and abandons the
        /// message if it comes back negative.
        /// </remarks>
        [AoMember(3)]
        public int Energy { get; set; }

        /// <summary>
        /// Whether the ammunition stack is gone.
        /// </summary>
        /// <remarks>
        /// An int32 on the wire and a boolean everywhere else: the reader at
        /// 0x10076C6A keeps only whether it was non-zero, and the writer at
        /// 0x10076CB9 puts that back as an int32. So anything other than 0 or 1
        /// here does not survive a round trip through the client, and nothing
        /// should send it.
        ///
        /// Non-zero takes the inventory entry away; zero sets its count to
        /// <see cref="AmmoCount"/> instead.
        /// </remarks>
        [AoMember(4)]
        public int AmmoUsedUp { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is Ammo (inventory page + slot).")]
        public Identity AmmoSlot { get => this.Ammo; set => this.Ammo = value; }

        [Obsolete("Wire field is Weapon.")]
        public Identity WeaponIdentity { get => this.Weapon; set => this.Weapon = value; }

        [Obsolete("Wire field is Energy (stat 26 on the weapon).")]
        public int WeaponAmmoCount { get => this.Energy; set => this.Energy = value; }

        #endregion
    }
}
