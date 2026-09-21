using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
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
                        dynels.Add(new StaticDynel(templateId, new Identity((IdentityType)identity, (int)staticDynel.Instance), staticDynel.Position));
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
                    dynels.Add(new StaticDynel(templateId, new Identity(type, (int)staticDynel.Instance), staticDynel.Position));
                }
            }

            return dynels.Count != 0;
        }

        private static void Deserialize(string filePath)
        {
            var playfieldDynels = new Dictionary<PlayfieldId, Dictionary<IdentityType, Dictionary<int, List<PfDynel>>>>();

            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                int playfieldCount = reader.ReadInt32();

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

                                dynelList.Add(new PfDynel(instance, position));
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

            internal PfDynel(uint instance, Vector3 position)
            {
                Instance = instance;
                Position = position;
            }
        }
    }
}