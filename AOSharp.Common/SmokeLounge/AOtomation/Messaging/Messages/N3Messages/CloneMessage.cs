// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CloneMessage.cs" company="OmniCell">
//   Copyright © 2026 OmniCell contributors.
//   Added to SmokeLounge.AOtomation.Messaging, which is distributed under the
//   Do What The Fuck You Want To Public License, Version 2, as published by
//   Sam Hocevar. See http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the CloneMessage type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\CloneMessage.cs).
    // Changes for this library only: the cloth record is ClothData (the client's ClothData_t), a copy of OmniCell's
    // Texture with its OverlayId/AlphaMode pair, because this library's Texture has only the first three fields.

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    /// <summary>
    /// Makes the character this message is addressed to look like someone else.
    /// </summary>
    /// <remarks>
    /// The name is the whole of it. The client resolves the message's identity
    /// to a SimpleChar_t and, at Gamecode.dll 0x10058800, writes the four values
    /// below straight into that character's stats:
    ///
    ///     HeadMesh -> stat 64,  headmesh
    ///     Race     -> stat 89,  race
    ///     Breed    -> stat 4,   breed
    ///     Sex      -> stat 59,  sex
    ///
    /// then hands the cloth list to the character's appearance and sets name
    /// slot 3 from the string. It reads stat 360, monsterscale, of its own
    /// accord to size the result, so scale is not carried here.
    ///
    /// There is no C# for this message before now and no captured copy of it,
    /// so the layout comes from the client alone - but from both halves of it,
    /// the reader at 0x10072E3B and the writer at 0x10072DDC, which agree field
    /// for field. The test that goes with it pins the layout rather than
    /// checking it against retail, because there is no retail copy to check.
    /// </remarks>
    [AoContract((int)N3MessageType.Clone)]
    public class CloneMessage : N3Message
    {
        #region Constructors and Destructors

        public CloneMessage()
        {
            this.N3MessageType = N3MessageType.Clone;
        }

        #endregion

        #region AoMember Properties

        /// <summary>
        /// What the character is wearing, as the same ClothData entries
        /// SimpleCharFullUpdate carries in its Textures list.
        /// </summary>
        [AoMember(0, SerializeSize = ArraySizeType.X3F1)]
        public ClothData[] Clothes { get; set; }

        /// <summary>
        /// Stat 64, headmesh.
        /// </summary>
        [AoMember(1)]
        public int HeadMesh { get; set; }

        /// <summary>
        /// Stat 89, race.
        /// </summary>
        [AoMember(2)]
        public byte Race { get; set; }

        /// <summary>
        /// Stat 4, breed.
        /// </summary>
        [AoMember(3)]
        public byte Breed { get; set; }

        /// <summary>
        /// Stat 59, sex.
        /// </summary>
        [AoMember(4)]
        public byte Sex { get; set; }

        /// <summary>
        /// The name to show, as a bare NUL-terminated string with no length.
        /// </summary>
        /// <remarks>
        /// BinaryStream::operator&gt;&gt;(char*) at BinaryStream.dll 0x100013A7
        /// reads one byte at a time until it reads a zero, and the matching
        /// operator&lt;&lt;(const char*) writes the characters and then a zero.
        /// No count precedes it. The client reads it into a 256 byte buffer.
        /// </remarks>
        [AoMember(5, SerializeSize = ArraySizeType.NullTerminated)]
        public string Name { get; set; }

        #endregion
    }
}
