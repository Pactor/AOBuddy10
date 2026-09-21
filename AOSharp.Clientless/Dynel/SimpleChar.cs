using System.Collections.Generic;
using System.Linq;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using static SmokeLounge.AOtomation.Messaging.GameData.SimpleCharInfo;

namespace AOSharp.Clientless
{
    public class SimpleChar : Dynel
    {
        public readonly Appearance Appearance;
        public CharacterFlags Flags => GetStat<CharacterFlags>(Stat.Flags);
        public int Expansions => GetStat(Stat.Expansion);
        public int AccountFlags => GetStat(Stat.AccountFlags);
        public int Level => GetStat(Stat.Level);
        public int OrgId => GetStat(Stat.Clan);
        public int TeamId => GetStat(Stat.Team);
        public Side Side => (Side)GetStat(Stat.Side);
        public string OrgName;
        public bool IsInOrg => OrgId != 0;
        public readonly bool IsNpc;
        public readonly MovementComponent MovementComponent;

        public bool IsAttacking => FightingIdentity.HasValue;
        private Identity? _fightingIdentity;
        public Identity? FightingIdentity
        {
            get => _fightingIdentity;
            set
            {
                SetStat(Stat.CharState, value == null ? 0 : 10);

                _fightingIdentity = value;
            }
        }
        public SimpleChar FightingTarget => FightingIdentity.HasValue && DynelManager.Find(FightingIdentity.Value, out SimpleChar target) ? target : null;


        public IReadOnlyList<Buff> Buffs => _buffs;

        private readonly List<Buff> _buffs = new List<Buff>();

        // Permanent, always-on stat bonuses that are NOT carried in the stat packets the
        // client receives (PERK and RESEARCH bonuses). Retail folds these into the
        // character's base ability/skill values; the clientless stream does not, so the bot
        // under-reads permanent stats (e.g. naked Strength 111 here vs 119 in retail) until
        // this layer adds them back. Applied INSIDE GetStat at the base level so that trickle
        // (which reads GetStat(ability)) recomputes correctly off the boosted abilities.
        // Populated by the plugin via SetPermanentBonuses; empty = no change (0 if absent).
        private Dictionary<Stat, int> _permanentBonuses = new Dictionary<Stat, int>();

        /// <summary>The permanent PERK/RESEARCH bonuses currently folded into GetStat.</summary>
        public IReadOnlyDictionary<Stat, int> PermanentBonuses => _permanentBonuses;

        /// <summary>
        /// Replace the permanent (PERK/RESEARCH) stat bonus layer. Additive and always-on:
        /// each entry adds to that stat's computed value at the base level, so ability boosts
        /// flow through trickle to dependent skills. Pass a fresh map to update; null clears.
        /// </summary>
        public void SetPermanentBonuses(Dictionary<Stat, int> bonuses)
        {
            _permanentBonuses = bonuses ?? new Dictionary<Stat, int>();
        }

        private int GetPermanentBonus(Stat stat) => _permanentBonuses.TryGetValue(stat, out int v) ? v : 0;

        public SimpleChar(SimpleCharFullUpdateMessage simpleCharMsg) : base(simpleCharMsg.Identity, simpleCharMsg.Position, simpleCharMsg.Heading)
        {
            SetStat(Stat.Flags, (int)simpleCharMsg.CharacterFlags);
            SetStat(Stat.AccountFlags, simpleCharMsg.AccountFlags);
            SetStat(Stat.Expansion, simpleCharMsg.Expansions);
            SetStat(Stat.Level, simpleCharMsg.Level);
            SetStat(Stat.Health, simpleCharMsg.Health - simpleCharMsg.HealthDamage);
            SetStat(Stat.MaxHealth, simpleCharMsg.Health);

            Name = simpleCharMsg.Name;
            IsNpc = simpleCharMsg.Flags.HasFlag(SimpleCharFullUpdateFlags.IsNpc);

            FightingIdentity = simpleCharMsg.FightingTarget;

            Appearance = new Appearance
            {
                Breed = simpleCharMsg.Appearance.Breed,
                Fatness = simpleCharMsg.Appearance.Fatness,
                Gender = simpleCharMsg.Appearance.Gender,
                HeadMesh = simpleCharMsg.HeadMesh.HasValue ? simpleCharMsg.HeadMesh.Value : 0,
                Side = simpleCharMsg.Appearance.Side,
            };

            SetStat(Stat.Side, (int)simpleCharMsg.Appearance.Side);

            if (simpleCharMsg.CharacterInfo is PlayerInfo playerInfo)
            {
                SetStat(Stat.CurrentNano, (int)playerInfo.CurrentNano);

                SetStat(Stat.Agility, playerInfo.AgilityBase);
                SetStat(Stat.Sense, playerInfo.SenseBase);
                SetStat(Stat.Strength, playerInfo.StrengthBase);
                SetStat(Stat.Stamina, playerInfo.StaminaBase);
                SetStat(Stat.Intelligence, playerInfo.IntelligenceBase);
                SetStat(Stat.Psychic, playerInfo.PsychicBase);

                SetStat(Stat.Team, playerInfo.Team);

                if (simpleCharMsg.Flags.HasFlag(SimpleCharFullUpdateFlags.HasOrgName))
                {
                    SetStat(Stat.Clan, playerInfo.OrgId);
                    OrgName = playerInfo.OrgName;

                    if (this is LocalPlayer)
                    {
                        Client.OrgId = OrgId;
                        Client.OrgName = OrgName;
                    }
                }
                else
                {
                    SetStat(Stat.Clan, 0);
                }
            }

            MovementComponent = new MovementComponent
            {
                Position = simpleCharMsg.Position,
                Heading = simpleCharMsg.Heading,
            };

            for (int i = 0; i < simpleCharMsg.ActiveNanos.Length; i++)
            {
                var activeNano = simpleCharMsg.ActiveNanos[i];
                var buff = new Buff(activeNano.Identity.Instance);
                buff.Cooldown.SetExpireTime(simpleCharMsg.ActiveNanos[i].Time2 / 100f);
                _buffs.Add(buff);
            }
        }

        public override bool TryGetStat(Stat stat, out int value)
        {
            if (!base.TryGetStat(stat, out value))
                return false;

            value = GetStat(stat);
            return true;
        }

        public override int GetStat(Stat stat)
        {
            int buffedValue = _buffs.Where(x => x.NanoItem != null && x.NanoItem.Modifiers.TryGetValue(SpellListType.Use, out var useMod) && useMod.ContainsKey(stat)).Sum(x => x.NanoItem.Modifiers[SpellListType.Use][stat]);

            // Permanent PERK/RESEARCH bonus is added at the base level (before trickle) so that
            // a boost to an ability propagates through GetTrickle into every dependent skill.
            return base.GetStat(stat) + GetPermanentBonus(stat) + GetTrickle(stat) + buffedValue;
        }

        public int GetTrickle(Stat stat)
        {
            if (!SkillTrickleData.SkillTrickle.TryGetValue(stat, out var trickle))
                return 0;

            return (int)((GetStat(Stat.Strength) * trickle[0] + GetStat(Stat.Agility) * trickle[1] + GetStat(Stat.Stamina) * trickle[2] + GetStat(Stat.Intelligence) * trickle[3] + GetStat(Stat.Sense) * trickle[4] + GetStat(Stat.Psychic) * trickle[5]) / 4);
        }

        internal void RegisterBuff(Buff buff) => _buffs.Add(buff);

        internal void RemoveBuff(int buffId) => _buffs.Remove(buffId);

        internal virtual void OnTeamLeft()
        {
            SetStat(Stat.Team, 0);
        }
    }

    public class Appearance
    {
        public Breed Breed { get; internal set; }
        public Fatness Fatness { get; internal set; }
        public Gender Gender { get; internal set; }
        public int HeadMesh { get; internal set; }
        public Side Side { get; internal set; }
    }
}
