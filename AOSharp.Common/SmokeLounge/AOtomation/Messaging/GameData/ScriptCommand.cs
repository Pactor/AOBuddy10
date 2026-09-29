// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScriptCommand.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ScriptCommand type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (GameData\ScriptCommand.cs); no changes.

    /// <summary>
    /// What a <see cref="Messages.N3Messages.ScriptMessage"/> asks the effect
    /// system to do.
    /// </summary>
    /// <remarks>
    /// The dispatcher at Gamecode 0x10130A1B switches on this at 0x10130B0A.
    /// Five values reach a branch and everything else falls to 0x10130BE9,
    /// which does nothing at all. "Supercede" is the client's own word: the
    /// variable the two superceding commands read is named SupercedeTime.
    /// </remarks>
    public enum ScriptCommand
    {
        /// <summary>
        /// Run the script and file the object it returns under the script id.
        /// </summary>
        Run = 0,

        /// <summary>
        /// Supercede the parent script, and drop the named script from the
        /// playfield if nothing is running under the script id.
        /// </summary>
        Supercede = 1,

        /// <summary>
        /// Run the script, file it, and then supercede the parent.
        /// </summary>
        RunAndSupercede = 2,

        /// <summary>
        /// Hand the script to the playfield under the hash of its name, with
        /// the time it has left.
        /// </summary>
        /// <remarks>
        /// This one also clears whatever was filed under the script id first.
        /// </remarks>
        Register = 3,

        /// <summary>
        /// Run it, but only near the position the message carries.
        /// </summary>
        /// <remarks>
        /// Inside 200 units it always runs; beyond that the client compares
        /// 40,000,000 over the squared distance against a millisecond counter
        /// taken modulo 1000, so it thins out with range rather than stopping.
        /// </remarks>
        RunIfNear = 4
    }
}
