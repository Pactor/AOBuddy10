using AOSharp.Common.GameData;
using AOSharp.Common.Unmanaged.Interfaces;
using SmokeLounge.AOtomation.Messaging.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace AOSharp.Clientless
{
    public static class Extensions
    {
        public static bool Find(this IReadOnlyDictionary<Stat, Cooldown> cooldowns, Stat stat, out Cooldown cooldown) => cooldowns.TryGetValue(stat, out cooldown);

        public static bool Contains(this IReadOnlyDictionary<Stat, Cooldown> cooldowns, Stat stat) => cooldowns.ContainsKey(stat);

        public static bool Find(this IReadOnlyList<Buff> buffs, int id, out Buff buff) => (buff = buffs.FirstOrDefault(x => x.Id == id)) != null;

        public static bool Contains(this IReadOnlyList<Buff> buffs, int id) => Contains(buffs, new[] { id });

        public static bool Remove(this List<Buff> buffs, int id)
        {
            var buff = buffs.First(x => x.Id == id);

            if (buff == null)
                return false;

            return buffs.Remove(buff);
        }
        public static bool Contains(this IReadOnlyList<Buff> buffs, NanoLine nanoLine) => buffs.Any(b => nanoLine == b.NanoItem.NanoLine);

        public static bool Contains(this IReadOnlyList<Buff> buffs, int[] ids) => buffs.Any(b => ids.Contains(b.Id));

        public static bool Find(this IReadOnlyList<Item> items, Identity slot, out Item item) => (item = items.FirstOrDefault(x => x.Slot == slot)) != null;

        public static bool FindByIdentity(this IReadOnlyList<Item> items, Identity identity, out Item item) => (item = items.FirstOrDefault(x => x.UniqueIdentity == identity)) != null;

        public static bool Find(this IReadOnlyList<Item> items, int id, out Item item) => (item = items.FirstOrDefault(x => x.Id == id || x.HighId == id)) != null;

        public static bool FindAtQl(this IReadOnlyList<Item> items, int id, int quality, out Item item) => (item = items.FirstOrDefault(x => (x.Id == id || x.HighId == id) && x.Ql == quality)) != null;

        public static bool Find(this IReadOnlyList<Item> items, int lowId, int highId, out Item item) => (item = items.FirstOrDefault(x => x.Id == lowId && x.HighId == highId)) != null;

        public static bool Find(this IEnumerable<Container> containers, Identity identity, out Container container) => (container = containers.FirstOrDefault(x => x.Identity == identity)) != null;

        public static bool Find(this IEnumerable<Container> containers, Identity slot, out Item item) => (item = containers.SelectMany(x => x.Items).FirstOrDefault(x => x.Slot == slot)) != null;

        public static void RemoveItem(this IEnumerable<Container> containers, Item item, out Container owningContainer)
        {
            RemoveItem(containers, item.Slot, out owningContainer);
        }

        public static void RemoveItem(this IEnumerable<Container> containers, Identity slot, out Container owningContainer)
        {
            owningContainer = null;

            foreach (Container container in containers)
            {
                foreach (Item contItem in container.Items.ToList())
                {
                    if (contItem.Slot != slot)
                        continue;

                    owningContainer = container;
                    container.Items.Remove(contItem);
                    return;
                }
            }
        }

        public static Dictionary<Stat, int> ToDict(this GameTuple<Stat, int>[] stats)
        {
            Dictionary<Stat, int> dictStats = new Dictionary<Stat, int>();

            foreach (var stat in stats)
                dictStats.Add(stat.Value1, stat.Value2);

            return dictStats;
        }

        public static List<Item> FindAll(this IReadOnlyList<Item> items, IEnumerable<int> ids) => items.Where(x => ids.Contains(x.Id)).ToList();

    }
}
