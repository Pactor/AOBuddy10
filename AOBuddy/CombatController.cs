using System;
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
        private bool _inCombat;
        private string _lastTargetLog = "";
        private double _noTargetLogAccum;

        public bool InCombat => _inCombat;

        public CombatController(BotContext ctx)
        {
            _ctx = ctx;
        }

        // Pick the owner's fight and, if it's a NEW target, issue the attack a single time. Returns
        // the target (null = no owner fight). Called each decision tick.
        public SimpleChar SelectAndEngage(LocalPlayer me, PlayerChar owner)
        {
            _sinceCombat += _ctx.Config.TickMs / 1000.0;
            SimpleChar target = GetAssistTarget(me, owner);
            if (target != null)
            {
                if (_attackedTarget != target.Identity)
                {
                    _ctx.Log($"ATTACK-> '{target.Name}' id={target.Identity} weapons={Inventory.Items.Count(x => x.Slot.Type == IdentityType.WeaponPage)}");
                    me.Attack(target);
                    _attackedTarget = target.Identity;
                }
                _inCombat = true;
                _sinceCombat = 0;

                FireReadySpecials(me, target);
            }
            else
            {
                _attackedTarget = null;   // fight over — the next target starts fresh, once
            }
            return target;
        }

        // Weapon SPECIAL attacks (Fast Attack, Brawl, Fling Shot, Burst, Full Auto, Aimed Shot, …) on top of
        // auto-attack. We fire ONLY specials the SERVER has told us this character has (me.KnownSpecials,
        // learned from SpecialUsed/SpecialAvailable — no hardcoding, works for any class/weapon), and only
        // when each is off cooldown. A special is a SEPARATE message that does NOT reset the main weapon swing
        // timer, so this cannot reintroduce the "never swings" bug. Snapshot the set first: firing registers a
        // cooldown, which touches the same collection we're iterating.
        private void FireReadySpecials(LocalPlayer me, SimpleChar target)
        {
            if (!_ctx.Config.UseSpecials) return;
            foreach (Stat special in me.KnownSpecials.ToArray())
            {
                if (!me.IsSpecialReady(special)) continue;
                me.PerformSpecialAttack(target.Identity, special);
                _ctx.Log($"SPECIAL-> {special} on '{target.Name}' id={target.Identity.Instance}");
            }
        }

        // Dump the EQUIPPED weapon(s) exactly as the item data describes them — every criteria (wield/attack
        // requirement: stat + operator + value) and every modifier — so we can read from DATA which weapon
        // SKILL he actually uses, instead of assuming. Equipped weapons are the Inventory items in the weapon
        // page (same source the attack log already uses). This is the ground truth the buff-relevance filter keys on.
        public int DumpWeapons(LocalPlayer me)
        {
            var weapons = Inventory.Items?.Where(x => x != null && x.Slot.Type == IdentityType.WeaponPage).ToList();
            if (weapons == null || weapons.Count == 0) { _ctx.Log("WEAPON: no equipped weapons in Inventory (not loaded yet?)."); return 0; }
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

        // Not the owner, not the bot, not a teammate — safe to attack.
        public bool IsHostile(SimpleChar c, LocalPlayer me, PlayerChar owner)
        {
            if (c == null) return false;
            if (c.Identity == me.Identity || c.Identity == owner.Identity) return false;
            if (Team.Members.Any(m => m.Identity == c.Identity)) return false;
            return true;
        }
    }
}
