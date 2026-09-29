// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AcgItem.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the AcgItem type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (GameData\AcgItem.cs); no changes.

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// An item, as GameData writes one: two templates and a level.
    /// </summary>
    /// <remarks>
    /// GameData::ACGItem_t, and the whole of it. Its stream operators are
    /// exported - the reader at GameData.dll 0x1000E9D7, the writer at
    /// 0x1000E9A7 - and messages reach them through imports rather than reading
    /// the fields themselves, which is why the same four int32s turn up in
    /// InventoryEntry, InventorySlot and ItemReplaced.
    ///
    /// The type holds three values: ACGItem_t(unsigned, unsigned, int),
    /// SetTemplates(unsigned, unsigned), SetLevel(int), GetTemplate(int),
    /// GetLevel(). The fourth int32 on the wire is not one of them.
    /// </remarks>
    public class AcgItem
    {
        #region AoMember Properties

        /// <summary>
        /// The item's first template.
        /// </summary>
        [AoMember(0)]
        public int LowId { get; set; }

        /// <summary>
        /// The item's second template.
        /// </summary>
        /// <remarks>
        /// The reader substitutes the first when this arrives zero, at
        /// 0x1000EA18.
        /// </remarks>
        [AoMember(1)]
        public int HighId { get; set; }

        /// <summary>
        /// The item's level, masked to 0x1FF on the way in.
        /// </summary>
        /// <remarks>
        /// ACGItem_t calls it the level, and its reader ands anything above 511
        /// down at 0x1000EA11.
        /// </remarks>
        [AoMember(2)]
        public int Quality { get; set; }

        /// <summary>
        /// A fourth number that is always zero.
        /// </summary>
        /// <remarks>
        /// The writer at 0x1000E9C3 pushes a literal zero here and the reader at
        /// 0x1000EA01 reads it into a local nothing looks at, so it is reserved
        /// rather than unknown.
        /// </remarks>
        [AoMember(3)]
        public int Unused { get; set; }

        #endregion
    }
}
