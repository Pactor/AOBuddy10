using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;

namespace AOSharp.Clientless
{
    internal static class StaticDynelData
    {
        private static Dictionary<PlayfieldId, Dictionary<IdentityType, Dictionary<int, List<PfDynel>>>> _staticDynelData;
      
        private static Dictionary<PlayfieldId, Dictionary<IdentityType, Dictionary<int, List<PfDynel>>>> StaticDynels
        {
            get
            {
                if (_staticDynelData == null)
                    Deserialize($"GameData\\StaticDynelData.bin");

                return _staticDynelData;
            }
        }

        public static bool GetDynels(PlayfieldId playfieldId, out List<StaticDynel> dynels)
        {
            dynels = new List<StaticDynel>();

            if (!StaticDynels.ContainsKey(playfieldId))
                return false;

            foreach (var staticDynelsTypes in StaticDynels[playfieldId])
            {
                var identity = staticDynelsTypes.Key;

                foreach (var staticDynels in staticDynelsTypes.Value)
                {
                    var templateId = staticDynels.Key;

                    foreach (var staticDynel in staticDynels.Value)
                    {
                        dynels.Add(new StaticDynel(templateId, new Identity((IdentityType)identity, (int)staticDynel.Instance), staticDynel.Position, staticDynel.Rotation));
                    }
                }
            }

            return dynels.Count != 0;
        }

        internal static bool GetDynels(PlayfieldId playfieldId, IdentityType type, out List<StaticDynel> dynels)
        {
            dynels = new List<StaticDynel>();

            if (!StaticDynels.ContainsKey(playfieldId))
                return false;

            if (!StaticDynels[playfieldId].TryGetValue(type, out var staticDynelsTypes))
                return false;

            foreach (var staticDynels in staticDynelsTypes)
            {
                var templateId = staticDynels.Key;

                foreach (var staticDynel in staticDynels.Value)
                {
                    dynels.Add(new StaticDynel(templateId, new Identity(type, (int)staticDynel.Instance), staticDynel.Position, staticDynel.Rotation));
                }
            }

            return dynels.Count != 0;
        }

        private static void Deserialize(string filePath)
        {
            var playfieldDynels = new Dictionary<PlayfieldId, Dictionary<IdentityType, Dictionary<int, List<PfDynel>>>>();

            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                // v2 (written by tools/rdb-zoning/staticdynels.py from the RDB's statel records): 'ASDD',
                // int 2, and a rotation per dynel. The v1 bin the SDK shipped has no rotations, so every
                // StaticDynel was built facing identity — all mission terminals "faced" north (2026-09-26).
                // A v1 file starts straight in with the playfield count.
                byte[] head = reader.ReadBytes(4);
                bool v2 = head.Length == 4 && head[0] == (byte)'A' && head[1] == (byte)'S' && head[2] == (byte)'D' && head[3] == (byte)'D';
                int playfieldCount;
                if (v2) { reader.ReadInt32(); playfieldCount = reader.ReadInt32(); }
                else playfieldCount = BitConverter.ToInt32(head, 0);

                for (int i = 0; i < playfieldCount; i++)
                {
                    int playfield = reader.ReadInt32();
                    var identityDynelDict = new Dictionary<IdentityType, Dictionary<int, List<PfDynel>>>();
                    int identityTypesCount = reader.ReadInt32();

                    for (int j = 0; j < identityTypesCount; j++)
                    {
                        int identityType = reader.ReadInt32();
                        var templateIdDynelDict = new Dictionary<int, List<PfDynel>>();
                        int templateIdCount = reader.ReadInt32();

                        for (int k = 0; k < templateIdCount; k++)
                        {
                            int templateId = reader.ReadInt32();
                            var dynelList = new List<PfDynel>();
                            int dynelCount = reader.ReadInt32();

                            for (int l = 0; l < dynelCount; l++)
                            {
                                uint instance = reader.ReadUInt32();
                                var position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                                var rotation = v2
                                    ? new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
                                    : Quaternion.Identity;

                                dynelList.Add(new PfDynel(instance, position, rotation));
                            }

                            templateIdDynelDict.Add(templateId, dynelList);
                        }

                        identityDynelDict.Add((IdentityType)identityType, templateIdDynelDict);
                    }

                    playfieldDynels.Add((PlayfieldId)playfield, identityDynelDict);
                }
            }

            _staticDynelData = playfieldDynels;
        }

        private class PfDynel
        {
            internal uint Instance;

            internal Vector3 Position;

            internal Quaternion Rotation;

            internal PfDynel(uint instance, Vector3 position, Quaternion rotation)
            {
                Instance = instance;
                Position = position;
                Rotation = rotation;
            }
        }
    }
}