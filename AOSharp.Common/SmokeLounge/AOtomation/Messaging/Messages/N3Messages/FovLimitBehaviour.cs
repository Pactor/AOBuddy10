// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FovLimitBehaviour.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the FovLimitBehaviour type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\FovLimitBehaviour.cs);
    // no changes.

    /// <summary>
    /// What an NPC's head does when its sweep reaches the end of its arc.
    /// </summary>
    /// <remarks>
    /// One signed byte, and Gamecode.dll 0x100525CA refuses anything outside 0
    /// to 2 - it writes a zero over any other value before the sweep ever runs.
    ///
    /// The three cases are the three arms of the switch the sweep takes when
    /// the current angle passes a limit, at 0x10052397 for the upper limit and
    /// 0x10052432 for the lower. Every captured copy carries Bounce.
    /// </remarks>
    public enum FovLimitBehaviour : byte
    {
        /// <summary>
        /// Stop at the limit and turn back the other way.
        /// </summary>
        /// <remarks>
        /// The sweep pins the current angle to the limit and then replaces the
        /// turn speed: minus TurnDownSpeed at the upper limit (0x10052405),
        /// plus TurnUpSpeed at the lower (0x10052456). This is why the captured
        /// TurnSpeed is only ever plus or minus the two speeds beside it.
        /// </remarks>
        Bounce = 0,

        /// <summary>
        /// Jump to the opposite limit and carry on the same way.
        /// </summary>
        /// <remarks>
        /// The angle is set to the far end of the arc and the speed is left
        /// alone, so the head sweeps the same direction over and over.
        /// </remarks>
        Wrap = 1,

        /// <summary>
        /// Stop at the limit and stay there.
        /// </summary>
        Hold = 2
    }
}
