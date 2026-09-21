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
    public class UniqueItem : Item
    {
        public Dictionary<Stat, int> Stats;

        public UniqueItem(Identity slot, SimpleItem simpleItem) : base(slot, simpleItem.Identity, simpleItem.ACGItem.LowId, simpleItem.ACGItem.HighId, simpleItem.ACGItem.QL)
        {
            Stats = simpleItem.Stats;
        }

        public UniqueItem(Identity slot, Identity identity, int lowId, int highId, int ql, Dictionary<Stat, int> stats) : base(slot, identity, lowId, highId, ql)
        {
            Stats = stats;
        }
    }
}