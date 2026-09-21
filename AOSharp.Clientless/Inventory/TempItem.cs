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
    public class TempItem : UniqueItem
    {
        private Cooldown _remainingTime;
        public double RemainingTime => _remainingTime != null ? _remainingTime.RemainingTime : 0;

        public TempItem(Identity slot, SimpleItem simpleItem) : base(slot, simpleItem)
        {
            _remainingTime = new Cooldown();
            _remainingTime.SetExpireTime(simpleItem.Stats.FirstOrDefault(x => x.Key == Stat.TimeExist).Value / 100f);
        }
    }
}