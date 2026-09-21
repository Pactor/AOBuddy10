using AOSharp.Common.GameData;
using AOSharp.Common.Helpers;
using AOSharp.Common.Unmanaged.DataTypes;
using AOSharp.Common.Unmanaged.Imports;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace AOSharp.Clientless
{
    internal class ReqChecker
    {
        private List<RequirementCriterion> _criteria;
        private (CriteriaSource SourceType, SimpleChar Char) _criteriaSource;

        private bool[] _state;
        private byte prevReqsMet;

        public ReqChecker(List<RequirementCriterion> criteria)
        {
            _criteria = criteria;
        }

        public bool MeetsReqs(SimpleChar target = null, bool ignoreTargetReqs = false)
        {
            //Set starting values
            _criteriaSource = (CriteriaSource.Self, DynelManager.LocalPlayer);
            _state = new bool[12];
            prevReqsMet = 0;

            //Default the end result to true
            _state[0] = true;

            foreach (RequirementCriterion criterion in _criteria)
            {
                bool metReq = false;

                if (GetNextCriteriaSource(criterion.Operator, out CriteriaSource newCriteriaSource))
                {
                    if (newCriteriaSource == CriteriaSource.User)
                        _criteriaSource = (CriteriaSource.User, DynelManager.LocalPlayer);
                    else if (newCriteriaSource == CriteriaSource.Target)
                        _criteriaSource = (CriteriaSource.Target, target);
                    else if (newCriteriaSource == CriteriaSource.FightingTarget)
                        //      _criteriaSource = (CriteriaSource.FightingTarget, DynelManager.LocalPlayer.FightingTarget);

                        continue;
                }

                if (criterion.Operator == UseCriteriaOperator.And)
                {
                    if (prevReqsMet < 2)
                        return false;

                    bool lastResult = _state[--prevReqsMet];
                    bool result = _state[--prevReqsMet];

                    //We can early exit on AND 
                    if (!result || !lastResult)
                        return false;

                    metReq = true;
                }
                else if (criterion.Operator == UseCriteriaOperator.Or)
                {
                    if (prevReqsMet < 2)
                        return false;

                    bool lastResult = _state[--prevReqsMet];
                    bool result = _state[--prevReqsMet];

                    metReq = result || lastResult;
                }
                else if (criterion.Operator == UseCriteriaOperator.Not)
                {
                    if (prevReqsMet < 1)
                        return false;

                    metReq = !_state[--prevReqsMet];
                }
                else
                {
                    metReq = MeetsReq(criterion, target, ignoreTargetReqs);
                }

                _state[prevReqsMet++] = metReq;
            }

            return _state[0];
        }

        private bool MeetsReq(RequirementCriterion criterion, SimpleChar target = null, bool ignoreTargetReqs = false)
        {
            bool metReq = false;

            if (_criteriaSource.SourceType == CriteriaSource.Target && ignoreTargetReqs)
            {
                metReq = true;
            }
            else if (_criteriaSource.Char == null)
            {
                metReq = false;
            }
            else
            {
                switch (criterion.Operator)
                {
                    case UseCriteriaOperator.EqualTo:
                    case UseCriteriaOperator.Unequal:
                    case UseCriteriaOperator.LessThan:
                    case UseCriteriaOperator.GreaterThan:
                    case UseCriteriaOperator.BitAnd:
                    case UseCriteriaOperator.NotBitAnd:
                        metReq = CheckStat(criterion, target);
                        break;
                    case UseCriteriaOperator.HasWornItem:
                        metReq = Inventory.Items.Find(criterion.Param2, out Item item) &&
                                    (item.Slot.Type == IdentityType.ArmorPage ||
                                    item.Slot.Type == IdentityType.ImplantPage ||
                                    item.Slot.Type == IdentityType.WeaponPage);
                        break;
                    case UseCriteriaOperator.IsNpc:
                        if (criterion.Param2 == 3)
                            metReq = target.IsNpc;
                        break;
                    case UseCriteriaOperator.HasRunningNano:
                        metReq = _criteriaSource.Char.Buffs.Any(x => x.Id == criterion.Param2);
                        break;
                    case UseCriteriaOperator.HasNotRunningNano:
                        metReq = _criteriaSource.Char.Buffs.All(x => x.Id != criterion.Param2);
                        break;
                    //case UseCriteriaOperator.HasPerk:
                    //    metReq = N3EngineClientAnarchy_t.HasPerk(N3Engine_t.GetInstance(), criterion.Param2);
                    //    break;
                    //case UseCriteriaOperator.HasNotPerk:
                    //    metReq = !N3EngineClientAnarchy_t.HasPerk(N3Engine_t.GetInstance(), criterion.Param2);
                    //    break;
                    case UseCriteriaOperator.IsPerkUnlocked:
                        metReq = true;
                        break;
                    case UseCriteriaOperator.HasRunningNanoLine:
                        metReq = _criteriaSource.Char.Buffs.Contains((NanoLine)criterion.Param2);
                        break;
                    case UseCriteriaOperator.HasNotRunningNanoLine:
                        metReq = !_criteriaSource.Char.Buffs.Contains((NanoLine)criterion.Param2);
                        break;
                    case UseCriteriaOperator.HasNcuFor:
                        //TODO: check against actual nano program NCU cost
                        metReq = _criteriaSource.Char.GetStat(Stat.MaxNCU) - _criteriaSource.Char.GetStat(Stat.CurrentNCU) > 0;
                        break;
                    case UseCriteriaOperator.HasFreeSlots:
                        //Param2 is amount of slots
                        metReq = Inventory.NumFreeSlots >= criterion.Param2;
                        break;
                    //case UseCriteriaOperator.TestNumPets:
                    //    Pet[] pets = DynelManager.LocalPlayer.Pets;
                    //    if (pets.Any(x => x.Type == PetType.Unknown))
                    //    {
                    //        metReq = false;
                    //        break;
                    //    }

                    //    PetType type = PetType.Unknown;
                    //    if (criterion.Param2 == 1)
                    //        type = PetType.Attack;
                    //    else if (criterion.Param2 == 1001)
                    //        type = PetType.Heal;
                    //    else if (criterion.Param2 == 2001)
                    //        type = PetType.Support;
                    //    else if (criterion.Param2 == 4001)
                    //        type = PetType.Social;

                    //    metReq = !pets.Any(x => x.Type == type);
                    //    break;
                    case UseCriteriaOperator.HasWieldedItem:
                        if (_criteriaSource.SourceType == CriteriaSource.Target)
                        {
                            metReq = true;
                        }
                        else
                        {
                            metReq = Inventory.Items.Any(i =>
                                (i.Id == criterion.Param2 || i.HighId == criterion.Param2) &&
                                (i.Slot.Instance >= (int)EquipSlot.Weap_Hud1 &&
                                    i.Slot.Instance <= (int)EquipSlot.Imp_Feet));
                        }
                        break;
                    case UseCriteriaOperator.IsSameAs:
                        //Not sure what these parmas correlate to but I don't know any other item that uses this operator either.
                        if (criterion.Param1 == 1 && criterion.Param2 == 3)
                        {
                            if (target == null)
                                metReq = false;
                            else
                                metReq = target.Identity == DynelManager.LocalPlayer.Identity;
                        }
                        break;
                    //case UseCriteriaOperator.AlliesNotInCombat:
                    //    if (Team.IsInTeam && Team.Members.Contains(_criteriaSource.Char.Identity))
                    //    {
                    //        metReq = !Team.IsInCombat();
                    //    }
                    //    else
                    //    {
                    //        metReq = !_criteriaSource.Char.IsAttacking && !DynelManager.Characters.Any(x => x.FightingTarget != null && (x.FightingTarget.Identity == _criteriaSource.Char.Identity || x.FightingTarget.Identity == DynelManager.LocalPlayer.Identity));
                    //    }
                    //    break;
                    //case UseCriteriaOperator.IsOwnPet:
                    //    metReq = DynelManager.LocalPlayer.Pets.Contains(_criteriaSource.Char.Identity);

                    //    break;
                    default:
                        //Chat.WriteLine($"Unknown Criteria -- Param1: {param1} - Param2: {criterion.Param2} - Op: {op}");
                        metReq = false;
                        break;
                }
            }

            return metReq;
        }

        private bool CheckStat(RequirementCriterion criterion, SimpleChar target)
        {
            if ((Stat)criterion.Param1 == Stat.TargetFacing)
            {
                //SimpleChar fightingTarget;
                //if ((fightingTarget = DynelManager.LocalPlayer.FightingTarget) != null)
                //{
                //    bool isFacing = fightingTarget.IsFacing(DynelManager.LocalPlayer);
                //    return (criterion.Param2 == 1) ? !isFacing : isFacing;
                //}
            }
            else if ((Stat)criterion.Param1 == Stat.MonsterData) // Ignore this check because something funky is going on with it.
            {
                return true;
            }
            else if ((Stat)criterion.Param1 == Stat.SelectedTargetType)
            {
                return target != null ? target is PlayerChar : true;
            }
            else
            {
                // TryGetStat, not GetStat: GetStat throws KeyNotFound for a stat the char doesn't
                // have, which aborted whole requirement checks. Missing = 0 here.
                _criteriaSource.Char.TryGetStat((Stat)criterion.Param1, out int stat);

                // AO requirement comparisons are INCLUSIVE: "GreaterThan"/"LessThan" pass when the
                // skill is equal to the value too (equal-or-greater / equal-or-less), matching retail.
                if (criterion.Operator == UseCriteriaOperator.EqualTo)
                    return (stat == criterion.Param2);
                if (criterion.Operator == UseCriteriaOperator.Unequal)
                    return (stat != criterion.Param2);
                if (criterion.Operator == UseCriteriaOperator.LessThan)
                    return (stat <= criterion.Param2);
                if (criterion.Operator == UseCriteriaOperator.GreaterThan)
                    return (stat >= criterion.Param2);
                if (criterion.Operator == UseCriteriaOperator.BitAnd)
                    return (stat & criterion.Param2) == criterion.Param2;
                if (criterion.Operator == UseCriteriaOperator.NotBitAnd)
                    return (stat & criterion.Param2) != criterion.Param2;

                //Chat.WriteLine($"Unknown Criteria -- Param1: {param1} - Param2: {param2} - Op: {op}");
            }

            return false;
        }

        private bool GetNextCriteriaSource(UseCriteriaOperator op, out CriteriaSource criteriaSource)
        {
            criteriaSource = CriteriaSource.Self;

            if (op == UseCriteriaOperator.OnUser)
                criteriaSource = CriteriaSource.User;
            else if (op == UseCriteriaOperator.OnTarget)
                criteriaSource = CriteriaSource.Target;
            else if (op == UseCriteriaOperator.OnFightingTarget)
                criteriaSource = CriteriaSource.FightingTarget;

            return op == UseCriteriaOperator.OnUser || op == UseCriteriaOperator.OnTarget || op == UseCriteriaOperator.OnFightingTarget;
        }

        private enum CriteriaSource
        {
            FightingTarget,
            Target,
            Self,
            User
        }
    }

    public class RequirementCriterion
    {
        public int Param1;
        public int Param2;
        public UseCriteriaOperator Operator;
    }
}
