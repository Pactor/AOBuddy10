// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpecialAttackInfoMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SpecialAttackInfoMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.SpecialAttackInfo)]
    public class SpecialAttackInfoMessage : N3Message
    {
        #region Constructors and Destructors

        public SpecialAttackInfoMessage()
        {
            this.N3MessageType = N3MessageType.SpecialAttackInfo;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public EquipSlot EquipSlot { get; set; }

        [AoMember(1)]
        public int Amount { get; set; }

        [AoMember(2)]
        public int AmmoCount { get; set; }

        [AoMember(3)]
        public Identity Target { get; set; }

        [AoMember(4)]
        public Stat Stat { get; set; }

        [AoMember(5)]
        public int Unk1 { get; set; }
        #endregion
    }
}