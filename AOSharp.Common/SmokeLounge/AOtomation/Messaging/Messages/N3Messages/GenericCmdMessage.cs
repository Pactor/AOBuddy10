// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GenericCmdMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the GenericCmdMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;
    using static SmokeLounge.AOtomation.Messaging.Messages.N3Messages.PlayfieldAnarchyFMessage;

    // Names ported from OmniCell's AOtomation.Messaging. OmniCell models Source + Target as one
    // Identity[] whose count Action decides; this SDK keeps Source (present for Repair, UseItemOnItem,
    // UseItemOnCharacter) and Target, which read the same bytes. Old names kept as [Obsolete] aliases.
    [AoContract((int)N3MessageType.GenericCmd)]
    public class GenericCmdMessage : N3Message
    {
        #region Constructors and Destructors

        public GenericCmdMessage()
        {
            this.N3MessageType = N3MessageType.GenericCmd;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// The Verification_e the client keeps on every command (n3Command_t::GetVerification).
        /// </summary>
        /// <remarks>
        /// On the server's echo of a command this is its verdict: 1 = accepted, 2 = refused
        /// (MISSION-MODE-PLAN.md; recordings: 1,010 of 1,988 echoes were refusals).
        /// </remarks>
        [AoMember(0)]
        public int Verification { get; set; }

        /// <summary>
        /// A serial number, so the client can tell its own commands apart.
        /// </summary>
        [AoMember(1)]
        public int Serial { get; set; }

        [AoFlags("action")]
        [AoMember(2)]
        public GenericCmdAction Action { get; set; }

        /// <summary>
        /// An int32 on the wire that the client keeps as a boolean. Its meaning is not established.
        /// </summary>
        [AoMember(3)]
        public int Flag { get; set; }

        [AoMember(4)]
        public Identity User { get; set; }

        /// <summary>
        /// The first of the two targets, present only for Repair, UseItemOnItem and UseItemOnCharacter.
        /// </summary>
        [AoUsesFlags("action", typeof(Identity), FlagsCriteria.EqualsToAny, new[] {
            (int)GenericCmdAction.Repair,
            (int)GenericCmdAction.UseItemOnItem,
            (int)GenericCmdAction.UseItemOnCharacter
        })]
        [AoMember(5)]
        public Identity? Source { get; set; }

        /// <summary>
        /// What the action is being done to.
        /// </summary>
        [AoMember(6)]
        public Identity Target { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is Verification (echo: 1 accepted, 2 refused).")]
        public int Temp1 { get => this.Verification; set => this.Verification = value; }

        [Obsolete("Wire field is Serial.")]
        public int Count { get => this.Serial; set => this.Serial = value; }

        [Obsolete("Wire field is Flag.")]
        public int Temp4 { get => this.Flag; set => this.Flag = value; }

        #endregion
    }
}
