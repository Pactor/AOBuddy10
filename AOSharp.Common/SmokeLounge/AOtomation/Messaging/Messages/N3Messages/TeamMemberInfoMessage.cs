// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TeamMemberInfoMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TeamMemberInfoMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.TeamMemberInfo)]
    public class TeamMemberInfoMessage : N3Message
    {
        #region Constructors and Destructors

        public TeamMemberInfoMessage()
        {
            this.N3MessageType = N3MessageType.TeamMemberInfo;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public Identity Character { get; set; }

        // The team window's live vitals for one member. Sent whenever that member's health or
        // nano changes, for members in THIS playfield only - so it keeps arriving for a teammate
        // who is out of render range, but stops entirely once he leaves the zone.
        // Wire-verified (sniffs/20260913-154958_s11.csv seq 628, s16 seq 350):
        //   Character=<a teammate> CurrentNano=347 MaxNano=347 MaxHealth=363 CurrentHealth=347
        [AoMember(1)]
        public int CurrentNano { get; set; }

        [AoMember(2)]
        public int MaxNano { get; set; }

        [AoMember(3)]
        public int MaxHealth { get; set; }

        [AoMember(4)]
        public int CurrentHealth { get; set; }

        #endregion
    }
}