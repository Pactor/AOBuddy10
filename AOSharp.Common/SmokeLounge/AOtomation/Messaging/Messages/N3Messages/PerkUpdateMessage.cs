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
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\PerkUpdateMessage.cs);
    // no changes.
    // AOBuddy census 2026-09-29: 0 copies in the bot's mission recordings and 135 in the retail dumps, every one read
    // to the last byte by this class. The remarks below that say no capture holds one are OmniCell's and predate
    // these.

    #region Usings ...

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    /// <summary>
    /// One research line's progress, under a name the client got wrong.
    /// </summary>
    /// <remarks>
    /// The name is not ours to change: the message id 0x435F7023 is the hash of
    /// the string PerkUpdateIIR, which is a real class name in the client, so
    /// PerkUpdate is what the client calls it. Everything it does is research.
    /// It looks its id up in the research definition table, writes stat 265
    /// personalresearchgoal, tracks experience outstanding on a research line,
    /// and on completion sets the research window's got_tech - not got_perk,
    /// which exists two strings away. The dispatcher also gates on stat 389,
    /// expansion, at 0x10053916, which is what decides whether a character has
    /// research at all.
    ///
    /// Nothing has ever captured one. PerkUpdate is absent from 646,372
    /// packets, and absent as an unreadable id too, which is the check that
    /// separates a message we cannot read from one that never arrives. Two
    /// logins, a perk purchase and a levelling session produced none. Every
    /// field below is read out of the client rather than off the wire, and
    /// that is enough here because the reader, the writer and the dispatcher
    /// are all accounted for instruction by instruction.
    ///
    /// Reader 0x100624BF, writer 0x100624F7, dispatcher 0x10063E88, vtable
    /// 0x10156F74. The reader is three int32s into + 0x18, + 0x1C and + 0x20
    /// and nothing else; the writer emits the same three.
    /// </remarks>
    [AoContract((int)N3MessageType.PerkUpdate)]
    public class PerkUpdateMessage : N3Message
    {
        public PerkUpdateMessage()
        {
            this.N3MessageType = N3MessageType.PerkUpdate;
        }

        /// <summary>
        /// Which research line this is about.
        /// </summary>
        /// <remarks>
        /// The same id space as <see cref="GameData.ResearchUpdateEntry"/>.
        /// The completion path at Gamecode 0x10052CFD takes the research table
        /// singleton from 0x1002C0D5 and looks this value up through 0x1002BDF5,
        /// which calls 0x1002BD8C - and 0x1002BD8C is the same lookup
        /// ResearchUpdate uses on its own ids, at 0x1003A7D8. Two messages, one
        /// table, one id space.
        ///
        /// It also keys the character's own research map, which hangs off the
        /// dynel at + 0x1F4: 0x10058FF8 creates it on first use and 0x10053210
        /// finds the entry for this id in it. That entry is where
        /// <see cref="ResearchXpRemaining"/> is stored, and its + 0x18 is the
        /// total the progress is measured against. The dispatcher emits the id
        /// on GlobalSignals_c + 0x240, one slot below ResearchUpdate at + 0x244.
        /// </remarks>
        [AoMember(1)]
        public int ResearchId { get; set; }

        /// <summary>
        /// The character's personal research goal: stat 265.
        /// </summary>
        /// <remarks>
        /// The dispatcher at 0x10063EF2 pushes this value and the stat id 0x109
        /// into the target's stat list, [dynel + 0xE8] through vtable + 0x40,
        /// which is the same shape that proved mechdata for MechInfo. 0x109 is
        /// 265, and 265 is personalresearchgoal.
        /// </remarks>
        [AoMember(2)]
        public int PersonalResearchGoal { get; set; }

        /// <summary>
        /// Experience still owed on this line before it completes.
        /// </summary>
        /// <remarks>
        /// Stored on the map entry at + 0x1C by 0x10052D86, which clamps a
        /// negative to zero - so this is an amount outstanding and not a
        /// balance. The dispatcher then computes, in floating point,
        /// (entry + 0x18 minus this) divided by entry + 0x18: the total for the
        /// line, less what is still owed, over the total. When that reaches 1
        /// it sets the DistributedValue named got_tech to true.
        ///
        /// got_tech is what ties the whole message to research rather than to
        /// perks. It sits in GUI.dll at 0x100F7C92 among "Allocate Research",
        /// "Available Research", "Finished Research", esc_research and the
        /// personal and global scroll lists - the research window - and it is
        /// one of a trio with got_ip and got_perk, so the client had a
        /// got_perk to set here and set the research one instead.
        /// </remarks>
        [AoMember(3)]
        public int ResearchXpRemaining { get; set; }
    }
}
