// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServerPosDebugInfoMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ServerPosDebugInfoMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging
    // (Messages\N3Messages\ServerPosDebugInfoMessage.cs). Changes for this library only: AOSharp's
    // Identity/Vector3/Quaternion types.

    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Where the server thinks the character is, for the debug overlay to draw
    /// beside where the client thinks it is.
    /// </summary>
    /// <remarks>
    /// Three Vector3s and nothing else. Reader 0x100774EE, writer 0x100774BC,
    /// dispatcher 0x10077527, vtable 0x10162360; the reader takes twelve floats
    /// and the writer puts back the same twelve.
    ///
    /// The dispatcher resolves the message identity to a character and copies
    /// the three vectors onto it at 0x10057777 - the first to the character's
    /// +0x224, the second to +0x230, the third to +0x23C.
    ///
    /// What the first two are for is settled by the only place that reads them
    /// back, at 0x1005ABFB, which runs every frame. It gives up at once if the
    /// vector at +0x224 is zero. Otherwise it calls
    /// VisualEnvFX_t::DisplaySyncPosition - exported from DisplaySystem.dll at
    /// 0x10060E67, taking four Vector3s - with the character's own
    /// Vehicle_t::GetGlobalPos as the first and last argument, +0x224 as the
    /// second, and +0x230 as the third; when +0x230 is zero it passes the
    /// client's own position there instead. DisplaySyncPosition draws a marker
    /// at each of the four in a different colour and joins them, and it does
    /// nothing at all unless bit 0x400 of the display flags is set - a debug
    /// overlay, which is what the message is called.
    ///
    /// So the first vector is the server's position for the character and the
    /// second is a second one it can send beside it. The third has no reader.
    /// See <see cref="Unknown1"/>.
    ///
    /// No capture contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.ServerPosDebugInfo)]
    public class ServerPosDebugInfoMessage : N3Message
    {
        #region Constructors and Destructors

        public ServerPosDebugInfoMessage()
        {
            this.N3MessageType = N3MessageType.ServerPosDebugInfo;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Where the server has the character.
        /// </summary>
        /// <remarks>
        /// Copied to the character's +0x224 and passed to DisplaySyncPosition
        /// as its second argument. Zero switches the whole overlay off: the
        /// reader at 0x1005AC04 checks this one first and returns.
        /// </remarks>
        [AoMember(0)]
        public Vector3 ServerPosition { get; set; }

        /// <summary>
        /// A second position to draw beside it.
        /// </summary>
        /// <remarks>
        /// Copied to +0x230 and passed as the third argument. When it is zero
        /// the client substitutes its own position at 0x1005AC29, so it is
        /// optional rather than required.
        /// </remarks>
        [AoMember(1)]
        public Vector3 SecondPosition { get; set; }

        /// <summary>
        /// A third vector with no reader.
        /// </summary>
        /// <remarks>
        /// Copied to the character's +0x23C at 0x1005779D and, as far as can be
        /// found, never read. The overlay takes the two above and not this one.
        /// The search was a scan of Gamecode's whole text section for memory
        /// operands with a displacement of 0x23C, which catches the plain
        /// [register + 0x23C] form and would miss an access computed off a
        /// pointer taken earlier - so this is "no reader found", not "no reader
        /// exists". It is the one field keeping this message off the green
        /// list.
        /// </remarks>
        [AoMember(2)]
        public Vector3 Unknown1 { get; set; }

        #endregion
    }
}
