using AOSharp.Common.GameData;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using Newtonsoft.Json;
using System.IO;
using AOSharp.Clientless.Logging;

namespace AOSharp.Clientless
{
    public static class Playfield
    {
        public static PlayfieldId ModelId;
        public static string Name => _playfieldNames.TryGetValue((int)ModelId, out string name) ? name : ModelId.ToString();
        public static ReadOnlyCollection<PlayfieldTower> Towers => _towers.Values.ToList().AsReadOnly();
        public static EventHandler<TowerUpdateEventArgs> TowerUpdate;
        private static Dictionary<Identity, PlayfieldTower> _towers = new Dictionary<Identity, PlayfieldTower>();
        private static Dictionary<int, string> _playfieldNames;

        internal static void Init(PlayfieldAnarchyFMessage playfieldMessage)
        {
            ModelId = (PlayfieldId)playfieldMessage.PlayfieldId1.Instance;
            _towers.Clear();
            DynelManager.Reset();
            DynelManager.InitStaticDynels(ModelId);
            Inventory.ResetContainers();
            FullUpdateProxy.Reset();
        }

        internal static void MakeTower(TowerInfo towerInfo, PlayfieldTowerUpdateType updateReason)
        {
            PlayfieldTower tower = new PlayfieldTower
            {
                PlaceholderId = towerInfo.PlaceholderId,
                TowerCharId = towerInfo.TowerCharId,
                Position = towerInfo.Position,
                Side = towerInfo.Side,
                Class = towerInfo.Class
            };

            _towers.Add(towerInfo.PlaceholderId, tower);

            TowerUpdate?.Invoke(null, new TowerUpdateEventArgs
            {
                Tower = tower,
                UpdateType = updateReason
            });
        }

        internal static void DestroyTower(Identity placeholderId)
        {
            if (_towers.TryGetValue(placeholderId, out PlayfieldTower tower))
            {
                _towers.Remove(placeholderId);

                TowerUpdate?.Invoke(null, new TowerUpdateEventArgs
                {
                    Tower = tower,
                    UpdateType = PlayfieldTowerUpdateType.Destroyed
                });
            }
        }

        internal static void LoadPlayfieldNames()
        {
            try
            {
                _playfieldNames = JsonConvert.DeserializeObject<Dictionary<int, string>>(File.ReadAllText($"GameData\\PlayfieldNames.json"));
            }
            catch 
            {
                Logger.Error("Failed to load Playfield Names.");
                _playfieldNames = new Dictionary<int, string>();
            }
        }

        public static bool TryGetPlayfieldNameFromId(int id, out string playfieldName)
        {
            if (_playfieldNames == null)
            {
                playfieldName = string.Empty;
                return false;
            }

            return _playfieldNames.TryGetValue(id, out playfieldName);
        }
    }

    public enum PlayfieldTowerUpdateType
    {
        InitialLoad,
        Planted,
        Destroyed
    }

    public class TowerUpdateEventArgs : EventArgs
    {
        public PlayfieldTower Tower { get; set; }
        public PlayfieldTowerUpdateType UpdateType { get; set; }
    }
}
