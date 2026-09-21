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
    public class VendingMachine : SimpleItem
    {
        public string Name { get; internal set; }

        public VendingMachine(Identity identity, Vector3? pos, Quaternion? rot, GameTuple<Stat, int>[] stats) : base(identity, pos, rot, stats.ToDict())
        {
            SetName();
        }

        private void SetName()
        {
            if (Stats.TryGetValue(Stat.StaticInstance, out int id) && id != 0)
                Name = new UniqueItem(Identity.None, Identity, id, id, 1, Stats).Name;
        }

        public override bool TryGetStat(Stat stat, out int value) => Stats.TryGetValue(stat, out value);
    }
}