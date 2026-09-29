// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TrapItemFullUpdateMessage.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TrapItemFullUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging
    // (Messages\N3Messages\TrapItemFullUpdateMessage.cs). Changes for this library only: AOSharp's
    // Identity/Vector3/Quaternion types; the Stat enum as the stat key (OmniCell: CharacterStat); nullable
    // Coordinate/Heading for the instance-gated position, as DoorFullUpdateMessage declares it; ArraySizeType.Int32
    // where OmniCell says Int32Terminated - this library has no Int32Terminated, and its string reader trims the
    // counted NUL, so the read is byte for byte the same; only a re-serialized string would come out one byte short of
    // the client's writer.
    // AOBuddy census 2026-09-29: 1796 copies in the bot's mission recordings and 133 in the retail dumps, every one
    // read to the last byte by this class. The remarks below that say no capture holds one are OmniCell's and predate
    // these.

    using AOSharp.Common.GameData;
    #region Usings ...

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    /// <summary>
    /// A trap or a mine on the ground: everything a
    /// <see cref="SimpleItemFullUpdateMessage"/> carries and five fields more.
    /// </summary>
    /// <remarks>
    /// Extracted-client TrapItemFullUpdateIIR_t has vtable 0x10167E00, reader
    /// 0x100A28CA, writer 0x100A2954 and dispatcher 0x100A2A96. The reader
    /// calls SimpleItemFullUpdate's own reader at 0x100A1661 for everything the
    /// family shares, then takes a version, an int32, two Identities and a
    /// byte; the writer emits the same, packing the byte from the two booleans,
    /// so the two agree.
    ///
    /// The dispatcher does nothing but check the target exists. The work is in
    /// two copy-backs: 0x100A29C3, which writes the first Identity to
    /// TrapItem_t + 0x1D4, and 0x100A2A43, which hands the two flag bits to the
    /// object's virtual slots 0x108 and 0x104.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.TrapItemFullUpdate)]
    public class TrapItemFullUpdateMessage : N3Message
    {
        public TrapItemFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.TrapItemFullUpdate;
        }

        public Identity Owner { get; set; }

        private int identityType;

        private int instance;

        [AoMember(1)]
        public int MsgVersion { get; set; }

        [AoMember(2)]
        public int Identitytype
        {
            get
            {
                return this.identityType;
            }
            set
            {
                this.identityType = value;
                this.Owner = new Identity() { Type = (IdentityType)value, Instance = this.instance };
            }
        }

        /// <summary>
        /// Zero is what makes the position fields present.
        /// </summary>
        /// <remarks>
        /// The gate is on the instance, not on the type beside it. See
        /// <see cref="SimpleItemFullUpdateMessage"/>, whose reader this one
        /// calls for everything down to the name.
        /// </remarks>
        [AoMember(3)]
        [AoFlags("flag")]
        public int Instance
        {
            get
            {
                return this.instance;
            }
            set
            {
                this.instance = value;
                this.Owner = new Identity() { Type = (IdentityType)this.identityType, Instance = value };
            }
        }

        [AoMember(4)]
        [AoUsesFlags("flag", typeof(Vector3), FlagsCriteria.HasNone, new[] { int.MaxValue })]
        public Vector3? Coordinate { get; set; }

        [AoMember(5)]
        [AoUsesFlags("flag", typeof(Quaternion), FlagsCriteria.HasNone, new[] { int.MaxValue })]
        public Quaternion? Heading { get; set; }

        [AoMember(6)]
        public int Playfield { get; set; }

        /// <summary>
        /// The shared item-message constant, which the client reads and drops.
        /// </summary>
        [AoMember(7)]
        public Identity Marker { get; set; }

        /// <summary>
        /// Which inventory the item is in. Stat 55, inventoryid.
        /// </summary>
        [AoMember(8)]
        public byte InventoryId { get; set; }

        /// <summary>
        /// Where on the body or in the page it sits. Stat 220, currbodylocation.
        /// </summary>
        [AoMember(9)]
        public byte BodyLocation { get; set; }

        [AoMember(10, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<Stat, uint>[] Stats { get; set; }

        [AoMember(11, SerializeSize = ArraySizeType.Int32)]
        public string Name { get; set; }

        /// <summary>
        /// Checked against the static at 0x101C21C4; a mismatch abandons the
        /// message.
        /// </summary>
        /// <remarks>
        /// The writer at 0x100A2954 emits the static rather than a field, so
        /// the two agree by construction.
        /// </remarks>
        [AoMember(12)]
        public int TrapVersion { get; set; }

        /// <summary>
        /// Stat 289, trapdifficulty.
        /// </summary>
        /// <remarks>
        /// The constructor that builds one of these from a live trap, at
        /// 0x100A2827, reads stat 0x121 - 289 - off the dynel into this field.
        /// A fresh TrapItem_t is created with 50 in it, set at 0x1009937B.
        ///
        /// It is one half of the roll the client makes when a character comes
        /// near a trap it has not spotted: see <see cref="Revealed"/>.
        /// </remarks>
        [AoMember(13)]
        public int TrapDifficulty { get; set; }

        /// <summary>
        /// An Identity the message writes onto the trap and nothing reads back.
        /// </summary>
        /// <remarks>
        /// The copy-back at 0x100A29F4 writes it to TrapItem_t + 0x1D4, and the
        /// constructor at 0x100A282D reads it out of the same place, so the two
        /// directions agree about where it lives. What it is for is not settled.
        ///
        /// TrapItem_t's own methods never read + 0x1D4 - not the constructor at
        /// 0x100992E0, which zeroes it, not the teardown at 0x1009943E, not
        /// CanBeTriggeredBy at 0x10099716, not the arm, disarm or reveal
        /// helpers. The whole image has 250 plain references to that offset,
        /// far too common to be identifying, so this is "no reader found in the
        /// class" and not "no reader exists". A routine at 0x10086ABB does fill
        /// a + 0x1D4 from a dynel's Identity and then read stat 490,
        /// originatortype, which would make this the originator - but it sits
        /// in a vtable that is not TrapItem_t's, and an offset that common is
        /// not enough to put it on this class.
        /// </remarks>
        [AoMember(14)]
        public Identity Unknown1 { get; set; }

        /// <summary>
        /// A second Identity the client can never send and never reads.
        /// </summary>
        /// <remarks>
        /// Read into the message at + 0x80 and left there. The only constructor
        /// zeroes it outright, at 0x100A2842, rather than filling it from
        /// anything, and neither copy-back mentions it. So the client can put
        /// nothing in it and does nothing with what arrives in it, and what the
        /// server means by it is not answerable from this module.
        /// </remarks>
        [AoMember(15)]
        public Identity Unknown2 { get; set; }

        /// <summary>
        /// The byte that closes the message: bit 0
        /// <see cref="Revealed"/>, bit 1 <see cref="Armed"/>.
        /// </summary>
        /// <remarks>
        /// One byte, not two. The reader at 0x10099736 takes a single byte and
        /// splits it - bit 0 to the message's + 0x88 and bit 1 to + 0x89 - and
        /// the writer at 0x100A2992 packs the two back together the same way.
        /// The two properties below are that byte read out; neither is on the
        /// wire on its own.
        /// </remarks>
        [AoMember(16)]
        public byte Flags { get; set; }

        /// <summary>
        /// Whether the trap will go off. Bit 1 of <see cref="Flags"/>.
        /// </summary>
        /// <remarks>
        /// A trap is created armed: the TrapItem_t constructor at 0x1009936A
        /// writes a word - mov word ptr [esi + 0x1E4], 1 - which sets the armed
        /// byte to 1 and the revealed byte beside it to 0 in one instruction,
        /// which is also what says the two are separate bytes in the object
        /// rather than one field.
        ///
        /// Three things read it. CanBeTriggeredBy, TrapItem_t slot 0xF0 at
        /// 0x10099716, returns false at once when it is clear. The disarm
        /// helper at 0x10085E9D does nothing unless it is set, and clears it
        /// when it acts. And the feedback at 0x10085D72 picks
        /// "Feedback_MineIsArmed" when it is set and
        /// "Feedback_MineAlreadyDisarmed" when it is not.
        ///
        /// Both directions are covered: the constructor at 0x100A285C reads
        /// TrapItem_t + 0x1E4 into the message, and the copy-back at 0x100A2A8A
        /// hands it back through slot 0x104, the one-line setter at 0x1009979D.
        /// </remarks>
        public bool Armed
        {
            get
            {
                return (this.Flags & 2) != 0;
            }

            set
            {
                this.Flags = (byte)(value ? this.Flags | 2 : this.Flags & ~2);
            }
        }

        /// <summary>
        /// Whether this character has already spotted the trap. Bit 0 of
        /// <see cref="Flags"/>.
        /// </summary>
        /// <remarks>
        /// CanBeTriggeredBy at 0x10099716 is the whole of it. After the armed
        /// test it returns true immediately when this is set; otherwise it
        /// takes the character's stat 136, perception, and this trap's stat
        /// 289, trapdifficulty, and puts them through the roll at 0x1002D26C -
        /// perception minus difficulty plus 75 against a random draw - and on a
        /// success calls slot 0x10C, which sets this to 1 and never clears it.
        ///
        /// So the roll is made once per character per trap, and this is the
        /// server saying the answer is already yes. The reveal helper at
        /// 0x10085E75 is guarded the same way.
        ///
        /// Bit 0, not bit 1: the reader puts the byte's bit 0 at message + 0x88
        /// and bit 1 at + 0x89, and the constructor fills + 0x88 from
        /// TrapItem_t + 0x1E5 - this - and + 0x89 from + 0x1E4.
        /// </remarks>
        public bool Revealed
        {
            get
            {
                return (this.Flags & 1) != 0;
            }

            set
            {
                this.Flags = (byte)(value ? this.Flags | 1 : this.Flags & ~1);
            }
        }
    }
}
