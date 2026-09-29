// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CentralControllerStatus.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CentralControllerStatus type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging
    // (Messages\N3Messages\CentralControllerStatus.cs); no changes.

    /// <summary>
    /// Whether a central controller is running, switched off, or wrecked.
    /// </summary>
    /// <remarks>
    /// Proven twice over. Gamecode.dll 0x1007E14A picks one of three strings for
    /// the controller's name - Active, Inactive, Destroyed - and 0x1007E258
    /// picks the notification the client plays, ControllerDeactivated when this
    /// is 1 and ControllerDestroyed otherwise, which is why zero plays nothing.
    ///
    /// Both readers refuse anything above 2: the one byte of
    /// CentralControllerState at 0x1009F63A, and the second byte of
    /// CentralControllerFullUpdate at 0x1009F499. They hand it to the same
    /// setter, 0x1007E206, so the standalone message carries exactly the field
    /// the full update carries.
    /// </remarks>
    public enum CentralControllerStatus : byte
    {
        Active = 0,

        Inactive = 1,

        Destroyed = 2
    }
}
