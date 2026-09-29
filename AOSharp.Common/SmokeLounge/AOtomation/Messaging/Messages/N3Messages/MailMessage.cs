using System;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace AOSharp.Common.SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    /// <summary>
    /// Mail, in nine shapes behind an int16.
    /// </summary>
    /// <remarks>
    /// Layout, names and doc comments ported from OmniCell's AOtomation.Messaging. AOSharp modelled
    /// only the Send shape (kind 6) and read every other kind wrongly (the retail captures: 11 of 12
    /// server copies and all 13 client copies threw).
    ///
    /// Extracted-client MailIIR_c has vtable 0x10167ECC, reader 0x100A35DA,
    /// writer 0x100A2FBA and dispatcher 0x100A2D91. The reader takes an int16
    /// and branches: 0 reads an X3F1-counted list of records; 1, 3, 5 and 7 read
    /// nothing but a 64-bit id; 2 reads one record; 4 reads an id and an int32;
    /// 6 reads three strings, an Identity, an int32 and a byte kept as a
    /// boolean; 8 reads a second int16 and then falls into 4's code.
    /// See <see cref="MailKind"/> for where the names come from.
    ///
    /// The dispatcher does not act on any of it. Every branch ends at a signal
    /// on GlobalSignals_c - + 0x274 for the list, + 0x278 for one record,
    /// + 0x27C for kind 4 and + 0x280 for kind 8 - and the subscribers are the
    /// inbox window and the message window in GUI.dll.
    /// </remarks>
    [AoContract((int)N3MessageType.Mail)]
    public class MailMessage : N3Message
    {
        public MailMessage()
        {
            this.N3MessageType = N3MessageType.Mail;
        }

        #region AoMember Properties

        /// <summary>
        /// Which shape the rest of the message is.
        /// </summary>
        [AoMember(0)]
        [AoFlags("kind")]
        public MailKind Kind { get; set; }

        /// <summary>
        /// The whole inbox, on a <see cref="MailKind.InboxList"/>. X3F1-counted.
        /// </summary>
        [AoMember(1, SerializeSize = ArraySizeType.X3F1)]
        [AoUsesFlags("kind", typeof(MailRecord[]), FlagsCriteria.EqualsToAny,
            new[] { (int)MailKind.InboxList })]
        public MailRecord[] Inbox { get; set; }

        /// <summary>
        /// One mail, on a <see cref="MailKind.MessageBody"/>.
        /// </summary>
        /// <remarks>
        /// Not a list of one: the reader takes a bare record with no count in
        /// front of it and appends it to the vector the client already holds.
        /// </remarks>
        [AoMember(2)]
        [AoUsesFlags("kind", typeof(MailRecord), FlagsCriteria.EqualsToAny,
            new[] { (int)MailKind.MessageBody })]
        public MailRecord Message { get; set; }

        /// <summary>
        /// The extra int16 that only <see cref="MailKind.MailUpdateWithCode"/>
        /// carries, ahead of the id.
        /// </summary>
        /// <remarks>
        /// The reader takes it and then jumps into kind 4's code, so it comes
        /// before <see cref="MailId"/> on the wire.
        /// </remarks>
        [AoMember(3)]
        [AoUsesFlags("kind", typeof(short), FlagsCriteria.EqualsToAny,
            new[] { (int)MailKind.MailUpdateWithCode })]
        public short? Code { get; set; }

        /// <summary>
        /// Which mail. Read with BinaryStream's 64-bit operator.
        /// </summary>
        /// <remarks>
        /// Carried by six of the nine kinds - the four the client sends to act
        /// on a single mail, and the two the server sends to update one. A long
        /// rather than a ulong because the model has no unsigned 64-bit
        /// serializer and the eight bytes are the same either way.
        /// </remarks>
        [AoMember(4)]
        [AoUsesFlags("kind", typeof(long), FlagsCriteria.EqualsToAny,
            new[]
                {
                    (int)MailKind.RequestMessage, (int)MailKind.TakeAll, (int)MailKind.MailUpdate,
                    (int)MailKind.Delete, (int)MailKind.Return, (int)MailKind.MailUpdateWithCode
                })]
        public long? MailId { get; set; }

        /// <summary>
        /// The value the two update kinds carry beside the id.
        /// </summary>
        /// <remarks>
        /// The captured one is 0x5D against a mail whose record carried 0x5C, so
        /// it reads as that record's flags word with one more bit set. See
        /// <see cref="MailKind.MailUpdate"/>.
        /// </remarks>
        [AoMember(5)]
        [AoUsesFlags("kind", typeof(int), FlagsCriteria.EqualsToAny,
            new[] { (int)MailKind.MailUpdate, (int)MailKind.MailUpdateWithCode })]
        public int? Value { get; set; }

        /// <summary>
        /// Who to send it to. Only on a <see cref="MailKind.Send"/>.
        /// </summary>
        /// <remarks>
        /// The six fields of a send are named by the compose window rather than
        /// by the export: GUI.dll pulls its arguments out of a Utils.dll Message
        /// by key at 0x101065C5 onward, in the order N3Msg_SendMail pushes them -
        /// "To", "Subject", "Text", "Item", "Cash" and "Express".
        /// </remarks>
        [AoMember(6, SerializeSize = ArraySizeType.Int16)]
        [AoUsesFlags("kind", typeof(string), FlagsCriteria.EqualsToAny, new[] { (int)MailKind.Send })]
        public string To { get; set; }

        /// <summary>
        /// The subject line. Only on a <see cref="MailKind.Send"/>.
        /// </summary>
        [AoMember(7, SerializeSize = ArraySizeType.Int16)]
        [AoUsesFlags("kind", typeof(string), FlagsCriteria.EqualsToAny, new[] { (int)MailKind.Send })]
        public string Subject { get; set; }

        /// <summary>
        /// The body. Only on a <see cref="MailKind.Send"/>.
        /// </summary>
        [AoMember(8, SerializeSize = ArraySizeType.Int16)]
        [AoUsesFlags("kind", typeof(string), FlagsCriteria.EqualsToAny, new[] { (int)MailKind.Send })]
        public string Text { get; set; }

        /// <summary>
        /// The item to attach. Only on a <see cref="MailKind.Send"/>.
        /// </summary>
        [AoMember(9)]
        [AoUsesFlags("kind", typeof(Identity), FlagsCriteria.EqualsToAny, new[] { (int)MailKind.Send })]
        public Identity? Item { get; set; }

        /// <summary>
        /// Credits to send with it. Only on a <see cref="MailKind.Send"/>.
        /// </summary>
        [AoMember(10)]
        [AoUsesFlags("kind", typeof(int), FlagsCriteria.EqualsToAny, new[] { (int)MailKind.Send })]
        public int? Cash { get; set; }

        /// <summary>
        /// The Standard or Express choice, as a byte. Only on a <see cref="MailKind.Send"/>.
        /// </summary>
        /// <remarks>
        /// The radio button in Views/MailMessageWindow.xml, whose two values are
        /// 0 and 1. The client's reader compares the byte against 1 and keeps the
        /// answer as a boolean.
        /// </remarks>
        [AoMember(11)]
        [AoUsesFlags("kind", typeof(byte), FlagsCriteria.EqualsToAny, new[] { (int)MailKind.Send })]
        public byte? Express { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is Kind (MailKind).")]
        public short Unknown1 { get => (short)this.Kind; set => this.Kind = (MailKind)value; }

        [Obsolete("Wire field is To (Send only).")]
        public string Recipient { get => this.To; set => this.To = value; }

        [Obsolete("Wire field is Text (Send only).")]
        public string Body { get => this.Text; set => this.Text = value; }

        [Obsolete("Wire field is Cash (Send only).")]
        public int Credits { get => this.Cash ?? 0; set => this.Cash = value; }

        #endregion
    }
}
