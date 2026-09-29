// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KnuBotRejectedItem.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the KnuBotRejectedItem type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class KnuBotRejectedItem
    {
        #region AoMember Properties

        /// <summary>
        /// Matched against the item's acgitemtemplateid, stat 702.
        /// </summary>
        [AoMember(0)]
        public int ItemTemplateId { get; set; }

        /// <summary>
        /// Matched against acgitemtemplateid2, stat 703.
        /// </summary>
        [AoMember(1)]
        public int ItemTemplateId2 { get; set; }

        /// <summary>
        /// Matched against the item's level, stat 54, which for an item is its
        /// quality. -1 matches any.
        /// </summary>
        [AoMember(2)]
        public int QualityLevel { get; set; }

        /// <summary>
        /// A fourth word the matcher never looks at (1234567890, the "not set" sentinel).
        /// </summary>
        [AoMember(3)]
        public int Unused { get; set; }

        [System.Obsolete("Wire field is ItemTemplateId (stat 702).")]
        public int LowId { get => this.ItemTemplateId; set => this.ItemTemplateId = value; }

        [System.Obsolete("Wire field is ItemTemplateId2 (stat 703).")]
        public int HighId { get => this.ItemTemplateId2; set => this.ItemTemplateId2 = value; }

        [System.Obsolete("Wire field is QualityLevel (-1 = any).")]
        public int Quality { get => this.QualityLevel; set => this.QualityLevel = value; }

        #endregion
    }
}