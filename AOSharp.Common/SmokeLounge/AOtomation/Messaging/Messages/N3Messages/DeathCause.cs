// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeathCause.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the DeathCause type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    /// <summary>
    /// What killed a character, and which line the client prints for it.
    /// </summary>
    /// <remarks>
    /// The five arms of the switch at Gamecode.dll 0x1005B3F7, each pushing its
    /// own feedback string, and the routine finishes by setting stat 27, health,
    /// to zero - so this runs only when the character actually died.
    ///
    /// HealthDamage carries zero here in all 447 captured copies, which is what
    /// a capture with no deaths in it looks like.
    /// </remarks>
    public enum DeathCause
    {
        /// <summary>
        /// The character did not die. The client skips the whole routine.
        /// </summary>
        None = 0,

        /// <summary>
        /// Feedback_DeathByTerminate.
        /// </summary>
        Terminate = 1,

        /// <summary>
        /// Feedback_DeathByReflectDamage.
        /// </summary>
        ReflectDamage = 2,

        /// <summary>
        /// Feedback_DeathByShieldDamage.
        /// </summary>
        ShieldDamage = 3,

        /// <summary>
        /// Feedback_DeathByWeaponDamage.
        /// </summary>
        WeaponDamage = 4,

        /// <summary>
        /// Feedback_DeathBySpellDamage.
        /// </summary>
        SpellDamage = 5
    }
}
