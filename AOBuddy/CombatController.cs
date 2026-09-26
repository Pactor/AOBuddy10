using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;

namespace AOBuddy
{
    /// <summary>
    /// COMBAT — decides WHAT the bot fights and issues the attack. It never moves the bot: the bot
    /// stays at the owner's side via FOLLOW, and the server enforces weapon reach, so combat is a
    /// pure overlay on top of following. It assists ONLY the owner's own fights.
    ///
    /// The one hard-won rule: issue Attack ONCE per target, gated on the target CHANGING — never on
    /// IsAttacking. Re-issuing Attack restarts the weapon swing timer, so re-sending it (e.g. around
    /// each stim) makes the bot swing once then wait forever (the "never swings" bug). The server
    /// keeps auto-swinging once started, and auto-stops when the fight ends — so we never send
    /// StopAttack either.
    /// </summary>
    public class CombatController
    {
        private readonly BotContext _ctx;

        private Identity? _attackedTarget;   // the target we've already issued Attack on

        /// <summary>The target the fight is with (what we issued Attack on, kept through the FightingTarget
        /// null-blinks), for the API's /status target bar. Null when nothing is engaged.</summary>
        public Identity? AttackedTarget => _attackedTarget;
        private double _notSwinging;         // seconds we've been on this target without the server swinging
        private bool _inCombat;

        // How long the server must leave us not-swinging on a mob we already engaged before we re-issue
        // Attack. Comfortably longer than any real weapon's recharge gap, so a normal fight issues exactly
        // one Attack per target and only a real stall produces a second.
        private const double StallResumeSeconds = 3.0;
        private string _lastTargetLog = "";
        private double _noTargetLogAccum;

        public bool InCombat => _inCombat;

        public CombatController(BotContext ctx)
        {
            _ctx = ctx;
        }

        // Targets not to fight for a while: a mob our blows don't touch (MissionRun.Attacker, 2026-09-23: 12
        // minutes on a find-person NPC, every blow refused with feedback 110). Stop swinging at it now.
        // Expiry is an absolute ctx.Clock seconds stamp (R2.1: was DateTime.UtcNow — same semantics, one clock).
        private readonly Dictionary<Identity, double> _setAside = new Dictionary<Identity, double>();
        public void ClearAside(Identity id) => _setAside.Remove(id);
        public bool IsSetAside(Identity id) => _setAside.TryGetValue(id, out var until) && _ctx.Clock.Seconds < until;
        public void SetAside(LocalPlayer me, Identity id, double seconds)
        {
            _setAside[id] = _ctx.Clock.Seconds + seconds;
            if (_attackedTarget == id) _attackedTarget = null;
            if (me != null && me.FightingIdentity.HasValue && me.FightingIdentity.Value == id) me.StopAttack();
        }

        // Pick the owner's fight and, if it's a NEW target, issue the attack a single time. Returns
        // the target (null = no owner fight). Called each decision tick.
        public SimpleChar SelectAndEngage(LocalPlayer me, PlayerChar owner, SimpleChar defend = null)
        {
            _sinceCombat += _ctx.Config.TickMs / 1000.0;

            // FOLLOW THE OWNER'S TARGET, ALWAYS. An earlier version committed to the first mob until it died
            // and ignored the owner switching — the owner has since retired that rule: his PET can pull aggro
            // we cannot see, so when he switches he is switching for a reason, and in practice the bot never
            // changed targets at all. So: take his fight every tick, and if he switches away and later comes
            // back to finish one off, that mob simply gets a second Attack — one to start it, one to resume
            // after the change, which is what a player does too.
            //
            // The one thing that must not happen is re-issuing Attack on the mob we are ALREADY swinging at:
            // that resets the weapon timer (swing once, then wait — the "not swinging" bug). The
            // _attackedTarget check below is what prevents it.
            SimpleChar target = GetAssistTarget(me, owner);
            if (target != null && IsSetAside(target.Identity)) target = null;
            // SOLO (mission run): with no owner fight, whatever is attacking the bot or its pets.
            if (target == null && defend != null) target = LogTarget(defend, "defending");

            // His FightingTarget flickers to null for a tick mid-fight. Don't read that as "fight over" and
            // drop the mob we are on — hold the current one while it is still a live, hostile, in-range mob.
            if (target == null && _attackedTarget.HasValue)
            {
                SimpleChar current = DynelManager.Characters.FirstOrDefault(c => c.Identity == _attackedTarget.Value);
                if (current != null && !IsSetAside(current.Identity) && IsHostile(current, me, owner) && IsAlive(current)
                    && me.DistanceFrom(current) <= _ctx.Config.AssistMaxDistance)
                    target = current;
            }

            if (target != null)
            {
                // ONE Attack per target. The server drives the swing from there: our AttackMessage sets
                // FightingIdentity (= IsAttacking) and it auto-swings on the weapon timer. Re-issuing on a
                // mob we are already swinging at RESETS that timer — swing once, then wait, the "never
                // swings" bug — so a target we have already engaged is never re-Attacked.
                //
                // The single exception is a genuine stall: the server can clear FightingIdentity between
                // swings (Client.cs handles StopFightMessage by doing exactly that), and without a resume the
                // bot would then stand idle on a live mob. So a resume is allowed, but only after the stall
                // has lasted StallResumeSeconds — long enough that it cannot fire in the gap between two
                // ordinary swings, which is what would turn "one Attack per target" into a stream of them.
                // ARE WE ALREADY SWINGING AT THIS ONE? Ask the server, not our own notes. FightingIdentity is
                // what it says we are fighting; _attackedTarget is only what we remember issuing. Those two
                // came apart whenever the owner's FightingTarget blinked null for a single tick: the else
                // branch below forgot the mob, the same mob came back a tick later, it looked new, and Attack
                // was re-issued - which RESETS the weapon timer, so he swung once and then stood there. That
                // is the pause. Eighteen of those null ticks in one session, two mobs re-attacked outright.
                bool alreadySwinging = me.FightingIdentity.HasValue && me.FightingIdentity.Value == target.Identity;
                bool newTarget = _attackedTarget != target.Identity && !alreadySwinging;
                if (alreadySwinging) _attackedTarget = target.Identity;   // re-sync our notes to the server
                if (!newTarget && !me.IsAttacking) _notSwinging += _ctx.Config.TickMs / 1000.0;
                else if (me.IsAttacking) _notSwinging = 0;

                if (newTarget || _notSwinging >= StallResumeSeconds)
                {
                    bool resume = !newTarget;
                    _ctx.Log($"ATTACK-> '{target.Name}' id={target.Identity} weapon={string.Join("+", EquippedWeapons().Select(w => w.Name))}{(resume ? $" (resume after {_notSwinging:0.0}s not swinging)" : "")}");
                    me.Attack(target);
                    _attackedTarget = target.Identity;
                    _notSwinging = 0;
                }
                _inCombat = true;
                _sinceCombat = 0;

                FireReadySpecials(me, target);
            }
            else
            {
                // Only forget the mob once we are genuinely off it. While the server still has us fighting
                // something, keep the note: dropping it on a flickering assist target is what made the same
                // mob look new a tick later and earned it a second Attack.
                if (!me.FightingIdentity.HasValue) _attackedTarget = null;
                _notSwinging = 0;
            }
            return target;
        }

        // Weapon SPECIAL attacks (Fast Attack, Brawl, Fling Shot, Burst, Full Auto, Aimed Shot, …) on top of
        // auto-attack. We fire ONLY specials the SERVER has told us this character has (me.KnownSpecials,
        // learned from SpecialUsed/SpecialAvailable — no hardcoding, works for any class/weapon), and only
        // when each is off cooldown. A special is a SEPARATE message that does NOT reset the main weapon swing
        // timer, so this cannot reintroduce the "never swings" bug. Snapshot the set first: firing registers a
        // cooldown, which touches the same collection we're iterating.
        // The ACTIVE weapon special attacks — the only stats that are on-demand, aim-at-a-mob specials.
        // KnownSpecials is learned from server COOLDOWN messages, which also cover non-attacks (FirstAid,
        // Treatment, Level, pet perks, nanos); firing those as "specials" interrupts the weapon swing
        // (attack-stop-attack) and wastes the action. This set is protocol fact — what a weapon special IS,
        // not per-class hardcoding — and the character's equipped weapon still decides which he can use.
        // Riposte/Parry are excluded (passive, auto-trigger on defence, not fired on demand).
        private static readonly HashSet<Stat> WeaponSpecials = new HashSet<Stat>
        {
            Stat.Brawl, Stat.Dimach, Stat.SneakAttack, Stat.FastAttack,
            Stat.Burst, Stat.FlingShot, Stat.AimedShot, Stat.FullAuto, Stat.Backstab,
        };

        private void FireReadySpecials(LocalPlayer me, SimpleChar target)
        {
            if (!_ctx.Config.UseSpecials) return;
            // Fire only the specials the EQUIPPED WEAPON allows (read from its own criteria), each when ready.
            foreach (Stat special in AllowedWeaponSpecials())
            {
                if (!me.IsSpecialReady(special)) continue;
                me.PerformSpecialAttack(target.Identity, special);
                _ctx.Log($"SPECIAL-> {special} on '{target.Name}' id={target.Identity.Instance}");
            }
        }

        // The special attacks the equipped weapon ALLOWS — read straight from the weapon's own criteria, where
        // a weapon lists its specials as requirements (e.g. "AimedShot 319", "FastAttack 251", "Burst 401").
        // This is the weapon's ground truth: a bow yields AimedShot, a pistol Burst/FlingShot/FullAuto, a melee
        // weapon Brawl/FastAttack/etc. No trained-skill reading, no class guessing — it works for any weapon,
        // and dual-wield contributes both hands' specials.
        public static HashSet<Stat> AllowedWeaponSpecials()
        {
            var allowed = new HashSet<Stat>();
            foreach (var w in EquippedWeapons())
            {
                if (w.Criteria == null) continue;
                foreach (var kv in w.Criteria)
                    foreach (var c in kv.Value)
                        if (WeaponSpecials.Contains((Stat)c.Param1))
                            allowed.Add((Stat)c.Param1);
            }
            return allowed;
        }

        // Dump the EQUIPPED weapon(s) exactly as the item data describes them — every criteria (wield/attack
        // requirement: stat + operator + value) and every modifier — so we can read from DATA which weapon
        // SKILL he actually uses, instead of assuming. Equipped weapons are the Inventory items in the weapon
        // page (same source the attack log already uses). This is the ground truth the buff-relevance filter keys on.
        // His actual weapon(s): the items in the HAND slots. The "weapon page" also holds HUD, utility,
        // belt and 6 NCU deck slots, so filtering on the page alone counts those too (9 "weapons"). A wielded
        // weapon is only ever in RightHand and/or LeftHand — this generalises to any class (bow, gun, melee).
        public static IEnumerable<Item> EquippedWeapons()
        {
            if (Inventory.Items == null) return Enumerable.Empty<Item>();
            return Inventory.Items.Where(x => x != null && x.Slot.Type == IdentityType.WeaponPage
                && (x.Slot.Instance == (int)EquipSlot.Weap_RightHand || x.Slot.Instance == (int)EquipSlot.Weap_LeftHand));
        }

        public int DumpWeapons(LocalPlayer me)
        {
            var weapons = EquippedWeapons().ToList();
            if (weapons.Count == 0) { _ctx.Log("WEAPON: no weapon in hand slots (not loaded yet?)."); return 0; }
            _ctx.Log($"WEAPON: {weapons.Count} equipped weapon(s) ---");
            foreach (var w in weapons)
            {
                _ctx.Log($"WEAPON: '{w.Name}' id={w.Id} ql={w.Ql} slot={w.Slot.Instance}");
                if (w.Criteria != null)
                    foreach (var kv in w.Criteria)
                        foreach (var c in kv.Value)
                        {
                            string statName = Enum.IsDefined(typeof(Stat), c.Param1) ? ((Stat)c.Param1).ToString() : c.Param1.ToString();
                            _ctx.Log($"WEAPON:   {kv.Key}: {statName} {c.Operator} {c.Param2}");
                        }
                if (w.Modifiers != null)
                    foreach (var m in w.Modifiers)
                        _ctx.Log($"WEAPON:   mod[{m.Key}] = {string.Join(", ", m.Value.Select(x => x.Key + ":" + x.Value))}");
            }
            _ctx.Log("WEAPON: --- end ---");
            return weapons.Count;
        }

        // Seconds since we last had an assist target (0 while fighting). Used to avoid sitting to rest in
        // the brief gap between mobs of a multi-mob pull.
        private double _sinceCombat = 999;
        public double SinceCombat => _sinceCombat;

        // Any hostile still actively engaged with the owner or the bot (so we should keep fighting, not
        // rest). Covers the lull where owner.FightingTarget is momentarily null but mobs are still on us.
        public bool HostilesEngaged(LocalPlayer me, PlayerChar owner)
        {
            if (owner == null) return false;
            return DynelManager.Characters.Any(c => IsHostile(c, me, owner)
                && ((c.FightingIdentity.HasValue && (c.FightingIdentity.Value == owner.Identity || c.FightingIdentity.Value == me.Identity))
                    || (c.IsAttacking && me.DistanceFrom(c) <= 20f)));
        }

        // Fight is over this tick. Log the disengage once and (throttled) explain why we found no
        // target if the owner still looks like he's fighting. We do NOT send StopAttack — the game
        // auto-stops combat when the fight ends.
        public void Disengage(LocalPlayer me, PlayerChar owner)
        {
            if (_inCombat) _ctx.Log("DISENGAGE (no assist target).");
            _inCombat = false;

            _noTargetLogAccum += _ctx.Config.TickMs / 1000.0;
            if (_noTargetLogAccum >= 2.0 && owner != null)
            {
                _noTargetLogAccum = 0;
                var onOwner = DynelManager.Characters.Where(n => n.FightingIdentity.HasValue && n.FightingIdentity.Value == owner.Identity && IsHostile(n, me, owner)).ToList();
                var fightingNear = DynelManager.Characters.Where(n => n.IsAttacking && IsHostile(n, me, owner) && owner.DistanceFrom(n) <= 20f).ToList();
                if (owner.FightingTarget != null || onOwner.Count > 0 || fightingNear.Count > 0)
                    _ctx.Log($"NO-TARGET: owner.FightingTarget={(owner.FightingTarget != null ? owner.FightingTarget.Name : "null")} npcsOnOwner={onOwner.Count} npcsFightingNear={fightingNear.Count} nearestFightingDist={(fightingNear.Count > 0 ? me.DistanceFrom(fightingNear.OrderBy(me.DistanceFrom).First()).ToString("0.0") : "-")}");
            }
        }

        // Solo mode: attack anything nearby that's already in combat.
        public void DoSolo(LocalPlayer me)
        {
            if (me.IsAttacking) return;
            SimpleChar target = DynelManager.Npcs
                .Where(n => n.IsAttacking && me.DistanceFrom(n) <= _ctx.Config.AssistMaxDistance)
                .OrderBy(me.DistanceFrom).FirstOrDefault();
            if (target != null) me.Attack(target);
        }

        public void Reset()
        {
            _inCombat = false;
            _attackedTarget = null;
            _notSwinging = 0;
        }

        // ---- Target selection ----------------------------------------------------

        private SimpleChar GetAssistTarget(LocalPlayer me, PlayerChar owner)
        {
            if (owner == null) return null;
            // The owner's chosen target — ALWAYS assist it, no distance cap. The bot won't move to it
            // (it stays at his side) and the server enforces reach, so a far target just means the
            // attack doesn't connect until it's close — never "it ignores your fight".
            SimpleChar ft = owner.FightingTarget;
            if (ft != null && IsHostile(ft, me, owner)) return LogTarget(ft, "owner.FightingTarget");

            // ANYTHING attacking the owner (regardless of Npc/Player/pet classification — some
            // hostiles come through as a player-pet, not an NpcChar). FightingIdentity == owner is a
            // precise, safe signal; self/owner/team are excluded.
            SimpleChar onOwner = DynelManager.Characters
                .Where(c => c.FightingIdentity.HasValue && c.FightingIdentity.Value == owner.Identity
                            && IsHostile(c, me, owner) && me.DistanceFrom(c) <= _ctx.Config.AssistMaxDistance)
                .OrderBy(me.DistanceFrom).FirstOrDefault();
            if (onOwner != null) return LogTarget(onOwner, "attacking-owner");

            // Fallback: nearest NPC fighting the OWNER specifically (covers a lagging FightingTarget
            // stat). MUST be scoped to the owner — using IsAttacking (attacking ANYONE) made the bot
            // jump into another player's fight just because a mob near the owner was in combat.
            SimpleChar fb = DynelManager.Npcs
                .Where(n => n.FightingIdentity.HasValue && n.FightingIdentity.Value == owner.Identity
                            && IsHostile(n, me, owner) && me.DistanceFrom(n) <= _ctx.Config.AssistMaxDistance)
                .OrderBy(me.DistanceFrom).FirstOrDefault();
            return LogTarget(fb, "fighting-owner-fallback");
        }

        // Log the assist target (and WHY) only when it changes, so combat problems are readable.
        private SimpleChar LogTarget(SimpleChar t, string src)
        {
            string key = t == null ? "none" : $"{t.Name}#{t.Identity.Instance}|{src}";
            if (key != _lastTargetLog)
            {
                _lastTargetLog = key;
                if (t != null) _ctx.Log($"TARGET: '{t.Name}' id={t.Identity.Instance} via {src} d={DynelManager.LocalPlayer?.DistanceFrom(t):0.0}");
                else _ctx.Log("TARGET: none (no owner fight)");
            }
            return t;
        }

        // Alive while it still has health. A mob's Health stat can be stale (not every hit updates the dynel),
        // so an unknown/absent Health reads as alive and we rely on despawn (removal from DynelManager) for
        // death; but a readable 0 HP means dead, so we stop committing to it and move to the next mob.
        private static bool IsAlive(SimpleChar c)
        {
            return !c.TryGetStat(Stat.Health, out int hp) || hp > 0;
        }

        // Not the owner, not the bot, not a teammate — safe to attack.
        public bool IsHostile(SimpleChar c, LocalPlayer me, PlayerChar owner)
        {
            if (c == null) return false;
            // The owner goes NULL the instant he leaves - a zone line, a lift, a travel terminal - and the
            // "hold the mob we are already on" path calls this with exactly that null. It threw on every tick
            // from the moment he stepped into a terminal until he came back. Nobody to compare against is not
            // a reason to fail; it only means that one identity check cannot be made.
            if (me != null && c.Identity == me.Identity) return false;
            if (owner != null && c.Identity == owner.Identity) return false;
            if (Team.Members.Any(m => m.Identity == c.Identity)) return false;
            return true;
        }
    }
}
