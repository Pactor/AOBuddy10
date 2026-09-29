// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RelocateDynelsMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the RelocateDynelsMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging
    // (Messages\N3Messages\RelocateDynelsMessage.cs). Changes for this library only: AOSharp's
    // Identity/Vector3/Quaternion types.
    // AOBuddy census 2026-09-29: 0 copies in the bot's mission recordings and 2 in the retail dumps, every one read to
    // the last byte by this class. The remarks below that say no capture holds one are OmniCell's and predate these.

    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Items on the ground have become items a character is carrying.
    /// </summary>
    /// <remarks>
    /// Two identities and a list, and the dispatcher at Gamecode 0x1003A577
    /// says what each of the three is, because it resolves all of them and the
    /// casts it asks for are the names. The message's own identity is resolved
    /// as a SimpleChar_t; the identity in the body as a plain n3Dynel_t; each
    /// identity in the list as a SimpleItem_t.
    ///
    /// Then it does two things per listed item. It calls
    /// n3Dynel_t::RelocateDynel on the body dynel with the item, the null
    /// position and the null rotation - so the item becomes a child of the body
    /// dynel, sitting at its origin - and it hands the same item to the
    /// character's inventory at 0x10058903, with a slot of -1 meaning anywhere.
    /// An item that was standing in the world is now hanging off something and
    /// listed in someone's inventory, which is what picking something up looks
    /// like from the outside.
    ///
    /// Reader 0x1003A61D, writer 0x1003A4EA, vtable 0x1015E4C4. The two agree
    /// field for field, and the list helper the writer uses, 0x1003A73A, emits
    /// the X3F1 count and then one Identity per entry - the same shape the
    /// reader's 0x1002BA77 takes.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.RelocateDynels)]
    public class RelocateDynelsMessage : N3Message
    {
        #region Constructors and Destructors

        public RelocateDynelsMessage()
        {
            this.N3MessageType = N3MessageType.RelocateDynels;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// What the items are relocated onto.
        /// </summary>
        /// <remarks>
        /// Read at 0x1003A62C. The dispatcher resolves it with the plain
        /// GetDynel - no cast, so it is only ever asked to be an n3Dynel_t -
        /// and calls RelocateDynel on it once per listed item. The message's own
        /// identity is the character; this is the thing the items end up
        /// attached to.
        /// </remarks>
        [AoMember(0)]
        public Identity Destination { get; set; }

        /// <summary>
        /// The items being moved.
        /// </summary>
        /// <remarks>
        /// Read at 0x1003A635 by the shared X3F1-of-Identity reader. The
        /// dispatcher resolves each one as a SimpleItem_t before adding it to
        /// the character's inventory, and skips any that does not resolve or
        /// that is already on its way out.
        /// </remarks>
        [AoMember(1, SerializeSize = ArraySizeType.X3F1)]
        public Identity[] Items { get; set; }

        #endregion
    }
}
