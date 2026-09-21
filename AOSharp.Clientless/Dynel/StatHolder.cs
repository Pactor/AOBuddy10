using AOSharp.Common.GameData;
using System.Collections.Generic;

namespace AOSharp.Clientless
{
    public class StatHolder
    {
        private Dictionary<Stat, int> _stats = new Dictionary<Stat, int>();

        internal void SetStat(Stat stat, int value) => _stats[stat] = value;

        public virtual int GetStat(Stat stat) => _stats[stat];

        public T GetStat<T>(Stat stat) => (T)(object)_stats[stat];

        public virtual bool TryGetStat(Stat stat, out int value) => _stats.TryGetValue(stat, out value);
    }
}
