// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FightModeUpdate.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the FightModeUpdate type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using System;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Names and doc comments ported from OmniCell's AOtomation.Messaging (FightModeUpdateMessage). AOSharp
    // declared both members as AoMember(0); they are now 0 and 1 in wire order. Old names kept as aliases.
    [AoContract((int)N3MessageType.FightModeUpdate)]
    public class FightModeUpdate : N3Message
    {
        #region Constructors and Destructors

        public FightModeUpdate()
        {
            this.N3MessageType = N3MessageType.FightModeUpdate;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Which playfield the districts below are in.
        /// </summary>
        /// <remarks>
        /// The dispatcher at Gamecode.dll 0x101250D5 hands the instance half
        /// straight to the playfield lookup and then resolves every district
        /// name against that playfield's district table.
        /// </remarks>
        [AoMember(0)]
        public Identity Playfield { get; set; }

        /// <summary>
        /// The suppression-field changes, one per district (the client's FightModeChange_t).
        /// </summary>
        [AoMember(1, SerializeSize = ArraySizeType.X3F1)]
        public FightModeName[] Entries { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [Obsolete("Wire field is Playfield.")]
        public Identity ResourceIdentity { get => this.Playfield; set => this.Playfield = value; }

        [Obsolete("Wire field is Entries.")]
        public FightModeName[] Name { get => this.Entries; set => this.Entries = value; }

        #endregion
    }
}
