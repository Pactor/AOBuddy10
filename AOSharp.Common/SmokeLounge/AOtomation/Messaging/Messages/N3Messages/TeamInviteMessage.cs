// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TeamInviteMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TeamInviteMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    // Layout, names and doc comments ported from OmniCell's AOtomation.Messaging (verified against
    // every recorded packet). Old AOSharp names are kept as [Obsolete] aliases where they differed.
    /// <summary>
    /// Somebody has asked this character to join their team.
    /// </summary>
    /// <remarks>
    /// Ported from AOSharp, which models it as an Identity, a byte and a
    /// counted string, and which has been driving a clientless client against
    /// the live servers - so the layout is what a running program accepts
    /// rather than what a capture happens to show. No copy of this message
    /// appears in any capture here, so nothing local contradicts or confirms
    /// it.
    ///
    /// OmniCell had a documentation page for this message and no class at all,
    /// which meant the body was never taken off the wire.
    /// </remarks>
    [AoContract((int)N3MessageType.TeamInvite)]
    public class TeamInviteMessage : N3Message
    {
        #region Constructors and Destructors

        public TeamInviteMessage()
        {
            this.N3MessageType = N3MessageType.TeamInvite;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Who is doing the asking.
        /// </summary>
        [AoMember(0)]
        public Identity Requestor { get; set; }

        /// <summary>
        /// Not identified. AOSharp reads it and does not use it.
        /// </summary>
        [AoMember(1)]
        public byte Unknown1 { get; set; }

        /// <summary>
        /// The requestor's name, so the invitation can be worded without a
        /// second lookup.
        /// </summary>
        [AoMember(2, SerializeSize = ArraySizeType.Int16)]
        public string Name { get; set; }

        #endregion
    }
}
