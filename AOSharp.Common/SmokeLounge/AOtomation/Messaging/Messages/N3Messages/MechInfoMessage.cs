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
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\MechInfoMessage.cs).
    // Changes for this library only: AOSharp's Identity/Vector3/Quaternion types; the Stat enum as the stat key
    // (OmniCell: CharacterStat); MechData is nullable, because this library's flag-gated read hands back null when
    // HasStats is 0 and a non-nullable member would throw unboxing it.

    using AOSharp.Common.GameData;
    #region Usings ...

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    [AoContract((int)N3MessageType.MechInfo)]
    public class MechInfoMessage : N3Message
    {
        public MechInfoMessage()
        {
            this.N3MessageType = N3MessageType.MechInfo;
        }

        /// <summary>
        /// Whether the stats and the trailing int32 are on the wire. 0 or 1.
        /// </summary>
        /// <remarks>
        /// Not a count and not a length. The reader at Gamecode.dll 0x10075557
        /// takes this int32 first and branches on it: zero and it reads an
        /// Identity and stops; non-zero and it builds a stat table, fills it
        /// from the stream, reads the Identity, and then reads one more int32.
        /// The writer at 0x100755EA is the same shape from the other side - it
        /// writes a literal 0 or a literal 1 depending on whether the message
        /// is carrying a stat table at all.
        ///
        /// The constructor at 0x10075C91 says where the two halves come from.
        /// Given a character it takes that character's stats and the identity
        /// of whatever is in their sixth inventory slot; given none it takes an
        /// identity from its caller and carries no stats.
        /// </remarks>
        [AoMember(1)]
        [AoFlags("hasStats")]
        public int HasStats { get; set; }

        /// <summary>
        /// The character's stats, when there are any.
        /// </summary>
        /// <remarks>
        /// A plain int32 count and then that many stat and value pairs - not an
        /// X3F1 count, unlike most lists on this wire. GameData's shared stat
        /// table reader at 0x10009DAD is what reads it, and the same function
        /// seeds any stat it is not told about with 1234567890.
        /// </remarks>
        [AoMember(2, SerializeSize = ArraySizeType.Int32)]
        [AoUsesFlags("hasStats", typeof(GameTuple<Stat, uint>[]), FlagsCriteria.EqualsToAny, new[] { 1 })]
        public GameTuple<Stat, uint>[] Stats { get; set; }

        /// <summary>
        /// The mech, taken from the character's sixth inventory slot.
        /// </summary>
        [AoMember(3)]
        public Identity MechIdentity { get; set; }

        /// <summary>
        /// Character stat 662, MechData. Present only when <see cref="HasStats"/> is set.
        /// </summary>
        /// <remarks>
        /// It was modelled as a four byte string called Hash, which is the
        /// right width and nothing else: the reader takes it with the same
        /// int32 reader it uses for every other integer here, at 0x100755C1,
        /// and the writer writes it back the same way.
        ///
        /// The constructor at 0x10075C91 takes it as its third argument and
        /// stores it at message +0x24. The dispatcher at 0x10075646 then hands
        /// that same value to the character stat interface with stat id 0x296
        /// (662), named MechData by the extracted stat table.
        /// </remarks>
        [AoMember(4)]
        [AoUsesFlags("hasStats", typeof(int), FlagsCriteria.EqualsToAny, new[] { 1 })]
        public int? MechData { get; set; }
    }
}
