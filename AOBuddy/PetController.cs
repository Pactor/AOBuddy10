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
    /// commands go via <see cref="LocalPlayer.CommandPets(PetCommand)"/>, and summoning is a normal nano cast.
    ///
    /// Two facts drive the design (confirmed with the user, an MP player):
    ///  - Attack and Heal pet commands are TARGET-BASED: the pet acts on the MASTER's current target, so we
    ///    set the target first, then command.
    ///  - A pet that dies just drops out of me.Pets; the summon nano's recharge IS the "resummon timer", so we
    ///    resummon the instant it is castable again.
    ///
    /// This controller NEVER moves the bot (follow owns movement). It logs the roster it sees (pet names +
    /// identities) so we can later tell the attack/heal/mezz pets apart per class.
    ///
    /// KNOWN LIMIT (base cut): commands go to ALL pets at once — we do not yet identify pet TYPE (attack vs
    /// heal vs mezz), so telling the group to Attack also re-tasks a heal pet. Per-type commands (keep the
    /// heal pet healing while the rest attack) come in the MP deep-dive, once the roster log tells us how the
    /// pet types are named/flagged. Until then use the manual pet* commands to steer.
    /// </summary>
    public class PetController
    {
        private readonly BotContext _ctx;

        private double _resummonAccum;
        private double _commandAccum;
        private Identity? _lastAttackTarget;
        private string _lastRoster = "";
        private string _lastSummonKey = "\0";
        private int _summonIdx;

        // The COMBAT pet-summon nano lines (server-tagged): highest-QL castable in each is the pet to keep up.
        // Deliberately NOT SocialPets — that line is vehicles/social (the MP "boogy board"), pure show.
        private static readonly NanoLine[] PetLines =
            { NanoLine.AttackPets, NanoLine.HealPets, NanoLine.SupportPets };

        public PetController(BotContext ctx)
        {
            _ctx = ctx;
        }

        public int PetCount => DynelManager.LocalPlayer?.Pets.Count() ?? 0;

        // DIAGNOSTIC: dump the bot's own identity and every nearby NPC's identity + owner, so we can see
        // from DATA why me.Pets (NPCs whose Owner == our identity) is empty even though pets are up in-game.
        public void DumpPets(LocalPlayer me)
        {
            if (me == null) { _ctx.Log("PETDBG: no LocalPlayer"); return; }
            _ctx.Log($"PETDBG: me.Identity Type={me.Identity.Type}/{(int)me.Identity.Type} Inst={me.Identity.Instance} | Client.LocalDynelId={Client.LocalDynelId} | Pets.Count={me.Pets.Count()}");
            var npcs = DynelManager.Npcs.Where(n => n != null).OrderBy(me.DistanceFrom).Take(20).ToList();
            _ctx.Log($"PETDBG: {DynelManager.Npcs.Count()} npc(s) total; nearest {npcs.Count}:");
            foreach (var n in npcs)
            {
                string owner = n.Owner.HasValue
                    ? $"{n.Owner.Value.Type}/{(int)n.Owner.Value.Type}:{n.Owner.Value.Instance}"
                    : "null";
                bool mine = n.Owner.HasValue && n.Owner.Value == me.Identity;
                _ctx.Log($"PETDBG:   '{n.Name}' id={(int)n.Identity.Type}:{n.Identity.Instance} d={me.DistanceFrom(n):0} owner={owner} mine={mine}");
            }
        }

        // Log the pet roster (name + id) whenever it changes, so we learn the attack/heal/mezz naming per
        // class — the data the per-pet-type control needs.
        private void LogRoster(LocalPlayer me)
        {
            string roster = string.Join(", ", me.Pets.Select(p => $"{p.Name}#{p.Identity.Instance}"));
            if (roster != _lastRoster)
            {
                _lastRoster = roster;
                _ctx.Log($"PET: roster = [{(roster.Length == 0 ? "none" : roster)}]");
            }
        }

        // Keep pets up. Resummons the moment a summon nano is castable again (its recharge is the resummon
        // timer). Gated by Config.UsePets and Config.AutoResummon so the owner can turn replacement off per
        // situation. Summon nanos come from Config.PetSummonNanoIds (auto-detection is a later step).
        public void MaintainPets(LocalPlayer me, double dt, bool supportBusy, Action<int> queueSummon)
        {
            if (me == null || !_ctx.Config.UsePets) return;

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

            // Short a pet. CYCLE through the summons (attack, heal, mezz) so EVERY pet type gets cast, not just
            // the first — the MP needs all three. We can't yet tell which type is missing, so we cast each in
            // turn (one per check — casts are serial); the server ignores a summon whose pet already exists.
            if (_summonIdx >= summons.Count) _summonIdx = 0;
            int nanoId = summons[_summonIdx++];
            queueSummon(nanoId);   // serialized with buffs by the shared cast queue
            _ctx.Log($"PET: summon — queued nano {nanoId} (have {have}/{summons.Count}).");
        }

        // The best summon nano in each pet line the character KNOWS: highest StackingOrder then QL that he can
        // actually cast (NanoItem.MeetsUseReqs — the same castability/eligibility check the auto-buff uses). No
        // config, no hardcoding — auto-upgrades as he trains and uploads better pet nanos. Cached; rebuilt when
        // the known-nano list changes.
        public List<int> AutoSummons(LocalPlayer me)
        {
            // Per pet line, pick the highest-StackingOrder (= best) summon nano. Prefer one MeetsUseReqs says
            // we can cast, BUT that check UNDER-reports for summon nanos (reads False even when buffed and able),
            // so if a line has none it calls castable, fall back to the best overall and let the SERVER decide.
            // Re-evaluated every check (no stale cache) so pets appear the moment the nano-skill buffs land.
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

        // In combat: point the pets at the owner's target. Target-based Attack — set our target to the mob,
        // then CommandPets(Attack). Immediate on a new target, then re-asserted occasionally.
        public void EngageTarget(LocalPlayer me, SimpleChar target, double dt)
        {
            if (me == null || target == null || !_ctx.Config.UsePets) return;
            if (!me.Pets.Any()) return;

            bool newTarget = _lastAttackTarget != target.Identity;
            _commandAccum += dt;
            if (!newTarget && _commandAccum < 3.0) return;   // re-assert every few seconds; instant on a new target
            _commandAccum = 0;
            _lastAttackTarget = target.Identity;

            Targeting.SetTarget(target.Identity);
            me.CommandPets(PetCommand.Attack);
            _ctx.Log($"PET: attack '{target.Name}' ({me.Pets.Count()} pets).");
        }

        // Manual, target-based heal: point the (heal) pet at a target — the owner as main, or the attack pet.
        // Sends Heal to the group; only a heal-capable pet acts on it.
        public void HealTarget(LocalPlayer me, Identity target)
        {
            if (me == null || !me.Pets.Any()) return;
            Targeting.SetTarget(target);
            me.CommandPets(PetCommand.Heal);
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
            _commandAccum = 0;
        }
    }
}
