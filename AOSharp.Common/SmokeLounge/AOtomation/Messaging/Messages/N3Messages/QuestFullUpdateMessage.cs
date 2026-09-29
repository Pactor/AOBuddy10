// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QuestFullUpdateMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the QuestFullUpdateMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.QuestFullUpdate)]
    public class QuestFullUpdateMessage : N3Message
    {
        #region Constructors and Destructors

        public QuestFullUpdateMessage()
        {
            this.N3MessageType = N3MessageType.QuestFullUpdate;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public Quest[] Quests { get; set; }

        /// <summary>One byte after the whole list (OmniCell QuestFullUpdateMessage.AnnounceAsNew). It used to be read as a
        /// byte at the end of EACH quest (Quest.Unknown28): right for a one-quest message, and with two quests the second was
        /// read one byte late - QuestId garbage (e.g. 14336784:CCD8AE00) in all 10 two-quest messages of 328 recorded.</summary>
        [AoMember(1)]
        public byte AnnounceAsNew { get; set; }

        #endregion
    }
}