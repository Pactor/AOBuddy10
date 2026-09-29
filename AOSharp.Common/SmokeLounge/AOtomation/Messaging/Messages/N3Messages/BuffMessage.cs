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
    using System;
    // Layout and names ported from OmniCell's AOtomation.Messaging. AOSharp read the two ints as one
    // Identity "Buff" (Type = character instance, Instance = nano id); that view is kept as an alias.
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

        /// <summary>
        /// OmniCell: 0 = BuffStarted, 1 = BuffEnded (OmniCell BuffMessageHandler). 0 in every recorded copy - and on
        /// retail a BuffMessage is the buff ENDING whatever this says: of 426 in the bot's recordings, 345 arrive as the
        /// SetNanoDuration they follow runs out (+-2 s), 59 mid-way (Mesmeric Gaze on mobs, broken by a hit), 18 right
        /// after a refresh (2026-09-29). The SDK removes the buff on it (Client.OnBuffMessage), which is right.
        /// </summary>
        [AoMember(0)]
        public short Action { get; set; }

        /// <summary>
        /// OmniCell writes the character's instance here. On the wire it equals the message identity's
        /// instance in most copies; the rest carry 53019 (0xCF1B, the NanoProgram type).
        /// </summary>
        [AoMember(1)]
        public int Instance { get; set; }

        /// <summary>The nano program id.</summary>
        [AoMember(2)]
        public int NanoId { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is Action (0 started, 1 ended).")]
        public short Unknown1 { get => this.Action; set => this.Action = value; }

        /// <summary>Old Identity view: Type = <see cref="Instance"/>, Instance = <see cref="NanoId"/>.</summary>
        [Obsolete("Wire fields are Instance and NanoId; Buff.Instance is NanoId.")]
        public Identity Buff
        {
            get => new Identity((IdentityType)this.Instance, this.NanoId);
            set { this.Instance = (int)value.Type; this.NanoId = value.Instance; }
        }

        #endregion
    }
}
