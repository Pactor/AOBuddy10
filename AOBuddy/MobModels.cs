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
            public override string ToString() =>
                $"monsterData={MonsterData} scale={MonsterScale} head={(HeadMesh?.ToString() ?? "-")} meshes=[{string.Join(",", Meshes ?? new uint[0])}] textures=[{string.Join(",", Textures ?? new int[0])}]";
        }

        private static readonly ConcurrentDictionary<Identity, Model> _byId = new ConcurrentDictionary<Identity, Model>();

        public static void OnMessage(Message m)
        {
            if (!(m?.Body is SimpleCharFullUpdateMessage s)) return;
            _byId[s.Identity] = new Model
            {
                MonsterData = s.MonsterData, MonsterScale = s.MonsterScale, HeadMesh = s.HeadMesh,
                Meshes = s.Meshes?.Select(x => x.Id).ToArray(), Textures = s.Textures?.Select(x => x.Id).ToArray(),
            };
        }

        public static Model Get(Identity id) => _byId.TryGetValue(id, out var md) ? md : null;
    }
}
