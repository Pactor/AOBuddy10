// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpellListMessage.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the SpellListMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using AOSharp.Common.GameData;

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    [AoContract((int)N3MessageType.SpellList)]
    public class SpellListMessage : N3Message
    {
        #region Constructors and Destructors

        public SpellListMessage()
        {
            this.N3MessageType = N3MessageType.SpellList;
        }

        #endregion

        #region AoMember Properties

        //This really isn't used by us and it's definition is incorrect so until
        //someone (Mali) properly defines the struct this message will just be empty.

        //[AoMember(0, SerializeSize = ArraySizeType.Int32)]
        //public NanoEffect[] NanoEffects { get; set; }

        //[AoMember(1)]
        //public Identity Character { get; set; }

        #endregion
    }
}