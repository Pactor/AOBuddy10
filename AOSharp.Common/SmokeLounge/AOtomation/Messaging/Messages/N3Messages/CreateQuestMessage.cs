// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateQuestMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CreateQuestMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Names ported from OmniCell's AOtomation.Messaging. Old AOSharp names kept as [Obsolete] aliases.
    [AoContract((int)N3MessageType.CreateQuest)]
    public class CreateQuestMessage : N3Message
    {
        #region Constructors and Destructors

        public CreateQuestMessage()
        {
            this.N3MessageType = N3MessageType.CreateQuest;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The offered quest to accept (OmniCell's CreateQuestMessageHandler looks the offer up by its Instance).
        /// </summary>
        [AoMember(0)]
        public Identity QuestIdentity { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is QuestIdentity.")]
        public Identity MissionId { get => this.QuestIdentity; set => this.QuestIdentity = value; }

        #endregion
    }
}
