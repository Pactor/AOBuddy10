// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MailRecord.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the MailRecord type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One mail, as the inbox lists it and as the message window shows it.
    /// </summary>
    /// <remarks>
    /// Read by Gamecode 0x10125F5F, and the layout below is that reader in
    /// order. The attachment block at the end is conditional: the reader takes
    /// a byte, compares it against 1, and only reads on when it is not 1 - so
    /// an inbox listing, where the byte is 1, stops at
    /// <see cref="HasNoAttachment"/>.
    ///
    /// Checked against retail. The shop-purchase mail in the 2026-09-11 capture
    /// arrives twice, once inside an <see cref="MailKind.InboxList"/> with the
    /// byte set and once as a <see cref="MailKind.MessageBody"/> with it clear,
    /// and both shapes read out exactly.
    /// </remarks>
    public class MailRecord
    {
        /// <summary>
        /// The mail's id. Read with BinaryStream's 64-bit operator.
        /// </summary>
        /// <remarks>
        /// The client reads it unsigned; this is a long because the model has no
        /// unsigned 64-bit serializer and the eight bytes are identical either
        /// way. It is an opaque handle - the four exported senders that ask the
        /// server to do something to a mail all take one of these and nothing
        /// else.
        /// </remarks>
        [AoMember(0)]
        public long MailId { get; set; }

        /// <summary>
        /// Reserved server metadata retained by the codec and ignored by this client.
        /// </summary>
        /// <remarks>
        /// Copied onward twice and read never. Zero in every captured copy.
        ///
        /// The inbox is the consumer worth naming, because the tool that used
        /// to justify this could not see it: the signal scan missed every connect
        /// made through a register-held GetInstance until 2026-09-12, and the
        /// mail window makes its own that way. GUI.dll connects 0x10109405 to
        /// GlobalSignals_c + 0x274, that handler walks the records at their
        /// 0x98 stride, and 0x10105867 copies each one field by field into a
        /// MailListItem_c - this word included, landing at the item's + 0x98.
        /// No method of either vtable MailListItem_c owns touches + 0x98. The
        /// message window, the other consumer, does not copy it at all.
        /// </remarks>
        [AoMember(1)]
        public int Unknown1 { get; set; }

        /// <summary>
        /// Who it is from. The inbox's "From" column.
        /// </summary>
        /// <remarks>
        /// Record + 0x14, and MailListItem_c answers column 1 from it. "Shop" in
        /// the captured copies, which is what a market purchase mail is from.
        /// </remarks>
        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public string From { get; set; }

        /// <summary>
        /// The subject. The inbox's "Subject" column.
        /// </summary>
        /// <remarks>
        /// Record + 0x30, column 2. "Your Item Purchase!" in the captures.
        /// </remarks>
        [AoMember(3, SerializeSize = ArraySizeType.Int16)]
        public string Subject { get; set; }

        /// <summary>
        /// When it was sent. A Unix timestamp. The inbox's "Sent" column.
        /// </summary>
        /// <remarks>
        /// Record + 0x50. The client keeps it as the low half of a 64-bit pair,
        /// zeroing the high half, and runs it through the date formatter at
        /// 0x10106307 - the one that prints "not-a-date-time", "+infinity" and
        /// "-infinity" for the sentinels.
        ///
        /// The captured value is 1789263109, which is 2026-09-13, two days after
        /// the session it arrived in.
        /// </remarks>
        [AoMember(4)]
        public int Sent { get; set; }

        /// <summary>
        /// When it expires. A Unix timestamp. The inbox's "Expires" column.
        /// </summary>
        /// <remarks>
        /// Record + 0x58, the same 64-bit pair treatment and the same formatter,
        /// column 4. The captured value is 1820492293 - 2027-09-08, a year and a
        /// day after <see cref="Sent"/>, which is what a mail expiry looks like.
        /// </remarks>
        [AoMember(5)]
        public int Expires { get; set; }

        /// <summary>
        /// A flags word. 0x5C in the captured copies.
        /// </summary>
        /// <remarks>
        /// Record + 0x60, and the message window reads four bits out of it: bit
        /// 1 suppresses the attachment, bit 4 gates the Return button, bit 5
        /// drives a ViewSelector and bit 6 replaces the body with canned text 25.
        ///
        /// A <see cref="MailKind.MailUpdate"/> arrived in the same session
        /// carrying this mail's id and 0x5D - this value with bit 0 set - which
        /// is what makes that kind read as a flags change.
        /// </remarks>
        [AoMember(6)]
        public int Flags { get; set; }

        /// <summary>
        /// 1 when nothing follows, and everything after this is absent.
        /// </summary>
        /// <remarks>
        /// The reader compares the byte against 1 and stops if it matches, so
        /// this is the record's own length switch rather than a field with a
        /// meaning of its own. An inbox listing sets it; the body of a mail with
        /// an attachment clears it.
        /// </remarks>
        [AoMember(7)]
        [AoFlags("attachment")]
        public byte HasNoAttachment { get; set; }

        /// <summary>
        /// Credits attached, or a charge to collect when negative.
        /// </summary>
        /// <remarks>
        /// Record + 0x64, and the message window reads it twice: positive it
        /// goes to the Cash view, and negative it is negated at 0x10107CA2 and
        /// goes to the C.O.D. view. One signed number doing both, which is why
        /// it is an amount rather than a count - <see cref="AttachmentCount"/>
        /// at + 0x74 is the count, and it belongs to the item beside it.
        ///
        /// Zero in the one captured copy that carries the block at all: a shop
        /// purchase delivers an item and asks for nothing.
        /// </remarks>
        [AoMember(8)]
        [AoUsesFlags("attachment", typeof(int), FlagsCriteria.NotEqualsToAny, new[] { 1 })]
        public int? Amount { get; set; }

        /// <summary>
        /// What is attached.
        /// </summary>
        /// <remarks>
        /// Read with GameData's own stream operator for ACGItem_t. The message
        /// window compares it against GameData::ACGItem_t::zero to decide whether
        /// to show an attachment slot at all.
        /// </remarks>
        [AoMember(9)]
        [AoUsesFlags("attachment", typeof(AcgItem), FlagsCriteria.NotEqualsToAny, new[] { 1 })]
        public AcgItem Attachment { get; set; }

        /// <summary>
        /// How many of the attached item. Zero in the captured copy.
        /// </summary>
        /// <remarks>
        /// Record + 0x74, and it rides with the item into the slot as its count.
        /// </remarks>
        [AoMember(10)]
        [AoUsesFlags("attachment", typeof(int), FlagsCriteria.NotEqualsToAny, new[] { 1 })]
        public int? AttachmentCount { get; set; }

        /// <summary>
        /// The body text.
        /// </summary>
        /// <remarks>
        /// Record + 0x78, and the message window puts it in the MessageText view.
        /// "Your purchased item is attached. Enjoy!" in the captured copy.
        /// </remarks>
        [AoMember(11, SerializeSize = ArraySizeType.Int16)]
        [AoUsesFlags("attachment", typeof(string), FlagsCriteria.NotEqualsToAny, new[] { 1 })]
        public string Body { get; set; }
    }
}
