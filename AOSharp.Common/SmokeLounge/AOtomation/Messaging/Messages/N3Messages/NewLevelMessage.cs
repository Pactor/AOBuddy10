// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NewLevelMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the NewLevelMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.NewLevel)]
    public class NewLevelMessage : N3Message
    {
        #region Constructors and Destructors

        public NewLevelMessage()
        {
            this.N3MessageType = N3MessageType.NewLevel;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public int Level { get; set; }

        [AoMember(1)]
        public int AvailableIp { get; set; }

        [AoMember(2)]
        public int CurrentXp { get; set; }

        [AoMember(3)]
        public int LastLevelXp { get; set; }

        [AoMember(4)]
        public int NextLevelXp { get; set; }

        [AoMember(5)]
        public int IpResetPointsGained { get; set; }

        [AoMember(6)]
        public int XpKillRange { get; set; }

        [AoMember(7)]
        public int LastAwardedXp { get; set; }

        #endregion
    }
}