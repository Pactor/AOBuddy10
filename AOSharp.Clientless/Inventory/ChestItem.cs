using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOSharp.Clientless
{
    public class ChestItem : SimpleItem
    {
        public ChestItem(Identity identity, Vector3? position, Quaternion? rotation, Dictionary<Stat, int> stats) : base(identity, position, rotation, stats) { }

        public ChestItem(Identity identity, Dictionary<Stat, int> stats) : base(identity, stats) { }
    }
}