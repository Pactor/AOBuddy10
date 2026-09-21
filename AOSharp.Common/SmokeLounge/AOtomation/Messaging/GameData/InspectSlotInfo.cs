// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NanoEffect.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the NanoEffect type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    public class InspectSlotInfo
    {
        [AoMember(0)]
        public EquipSlot EquipSlot { get; set; }

        [AoMember(1)]
        public int Unk1 { get; set; }

        [AoMember(2)]
        public Identity UniqueIdentity { get; set; }

        [AoMember(3)]
        public int LowId { get; set; }

        [AoMember(4)]
        public int HighId { get; set; }

        [AoMember(5)]
        public int Ql { get; set; }

        [AoMember(6)]
        public int Unk2 { get; set; }
    }
}