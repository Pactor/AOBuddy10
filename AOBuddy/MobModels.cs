using System.Collections.Concurrent;
using System.Linq;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOBuddy
{
    /// <summary>
    /// MOB MODELS (owner, 2026-09-27: "we need that model data always"). The model is not a stat on the dynel -
    /// MonsterData/Mesh/MonsterTexture read back empty for every mob at 14:37 - it comes in the spawn packet,
    /// SimpleCharFullUpdateMessage: MonsterData, MonsterScale, HeadMesh, Meshes[] and Textures[]. Kept per identity
    /// for the spawn log (NavController) and the mob atlas (MobAtlas).
    /// </summary>
    public static class MobModels
    {
        public sealed class Model
        {
            public uint MonsterData; public short MonsterScale; public int? HeadMesh;
            public uint[] Meshes; public int[] Textures;

            // Who this is, from the same packet (owner, 2026-09-29: the world spawn tables filled up
            // with players and player pets). The model alone cannot say: a person and a humanoid NPC
            // are both built from a head and textures and both carry MonsterData 0. These two can.
            //
            // IsPlayer: the IsNpc bit of the update's own flags word, which is also what decides
            // whether the server sends an NPC info block at all and so what AOSharp builds an NpcChar
            // from. Only NPCs are logged, so this is a belt-and-braces record rather than news - but
            // it is written down, because a table built from this file had no way to check.
            //
            // PetType and PetMaster are the ones that matter. A player's pet IS an NpcChar and so is
            // logged like any creature, and Salvinous and Rage Materialization popping in and walking
            // a patrol look exactly like a spawn doing the same. PetType is on the message; the owner
            // rides behind bit 4 of Flags2 and so is only in the updates carrying that bit, which is
            // why both are kept.
            //
            // The CharacterFlags Pet bit is deliberately not used: it reads set on ordinary mission
            // creatures - a Rhinoman Smasher had it - so it means something else.
            public bool IsPlayer; public short PetType; public int PetMaster;

            public bool IsPet => PetType != 0 || PetMaster != 0;

            public override string ToString() =>
                $"monsterData={MonsterData} scale={MonsterScale} head={(HeadMesh?.ToString() ?? "-")} meshes=[{string.Join(",", Meshes ?? new uint[0])}] textures=[{string.Join(",", Textures ?? new int[0])}] {(IsPlayer ? "player" : "npc")}{(IsPet ? " pet" : "")}";
        }

        private static readonly ConcurrentDictionary<Identity, Model> _byId = new ConcurrentDictionary<Identity, Model>();

        public static void OnMessage(Message m)
        {
            if (!(m?.Body is SimpleCharFullUpdateMessage s)) return;
            _byId[s.Identity] = new Model
            {
                MonsterData = s.MonsterData, MonsterScale = s.MonsterScale, HeadMesh = s.HeadMesh,
                Meshes = s.Meshes?.Select(x => x.Id).ToArray(), Textures = s.Textures?.Select(x => x.Id).ToArray(),
                IsPlayer = !s.Flags.HasFlag(SimpleCharFullUpdateFlags.IsNpc),
                PetType = s.PetType,
                PetMaster = s.Owner?.Instance ?? 0,
            };
        }

        public static Model Get(Identity id) => _byId.TryGetValue(id, out var md) ? md : null;
    }
}
