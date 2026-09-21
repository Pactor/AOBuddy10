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
    public class WeaponItem : SimpleItem
    {
        public WeaponItem(Identity identity, Vector3? position, Quaternion? rotation, Dictionary<Stat, int> stats) : base(identity, position, rotation, stats) { }

        public WeaponItem(Identity identity, Dictionary<Stat, int> stats) : base(identity, stats) { }
    }
}