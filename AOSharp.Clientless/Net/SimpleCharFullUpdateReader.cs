using System.Collections.Generic;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using StreamReader = SmokeLounge.AOtomation.Messaging.Serialization.StreamReader;

namespace AOSharp.Clientless.Net
{
    /// <summary>
    /// A corrected wire reader for the N3 SimpleCharFullUpdate message.
    /// </summary>
    /// <remarks>
    /// This is a workaround for AOSharpSDK 1.0.89. The SimpleCharFullUpdate
    /// deserializer compiled into the NuGet AOSharp.Common.dll is an older,
    /// incomplete version: it mis-parses pet-type / complex NPC spawns
    /// (e.g. "Uncontrollable Anger") and throws EndOfStreamException, so the
    /// spawn is dropped and the dynel never registers in DynelManager. There is
    /// no public API on MessageSerializer to register or replace a serializer,
    /// so instead of fixing the sealed NuGet reader we detect SimpleCharFullUpdate
    /// packets in NetworkSession before the buggy reader runs and parse them here.
    ///
    /// The field order below is a port of OmniCell's corrected reader, which was
    /// reverse-engineered field-for-field from the game client
    /// (Gamecode.dll n3SimpleCharFullUpdateIIR_t) and decodes the reference
    /// captures with zero errors. Notable fixes over the old SDK reader:
    ///   - HasExtendedTextures gates an X3F1-counted array of 44-byte entries,
    ///     not a single 4-byte field.
    ///   - Active-nano entries are five int32s each (20 bytes).
    ///   - The small health form is unsigned (a 43,140-hp pet read as -22,396).
    ///   - Several fields (vehicle-data blob, immune/second-scale bytes, waypoints,
    ///     weapon pairs, shadowbreed, cat textures, Flags2 sub-blocks) were missing.
    ///
    /// The AOSharp SimpleCharFullUpdateMessage shape differs from OmniCell's in a
    /// number of trailing / cosmetic fields (it has no NpcInfo, no ParentDynel,
    /// etc). We parse the full wire in order so nothing downstream is misaligned,
    /// but only populate the fields the SDK actually consumes (see SimpleChar,
    /// NpcChar, PlayerChar and DynelManager.OnDynelSpawned); wire fields with no
    /// SDK home are read to advance the offset and discarded.
    ///
    /// The wire flag/Flags2 bit positions are identical between OmniCell and the
    /// AOSharp enums (only some names differ), so we branch on the raw int bits
    /// with named constants below and store the raw value straight into
    /// message.Flags / message.Flags2.
    /// </remarks>
    internal static class SimpleCharFullUpdateReader
    {
        // Wire flag bits (Flags int32), named per the client / OmniCell meaning.
        private const int HasPlayfieldId = 0x00000040;
        private const int HasParentDynel = 0x00000020;
        private const int HasHeading = 0x00000200;
        private const int IsNpc = 0x00000001;
        private const int HasSmallNpcFamily = 0x00020000;
        private const int HasSmallNpcLosHeight = 0x00080000;
        private const int HasSmallPetType = 0x02000000;
        private const int HasOrgName = 0x04000000;
        private const int HasExtendedLevel = 0x00001000;
        private const int HasSmallHealth = 0x00000800;
        private const int HasSmallHealthDamage = 0x00004000;
        private const int HasHeadMesh = 0x00000080;
        private const int HasExtendedRunSpeed = 0x00002000;
        private const int IsUnderAttack = 0x00000400;
        private const int HasExtendedTextures = 0x00000010;
        private const int IsImmune = 0x00800000;
        private const int HasSecondMonsterScale = 0x01000000;
        private const int HasWaypoints = 0x00010000;
        private const int HasNoWeaponPairs = 0x00000100;
        private const int HasShadowBreed = 0x20000000;
        private const int HasCatTextures = 0x40000000;

        // Flags2 bits.
        private const int Flags2HasStatUpdate = 0x1;
        private const int Flags2HasBattlestationSide = 0x2;
        private const int Flags2HasPetMaster = 0x4;

        // TEMP DIAGNOSTIC: distinct chars already dumped to scfuwire.log (dedupe key = npc/player + name).
        private static readonly System.Collections.Generic.HashSet<string> _wireSeen = new System.Collections.Generic.HashSet<string>();

        /// <summary>
        /// Reads a complete SimpleCharFullUpdate message body (including the N3
        /// message preamble) from a stream positioned at the start of the body,
        /// i.e. immediately after the 16-byte packet header.
        /// </summary>
        public static SimpleCharFullUpdateMessage Read(StreamReader r)
        {
            var scfu = new SimpleCharFullUpdateMessage();

            // N3Message preamble.
            scfu.N3MessageType = (N3MessageType)r.ReadInt32();
            scfu.Identity = ReadIdentity(r);
            scfu.Unknown = r.ReadByte();

            scfu.Version = r.ReadByte();

            int flags = r.ReadInt32();
            scfu.Flags = (SimpleCharFullUpdateFlags)flags;

            if ((flags & HasPlayfieldId) != 0)
                scfu.PlayfieldId = r.ReadInt32();

            if ((flags & HasParentDynel) != 0)
                ReadIdentity(r); // parent dynel: read to advance, no SDK field for it

            scfu.Position = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

            if ((flags & HasHeading) != 0)
                scfu.Heading = new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            else
                scfu.Heading = new Quaternion(0f, 0f, 0f, 1f); // identity, so downstream movement never sees null

            scfu.Appearance = new SmokeLounge.AOtomation.Messaging.GameData.Appearance { Value = r.ReadUInt32() };

            // Written length includes the NUL terminator; ReadString trims it.
            int nameLength = r.ReadByte();
            scfu.Name = r.ReadString(nameLength);

            scfu.CharacterFlags = (CharacterFlags)r.ReadInt32();
            scfu.AccountFlags = r.ReadInt16();
            scfu.Expansions = r.ReadInt16();

            if ((flags & IsNpc) != 0)
            {
                // The AOSharp message has no NpcInfo type and SimpleChar only reads
                // CharacterInfo when it is a PlayerInfo, so read the NPC fields to
                // keep the offset correct and leave CharacterInfo null.
                if ((flags & HasSmallNpcFamily) != 0) r.ReadByte(); else r.ReadInt16();
                if ((flags & HasSmallNpcLosHeight) != 0) r.ReadByte(); else r.ReadInt16();
                if ((flags & HasSmallPetType) != 0) r.ReadByte(); else r.ReadInt16();
                short npcUnknown2 = r.ReadInt16();
                if (npcUnknown2 > 0)
                    r.ReadByte();
            }
            else
            {
                var pc = new SimpleCharInfo.PlayerInfo();
                pc.CurrentNano = r.ReadUInt32();
                pc.Team = r.ReadInt32();
                pc.Swim = r.ReadInt16();
                pc.StrengthBase = r.ReadInt16();
                pc.AgilityBase = r.ReadInt16();
                pc.StaminaBase = r.ReadInt16();
                pc.IntelligenceBase = r.ReadInt16();
                pc.SenseBase = r.ReadInt16();
                pc.PsychicBase = r.ReadInt16();

                if (scfu.CharacterFlags.HasFlag(CharacterFlags.HasVisibleName))
                {
                    pc.FirstName = r.ReadString(r.ReadInt16());
                    pc.LastName = r.ReadString(r.ReadInt16());
                }

                if ((flags & HasOrgName) != 0)
                {
                    if (scfu.Version > 0x39)
                        pc.OrgId = r.ReadInt32();

                    pc.OrgName = r.ReadString(r.ReadInt16());
                }

                scfu.CharacterInfo = pc;
            }

            scfu.Level = (flags & HasExtendedLevel) != 0 ? r.ReadInt16() : (short)r.ReadByte();

            // Small health is UNSIGNED (16-bit) - reading it signed produced negative
            // hp for high-health pets.
            scfu.Health = (flags & HasSmallHealth) != 0 ? r.ReadUInt16() : r.ReadInt32();

            if ((flags & HasSmallHealthDamage) != 0)
                scfu.HealthDamage = r.ReadByte();
            else if ((flags & HasSmallHealth) != 0)
                scfu.HealthDamage = r.ReadUInt16();
            else
                scfu.HealthDamage = r.ReadInt32();

            scfu.MonsterData = r.ReadUInt32();
            scfu.MonsterScale = r.ReadInt16();
            scfu.VisualFlags = r.ReadInt16();
            scfu.VisibleTitle = r.ReadByte();

            // Counted vehicle-data blob (client copies it wholesale then skips it).
            scfu.ScfuUnk1 = r.ReadBytes(r.ReadInt32());

            if ((flags & HasHeadMesh) != 0)
                scfu.HeadMesh = (int)r.ReadUInt32();

            scfu.RunSpeedBase = (flags & HasExtendedRunSpeed) != 0 ? r.ReadInt16() : (short)r.ReadByte();

            if ((flags & IsUnderAttack) != 0)
                scfu.FightingTarget = ReadIdentity(r);

            if ((flags & HasExtendedTextures) != 0)
            {
                // X3F1-counted array of 44-byte entries: name[32] + 3 int32.
                int count = X3F1Count(r.ReadInt32());
                for (int i = 0; i < count; i++)
                    r.ReadBytes(44); // not consumed by the SDK; advance the offset
            }

            if ((flags & IsImmune) != 0)
                r.ReadByte();

            if ((flags & HasSecondMonsterScale) != 0)
                r.ReadByte();

            int nanoCount = X3F1Count(r.ReadInt32());
            var nanos = new SimpleCharInfo.ActiveNano[nanoCount];
            for (int i = 0; i < nanoCount; i++)
            {
                // Wire: nanoId, nanoInstance, unknown, time1, time2 (5 int32).
                // The SDK's ActiveNano stores the first two as an Identity, the
                // third as NanoInstance, then Time1/Time2. SimpleChar consumes
                // Identity.Instance (buff nano id) and Time2 (expiry).
                int nanoId = r.ReadInt32();
                int nanoInstance = r.ReadInt32();
                int unknown = r.ReadInt32();
                int time1 = r.ReadInt32();
                int time2 = r.ReadInt32();
                nanos[i] = new SimpleCharInfo.ActiveNano
                {
                    Identity = new Identity((IdentityType)nanoId, nanoInstance),
                    NanoInstance = unknown,
                    Time1 = time1,
                    Time2 = time2
                };
            }
            scfu.ActiveNanos = nanos;

            if ((flags & HasWaypoints) != 0)
            {
                ReadIdentity(r); // waypoint target
                int pointCount = r.ReadInt32(); // plain count, not X3F1
                var points = new List<Vector3>(pointCount);
                for (int i = 0; i < pointCount; i++)
                    points.Add(new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()));
                scfu.Waypoints = points;
            }

            int textureCount = X3F1Count(r.ReadInt32());
            var textures = new Texture[textureCount];
            for (int i = 0; i < textureCount; i++)
            {
                var texture = new Texture
                {
                    Place = r.ReadInt32(),
                    Id = r.ReadInt32(),
                    Unknown = r.ReadInt32()
                };

                // HasExtra: Place>0 and its high 16 bits > 0 means two trailing int32s.
                if (texture.Place > 0 && (short)(texture.Place >> 16) > 0)
                {
                    r.ReadInt32(); // overlay id
                    r.ReadInt32(); // alpha mode
                }

                textures[i] = texture;
            }
            scfu.Textures = textures;

            int meshCount = X3F1Count(r.ReadInt32());
            var meshes = new SmokeLounge.AOtomation.Messaging.GameData.Mesh[meshCount];
            for (int i = 0; i < meshCount; i++)
            {
                meshes[i] = new SmokeLounge.AOtomation.Messaging.GameData.Mesh
                {
                    Position = r.ReadByte(),
                    Id = r.ReadUInt32(),
                    OverrideTextureId = r.ReadInt32(),
                    Layer = r.ReadByte()
                };
            }
            scfu.Meshes = meshes;

            if ((flags & HasNoWeaponPairs) != 0)
            {
                int pairCount = X3F1Count(r.ReadInt32());
                for (int i = 0; i < pairCount; i++)
                    r.ReadBytes(16); // 4 int32 per pair; not consumed
            }

            if ((flags & HasShadowBreed) != 0)
                r.ReadByte();

            if ((flags & HasCatTextures) != 0)
            {
                int catCount = X3F1Count(r.ReadInt32());
                for (int i = 0; i < catCount; i++)
                    r.ReadBytes(8); // 2 int32 per entry; not consumed
            }

            int flags2 = r.ReadInt32();
            scfu.Flags2 = (ScfuFlags2)flags2;

            // TEMP DIAGNOSTIC (pets-not-owned): dump each distinct char's raw Flags/Flags2 so we can see
            // whether the pet-master bit (Flags2 & 0x4) is present and whether the read is aligned. Deduped
            // by name, appended to scfuwire.log next to the host exe.
            try
            {
                bool isNpc = (flags & IsNpc) != 0;
                string key = (isNpc ? "N:" : "P:") + (scfu.Name ?? "");
                if (_wireSeen.Add(key))
                {
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(System.AppContext.BaseDirectory, "scfuwire.log"),
                        $"{System.DateTime.Now:HH:mm:ss.fff} name='{scfu.Name}' npc={isNpc} flags=0x{flags:X8} flags2=0x{flags2:X8} hasPetMaster={((flags2 & Flags2HasPetMaster) != 0)} idInst={scfu.Identity.Instance}\n");
                }
            }
            catch { }

            if ((flags2 & Flags2HasStatUpdate) != 0)
            {
                int statCount = r.ReadInt32();
                for (int i = 0; i < statCount; i++)
                    r.ReadBytes(8); // stat + value; not consumed
                r.ReadInt32();      // mech data
                ReadIdentity(r);    // source
            }

            if ((flags2 & Flags2HasBattlestationSide) != 0)
                r.ReadByte();

            if ((flags2 & Flags2HasPetMaster) != 0)
            {
                // A single int32 pet-master id. NpcChar exposes this as Owner, so
                // wrap it in an Identity the consumer can use.
                int petMaster = r.ReadInt32();
                scfu.Owner = new Identity(IdentityType.SimpleChar, petMaster);
            }

            scfu.ScfuUnk2 = r.ReadByte(); // trailing Unknown2

            return scfu;
        }

        private static Identity ReadIdentity(StreamReader r)
        {
            int type = r.ReadInt32();
            int instance = r.ReadInt32();
            return new Identity((IdentityType)type, instance);
        }

        /// <summary>
        /// Turns an X3F1 encoded array header into an element count. The wire form
        /// is (count + 1) * 0x3F1. An invalid header means the read is already at
        /// the wrong offset, so throw rather than silently returning an empty array
        /// (which is how the old reader hid a mis-parse). The throw is caught and
        /// logged by NetworkSession, exactly like any other deserialize failure.
        /// </summary>
        private static int X3F1Count(int value)
        {
            if (value <= 0 || value % 0x3F1 != 0 || (value / 0x3F1) - 1 > 0x7530)
                throw new System.InvalidOperationException(
                    value + " is not an X3F1 array header; SimpleCharFullUpdate read is misaligned");

            return (value / 0x3F1) - 1;
        }
    }
}
