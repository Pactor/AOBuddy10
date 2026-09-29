// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MailKind.cs" company="OmniCell">
//   Copyright (c) 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the MailKind type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// Which of nine shapes a <see cref="Messages.N3Messages.MailMessage"/> is.
    /// </summary>
    /// <remarks>
    /// The int16 at the front of the message, and the client's reader at
    /// Gamecode 0x100A35DA branches on it into nine bodies. Five of the names
    /// are the client's own: the constructor at 0x100A339F takes an id and a
    /// kind and is called from exactly four exported functions, each pushing
    /// its own number - N3Msg_RequestMailMessage pushes 1, N3Msg_MailTakeAll 3,
    /// N3Msg_DeleteMail 5 and N3Msg_ReturnMail 7 - and the second constructor,
    /// at 0x100A3206, is called only from N3Msg_SendMail and pushes 6.
    ///
    /// The other four are what the server sends, so nothing in the client
    /// names them; they are named here for the shape they carry, with what the
    /// captures suggest in the remarks.
    /// </remarks>
    public enum MailKind : short
    {
        /// <summary>
        /// The whole inbox: an X3F1-counted list of mail records.
        /// </summary>
        InboxList = 0,

        /// <summary>
        /// Send me the body of this mail. N3Msg_RequestMailMessage.
        /// </summary>
        RequestMessage = 1,

        /// <summary>
        /// One mail record, appended to the list the client already holds.
        /// </summary>
        /// <remarks>
        /// The reply to <see cref="RequestMessage"/>. The record carries the
        /// attachment block that the inbox listing leaves out.
        /// </remarks>
        MessageBody = 2,

        /// <summary>
        /// Take everything attached to every mail. N3Msg_MailTakeAll.
        /// </summary>
        TakeAll = 3,

        /// <summary>
        /// A mail id and an int32.
        /// </summary>
        /// <remarks>
        /// Named for its shape. What the one captured example suggests: the id
        /// is a mail the client already has in its inbox, and the int32 is that
        /// record's flags word with one more bit set - 0x5D against the 0x5C the
        /// same mail carried in the listing that arrived moments before. So it
        /// reads as the server saying a mail's flags have changed, which is what
        /// opening one would do. Not claimed: one capture, and the client's
        /// dispatcher only forwards the pair to a signal.
        /// </remarks>
        MailUpdate = 4,

        /// <summary>
        /// Delete this mail. N3Msg_DeleteMail.
        /// </summary>
        Delete = 5,

        /// <summary>
        /// Send a mail: three strings, an item, an amount and a flag.
        /// N3Msg_SendMail.
        /// </summary>
        Send = 6,

        /// <summary>
        /// Send this mail back. N3Msg_ReturnMail.
        /// </summary>
        Return = 7,

        /// <summary>
        /// An int16, then the same pair <see cref="MailUpdate"/> carries.
        /// </summary>
        /// <remarks>
        /// The reader falls straight into kind 4's code after taking the extra
        /// int16, so the tail is identical. No capture contains one.
        /// </remarks>
        MailUpdateWithCode = 8
    }
}
