// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterInfoPacket.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CharacterInfoPacket type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    /// <summary>
    /// The examine record of a player (NotAPlayer 0x10 clear). The record itself is on <see cref="InfoPacket"/>;
    /// this class keeps the AOSharp original's names that differ from it.
    /// </summary>
    /// <remarks>
    /// Changed 2026-09-29: <c>OrganizationId</c> is now <c>int</c> (unconditional on the wire) and
    /// <c>CityPlayfieldId</c> <c>int?</c> (only with the organization flag); neither was used by any code.
    /// </remarks>
    public class CharacterInfoPacket : InfoPacket
    {
        /// <summary>
        /// Alias of <see cref="InfoPacket.Version"/> (the AOSharp original's name).
        /// </summary>
        public byte Unknown1 { get => this.Version; set => this.Version = value; }

        /// <summary>
        /// Alias of <see cref="InfoPacket.AuxiliaryName"/> (the AOSharp original's name).
        /// </summary>
        public string LegacyTitle { get => this.AuxiliaryName; set => this.AuxiliaryName = value; }

        /// <summary>
        /// What the AOSharp original read here: the Int16 length of <see cref="InfoPacket.DisplayText"/>.
        /// </summary>
        public short Unknown2 => (short)(this.DisplayText?.Length ?? 0);
    }
}
