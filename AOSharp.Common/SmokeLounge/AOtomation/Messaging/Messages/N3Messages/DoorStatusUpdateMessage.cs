// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DoorStatusUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the DoorStatusUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\DoorStatusUpdateMessage.cs);
    // the AOSharp original read an int over Locked, Open and half of AccessKey, so its Open was an AccessKey byte.
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A door opening, closing, locking or unlocking.
    /// </summary>
    /// <remarks>
    /// Extracted-client DoorStatusUpdateIIR_t has vtable 0x10167B20 and reader
    /// 0x100A0330. What the fields are is not guesswork here, because the
    /// client both reads this message and builds it: the constructor at
    /// 0x100A02C0 fills one in from a Door_t, field by field, and the
    /// dispatcher at 0x100A015A applies one to a Door_t, field by field. The
    /// two agree, and between them every field but two is named.
    ///
    /// The three flags go over the wire as a byte each and the reader at
    /// 0x100A0138 normalises anything that is not 1 to 0, so each is a bool and
    /// not a small enum. They are declared as bytes here because nothing else
    /// in this assembly serialises a bool, not because the wire is wider.
    ///
    /// All 117 captured copies carry zero in everything except the version, so
    /// the captures confirm the shape and say nothing about the values. Every
    /// door in that capture was shut, unlocked and keyless.
    /// </remarks>
    [AoContract((int)N3MessageType.DoorStatusUpdate)]
    public class DoorStatusUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public DoorStatusUpdateMessage()
        {
            this.N3MessageType = N3MessageType.DoorStatusUpdate;
            this.Version = 2;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// 2, and the reader accepts nothing else.
        /// </summary>
        /// <remarks>
        /// Compared against the static at Gamecode.dll 0x101C2020, which holds
        /// 2; a mismatch abandons the message. 2 in all 117 captured copies.
        /// </remarks>
        [AoMember(0)]
        public int Version { get; set; }

        /// <summary>
        /// Whether the door is locked.
        /// </summary>
        /// <remarks>
        /// The constructor takes it from the door's vtable slot 0xFC, which is
        /// one instruction - HasFlag(0x40) - and the unlock routine at
        /// 0x10085613 is the other half of the proof: it tests that same slot
        /// and then clears exactly bit 0x40 out of the flags word before
        /// writing it back. So bit 0x40 of a dynel's flags is its lock, and
        /// this is that bit.
        ///
        /// Applying it picks between two of the door's animation timers, at
        /// 0x100804CA and 0x1007F468.
        /// </remarks>
        [AoMember(1)]
        public byte Locked { get; set; }

        /// <summary>
        /// Whether the door is open.
        /// </summary>
        /// <remarks>
        /// The constructor takes it from HasFlag(0x80), and the dispatcher
        /// settles what that bit means: true runs the door's open animation and
        /// then n3RoomMonitor_t::DoorOpened, false runs
        /// n3RoomMonitor_t::DoorClosed. Those two are imports from N3.dll and
        /// they are named in the import table, so this one is not an inference
        /// at all.
        /// </remarks>
        [AoMember(2)]
        public byte Open { get; set; }

        /// <summary>
        /// Stat 195, accesskey - which key opens this door.
        /// </summary>
        /// <remarks>
        /// The constructor reads stat 0xC3 off the door and the dispatcher
        /// writes it back to the same stat. 0 in all 117 captured copies.
        /// </remarks>
        [AoMember(3)]
        public int AccessKey { get; set; }

        /// <summary>
        /// A flag the client can only ever set, never clear, and the only thing
        /// that reads it is this message.
        /// </summary>
        /// <remarks>
        /// It is byte 0x1D5 of a Door_t. Two things in the whole image touch
        /// it: this message's dispatcher, which sets it to 1 when this field is
        /// true, and 0x1007F2AE, which sets it to 1 and then opens the door.
        /// That second one is the door's vtable slot 0xB0, and slot 0xAC next
        /// to it opens the door without setting it - so a door has two ways to
        /// be opened and this marks which one was used. The constructor reads
        /// the byte straight back out to fill this field.
        ///
        /// What distinguishes the two entry points is not established: nothing
        /// in Gamecode calls slot 0xB0 through a vtable, so the caller is in
        /// another module. Naming it would be a guess about which one.
        /// </remarks>
        [AoMember(4)]
        public byte Unknown5 { get; set; }

        /// <summary>
        /// An X3F1 counted list of Identities, empty in every captured copy.
        /// </summary>
        /// <remarks>
        /// Read by 0x1002DA7E - a plain X3F1 count and then that many
        /// Identities through the standard reader at 0x1013D2F9 - into a vector
        /// at message + 0x20, which the message's own constructor initialises
        /// empty and never fills. The dispatcher does not touch it either, so
        /// nothing in the client either sends it or acts on it.
        ///
        /// A door that carries a lock and an access key carrying a list of
        /// identities invites the same reading as the keyholder list on the
        /// item messages, and that is exactly why it is not named that here:
        /// no code joins the two.
        /// </remarks>
        [AoMember(5, SerializeSize = ArraySizeType.X3F1)]
        public Identity[] Unknown6 { get; set; }

        #endregion
    }
}
