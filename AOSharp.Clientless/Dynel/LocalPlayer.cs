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
    public class LocalPlayer : PlayerChar
    {
        public bool IsCasting { get; internal set; } = false;

        public new readonly LocalPlayerMovementComponent MovementComponent;

        public IReadOnlyDictionary<Stat, Cooldown> Cooldowns => _cooldowns;

        private readonly Dictionary<Stat, Cooldown> _cooldowns = new Dictionary<Stat, Cooldown>();

        // Weapon special attacks the server has told us this character can use, learned from the wire
        // (SpecialUsed / SpecialAvailable) so it works for ANY class/weapon with no hardcoding.
        public IReadOnlyCollection<Stat> KnownSpecials => _knownSpecials;

        private readonly HashSet<Stat> _knownSpecials = new HashSet<Stat>();

        public int[] SpellList;

        /// <summary>
        /// The raw perk table from the last FullCharacter packet, exposed for inspection.
        /// NOTE: its semantics are unresolved — captures show it empty or carrying only an
        /// id with no trained level, so it is NOT a reliable source for perk stat bonuses
        /// (the plugin uses a configured perk list instead). Kept for future investigation.
        /// </summary>
        public FullCharacterMessage.Perk[] Perks;

        public LocalPlayer(SimpleCharFullUpdateMessage simpleCharMsg) : base(simpleCharMsg)
        {
            MovementComponent = new LocalPlayerMovementComponent
            {
                Position = simpleCharMsg.Position,
                Heading = simpleCharMsg.Heading,
            };
        }

        public void Attack(SimpleChar target) => Attack(target.Identity);

        public void Attack(Identity target)
        {
            Client.Send(new AttackMessage
            {
                Target = target
            });
        }

        public void StopAttack()
        {
            Client.Send(new StopFightMessage());
        }

        // Fire a weapon SPECIAL attack (Fast Attack, Brawl, Fling Shot, Burst, Full Auto, Aimed Shot, Sneak
        // Attack, Dimach …). The special is identified by its skill Stat; the equipped weapon + skills decide
        // which are valid. This is a SEPARATE message from Attack and does NOT reset the main weapon swing
        // timer, so it's safe to fire alongside auto-attack. We optimistically register a short cooldown so we
        // don't re-send before the server's SpecialUsed echo sets the real recharge.
        public void PerformSpecialAttack(Identity target, Stat special)
        {
            Client.Send(new CharSecSpecAttackMessage
            {
                Target = target,
                Stat = special
            });
            RegisterCooldown(special, 2);
        }

        // A learned special is ready when it is not currently on cooldown.
        public bool IsSpecialReady(Stat special)
        {
            if (!_cooldowns.TryGetValue(special, out Cooldown cooldown))
                return true;
            return cooldown.RemainingTime <= 0;
        }

        // ---- PETS ----------------------------------------------------------------
        // Our pets are simply the NPCs the server says WE own (NpcChar.Owner == our identity). No AddPet/
        // RemovePet handling is needed (those messages don't deserialise in this SDK anyway) — a summoned pet
        // appears in this list, and a dead or dismissed one drops out, so this doubles as re-summon detection.
        public IEnumerable<NpcChar> Pets => DynelManager.Npcs.Where(n => n.Owner.HasValue && n.Owner.Value == Identity);

        // Send a pet command to ALL our pets. Attack = attack whatever the master is attacking (names no
        // target — so set your target first), Follow/Behind/Guard/Wait reposition them, Terminate dismisses.
        public void CommandPets(PetCommand command) => CommandPets(command, Pets.Select(p => p.Identity));

        // Send a pet command to specific pets by identity.
        public void CommandPets(PetCommand command, IEnumerable<Identity> pets)
        {
            Identity[] ids = pets.ToArray();
            if (ids.Length == 0)
                return;

            Client.Send(new PetCommandMessage
            {
                Command = command,
                Pets = ids.Select(id => new PetBase(id)).ToArray()
            });
        }


        internal bool SetCastState(bool state) => IsCasting = state;

        public void Cast(int nanoId)
        {
            Targeting.SetTarget(Identity);
            CastNano(Identity, nanoId);
        }

        public void Cast(SimpleChar target, int nanoId)
        {
            Targeting.SetTarget(target);
            CastNano(target.Identity, nanoId);
        }

        public void Cast(Identity target, int nanoId)
        {
            Targeting.SetTarget(target);
            CastNano(target, nanoId);
        }

        private void CastNano(Identity target, int nanoId)
        {
            Client.Send(new CharacterActionMessage()
            {
                Action = CharacterActionType.CastNano,
                Target = target,
                Parameter1 = (int)IdentityType.NanoProgram,
                Parameter2 = nanoId
            });
        }

        internal void ApplyFullCharacter(FullCharacterMessage fullChar)
        {
            // The full-character packet carries FOUR stat arrays. Only Stats1/Stats2 were being
            // applied, so skills (First Aid, Treatment, weapon/nano skills — in Stats3/Stats4)
            // never entered the stat dict and GetStat threw for them. Apply all four.
            foreach (var stat in fullChar.Stats1)
                SetStat((Stat)stat.Value1, (int)stat.Value2);

            foreach (var stat in fullChar.Stats2)
                SetStat((Stat)stat.Value1, (int)stat.Value2);

            if (fullChar.Stats3 != null)
                foreach (var stat in fullChar.Stats3)
                    SetStat((Stat)stat.Value1, (int)stat.Value2);

            if (fullChar.Stats4 != null)
                foreach (var stat in fullChar.Stats4)
                    SetStat((Stat)stat.Value1, (int)stat.Value2);

            SpellList = fullChar.UploadedNanoIds;
            Perks = fullChar.Perks;
        }

        public override int GetStat(Stat stat)
        {
            if (Inventory.Items == null)
                return 0;

            int equippedValue = Inventory.Items.Where(x => x.Slot.Instance <= (int)EquipSlot.Imp_Feet && x.Modifiers.TryGetValue(SpellListType.Wear, out var wearModifiers) && wearModifiers.ContainsKey(stat)).Sum(x => x.Modifiers[SpellListType.Wear][stat]);

            return base.GetStat(stat) + equippedValue;
        }

        internal bool TryGetCooldown(Stat stat, out Cooldown cooldown) => _cooldowns.TryGetValue(stat, out cooldown);
      
        internal bool RemoveCooldown(Stat stat)
        {
            _knownSpecials.Add(stat);   // the server announced this special as available -> we have it
            return _cooldowns.Remove(stat);
        }

        internal void RegisterCooldown(Stat stat, int timeInSeconds)
        {
            _knownSpecials.Add(stat);   // the server told us this special was used -> we have it
            if (!_cooldowns.TryGetValue(stat, out Cooldown cooldown))
            {
                cooldown = new Cooldown();
                _cooldowns.Add(stat, cooldown);
            }

            cooldown.SetExpireTime(timeInSeconds);
        }

        internal override void OnTeamLeft()
        {
            Client.Chat?.RemoveChannelId(TeamId);

            base.OnTeamLeft();
        }
    }
}