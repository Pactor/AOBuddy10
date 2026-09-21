// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TeamInviteMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TeamInviteMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.TeamInvite)]
    public class TeamInviteMessage : N3Message
    {
        #region Constructors and Destructors

        public TeamInviteMessage()
        {
            this.N3MessageType = N3MessageType.TeamInvite;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public Identity Requestor { get; set; }

        [AoMember(1)]
        public byte Unknown1 { get; set; }

        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public string Name { get; set; }

        #endregion
    }
}