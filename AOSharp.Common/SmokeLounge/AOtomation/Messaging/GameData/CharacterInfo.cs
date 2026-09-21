// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CharacterInfo.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CharacterInfo type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class CharacterInfo
    {
        #region AoMember Properties

        [AoMember(0)]
        public Identity MissionIdentity { get; set; }

        [AoMember(1, SerializeSize = ArraySizeType.NullTerminated)]
        public string Name { get; set; }

        #endregion
    }
}