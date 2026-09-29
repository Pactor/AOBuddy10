#region License

// Copyright (c) 2005-2014, CellAO Team
// 
// 
// All rights reserved.
// 
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

#endregion

namespace SmokeLounge.AOtomation.Messaging.Messages.N3Messages
{
    // Ported 2026-09-29 from OmniCell's SmokeLounge.AOtomation.Messaging (Messages\N3Messages\PlaySoundMessage.cs).
    // Changes for this library only: AOSharp's Identity/Vector3/Quaternion types; ArraySizeType.Int32 where OmniCell
    // says Int32Terminated - this library has no Int32Terminated, and its string reader trims the counted NUL, so the
    // read is byte for byte the same; only a re-serialized string would come out one byte short of the client's
    // writer.
    // AOBuddy census 2026-09-29: 11 copies in the bot's mission recordings and 14 in the retail dumps, every one read
    // to the last byte by this class.

    using AOSharp.Common.GameData;
    #region Usings ...

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Serialization;
    using SmokeLounge.AOtomation.Messaging.Serialization.MappingAttributes;

    #endregion

    /// <summary>
    /// A sound for the client to play, named by resource.
    /// </summary>
    /// <remarks>
    /// PlaySoundIIR_c has vtable 0x1015E49C, reader 0x1003A368, writer
    /// 0x1003A2F7 and dispatcher 0x1003A262. Two fields: a counted string and
    /// an Identity.
    ///
    /// The count includes the terminator, and the writer is what proves it. At
    /// 0x1003A2FE it takes the string's length, adds one, refuses the whole
    /// message unless that minus one is at most 0x3E7, writes the incremented
    /// count as an int32 and then writes that many bytes out of the string's
    /// own storage - so the NUL travels and is counted. The reader agrees: it
    /// refuses a count above 1000 or at or below zero, allocates one byte more
    /// than the count, and NUL-terminates after what it read, which is a
    /// second terminator and is why the one on the wire does no harm.
    ///
    /// This was modelled as a plain int32 count, one byte short, and every one
    /// of the nine copies in the 2026-09-11 capture failed the round trip on
    /// it.
    /// </remarks>
    [AoContract((int)N3MessageType.PlaySound)]
    public class PlaySoundMessage : N3Message
    {
        public PlaySoundMessage()
        {
            this.N3MessageType = N3MessageType.PlaySound;
        }

        /// <summary>
        /// The sound resource's name, at most 999 characters.
        /// </summary>
        [AoMember(0, SerializeSize = ArraySizeType.Int32)]
        public string SoundResource { get; set; }

        /// <summary>
        /// What the sound is coming from.
        /// </summary>
        [AoMember(1)]
        public Identity Source { get; set; }
    }
}
