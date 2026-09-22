using System;
using System.Collections.Generic;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using StreamReader = SmokeLounge.AOtomation.Messaging.Serialization.StreamReader;

namespace AOSharp.Clientless.Net
{
    /// <summary>
    /// Corrected FullCharacter reader — used ONLY when the stock generated serializer throws (see
    /// NetworkSession.TryDeserializeFullCharacter), so the working common path is never altered.
    ///
    /// WHY: the bundled FullCharacterMessage models the trailing section as
    /// [TeamMembers(struct)][Unknown12][Perks], but on this client a character WITH A PET UP carries the
    /// pet's 8-byte identity in the first trailing X3F1 array — exactly where the stock reader expects a full
    /// TeamMember struct. It reads the pet's 16 trailing bytes as a TeamMember (identity + Int16 name length
    /// + int + byte + two shorts) and runs one Int16 past the end (EndOfStreamException), so the ENTIRE
    /// FullCharacter is dropped whenever a pet is out. At login (no pet) that array is empty and the stock
    /// reader is fine — which is why the crash only appears in combat with a pet.
    ///
    /// This reader parses the well-understood leading fields (all BEFORE the trailing section, verified
    /// against a real capture) so the bot keeps its own stats + spell list on zone, then reads the trailing
    /// pet-identity array defensively (bounded, never over-reads) so we learn the character's pets
    /// AUTHORITATIVELY — the most reliable ownership source there is. Perks are left null here (the login
    /// FullCharacter carries them intact; ApplyFullCharUpdate must not overwrite perks with null).
    /// </summary>
    internal static class FullCharacterReader
    {
        // X3F1 array size: the wire holds Int32 == (count + 1) * 0x3F1.
        private static int ReadX3F1Count(StreamReader r) => r.ReadInt32() / 0x03F1 - 1;

        private static Identity ReadIdentity(StreamReader r)
        {
            int type = r.ReadInt32();
            int instance = r.ReadInt32();
            return new Identity((IdentityType)type, instance);
        }

        private static void SkipX3F1(StreamReader r, int elemSize)
        {
            int c = ReadX3F1Count(r);
            if (c > 0) r.ReadBytes(c * elemSize);
        }

        private static void SkipInt32Array(StreamReader r, int elemSize)
        {
            int c = r.ReadInt32();
            if (c > 0) r.ReadBytes(c * elemSize);
        }

        private static int[] ReadX3F1Int32s(StreamReader r)
        {
            int c = ReadX3F1Count(r);
            var a = new int[c > 0 ? c : 0];
            for (int i = 0; i < c; i++) a[i] = r.ReadInt32();
            return a;
        }

        private static GameTuple<int, int>[] ReadX3F1IntInt(StreamReader r)
        {
            int c = ReadX3F1Count(r);
            var a = new GameTuple<int, int>[c > 0 ? c : 0];
            for (int i = 0; i < c; i++)
                a[i] = new GameTuple<int, int> { Value1 = r.ReadInt32(), Value2 = r.ReadInt32() };
            return a;
        }

        private static GameTuple<byte, byte>[] ReadX3F1ByteByte(StreamReader r)
        {
            int c = ReadX3F1Count(r);
            var a = new GameTuple<byte, byte>[c > 0 ? c : 0];
            for (int i = 0; i < c; i++)
                a[i] = new GameTuple<byte, byte> { Value1 = r.ReadByte(), Value2 = r.ReadByte() };
            return a;
        }

        private static GameTuple<byte, short>[] ReadX3F1ByteShort(StreamReader r)
        {
            int c = ReadX3F1Count(r);
            var a = new GameTuple<byte, short>[c > 0 ? c : 0];
            for (int i = 0; i < c; i++)
                a[i] = new GameTuple<byte, short> { Value1 = r.ReadByte(), Value2 = r.ReadInt16() };
            return a;
        }

        /// <summary>
        /// Reads a FullCharacter body from a stream positioned at the start of the body (offset 16, right
        /// after the 16-byte packet header). <paramref name="packetLength"/> bounds the defensive tail read.
        /// </summary>
        public static FullCharacterMessage Read(StreamReader r, int packetLength)
        {
            var fc = new FullCharacterMessage();

            // N3Message preamble.
            fc.N3MessageType = (N3MessageType)r.ReadInt32();
            fc.Identity = ReadIdentity(r);
            fc.Unknown = r.ReadByte();

            fc.Version = r.ReadInt32();

            SkipX3F1(r, 32);                     // InventorySlots  (int + short + short + Identity + 4*int)
            fc.UploadedNanoIds = ReadX3F1Int32s(r);
            SkipX3F1(r, 3);                      // Unknown2 (UnknownDataType1: 3 bytes)
            r.ReadInt32();                       // Unknown3
            SkipInt32Array(r, 20);               // Unknown4 (UnknownDataType2: int + Identity + int + int)
            r.ReadInt32();                       // Unknown5
            SkipInt32Array(r, 20);               // Unknown6
            r.ReadInt32();                       // Unknown7
            SkipInt32Array(r, 20);               // Unknown8
            fc.Stats1 = ReadX3F1IntInt(r);
            fc.Stats2 = ReadX3F1IntInt(r);
            fc.Stats3 = ReadX3F1ByteByte(r);
            fc.Stats4 = ReadX3F1ByteShort(r);
            SkipInt32Array(r, 8);                // AbsorbStats (int + int)
            SkipInt32Array(r, 8);                // UnknownIdentities (Identity)

            // Trailing section. The first X3F1 array here is the character's PET identities (this is the very
            // array the stock reader mis-parses as a TeamMember struct). Read it defensively so we never run
            // off the end even if a future variant differs.
            var pets = new List<Identity>();
            if (packetLength - (int)r.Position >= 4)
            {
                int count = ReadX3F1Count(r);
                if (count > 0 && count <= 64 && packetLength - (int)r.Position >= count * 8)
                    for (int i = 0; i < count; i++) pets.Add(ReadIdentity(r));
            }
            fc.Pets = pets.ToArray();

            // Do NOT wipe perks — they arrive on the login FullCharacter, which the stock reader parses fine.
            fc.Perks = null;
            return fc;
        }
    }
}
