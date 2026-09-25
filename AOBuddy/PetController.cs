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
        private readonly OwnerTracker _owner;

        private double _resummonAccum;
        private double _petClock;                                            // seconds, for per-nano summon cooldowns
        private readonly Dictionary<int, double> _summonAt = new Dictionary<int, double>();  // nanoId -> last summon time
        private string _attackTaskedRoster = "\0";
        private string _healTaskedRoster = "\0";
        private Identity? _lastAttackTarget;
        private Identity? _lastHealTarget;
        private string _lastRoster = "";
        private string _lastSummonKey = "\0";

        // WHICH PETS ARE OURS, per the server. me.Pets scans the NPCs currently in view, so a pet that
        // wanders out of broadcast range reads as gone and comes back minutes later with the SAME instance
        // id - which is exactly what an MP's attack pet did for a whole session while the bot queued a
        // resummon every twelve seconds. AddPet / RemovePet are the server stating outright that a pet is
        // ours or is not, so they own this set; the dynel scan only fills in what a pet IS and where it is.
        private readonly HashSet<int> _owned = new HashSet<int>();
        private readonly Dictionary<int, double> _lastSeen = new Dictionary<int, double>();   // instance -> _petClock

        /// <summary>Server told us a pet is ours (AddPetMessage).</summary>
        public void OnPetAdded(Identity pet)
        {
            if (_owned.Add(pet.Instance))
                _ctx.Log($"PET: server added #{pet.Instance} (now {_owned.Count} owned).");
        }

        /// <summary>Server told us a pet is no longer ours - dead, dismissed, or left behind (RemovePetMessage).</summary>
        public void OnPetRemoved(Identity pet)
        {
            if (_owned.Remove(pet.Instance))
                _ctx.Log($"PET: server removed #{pet.Instance} (now {_owned.Count} owned).");
            _lastSeen.Remove(pet.Instance);
        }

        /// <summary>How many pets the SERVER says we have. Falls back to the dynel scan before the first
        /// AddPet arrives (a pet summoned before we logged in is only known from the scan).</summary>
        public int OwnedCount(LocalPlayer me) => _owned.Count > 0 ? _owned.Count : Pets(me).Count();

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

        public PetController(BotContext ctx, string pluginDir, OwnerTracker owner)
        {
            _ctx = ctx;
            _owner = owner;
            LoadShellNanos(pluginDir);
        }

        // ---- Commands (R3.4: the pet half of Main's old command switch, moved verbatim) ----------
        // parts is the raw command split (parts[0] is the word itself); replies are byte-identical to
        // the old Main case bodies. The toggles write ctx.Config (UsePets/AutoResummon/BuffPets) —
        // the same fields Main's ladder reads; RuntimeState (R4.1) will collect them later.
        public void Command(string[] parts, Action<string> reply)
        {
            string arg = parts.Length > 1 ? parts[1].ToLowerInvariant() : "";
            switch (parts[0].ToLowerInvariant())
            {
                case "pets":
                {
                    _ctx.Config.UsePets = !_ctx.Config.UsePets;
                    LocalPlayer mp = DynelManager.LocalPlayer;
                    reply($"Pets {(_ctx.Config.UsePets ? "ON" : "OFF")}. Up now: {(mp != null ? mp.Pets.Count() : 0)}.");
                    break;
                }
                case "petdbg":
                {
                    DumpPets(DynelManager.LocalPlayer);
                    reply("Dumped pet/owner diagnostics to aobuddy.log (PETDBG: lines).");
                    break;
                }
                case "resummon": _ctx.Config.AutoResummon = !_ctx.Config.AutoResummon; reply($"Auto-resummon {(_ctx.Config.AutoResummon ? "ON" : "OFF")}."); break;
                case "petbuffs": _ctx.Config.BuffPets = !_ctx.Config.BuffPets; reply($"Pet buffs {(_ctx.Config.BuffPets ? "ON" : "OFF")}."); break;
                case "petfollow": FollowMaster(DynelManager.LocalPlayer); reply("Pets: follow me."); break;

                case "petkill":
                case "petterminate":
                case "petdismiss":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer;
                    if (mp == null) { reply("Not in play yet."); break; }
                    int had = OwnedCount(mp);
                    Dismiss(mp);
                    reply($"Dismissed {had} pet(s). 'petsummon' brings them back.");
                    break;
                }

                case "petsummon":
                case "petresummon":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer;
                    if (mp == null) { reply("Not in play yet."); break; }
                    if (!_ctx.Config.UsePets) { _ctx.Config.UsePets = true; reply("Pets were off — turning them on."); }
                    _ctx.Config.AutoResummon = true;
                    ResummonAll(mp);
                    reply("Resummoning the full set — one at a time as nano allows.");
                    break;
                }

                case "pethealme":
                case "petheal":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer; PlayerChar po = _owner.Find();
                    if (mp == null || po == null) { reply("Can't — I don't see you."); break; }
                    if (!HealTargetLatched(mp, po.Identity, "you")) { reply("No heal pet up."); break; }
                    reply("Heal pet is on you.");
                    break;
                }

                case "pethealself":
                case "pethealme2":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer;
                    if (mp == null) { reply("Not in play yet."); break; }
                    if (!HealTargetLatched(mp, mp.Identity, "myself")) { reply("No heal pet up."); break; }
                    reply("Heal pet is on me.");
                    break;
                }

                case "pethealpet":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer;
                    if (mp == null) { reply("Not in play yet."); break; }
                    NpcChar tank = AttackPet(mp);
                    if (tank == null) { reply("No attack pet up to heal."); break; }
                    if (!HealTargetLatched(mp, tank.Identity, $"my attack pet ({tank.Name})")) { reply("No heal pet up."); break; }
                    reply($"Heal pet is on {tank.Name}.");
                    break;
                }

                case "pethealauto":
                {
                    HealAuto();
                    reply("Heal pet back to automatic: me when I'm melee, my attack pet when I'm ranged.");
                    break;
                }

                case "pethealtarget":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer;
                    if (mp == null) { reply("Not in play yet."); break; }
                    if (string.IsNullOrWhiteSpace(arg)) { reply($"Who? 'pethealtarget <name>', or {HealWhoHint()}"); break; }
                    SimpleChar who = ResolveHealSubject(arg, mp);
                    if (who == null) { reply($"I can't see anyone called '{arg}'."); break; }
                    if (!HealTargetLatched(mp, who.Identity, who.Name)) { reply("No heal pet up."); break; }
                    reply($"Heal pet is on {who.Name}.");
                    break;
                }

                case "pethealmytarget":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer; PlayerChar po = _owner.Find();
                    if (mp == null || po == null) { reply("Can't — I don't see you."); break; }
                    // Only what you are FIGHTING is on the wire. A selection you have merely clicked is not:
                    // LookAtMessage, which carries a target change, is only ever sent with the sender's own
                    // identity — in every capture under E:\Funcom\sniffs and E:\Funcom\captures, not once
                    // relayed for another character. So outside combat there is nothing here to read, and
                    // saying "no target" alone just leaves you stuck.
                    SimpleChar t = po.FightingTarget;
                    if (t == null)
                    {
                        reply("I can only see what you're FIGHTING — the server never tells me what you've "
                              + $"merely clicked on. So name it instead: {HealWhoHint()}");
                        break;
                    }
                    if (!HealTargetLatched(mp, t.Identity, t.Name)) { reply("No heal pet up."); break; }
                    reply($"Heal pet is on your target, {t.Name}.");
                    break;
                }

                case "petstatus":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer;
                    if (mp == null) { reply("Not in play yet."); break; }
                    var roster = mp.Pets.Select(p => $"{p.Name} ({p.Role}, {mp.DistanceFrom(p):0}m)").ToList();
                    reply($"Owned {OwnedCount(mp)}, visible {roster.Count}"
                          + (roster.Count > 0 ? ": " + string.Join(", ", roster) : "")
                          + $". Heal pet targets {HealTargetDescription(mp)}.");
                    break;
                }
                case "petattack":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer; PlayerChar po = _owner.Find();
                    if (mp == null || po == null || po.FightingTarget == null) { reply("No target — are you fighting?"); break; }
                    EngageTarget(mp, po.FightingTarget, 999.0);   // force it through now
                    reply($"Pets: attacking {po.FightingTarget.Name}.");
                    break;
                }
            }
        }

        /// <summary>A visible character by name, for commands that name someone (case-insensitive, and a
        /// unique prefix will do so you need not type a full name in a tell).</summary>
        private static SimpleChar FindCharByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            name = name.Trim();
            var all = DynelManager.Characters.Where(c => !string.IsNullOrEmpty(c.Name)).ToList();
            SimpleChar exact = all.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;
            var starts = all.Where(c => c.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase)).ToList();
            return starts.Count == 1 ? starts[0] : null;
        }

        /// <summary>
        /// Who a heal-pet command means. A name works, and so do the words you'd actually type in a tell:
        /// "me"/"you" is the owner, "him"/"self"/"bot" is the bot, "pet" is the attack pet. The two the owner
        /// reaches for most — the bot and himself — are exactly the two that cannot be picked by clicking,
        /// because a selection is never broadcast (see pethealmytarget).
        /// </summary>
        private SimpleChar ResolveHealSubject(string who, LocalPlayer me)
        {
            switch ((who ?? "").Trim().ToLowerInvariant())
            {
                case "me":
                case "you":
                case "owner":
                    return _owner.Find();
                case "him":
                case "he":
                case "self":
                case "bot":
                case "himself":
                    return me;
                case "pet":
                case "attackpet":
                    return AttackPet(me);
            }
            return FindCharByName(who);
        }

        /// <summary>The ways to name a heal-pet subject, for a reply that would otherwise be a dead end.</summary>
        private string HealWhoHint() =>
            "'pethealme' (you), 'pethealself' (me), 'pethealpet' (my attack pet), "
            + "or 'pethealtarget <name>' for anyone else I can see.";

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
            foreach (NpcChar p in me.Pets) _lastSeen[p.Identity.Instance] = _petClock;

            string roster = string.Join(", ", me.Pets.Select(p => $"{p.Name}#{p.Identity.Instance}:{p.Role}"));
            // Say how many the SERVER says we own alongside how many we can see, so "pet gone" and "pet out
            // of range" stop looking the same in the log.
            string line = $"[{(roster.Length == 0 ? "none" : roster)}] visible={me.Pets.Count()} owned={_owned.Count}";
            if (line != _lastRoster)
            {
                _lastRoster = line;
                _ctx.Log($"PET: roster = {line}");
            }
        }

        // Keep pets up. Resummons the moment a summon nano is castable again (its recharge is the resummon
        // timer). Gated by Config.UsePets and Config.AutoResummon so the owner can turn replacement off per
        // situation.
        public void MaintainPets(LocalPlayer me, double dt, bool supportBusy, Action<CastRequest> queueCast)
        {
            if (me == null || !_ctx.Config.UsePets) return;

            _petClock += dt;
            LogRoster(me);

            // ONE cast at a time: queue summons and pet buffs through the same cast system as the other buffs
            // so they serialize instead of interrupting each other. Only add another when nothing is queued or
            // in flight — so each cast completes before the next is attempted.
            if (supportBusy || me.IsCasting) return;

            _resummonAccum += dt;
            if (_resummonAccum < 1.0) return;    // check ~1/s, not every frame
            _resummonAccum = 0;

            // Summoning comes first; buffs go on whatever pets are up whenever no summon was queued this
            // pass. Buffing must NOT hang off the summon list: when that list came back empty (every summon
            // read uncastable) or AutoResummon was off, the pets standing right there were never buffed.
            if (_ctx.Config.AutoResummon && TrySummon(me, queueCast)) return;
            if (_ctx.Config.BuffPets) BuffPets(me, queueCast);
        }

        // Queue one summon if we're short a pet. True if a summon was queued.
        private bool TrySummon(LocalPlayer me, Action<CastRequest> queueCast)
        {
            // Summon nanos: AUTO-detected best-QL-per-pet-line (no config). An explicit override is honoured
            // if set, but by default he picks his own best pets.
            List<int> summons = (_ctx.Config.PetSummonNanoIds != null && _ctx.Config.PetSummonNanoIds.Count > 0)
                ? _ctx.Config.PetSummonNanoIds
                : AutoSummons(me);

            // Count what the SERVER says we own, not what is in view. Summoning because a live pet wandered
            // out of range is how you end up with two attack pets, or with a summon refused every twelve
            // seconds for ten minutes because the one you have is alive and standing somewhere else.
            int have = OwnedCount(me);
            if (have >= summons.Count) return false;   // full complement is up

            // Short a pet. Summon the first type we haven't summoned within SummonRecastSec — each pet needs a
            // few seconds to appear and register, and re-casting the same summon before then replaces the pet
            // and drains nano. The list is attack-first, so the attack pet is always re-established first.
            // SHELLS (Engineer robots, Bureaucrat droids): the summon nano makes a shell item, and using the shell
            // makes the pet. So: a shell already in the bags is used first; only with none there is the nano cast,
            // and the shell it makes is used on the next pass. A shell we cannot use yet holds the nano back -
            // casting again would only make another shell, and most robot nanos charge credits for each one.
            var shell = ShellCheck(me, out Item unusable);
            if (shell != null)
            {
                _ctx.Log($"PET: using shell '{shell.Name}' id={shell.Id} ql={shell.Ql} (have {have}/{summons.Count}).");
                _shellUsedAt[(shell.Id, shell.Slot.Instance)] = _petClock;
                shell.Use();
                return true;
            }
            if (_shellPending) return false;                     // just used one: wait for the pet
            if (unusable != null)
            {
                if (_shellWarned != unusable.Id) { _shellWarned = unusable.Id; _ctx.Log($"PET: shell '{unusable.Name}' ql={unusable.Ql} is in the bags but its requirements aren't met; not casting another."); }
                return false;
            }

            foreach (int nanoId in summons)
            {
                if (_summonAt.TryGetValue(nanoId, out double last) && _petClock - last < SummonRecastSec)
                    continue;
                _summonAt[nanoId] = _petClock;
                queueCast(new CastRequest { OnSelf = true, NanoId = nanoId, Label = "summon pet" });   // serialized with buffs by the shared cast queue
                _ctx.Log($"PET: summon — queued nano {nanoId} (have {have}/{summons.Count}).");
                return true;
            }
            return false;
        }

        // ---- Shells -----------------------------------------------------------------------
        // A shell is known by its requirements, not its name (item data, 2026-09-23: all 175 items carrying the
        // pet-slot gate were read): it needs OUR profession (Profession == 3 on every Engineer shell, 8 on every
        // Bureaucrat one) and a free attack-pet slot (TestNumPets 1). That leaves out towers (5001), charm
        // critters (7001/7002), the Unicorn Intercom and other classes' shells. Only the bags count - never the
        // bank or a worn slot.
        private readonly Dictionary<(int, int), double> _shellUsedAt = new Dictionary<(int, int), double>();
        private int _shellWarned;
        private bool _shellPending;

        private static bool IsOurShell(Item i, int profession)
        {
            if (i?.Criteria == null || !i.Criteria.TryGetValue(ItemActionInfo.UseCriteria, out var use)) return false;
            return use.Any(c => c.Operator == UseCriteriaOperator.TestNumPets && c.Param2 / 1000 == 0)
                && use.Any(c => c.Operator == UseCriteriaOperator.EqualTo && c.Param1 == (int)Stat.Profession && c.Param2 == profession);
        }

        /// <summary>The best shell in the bags we can use now (highest QL), or null; `unusable` = one we hold
        /// but cannot use yet. Sets _shellPending while a shell we used is still waiting to become a pet.</summary>
        private Item ShellCheck(LocalPlayer me, out Item unusable)
        {
            unusable = null; _shellPending = false;
            if (Inventory.Items == null || !me.TryGetStat(Stat.Profession, out int prof)) return null;
            Item best = null;
            foreach (Item i in Inventory.Items)
            {
                if (i == null || i.Slot.Type != IdentityType.Inventory || !IsOurShell(i, prof)) continue;
                if (_shellUsedAt.TryGetValue((i.Id, i.Slot.Instance), out double at) && _petClock - at < SummonRecastSec) { _shellPending = true; continue; }
                bool ok; try { ok = i.MeetsUseReqs(me); } catch { ok = false; }
                if (!ok) { if (unusable == null || i.Ql > unusable.Ql) unusable = i; continue; }
                if (best == null || i.Ql > best.Ql) best = i;
            }
            return best;
        }

        // Summon nanos that make a shell (SpawnItem) rather than a pet: GameData/PetShellNanos.json, id -> the
        // level of the pet it makes. Their nano line is NOSTACKING, so the pet-line search never finds them.
        private readonly Dictionary<int, int> _shellNanos = new Dictionary<int, int>();

        private void LoadShellNanos(string pluginDir)
        {
            try
            {
                string f = System.IO.Path.Combine(pluginDir ?? "", "GameData", "PetShellNanos.json");
                if (!System.IO.File.Exists(f)) { _ctx.Log("PET: GameData/PetShellNanos.json missing - Engineer/Bureaucrat shell pets won't be summoned."); return; }
                var doc = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(f));
                foreach (var n in doc["nanos"]) _shellNanos[(int)n["id"]] = (int)n["level"];
            }
            catch (Exception e) { _ctx.Log("PET: couldn't read PetShellNanos.json: " + e.Message); }
        }

        // ---- Pet buffs ------------------------------------------------------------
        // Config.AttackPetBuffLines go on the attack pet, Config.AllPetsBuffLines on every combat pet. Per
        // (pet, line) the best nano he knows that MeetsUseReqs says he can cast ON THAT PET — so the nano's own
        // target requirements (e.g. NPCFamily 97) still decide — and only if the pet doesn't already carry an
        // equal-or-better buff of that line. One buff queued per pass; the shared cast queue serializes it.

        // After queueing a pet buff, don't queue the same one on the same pet again for this long: the cast
        // takes a moment to land and show up in the pet's buff list.
        private const double PetBuffRecastSec = 20.0;
        private readonly Dictionary<string, double> _petBuffAt = new Dictionary<string, double>();   // "pet:nano" -> _petClock
        private readonly HashSet<string> _petBuffWarned = new HashSet<string>();

        private void BuffPets(LocalPlayer me, Action<CastRequest> queueCast)
        {
            int[] spells = me.SpellList;
            if (spells == null || spells.Length == 0) return;

            List<NpcChar> pets = Pets(me).Where(p => p.IsCombatPet).ToList();
            if (pets.Count == 0) return;

            var attackLines = ParseLines(_ctx.Config.AttackPetBuffLines);
            var allLines = ParseLines(_ctx.Config.AllPetsBuffLines);
            if (attackLines.Count == 0 && allLines.Count == 0) return;

            // Known nanos of the configured lines, grouped per line, best (highest StackingOrder) first.
            var byLine = new Dictionary<NanoLine, List<NanoItem>>();
            foreach (int id in spells)
            {
                if (!ItemData.Find(id, out NanoItem ni) || ni == null) continue;
                if (!attackLines.Contains(ni.NanoLine) && !allLines.Contains(ni.NanoLine)) continue;
                if (!byLine.TryGetValue(ni.NanoLine, out var list)) byLine[ni.NanoLine] = list = new List<NanoItem>();
                list.Add(ni);
            }
            foreach (var list in byLine.Values)
                list.Sort((a, b) => (b.StackingOrder & 0xFFFFF).CompareTo(a.StackingOrder & 0xFFFFF));

            foreach (NpcChar pet in pets)
            {
                foreach (var kv in byLine)
                {
                    bool wanted = allLines.Contains(kv.Key) || (pet.Role == PetType.Attack && attackLines.Contains(kv.Key));
                    if (!wanted) continue;

                    NanoItem best = kv.Value.FirstOrDefault(ni => CastableOn(ni, pet));
                    if (best == null)
                    {
                        WarnNoneCastable(me, pet, kv.Key);
                        continue;
                    }

                    if (!PetNeedsBuff(pet, best)) continue;
                    string key = pet.Identity.Instance + ":" + best.Id;
                    if (_petBuffAt.TryGetValue(key, out double last) && _petClock - last < PetBuffRecastSec) continue;
                    if (best.Range > 0 && me.DistanceFrom(pet) > best.Range) continue;   // wait until it's in reach

                    _petBuffAt[key] = _petClock;
                    queueCast(new CastRequest { Target = pet.Identity, NanoId = best.Id, Label = "buff pet" });
                    _ctx.Log($"PET: buff — queued '{best.Name}' [{best.Id}] ({kv.Key}) on {pet.Name}#{pet.Identity.Instance} ({pet.Role}).");
                    return;
                }
            }
        }

        private static bool CastableOn(NanoItem ni, NpcChar pet)
        {
            try { return ni.MeetsUseReqs(pet, false); } catch { return false; }
        }

        // Missing, or an equal-or-better buff of the line isn't up, or it is about to run out. The refresh margin
        // is capped at a quarter of the buff's duration, so short-term buffs aren't recast the moment they land.
        private bool PetNeedsBuff(NpcChar pet, NanoItem best)
        {
            Buff up = pet.Buffs?.FirstOrDefault(b => b.Id == best.Id
                || (b.NanoItem != null && b.NanoItem.NanoLine == best.NanoLine && b.NanoItem.StackingOrder >= best.StackingOrder));
            if (up == null) return true;

            double remaining = up.Cooldown?.RemainingTime ?? -1;
            if (remaining < 0) return false;   // up, timer unknown — leave it
            double margin = Math.Min(_ctx.Config.RebuffMarginSeconds, best.TotalTime / 4.0);
            return remaining < margin;
        }

        // A line with known nanos but none castable on this pet: say why once (skills, or the pet's target
        // stats not matching the nano's target requirements), so a wire value we misread is visible.
        private void WarnNoneCastable(LocalPlayer me, NpcChar pet, NanoLine line)
        {
            string key = pet.Identity.Instance + ":" + line;
            if (!_petBuffWarned.Add(key)) return;
            pet.TryGetStat(Stat.Breed, out int breed);
            pet.TryGetStat(Stat.NPCFamily, out int family);
            _ctx.Log($"PET: buff — no {line} nano castable on {pet.Name}#{pet.Identity.Instance} ({pet.Role}, breed={breed}, family={family}).");
        }

        private readonly HashSet<string> _badLineNames = new HashSet<string>();

        // Config line names -> NanoLine. Accepts enum names or numbers; unknown names are logged once.
        private HashSet<NanoLine> ParseLines(List<string> names)
        {
            var lines = new HashSet<NanoLine>();
            if (names == null) return lines;
            foreach (string raw in names)
            {
                string n = raw?.Trim();
                if (string.IsNullOrEmpty(n)) continue;
                if (Enum.TryParse(n, true, out NanoLine line)) lines.Add(line);
                else if (_badLineNames.Add(n)) _ctx.Log($"PET: buff — unknown NanoLine '{n}' in config, ignored.");
            }
            return lines;
        }

        // The best summon nano in each pet line the character KNOWS: highest StackingOrder then QL that he can
        // actually cast (NanoItem.MeetsUseReqs — the same castability check the auto-buff uses). No config, no
        // hardcoding — auto-upgrades as he trains and uploads better pet nanos.
        public List<int> AutoSummons(LocalPlayer me)
        {
            // Per pet line, pick the highest-StackingOrder (= best) summon nano MeetsUseReqs says we can cast.
            // ignorePetLimit: rank on skills only — a pet already up in that slot fails TestNumPets, and the
            // line must still count toward the complement. A line with nanos but none castable is left out
            // rather than guessing the best one: that cast is refused for skills (e.g. 456 ST/MC at 450).
            var bestCastId = new Dictionary<NanoLine, int>(); var bestCastRank = new Dictionary<NanoLine, long>();
            var knownLines = new HashSet<NanoLine>();
            int[] spells = me == null ? null : me.SpellList;
            if (spells != null)
            {
                foreach (int id in spells)
                {
                    if (!ItemData.Find(id, out NanoItem ni) || ni == null) continue;
                    if (Array.IndexOf(PetLines, ni.NanoLine) < 0) continue;
                    knownLines.Add(ni.NanoLine);
                    long rank = ni.StackingOrder & 0xFFFFF;
                    bool castable; try { castable = ni.MeetsUseReqs(me, false, true); } catch { castable = false; }
                    if (castable && (!bestCastRank.TryGetValue(ni.NanoLine, out long c) || rank > c)) { bestCastRank[ni.NanoLine] = rank; bestCastId[ni.NanoLine] = id; }
                }
            }

            // Shell-making summons (Engineer robot, Bureaucrat droid): the highest-level one he knows and can cast.
            int shellId = 0, shellLevel = -1; bool shellKnown = false;
            if (spells != null)
                foreach (int id in spells)
                {
                    if (!_shellNanos.TryGetValue(id, out int lvl)) continue;
                    shellKnown = true;
                    if (lvl <= shellLevel || !ItemData.Find(id, out NanoItem sn) || sn == null) continue;
                    bool castable; try { castable = sn.MeetsUseReqs(me, false, true); } catch { castable = false; }
                    if (castable) { shellId = id; shellLevel = lvl; }
                }

            var result = new List<int>();
            var dbg = new List<string>();
            if (shellId != 0) { result.Add(shellId); dbg.Add($"Shell:{shellId}(L{shellLevel})"); }
            else if (shellKnown) dbg.Add("Shell:none-castable");
            foreach (NanoLine line in PetLines)
            {
                if (bestCastId.TryGetValue(line, out int cid)) { result.Add(cid); dbg.Add($"{line}:{cid}"); }
                else if (knownLines.Contains(line)) dbg.Add($"{line}:none-castable");
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

            // An explicit 'pethealtarget' order wins until it is cleared.
            Identity? healTarget = _healLatched ?? HealPetTarget(me);
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

        /// <summary>
        /// Keep the pets with us. An Attack command sends a pet after its mob and NOTHING brings it back:
        /// it finishes the fight wherever that ended and stands there, picking up anything that wanders by.
        /// A pet in combat holds its MASTER in combat, and in combat the server suppresses health and nano
        /// regen and refuses heal items - so one pet left behind quietly stops the bot recovering at all.
        /// That is not a theory: an attack pet sat 38m away for a whole session while its master's HP and
        /// nano stayed frozen to the point and every recharger use was refused.
        ///
        /// So: when the fight ends, call them in, and call in any pet that has drifted past RecallMeters
        /// even mid-fight. Issued once per episode, because re-sending Follow every tick would cancel the
        /// pet's own pathing each time.
        /// </summary>
        public void RecallStragglers(LocalPlayer me, bool fighting)
        {
            if (me == null || !_ctx.Config.UsePets) return;

            bool endedFight = _wasFighting && !fighting;
            _wasFighting = fighting;

            // NEVER DURING A FIGHT. A pet sent at a mob runs to the mob, and this bot engages at twenty to
            // thirty-five metres - so an obedient pet is past RecallMeters within a second or two of being
            // given the order. Recalling it then replaces the Attack with a Follow and it never lands a blow:
            // "PET: attack 'Security Camera'" at 20:28:00, "PET: follow me - 2 pet(s) past 20m" at 20:28:04,
            // every single fight. A straggler is an OUT-OF-COMBAT problem; the pet left standing where its
            // fight ended is the one this exists for, and that case is reached through endedFight below.
            if (fighting) { _recallSent = false; return; }

            List<Identity> bring = new List<Identity>();
            foreach (NpcChar p in Pets(me))
            {
                // The heal pet is never a straggler. It is on a Heal order and follows whoever it is healing
                // on its own; a Follow sent to it REPLACES that order, and MaintainHealPet only re-issues on a
                // change of healer or target, so the pet would quietly stop healing and nothing would notice.
                if (p.Role == PetType.Heal) continue;

                // Out of view counts as far: we cannot measure it, and the last thing we knew was that it
                // was at the edge. Only act on that once it has been gone a while, so an ordinary flicker
                // behind a wall does not trigger a recall.
                float d = me.DistanceFrom(p);
                if (d > RecallMeters) bring.Add(p.Identity);
            }

            // A pet the server says is ours but that we cannot see at all is the important case - it is the
            // one that strands. We cannot name it in a targeted command without its dynel, so those get the
            // all-pets form of Follow.
            bool haveUnseen = _owned.Count > Pets(me).Count();

            if (!endedFight && bring.Count == 0 && !haveUnseen) { _recallSent = false; return; }
            if (_recallSent && !endedFight) return;   // one recall per episode
            _recallSent = true;

            if (haveUnseen || bring.Count == 0)
            {
                // The all-pets form reaches the healer too, so its Heal order is gone: make MaintainHealPet
                // give it again on the next tick rather than leave a heal pet standing there doing nothing.
                me.CommandPets(PetCommand.Follow);
                InvalidateHealTask();
                _ctx.Log($"PET: follow me — {(haveUnseen ? "a pet we own is out of sight" : "fight over")} (visible={Pets(me).Count()} owned={_owned.Count}).");
            }
            else
            {
                me.CommandPets(PetCommand.Follow, bring);
                _ctx.Log($"PET: follow me — {bring.Count} pet(s) past {RecallMeters:0}m.");
            }
        }

        /// <summary>
        /// Forget that the heal pet has been told who to heal, so the order is re-issued. Any command that
        /// can reach the healer REPLACES its Heal - Follow, Attack, a regroup - and because the order is
        /// normally given once per summon, nothing would ever put it back: the pet just stops healing while
        /// standing right next to the person it is supposed to be keeping alive.
        /// </summary>
        private void InvalidateHealTask() => _healTaskedRoster = null;

        private bool _wasFighting;
        private bool _recallSent;

        // How far a pet may drift before we call it back. Well inside the ~40m at which a dynel stops being
        // broadcast to us, so we recall it while we can still see it rather than after it has stranded.
        private const float RecallMeters = 20f;

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
            // Reaches the healer as well, so its Heal order has to be given again (see InvalidateHealTask).
            if (me != null && me.Pets.Any()) { me.CommandPets(PetCommand.Follow); InvalidateHealTask(); _ctx.Log("PET: follow."); }
        }

        public void Dismiss(LocalPlayer me)
        {
            if (me != null && me.Pets.Any()) { me.CommandPets(PetCommand.Terminate); _ctx.Log("PET: terminate all."); }
            // The server will send RemovePet for each, but clear our own view now so nothing tries to
            // command a pet we just dismissed in the gap before those arrive.
            _owned.Clear();
            _lastSeen.Clear();
            _healTaskedRoster = "\0";
            _attackTaskedRoster = "\0";
        }

        /// <summary>
        /// Throw away every summon cooldown so the keep-up pass re-casts the full set on its next tick.
        /// Used by 'petsummon' after a dismissal, when waiting out SummonRecastSec would be pointless -
        /// we know the pets are gone because we just terminated them.
        /// </summary>
        public void ResummonAll(LocalPlayer me)
        {
            _summonAt.Clear();
            _shellUsedAt.Clear();
            _healTaskedRoster = "\0";
            _attackTaskedRoster = "\0";
            _lastAttackTarget = null;
            _lastHealTarget = null;
            _ctx.Log($"PET: resummon — cleared summon timers, {(me == null ? 0 : OwnedCount(me))} pet(s) currently owned.");
        }

        /// <summary>
        /// Point the heal pet at a specific target and keep it there. The automatic tasking would otherwise
        /// put it back on its own choice (the master when melee, the attack pet when ranged) the next time
        /// the roster changes, so an explicit order latches until it is changed or the pet is resummoned.
        /// </summary>
        public bool HealTargetLatched(LocalPlayer me, Identity target, string describe)
        {
            NpcChar healer = HealPet(me);
            if (me == null || healer == null) return false;

            Targeting.SetTarget(target);
            me.CommandPets(PetCommand.Heal, new[] { healer.Identity });
            _healLatched = target;
            _healTaskedRoster = healer.Identity.Instance + ">" + target.Instance;
            _lastHealTarget = target;
            _ctx.Log($"PET: heal '{healer.Name}' -> {describe} (ordered).");
            return true;
        }

        /// <summary>Drop an explicit heal order and go back to choosing by weapon reach.</summary>
        public void HealAuto()
        {
            _healLatched = null;
            _healTaskedRoster = "\0";
            _ctx.Log("PET: heal target back to automatic (melee: me, ranged: the attack pet).");
        }

        private Identity? _healLatched;

        public string HealTargetDescription(LocalPlayer me)
        {
            if (_healLatched.HasValue) return $"#{_healLatched.Value.Instance} (ordered)";
            return IsMeleeLoadout(me) ? "myself (melee loadout)" : "my attack pet (ranged loadout)";
        }

        public void Reset()
        {
            _lastAttackTarget = null;
            _lastHealTarget = null;
            _attackTaskedRoster = "\0";
            _healTaskedRoster = "\0";
            _wasFighting = false;
            _recallSent = false;
            // Zoning does NOT necessarily re-create the pets. Through a travel terminal they came along with
            // their instance ids unchanged (245447606/7 before and after) and no fresh AddPet followed, so
            // clearing this left the bot reporting "owned=0" while three of its own pets stood beside it.
            // Re-seed from the pets the server still says are ours instead of assuming they are gone; a pet
            // that really was left behind simply is not in the list.
            _owned.Clear();
            LocalPlayer me = DynelManager.LocalPlayer;
            if (me != null)
                foreach (NpcChar p in me.Pets)
                    _owned.Add(p.Identity.Instance);
            _lastSeen.Clear();
        }
    }
}
