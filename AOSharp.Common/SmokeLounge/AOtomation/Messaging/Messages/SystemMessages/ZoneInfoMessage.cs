// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ZoneInfoMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the ZoneInfoMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.SystemMessages
{
    using System.Net;

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)SystemMessageType.ZoneInfo)]
    public class ZoneInfoMessage : SystemMessage
    {
        #region Constructors and Destructors

        public ZoneInfoMessage()
        {
            this.SystemMessageType = SystemMessageType.ZoneInfo;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public int CharacterId { get; set; }

        [AoMember(1)]
        public IPAddress ServerIpAddress { get; set; }

        [AoMember(2)]
        public ushort ServerPort { get; set; }

        [AoMember(3)]
        public uint Cookie1 { get; set; }

        [AoMember(4)]
        public uint Cookie2 { get; set; }

        // Members 5-6 added 2026-09-29 from OmniCell (Messages\SystemMessages\ZoneInfoMessage.cs:83-94); the AOSharp
        // original left these 8 bytes unread. The client stops reading after the cookie and does not need them.

        /// <summary>
        /// 0 for the copy pointing at port 7501, 1 for those pointing at 7509 and 7512 (OmniCell's captures); 1 in
        /// the AOBuddy recordings. Meaning open.
        /// </summary>
        [AoMember(5)]
        public int Unknown1 { get; set; }

        /// <summary>
        /// 0x59DAD28A in OmniCell's three captures, 0x3BAD896B in the AOBuddy recordings: the server's own datum,
        /// not a literal the client checks.
        /// </summary>
        [AoMember(6)]
        public uint Unknown2 { get; set; }

        #endregion
    }
}