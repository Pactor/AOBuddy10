// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TeamMemberMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TeamMemberMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.TeamMember)]
    public class TeamMemberMessage : N3Message
    {
        #region Constructors and Destructors

        public TeamMemberMessage()
        {
            this.N3MessageType = N3MessageType.TeamMember;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public Identity Character { get; set; }

        [AoMember(1)]
        public Identity Team { get; set; }

        // Wire-verified (sniffs/20260913-154958_s11.csv seq 627/630):
        //   one member: RaidGroup=-1 Level=15 Profession=8  (8 = Bureaucrat, and his pet in that
        //       same capture is a "Bureaucrat Worker", which is what confirms the field)
        //   the other:  RaidGroup=-1 Level=17 Profession=14 (14 = Keeper)
        // The previous layout read Profession and the name's length as one int32 plus an int16
        // count. It parsed the name correctly by accident - the count's high half is zero for any
        // real name - while losing the profession entirely.
        [AoMember(2)]
        public int RaidGroup { get; set; }

        [AoMember(3)]
        public int Level { get; set; }

        [AoMember(4)]
        public short Profession { get; set; }

        [AoMember(5, SerializeSize = ArraySizeType.Int32)]
        public string Name { get; set; }

        #endregion
    }
}