using System.Collections.Generic;
using AOSharp.Common.GameData;

namespace AOSharp.Clientless
{
    public class ItemBase
    {
        public string Name;
        public int Icon;
        public int Id;
        public int Ql;
        public Dictionary<ItemActionInfo, List<RequirementCriterion>> Criteria = new Dictionary<ItemActionInfo, List<RequirementCriterion>>();
        public Dictionary<SpellListType, Dictionary<Stat, int>> Modifiers = new Dictionary<SpellListType, Dictionary<Stat, int>>();
        public Dictionary<Stat, int> UseModifiers => Modifiers[SpellListType.Use];
        public Dictionary<Stat, int> WearModifiers => Modifiers[SpellListType.Wear];

        public ItemBase(int id, int ql)
        {
            Id = id;
            Ql = ql;
        }

        /// <summary>
        /// True if the local player (optionally against <paramref name="target"/>) currently
        /// meets this item/nano's use requirements. For a NanoItem this answers "can I cast it?"
        /// — profession, level and skill gates are all evaluated from live stats.
        /// </summary>
        public bool MeetsUseReqs(SimpleChar target = null, bool ignoreTargetReqs = false)
        {
            if (!Criteria.TryGetValue(ItemActionInfo.UseCriteria, out List<RequirementCriterion> useCriteria))
                return true; // no gate = usable
            return new ReqChecker(useCriteria).MeetsReqs(target, ignoreTargetReqs);
        }
    }
}