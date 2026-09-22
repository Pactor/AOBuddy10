using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;   // PetCommand

namespace AOBuddy
{
    /// <summary>
    /// PETS — summon/keep-up and command pets, for ANY pet class (MP, Engineer, Crat native pet). It is
    /// class-agnostic on the SDK primitives: our pets are the NPCs we own (<see cref="LocalPlayer.Pets"/>),
    /// commands go via <see cref="LocalPlayer.CommandPets(PetCommand, IEnumerable{Identity})"/>, and
    /// summoning is a normal nano cast.
    ///
    /// WHAT EACH PET IS FOR comes from the server, not from us. Every pet's SimpleCharFullUpdate carries
    /// SimpleNpcInfo.PetType — 10 attack, 11 heal, 12 mezz — and PetToMaster repeats it on attach. The SDK
    /// used to read that byte and throw it away, which is why this file previously had to guess the attack
    /// pet as "whichever pet is nearest the mob". It no longer guesses: see NpcChar.Role.
    /// Verified across roughly a hundred pets in sniffs/ and captures/; in captures/mp_203534_s37.csv the
    /// type 11 pet is seen casting its heal ON the type 10 pet while the type 12 pet debuffs the mob.
    ///
    /// Two facts drive the rest of the design (confirmed with the user, an MP player):
    ///  - Attack and Heal pet commands are TARGET-BASED: the pet acts on the MASTER's current target, so we
    ///    set the target first, then command.
    ///  - A pet that dies just drops out of me.Pets; the summon nano's recharge IS the "resummon timer", so we
    ///    resummon the instant it is castable again.
    ///
    /// This controller NEVER moves the bot (follow owns movement). Commanding a pet DOES move the bot's
    /// target — the caller is responsible for putting the weapon target back on the mob afterwards, which
    /// Main does in one place for every action that retargets.
    /// </summary>
    public class PetController
    {
        private readonly BotContext _ctx;

        private double _resummonAccum;
        private double _petClock;                                            // seconds, for per-nano summon cooldowns
        private readonly Dictionary<int, double> _summonAt = new Dictionary<int, double>();  // nanoId -> last summon time
        private string _attackTaskedRoster = "\0";
        private string _healTaskedRoster = "\0";
        private Identity? _lastAttackTarget;
        private Identity? _lastHealTarget;
        private string _lastRoster = "";
        private string _lastSummonKey = "\0";

        // A pet takes a few seconds to appear and register after its summon casts; re-casting the same summon
        // before then just replaces the pet and burns nano (the have 0/N spam). Don't re-summon the SAME nano
        // within this window — a genuinely dead pet re-summons once it lapses.
        private const double SummonRecastSec = 12.0;

        // Physical split between a melee reach and a ranged reach. Stat.AttackRange is in hundredths of a
        // metre and is set BY THE EQUIPPED WEAPON: melee weapons reach ~1-5 m, ranged weapons ~15 m+. 10 m
        // cleanly separates the two — this is a property of weapons, not a per-character config.
        private const int MeleeAttackRangeMaxCm = 1000;

        // The COMBAT pet-summon nano lines (server-tagged): highest-QL castable in each is the pet to keep up.
        // ORDER MATTERS: AttackPets first, because a ranged master's heal pet is told to heal the ATTACK pet,
        // so the attack pet has to exist before that command means anything.
        // Deliberately NOT SocialPets — that line is vehicles/social (the MP "boogy board"), pure show.
        private static readonly NanoLine[] PetLines =
            { NanoLine.AttackPets, NanoLine.HealPets, NanoLine.SupportPets };

        public PetController(BotContext ctx)
        {
            _ctx = ctx;
        }

        public int PetCount => DynelManager.LocalPlayer?.Pets.Count() ?? 0;

        // ---- The roster, by role ------------------------------------------------

        private static IEnumerable<NpcChar> Pets(LocalPlayer me) =>
            me == null ? Enumerable.Empty<NpcChar>() : me.Pets;

        /// <summary>The fighting pet (PetType 10), or null.</summary>
        public static NpcChar AttackPet(LocalPlayer me) => Pets(me).FirstOrDefault(p => p.Role == PetType.Attack);

        /// <summary>The healing pet (PetType 11), or null.</summary>
        public static NpcChar HealPet(LocalPlayer me) => Pets(me).FirstOrDefault(p => p.Role == PetType.Heal);

        /// <summary>The crowd-control pet (PetType 12), or null.</summary>
        public static NpcChar SupportPet(LocalPlayer me) => Pets(me).FirstOrDefault(p => p.Role == PetType.Support);

        /// <summary>
        /// The pets an Attack command should go to: every combat pet EXCEPT the healer. Sending Attack to
        /// the whole group is what used to re-task the heal pet off healing; per-pet commands are a normal
        /// part of the protocol (PetCommandMessage carries the pet list), so there is no reason to.
        /// </summary>
        private static List<Identity> Attackers(LocalPlayer me) =>
            Pets(me).Where(p => p.Role == PetType.Attack || p.Role == PetType.Support)
                    .Select(p => p.Identity).ToList();

        // DIAGNOSTIC: dump the bot's own identity and every nearby NPC's identity + owner + pet type.
        public void DumpPets(LocalPlayer me)
        {
            if (me == null) { _ctx.Log("PETDBG: no LocalPlayer"); return; }
            _ctx.Log($"PETDBG: me.Identity Type={me.Identity.Type}/{(int)me.Identity.Type} Inst={me.Identity.Instance} | Client.LocalDynelId={Client.LocalDynelId} | Pets.Count={me.Pets.Count()}");
            foreach (NpcChar p in me.Pets)
                _ctx.Log($"PETDBG:   MINE '{p.Name}' id={p.Identity.Instance} petType={p.PetTypeId}({p.Role}) family={p.NpcFamily} d={me.DistanceFrom(p):0}");
            var npcs = DynelManager.Npcs.Where(n => n != null).OrderBy(me.DistanceFrom).Take(20).ToList();
            _ctx.Log($"PETDBG: {DynelManager.Npcs.Count()} npc(s) total; nearest {npcs.Count}:");
            foreach (var n in npcs)
            {
                string owner = n.Owner.HasValue
                    ? $"{n.Owner.Value.Type}/{(int)n.Owner.Value.Type}:{n.Owner.Value.Instance}"
                    : "null";
                bool mine = n.Owner.HasValue && n.Owner.Value == me.Identity;
                _ctx.Log($"PETDBG:   '{n.Name}' id={(int)n.Identity.Type}:{n.Identity.Instance} d={me.DistanceFrom(n):0} owner={owner} petType={n.PetTypeId}({n.Role}) mine={mine}");
            }
        }

        // Log the pet roster (name + id + role) whenever it changes.
        private void LogRoster(LocalPlayer me)
        {
            string roster = string.Join(", ", me.Pets.Select(p => $"{p.Name}#{p.Identity.Instance}:{p.Role}"));
            if (roster != _lastRoster)
            {
                _lastRoster = roster;
                _ctx.Log($"PET: roster = [{(roster.Length == 0 ? "none" : roster)}]");
            }
        }

        // Keep pets up. Resummons the moment a summon nano is castable again (its recharge is the resummon
        // timer). Gated by Config.UsePets and Config.AutoResummon so the owner can turn replacement off per
        // situation.
        public void MaintainPets(LocalPlayer me, double dt, bool supportBusy, Action<int> queueSummon)
        {
            if (me == null || !_ctx.Config.UsePets) return;

            _petClock += dt;
            LogRoster(me);

            if (!_ctx.Config.AutoResummon) return;
            // ONE cast at a time: queue summons through the same cast system as buffs (queueSummon) so they
            // serialize instead of interrupting each other or the buffs. Only add another when nothing is
            // queued or in flight — so each summon completes before the next is attempted.
            if (supportBusy || me.IsCasting) return;

            // Summon nanos: AUTO-detected best-QL-per-pet-line (no config). An explicit override is honoured
            // if set, but by default he picks his own best pets.
            List<int> summons = (_ctx.Config.PetSummonNanoIds != null && _ctx.Config.PetSummonNanoIds.Count > 0)
                ? _ctx.Config.PetSummonNanoIds
                : AutoSummons(me);
            if (summons.Count == 0) return;

            _resummonAccum += dt;
            if (_resummonAccum < 1.0) return;    // check ~1/s, not every frame
            _resummonAccum = 0;

            if (me.IsCasting) return;
            int have = me.Pets.Count();
            if (have >= summons.Count) return;   // full complement is up

            // Short a pet. Summon the first type we haven't summoned within SummonRecastSec — each pet needs a
            // few seconds to appear and register, and re-casting the same summon before then replaces the pet
            // and drains nano. The list is attack-first, so the attack pet is always re-established first.
            foreach (int nanoId in summons)
            {
                if (_summonAt.TryGetValue(nanoId, out double last) && _petClock - last < SummonRecastSec)
                    continue;
                _summonAt[nanoId] = _petClock;
                queueSummon(nanoId);   // serialized with buffs by the shared cast queue
                _ctx.Log($"PET: summon — queued nano {nanoId} (have {have}/{summons.Count}).");
                return;
            }
        }

        // The best summon nano in each pet line the character KNOWS: highest StackingOrder then QL that he can
        // actually cast (NanoItem.MeetsUseReqs — the same castability check the auto-buff uses). No config, no
        // hardcoding — auto-upgrades as he trains and uploads better pet nanos.
        public List<int> AutoSummons(LocalPlayer me)
        {
            // Per pet line, pick the highest-StackingOrder (= best) summon nano. Prefer one MeetsUseReqs says
            // we can cast, BUT that check UNDER-reports for summon nanos (reads False even when buffed and able),
            // so if a line has none it calls castable, fall back to the best overall and let the SERVER decide.
            var bestCastId = new Dictionary<NanoLine, int>(); var bestCastRank = new Dictionary<NanoLine, long>();
            var bestAnyId = new Dictionary<NanoLine, int>();  var bestAnyRank = new Dictionary<NanoLine, long>();
            int[] spells = me == null ? null : me.SpellList;
            if (spells != null)
            {
                foreach (int id in spells)
                {
                    if (!ItemData.Find(id, out NanoItem ni) || ni == null) continue;
                    if (Array.IndexOf(PetLines, ni.NanoLine) < 0) continue;
                    long rank = ni.StackingOrder & 0xFFFFF;
                    if (!bestAnyRank.TryGetValue(ni.NanoLine, out long a) || rank > a) { bestAnyRank[ni.NanoLine] = rank; bestAnyId[ni.NanoLine] = id; }
                    bool castable; try { castable = ni.MeetsUseReqs(me, false); } catch { castable = false; }
                    if (castable && (!bestCastRank.TryGetValue(ni.NanoLine, out long c) || rank > c)) { bestCastRank[ni.NanoLine] = rank; bestCastId[ni.NanoLine] = id; }
                }
            }

            var result = new List<int>();
            var dbg = new List<string>();
            foreach (NanoLine line in PetLines)
            {
                if (bestCastId.TryGetValue(line, out int cid)) { result.Add(cid); dbg.Add($"{line}:{cid}"); }
                else if (bestAnyId.TryGetValue(line, out int aid)) { result.Add(aid); dbg.Add($"{line}:{aid}(server-decides)"); }
            }
            string key = string.Join(",", dbg);
            if (key != _lastSummonKey) { _lastSummonKey = key; _ctx.Log($"PET: summons = [{(key.Length == 0 ? "none — no pet nanos known" : key)}]"); }
            return result;
        }

        // In combat: point the ATTACK (and mezz) pets at the owner's target. Target-based — set our target to
        // the mob, then CommandPets(Attack) naming exactly those pets, so the heal pet is left healing.
        // Issue it ONCE PER MOB, like the bot's own Attack-once-per-target: re-asserting just resets the pet's
        // swing timer for no gain. Re-issue only when the TARGET changes (a new mob) or the attacker roster
        // changes (a pet died and was resummoned mid-fight and needs tasking onto the current mob).
        // Returns true if it moved the bot's target, so the caller can put it back on the mob.
        public bool EngageTarget(LocalPlayer me, SimpleChar target, double dt)
        {
            if (me == null || target == null || !_ctx.Config.UsePets) return false;
            List<Identity> attackers = Attackers(me);
            if (attackers.Count == 0) return false;

            string rosterSig = string.Join(",", attackers.Select(i => i.Instance).OrderBy(x => x));
            if (_lastAttackTarget == target.Identity && rosterSig == _attackTaskedRoster) return false;
            _lastAttackTarget = target.Identity;
            _attackTaskedRoster = rosterSig;

            Targeting.SetTarget(target.Identity);
            me.CommandPets(PetCommand.Attack, attackers);
            _ctx.Log($"PET: attack '{target.Name}' ({attackers.Count} attacker(s), heal pet left alone).");
            return true;
        }

        // AUTO heal-pet tasking. The heal pet is TARGET-BASED (set target, then Heal), and we now address it
        // by identity so no other pet is re-tasked. WHO it should heal is read from the EQUIPPED WEAPON:
        //   - MELEE master: he stands in the fray taking hits, so the heal pet heals HIM.
        //   - RANGED master: he hangs back and the attack pet tanks, so it heals the ATTACK PET — and until
        //     that attack pet exists there is nothing to name, so no command is sent at all.
        // Flips automatically the instant he swaps a sword for a bow.
        // Returns true if it moved the bot's target, so the caller can put it back on the mob.
        public bool MaintainHealPet(LocalPlayer me, SimpleChar target, double dt)
        {
            if (me == null || !_ctx.Config.UsePets) return false;

            NpcChar healer = HealPet(me);
            if (healer == null) return false;          // no heal pet up — nothing to task

            Identity? healTarget = HealPetTarget(me);
            if (!healTarget.HasValue) return false;    // ranged with no attack pet yet — the command would be ignored

            // Task the heal pet ONCE PER SUMMON — once told who to heal, it keeps healing that target. So
            // re-task only when the healer itself changes (died / was resummoned) or the heal TARGET changes
            // (a melee<->ranged weapon swap, or the attack pet was replaced), never on a timer.
            string rosterSig = healer.Identity.Instance + ">" + healTarget.Value.Instance;
            if (rosterSig == _healTaskedRoster) return false;
            _healTaskedRoster = rosterSig;
            _lastHealTarget = healTarget;

            Targeting.SetTarget(healTarget.Value);
            me.CommandPets(PetCommand.Heal, new[] { healer.Identity });
            _ctx.Log($"PET: heal '{healer.Name}' -> #{healTarget.Value.Instance} ({(IsMeleeLoadout(me) ? "melee: self" : "ranged: attack pet")}).");
            return true;
        }

        // Melee vs ranged straight from the equipped weapon's attack range — no class assumption, no config.
        // Defaults to melee (heal self) when the range stat isn't known yet: the safe pick for an in-the-fray
        // master, and it needs no attack pet to be meaningful.
        public static bool IsMeleeLoadout(LocalPlayer me)
        {
            return me == null || !me.TryGetStat(Stat.AttackRange, out int cm) || cm <= 0 || cm <= MeleeAttackRangeMaxCm;
        }

        // Who the heal pet keeps alive. Melee -> the master. Ranged -> the attack pet, identified by the
        // server's own pet type rather than by which pet happens to be standing nearest the mob. Null when
        // ranged and no attack pet is up: the heal command needs a target to name.
        private Identity? HealPetTarget(LocalPlayer me)
        {
            if (IsMeleeLoadout(me)) return me.Identity;
            NpcChar tank = AttackPet(me);
            return tank != null ? (Identity?)tank.Identity : null;
        }

        // Manual, target-based heal: point the heal pet at a target (the 'pethealme' command).
        public void HealTarget(LocalPlayer me, Identity target)
        {
            NpcChar healer = HealPet(me);
            if (me == null || healer == null) return;
            Targeting.SetTarget(target);
            me.CommandPets(PetCommand.Heal, new[] { healer.Identity });
            _healTaskedRoster = healer.Identity.Instance + ">" + target.Instance;   // don't fight the manual choice
            _ctx.Log($"PET: heal command on target #{target.Instance}.");
        }

        public void FollowMaster(LocalPlayer me)
        {
            if (me != null && me.Pets.Any()) { me.CommandPets(PetCommand.Follow); _ctx.Log("PET: follow."); }
        }

        public void Dismiss(LocalPlayer me)
        {
            if (me != null && me.Pets.Any()) { me.CommandPets(PetCommand.Terminate); _ctx.Log("PET: terminate all."); }
        }

        public void Reset()
        {
            _lastAttackTarget = null;
            _lastHealTarget = null;
            _attackTaskedRoster = "\0";
            _healTaskedRoster = "\0";
        }
    }
}
