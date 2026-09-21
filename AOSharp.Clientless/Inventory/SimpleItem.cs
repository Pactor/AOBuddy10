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
    public class SimpleItem : Dynel
    {
        public Dictionary<Stat, int> Stats;
        public ACGItemQueryData ACGItem = new ACGItemQueryData();

        public SimpleItem(Identity identity, Vector3? position, Quaternion? rotation, Dictionary<Stat, int> stats) : this(identity, stats)
        {
            if (position.HasValue && rotation.HasValue)
                InitTransform(position.Value, rotation.Value);
        }

        public SimpleItem(Identity identity, Dictionary<Stat, int> stats) : base(identity)
        {
            SetStats(stats);
        }


        private void SetStats(Dictionary<Stat, int> stats)
        {
            Stats = stats;

            if (Stats.TryGetValue(Stat.ACGItemTemplateID, out int lowId) && lowId == 0)
                return;

            ACGItem = new ACGItemQueryData
            {
                LowId = Stats.FirstOrDefault(x => x.Key == Stat.ACGItemTemplateID).Value,
                HighId = Stats.FirstOrDefault(x => x.Key == Stat.ACGItemTemplateID2).Value,
                QL = Stats.FirstOrDefault(x => x.Key == Stat.ACGItemLevel).Value,
            };
        }
    }
}