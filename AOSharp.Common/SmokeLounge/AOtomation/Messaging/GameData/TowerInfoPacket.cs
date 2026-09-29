// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TowerInfoPacket.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TowerInfoPacket type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using System.Text;

    /// <summary>
    /// The examine record of a tower (NotAPlayer 0x10 with HasAcgItems 0x04 or Suppression 0x08). The record is on
    /// <see cref="InfoPacket"/>; the AOSharp original's flat field names are kept below as read-only aliases of the
    /// record fields they were really reading.
    /// </summary>
    public class TowerInfoPacket : InfoPacket
    {
        /// <summary>Alias of <see cref="InfoPacket.Version"/>.</summary>
        public byte Unknown1 => this.Version;

        /// <summary>Alias of <see cref="InfoPacket.Profession"/>.</summary>
        public byte Unknown2 => (byte)this.Profession;

        /// <summary>Alias of <see cref="InfoPacket.Level"/>.</summary>
        public byte Unknown3 => this.Level;

        /// <summary>Alias of <see cref="InfoPacket.TitleLevel"/>.</summary>
        public byte Unknown4 => this.TitleLevel;

        /// <summary>Alias of <see cref="InfoPacket.VisualProfession"/>.</summary>
        public byte Unknown5 => (byte)this.VisualProfession;

        /// <summary>High byte of <see cref="InfoPacket.SideXp"/>.</summary>
        public byte Unknown6 => (byte)((ushort)this.SideXp >> 8);

        /// <summary>Low byte of <see cref="InfoPacket.SideXp"/>.</summary>
        public byte Unknown7 => (byte)this.SideXp;

        /// <summary>Alias of <see cref="InfoPacket.BreedHostility"/>.</summary>
        public int Unknown8 => this.BreedHostility;

        /// <summary>Length of <see cref="InfoPacket.FirstName"/>.</summary>
        public short Unknown9 => (short)(this.FirstName?.Length ?? 0);

        /// <summary>Length of <see cref="InfoPacket.LastName"/>.</summary>
        public short Unknown10 => (short)(this.LastName?.Length ?? 0);

        /// <summary>Length of <see cref="InfoPacket.AuxiliaryName"/>.</summary>
        public short Unknown11 => (short)(this.AuxiliaryName?.Length ?? 0);

        /// <summary>The bytes of <see cref="InfoPacket.DisplayText"/>.</summary>
        public byte[] FormattedText => Encoding.ASCII.GetBytes(this.DisplayText ?? string.Empty);

        /// <summary>The X3F1 header of <see cref="InfoPacket.Towers"/>.</summary>
        public int TowerCount3F1 => ((this.Towers?.Length ?? 0) + 1) * 0x3F1;

        /// <summary>First ACG item's low id.</summary>
        public int TowerLowId => this.Towers != null && this.Towers.Length > 0 ? this.Towers[0].LowId : 0;

        /// <summary>First ACG item's high id.</summary>
        public int TowerHighId => this.Towers != null && this.Towers.Length > 0 ? this.Towers[0].HighId : 0;

        /// <summary>First ACG item's quality.</summary>
        public int TowerQuality => this.Towers != null && this.Towers.Length > 0 ? this.Towers[0].Quality : 0;

        /// <summary>First ACG item's unused fourth word.</summary>
        public int Unknown12 => this.Towers != null && this.Towers.Length > 0 ? this.Towers[0].Unknown : 0;

        /// <summary>Alias of <see cref="InfoPacket.SuppressionTimer"/>.</summary>
        public int? Timer => this.SuppressionTimer;

        /// <summary>Alias of <see cref="InfoPacket.SuppressionLevel"/>.</summary>
        public byte? NextSuppressionGas => this.SuppressionLevel;

        /// <summary>Alias of <see cref="InfoPacket.InvadersKilled"/>.</summary>
        public int Unknown14 => this.InvadersKilled;

        /// <summary>Alias of <see cref="InfoPacket.KilledByInvaders"/>.</summary>
        public int Unknown15 => this.KilledByInvaders;

        /// <summary>Alias of <see cref="InfoPacket.AiLevel"/>.</summary>
        public int Unknown16 => this.AiLevel;
    }
}
