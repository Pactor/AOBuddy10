// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SendScoreMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SendScoreMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// The battlestation scoreboard.
    /// </summary>
    /// <remarks>
    /// Layout ported from OmniCell's AOtomation.Messaging (from the client reader at Gamecode
    /// 0x10132050): the body is two X3F1 int32 arrays. OmniCell says AOSharp's flat model "is wrong.
    /// Its field names were attached to the wrong bytes", so the old names are kept only as
    /// [Obsolete] properties that are no longer on the wire. No recording contains one.
    /// </remarks>
    [AoContract((int)N3MessageType.SendScore)]
    public class SendScoreMessage : N3Message
    {
        #region Constructors and Destructors

        public SendScoreMessage()
        {
            this.N3MessageType = N3MessageType.SendScore;
        }

        #endregion

        #region AoMember Properties

        /// <summary>The scores: element 0 = Clan victory points, 1 = Omni victory points.</summary>
        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public int[] Scores { get; set; }

        /// <summary>One three-state value per battlestation site.</summary>
        [AoMember(1, SerializeSize = ArraySizeType.X3F1)]
        public int[] SiteStates { get; set; }

        #endregion

        #region Old AOSharp names (not on the wire)

        [Obsolete("Not on the wire; the body is Scores and SiteStates.")]
        public int Unknown1 { get; set; }

        [Obsolete("Not on the wire; the body is Scores and SiteStates.")]
        public BattlestationSide A { get; set; }

        [Obsolete("Not on the wire; the body is Scores and SiteStates.")]
        public BattlestationSide B { get; set; }

        [Obsolete("Not on the wire; the body is Scores and SiteStates.")]
        public BattlestationSide C { get; set; }

        [Obsolete("Not on the wire; the body is Scores and SiteStates.")]
        public BattlestationSide Core { get; set; }

        [Obsolete("Not on the wire; the body is Scores and SiteStates.")]
        public int Unknown2 { get; set; }

        [Obsolete("Not on the wire; the body is Scores and SiteStates.")]
        public int RedScore { get; set; }

        [Obsolete("Not on the wire; the body is Scores and SiteStates.")]
        public int BlueScore { get; set; }

        #endregion
    }
}
