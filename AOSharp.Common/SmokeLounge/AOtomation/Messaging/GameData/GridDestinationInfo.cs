// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Tower.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the GridDestinationInfo type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class GridDestinationInfo
    {
        #region AoMember Properties

        /// <summary>PlayfieldId and the destination Identity (OmniCell GridDestination.PlayfieldId + Identity).</summary>
        [AoMember(0)]
        public DestinationInfo DestinationInfo { get; set; }

        /// <summary>The label shown for the destination (OmniCell GridDestination.Name).</summary>
        [AoMember(1, SerializeSize = ArraySizeType.Int16)]
        public string Name { get; set; }

        /// <summary>The area's level (OmniCell GridDestination.AreaLevel).</summary>
        [AoMember(2)]
        public int AreaLevel { get; set; }

        /// <summary>The area's type, a Roman-numeral type (OmniCell GridDestination.AreaType).</summary>
        [AoMember(3)]
        public int AreaType { get; set; }

        #endregion

        #region Old AOSharp names (aliases, not on the wire)

        [System.Obsolete("Wire field is Name.")]
        public string Location { get => this.Name; set => this.Name = value; }

        [System.Obsolete("Wire field is AreaLevel.")]
        public int Unknown1 { get => this.AreaLevel; set => this.AreaLevel = value; }

        [System.Obsolete("Wire field is AreaType.")]
        public int Unknown2 { get => this.AreaType; set => this.AreaType = value; }

        #endregion
    }
}