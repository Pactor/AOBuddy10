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
//   Defines the BuffMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Serialization;
using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

namespace AOSharp.Common.SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    [AoContract((int)N3MessageType.Buff)]
    public class BuffMessage : N3Message
    {
        #region Constructors and Destructors

        public BuffMessage()
        {
            this.N3MessageType = N3MessageType.Buff;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public short Unknown1 { get; set; }

        [AoMember(1)]
        public Identity Buff { get; set; }

        #endregion
    }
}
