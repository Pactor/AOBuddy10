// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetNameMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SetNameMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\SetNameMessage.cs).
    // Changes for this library only: AOSharp's Identity/Vector3/Quaternion types.
    // AOBuddy census 2026-09-29: 0 copies in the bot's mission recordings and 13 in the retail dumps, every one read
    // to the last byte by this class. The remarks below that say no capture holds one are OmniCell's and predate
    // these.

    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One of a dynel's four name slots, replaced.
    /// </summary>
    /// <remarks>
    /// SetNameIIR_t has vtable 0x101622F8, reader 0x10077090, writer 0x10077004
    /// and dispatcher 0x10077207. The reader takes an int32, a byte, an
    /// int32 length and that many raw bytes, an Identity and a final int32; the
    /// writer emits the same six in the same order, so there are two
    /// independent sources for the layout. The only constructor, at 0x10077065,
    /// is reached from the message factory at 0x1000BDCD and nowhere else, so
    /// the client never builds one of these - the reader and the dispatcher are
    /// all there is.
    ///
    /// The dispatcher resolves the message's own identity with
    /// n3Dynel_t::GetDynel and calls 0x1005BB92 on it with the int32 and the
    /// string. That function is a four way switch on the int32, and each arm
    /// writes a different std::string on the dynel: 1 goes to +0x170, 2 to
    /// +0x18C, 3 to +0x154 and 4 to a heap string at +0x1A8, and anything else
    /// does nothing at all. Two of those four are named by exports that read
    /// them straight back - N3Msg_GetFirstName returns +0x170 at 0x10018968 and
    /// N3Msg_GetLastName returns +0x18C at 0x100189A5.
    ///
    /// <see cref="CloneMessage"/> uses slot 3, and slot 3 is the only one the
    /// client treats specially: the dispatcher first clears bit 0x40 through
    /// two virtual calls, and the store itself replaces every '/' in the string
    /// with 'X' at 0x1005BCF6, which is what a name a person typed gets.
    ///
    /// The reader refuses the whole message unless the length is between 1 and
    /// 31, so a name is at most 31 bytes and there is no terminator on the
    /// wire.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.SetName)]
    public class SetNameMessage : N3Message
    {
        #region Constructors and Destructors

        public SetNameMessage()
        {
            this.N3MessageType = N3MessageType.SetName;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Which of the dynel's four name slots to write, 1 to 4.
        /// </summary>
        /// <remarks>
        /// 1 is the first name and 2 is the last name, both named by the
        /// exports that read those members back. 3 is the slot Clone uses and 4
        /// is a heap string; neither has an export that names it. Any other
        /// value reaches 0x1005BD4C, which does nothing.
        /// </remarks>
        [AoMember(0)]
        public int NameSlot { get; set; }

        /// <summary>
        /// A byte the reader keeps as a boolean and nothing reads.
        /// </summary>
        /// <remarks>
        /// The reader stores whether it was non-zero at the message's +0x38,
        /// and the writer emits that byte again, so only zero and one ever come
        /// back out. The dispatcher never looks at it.
        /// </remarks>
        [AoMember(1)]
        public byte Unknown1 { get; set; }

        /// <summary>
        /// The name, 1 to 31 bytes behind an int32 length.
        /// </summary>
        [AoMember(2, SerializeSize = ArraySizeType.Int32)]
        public string Name { get; set; }

        /// <summary>
        /// An Identity the dispatcher never reads.
        /// </summary>
        [AoMember(3)]
        public Identity Unknown2 { get; set; }

        /// <summary>
        /// A trailing int32 the dispatcher never reads.
        /// </summary>
        [AoMember(4)]
        public int Unknown3 { get; set; }

        #endregion
    }
}
