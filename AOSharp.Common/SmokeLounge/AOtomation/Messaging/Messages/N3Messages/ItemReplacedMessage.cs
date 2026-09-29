// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ItemReplacedMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ItemReplacedMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\ItemReplacedMessage.cs);
    // no changes.
    // AOBuddy census 2026-09-29: 0 copies in the bot's mission recordings and 366 in the retail dumps, every one read
    // to the last byte by this class. The remarks below that say no capture holds one are OmniCell's and predate
    // these.

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One of the character's worn items has turned into a different one.
    /// </summary>
    /// <remarks>
    /// Three fields and all three are named by the one call the dispatcher
    /// makes. Reader 0x10003EDC, writer 0x10003F19, dispatcher 0x10003F4D,
    /// vtable 0x10155D74; the reader takes an int32 and then two ACGItem_ts
    /// through GameData's own stream operator, and the writer puts back the
    /// same three.
    ///
    /// The dispatcher resolves the message's identity to a character, takes
    /// that character's inventory at +0x1B8, and hands it the three fields at
    /// 0x1004D44C. That function refuses a slot above 0x3F, which is what makes
    /// the int32 a placement; it asks n3Dynel_t::IsClientChar whether this is
    /// our own character, and if it is, it reads what is actually in the slot
    /// and compares it against the third field, doing nothing when they already
    /// match - so the third field is what the slot should end up holding. If it
    /// is somebody else's character it cannot look, and uses the second field
    /// as what was there before. Then it clears the slot at 0x1004D4EE and puts
    /// the third field in at 0x1004D502.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.ItemReplaced)]
    public class ItemReplacedMessage : N3Message
    {
        #region Constructors and Destructors

        public ItemReplacedMessage()
        {
            this.N3MessageType = N3MessageType.ItemReplaced;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Which slot, 0 to 0x3F.
        /// </summary>
        /// <remarks>
        /// The handler at 0x1004D456 refuses anything above 0x3F before it does
        /// anything else.
        /// </remarks>
        [AoMember(0)]
        public int Placement { get; set; }

        /// <summary>
        /// What was in the slot.
        /// </summary>
        /// <remarks>
        /// Only used when the character is not the one this client is playing -
        /// for our own character the client reads the slot instead of being
        /// told.
        /// </remarks>
        [AoMember(1)]
        public AcgItem Old { get; set; }

        /// <summary>
        /// What is in it now.
        /// </summary>
        [AoMember(2)]
        public AcgItem New { get; set; }

        #endregion
    }
}
