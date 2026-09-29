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
            // The server's verdict on each special we send (see FireReadySpecials).
            Client.SpecialUsed += OnSpecialUsed;
            Client.SpecialAvailable += OnSpecialAvailable;
            Client.Feedback += OnSpecialFeedback;
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
            // ...but FINISH the one he is on first: switching to each new arrival left the last one alive and on him, and
            // they piled up (05:15:25-05:16:23, 2026-09-29: three Rhinoman Smashers / a Techwrecker, each hit and dropped
            // for the next, three on him, dead at the door). The owner: kill them as they come, don't drag them round.
            if (target == null && defend != null)
            {
                SimpleChar cur = _attackedTarget.HasValue ? DynelManager.Characters.FirstOrDefault(c => c.Identity == _attackedTarget.Value) : null;
                if (cur != null && cur.Identity != defend.Identity && !IsSetAside(cur.Identity) && IsAlive(cur) && IsHostile(cur, me, owner)
                    && me.DistanceFrom(cur) <= _ctx.Config.AssistMaxDistance)
                    target = cur;
                else
                    target = LogTarget(defend, "defending");
            }

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
                    // OPENER: a special that needs the target unaware (Sneak Attack, Aimed Shot) can only land
                    // before our first blow makes it aware - so it goes out BEFORE the one Attack, never after.
                    if (newTarget) FireReadySpecials(me, target, opening: true);
                    _ctx.Log($"ATTACK-> '{target.Name}' id={target.Identity} weapon={string.Join("+", EquippedWeapons().Select(w => w.Name))}{(resume ? $" (resume after {_notSwinging:0.0}s not swinging)" : "")}");
                    me.Attack(target);
                    _attackedTarget = target.Identity;
                    _notSwinging = 0;
                }
                _inCombat = true;
                _sinceCombat = 0;

                FireReadySpecials(me, target, opening: false);
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

        // ---- WEAPON SPECIALS: fired on the SERVER'S verdict, never on a guessed timer -------------------------
        //
        // What the server says (retail capture 20260923-114223 s4/s5, bot log 2026-09-28 20:18-20:20):
        //  * ACCEPTED: CharSecSpecAttack echo + CharacterAction 170 SpecialUsed(skill, recharge SECONDS) +
        //    SpecialAttackInfo. Then 164 SpecialAvailable(skill) when the recharge ends (Brawl 15 s -> 15.1 s later).
        //    Seen: Brawl 15, Fling Shot 23, Dimach 1800 (the recharge depends on weapon + skill; the server sends it).
        //  * REFUSED: only a Feedback 110 - no SpecialUsed. That is why the old guessed 2 s placeholder re-sent every
        //    refused special every ~2 s for the whole fight. The refusal ids (text.mdb via Tyrbot docs/mmdb.txt):
        private const int FbTargetAware = 90809749;       // "Special attack not possible. The target is aware of your presence."
        private const int FbWaitPrevious = 154558667;     // "Wait for your previous special attack to complete."
        private const int FbOutOfRange = 165509237;       // "The target is outside special attack range!"
        private const int FbMustFightOther = 38085045;    // "Special attack not possible. The target must be fighting someone else."
        private const int FbMustBeBehind = 244601803;     // "Special attack not possible. You must be behind the target."
        private const int FbBehindTarget = 164102084;     // "You must be behind the target!"
        private const int FbUnavailable = 22901477;       // "Special attack is unavailable."
        //
        // "Wait for your previous special attack to complete" has TWO causes on the wire:
        //  (a) another special was accepted a moment ago: s4 t=791.159 Brawl+Dimach sent together -> Brawl accepted,
        //      Dimach refused; Dimach alone at t=793.306 (1.96 s after Brawl's SpecialUsed) accepted. Bot log 20:19:50.645
        //      FastAttack accepted, Dimach refused; Dimach at 20:19:52.828 (2.18 s later) accepted.
        //  (b) that special is itself still recharging: s4 Dimach SpecialUsed 1800 at t=793.377, then every Dimach press
        //      for the rest of the capture (84x) got this id and no SpecialAvailable(144) ever came.
        // Hence: ONE special per server round-trip, a gap of SpecialGapSeconds after an accepted one (the shortest gap
        // seen to be accepted), and a (b)-refusal waits for the server's SpecialAvailable.
        private const double SpecialGapSeconds = 2.0;
        private const double VerdictTimeoutSeconds = 3.0;   // server answers in ~0.1-0.4 s (log); no answer = give up waiting
        private const double LockoutWindowSeconds = SpecialGapSeconds + VerdictTimeoutSeconds;
        // FALLBACK only: a special refused as "still recharging" whose recharge started before we could see it (e.g. used
        // before this login - Dimach 1800 s kept being refused after a restart, log 20:18:44). The server announces the
        // end with SpecialAvailable; if that never comes we re-probe with ONE packet this often instead of never again.
        private const double UnknownRechargeProbeSeconds = 120.0;
        // Out of range: retry only once we are this much closer than where it was refused (hysteresis, not a game rule).
        private const float RangeRetryCloserMeters = 1.0f;

        // Specials that need the target UNAWARE of us - usable only as the opener, before our first blow. Sneak Attack:
        // proven (every Sneak Attack on a mob already on him refused 90809749, log 20:18:44-20:20:10). Aimed Shot: NOT seen
        // on the wire yet; the same "unaware" rule is assumed as the SAFE option (it only ever narrows when we fire).
        private static readonly HashSet<Stat> OpenerSpecials = new HashSet<Stat> { Stat.SneakAttack, Stat.AimedShot };

        private Stat? _specPending;            // sent, verdict not in yet - nothing else goes out meanwhile
        private double _specSentAt;
        private Identity _specPendingTarget;
        private float _specSentDist;
        private double _specNextAt;            // no special before this (after an accepted one / an in-progress lockout)
        private double _lastSpecAcceptedAt = -999;
        private Identity? _specTarget;         // the target the per-target notes below are about
        private readonly Dictionary<Stat, string> _specBlockedOnTarget = new Dictionary<Stat, string>();   // condition refusals
        private readonly Dictionary<Stat, float> _specOutOfRangeAt = new Dictionary<Stat, float>();       // refused at this distance
        private readonly Dictionary<Stat, double> _specRechargeUntil = new Dictionary<Stat, double>();   // server recharge, kept across zones

        // Learned special ranges, per special AND weapon set: "Dimach|Illegally Modified Ofab Viper+..." -> ok (farthest
        // accepted), no (closest refused). Only ever from the server's own accept/refuse.
        private static readonly string RangeFile = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(typeof(CombatController).Assembly.Location) ?? ".", "special-ranges.json");
        private Newtonsoft.Json.Linq.JObject _ranges;
        private static string RangeKey(Stat s) => s + "|" + string.Join("+", EquippedWeapons().Select(w => w.Name).OrderBy(n => n));
        private (float ok, float no) LearnedRange(Stat s)
        {
            if (_ranges == null) _ranges = JsonStore.Load<Newtonsoft.Json.Linq.JObject>(RangeFile, _ctx.Log) ?? new Newtonsoft.Json.Linq.JObject();
            var e = _ranges[RangeKey(s)] as Newtonsoft.Json.Linq.JObject;
            return ((float?)e?["ok"] ?? 0f, (float?)e?["no"] ?? 0f);
        }
        private void NoteRange(Stat s, float dist, bool ok)
        {
            var (okAt, no) = LearnedRange(s);
            if (ok ? dist <= okAt : no > 0 && dist >= no) return;   // ok = farthest accepted (for the record), no = closest refused
            if (ok) okAt = dist; else no = dist;
            _ranges[RangeKey(s)] = new Newtonsoft.Json.Linq.JObject { ["ok"] = Math.Round(okAt, 1), ["no"] = Math.Round(no, 1) };
            JsonStore.Save(RangeFile, _ranges.ToString(), _ctx.Log);
        }
        private readonly Dictionary<Stat, double> _specWaitAvailable = new Dictionary<Stat, double>();    // -> next probe time

        private void FireReadySpecials(LocalPlayer me, SimpleChar target, bool opening)
        {
            if (!_ctx.Config.UseSpecials || me == null || target == null) return;
            double now = _ctx.Clock.Seconds;

            if (_specTarget != target.Identity)
            {
                _specTarget = target.Identity;
                _specBlockedOnTarget.Clear();
                _specOutOfRangeAt.Clear();
            }

            if (_specPending.HasValue)
            {
                if (now - _specSentAt < VerdictTimeoutSeconds) return;
                _ctx.Log($"SPECIAL: no verdict for {_specPending.Value} after {VerdictTimeoutSeconds:0} s - not waiting any longer.");
                _specPending = null;
            }
            if (now < _specNextAt) return;

            float dist = me.DistanceFrom(target);
            // Fire only the specials the EQUIPPED WEAPON allows (read from its own criteria) - at most ONE per round-trip.
            foreach (Stat special in AllowedWeaponSpecials().OrderBy(s => (int)s))
            {
                if (!me.IsSpecialReady(special)) continue;   // the server's SpecialUsed recharge is still running
                // ...and our own copy of it: every zone makes a new LocalPlayer, and its cooldowns go with the old one
                // (20:35:15, 2026-09-28: Dimach accepted with 300 s, two zones later re-sent after 97 s and refused).
                if (_specRechargeUntil.TryGetValue(special, out double until) && now < until) continue;
                if (_specWaitAvailable.TryGetValue(special, out double probeAt) && now < probeAt) continue;
                if (_specBlockedOnTarget.ContainsKey(special)) continue;
                if (_specOutOfRangeAt.TryGetValue(special, out float refusedAt) && dist > refusedAt - RangeRetryCloserMeters) continue;

                // Needs the target unaware: only as the opener, on a mob not fighting anyone yet. A mob already fighting
                // the owner or a pet may or may not be "aware" of HIM - the data can't tell, so the safe answer is don't.
                // Not only at the moment he picks the mob: that is usually from far off (20:28:39, 2026-09-28, Sneak
                // Attack at 14 m, refused out of range). Any time before the mob fights anyone, within the range the
                // server has not refused this special at (learned across mobs).
                if (OpenerSpecials.Contains(special) && target.FightingIdentity.HasValue) continue;
                // The range the server has shown for this special with these weapons (special-ranges.json, kept across
                // restarts): short of the closest distance it was refused at. A fresh login re-learned it metre by metre,
                // six refusals per mob (20:36:48, 2026-09-28). Not "within the farthest accepted": that only ever
                // shrinks (Fast Attack pinned to 1.1 m).
                var (_, refusedAt2) = LearnedRange(special);
                if (refusedAt2 > 0 && dist > refusedAt2 - RangeRetryCloserMeters) continue;
                // Backstab: "the target must be fighting someone else" (38085045). Whether we are "behind" it the
                // server decides; a behind-refusal blocks it on this target (see OnSpecialFeedback).
                if (special == Stat.Backstab && !(target.FightingIdentity.HasValue && target.FightingIdentity.Value != me.Identity)) continue;

                me.PerformSpecialAttack(target.Identity, special);
                _specPending = special;
                _specSentAt = now;
                _specPendingTarget = target.Identity;
                _specSentDist = dist;
                if (_specWaitAvailable.ContainsKey(special)) _specWaitAvailable[special] = now + UnknownRechargeProbeSeconds;
                _ctx.Log($"SPECIAL-> {special} on '{target.Name}' id={target.Identity.Instance} d={dist:0.0}{(opening ? " (opener)" : "")}");
                return;
            }
        }

        // ACCEPTED: the server started the special's real recharge.
        private void OnSpecialUsed(Stat stat, int seconds)
        {
            if (!WeaponSpecials.Contains(stat)) return;   // First Aid / Treatment / Level also arrive here
            double now = _ctx.Clock.Seconds;
            _specWaitAvailable.Remove(stat);
            _lastSpecAcceptedAt = now;
            _specNextAt = now + SpecialGapSeconds;
            _specRechargeUntil[stat] = now + seconds;
            if (_specPending == stat) NoteRange(stat, _specSentDist, ok: true);
            if (_specPending == stat) _specPending = null;
            _ctx.Log($"SPECIAL ok: {stat} - server recharge {seconds} s.");
        }

        private void OnSpecialAvailable(Stat stat)
        {
            if (!WeaponSpecials.Contains(stat)) return;
            _specRechargeUntil.Remove(stat);
            if (_specWaitAvailable.Remove(stat)) _ctx.Log($"SPECIAL: {stat} available again (server).");
        }

        // REFUSED: the Feedback 110 that answers the special we just sent. Nothing else goes out while one is pending,
        // so the refusal is that special's. (Dual-wield sends some refusals twice, one per hand - a repeat with nothing
        // pending is ignored.)
        private void OnSpecialFeedback(int category, int id)
        {
            if (category != 110 || !_specPending.HasValue) return;
            Stat special = _specPending.Value;
            double now = _ctx.Clock.Seconds;
            bool sameTarget = _specTarget.HasValue && _specTarget.Value == _specPendingTarget;
            string why;
            switch (id)
            {
                case FbTargetAware:
                    if (sameTarget) _specBlockedOnTarget[special] = "target aware";
                    why = "target is aware of him - not again on this mob";
                    break;
                case FbMustFightOther:
                    if (sameTarget) _specBlockedOnTarget[special] = "target not fighting someone else";
                    why = "target must be fighting someone else - not again on this mob";
                    break;
                case FbMustBeBehind:
                case FbBehindTarget:
                    // Combat never moves him, so he won't get behind it this fight.
                    if (sameTarget) _specBlockedOnTarget[special] = "not behind";
                    why = "must be behind the target - not again on this mob";
                    break;
                case FbOutOfRange:
                    if (sameTarget) _specOutOfRangeAt[special] = _specSentDist;
                    NoteRange(special, _specSentDist, ok: false);
                    why = $"out of special range at {_specSentDist:0.0} m - retry only closer";
                    break;
                case FbWaitPrevious:
                    if (now - _lastSpecAcceptedAt < LockoutWindowSeconds)
                    {
                        _specNextAt = now + SpecialGapSeconds;   // (a) another special still completing
                        why = "previous special still completing - wait";
                    }
                    else
                    {
                        _specWaitAvailable[special] = now + UnknownRechargeProbeSeconds;   // (b) its own recharge
                        why = "still recharging (from before we saw it) - wait for the server's SpecialAvailable";
                    }
                    break;
                case FbUnavailable:
                    _specWaitAvailable[special] = now + UnknownRechargeProbeSeconds;
                    why = "unavailable - wait for the server's SpecialAvailable";
                    break;
                default:
                    return;   // not a special verdict (loot, kill, ...) - keep waiting for the real one
            }
            _specPending = null;
            _ctx.Log($"SPECIAL refused: {special} - {why} (feedback 110/{id}).");
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
            // SOLO (no owner - the mission run) and THE PETS (Algorithman, 2026-09-26: "bot doesn't react if mobs
            // attack the pets"): this returned false with no owner and never looked at the pets, so a mob chewing on
            // a pet left him 'out of combat' and the blitz ran on through the room. A live mob (no owner) fighting
            // him or one of his pets counts, with or without an owner.
            if (me == null) return false;
            var pets = Guarded(me, owner);
            if (pets.Count > 0 || owner == null)
            {
                bool onUs = DynelManager.Npcs.Any(n => n != null && !n.Owner.HasValue && !pets.Contains(n.Identity)
                    && n.FightingIdentity.HasValue && (n.FightingIdentity.Value == me.Identity || pets.Contains(n.FightingIdentity.Value))
                    && (!n.TryGetStat(Stat.Health, out int hp) || hp > 0) && !IsSetAside(n.Identity));
                if (onUs) return true;
            }
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
            // Per-fight special notes go; the server-side recharge waits (_specWaitAvailable) are the character's, and stay.
            _specPending = null;
            _specTarget = null;
            _specBlockedOnTarget.Clear();
            _specOutOfRangeAt.Clear();
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

            // ANYTHING attacking a PET - the owner's or the bot's (Algorithman, 2026-09-26: "he stands still while the
            // droid wails on my healpet"). Pets are the NPCs whose Owner is the owner or the bot.
            var guarded = Guarded(me, owner);
            SimpleChar onPet = DynelManager.Characters
                .Where(c => c.FightingIdentity.HasValue && guarded.Contains(c.FightingIdentity.Value) && !guarded.Contains(c.Identity)
                            && IsHostile(c, me, owner) && IsAlive(c) && me.DistanceFrom(c) <= _ctx.Config.AssistMaxDistance)
                .OrderBy(me.DistanceFrom).FirstOrDefault();
            if (onPet != null) return LogTarget(onPet, "attacking-a-pet");

            // Fallback: nearest NPC fighting the OWNER specifically (covers a lagging FightingTarget
            // stat). MUST be scoped to the owner — using IsAttacking (attacking ANYONE) made the bot
            // jump into another player's fight just because a mob near the owner was in combat.
            SimpleChar fb = DynelManager.Npcs
                .Where(n => n.FightingIdentity.HasValue && n.FightingIdentity.Value == owner.Identity
                            && IsHostile(n, me, owner) && me.DistanceFrom(n) <= _ctx.Config.AssistMaxDistance)
                .OrderBy(me.DistanceFrom).FirstOrDefault();
            return LogTarget(fb, "fighting-owner-fallback");
        }

        /// <summary>The pets to defend: the bot's, and the owner's (NPCs whose Owner is him).</summary>
        public static HashSet<Identity> Guarded(LocalPlayer me, PlayerChar owner)
        {
            var g = new HashSet<Identity>(me.Pets.Select(p => p.Identity));
            foreach (var n in DynelManager.Npcs)
                if (n != null && n.Owner.HasValue && (n.Owner.Value == me.Identity || (owner != null && n.Owner.Value == owner.Identity))) g.Add(n.Identity);
            return g;
        }

        // Log the assist target (and WHY) only when it changes, so combat problems are readable.
        private SimpleChar LogTarget(SimpleChar t, string src)
        {
            string key = t == null ? "none" : $"{t.Name}#{t.Identity.Instance}|{src}";
            if (key != _lastTargetLog)
            {
                _lastTargetLog = key;
                if (t != null) _ctx.Log($"TARGET: '{t.Name}' lvl {(t.TryGetStat(Stat.Level, out int tl) ? tl : 0)} id={t.Identity.Instance} via {src} d={DynelManager.LocalPlayer?.DistanceFrom(t):0.0}");
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
