// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MissionItemReward.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the MissionItemReward type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class MissionItemReward
    {
        #region AoMember Properties

        [AoMember(0)]
        public int LowId { get; set; }

        [AoMember(1)]
        public int HighId { get; set; }

        [AoMember(2)]
        public int Ql { get; set; }

        [AoMember(3)]
        public int Unk { get; set; }

        #endregion
    }
}