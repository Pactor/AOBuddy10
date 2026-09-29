#region License

// Copyright (c) 2005-2014, CellAO Team
// 
// 
// All rights reserved.
// 
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

#endregion

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    #region Usings ...

    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\DoorFullUpdateMessage.cs),
    // which decodes every captured door; the AOSharp original read an Identity where MsgVersion is and lost the rest.
    // Changes for this library only: AOSharp's Identity/Vector3/Quaternion/Stat types, nullable Coordinate/Heading
    // (as SimpleItemFullUpdateMessage declares its flag-gated position), and the Stat enum as the stat key.
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    [AoContract((int)N3MessageType.DoorFullUpdate)]
    public class DoorFullUpdateMessage : N3Message
    {
        public Identity Owner { get; set; }

        private int identityType;

        private int instance;

        public DoorFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.DoorFullUpdate;
        }

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
                this.Owner = new Identity((IdentityType)value, this.instance);
            }
        }

        /// <summary>
        /// The instance half of the owner identity, and the gate on the
        /// position below.
        /// </summary>
        /// <remarks>
        /// The instance and not the type. SimpleItemFullUpdate, which this
        /// message is built on, settles it: across 690 captured copies of its
        /// vending machine relative, 281 carry a position and exactly 281 have
        /// a zero instance, while 362 have a zero type. Reading it off the type
        /// left thirty seven packets unparseable until 2026-09-10.
        ///
        /// This message had kept the type gate. It never showed, because all
        /// 142 captured doors have both halves zero and the two rules agree
        /// there - but a door owned by something would have been read wrong.
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
                this.Owner = new Identity((IdentityType)this.identityType, value);
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
        /// The door's state machine. Type 1000015 and instance 1 in every
        /// captured copy.
        /// </summary>
        /// <remarks>
        /// The same field the rest of the item family carries, and the doors
        /// are what show it is not a marker. 1000015 is the identity type of a
        /// state machine - the client builds that identity from stat 450,
        /// statemachine, at 0x10087B15 and again in SimpleItemFullUpdate's own
        /// dispatcher at 0x100A1C31, then looks it up in the registry at
        /// 0x101C18E0. Items carry instance 0, meaning none. A door carries 1,
        /// because a door has states to be in.
        /// </remarks>
        [AoMember(7)]
        public Identity StateMachine { get; set; }

        /// <summary>
        /// Stat 55, inventoryid. 0 in every captured copy.
        /// </summary>
        /// <remarks>
        /// Sign-extended into the stat by Gamecode.dll 0x100A1B83, which is
        /// shared with every other message in the item family.
        /// </remarks>
        [AoMember(8)]
        public byte InventoryId { get; set; }

        /// <summary>
        /// Stat 220, currbodylocation. 111 in every captured copy.
        /// </summary>
        /// <remarks>
        /// Sign-extended into the stat by 0x100A1B94.
        /// </remarks>
        [AoMember(9)]
        public byte BodyLocation { get; set; }

        // 3f1. Stat 0 (Flags) carries the door's state bits: 0x40 locked, 0x80 open - the same two bits
        // DoorStatusUpdateMessage's Locked and Open are read from (see its remarks).
        [AoMember(10, SerializeSize = ArraySizeType.X3F1)]
        public GameTuple<Stat, uint>[] Stats { get; set; }

        [AoMember(11, SerializeSize = ArraySizeType.Int32)]
        public string Name { get; set; }

        /// <summary>
        /// A second version, 2, checked against the static at 0x101C2084.
        /// </summary>
        /// <remarks>
        /// This and the two fields after it are LockableItemFullUpdate's, not
        /// this message's: DoorFullUpdateIIR_t::ReadSubClass at 0x1009FF81
        /// calls 0x100A0C87 for all of it and then reads only the last two
        /// fields below.
        /// </remarks>
        [AoMember(12)]
        public int TailVersion { get; set; }

        /// <summary>
        /// Stat 299, lockdifficulty. 50 in every captured copy.
        /// </summary>
        [AoMember(13)]
        public int LockDifficulty { get; set; }

        /// <summary>
        /// Who can lock and unlock this door. Empty in every captured copy.
        /// </summary>
        /// <remarks>
        /// The LockableItem_t keyholder list - see the same field on
        /// ChestItemFullUpdate for how the client uses it.
        /// </remarks>
        [AoMember(14, SerializeSize = ArraySizeType.X3F1)]
        public Identity[] Keyholders { get; set; }

        /// <summary>
        /// A third version, 2, and the reader accepts nothing else.
        /// </summary>
        /// <remarks>
        /// This one is the door's own: 0x1009FFA7 compares it against the
        /// static at 0x101C1FF8, which holds 2, and abandons the message on a
        /// mismatch.
        /// </remarks>
        [AoMember(15)]
        public int DoorVersion { get; set; }

        /// <summary>
        /// The room the door stands in.
        /// </summary>
        /// <remarks>
        /// This and <see cref="AdjoiningRoom"/> were one int32 and one Unknown.
        /// They are two room indices, and N3.dll names them without any
        /// inference: the client keeps the pair at Door_t + 0x1D0, and
        /// 0x1007F51F splits it into its two 16 bit halves and hands each one
        /// to n3Playfield_t::GetRoom, skipping either half that reads 0xFFFF.
        /// The same routine only runs at all when n3Playfield_t::IsDungeon
        /// agrees, and the pair goes out whole to n3RoomMonitor_t::DoorOpened
        /// and DoorClosed at 0x1007F4C3 and 0x1007F4FD. Building a
        /// DoorFullUpdate from a door copies Door_t + 0x1D0 straight into the
        /// message at + 0x78, at 0x100A0101, which closes the loop.
        ///
        /// Which half is which was worked out from the geometry before the
        /// client was asked, and the two agree. This half is the index into the
        /// room table the same playfield's PlayfieldAnarchyF carries: the
        /// captured mission's generator lists thirty two rooms with a grid cell
        /// each, and putting a door's world position back on that grid -
        /// divide X by ten, and the same for Z measured from the far edge -
        /// lands it on the cell this half names, within the half cell a door
        /// sits off centre by because it is in a wall. It runs 1 to 22 across
        /// the building, with -1 on the one door that leads outside.
        ///
        /// The earlier reading of the other half as "not a room" was wrong, and
        /// wrong in a way the geometry could not show: a door between two rooms
        /// is only ever in one of them.
        /// </remarks>
        [AoMember(16)]
        public short Room { get; set; }

        /// <summary>
        /// The room on the other side of the door, or -1 if there is none.
        /// </summary>
        /// <remarks>
        /// The client looks this one up first. Doors share it the way doors off
        /// a corridor would - seven of the captured twenty five carry 8 and six
        /// carry 1 - which is what made it look like a grouping rather than a
        /// room until GetRoom settled it.
        /// </remarks>
        [AoMember(17)]
        public short AdjoiningRoom { get; set; }
    }
}