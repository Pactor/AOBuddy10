using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AOSharp.Clientless;
using AOSharp.Common.GameData;

namespace AOBuddy
{
    public struct CastRequest { public Identity Target; public bool OnSelf; public int NanoId; public string Label; }

    /// <summary>
    /// SUPPORT — everything that keeps the two of you alive and buffed, none of which moves the bot:
    /// emergency stims (owner AND self, in combat), out-of-combat rest on a recharger (sit), healing
    /// auras/HoTs kept running, a cast queue for commanded buffs/heals, self-HP/nano prediction
    /// between the sparse server updates, and low-supply warnings.
    ///
    /// Stims are a combat heal (they restore HP and nano and can be used mid-fight); the recharger
    /// rest is out-of-combat only and must sit. Neither ever issues a combat packet, so healing
    /// never disturbs the weapon swing that COMBAT started.
    /// </summary>
    public class SupportController
    {
        private readonly BotContext _ctx;
        private readonly Movement _move;

        private readonly Queue<CastRequest> _castQueue = new Queue<CastRequest>();

        // Predicted self HP/nano (server sends these sparsely; we predict between updates and snap
        // to any fresh server value the instant one arrives).
        private double _predHp = -1, _predNano = -1;
        private int _lastRawHp = -1, _lastRawNano = -1;

        // HEAL-ITEM SKILL LOCKS. Using a heal item locks a skill server-side (the item data's OnUse LockSkill:
        // stim -> First Aid 40s, recharger -> Treatment 15s). The server announces that lock as a SpecialUsed
        // on the skill stat and lifts it with SpecialAvailable, so it lands in me.Cooldowns like a weapon
        // special — that is the authority on whether the item can be used. HP/nano readback can't judge it:
        // the server sends vitals sparsely, so "did HP rise a beat later?" read as "no" and the old learner
        // kept re-firing a locked stim (each one a retarget that pauses the swing, for no heal). Until the
        // server's lock arrives we hold our own timer from the item data, so a late packet can't let us re-fire.
        private readonly Dictionary<Stat, double> _lockUntil = new Dictionary<Stat, double>();  // skill -> sessionSeconds usable again
        private readonly HashSet<Stat> _lockSeen = new HashSet<Stat>();   // server lock observed since our last use

        // A heal item we have SENT but not yet seen the server accept. Using an item is a request, not an
        // outcome: the server refuses it outright when the character is in combat, and says so only by
        // NOT locking the skill. Six rechargers were once logged as "used" while every one was rejected and
        // nano never moved, which is what made that failure take a packet capture to find.
        //
        // Accepting shows up as a skill lock (stim -> First Aid, recharger -> Treatment) arriving in
        // me.Cooldowns within a beat. So: hold a short window after each use, then decide.
        private readonly Dictionary<Stat, double> _healUseSentAt = new Dictionary<Stat, double>();
        private readonly Dictionary<Stat, string> _healUseItem = new Dictionary<Stat, string>();
        // The posture and combat state AT THE MOMENT OF THE USE, so a refusal can name its likely cause rather
        // than always blaming combat. A sit-only item used while standing is refused just as flatly.
        private readonly Dictionary<Stat, string> _healUseState = new Dictionary<Stat, string>();
        private bool _lastKnownInCombat;

        // THE SIT / USE / CONFIRM / STAND CYCLE. A recharger is a sit-only item and the server checks posture
        // when it PROCESSES the use, not when we send it - so standing straight after the send got every use
        // refused. We stay seated until the server has actually answered, then stand. _restUseStat is the skill
        // that use locks (Treatment for a recharger, First Aid for a stim), which is how we recognise the answer.
        private bool _awaitingRestUseConfirm;
        private bool _restUseConfirmed;
        private bool _restUseRefused;
        private Stat _restUseStat;

        // How long to wait for the server's lock before calling the use refused. Comfortably longer than the
        // gap actually observed (0.3s and 1.0s on accepted stims) without being long enough to stall.
        private const double HealVerifySeconds = 3.0;

        // How long to wait before retrying after a REFUSED use. Short, because a refusal costs nothing and
        // the blocking condition (combat) can lift at any moment - but not zero, so we do not spin.
        private const double HealRefusedRetrySeconds = 4.0;

        /// <summary>True while a heal item we sent is still unconfirmed.</summary>
        public bool HealUsePending => _healUseSentAt.Count > 0;

        private double _healCd;           // cooldown so emergency heals don't fire every tick
        private double _sessionSeconds;   // monotonic clock for aura recast rate-limiting
        private double _auraAccum;
        private readonly Dictionary<int, double> _auraLastCast = new Dictionary<int, double>();
        private double _rechargeAccum;

        // Auto-buff: learned nanos classified as keep-up buffs (rebuilt when SpellList changes).
        private sealed class BuffPlan { public int NanoId; public NanoItem Nano; public bool Self; public NanoLine Line; public int Ncu; }
        private List<BuffPlan> _buffPlans;
        private HashSet<int> _coverers;   // composite buffs that bundle single buffs' effect (cast these, not the singles)
        private readonly Dictionary<string, double> _lastBuffCastAt = new Dictionary<string, double>();  // (target:nano) -> sessionSeconds
        private int _lastSpellCount = -1;

        // Buffs that DON'T land on the owner (self-only auras etc.) — learned by verifying the cast landed,
        // and PERSISTED so we never waste a cast on the owner again, even across relogs.
        private HashSet<int> _ownerNoLand;
        private readonly Dictionary<int, double> _ownerVerify = new Dictionary<int, double>();  // nanoId -> sessionSeconds cast on owner
        private const double OwnerVerifyGraceSec = 20.0;
        private const string OwnerNoLandFile = "buff_owner_noland.txt";
        private double _buffScanAccum;
        private const double BuffScanSec = 2.0;
        private double _buffGrace = 10.0;   // wait for ActiveNanos to load after login before the first scan
        private const double BuffZoneGrace = 8.0;
        private double _nanoRefillAccum;
        private double _refillElapsed;             // time spent recharging for a queued cast this sit
        private int _refillPeakNano;              // highest nano% seen while refilling (progress tracking)
        private double _refillLastGainAt;         // _refillElapsed when nano last climbed

        private bool _sitting;
        private bool _resting;
        private double _restElapsed;
        private int _restPeakHp, _restPeakNano;   // highest HP/nano seen this sit (progress tracking)
        private double _restLastGainAt;           // _restElapsed when HP or nano last climbed
        private double _restCooldown;
        private double _restZoneSuppress; // after a zone, HP/nano read stale-low until the next full update
        private bool _restedSinceCombat;  // rest once per lull — no re-sitting until the next fight

        private double _supplyAccum;
        private bool _warnedStim, _warnedRecharger;
        private bool _everHadStims, _everHadRechargers;   // don't false-warn "OUT" before bags load

        // Which heal items he can use is worked out from the item's own use requirement against his live
        // skill - see HealItemPool and MeetsHealReqs. Nothing is blacklisted on a refusal: a buff raises First
        // Aid and Treatment, so an item refused a moment ago may be usable now.
        private static string HealKey(Item it) => (it == null ? "?" : it.Name) + "#" + (it == null ? 0 : it.Ql);

        private double _ownerSpeed;
        private Vector3? _ownerTickPos;

        // WHEN THE OWNER LAST ACTUALLY MOVED. His dynel position only updates when the server broadcasts it,
        // which is far less often than our tick - so a per-tick "distance / tick length" speed reads 0 on most
        // ticks and spikes absurdly (28, 59 u/s) on the ticks a broadcast lands. Anything gated on that speed
        // being low was therefore ALWAYS true, including the guard meant to stop the bot sitting down to
        // recharge while the owner is running: he sat eight times in thirty seconds while being led to an exit.
        // Time-since-he-last-moved is immune to the sampling rate, so that is what the guard uses.
        private double _ownerLastMovedAt;
        private const double OwnerStillSeconds = 1.5;

        // A RUN of rest cycles, tracked across the sit/stand boundary (see RestTick). Reset by a real gain or
        // by a fight; if neither happens the bot backs off rather than sitting on the spot forever.
        // The server's echo of our sit (Client.PostureToggled, action 0x57) — the only trustworthy statement
        // that the character is really seated, since Stat.CurrentMovementMode never updates after login.
        private bool _seatConfirmed;
        private bool _seatFallbackLogged;
        private const double SeatFallbackSeconds = 1.5;

        // AFTER A USE THE SERVER TOOK, WAIT TO SEE IT. Health and nano arrive as sparse stat updates, so for
        // several seconds after a heal the percentages still read exactly what they did before it - and a bot
        // that re-decides on those numbers concludes it still needs healing and takes another item. Because a
        // stim and a recharger lock DIFFERENT skills (First Aid, Treatment), the other one is always ready, so
        // it alternates: five items used in ninety seconds where one was enough. Hold until a raw stat
        // actually moves, or until the wait runs out, whichever comes first.
        private double _postUseHoldUntil;
        private int _postUseRawHp = -1, _postUseRawNano = -1;
        private const double PostUseSettleSeconds = 10.0;

        private double _restRunSince;
        private int _restRunPeakHp = -1, _restRunPeakNano = -1;
        private double _restSuppressUntil;
        private const double RestRunNoGainSeconds = 25.0;
        private const double RestRunBackoffSeconds = 60.0;

        public bool Resting => _resting;
        public bool Sitting => _sitting;
        public double OwnerSpeed => _ownerSpeed;
        /// <summary>Seconds since the owner's position last changed (large = he is standing still).</summary>
        public double OwnerStillFor => _sessionSeconds - _ownerLastMovedAt;
        public bool HasQueuedCasts => _castQueue.Count > 0;

        public SupportController(BotContext ctx, Movement move)
        {
            _ctx = ctx;
            _move = move;
        }

        private double _lastCastAt = -999;   // sessionSeconds of the last cast queued/fired — rest yields to casting
        public void QueueCast(CastRequest req) { _castQueue.Enqueue(req); _lastCastAt = _sessionSeconds; }
        /// <summary>Seconds since a cast was last queued or fired (buffs, heals, pet summons).</summary>
        public double SecondsSinceCast => _sessionSeconds - _lastCastAt;

        // Per decision-tick clocks.
        public void AdvanceClocks()
        {
            _healCd += _ctx.Config.TickMs / 1000.0;
            _sessionSeconds += _ctx.Config.TickMs / 1000.0;
        }

        // Measure the owner's movement against the broadcasts we actually get, not against our tick. A position
        // that has not changed is not evidence he stopped - it usually just means no broadcast arrived this
        // tick - so speed is distance over the time since he LAST moved, and "stopped" is only concluded after
        // he has been in the same place for a while. See _ownerLastMovedAt.
        /// <summary>The server confirmed our sit/stand actually took effect. While we are trying to rest, that
        /// means we are now genuinely seated and a sit-only item can be used.</summary>
        public void OnPostureToggled()
        {
            if (_sitting) _seatConfirmed = true;
        }

        public void UpdateOwnerSpeed(PlayerChar owner)
        {
            if (owner == null) return;
            Vector3 op = owner.Transform.Position;

            if (!_ownerTickPos.HasValue) { _ownerLastMovedAt = _sessionSeconds; _ownerTickPos = op; return; }

            float moved = Vector3.Distance(op, _ownerTickPos.Value);
            if (moved > 0.05f)
            {
                double since = Math.Max(0.05, _sessionSeconds - _ownerLastMovedAt);
                _ownerSpeed = moved / since;
                _ownerLastMovedAt = _sessionSeconds;
            }
            else if (_sessionSeconds - _ownerLastMovedAt > OwnerStillSeconds)
            {
                _ownerSpeed = 0;
            }

            _ownerTickPos = op;
        }

        // Queued casts (auto/manual buffs, heals) first, when free to cast. If we can't afford the next
        // cast's nano, refill (recharger/stim) instead of casting — this drives the fluid buff sequence.
        // Returns true if it handled the tick (cast OR refilling), so Main holds while we buff.
        // Resurrection Sickness (the debuff after a reclaim) blocks using stims/rechargers AND casting.
        // While it's up the bot must not try to heal/rest/cast — and crucially must NOT run the no-heal
        // blacklist (a stim/recharger failing here is rez-sickness, not a bad item). It's a named nano in
        // me.Buffs; when it expires, everything resumes on its own.
        public static bool IsRezSick(LocalPlayer me)
        {
            var buffs = me?.Buffs;
            if (buffs == null) return false;
            foreach (var b in buffs)
            {
                string n = b.NanoItem != null ? b.NanoItem.Name : null;
                if (n != null && n.IndexOf("Resurrection Sickness", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        public bool TryDrainCast(LocalPlayer me, bool manualActive, bool inCombat)
        {
            if (me.IsCasting || manualActive || _castQueue.Count == 0) return false;
            if (IsRezSick(me)) return false;   // can't cast / refill through rez sickness

            CastRequest req = _castQueue.Peek();
            if (!CanAfford(me, req.NanoId))
            {
                NanoRefillForCast(me, req, inCombat);   // sit/stim to top up, or drop if impossible
                return true;                            // hold this tick; cast lands once nano is back
            }

            StandIfSitting(me);
            _resting = false;   // if we were seated recharging for this cast, we're standing to cast now
            DoCast(me, _castQueue.Dequeue());
            return true;
        }

        // Keep configured aura / HoT buffs running (e.g. a Keeper's Adaptive Ambient Restoration).
        // We RE-CAST each aura on a fixed interval (just under its duration) so it's always up; one
        // aura per pass, never mid-swing.
        public void KeepAuras(LocalPlayer me)
        {
            if (_ctx.Config.HealAuraNanoIds == null || _ctx.Config.HealAuraNanoIds.Count == 0) return;
            _auraAccum += _ctx.Config.TickMs / 1000.0;
            if (_auraAccum < 3.0) return;
            _auraAccum = 0;
            if (me.IsAttacking) return;   // don't interrupt a swing to refresh an aura
            foreach (int id in _ctx.Config.HealAuraNanoIds)
            {
                if (id <= 0) continue;
                Buff buff = me.Buffs?.FirstOrDefault(b => b.Id == id);
                bool needs;
                if (buff != null)
                    needs = (buff.Cooldown?.RemainingTime ?? 0) <= _ctx.Config.AuraRefreshMargin;
                else
                    needs = !_auraLastCast.TryGetValue(id, out double last) || (_sessionSeconds - last) >= _ctx.Config.AuraRecastSeconds;
                if (needs)
                {
                    _castQueue.Enqueue(new CastRequest { OnSelf = true, NanoId = id, Label = "aura" });
                    _auraLastCast[id] = _sessionSeconds;
                }
            }
        }

        // ---- AUTO-BUFF (nano keep-up) --------------------------------------------
        // Auto-detected from LEARNED nanos (me.SpellList): a keep-up BUFF = takes NCU + beneficial effect +
        // castable on a friendly. Each scan, find the first buff that's missing / about to expire on a valid
        // target and QUEUE it (one per pass); TryDrainCast then casts it, refilling nano as needed. Out of
        // combat only (unless BuffInCombat). No hardcoded nano ids — works for anything he ever learns.
        // True while buffs are queued/in flight — pet summoning yields to this so the nano-skill buff CHAIN can
        // finish (and make the pets castable) instead of the two fighting over the cast slot.
        public bool HasPendingCasts => _castQueue != null && _castQueue.Count > 0;

        public void KeepBuffs(LocalPlayer me, PlayerChar owner, bool inCombat)
        {
            if (!_ctx.Config.AutoBuff) return;
            if (inCombat && !_ctx.Config.BuffInCombat) return;
            // Rez sickness: the server refuses casts, but a queued buff still marked itself as cast, and a buff
            // just 'cast' is not tried again for most of its duration - so buffs queued while sick never went
            // back on (2026-09-23 22:16: Vengeance of the Loyal and Impartiality of the Blade). Wait it out,
            // and when it ends forget what was 'cast' during it.
            if (IsRezSick(me)) { _wasRezSick = true; return; }
            if (_wasRezSick) { _wasRezSick = false; _lastBuffCastAt.Clear(); _castQueue.Clear(); _ctx.Log("AUTO-BUFF: rez sickness over - rebuffing everything missing."); }

            // Startup/zone grace: right after login (or a zone) the server hasn't sent our ActiveNanos yet,
            // so me.Buffs is momentarily empty and an already-running buff would look "missing" and get
            // needlessly recast (seen: recasting a 4-hour self buff on every relog). Wait for the buff list
            // to arrive before the first scan.
            if (_buffGrace > 0) { _buffGrace -= _ctx.Config.TickMs / 1000.0; return; }

            _buffScanAccum += _ctx.Config.TickMs / 1000.0;
            if (_buffScanAccum < BuffScanSec) return;
            _buffScanAccum = 0;
            if (me.IsCasting || _castQueue.Count > 0) return;   // one buff in flight at a time

            // Never let a quirky nano's data take down the whole update loop — auto-buff is optional.
            try { ScanAndQueueBuffs(me, owner); }
            catch (Exception ex) { _ctx.Config.AutoBuff = false; _ctx.Log($"AUTO-BUFF disabled after error: {ex.Message}"); }
        }

        private void ScanAndQueueBuffs(LocalPlayer me, PlayerChar owner)
        {
            EnsureNoLandLoaded();
            VerifyOwnerLandings(owner);
            RebuildBuffPlansIfNeeded(me);
            if (_buffPlans.Count == 0) return;

            foreach (BuffPlan plan in _buffPlans)
            {
                // Per target, cast the BEST buff in each line the target can be given RIGHT NOW. Castability is
                // checked LIVE (IsBestCastableInLine -> SafeMeetsReqs), so as skills rise — his own earlier
                // buffs, or the owner buffing him — a better version becomes castable and he upgrades. THE CHAIN.
                if (_ctx.Config.BuffSelf && IsBestCastableInLine(plan, me) && NeedsBuff(me, plan))
                {
                    _castQueue.Enqueue(new CastRequest { OnSelf = true, NanoId = plan.NanoId, Label = "buff self" });
                    _lastBuffCastAt[BuffKey(me, plan.NanoId)] = _sessionSeconds;
                    _ctx.Log($"AUTO-BUFF queue self: {BuffName(plan)} [{plan.NanoId}]");
                    return;
                }
                if (_ctx.Config.BuffOwner && owner != null && !_ownerNoLand.Contains(plan.NanoId)
                    && IsBestCastableInLine(plan, owner) && NeedsBuff(owner, plan))
                {
                    _castQueue.Enqueue(new CastRequest { Target = owner.Identity, NanoId = plan.NanoId, Label = "buff owner" });
                    _lastBuffCastAt[BuffKey(owner, plan.NanoId)] = _sessionSeconds;
                    _ownerVerify[plan.NanoId] = _sessionSeconds;   // check ~20s later that it actually landed
                    _ctx.Log($"AUTO-BUFF queue owner: {BuffName(plan)} [{plan.NanoId}] | ownerBuffs={owner.Buffs?.Count ?? -1}");
                    return;
                }
                if (_ctx.Config.BuffTeam)
                {
                    foreach (TeamMember tm in Team.Members)
                    {
                        SimpleChar mc = DynelManager.Characters.FirstOrDefault(x => x.Identity == tm.Identity);
                        if (mc == null || mc.Identity == me.Identity) continue;
                        if (IsBestCastableInLine(plan, mc) && NeedsBuff(mc, plan))
                        {
                            _castQueue.Enqueue(new CastRequest { Target = mc.Identity, NanoId = plan.NanoId, Label = "buff team" });
                            _lastBuffCastAt[BuffKey(mc, plan.NanoId)] = _sessionSeconds;
                            _ctx.Log($"AUTO-BUFF queue team {mc.Name}: {BuffName(plan)} [{plan.NanoId}]");
                            return;
                        }
                    }
                }
            }
        }

        // The preferred buff in a line FOR THIS TARGET = the best one the target can be given right now. Only
        // considers versions castable ON the target (SafeMeetsReqs, checked LIVE each scan), so rising skills
        // promote a stronger/longer version — the buff chain, and re-evaluated when the owner buffs him too.
        private bool IsBestCastableInLine(BuffPlan plan, SimpleChar target)
        {
            bool self = DynelManager.LocalPlayer != null && target.Identity == DynelManager.LocalPlayer.Identity;
            // A COVERER (composite that bundles single buffs, e.g. Composite Mochams) reads MeetsUseReqs=False for
            // the same reason pet summons do — the offline check under-reports it — but the user confirms he can
            // cast it. Trust it on SELF and let the server decide; the strict check still gates buffs on others.
            Func<BuffPlan, bool> castable = p => SafeMeetsReqs(p.Nano, target) || (self && IsCoverer(p));
            if (!castable(plan)) return false;
            if (plan.Line == NanoLine.NOSTACKING) return true;   // independent line — cast it if castable
            BuffPlan best = null;
            foreach (BuffPlan p in _buffPlans)
            {
                if (p.Line != plan.Line || !castable(p)) continue;
                if (best == null || RankInLine(p) > RankInLine(best)) best = p;
            }
            return best != null && best.NanoId == plan.NanoId;
        }

        private bool IsCoverer(BuffPlan plan) => _coverers != null && _coverers.Contains(plan.NanoId);

        // Use-stat modifiers of a nano as stat->value (its effect map). Empty if it has none.
        private static Dictionary<Stat, int> UseMods(NanoItem ni)
        {
            if (ni?.Modifiers != null && ni.Modifiers.TryGetValue(SpellListType.Use, out var use) && use != null)
                return new Dictionary<Stat, int>(use);
            return new Dictionary<Stat, int>();
        }

        // Re-classify only when the learned set changed (he uploaded/removed a nano).
        private void RebuildBuffPlansIfNeeded(LocalPlayer me)
        {
            int[] spells = me.SpellList ?? new int[0];
            if (_buffPlans != null && spells.Length == _lastSpellCount) return;
            _lastSpellCount = spells.Length;

            var plans = new List<BuffPlan>();
            foreach (int id in spells)
            {
                if (_ctx.Config.ExcludeNanoIds != null && _ctx.Config.ExcludeNanoIds.Contains(id)) { _ctx.Log($"AUTO-BUFF skip [{id}]: excluded by config."); continue; }
                if (!ItemData.Find(id, out NanoItem ni) || ni == null) { _ctx.Log($"AUTO-BUFF skip [{id}]: no nano data (not in item DB)."); continue; }
                string nm = ni.Name ?? "?";
                // NOT keep-up buffs: pet SUMMONS + pet-target buffs (the pet controller owns those), and
                // VEHICLES / nukes / recalls / summoned weapons — those have UNMAPPED numeric NanoLines and must
                // never be auto-cast: casting a vehicle mounts the char and blocks ALL casting (the "boogy
                // board" bug). Real keep-up buffs all have a named NanoLine.
                if (IsNotAKeepUpBuff(ni.NanoLine)) { _ctx.Log($"AUTO-BUFF skip '{nm}' [{id}]: line={ni.NanoLine} — pet/vehicle/nuke, not a keep-up buff."); continue; }
                bool self = SafeMeetsReqs(ni, me);
                int net = NetUseMod(ni);
                // Keep-up buff = lasting (NCU>0) and NOT a debuff. We do NOT require positive Use mods:
                // HoTs/auras (e.g. Adaptive Ambient Restoration) heal over time with NO stat modifiers, so
                // requiring positive mods wrongly dropped them. Only reject a nano whose Use mods are clearly
                // NEGATIVE (a debuff). Who it's actually cast on is decided later by the friendly-target
                // probe (MeetsUseReqs), so a hostile nano can never land on a friendly regardless.
                if (ni.NCU <= 0) { _ctx.Log($"AUTO-BUFF skip '{nm}' [{id}]: NCU={ni.NCU} (not a lasting buff). self={self} netMod={net}"); continue; }
                if (net != int.MinValue && net < 0) { _ctx.Log($"AUTO-BUFF skip '{nm}' [{id}]: debuff (netMod={net}). NCU={ni.NCU} self={self}"); continue; }
                plans.Add(new BuffPlan { NanoId = id, Nano = ni, Self = self, Line = ni.NanoLine, Ncu = ni.NCU });
                _ctx.Log($"AUTO-BUFF keep '{nm}' [{id}]: NCU={ni.NCU} netMod={net} self={self} line={ni.NanoLine}");
            }
            // Bundled-single suppression: a COMPOSITE buff provides several single buffs' exact effect in one
            // nano (verified in the log: 'Composite Mochams' gives all six nano skills at +140 — exactly what the
            // six single 'Mocham's Gift' give, at ~50 NCU each). Drop a single B when another known buff C
            // modifies a STRICT SUPERSET of B's stats at the SAME value on each: B then adds nothing, so we cast
            // only the composite ("least NCU, more buffs"). The value must be EQUAL, so Expertise/Teachings
            // (+20/+25 — different tier, they STACK) are never suppressed. No hardcoded ids — pure effect data.
            // Same rule also folds the single nano Expertise buffs into 'Composite Nano Expertise', etc.
            var mods = plans.ToDictionary(p => p.NanoId, p => UseMods(p.Nano));
            var suppressed = new HashSet<int>();
            var coverers = new HashSet<int>();
            foreach (BuffPlan b in plans)
            {
                var mb = mods[b.NanoId];
                if (mb.Count == 0) continue;
                foreach (BuffPlan c in plans)
                {
                    if (c.NanoId == b.NanoId) continue;
                    var mc = mods[c.NanoId];
                    if (mc.Count <= mb.Count) continue;   // C must bundle strictly MORE stats than B
                    bool exactCover = true;
                    foreach (var kv in mb)
                        if (!mc.TryGetValue(kv.Key, out int cv) || cv != kv.Value) { exactCover = false; break; }
                    if (exactCover)
                    {
                        suppressed.Add(b.NanoId); coverers.Add(c.NanoId);
                        _ctx.Log($"AUTO-BUFF suppress '{b.Nano?.Name}' [{b.NanoId}] — bundled by '{c.Nano?.Name}' [{c.NanoId}] (cast the composite, not the single).");
                        break;
                    }
                }
            }
            _coverers = coverers;
            _buffPlans = plans.Where(p => !suppressed.Contains(p.NanoId)).ToList();
            _ctx.Log($"AUTO-BUFF: classified {_buffPlans.Count} keep-up buffs from {spells.Length} learned nanos ({suppressed.Count} single(s) suppressed by composites).");
        }

        // A target needs this buff if it's missing (and nothing better of the same line is up, and it fits
        // NCU) or it's within RebuffMargin of expiring.
        private const double MinRecastSeconds = 120;   // hard floor between recasts of the same buff on a target

        private bool NeedsBuff(SimpleChar target, BuffPlan plan)
        {
            // Is it ALREADY UP? Match by exact id OR by same NanoLine with equal/greater StackingOrder — because a
            // composite's LANDED buff often has a different id than the cast nano (why 'Composite Utility Expertise'
            // recast every ~120s while up, but 'Composite Mochams' — which lands under its own id — did not). If up,
            // only recast when the server reports a real remaining time that's about to expire; a present buff with
            // NO timer info (common right after login) is UP and must NEVER be recast (that wasted mana each login).
            Buff active = FindActiveBuff(target, plan);
            if (active != null)
            {
                double rt = active.Cooldown?.RemainingTime ?? -1;
                if (rt < 0) return false;                                   // present, timer unknown => up, leave it
                bool need = rt < _ctx.Config.RebuffMarginSeconds;
                if (need) _ctx.Log($"AUTO-BUFF: '{BuffName(plan)}' [{plan.NanoId}] expiring ({rt:0}s left) — refresh.");
                return need;
            }

            // Not in the target's buff list. For OTHERS (owner/team) that is usually just STALENESS — the
            // server doesn't stream us their buff timers — so don't spam-recast: if we cast this on this
            // target recently, assume it's still up until an assumed-duration window elapses. The duration
            // is proxied from OUR OWN copy of the same buff (reliable, e.g. a 4h ambient), else a fallback.
            if (_lastBuffCastAt.TryGetValue(BuffKey(target, plan.NanoId), out double last))
            {
                double window = AssumedBuffDuration(plan.NanoId) - _ctx.Config.RebuffMarginSeconds;
                if (window < MinRecastSeconds) window = MinRecastSeconds;   // HARD FLOOR: never spam-recast. An
                // untrackable aura/HoT (no Use mods, never appears in Buffs) would otherwise re-queue every tick
                // forever (seen: 587x 'Adaptive Ambient Restoration'). One cast then wait at least this long.
                if (_sessionSeconds - last < window) return false;
            }

            if (HasBetterSameLine(target, plan)) return false;   // don't downgrade a stronger same-line buff
            if (!NcuFits(target, plan)) return false;            // won't fit — skip (can't cast anyway)
            return true;
        }

        // Find the buff already up on the target that satisfies this plan: exact cast id, or (for a stacking line)
        // any buff of the SAME NanoLine with equal/greater StackingOrder — the game's real stacking identity, which
        // is how we recognise a composite that landed under a different id, and how we avoid downgrading a stronger
        // same-line buff. NOSTACKING (independent) lines can only be matched by exact id.
        private static Buff FindActiveBuff(SimpleChar target, BuffPlan plan)
        {
            if (target.Buffs == null) return null;
            Buff byId = target.Buffs.FirstOrDefault(b => b.Id == plan.NanoId);
            if (byId != null || plan.Line == NanoLine.NOSTACKING) return byId;
            return target.Buffs.FirstOrDefault(b => b.NanoItem != null
                && b.NanoItem.NanoLine == plan.Line && b.NanoItem.StackingOrder >= plan.Nano.StackingOrder);
        }

        // How long we assume a buff we just cast will last on a target: our own copy's remaining time if we
        // have it (same nano = same duration), else the config fallback.
        private double AssumedBuffDuration(int nanoId)
        {
            LocalPlayer me = DynelManager.LocalPlayer;
            Buff mine = me?.Buffs?.FirstOrDefault(b => b.Id == nanoId);
            double rt = mine?.Cooldown?.RemainingTime ?? 0;
            return rt > 1 ? rt : _ctx.Config.BuffAssumeSeconds;
        }

        private static string BuffKey(SimpleChar target, int nanoId) => target.Identity.Instance + ":" + nanoId;

        public void OnDeathResetBuffs() { _lastBuffCastAt.Clear(); _castQueue.Clear(); }   // buffs drop on death — allow rebuff after reclaim
        private bool _wasRezSick;

        // ---- Owner-land learning (which buffs actually apply to the owner) -------
        private void EnsureNoLandLoaded()
        {
            if (_ownerNoLand != null) return;
            _ownerNoLand = new HashSet<int>();
            try
            {
                if (File.Exists(OwnerNoLandFile))
                    foreach (string tok in File.ReadAllText(OwnerNoLandFile).Split(new[] { ',', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                        if (int.TryParse(tok, out int id)) _ownerNoLand.Add(id);
            }
            catch { }
        }

        private void SaveNoLand()
        {
            try { File.WriteAllText(OwnerNoLandFile, string.Join(",", _ownerNoLand)); } catch { }
        }

        // A short while after we cast a buff on the owner, confirm it actually landed in his buff list. If
        // it didn't (a self-only aura), remember it as owner-no-land so we never cast it on him again.
        private void VerifyOwnerLandings(PlayerChar owner)
        {
            if (_ownerVerify.Count == 0 || owner == null) return;
            List<int> done = null;
            foreach (var kv in _ownerVerify)
            {
                if (_sessionSeconds - kv.Value < OwnerVerifyGraceSec) continue;
                if (done == null) done = new List<int>();
                done.Add(kv.Key);
                bool landed = owner.Buffs?.Any(b => b.Id == kv.Key) ?? false;
                if (!landed)
                {
                    EnsureNoLandLoaded();
                    if (_ownerNoLand.Add(kv.Key))
                    {
                        SaveNoLand();
                        _ctx.Log($"AUTO-BUFF: [{kv.Key}] did not land on the owner (self-only) — won't cast it on him again.");
                    }
                }
            }
            if (done != null) foreach (int id in done) _ownerVerify.Remove(id);
        }

        private bool HasBetterSameLine(SimpleChar target, BuffPlan plan)
        {
            if (target.Buffs == null || plan.Line == NanoLine.NOSTACKING) return false;   // NOSTACKING = independent line
            foreach (Buff b in target.Buffs)
            {
                NanoItem bn = b.NanoItem;
                if (bn != null && bn.NanoLine == plan.Line && bn.StackingOrder >= plan.Nano.StackingOrder && b.Id != plan.NanoId)
                    return true;
            }
            return false;
        }

        private static bool NcuFits(SimpleChar target, BuffPlan plan)
        {
            if (!target.TryGetStat(Stat.MaxNCU, out int maxNcu) || maxNcu <= 0) return true;   // unknown -> let server decide
            target.TryGetStat(Stat.CurrentNCU, out int usedNcu);
            return usedNcu + plan.Ncu <= maxNcu;
        }

        // Sum of the nano's Use-stat modifiers (its effect). int.MinValue = no Use modifiers at all.
        private static int NetUseMod(NanoItem ni)
        {
            if (ni.Modifiers == null || !ni.Modifiers.TryGetValue(SpellListType.Use, out var use) || use == null || use.Count == 0)
                return int.MinValue;
            long net = 0; foreach (int v in use.Values) net += v;
            return net > int.MaxValue ? int.MaxValue : (net < int.MinValue + 1 ? int.MinValue + 1 : (int)net);
        }

        // Same-line choice (e.g. an HP buff vs a Nano buff of one line): keep up only the PREFERRED one.
        // Preference (data-driven, no ids): a buff that raises Health/MaxHealth beats one that doesn't
        // (user rule: always want the HP buff), then higher StackingOrder, then higher QL. NOSTACKING
        // (independent) buffs are always kept.
        private bool IsPreferredInLine(BuffPlan plan)
        {
            if (plan.Line == NanoLine.NOSTACKING) return true;
            BuffPlan best = null;
            foreach (BuffPlan p in _buffPlans)
            {
                if (p.Line != plan.Line) continue;
                if (best == null || RankInLine(p) > RankInLine(best)) best = p;
            }
            return best == null || best.NanoId == plan.NanoId;
        }

        private static long RankInLine(BuffPlan p)
        {
            long hp = BuffsHealth(p.Nano) ? 1L : 0L;            // user rule: HP buff wins its line
            long stack = p.Nano.StackingOrder & 0xFFFFF;        // then strongest effect tier
            long dur = DurationHours(p.Nano) & 0xFF;            // then LONGEST duration (8h Mochams > 1h, same effect)
            long ql = (uint)p.Nano.Ql & 0xFFF;
            return (hp << 48) | (stack << 28) | (dur << 12) | ql;
        }

        // Buff duration in hours, parsed from the name ("... (8 hours)"). Not in the offline data otherwise.
        private static long DurationHours(NanoItem ni)
        {
            if (string.IsNullOrEmpty(ni.Name)) return 0;
            Match m = Regex.Match(ni.Name, @"\((\d+)\s*hour", RegexOptions.IgnoreCase);
            return (m.Success && long.TryParse(m.Groups[1].Value, out long h)) ? h : 0;
        }

        private static bool BuffsHealth(NanoItem ni)
        {
            if (ni.Modifiers == null || !ni.Modifiers.TryGetValue(SpellListType.Use, out var use) || use == null) return false;
            foreach (var kv in use)
                if ((kv.Key == Stat.Health || kv.Key == Stat.MaxHealth) && kv.Value > 0) return true;
            return false;
        }

        private static bool SafeMeetsReqs(NanoItem ni, SimpleChar target)
        {
            try { return ni.MeetsUseReqs(target, false); } catch { return false; }
        }

        private static string BuffName(BuffPlan plan) => plan.Nano?.Name ?? ("nano " + plan.NanoId);

        // Lines the auto-buff must NOT keep up: pet summons + pet-target buffs (pet controller's job), and any
        // UNMAPPED numeric NanoLine — those are vehicles / nukes / recalls / summoned weapons, never keep-up
        // self buffs. Casting a vehicle (e.g. "boogy board") mounts the char and blocks all casting.
        private static readonly HashSet<NanoLine> _petAndSummonLines = new HashSet<NanoLine>
        {
            NanoLine.AttackPets, NanoLine.HealPets, NanoLine.SupportPets, NanoLine.SocialPets,
            NanoLine.MPPetDamageBuffs, NanoLine.MPPetInitiativeBuffs, NanoLine.PetShortTermDamageBuffs, NanoLine.PetWarp
        };
        private static bool IsNotAKeepUpBuff(NanoLine line)
            => _petAndSummonLines.Contains(line) || !Enum.IsDefined(typeof(NanoLine), line);

        // Full nano inventory dump — every KNOWN nano with its real metadata (line, QL, stacking, NCU, effect
        // stats, castable-by-us). This is how we "really understand our nanos" per class: run 'nanos', read the
        // log, and see exactly what he has and how it's tagged. Empirical, no guessing.
        public int DumpNanos(LocalPlayer me)
        {
            int[] spells = me == null ? null : me.SpellList;
            if (spells == null) { _ctx.Log("NANOS: SpellList null (not loaded yet)."); return 0; }
            _ctx.Log($"NANOS: dumping {spells.Length} known nanos ---");
            int n = 0;
            foreach (int id in spells)
            {
                if (!ItemData.Find(id, out NanoItem ni) || ni == null) { _ctx.Log($"NANOS: [{id}] no item data"); continue; }
                bool cast; try { cast = ni.MeetsUseReqs(me, false); } catch { cast = false; }
                string mods = "-";
                if (ni.Modifiers != null && ni.Modifiers.TryGetValue(SpellListType.Use, out var use) && use != null && use.Count > 0)
                    mods = string.Join(",", use.Select(kv => kv.Key + ":" + kv.Value));
                _ctx.Log($"NANOS: [{id}] '{ni.Name}' QL{ni.Ql} line={ni.NanoLine} stack={ni.StackingOrder} ncu={ni.NCU} castable={cast} mods={mods}");
                n++;
            }
            _ctx.Log($"NANOS: --- {n} dumped ---");
            return n;
        }

        // For the 'autobuff' command: what did we classify, and its live status.
        public List<string> DescribeBuffPlans(LocalPlayer me, PlayerChar owner)
        {
            RebuildBuffPlansIfNeeded(me);
            var lines = new List<string>();
            foreach (BuffPlan p in _buffPlans.OrderBy(x => BuffName(x), StringComparer.OrdinalIgnoreCase))
            {
                string tgt = (p.Self ? "self" : "") + (owner != null && SafeMeetsReqs(p.Nano, owner) ? (p.Self ? "+owner" : "owner") : "");
                if (tgt.Length == 0) tgt = "?";
                Buff on = me.Buffs?.FirstOrDefault(b => b.Id == p.NanoId);
                string status = on != null ? $"up {on.Cooldown?.RemainingTime ?? 0:0}s" : "DOWN";
                lines.Add($"{BuffName(p)} [{p.NanoId}] ncu{p.Ncu} cost{AdjustedNanoCost(me, p.NanoId)} -> {tgt} ({status})");
            }
            return lines;
        }

        // ---- Nano cost / affordability / refill ----------------------------------
        // Breed-adjusted cost, matching the client (Buff.GetCost): cost = base * max(breedFloor, NPCostMod)/100.
        public int AdjustedNanoCost(LocalPlayer me, int nanoId)
        {
            if (!ItemData.Find(nanoId, out NanoItem ni) || ni == null) return 0;
            int mod = me.TryGetStat(Stat.NPCostModifier, out int m) ? m : 0;   // TryGetStat: GetStat throws on a missing key
            int breed = me.TryGetStat(Stat.Breed, out int b) ? b : 0;
            int floor;
            switch ((Breed)breed)
            {
                case Breed.Nanomage: floor = 45; break;
                case Breed.Atrox: floor = 55; break;
                default: floor = 50; break;   // Solitus / Opifex / other
            }
            if (mod < floor) mod = floor;
            return (int)(ni.Cost * (mod / 100.0));
        }

        private bool CanAfford(LocalPlayer me, int nanoId)
        {
            int cost = AdjustedNanoCost(me, nanoId);
            if (cost <= 0) return true;                       // unknown cost -> let the server decide
            // Unknown POOL gets the same treatment as unknown cost. Reading a missing stat as "broke" is how
            // a bot whose stats never loaded refused every cast in silence while claiming a full nano bar.
            if (!me.TryGetStat(Stat.CurrentNano, out int cur)) return true;
            return cur >= cost;
        }

        // Can't afford the pending cast: refill nano. Sitting recharger fills the most (out of combat);
        // a standing stim is a quick top-up. If neither is available, drop the cast so we don't hang.
        private void NanoRefillForCast(LocalPlayer me, CastRequest req, bool inCombat)
        {
            if (!inCombat)
            {
                Item recharger = BestRestHeal(false, true);   // RK recharger first, else SL Coil-of-Nano/Vet-Lab
                if (recharger != null)
                {
                    if (!_sitting)
                    {
                        me.MovementComponent.ChangeMovement(MovementAction.SwitchToSit);
                        _sitting = true; _nanoRefillAccum = 999;
                        _ctx.Log($"AUTO-BUFF: sitting to recharge nano for {req.Label} (need {AdjustedNanoCost(me, req.NanoId)}).");
                    }
                    _resting = true;   // tell the movement arbiter to hold still while we recharge to buff
                    _ctx.SetBehavior("Resting");
                    _nanoRefillAccum += _ctx.Config.TickMs / 1000.0;
                    if (_nanoRefillAccum >= 1.5 && HealItemReady(me, recharger))   // locked -> keep sitting, don't re-poke
                    { _nanoRefillAccum = 0; recharger.Use(); MarkHealItemUsed(recharger); }   // self-use, proven stim pattern
                    return;
                }
            }

            // No recharger available (or in combat) → do NOT burn a stim for nano. Stims are HP heals; nano is
            // recovered with rechargers while buffing. With nothing to refill nano, skip this cast so the queue
            // doesn't hang — it re-queues next scan and casts once a recharger has topped nano up.
            _castQueue.Dequeue();
            _resting = false;
            StandIfSitting(me);
            _ctx.Log($"AUTO-BUFF: can't afford {req.Label} [{req.NanoId}] and no recharger for nano — skipping (stims heal HP, they don't fuel casts).");
        }

        // Emergency heal (stims/nanos, works IN combat). Owner first, then self, then team. Throttled
        // so it can't fire every tick (that trapped spamming a cooling-down stim and never fighting),
        // and never interrupts an active rest. On a successful heal it stands from a sit and clears
        // resting. Returns true if it healed.
        public bool TryEmergencyHeal(LocalPlayer me, PlayerChar owner, bool fighting)
        {
            if (_resting || _healCd < _ctx.Config.HealIntervalSec) return false;
            if (IsRezSick(me)) return false;   // stims/rechargers can't be used through rez sickness
            if (!DoEmergencyHeal(me, owner, fighting)) return false;
            _healCd = 0; StandIfSitting(me); _resting = false;
            return true;
        }

        // Fire a stim only if its skill lock is off. While locked, returns false so the caller keeps
        // attacking instead of re-poking a locked stim and pausing the swing.
        private bool FireStim(LocalPlayer me, Item stim, PlayerChar owner, bool onOwner)
        {
            if (!HealItemReady(me, stim)) return false;   // locked -> keep swinging, no retarget
            if (onOwner) stim.Use(owner); else stim.Use();
            MarkHealItemUsed(stim);
            return true;
        }

        private bool IsRecharger(Item it)
            => it?.Name != null && !string.IsNullOrEmpty(_ctx.Config.RechargerKeyword)
               && it.Name.IndexOf(_ctx.Config.RechargerKeyword, StringComparison.OrdinalIgnoreCase) >= 0;

        private Stat LockSkill(Item it) => IsRecharger(it) ? Stat.Treatment : Stat.FirstAid;

        // Is this heal item's skill lock off? Syncs our timer to the server's lock whenever it has one: a live
        // lock sets the remaining time, and a lock we saw that has since been lifted/run out frees it. An
        // expired leftover entry from an earlier use is ignored, so it can't cut short the timer of a new use.
        private bool HealItemReady(LocalPlayer me, Item it)
        {
            Stat s = LockSkill(it);
            if (me.Cooldowns.TryGetValue(s, out Cooldown cd) && cd.RemainingTime > 0)
            {
                if (_lockSeen.Add(s)) _ctx.Log($"HEAL-LOCK: server locked {s} for {cd.RemainingTime:0.#}s");
                _lockUntil[s] = _sessionSeconds + cd.RemainingTime;
            }
            else if (_lockSeen.Remove(s)) _lockUntil[s] = _sessionSeconds;
            return !_lockUntil.TryGetValue(s, out double until) || _sessionSeconds >= until;
        }

        // We have SENT a use. Hold off re-poking for a moment, but do NOT commit the full reuse timer yet -
        // that is only correct if the server accepted it. Committing it unconditionally meant a refused use
        // cost the whole 15s/40s, and HealItemReady could never give that time back: it only clears a local
        // timer after having SEEN a server lock, which a refusal never produces.
        private void MarkHealItemUsed(Item it)
        {
            Stat s = LockSkill(it);
            _lockUntil[s] = _sessionSeconds + HealVerifySeconds;
            _lockSeen.Remove(s);
            _healUseSentAt[s] = _sessionSeconds;
            _healUseItem[s] = it?.Name ?? "?";
            _healUseState[s] = (_sitting ? "seated" : "standing") + ", " + (_lastKnownInCombat ? "in combat" : "out of combat");
        }

        /// <summary>
        /// Decide what became of each heal item we sent. Called every frame, because the server's lock has to
        /// be noticed when it ARRIVES - HealItemReady only looks when the bot next wants to use something,
        /// which in one session was twenty-three seconds later.
        /// </summary>
        private void VerifyHealUses(LocalPlayer me)
        {
            if (me == null || _healUseSentAt.Count == 0) return;

            List<Stat> done = null;
            foreach (var kv in _healUseSentAt)
            {
                Stat s = kv.Key;
                string item = _healUseItem.TryGetValue(s, out string n) ? n : "?";

                if (me.Cooldowns.TryGetValue(s, out Cooldown cd) && cd.RemainingTime > 0)
                {
                    // Accepted: the server locked the skill. Take ITS timer, which is the real one.
                    _lockUntil[s] = _sessionSeconds + cd.RemainingTime;
                    _lockSeen.Add(s);
                    _ctx.Log($"HEAL-OK: server accepted {item} — {s} locked {cd.RemainingTime:0.#}s.");
                    if (_awaitingRestUseConfirm && s == _restUseStat) _restUseConfirmed = true;
                    (done ?? (done = new List<Stat>())).Add(s);
                    continue;
                }

                if (_sessionSeconds - kv.Value < HealVerifySeconds) continue;

                // Refused: no lock, so the item was never consumed. Say so plainly instead of logging a
                // success, and retry soon rather than sitting out the full reuse timer we never earned.
                _lockUntil[s] = _sessionSeconds + HealRefusedRetrySeconds;
                _refusedHealUses++;
                string where = _healUseState.TryGetValue(s, out string w) ? w : "posture unknown";
                string why = s == Stat.Treatment && where.StartsWith("standing")
                    ? "a recharger is a sit-only item and we were standing when the server processed it"
                    : where.EndsWith("in combat")
                        ? "the server rejects heal items while in combat — check for a pet still fighting"
                        : "neither combat nor posture explains it; check rez sickness and the item itself";
                _ctx.Log($"HEAL-REFUSED: {item} was NOT accepted — no {s} lock in {HealVerifySeconds:0.#}s "
                         + $"({where}). Likely: {why}. (refused {_refusedHealUses} so far this session)");

                // Seated, out of combat, not rez sick - there is nothing left to blame but this particular
                // item, so stop offering it and let the next one down be tried. Only THIS case demotes: a
                // refusal in combat or while standing says nothing about the item, and blacklisting on a
                // guess is what once condemned a perfectly good recharger.
                if (_awaitingRestUseConfirm && s == _restUseStat) _restUseRefused = true;
                (done ?? (done = new List<Stat>())).Add(s);
            }

            if (done != null)
                foreach (Stat s in done) { _healUseSentAt.Remove(s); _healUseItem.Remove(s); _healUseState.Remove(s); }
        }

        private int _refusedHealUses;

        /// <summary>How many heal-item uses the server has rejected this session (0 is the healthy value).</summary>
        public int RefusedHealUses => _refusedHealUses;

        private bool DoEmergencyHeal(LocalPlayer me, PlayerChar owner, bool fighting)
        {
            int selfHp = SelfHpPct(me);
            int selfNano = SelfNanoPct(me);
            // No reading at all means no decision. Treating "unknown" as "critical" would have this firing a
            // stim every couple of seconds on a character whose stats simply had not arrived yet.
            if (selfHp == Unknown && selfNano == Unknown) return false;
            // The owner counts as low only on a FRESH reading — a stale one (he's been out of the team feed,
            // no reply to our InfoRequest yet, or we just healed him) is unknown, not low. The old dynel read
            // said "49%" for 22 minutes and he got stimmed on it again and again.
            int ownerHp = 101;
            bool ownerKnown = owner != null && _ctx.Vitals.TryFreshHp(owner, out ownerHp, out _);
            // A stim restores BOTH health and nano, so for a caster a full health bar and an empty nano bar
            // is just as much a reason to use one mid-fight as a health drop: without nano he cannot cast,
            // which for a nano class is the same as being out of the fight.
            bool selfLow = AtOrBelow(selfHp, _ctx.Config.HealSelfBelowPercent)
                           || AtOrBelow(selfNano, _ctx.Config.StimNanoBelowPercent);
            bool ownerLow = ownerKnown && ownerHp <= _ctx.Config.HealOwnerBelowPercent;

            // 1) HIMSELF FIRST WHILE HE IS THE ONE IN DANGER. Below the critical floor he always heals himself
            //    and nothing argues with that: a dead bot heals nobody.
            //
            //    Above it, the stim goes to whoever is actually closer to dying. "Himself first, full stop" was
            //    written to stop him stimming a healthier owner while about to die, and it does - but taken
            //    literally it also does the opposite. It killed the owner: the bot stimmed ITSELF at 52%, well
            //    clear of danger, while the owner fell 58% -> 0%. Worse, both of them draw on the same First
            //    Aid lock, so that one self-heal shut the owner out for forty seconds, which is exactly how
            //    long he had left. One stim, one lock, so it has to go to the right person the first time.
            bool selfCritical = AtOrBelow(selfHp, _ctx.Config.SelfCriticalPercent);
            bool ownerWorse = ownerKnown && selfHp != Unknown && ownerHp < selfHp;
            if (selfCritical || (fighting && selfLow && !ownerWorse))
            {
                if (TrySelfHeal(me, selfHp, selfNano)) return true;
            }

            // 2) Owner low (and not out-prioritised by the bot's own survival above).
            if (ownerLow)
            {
                string seen = _ctx.Vitals.Describe(owner);
                if (_ctx.Config.HealNanoId > 0)
                {
                    DoCast(me, new CastRequest { Target = owner.Identity, NanoId = _ctx.Config.HealNanoId, Label = "heal owner" });
                    _ctx.Vitals.Invalidate(owner.Identity);
                    _ctx.Log($"HEAL owner [{seen}]");
                    return true;
                }
                Item so;
                if (me.DistanceFrom(owner) <= _ctx.Config.StimOwnerRange && (so = BestEmergencyHeal(true, false)) != null && FireStim(me, so, owner, true))
                {
                    _ctx.Vitals.Invalidate(owner.Identity);
                    _ctx.Log($"STIM owner [{seen}] with QL{so.Ql} {so.Name}");
                    return true;
                }
            }

            // 3) Out of combat and below the heal threshold with no owner to worry about — top up rather
            //    than wait for a rest. (In a fight this case was already taken by step 1.)
            if (!fighting && selfLow)
            {
                if (TrySelfHeal(me, selfHp, selfNano)) return true;
            }

            // 4) Team heal (doctor): any teammate hurting and a team heal configured.
            if (_ctx.Config.TeamHealNanoId > 0 && Team.Members.Any(m => LowHealthMember(m)))
            {
                DoCast(me, new CastRequest { OnSelf = true, NanoId = _ctx.Config.TeamHealNanoId, Label = "team heal" });
                foreach (TeamMember m in Team.Members) _ctx.Vitals.Invalidate(m.Identity);
                return true;
            }
            return false;
        }

        private bool TrySelfHeal(LocalPlayer me, int selfHp, int selfNano)
        {
            if (_ctx.Config.SelfHealNanoId > 0 && AtOrBelow(selfHp, _ctx.Config.HealSelfBelowPercent))
            { DoCast(me, new CastRequest { OnSelf = true, NanoId = _ctx.Config.SelfHealNanoId, Label = "self heal" }); return true; }
            // Stims restore health AND nano, so ask for whichever is actually short.
            bool needHp = AtOrBelow(selfHp, _ctx.Config.HealSelfBelowPercent)
                          || AtOrBelow(selfHp, _ctx.Config.SelfCriticalPercent);
            bool needNano = AtOrBelow(selfNano, _ctx.Config.StimNanoBelowPercent);
            Item ss = BestEmergencyHeal(needHp || !needNano, needNano || !needHp);
            if (ss != null && FireStim(me, ss, null, false)) { _ctx.Log($"STIM self hp={selfHp}% nano={selfNano}% with QL{ss.Ql} {ss.Name}"); return true; }
            return false;
        }

        // The bot needs to sit and recover (own HP or nano low). Owner-independent — used to make recovery
        // take priority over wandering after a lost owner (don't zone-sweep/catch-up when he should be healing).
        private int _restPrevHp = Unknown;
        private double _hurtAt = -999;
        public bool UnderFire => _sessionSeconds - _hurtAt < 6;

        public bool NeedsRecovery(LocalPlayer me)
            => me != null && (Below(SelfHpPct(me), _ctx.Config.RestBelowPercent)
                              || Below(SelfNanoPct(me), _ctx.Config.RestNanoBelowPercent));

        // A new fight started — allow one rest again once it's over, and stand from any sit.
        public void OnFight(LocalPlayer me)
        {
            StandIfSitting(me); _resting = false; _restedSinceCombat = false;
            _awaitingRestUseConfirm = false;   // a fight ends the sit; whatever the server says about that use
            // A fight is a new situation: forget any backed-off run of fruitless sits and judge afresh after it.
            _restRunSince = 0; _restRunPeakHp = -1; _restRunPeakNano = -1; _restSuppressUntil = 0;
            _postUseHoldUntil = 0;   // a fight overtakes whatever the last item was going to do
        }

        // Out-of-combat REST — between fights he SITS and rechargers restore BOTH HP and nano. Sit when HP OR nano
        // is low; recharge (over time, while seated) until both hit their targets, the owner moves / a fight starts,
        // or the item stops helping. Stims are NOT used here (they're combat HP heals). No "dead item" blacklist:
        // a recharger heals slowly, so slow gain is progress, not failure — the old blacklist wrongly killed a good
        // recharger and then cried "OUT of rechargers". Returns true while resting (Main then holds still).
        public bool RestTick(LocalPlayer me, PlayerChar owner, bool inCombat, bool combatLull = false)
        {
            if (_restCooldown > 0) _restCooldown -= _ctx.Config.TickMs / 1000.0;
            if (_restZoneSuppress > 0) _restZoneSuppress -= _ctx.Config.TickMs / 1000.0;

            // Rez sickness blocks rechargers — don't sit, and (critically) don't run the no-heal blacklist,
            // or a good recharger that "failed" only because of rez sickness would be blacklisted for good.
            if (IsRezSick(me)) { if (_sitting) StandIfSitting(me); _resting = false; return false; }

            // Resting is the bot's OWN need — it does NOT depend on the owner. He could be anywhere (or gone);
            // if the bot needs HP/nano and isn't in a fight, it sits and recovers. So a missing/lost owner
            // counts as "settled" and "still" (never a reason to skip a rest).
            bool settled = owner == null || me.DistanceFrom(owner) <= _ctx.Config.FollowDistance + 2f;
            // He is only "still" once he has stayed put for a moment. Sitting down the instant a broadcast
            // happens not to have arrived is how the bot came to plant itself repeatedly while being led
            // somewhere - and a rest is worth nothing if he has to stand up again a second later.
            bool ownerStill = owner == null || OwnerStillFor >= OwnerStillSeconds;
            int hpNow = SelfHpPct(me);
            // UNDER FIRE: HP falling in the last 6 s means something is still hitting him, whatever combat says.
            // 06:33 (2026-09-24): two turrets set aside as stationary shooters, 'out of combat', he sat down to
            // rest in their line of fire and died seated, recharger refused twice.
            if (hpNow != Unknown) { if (_restPrevHp != Unknown && hpNow < _restPrevHp) _hurtAt = _sessionSeconds; _restPrevHp = hpNow; }
            bool underFire = _sessionSeconds - _hurtAt < 6;
            int nanoNow = SelfNanoPct(me);
            // Between fights he sits and rechargers restore BOTH HP and nano — if either is low, rest. Kept
            // available for the whole sit so it doesn't go null mid-rest.
            bool needRest = Below(hpNow, _ctx.Config.RestBelowPercent)
                            || Below(nanoNow, _ctx.Config.RestNanoBelowPercent);
            // Heal items (stims AND rechargers) BOTH restore HP and nano — treat them the same. Use whichever
            // he has: recharger first (reusable, doesn't cost supplies), else a stim. If HP OR nano is low, heal.
            // STIM FIRST. Both restore HP and nano, but the stim's Use is the PROVEN one — it's the item the
            // heal path has used successfully for days. The Recharger fires the identical GenericCmd Use yet
            // moved nano 0 points across every use (rawNano flat 30/184) and carries UseMods[(none)], so it is
            // not a trustworthy nano source. Try the proven item first, fall back to the recharger.
            // Stim and recharger lock different skills (First Aid / Treatment), so if the stim is still locked
            // from a fight, the recharger may be free — prefer whichever is actually usable now.
            Item recharger = null;
            if (needRest || _resting)
            {
                Item stim = BestUsableHealItem(_ctx.Config.StimKeyword, _ctx.Config.StimItemName);
                Item rech = BestUsableHealItem(_ctx.Config.RechargerKeyword, _ctx.Config.RechargerItemName)
                            ?? BestRestHeal(true, true);
                recharger = stim != null && HealItemReady(me, stim) ? stim
                          : rech != null && HealItemReady(me, rech) ? rech
                          : stim ?? rech;
            }
            bool haveRecharger = recharger != null;   // a stim counts too — it restores HP and nano just the same

            // Casting takes posture priority: a nano cast auto-STANDS him, so if he's also resting he ping-pongs
            // stand(cast)/sit every tick while buffing/summoning (log 13:07). REST yields while casts are queued or
            // were fired in the last few seconds — nano to afford a specific buff is topped by the separate
            // nano-refill path, then he sits here to recover once casting is idle.
            // ACTIVE casting only. A cast merely QUEUED but unaffordable (exactly the low-nano case) must NOT
            // count as "casting" — otherwise the queued summon blocks the very recharge needed to afford it, and
            // he deadlocks: can't summon (no nano), can't recharge (queued summon blocks rest). Only a cast that
            // is actually firing (auto-stands him) should make rest yield, to avoid sit/stand ping-pong.
            bool castsActive = me.IsCasting || (_sessionSeconds - _lastCastAt) < 3.0;

            // combatLull only blocks STARTING a rest (don't sit mid-pull). Once seated we keep resting through a
            // flapping lull — otherwise the caller stood him every time a hostile drifted in/out of range and he
            // ping-ponged sit/stand (log 13:03).
            // CASTER NANO RECOVERY: a caster whose nano is low MUST recharge even with HP full — and even when a
            // cast (e.g. a pet summon) is queued but UNaffordable, which is exactly why nano is low. That queued
            // cast makes castsActive true and used to block the very recharge needed to afford it (nano stuck at
            // 4%, "sits and does nothing"). So when nano is below the rest threshold and we're not mid-cast, low
            // nano overrides castsActive / restedSinceCombat / restCooldown. The castsActive guard still applies
            // when nano is fine (it only exists to stop sit/stand ping-pong while actively firing casts).
            bool nanoStarved = !me.IsCasting && Below(nanoNow, _ctx.Config.RestNanoBelowPercent);
            // Do NOT sit again until the heal item's use timer has run out — sitting early just burns a use
            // that the server ignores. This is the "wait for the timer" half of the sit/use/stand cycle.
            bool itemReady = recharger == null || HealItemReady(me, recharger);

            // IS ANY OF THIS WORKING? The per-sit no-progress check below cannot answer that: every sit resets
            // its timer, so a bot that sits, uses, stands and sits again never accumulates enough elapsed time
            // to notice nothing is improving. It once ran eight of those cycles in thirty seconds with HP
            // pinned at 73% the whole way. This tracker spans the cycles - if HP and nano have not climbed
            // across a run of them, stop trying for a while instead of doing it forever.
            if (hpNow > _restRunPeakHp || nanoNow > _restRunPeakNano)
            {
                if (hpNow > _restRunPeakHp) _restRunPeakHp = hpNow;
                if (nanoNow > _restRunPeakNano) _restRunPeakNano = nanoNow;
                _restRunSince = _sessionSeconds;
            }
            if (_restRunSince > 0 && _sessionSeconds - _restRunSince >= RestRunNoGainSeconds)
            {
                _restSuppressUntil = _sessionSeconds + RestRunBackoffSeconds;
                _restRunSince = 0;
                StandIfSitting(me); _resting = false;
                _ctx.Log($"REST: {RestRunNoGainSeconds:0}s of sitting and using items moved nothing "
                         + $"(hp={Pct(hpNow)} nano={Pct(nanoNow)}) — standing and leaving it for "
                         + $"{RestRunBackoffSeconds:0}s.");
                return false;
            }

            // Still waiting to see what the last accepted item did? Then the numbers we would decide on are
            // the ones from BEFORE it, and deciding on those is what makes him use a second item he does not
            // need. A raw stat moving ends the wait early - that is the result arriving.
            if (_postUseHoldUntil > 0)
            {
                me.TryGetStat(Stat.Health, out int settleHp);
                me.TryGetStat(Stat.CurrentNano, out int settleNano);
                if (settleHp != _postUseRawHp || settleNano != _postUseRawNano)
                {
                    _ctx.Log($"REST: the last item landed — hp {_postUseRawHp} -> {settleHp}, nano {_postUseRawNano} -> {settleNano}.");
                    _postUseHoldUntil = 0;
                }
                else if (_sessionSeconds >= _postUseHoldUntil)
                {
                    _ctx.Log($"REST: {PostUseSettleSeconds:0}s after an accepted item and neither hp ({settleHp}) nor "
                             + "nano has moved — the server has sent no update, so another item would be guesswork.");
                    _postUseHoldUntil = 0;
                    _restSuppressUntil = _sessionSeconds + RestRunBackoffSeconds;
                }
            }

            bool startRest = !inCombat && !underFire && !combatLull && _restZoneSuppress <= 0 && haveRecharger && settled
                             && ownerStill && needRest && itemReady && _sessionSeconds >= _restSuppressUntil
                             && _postUseHoldUntil <= 0
                             && (nanoStarved || (!castsActive && !_restedSinceCombat && _restCooldown <= 0));
            // Keep resting until HP AND nano are actually topped (so he doesn't sit forever at full, and doesn't
            // stand while nano is still low). A queued-but-not-firing summon no longer blocks this (castsActive is
            // active-only now); only a cast currently firing yields, to avoid ping-pong.
            bool stillNeedsRecovery = Below(hpNow, _ctx.Config.RestUntilPercent)
                                      || Below(nanoNow, _ctx.Config.RestNanoUntilPercent);
            // An unanswered use holds the sit open on its own: standing before the server replies is exactly
            // what was getting the use thrown away.
            bool continueRest = _resting && !inCombat && !underFire && settled
                                && (_awaitingRestUseConfirm || (stillNeedsRecovery && (nanoStarved || !castsActive)));
            if (!(startRest || continueRest)) return false;

            if (!_sitting)
            {
                me.MovementComponent.ChangeMovement(MovementAction.SwitchToSit);
                _sitting = true; _resting = true; _rechargeAccum = 0; _restElapsed = 0;
                _restPeakHp = hpNow; _restPeakNano = nanoNow; _restLastGainAt = 0;
                _seatConfirmed = false; _seatFallbackLogged = false;   // wait for the server to say we sat
                if (_restRunSince <= 0) { _restRunSince = _sessionSeconds; _restRunPeakHp = hpNow; _restRunPeakNano = nanoNow; }
                _ctx.Log($"REST: sitting to recover at hp={Pct(hpNow)} nano={Pct(nanoNow)}.");
            }
            else _restElapsed += _ctx.Config.TickMs / 1000.0;
            _resting = true;
            _ctx.SetBehavior("Resting");

            // SIT -> USE -> DETECT -> STAND. The server's answer to the use we sent while seated arrives via
            // VerifyHealUses; act on it here.
            if (_awaitingRestUseConfirm && _restUseConfirmed)
            {
                _awaitingRestUseConfirm = false; _restUseConfirmed = false; _restUseRefused = false;
                StandIfSitting(me); _resting = false;
                _restedSinceCombat = false;      // the item's timer decides the next sit, not a once-per-lull rule
                _restCooldown = 1.0;

                // The server took it. Now wait for its effect before judging whether more is needed - see
                // _postUseHoldUntil. Remember the raw numbers as they stand right now, because it is a CHANGE
                // in those, not the clock, that tells us the heal has arrived.
                me.TryGetStat(Stat.Health, out _postUseRawHp);
                me.TryGetStat(Stat.CurrentNano, out _postUseRawNano);
                _postUseHoldUntil = _sessionSeconds + PostUseSettleSeconds;

                _ctx.Log($"REST: use confirmed — standing (hp={Pct(hpNow)} nano={Pct(nanoNow)}), waiting up to "
                         + $"{PostUseSettleSeconds:0}s to see what it did.");
                return true;
            }
            if (_awaitingRestUseConfirm && _restUseRefused)
            {
                // Refused. Stay DOWN and let the retry timer run out — standing now would only guarantee the
                // next one is refused too.
                _awaitingRestUseConfirm = false; _restUseConfirmed = false; _restUseRefused = false;
                _ctx.Log("REST: use refused — staying seated and retrying.");
            }

            // Progress tracking: note the last time HP or nano climbed (rechargers heal over time — a slow gain is
            // still progress, NOT a dead item). This replaces the old blacklist that wrongly killed a good, slow
            // recharger and then cried "OUT of rechargers".
            if (hpNow > _restPeakHp || nanoNow > _restPeakNano)
            {
                if (hpNow > _restPeakHp) _restPeakHp = hpNow;
                if (nanoNow > _restPeakNano) _restPeakNano = nanoNow;
                _restLastGainAt = _restElapsed;
            }

            _rechargeAccum += _ctx.Config.TickMs / 1000.0;

            // ARE WE ACTUALLY SEATED? Use the item the moment the server confirms the sit, not after a fixed
            // wait. The old fixed 1.5s was dead time in which anything could stand him up again - and while the
            // owner was walking, follow did exactly that within about half a second, over and over, so the use
            // was never reached at all: eight sits in thirty seconds and not one recharger pressed.
            bool seated = _seatConfirmed || _rechargeAccum >= SeatFallbackSeconds;
            if (seated && !_seatConfirmed && !_seatFallbackLogged)
            {
                _seatFallbackLogged = true;
                _ctx.Log($"REST: no sit confirmation from the server in {SeatFallbackSeconds:0.#}s — using the item anyway.");
            }

            if (seated && !haveRecharger)
            {
                _rechargeAccum = 0;
                _ctx.Log($"RECHARGE-DBG: sitting at hp={hpNow}% nano={nanoNow}% but NO usable recharger — BestRestHeal={(recharger == null ? "null" : recharger.Name + " QL" + recharger.Ql)} consumableStim={(recharger != null && IsConsumableStim(recharger))} invItems={AllInvItems().Count(i => i?.Name != null && i.Name.IndexOf(_ctx.Config.RechargerKeyword, StringComparison.OrdinalIgnoreCase) >= 0)}");
            }
            if (seated && haveRecharger)
            {
                _rechargeAccum = 0;
                // Both HP and nano at target — nothing to recover, don't use an item on full stats. Stand, done.
                if (hpNow != Unknown && nanoNow != Unknown
                    && hpNow >= _ctx.Config.RestUntilPercent && nanoNow >= _ctx.Config.RestNanoUntilPercent)
                {
                    StandIfSitting(me); _resting = false; _restedSinceCombat = true; _restCooldown = 1.0;
                    _ctx.Log($"REST: hp & nano at targets — done, standing.");
                    return true;
                }
                // FACT-FINDING (ends the guessing loop): dump what the item's Use actually restores + the RAW
                // server nano each use. If UseMods has no CurrentNano the item can't refill nano; if rawNano
                // climbs while nano% looks flat, the % read is just stale (server sends sparse nano deltas).
                me.TryGetStat(Stat.CurrentNano, out int rawNano); me.TryGetStat(Stat.MaxNanoEnergy, out int rawMaxNano);
                string useMods = (recharger.Modifiers != null && recharger.Modifiers.TryGetValue(SpellListType.Use, out var um) && um != null && um.Count > 0)
                    ? string.Join(",", um.Select(kv => kv.Key + "=" + kv.Value)) : "(none)";
                // Seated since before the lock ran out? Don't burn a use the server will ignore — wait.
                if (!HealItemReady(me, recharger)) return true;
                recharger.Use();                    // self-use — matches the PROVEN stim self-heal path (stim.Use(), used for days)
                MarkHealItemUsed(recharger);
                double reuseSec = IsRecharger(recharger) ? _ctx.Config.RechargerReuseSec : _ctx.Config.StimReuseSec;
                // "sent", not "used": whether the server took it is decided by VerifyHealUses, which logs
                // HEAL-OK or HEAL-REFUSED a moment later. Claiming success here is what hid six straight
                // refusals behind a log line that read like everything was working.
                // Raw HP as well as the percentage. A percentage that will not move is impossible to argue
                // with; the two numbers behind it say whether the item healed nothing or the server simply
                // has not sent us a new Health yet.
                me.TryGetStat(Stat.Health, out int rawHp); me.TryGetStat(Stat.MaxHealth, out int rawMaxHp);
                _ctx.Log($"RECHARGE: sent {recharger.Name} QL{recharger.Ql} (seated {_restElapsed:0.0}s) hp={Pct(hpNow)} rawHp={rawHp}/{rawMaxHp} nano={Pct(nanoNow)} rawNano={rawNano}/{rawMaxNano} UseMods[{useMods}] — awaiting server lock (reuse {reuseSec:0.#}s if accepted)");

                // Sit, use, DETECT that it landed, then stand. Standing at this point instead - which is what
                // this did, to force the posture-change nano update - meant the server saw a standing character
                // when it processed a sit-only item and refused it: three sends, three HEAL-REFUSED, HP stuck
                // at 49% with a perfectly good QL11 Health and Nano Recharger in the bag. It is also what made
                // the recharger look like it "moved nano 0 points across every use": it was never used at all.
                _awaitingRestUseConfirm = true;
                _restUseConfirmed = false;
                _restUseRefused = false;
                _restUseStat = LockSkill(recharger);
                return true;
            }

            bool recovered = hpNow >= _ctx.Config.RestUntilPercent && nanoNow >= _ctx.Config.RestNanoUntilPercent;
            // No gain for a WHILE = the item really isn't helping — stand. The window is long (15s) because the
            // server sends HP/nano sparsely, so a working recharger's climb can look flat for several seconds;
            // a 5s window stood him up mid-recharge and (with nanoStarved) he instantly re-sat = the sit/stand loop.
            bool stalled = (_restElapsed - _restLastGainAt) >= 15.0;
            if (recovered || stalled)
            {
                StandIfSitting(me); _resting = false;
                _restedSinceCombat = true;
                _restCooldown = recovered ? 1.0 : 6.0;
                _ctx.Log($"REST end at hp={hpNow}% nano={nanoNow}% ({(recovered ? "recovered" : "no-progress")}), standing. Won't re-sit until next combat.");
            }
            return true;
        }

        public void SetIdleState(LocalPlayer me) { StandIfSitting(me); _resting = false; _awaitingRestUseConfirm = false; }

        public void StandIfSitting(LocalPlayer me)
        {
            if (_sitting) { me.MovementComponent.ChangeMovement(MovementAction.LeaveSit); _sitting = false; }
        }

        // After a zone/teleport, HP/nano read stale-low until the next full update — don't sit to
        // "heal" during that window.
        public void OnZone() { _restZoneSuppress = 6.0; _buffGrace = BuffZoneGrace; }

        private bool LowHealthMember(TeamMember m)
        {
            SimpleChar c = DynelManager.Characters.FirstOrDefault(x => x.Identity == m.Identity);
            if (c == null) return false;
            LocalPlayer me = DynelManager.LocalPlayer;
            if (me != null && c.Identity == me.Identity) return AtOrBelow(SelfHpPct(me), _ctx.Config.TeamHealBelowPercent);
            return _ctx.Vitals.TryFreshHp(c, out int hp, out _) && hp <= _ctx.Config.TeamHealBelowPercent;
        }

        private void DoCast(LocalPlayer me, CastRequest req)
        {
            _move.Stop(me, _ctx.Config.SendIntervalMs); // stand still to cast
            _lastCastAt = _sessionSeconds;              // mark casting active so rest won't fight the cast for posture
            if (req.OnSelf || req.Target == me.Identity) me.Cast(req.NanoId);
            else me.Cast(req.Target, req.NanoId);
        }

        // ---- Self-vitals prediction ----------------------------------------------

        public void UpdateVitals(LocalPlayer me, double dt, bool inCombat)
        {
            // Settle any heal item we sent but have not seen accepted yet (see VerifyHealUses).
            _lastKnownInCombat = inCombat;
            VerifyHealUses(me);

            if (me.TryGetStat(Stat.MaxHealth, out int maxHp) && maxHp > 0)
            {
                me.TryGetStat(Stat.Health, out int rawHp);
                // Track the REAL stat only — no predictive climb. Predicting regen (especially while sitting)
                // faked a heal: HP read shot up, needRest flipped false, the recharger went null, and he stood
                // before the item ever landed ("sits like he'll heal, then doesn't"). Real HP rises when a
                // real stat update lands (which a recharger use triggers).
                if (_predHp < 0 || rawHp != _lastRawHp) { _predHp = rawHp; _lastRawHp = rawHp; }
            }

            if (me.TryGetStat(Stat.MaxNanoEnergy, out int maxNano) && maxNano > 0)
            {
                me.TryGetStat(Stat.CurrentNano, out int rawNano);
                if (_predNano < 0 || rawNano != _lastRawNano) { _predNano = rawNano; _lastRawNano = rawNano; }
            }
        }

        // -1 means WE DO NOT KNOW: the stat has not arrived, or no reading has been taken yet. It used to
        // return 100 in both cases, so a bot with no stats at all reported a full bar while every cast it
        // tried was refused for want of nano. Unknown has to look like unknown, or a whole session can go
        // by with the log insisting everything is fine.
        public const int Unknown = -1;

        /// <summary>
        /// Is this reading low enough to act on? A percentage we do not have is NOT low - acting on an
        /// unknown is how a bot with no stats sits down to rest forever, or stims itself on a number it
        /// never actually read. Unknown means do nothing and wait for a reading.
        /// </summary>
        private static bool Below(int pct, int threshold) => pct != Unknown && pct < threshold;

        private static bool AtOrBelow(int pct, int threshold) => pct != Unknown && pct <= threshold;

        /// <summary>A percentage for the log: "?" when there is no reading, so a missing stat reads as
        /// missing instead of as a healthy-looking number.</summary>
        private static string Pct(int pct) => pct == Unknown ? "?" : pct + "%";

        public int SelfHpPct(LocalPlayer me)
        {
            if (me == null || !me.TryGetStat(Stat.MaxHealth, out int max) || max <= 0) return Unknown;
            if (_predHp < 0) return Unknown;
            return (int)(100.0 * Math.Min(_predHp, max) / max);
        }

        public int SelfNanoPct(LocalPlayer me)
        {
            if (me == null || !me.TryGetStat(Stat.MaxNanoEnergy, out int max) || max <= 0) return Unknown;
            if (_predNano < 0) return Unknown;
            return (int)(100.0 * Math.Min(_predNano, max) / max);
        }

        // ---- Supplies ------------------------------------------------------------

        public void CheckSupplies(PlayerChar owner, double dt)
        {
            _supplyAccum += dt;
            if (_supplyAccum < 15.0 || owner == null) return;
            _supplyAccum = 0;

            void Tell(string m) { try { Client.SendPrivateMessage(owner.Identity.Instance, m); } catch { } _ctx.Log("SUPPLY: " + m); }

            // Bags load their contents a moment AFTER login, so at first the pool reads 0 even with a full
            // backpack. Don't cry "OUT" until we've actually SEEN some at least once (a real depletion),
            // otherwise the login sync window fires a false alarm.
            // "Low on stims (8 left)" while carrying dozens is not a miscount - it counts the ones he can
            // actually USE, after the First Aid skill gate. Say so, and say how many he is carrying, or the
            // warning reads as a bug and the real problem (QLs above his skill) stays invisible.
            int stims = CountUsableHealItems(_ctx.Config.StimKeyword, _ctx.Config.StimItemName);
            int stimsCarried = CountHealItemsCarried(_ctx.Config.StimKeyword, _ctx.Config.StimItemName);
            if (stims > 0) _everHadStims = true;
            if (stims <= _ctx.Config.LowStimCount)
            {
                if (_everHadStims && !_warnedStim)
                {
                    string extra = stimsCarried > stims
                        ? $" I'm carrying {stimsCarried}; {stimsCarried - stims} the server has refused this session."
                        : "";
                    Tell((stims == 0 ? "I'm OUT of stims I can use — please resupply me." : $"Low on stims ({stims} left) — please resupply.") + extra);
                    _warnedStim = true;

                    // What he is carrying, by QL, with the skills alongside. Not a gate any more - just the
                    // numbers, so a count that looks wrong can be checked against the bag instead of argued about.
                    LocalPlayer meNow = DynelManager.LocalPlayer;
                    string fa = meNow != null && meNow.TryGetStat(Stat.FirstAid, out int faV) ? faV.ToString() : "unknown";
                    string tr = meNow != null && meNow.TryGetStat(Stat.Treatment, out int trV) ? trV.ToString() : "unknown";
                    var byQl = AllInvItems().Where(it => it?.Name != null
                            && it.Name.IndexOf(_ctx.Config.StimKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        .GroupBy(it => it.Ql).OrderBy(g => g.Key)
                        .Select(g => $"QL{g.Key}x{g.Sum(it => Math.Max(1, it.Count))}").ToList();
                    _ctx.Log($"SUPPLY-DBG: FirstAid={fa} Treatment={tr} offered={stims} carried={stimsCarried} "
                             + $"stims=[{string.Join(", ", byQl)}]");
                }
            }
            else _warnedStim = false;

            int rechargers = CountUsableHealItems(_ctx.Config.RechargerKeyword, _ctx.Config.RechargerItemName);
            if (rechargers > 0) _everHadRechargers = true;
            if (rechargers <= _ctx.Config.LowRechargerCount)
            {
                if (_everHadRechargers && !_warnedRecharger) { Tell(rechargers == 0 ? "I'm OUT of rechargers — please resupply me." : $"Low on rechargers ({rechargers} left) — please resupply."); _warnedRecharger = true; }
            }
            else _warnedRecharger = false;
        }

        // Best (highest-QL) stim/recharger the bot can actually USE. Pools main inventory AND open bags.
        // All items in the main inventory AND open bags (one place so every heal query pools the same set).
        private static List<Item> AllInvItems()
        {
            var all = new List<Item>();
            if (Inventory.Items != null) all.AddRange(Inventory.Items);
            if (Inventory.Containers != null)
                foreach (var c in Inventory.Containers)
                    if (c?.Items != null) all.AddRange(c.Items);
            return all;
        }

        /// <summary>Everything in the bags matching this kind of heal item, skill requirement ignored — what he
        /// is CARRYING, as opposed to what he can use.</summary>
        public IEnumerable<Item> HealItemPoolUnfiltered(string keyword, string exactName)
        {
            IEnumerable<Item> poolQ = AllInvItems().Where(it => it?.Name != null);
            return string.IsNullOrEmpty(exactName)
                ? poolQ.Where(it => it.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                : poolQ.Where(it => string.Equals(it.Name, exactName, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<Item> HealItemPool(string keyword, string exactName)
        {
            IEnumerable<Item> poolQ = HealItemPoolUnfiltered(keyword, exactName);
            // We know the item's QL, we know the skill it asks for, and we know what he has - so work it out
            // rather than sending a use and waiting to be told no. His skills are live stats, so a buff that
            // raises First Aid raises what he can reach in the same instant.
            //
            // The one rule that matters: a skill we cannot READ is not a skill of zero. Defaulting a missing
            // stat to 0 failed every requirement, so the whole bag was rejected and this fell through to its
            // last resort - one stack, the lowest QL - and the bot offered 8 QL1 stims while carrying 146
            // across five QLs, every one of them usable. Unknown means do not exclude.
            var matches = poolQ.ToList();
            LocalPlayer meNow = DynelManager.LocalPlayer;
            var usable = matches.Where(it => MeetsHealReqs(it, meNow)).ToList();
            if (usable.Count > 0) return usable;

            // Nothing passes a requirement we could actually read: his skill really is short of every QL he is
            // carrying. Offer the lowest QL - all of it, not one stack - as the best chance of being accepted.
            if (matches.Count == 0) return matches;
            int lowest = matches.Min(it => it.Ql);
            return matches.Where(it => it.Ql == lowest).ToList();
        }

        // Item.MeetsUseReqs returns FALSE when an item has no UseCriteria at all (basic stims have
        // none offline), which wrongly rejects them. ItemBase rule: no criteria = usable.
        public static bool CanUseHeal(Item it)
        {
            try { return ((ItemBase)it).MeetsUseReqs(null, true); } catch { return false; }
        }

        // The reliable heal-usability check: enforce ONLY the real skill gate — First Aid (123) for stims,
        // Treatment (124) for rechargers — against the bot's live skill. No skill criterion = usable (the
        // offline no-criteria stim case). Other criteria are left to the server. This is what stops the bot
        // grabbing the highest-QL item it can't actually use (Use silently fails = "sits, no heal").
        public static bool MeetsHealReqs(Item it, LocalPlayer me)
        {
            try
            {
                if (me == null || it == null || it.Criteria == null) return true;
                if (!it.Criteria.TryGetValue(ItemActionInfo.UseCriteria, out var crits) || crits == null || crits.Count == 0)
                    return true;
                foreach (var c in crits)
                {
                    if (c.Param1 == 123 || c.Param1 == 124)   // First Aid / Treatment
                    {
                        // A skill we cannot READ is not a skill of zero. Defaulting the missing stat to 0
                        // failed every requirement and quietly shrank the usable pool - the bot reported
                        // "8 stims left" while carrying twenty-four it could use. Unknown means don't refuse;
                        // if the item really is out of reach the server refuses the Use and HEAL-REFUSED says so.
                        if (!me.TryGetStat((Stat)c.Param1, out int have)) continue;
                        if (have < c.Param2) return false;    // skill requirement genuinely not met
                    }
                }
                return true;
            }
            catch { return true; }   // criteria unreadable — don't hard-refuse
        }

        public Item BestUsableHealItem(string keyword, string exactName)
            => HealItemPool(keyword, exactName).OrderByDescending(it => it.Ql).FirstOrDefault();

        public int CountUsableHealItems(string keyword, string exactName)
            => HealItemPool(keyword, exactName).Sum(it => Math.Max(1, it.Count));

        /// <summary>Every matching item in the bags, skill gate or not - what he is actually carrying, as
        /// opposed to what he can use. The gap between the two is the thing worth telling him about.</summary>
        public int CountHealItemsCarried(string keyword, string exactName)
            => AllInvItems().Where(it => it?.Name != null)
                .Where(it => string.IsNullOrEmpty(exactName)
                    ? it.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0
                    : string.Equals(it.Name, exactName, StringComparison.OrdinalIgnoreCase))
                .Sum(it => Math.Max(1, it.Count));

        // A CONSUMABLE stim (spent on use) vs a reusable recharger/coil. Discriminated by name: our stims contain
        // the StimKeyword ("Stim") and rechargers the RechargerKeyword ("Recharger"). Rest must not burn stims.
        private bool IsConsumableStim(Item it)
        {
            if (it?.Name == null) return false;
            string k = _ctx.Config.StimKeyword, r = _ctx.Config.RechargerKeyword;
            bool isStim = !string.IsNullOrEmpty(k) && it.Name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0;
            bool isRecharger = !string.IsNullOrEmpty(r) && it.Name.IndexOf(r, StringComparison.OrdinalIgnoreCase) >= 0;
            return isStim && !isRecharger;
        }

        // ---- Shadowlands-ready heal selection (RK behaviour unchanged) -----------
        // Emergency combat heal: RK stim FIRST (byte-identical to before), then, only if there is none
        // (i.e. Shadowlands), the best combat-usable INSTANT profile item (never a slow Coil).
        public Item BestEmergencyHeal(bool needHp, bool needNano)
        {
            Item rk = BestUsableHealItem(_ctx.Config.StimKeyword, _ctx.Config.StimItemName);
            if (rk != null) return rk;
            return BestByProfiles(needHp, needNano, inCombat: true);
        }

        // Out-of-combat recharge item: RK recharger FIRST (identical), then, only if none, the best
        // rest-appropriate profile item (Coil of Health while sitting, Veterans Lab, etc.).
        public Item BestRestHeal(bool needHp, bool needNano)
        {
            Item rk = BestUsableHealItem(_ctx.Config.RechargerKeyword, _ctx.Config.RechargerItemName);
            if (rk != null) return rk;
            return BestByProfiles(needHp, needNano, inCombat: false);
        }

        private Item BestByProfiles(bool needHp, bool needNano, bool inCombat)
        {
            List<HealProfile> profiles = EffectiveHealProfiles().Where(p =>
                ((needHp && p.RestoresHealth) || (needNano && p.RestoresNano)) &&
                (!inCombat || (p.UsableInCombat && !p.RequiresSit && !p.OverTime)))   // emergency = combat, instant, no-sit
                .ToList();
            if (profiles.Count == 0) return null;

            var names = profiles.Select(p => p.NameContains).Where(s => !string.IsNullOrEmpty(s)).ToList();
            var matches = AllInvItems().Where(it => it?.Name != null
                && names.Any(n => it.Name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            var usable = matches.Where(CanUseHeal).ToList();
            return (usable.Count > 0 ? usable : matches).OrderByDescending(it => it.Ql).FirstOrDefault();
        }

        private List<HealProfile> _healProfiles;
        private List<HealProfile> EffectiveHealProfiles()
        {
            if (_healProfiles != null) return _healProfiles;
            _healProfiles = (_ctx.Config.HealProfiles != null && _ctx.Config.HealProfiles.Count > 0)
                ? _ctx.Config.HealProfiles : DefaultHealProfiles();
            return _healProfiles;
        }

        // Built-in defaults covering RK + Shadowlands. RK Stim/Recharger use the configured keywords so the
        // keyword-first path and these stay in sync. RequiresSit on Recharger/Coil-of-Health keeps them OUT of
        // combat emergencies (matches current RK behaviour). Reusable = never counts against supplies.
        private List<HealProfile> DefaultHealProfiles()
        {
            return new List<HealProfile>
            {
                new HealProfile { NameContains = _ctx.Config.StimKeyword,      RestoresHealth = true, RestoresNano = true, UsableInCombat = true },
                new HealProfile { NameContains = _ctx.Config.RechargerKeyword, RestoresHealth = true, RestoresNano = true, RequiresSit = true },
                new HealProfile { NameContains = "First-Aid Kit",             RestoresHealth = true, UsableInCombat = true },
                new HealProfile { NameContains = "Nano Kit",                  RestoresNano = true,  UsableInCombat = true },
                new HealProfile { NameContains = "Coil of Health",            RestoresHealth = true, RequiresSit = true, OverTime = true },
                new HealProfile { NameContains = "Coil of Nano",              RestoresNano = true,  UsableInCombat = true, OverTime = true },
                new HealProfile { NameContains = "Veterans Healing Laboratory", RestoresHealth = true, UsableInCombat = true, Reusable = true },
            };
        }
    }
}
