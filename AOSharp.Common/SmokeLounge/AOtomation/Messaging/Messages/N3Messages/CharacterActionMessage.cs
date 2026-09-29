// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterActionMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CharacterActionMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.CharacterAction)]
    public class CharacterActionMessage : N3Message
    {
        #region Constructors and Destructors

        public CharacterActionMessage()
        {
            this.N3MessageType = N3MessageType.CharacterAction;
        }

        #endregion

        #region AoMember Properties

        [AoMember(0)]
        public CharacterActionType Action { get; set; }

        /// <summary>
        /// Not identified. OmniCell's server always fills 0; on the wire it is 0 except 0x42000018 on a
        /// few action-102 messages.
        /// </summary>
        [AoMember(1)]
        public int Unknown1 { get; set; }

        [AoMember(2)]
        public Identity Target { get; set; }

        /// <summary>
        /// First action parameter; its meaning depends on <see cref="Action"/>. Wire and OmniCell agree on:
        /// SetNanoDuration (98): the CASTER's instance (OmniCell ConstructSetNanoDuration sets
        /// Parameter1 = caster.Identity.Instance; see <see cref="SetNanoDurationCasterInstance"/>),
        /// Target = NanoProgram:nano id. FinishNanoCasting (107): 1. SpecialUsed: the special's skill
        /// stat. UploadNano (0xCC): 53019 (NanoProgram). MissionChanged (0x3B): 56003.
        /// 0x84: the locked skill (123 FirstAid).
        /// </summary>
        [AoMember(3)]
        public int Parameter1 { get; set; }

        /// <summary>
        /// Second action parameter; its meaning depends on <see cref="Action"/>: SetNanoDuration the
        /// duration (1/100 s), FinishNanoCasting the nano id, UploadNano the nano id, MissionChanged the
        /// quest id, 0x84 the lock seconds remaining (counting down).
        /// </summary>
        [AoMember(4)]
        public int Parameter2 { get; set; }

        /// <summary>
        /// Not identified. OmniCell's server always fills 0; 0 in every recorded copy.
        /// </summary>
        [AoMember(5)]
        public short Unknown2 { get; set; }

        #endregion

        #region Convenience (not on the wire)

        /// <summary>
        /// For SetNanoDuration: the instance of the character that cast the nano (Parameter1); 0 otherwise.
        /// The wire carries only the instance; the caster is a character or a pet (SimpleChar).
        /// </summary>
        public int SetNanoDurationCasterInstance => this.Action == CharacterActionType.SetNanoDuration ? this.Parameter1 : 0;

        #endregion
    }
}