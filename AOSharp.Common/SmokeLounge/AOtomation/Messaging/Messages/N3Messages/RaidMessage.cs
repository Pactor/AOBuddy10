// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RaidMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the RaidMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\RaidMessage.cs); no
    // changes.

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A raid being formed, or the list of instances it is locked out of.
    /// </summary>
    /// <remarks>
    /// RaidIIR_c has vtable 0x10167F2C, reader 0x100A3B2E, writer 0x100A3B63
    /// and dispatcher 0x100A40C0. The reader takes an int16 and widens it; the
    /// writer narrows the same field back, so two bytes travel even though the
    /// client holds four. Only the value 1 carries a body, and then it is read
    /// by 0x1005731B: an int32 that must be 1 or the rest is skipped, then a
    /// plain int32 count and that many twenty byte records.
    ///
    /// The list was a dead end for a long time. It goes into +0xB8 of the
    /// object the character's world handler returns, and no export reaches it -
    /// Interfaces.dll has N3Msg_RequestRaidLocks, which asks for it, and
    /// nothing that reads it. The reader is in Gamecode after all: the same
    /// dispatcher calls 0x100430C2, which walks +0xB8 at stride 0x14 and prints
    /// a line per entry under "Raid locks:". See <see cref="GameData.RaidLock"/>
    /// for what each line is made of.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.Raid)]
    public class RaidMessage : N3Message
    {
        #region Constructors and Destructors

        public RaidMessage()
        {
            this.N3MessageType = N3MessageType.Raid;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Which of the two things this is.
        /// </summary>
        [AoMember(0)]
        [AoFlags("kind")]
        public RaidUpdateType Kind { get; set; }

        /// <summary>
        /// The list format's version, and always 1.
        /// </summary>
        /// <remarks>
        /// 0x10057334 skips the whole list when it is anything else, so a
        /// different number here costs the client the locks and nothing more.
        /// </remarks>
        [AoMember(1)]
        [AoUsesFlags("kind", typeof(int), FlagsCriteria.EqualsToAny, new[] { (int)RaidUpdateType.Locks })]
        public int? Version { get; set; }

        /// <summary>
        /// The locks themselves, behind a plain int32 count.
        /// </summary>
        [AoMember(2, SerializeSize = ArraySizeType.Int32)]
        [AoUsesFlags("kind", typeof(RaidLock[]), FlagsCriteria.EqualsToAny, new[] { (int)RaidUpdateType.Locks })]
        public RaidLock[] Locks { get; set; }

        #endregion
    }
}
