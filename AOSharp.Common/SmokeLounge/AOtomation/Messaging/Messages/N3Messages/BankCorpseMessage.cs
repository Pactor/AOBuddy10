// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BankCorpseMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the BankCorpseMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\BankCorpseMessage.cs).
    // Changes for this library only: AOSharp's Identity/Vector3/Quaternion types.

    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// What is in the container the bank window is about to show.
    /// </summary>
    /// <remarks>
    /// Two fields. Reader 0x1007238B, writer 0x100723BA, dispatcher 0x100723DE,
    /// vtable 0x10161D54; the reader takes a container through the shared
    /// container reader at 0x1002A5DB - the same one FullCharacter's inventory
    /// uses - and then an int32, and the writer puts back the same two.
    ///
    /// The dispatcher resolves the message's identity to a character and then
    /// finishes the container off: it writes 0xDEAE into the container's +0x20
    /// and the int32 into its +0x24, which together are an Identity, and hangs
    /// the container on the character's inventory at +0x180. Only the instance
    /// half travels; the client supplies the type itself, which is why the wire
    /// carries a bare int32 and not an Identity.
    ///
    /// 0xDEAE is one of a run of window types the client already names -
    /// 0xDEA9 is the team window, 0xDEAA an organization, 0xDEAD the incoming
    /// trade window - and N3Msg_GetContainerInventoryList at 0x100175A6
    /// switches on exactly those four: asked about an identity of type 0xDEAE
    /// it returns 0x10046E1C, which reads the inventory's +0x180 and checks the
    /// container's +0x24 against the identity it was asked about. That closes
    /// the loop: the int32 here is what the client will later be asked for by
    /// name.
    ///
    /// The Bank message is the same container arriving the other way round: its
    /// dispatcher at 0x1007254B writes the character's own instance into +0x24
    /// instead of taking one off the wire, so Bank is your own bank and this is
    /// somebody else's.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.BankCorpse)]
    public class BankCorpseMessage : N3Message
    {
        #region Constructors and Destructors

        public BankCorpseMessage()
        {
            this.N3MessageType = N3MessageType.BankCorpse;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// What is in it.
        /// </summary>
        /// <remarks>
        /// Read at 0x10072399 by the shared container reader. Each entry is a
        /// placement, two int16s, an Identity and a GameData::ACGItem_t - the
        /// same record FullCharacter's inventory carries.
        /// </remarks>
        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public InventorySlot[] Contents { get; set; }

        /// <summary>
        /// The instance half of the container's identity.
        /// </summary>
        /// <remarks>
        /// Read at 0x100723A6 and written to the container's +0x24 at
        /// 0x1007240E, beside the 0xDEAE the client puts in +0x20 itself.
        /// </remarks>
        [AoMember(1)]
        public int Instance { get; set; }

        #endregion
    }
}
