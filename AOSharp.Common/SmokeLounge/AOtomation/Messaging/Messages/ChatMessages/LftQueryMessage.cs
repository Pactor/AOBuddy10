// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrivateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the PrivateGroupMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.ChatMessages
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class LftQueryMessage : ChatMessageBody
    {
        #region Public Properties

        public override ChatMessageType PacketType
        {
            get
            {
                return ChatMessageType.LftQuery;
            }
        }

        #endregion

        [AoMember(0)]
        public uint Side { get; set; }

        [AoMember(1)]
        public uint Profession { get; set; }

        [AoMember(2)]
        public uint Location { get; set; }

        [AoMember(3)]
        public uint Unknown1 { get; set; }
    }
}