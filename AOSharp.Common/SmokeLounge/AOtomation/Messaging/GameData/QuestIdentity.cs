// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QuestIdentity.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the QuestIdentity type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class QuestIdentity
    {
        #region AoMember Properties

        [AoMember(0)]
        public Identity Identity { get; set; }

        [AoMember(1)]
        public int Unknown1 { get; set; }

        #endregion
    }
}