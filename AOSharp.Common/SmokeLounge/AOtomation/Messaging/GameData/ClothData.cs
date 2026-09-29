// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Texture.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the Texture type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.GameData
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (GameData\Texture.cs). Changes for this
    // library only: the cloth record is ClothData (the client's ClothData_t), a copy of OmniCell's Texture with its
    // OverlayId/AlphaMode pair, because this library's Texture has only the first three fields.

    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    public class ClothData
    {
        #region AoMember Properties

        /// <summary>
        /// Which part of the body, in the low half, and whether the two
        /// trailing int32s are present, in the high half.
        /// </summary>
        /// <remarks>
        /// The client calls the record ClothData_t and the low half a
        /// ClothPart_e, and it carries the names: ClothData_t::GetName at
        /// GameData.dll 0x1000A5F0 is one instruction, an index into the table
        /// at 0x10032364, and the table is hands, body, feet, arms, legs, head
        /// - 0 to 5.
        ///
        /// The high half is not a part. The writer at 0x1000A5FF ORs 0x10000
        /// into it when either trailing int32 is non-zero and clears it
        /// otherwise, which is what HasExtra below tests.
        /// </remarks>
        [AoMember(0)]
        public int Place { get; set; }

        [AoMember(1)]
        public int Id { get; set; }

        /// <summary>
        /// Which bank of five places this entry belongs to.
        /// </summary>
        /// <remarks>
        /// Gamecode.dll 0x100483E5 walks these entries into the character's
        /// appearance table and indexes it with Group * 5 + Place, so a group
        /// is five places wide and this picks the group.
        ///
        /// The captures bear that out exactly. Every SimpleCharFullUpdate that
        /// carries clothes carries five of these, with Place running 0 to 4 and
        /// this field zero throughout - one full group, the first one.
        /// </remarks>
        [AoMember(2)]
        public int Group { get; set; }

        /// <summary>
        /// A second texture, drawn over the first.
        /// </summary>
        /// <remarks>
        /// GameData.dll 0xA661 reads Place, Id and Group, and then reads two
        /// more int32s if and only if Place is positive and the signed short in
        /// its high half is positive as well. So Place is two packed shorts, the
        /// low one an actual place and the high one deciding the length of the
        /// rest of the entry.
        ///
        /// Both are null together or set together. The pair was missing until
        /// the reader was taken out of the client, which is why entries with the
        /// high half set used to derail everything that followed them.
        ///
        /// What they are for is settled now, and it takes three functions to
        /// get there. The redraw at Gamecode.dll 0x1004BBAE walks the entries
        /// and copies Id, this and <see cref="AlphaMode"/> into the three
        /// trailing int32s of a GameData TextureData_t, in that order, before
        /// handing the lot to VisualCATMesh_t::SetCATTextures. That function is
        /// in DisplaySystem.dll, and its loop at 0x10070247 is short enough to
        /// read whole: it draws the first of the three at the layer its caller
        /// named - 2, for clothes - with the third as the alpha mode, and then
        /// draws the second at layer 3 with no alpha at all.
        ///
        /// So this is a second texture on top of the first, and the clear path
        /// beside it clears layers 2 and 3 together, which is the same two.
        /// </remarks>
        [AoMember(3)]
        public int? OverlayId { get; set; }

        /// <summary>
        /// How the texture in <see cref="Id"/> is blended. 0 or 5.
        /// </summary>
        /// <remarks>
        /// TextureData_t::AlphaMode, the fourth argument of
        /// VisualCATMesh_t::SetCATTexture. The layer 3 overlay beside it is
        /// always drawn with 0; only this one carries a mode. TextureData_t's
        /// own stream format narrows it further: its reader at GameData.dll
        /// 0x1000C0B4 turns whatever it finds into 5 or 0 and its writer at
        /// 0x1000C074 writes back 1 or 0, so 5 is the only non-zero value the
        /// client ever holds.
        /// </remarks>
        [AoMember(4)]
        public int? AlphaMode { get; set; }

        /// <summary>
        /// Whether the two trailing int32s are on the wire for this entry.
        /// </summary>
        public bool HasExtra
        {
            get
            {
                return this.Place > 0 && (short)(this.Place >> 16) > 0;
            }
        }

        #endregion
    }
}