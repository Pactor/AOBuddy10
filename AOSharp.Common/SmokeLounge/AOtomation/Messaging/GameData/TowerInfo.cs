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
    using System;
    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// A tower in PlayfieldAllTowers / PlayfieldTowerUpdateClient (OmniCell: TowerProxyBase).
    /// Old AOSharp names kept as [Obsolete] aliases.
    /// </summary>
    public class TowerInfo
    {
        #region AoMember Properties

        /// <summary>The tower field this tower belongs to.</summary>
        [AoMember(0)]
        public Identity TowerFieldIdentity { get; set; }

        /// <summary>Whose tower it is.</summary>
        [AoMember(1)]
        public Identity OwnerIdentity { get; set; }

        [AoMember(2)]
        public Vector3 Coordinates { get; set; }

        /// <summary>The mesh to draw it with.</summary>
        [AoMember(3)]
        public int MeshId { get; set; }

        /// <summary>Whose side the tower is on.</summary>
        [AoMember(4)]
        public Side Side { get; set; }

        /// <summary>The animation to play on it (AOSharp: DestroyedMeshId; it is not a mesh).</summary>
        [AoMember(5)]
        public int AnimationId { get; set; }

        /// <summary>How big to draw it.</summary>
        [AoMember(6)]
        public float Scale { get; set; }

        /// <summary>Three bits the map marker is chosen by (1/2/4; AOSharp's TowerClass).</summary>
        [AoMember(7)]
        public int MarkerFlags { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [System.Obsolete("Wire field is TowerFieldIdentity.")]
        public Identity PlaceholderId { get => this.TowerFieldIdentity; set => this.TowerFieldIdentity = value; }

        [System.Obsolete("Wire field is OwnerIdentity.")]
        public Identity TowerCharId { get => this.OwnerIdentity; set => this.OwnerIdentity = value; }

        [System.Obsolete("Wire field is Coordinates.")]
        public Vector3 Position { get => this.Coordinates; set => this.Coordinates = value; }

        [System.Obsolete("Wire field is AnimationId.")]
        public int DestroyedMeshId { get => this.AnimationId; set => this.AnimationId = value; }

        [System.Obsolete("Wire field is MarkerFlags (map-marker bits).")]
        public TowerClass Class { get => (TowerClass)this.MarkerFlags; set => this.MarkerFlags = (int)value; }

        #endregion
    }
}
