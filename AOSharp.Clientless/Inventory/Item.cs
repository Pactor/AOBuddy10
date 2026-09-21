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
    public class Item : DummyItem
    {
        public Identity Slot;

        public Identity UniqueIdentity;

        /// <summary>Stack size (how many are in this slot). 1 for non-stacking items.</summary>
        public int Count = 1;

        public readonly int HighId;

        public readonly int HighQl;

        public Item(int id, int highId, int ql) : base(id, ql)
        {
            Slot = Identity.None;
            HighId = highId;
            UniqueIdentity = Identity.None;
            CreateItem(id, highId, ql);
        }

        public Item(Identity slot, Identity identity, ACGItemQueryData itemData) : base(itemData.LowId, itemData.QL)
        {
            Slot = slot;
            HighId = itemData.HighId;
            UniqueIdentity = identity;
            CreateItem(itemData.LowId, itemData.HighId, itemData.QL);
        }

        public Item(Identity slot, Identity identity, int id, int highId, int ql) : base(id, ql)
        {
            Slot = slot;
            HighId = highId;
            UniqueIdentity = identity;
            CreateItem(id, highId, ql);
        }

        public void CreateItem(int id, int highId, int ql)
        {
            if (!ItemData.Find(id, out DummyItem lowTemplate))
                return;

            if (!ItemData.Find(highId, out DummyItem highTemplate))
                return;

            Name = lowTemplate.Name;

            foreach (var criteria in lowTemplate.Criteria)
            {
                var lowCriteria = criteria.Value;
                var highCriteria = highTemplate.Criteria[criteria.Key];
                List<RequirementCriterion> interpolatedCriteria = new List<RequirementCriterion>();

                for (int i = 0; i < lowCriteria.Count; i++)
                {
                    var param2Low = lowCriteria[i].Param2;
                    var param2High = highCriteria[i].Param2;

                    if (ql == lowTemplate.Ql)
                        interpolatedCriteria = lowCriteria;
                    else if (ql == highTemplate.Ql)
                        interpolatedCriteria = highCriteria;
                    else
                    {
                        interpolatedCriteria.Add(new RequirementCriterion
                        {
                            Operator = lowCriteria[i].Operator,
                            Param1 = lowCriteria[i].Param1,
                            Param2 = (int)Math.Round(param2Low + ((float)ql - lowTemplate.Ql) * (param2High - param2Low) / (highTemplate.Ql - lowTemplate.Ql))
                        });
                    }
                }

                Criteria[criteria.Key] = interpolatedCriteria;
            }

            foreach (var modifier in lowTemplate.Modifiers)
            {
                var lowMod = modifier.Value;
                var highMod = highTemplate.Modifiers[modifier.Key];
                Dictionary<Stat, int> interpolatedStats = new Dictionary<Stat, int>();

                foreach (var stat in lowMod)
                {
                    var lowValue = stat.Value;
                    var highValue = highMod[stat.Key];

                    if (ql == lowTemplate.Ql)
                        interpolatedStats = lowMod;
                    else if (ql == highTemplate.Ql)
                        interpolatedStats = highMod;
                    else
                        interpolatedStats.Add(stat.Key, (int)Math.Round(lowValue + ((float)ql - lowTemplate.Ql) * (highValue - lowValue) / (highTemplate.Ql - lowTemplate.Ql)));

                    Modifiers[modifier.Key] = interpolatedStats;
                }
            }
        }
        public void Use(SimpleChar target = null)
        {
            if (target == null)
                target = DynelManager.LocalPlayer;

            Targeting.SetTarget(target);

            Client.Send(new GenericCmdMessage()
            {
                Action = GenericCmdAction.Use,
                User = DynelManager.LocalPlayer.Identity,
                Target = Slot,
                Count = 1
            });
        }

        public static void Use(Identity slot)
        {
            Client.Send(new GenericCmdMessage()
            {
                Action = GenericCmdAction.Use,
                User = DynelManager.LocalPlayer.Identity,
                Target = slot
            });
        }

        public void Equip(EquipSlot equipSlot)
        {
            MoveToInventory((int)equipSlot);
        }

        public void MoveToInventory(int targetSlot = 0x6F)
        {
            MoveItemToInventory(Slot, targetSlot);
        }

        public void MoveToBank()
        {
            Logger.Information($"MoveToBank Slot {Slot}");
            MoveToContainer(new Identity(IdentityType.Bank, Client.LocalDynelId));
        }

        public void MoveToContainer(Container target)
        {
            ContainerAddItem(Slot, target.Identity);
        }

        public void MoveToContainer(Identity target)
        {
            ContainerAddItem(Slot, target);
        }

        public static void MoveItemToInventory(Identity sourceSlot, int slot = 0x6F)
        {
            Client.Send(new ClientMoveItemToInventory()
            {
                SourceContainer = sourceSlot,
                Slot = slot
            });
        }

        public static void ContainerAddItem(Identity sourceSlot, Identity targetIdentity)
        {
            Client.Send(new ClientContainerAddItem()
            {
                Source = sourceSlot,
                Target = targetIdentity
            });
        }

        public void Delete()
        {
            Client.Send(new CharacterActionMessage()
            {
                Action = CharacterActionType.DeleteItem,
                Target = Slot,
            });
        }

        public bool MeetsUseReqs(SimpleChar target = null, bool ignoreTargetReqs = false)
        {
            if (!Criteria.TryGetValue(ItemActionInfo.UseCriteria, out List<RequirementCriterion> useCriteria))
                return false;

            return new ReqChecker(useCriteria).MeetsReqs(target, ignoreTargetReqs);
        }
    }
}