// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CentralControllerKind.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CentralControllerKind type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging
    // (Messages\N3Messages\CentralControllerKind.cs); no changes.

    /// <summary>
    /// What a central controller controls.
    /// </summary>
    /// <remarks>
    /// The client's own words. Gamecode.dll 0x1007E165 switches on this value to
    /// pick one of three strings and builds the controller's name out of them
    /// with the format "%s %s controller" at 0x10162F10, the other half being
    /// <see cref="CentralControllerState"/>.
    ///
    /// A captured controller carries kind 2 and state 0, and the name on the
    /// wire beside them reads "Active sentry controller" - so the two bytes and
    /// the name agree without anything having to be assumed.
    ///
    /// The reader at 0x1009F492 refuses anything above 3.
    /// </remarks>
    public enum CentralControllerKind : byte
    {
        None = 0,

        Mine = 1,

        Sentry = 2,

        Fence = 3
    }
}
