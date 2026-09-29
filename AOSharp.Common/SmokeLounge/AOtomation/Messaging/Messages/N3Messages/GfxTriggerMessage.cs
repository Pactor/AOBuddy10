// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GfxTriggerMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the GfxTriggerMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\GfxTriggerMessage.cs).
    // Changes for this library only: AOSharp's Identity/Vector3/Quaternion types.

    using AOSharp.Common.GameData;
    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Play a graphical effect somewhere.
    /// </summary>
    /// <remarks>
    /// Reader 0x100398D3, writer 0x10039991, dispatcher 0x10039A44, vtable
    /// 0x1015E434. Two int32s and then one of six shapes, chosen by the first;
    /// the reader and the writer branch the same way on the same value, so the
    /// layout has two sources.
    ///
    /// The dispatcher hands each shape to a different overload of
    /// _EffectHandler_t::CreateEffect2, and Gamecode exports all six of them
    /// with their signatures, which is what names every field here:
    ///
    ///   1  CreateEffect2(int, Vector3 const&amp;)                              0x100D1F6D
    ///   2  CreateEffect2(int, n3Dynel_t const&amp;, int)                       0x100D2070
    ///   3  CreateEffect2(int, Vector3 const&amp;, Vector3 const&amp;)         0x100D20C8
    ///   4  CreateEffect2(int, Vector3 const&amp;, n3Dynel_t const&amp;)       0x100D21D0
    ///   5  CreateEffect2(int, n3Dynel_t const&amp;, Vector3 const&amp;, int)  0x100D2228
    ///   6  CreateEffect2(int, n3Dynel_t const&amp;, n3Dynel_t const&amp;, int) 0x100D2339
    ///
    /// The first int32 of every one of those is the effect, and the trailing
    /// int is only there when there is a dynel to hang the effect off.
    ///
    /// See GfxTriggerSerializer - the attribute serializer cannot express a
    /// shape chosen by equality with a value.
    /// </remarks>
    [AoContract((int)N3MessageType.GfxTrigger)]
    public class GfxTriggerMessage : N3Message
    {
        #region Constructors and Destructors

        public GfxTriggerMessage()
        {
            this.N3MessageType = N3MessageType.GfxTrigger;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// Which of the six shapes follows, 1 to 6.
        /// </summary>
        /// <remarks>
        /// Read first at 0x100398EA and used by the reader, the writer and the
        /// dispatcher alike. Anything outside 1 to 6 leaves the body empty -
        /// the reader's chain at 0x100398F4 falls through to the end and reads
        /// nothing more.
        /// </remarks>
        [AoMember(0)]
        public int Selector { get; set; }

        /// <summary>
        /// Which effect to play.
        /// </summary>
        /// <remarks>
        /// The first argument of every CreateEffect2 overload.
        /// CreateGfxControl at 0x100D0656 looks it up in the effect handler's
        /// template storage and switches on the template's kind to decide which
        /// _GfxControl_t to build, so it is a gfx template id. 0xC34F is
        /// refused outright at 0x100D207A.
        /// </remarks>
        [AoMember(1)]
        public int EffectId { get; set; }

        /// <summary>
        /// What the effect hangs off. Shapes 2, 4, 5 and 6.
        /// </summary>
        [AoMember(2)]
        public Identity Target { get; set; }

        /// <summary>
        /// What it reaches to. Shape 6 only.
        /// </summary>
        [AoMember(3)]
        public Identity SecondTarget { get; set; }

        /// <summary>
        /// Where it is, or where it starts. Shapes 1, 3, 4 and 5.
        /// </summary>
        [AoMember(4)]
        public Vector3 Position { get; set; }

        /// <summary>
        /// Where it reaches to. Shape 3 only.
        /// </summary>
        [AoMember(5)]
        public Vector3 SecondPosition { get; set; }

        /// <summary>
        /// Where on the target it hangs. Shapes 2, 5 and 6 - the ones with a
        /// dynel in them.
        /// </summary>
        /// <remarks>
        /// See <see cref="GameData.GfxAttachment"/>, whose values are the
        /// client's own table of bone names.
        /// </remarks>
        [AoMember(6)]
        public GfxAttachment Attachment { get; set; }

        #endregion
    }
}
