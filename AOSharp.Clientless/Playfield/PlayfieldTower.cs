using AOSharp.Common.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Clientless
{
    public class PlayfieldTower
    {
        public Identity PlaceholderId { get; internal set; }
        public Identity TowerCharId { get; internal set; }
        public Vector3 Position {  get; internal set; }
        public TowerClass Class { get; internal set; }
        public Side Side { get; internal set; }

        internal PlayfieldTower()
        {
        }
    }
}
