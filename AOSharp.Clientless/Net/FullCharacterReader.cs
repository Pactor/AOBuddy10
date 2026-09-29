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
    /// THE FullCharacter reader: every FullCharacter goes through here (NetworkSession.TryDeserializeFullCharacter).
    ///
    /// The layout is OmniCell's FullCharacterMessage (E:\Funcom\OmniCell\...\Messages\N3Messages\FullCharacterMessage.cs),
    /// which decodes all 530 FullCharacters in the bot's mission recordings with nothing left over. The stock
    /// generated class is right up to the stats and wrong after them: it reads TeamFlags as a count, the Pets
    /// as TeamMembers, the Buffs as Unknown12 and the ResearchGoals as Perks. That only works solo with no pet:
    ///   * a pet up - the pet identities are read as TeamMember structs and it throws (14 of 530);
    ///   * teamed (TeamFlags 1) - it doesn't throw, it loads the empty BUFF list as Perks, and the trained perks
    ///     are wiped for the session (audit 2026-09-29, 1 of 530);
    ///   * a raid (TeamFlags 3) - both the stock class and the old fallback skipped 24 bytes where the wire has 12.
    ///
    /// The whole body is read and must end exactly at the packet's end; anything else throws, and the caller
    /// then does NOT apply what it got (a misread FullCharacter is worse than none: it wipes perks and pets).
    /// </summary>
    internal static class FullCharacterReader
    {
        // X3F1 array size: the wire holds Int32 == (count + 1) * 0x3F1.
        private static int ReadX3F1Count(StreamReader r)
        {
            int v = r.ReadInt32();
            if (v <= 0 || v % 0x3F1 != 0 || v / 0x3F1 - 1 > 30000)
                throw new InvalidOperationException($"{v} is not an X3F1 count");
            return v / 0x3F1 - 1;
        }

        private static int ReadInt32Count(StreamReader r)
        {
            int c = r.ReadInt32();
            if (c < 0 || c > 30000) throw new InvalidOperationException($"{c} is not a list count");
            return c;
        }

        private static Identity ReadIdentity(StreamReader r)
        {
            int type = r.ReadInt32();
            int instance = r.ReadInt32();
            return new Identity((IdentityType)type, instance);
        }

        private static T[] ReadArray<T>(int count, Func<T> read)
        {
            var a = new T[count];
            for (int i = 0; i < count; i++) a[i] = read();
            return a;
        }

        private static InventorySlot ReadInventorySlot(StreamReader r) => new InventorySlot
        {
            Placement = r.ReadInt32(), Flags = r.ReadInt16(), Count = r.ReadInt16(), Identity = ReadIdentity(r),
            ItemLowId = r.ReadInt32(), ItemHighId = r.ReadInt32(), Quality = r.ReadInt32(), Unused = r.ReadInt32(),
        };

        // SkillEntries / PerkEntries / NanoEntries: (Version, Identity, LockDuration, LockRemaining).
        private static FullCharacterMessage.UnknownDataType2 ReadLockEntry(StreamReader r) => new FullCharacterMessage.UnknownDataType2
        {
            MsgVersion = r.ReadInt32(), Unknown2 = ReadIdentity(r), Unknown3 = r.ReadInt32(), Unknown4 = r.ReadInt32(),
        };

        // TeamMemberEntry: Identity, Name (Int16 length), OrganizationId, Side (byte), Level (short), Profession (short).
        private static FullCharacterMessage.TeamMember ReadTeamMember(StreamReader r)
        {
            var m = new FullCharacterMessage.TeamMember { Identity = ReadIdentity(r) };
            short len = r.ReadInt16();
            if (len < 0) throw new InvalidOperationException($"team member name length {len}");
            m.Name = len > 0 ? System.Text.Encoding.ASCII.GetString(r.ReadBytes(len)).TrimEnd('\0') : "";
            m.Unknown1 = r.ReadInt32();   // OrganizationId
            m.Unknown2 = r.ReadByte();    // Side
            m.Level = r.ReadInt16();
            m.Profession = r.ReadInt16();
            return m;
        }

        // NanoEffect (OmniCell Serialization\Serializers\Custom\NanoEffects.cs): Effect identity, Version, criteria
        // (count + Stat/Value/Operator), Hits, Amount, Target, SpellList, then the function's arguments in the
        // client's own format (NanoEffectFormats): 4 bytes each, a string argument followed by its bytes.
        private static FullCharacterMessage.BuffEntry ReadBuff(StreamReader r)
        {
            var b = new FullCharacterMessage.BuffEntry { Effect = ReadIdentity(r) };
            r.ReadInt32();                                   // Version
            int criteria = r.ReadInt32();
            if (criteria < 0 || criteria > 1000) throw new InvalidOperationException($"{criteria} criteria");
            r.ReadBytes(criteria * 12);
            b.Hits = r.ReadInt32(); b.Amount = r.ReadInt32(); b.Target = r.ReadInt32(); b.SpellList = r.ReadInt32();
            int[] args = NanoEffectFormats.ArgumentsFor((int)b.Effect.Type)
                         ?? throw new InvalidOperationException($"no spell format for function {(int)b.Effect.Type}");
            foreach (int arg in args)
            {
                byte[] head = r.ReadBytes(4);
                if (NanoEffectFormats.KindOf(arg) != NanoEffectFormats.StringArgument) continue;
                int length = (head[0] << 24) | (head[1] << 16) | (head[2] << 8) | head[3];
                r.ReadBytes(length);
            }
            return b;
        }

        // ResearchGoals (FullCharacterEntry): Id, Marker; Marker with all of 0xFFFFFF00 -> IdRepeated, Value;
        // otherwise three plain ints. Kept in the SDK's Perk shape: SkillId = Id (PerkData maps it), Unknown1 = Marker.
        private static FullCharacterMessage.Perk ReadResearchGoal(StreamReader r)
        {
            var p = new FullCharacterMessage.Perk { SkillId = r.ReadInt32(), Unknown1 = r.ReadInt32() };
            if ((p.Unknown1 & unchecked((int)0xFFFFFF00)) == unchecked((int)0xFFFFFF00)) { p.Unknown2 = r.ReadInt32(); p.Unknown3 = r.ReadInt32(); }
            else { p.Unknown2 = r.ReadInt32(); p.Unknown3 = r.ReadInt32(); r.ReadInt32(); }
            return p;
        }

        /// <summary>
        /// Reads a FullCharacter body from a stream positioned at the start of the body (offset 16, right after
        /// the 16-byte packet header). Throws unless the body ends exactly at <paramref name="packetLength"/>.
        /// </summary>
        public static FullCharacterMessage Read(StreamReader r, int packetLength)
        {
            var fc = new FullCharacterMessage();

            // N3Message preamble.
            fc.N3MessageType = (N3MessageType)r.ReadInt32();
            fc.Identity = ReadIdentity(r);
            fc.Unknown = r.ReadByte();

            fc.Version = r.ReadInt32();
            fc.InventorySlots = ReadArray(ReadX3F1Count(r), () => ReadInventorySlot(r));
            fc.UploadedNanoIds = ReadArray(ReadX3F1Count(r), () => r.ReadInt32());
            fc.Unknown2 = ReadArray(ReadX3F1Count(r), () => new FullCharacterMessage.UnknownDataType1 { Unknown1 = r.ReadByte(), Unknown2 = r.ReadByte(), Unknown3 = r.ReadByte() });
            fc.SkillEntriesVersion = r.ReadInt32();
            fc.Unknown4 = ReadArray(ReadInt32Count(r), () => ReadLockEntry(r));   // skill locks
            fc.Unknown5 = r.ReadInt32();
            fc.Unknown6 = ReadArray(ReadInt32Count(r), () => ReadLockEntry(r));   // perk locks
            fc.Unknown7 = r.ReadInt32();
            fc.Unknown8 = ReadArray(ReadInt32Count(r), () => ReadLockEntry(r));   // nano locks
            fc.Stats1 = ReadArray(ReadX3F1Count(r), () => new GameTuple<int, int> { Value1 = r.ReadInt32(), Value2 = r.ReadInt32() });
            fc.Stats2 = ReadArray(ReadX3F1Count(r), () => new GameTuple<int, int> { Value1 = r.ReadInt32(), Value2 = r.ReadInt32() });
            fc.Stats3 = ReadArray(ReadX3F1Count(r), () => new GameTuple<byte, byte> { Value1 = r.ReadByte(), Value2 = r.ReadByte() });
            fc.Stats4 = ReadArray(ReadX3F1Count(r), () => new GameTuple<byte, short> { Value1 = r.ReadByte(), Value2 = r.ReadInt16() });
            fc.AbsorbStats = ReadArray(ReadInt32Count(r), () => new GameTuple<int, int> { Value1 = r.ReadInt32(), Value2 = r.ReadInt32() });   // SkillMap

            fc.TeamFlags = r.ReadInt32();
            if (fc.TeamFlags == 1 || fc.TeamFlags == 3) fc.TeamIdentity = ReadIdentity(r);
            if (fc.TeamFlags == 3) fc.RaidTeamIndex = r.ReadInt32();
            if (fc.TeamFlags == 1) fc.TeamMembers = ReadArray(ReadX3F1Count(r), () => ReadTeamMember(r));
            else if (fc.TeamFlags == 3)
            {
                fc.RaidTeams = ReadArray(6, () => ReadArray(ReadX3F1Count(r), () => ReadTeamMember(r)));
                if (fc.RaidTeamIndex is int ri && ri >= 0 && ri < 6) fc.TeamMembers = fc.RaidTeams[ri];
            }
            else if (fc.TeamFlags != 0) throw new InvalidOperationException($"team flags {fc.TeamFlags}");
            fc.UnknownIdentities = fc.TeamIdentity.HasValue ? new[] { fc.TeamIdentity.Value } : new Identity[0];

            fc.Pets = ReadArray(ReadX3F1Count(r), () => ReadIdentity(r));
            fc.Buffs = ReadArray(ReadX3F1Count(r), () => ReadBuff(r));
            fc.Perks = ReadArray(ReadX3F1Count(r), () => ReadResearchGoal(r));   // ResearchGoals

            if (r.Position != packetLength)
                throw new InvalidOperationException($"FullCharacter read ended at {r.Position} of {packetLength}");
            return fc;
        }
    }
}
