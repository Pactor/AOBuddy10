// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CentralControllerStateMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CentralControllerStateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging
    // (Messages\N3Messages\CentralControllerStateMessage.cs); no changes.

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// One byte: a central controller has been switched off or destroyed.
    /// </summary>
    /// <remarks>
    /// The whole message. The reader at Gamecode.dll 0x1009F624 reads a single
    /// byte, refuses anything above 2, and the dispatcher at 0x1009F5E2 resolves
    /// the message identity to a controller and hands the byte to the same
    /// setter CentralControllerFullUpdate uses for its own state byte.
    ///
    /// Has never appeared in a capture. The layout is from the client, and the
    /// meaning of the byte is from CentralControllerFullUpdate, which has.
    /// </remarks>
    [AoContract((int)N3MessageType.CentralControllerState)]
    public class CentralControllerStateMessage : N3Message
    {
        #region Constructors and Destructors

        public CentralControllerStateMessage()
        {
            this.N3MessageType = N3MessageType.CentralControllerState;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public CentralControllerStatus Status { get; set; }

        #endregion
    }
}
