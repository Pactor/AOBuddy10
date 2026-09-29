// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DamageType.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the DamageType type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    /// <summary>
    /// What kind of damage was dealt, in the client's own numbering.
    /// </summary>
    /// <remarks>
    /// The whole table, ids and names, is built in one run at Gamecode.dll
    /// 0x10033C41 - each entry is an int32 written to a stack slot and a string
    /// pushed straight after it - and the client looks a value up in it at
    /// 0x10036CF5, printing "Missing damagetype: %d" when it is not there.
    ///
    /// Eight of the twelve are the armour classes, and their ids are the armour
    /// class stat ids: 90 to 97 are projectileac through fireac. The other four
    /// are not armour at all. Nano is 168, Unknown is 27 - and 27 is what the
    /// message formatter substitutes when the field arrives as zero, at
    /// 0x10012C87 - Fall is 474, which is the falldamage stat, and Backstab is
    /// 489.
    ///
    /// Note that Poison and Fire are the other way round from the order the
    /// table is written in: the client assigns 0x61 to fire and then 0x60 to
    /// poison, which is easy to transcribe backwards.
    /// </remarks>
    public enum DamageType
    {
        /// <summary>
        /// Sent as zero, and the formatter turns it into Unknown.
        /// </summary>
        None = 0,

        /// <summary>
        /// 27, and what a zero on the wire becomes.
        /// </summary>
        Unknown = 27,

        /// <summary>
        /// Stat 90, projectileac.
        /// </summary>
        Projectile = 90,

        /// <summary>
        /// Stat 91, meleeac.
        /// </summary>
        Melee = 91,

        /// <summary>
        /// Stat 92, energyac.
        /// </summary>
        Energy = 92,

        /// <summary>
        /// Stat 93, chemicalac.
        /// </summary>
        Chemical = 93,

        /// <summary>
        /// Stat 94, radiationac.
        /// </summary>
        Radiation = 94,

        /// <summary>
        /// Stat 95, coldac.
        /// </summary>
        Cold = 95,

        /// <summary>
        /// Stat 96, poisonac.
        /// </summary>
        Poison = 96,

        /// <summary>
        /// Stat 97, fireac.
        /// </summary>
        Fire = 97,

        /// <summary>
        /// 168. Not an armour class.
        /// </summary>
        Nano = 168,

        /// <summary>
        /// 474, which is also the falldamage stat. The damage message has a
        /// branch of its own for this one.
        /// </summary>
        Fall = 474,

        /// <summary>
        /// 489.
        /// </summary>
        Backstab = 489
    }
}
