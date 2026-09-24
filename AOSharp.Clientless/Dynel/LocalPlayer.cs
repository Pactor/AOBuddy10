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

        // PET OWNERSHIP BY SUMMON. The pet-master wire bit (SimpleCharFullUpdate Flags2 0x4) is UNRELIABLE —
        // some summoned pets never send it (wire-proven in scfuwire.log), so they were never recognised as
        // ours (me.Pets empty -> pet never commanded, endless resummon). Robust rule the owner asked for: "if
        // he summoned it, it's his." When we cast a pet-summon nano we open this claim window; DynelManager
        // marks the fresh NPC that spawns next to us during it as owned. Monotonic ms clock (TickCount64).
        public long ExpectingPetUntilMs { get; private set; }

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

            // If this cast summons a pet, open the ownership-claim window (see ExpectingPetUntilMs). The pet
            // appears a few seconds later (after the cast), so allow a generous window.
            if (IsPetSummonNano(nanoId))
                ExpectingPetUntilMs = Environment.TickCount64 + 12000;
        }

        private static bool IsPetSummonNano(int nanoId)
        {
            if (!ItemData.Find(nanoId, out NanoItem ni) || ni == null) return false;
            return ni.NanoLine == NanoLine.AttackPets || ni.NanoLine == NanoLine.HealPets || ni.NanoLine == NanoLine.SupportPets;
        }

        // A nano was uploaded/learned mid-session (server SpellList message). The FullCharacter only refreshes
        // on login/zone, so without this a just-learned nano (e.g. the MP's first heal pet) wouldn't appear in
        // SpellList until a relog — and the bot would never summon it. Append it so AutoSummons picks it up live.
        internal void AddUploadedNano(int nanoId)
        {
            if (nanoId <= 0) return;
            int[] cur = SpellList ?? new int[0];
            if (Array.IndexOf(cur, nanoId) >= 0) return;
            var next = new int[cur.Length + 1];
            Array.Copy(cur, next, cur.Length);
            next[cur.Length] = nanoId;
            SpellList = next;
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

            // An EMPTY nano list is not the same as having no nanos. The FullCharacter that arrives after a
            // zone carries none, and overwriting a good list with it left the bot knowing nothing it could
            // cast: "summons = [none - no pet nanos known]", "0 learned nanos", and a heal pet it could never
            // replace. Nobody forgets their nanos by walking through a terminal - keep what we have until the
            // server actually sends a list. Same reasoning as the perks guard below, which was already here.
            if (fullChar.UploadedNanoIds != null && fullChar.UploadedNanoIds.Length > 0)
                SpellList = fullChar.UploadedNanoIds;

            // Perks may be null when this came from the corrected fallback reader (the pet-case FullCharacter,
            // whose trailing section it doesn't fully decode). Don't wipe the perks the login FullCharacter set.
            if (fullChar.Perks != null)
                Perks = fullChar.Perks;
        }

        public override int GetStat(Stat stat)
        {
            // No inventory known: no gear bonuses, but still the real stat. Returning 0 here made EVERY stat
            // (MaxHealth, MaxNanoEnergy, ...) read 0 for a whole session after a pets-up login (hb 'hp=? nano=?',
            // log 2026-09-24 08:49).
            if (Inventory.Items == null)
                return base.GetStat(stat);

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