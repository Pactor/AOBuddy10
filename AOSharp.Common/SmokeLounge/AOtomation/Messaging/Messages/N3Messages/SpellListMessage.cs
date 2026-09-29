// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpellListMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SpellListMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A nano program (or other spell) running on a character: its effects, who cast it and on whom.
    /// </summary>
    /// <remarks>
    /// Ported 2026-09-29 from OmniCell's Messages\N3Messages\SpellListMessage.cs and SpellListSerializer.cs, which
    /// read every captured copy with zero bytes left. The AOSharp original had every member commented out and read
    /// nothing. This is "an effect is being applied/running", NOT "a nano was learned": in the mission recordings
    /// "Ambient Restoration" (effect 53066, on self), "Mesmeric Gaze" (HasNano 233826, on mobs) and "Minor
    /// Regeneration of Health and Nano" (HasNano 291081, on self) dominate.
    ///
    /// Wire order: X3F1 NanoEffect list, Identity Source, Identity Character, byte SetSpellFlag, Int16-counted
    /// Name, byte HasNano, Identity Nano only when HasNano != 0, byte UnreadFlag, int32 ApplyScope.
    /// </remarks>
    [AoContract((int)N3MessageType.SpellList)]
    public class SpellListMessage : N3Message
    {
        #region Constructors and Destructors

        public SpellListMessage()
        {
            this.N3MessageType = N3MessageType.SpellList;
            this.NanoEffects = new NanoEffectRecord[0];
            this.Name = string.Empty;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The effects the spell applies, read by the client's effect reader (Gamecode.dll 0x100A71AE).
        /// </summary>
        [AoMember(0)]
        public NanoEffectRecord[] NanoEffects { get; set; }

        /// <summary>
        /// Identity.None in about half the captures, the caster (a CanbeAffected identity) in the rest.
        /// </summary>
        /// <remarks>
        /// The client skips it when None, otherwise looks it up and casts it to Beholder_t (OmniCell
        /// SpellListMessage.cs:63-78).
        /// </remarks>
        [AoMember(1)]
        public Identity Source { get; set; }

        /// <summary>
        /// The character the spell is running on.
        /// </summary>
        [AoMember(2)]
        public Identity Character { get; set; }

        /// <summary>
        /// Sets or clears bit 0 of the spell object's flag word on the client (Gamecode.dll 0x10002823).
        /// </summary>
        [AoMember(3)]
        public byte SetSpellFlag { get; set; }

        /// <summary>
        /// The spell's name ("Ambient Restoration", "Mesmeric Gaze"); empty for unnamed effects.
        /// </summary>
        /// <remarks>
        /// Int16-counted: Gamecode.dll 0x10038AF8 is the short-counted string reader used here.
        /// </remarks>
        [AoMember(4, SerializeSize = ArraySizeType.Int16)]
        public string Name { get; set; }

        /// <summary>
        /// Whether <see cref="Nano"/> follows.
        /// </summary>
        [AoMember(5)]
        [AoFlags("spellListHasNano")]
        public byte HasNano { get; set; }

        /// <summary>
        /// The nano program itself (type NanoProgram), present only when <see cref="HasNano"/> is non-zero.
        /// </summary>
        [AoMember(6)]
        [AoUsesFlags("spellListHasNano", typeof(Identity), FlagsCriteria.HasAny, int.MaxValue)]
        public Identity? Nano { get; set; }

        /// <summary>
        /// A flag the client reads and never uses (followed to 0x100A5F4B, which ignores it). 0 in every capture.
        /// </summary>
        [AoMember(7)]
        public byte UnreadFlag { get; set; }

        /// <summary>
        /// Which effects the client applies: 0 all; 1 only the appearance functions (53035, 53037, 53038, 53039,
        /// 53054, 53055); 2 the same less Texture (gate at Gamecode.dll 0x10002779).
        /// </summary>
        [AoMember(8)]
        public int ApplyScope { get; set; }

        #endregion
    }
}
