// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TowerInfo.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TowerInfo type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class TowerInfo
    {
        #region AoMember Properties

        [AoMember(0)]
        public Identity PlaceholderId { get; set; }

        [AoMember(1)]
        public Identity TowerCharId { get; set; }

        [AoMember(2)]
        public Vector3 Position { get; set; }

        [AoMember(3)]
        public int MeshId { get; set; }

        [AoMember(4)]
        public Side Side { get; set; }

        [AoMember(5)]
        public int DestroyedMeshId { get; set; }

        [AoMember(6)]
        public float Scale { get; set; }

        [AoMember(7)]
        public TowerClass Class { get; set; }
        #endregion
    }
}