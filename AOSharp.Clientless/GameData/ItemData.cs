using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Clientless
{
	public static class ItemData
    {
        private static long DUMMYITEM_START_OFFSET;
        private static long NANOITEM_START_OFFSET;
        private static string DATA_PATH = $"GameData\\ItemData.bin";
        private static string OFFSET_PATH = $"GameData\\ItemData.idx";
        private static Dictionary<int, long> _offsets;
        private static Dictionary<int, ItemBase> _itemDataCache = new Dictionary<int, ItemBase>();

        public static bool Find<T>(int id, out T baseItem) where T : ItemBase
        {
            baseItem = null;

            if (!Client.ItemDataLoaded)
                return false;

            if (_offsets == null)
                LoadOffsets();

            if (!_offsets.TryGetValue(id, out long offset))
            {
                Logger.Warning($"Item with id: {id} not found");
                return false;
            }

            if (_itemDataCache.TryGetValue(id, out ItemBase itemBase))
            {
                if (itemBase is T type)
                {
                    baseItem = type;
                    return true;
                }
                else
                    return false;
            }

            if (!File.Exists(DATA_PATH))
            {
                Logger.Warning($"ItemData Data File Not Found.");
                return false;
            }

            using (FileStream item = new FileStream(DATA_PATH, FileMode.Open, FileAccess.Read))
            {
                item.Seek(offset, SeekOrigin.Begin);

                using (BinaryReader reader = new BinaryReader(item))
                {
                    if (_offsets[id] >= NANOITEM_START_OFFSET)
                        baseItem = DeserializeNanoItem(reader) as T;
                    else
                        baseItem = DeserializeDummyItem(reader) as T;
                }
            }

            return baseItem != null;
        }

        /// <summary>
        /// All nano-program template ids in the offline data (offsets at/after the nano section).
        /// Use with Find&lt;NanoItem&gt; + MeetsUseReqs to build "castable / learnable" lists.
        /// </summary>
        public static IReadOnlyList<int> AllNanoIds()
        {
            if (_offsets == null)
                LoadOffsets();

            if (_offsets == null)
                return new List<int>();

            return _offsets.Where(kv => kv.Value >= NANOITEM_START_OFFSET).Select(kv => kv.Key).ToList();
        }

        private static void LoadOffsets()
        {
            _offsets = new Dictionary<int, long>();

            if (!File.Exists(OFFSET_PATH))
            {
                Logger.Warning($"ItemData Index File Not Found.");
                return;
            }

            using (FileStream offsets = new FileStream(OFFSET_PATH, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(offsets))
                {
                    DUMMYITEM_START_OFFSET = reader.ReadInt64();
                    NANOITEM_START_OFFSET = reader.ReadInt64();

                    int count = reader.ReadInt32();

                    for (int i = 0; i < count; i++)
                        _offsets.Add(reader.ReadInt32(), reader.ReadInt64());
                }
            }
        }
        private static DummyItem DeserializeDummyItem(BinaryReader reader)
        {
            ItemBase itemBase = DeserializeItemBase(reader);
            DummyItem dummyItem = new DummyItem(itemBase.Id, itemBase.Ql);

            dummyItem.Name = itemBase.Name;
            dummyItem.Icon = itemBase.Icon;
            dummyItem.Modifiers = itemBase.Modifiers;
            dummyItem.Criteria = itemBase.Criteria;
            return dummyItem;
        }

        private static NanoItem DeserializeNanoItem(BinaryReader reader)
        {
            ItemBase itemBase = DeserializeItemBase(reader);

            NanoItem nanoItem = new NanoItem(itemBase.Id, itemBase.Ql);

            nanoItem.Name = itemBase.Name;
            nanoItem.Icon = itemBase.Icon;
            nanoItem.Modifiers = itemBase.Modifiers;
            nanoItem.Criteria = itemBase.Criteria;
            nanoItem.Cost = reader.ReadInt16();
            nanoItem.NanoLine = (NanoLine)reader.ReadInt32();
            nanoItem.NanoSchool = (NanoSchool)reader.ReadInt16();
            nanoItem.NCU = reader.ReadInt16();
            nanoItem.StackingOrder = reader.ReadInt32();
            nanoItem.Range = reader.ReadByte();
            nanoItem.TotalTimeInTicks = reader.ReadInt32();
            nanoItem.AttackDelayInTicks = reader.ReadInt16();
            nanoItem.RechargeDelayInTicks = reader.ReadInt16();

            return nanoItem;
        }

        private static ItemBase DeserializeItemBase(BinaryReader reader)
        {
            string name = reader.ReadString();
            int icon = reader.ReadInt32();
            ItemBase itemBase = new ItemBase(reader.ReadInt32(), reader.ReadInt16());
            itemBase.Name = name;
            itemBase.Icon = icon;
            itemBase.Criteria = new Dictionary<ItemActionInfo, List<RequirementCriterion>>();
            byte criteriaCount = reader.ReadByte();

            for (int i = 0; i < criteriaCount; i++)
            {
                List<RequirementCriterion> criteriaList = new List<RequirementCriterion>();
                ItemActionInfo key = (ItemActionInfo)reader.ReadByte();
                byte reqCount = reader.ReadByte();

                for (int j = 0; j < reqCount; j++)
                {
                    RequirementCriterion req = new RequirementCriterion();
                    req.Param1 = reader.ReadInt16();
                    req.Param2 = reader.ReadInt32();
                    req.Operator = (UseCriteriaOperator)reader.ReadByte();
                    criteriaList.Add(req);
                }

                itemBase.Criteria.Add(key, criteriaList);
            }

            itemBase.Modifiers = new Dictionary<SpellListType, Dictionary<Stat, int>>();
            int modifiersCount = reader.ReadByte();

            for (int i = 0; i < modifiersCount; i++)
            {
                Dictionary<Stat, int> spellModifiers = new Dictionary<Stat, int>();
                SpellListType key = (SpellListType)reader.ReadInt32();
                byte spellCount = reader.ReadByte();

                for (int j = 0; j < spellCount; j++)
                    spellModifiers.Add((Stat)reader.ReadInt16(), reader.ReadInt32());

                itemBase.Modifiers.Add(key, spellModifiers);
            }

            return itemBase;
        }
    }
}
