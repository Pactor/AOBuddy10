using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using Newtonsoft.Json.Linq;

namespace AOBuddy
{
    /// <summary>
    /// CHEWY — deciding which buffs to ask a buffer toon for (GameData/ChewysBuffs.json: name, effect,
    /// ncu, nano id, and the short 'tell' code Chewysfix understands), and doing the asking. The list
    /// covers every profession Chewy's toons play, so the bot picks across all of them.
    ///
    /// THE DEDUCTION: every buff gets a category (offense / defense / sustain / utility) derived from
    /// what its nano actually modifies (NanoItem.Modifiers[Use] — stat ids, not effect text), and a
    /// magnitude (the summed modifier values, normalized per category). Score = weight x magnitude / NCU,
    /// with the three weights configurable (BuffWeightOffense/Defense/Sustain, relative to each other).
    /// Only ONE buff per nano LINE is ever picked (same line = overwrites: two essences, two damage
    /// shields, two wranglers... replace each other), and the picks must fit the bot's FREE NCU. Short
    /// HoTs are never asked for (~3 min runtime, not worth a slot), the leet polymorph is off by default
    /// ('buffs on leet' to allow). Every pick must also FIT THE RECEIVER: the target-side requirements of
    /// what lands on us (level gates, expansion flags — e.g. the umbral wranglers need the receiver at
    /// Level 205/195/185, Composite Teachings needs Shadowlands) are read against our own stats, and a
    /// buff we can't hold is skipped with its reason shown in 'buffs plan'. And OFFENSE must be relevant:
    /// a buff that only raises weapon skills we don't wield ("Composite Ranged Expertise" on a 1h-blunt
    /// fighter) is dead NCU — the equipped weapons' own criteria say which skills and specials we use,
    /// and pet professions (MP, Engineer) with empty hands keep their nano-school offense instead.
    ///
    /// THE ASKING is staged, because some things are prerequisites for others:
    ///   0. travel         — to the buff spot first (BuffTravelX/Z/Playfield, ICC by default): Chewy's
    ///                       toons cast at range, so the bot goes where they can reach it.
    ///   1. 'cast ncu'      — the Fixer team Max-NCU ladder, the BEST tier the bot's level allows (also to
    ///                       replace a weaker running one). The fill is computed against the PROJECTED
    ///                       Max NCU after that upgrade, so the extra space is spent, not wasted.
    ///   2. 'cast c1 c2'    — requirement helpers (attributes / nano expertise) for a wrangle self-cast.
    ///   3. 'cast 131'      — the wrangle itself (largest one that fits).
    ///   4. self-cast       — each configured WrangleCastNanoIds the requirements now allow for.
    ///   5. unwrangle       — bot removes the wrangle itself (LocalPlayer.RemoveFriendlyNano), giving
    ///                        its ~58 NCU back.
    ///   6. 'cast a b c..'  — the rest of the plan, recomputed against the NCU actually free then.
    /// Each stage waits (bounded) for its buffs to land before the next; a timeout logs and moves on —
    /// Chewy is a person's toon, not a service, so nothing retries forever.
    /// </summary>
    public class ChewyBuffController
    {
        private readonly BotContext _ctx;
        private readonly SupportController _support;
        private readonly OverlandController _overland;
        private readonly string _file;
        private readonly Action<string> _tell;   // progress reports to the owner

        private readonly List<BuffDef> _buffs = new List<BuffDef>();
        private bool _loadFailed;
        private bool _resolveLogged;

        // Session-only state (config file is hand-maintained and never written).
        private int _wOff, _wDef, _wSus;                            // focus weights ('buffs focus o d s')
        private readonly HashSet<string> _offTells = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ------------------------------------------------------------------ data model

        private sealed class BuffDef
        {
            public string Profession, Name, Effect, Tell;
            public int Ncu;
            public int? Id;                     // null only for the NCU family (several ids)
            public int[] Ids;                   // the NCU family's four
            public int[] LandIds;               // what actually LANDS on the receiver (umbral wranglers:
                                                // the cast nano team-casts these; the receiver's gates live on them)
            public bool Team;
            // Resolved from the nano's own data at runtime:
            public bool Resolved;               // NanoItem found in ItemData
            public NanoLine Line;               // buffs of one line overwrite each other
            public string LineKey;              // grouping key (line, or tell when unresolved)
            public string Cat;                  // offense|defense|sustain|utility|enabler
            public double Magnitude;            // normalized effect size for scoring
            public HashSet<int> OffenseStatIds; // the OFFENSE stats it boosts (weapon-relevance filter)
            public bool IsWrangler;             // the staged wrangle line (excluded from the normal fill)
            public bool Movement;               // boosts run speed — its line is reserved before the score fill
        }

        public ChewyBuffController(BotContext ctx, SupportController support, OverlandController overland, string pluginDir, Action<string> tell)
        {
            _ctx = ctx;
            _support = support;
            _overland = overland;
            _file = Path.Combine(pluginDir, "GameData", "ChewysBuffs.json");
            _tell = tell;
            _wOff = ctx.Config.BuffWeightOffense;
            _wDef = ctx.Config.BuffWeightDefense;
            _wSus = ctx.Config.BuffWeightSustain;
            Load();
            // Team-cast buffs only land on teammates: accept Chewy's invites so he can team-cast on us.
            // (Main's own handler accepts every invite when AutoAcceptOwnerTeamInvite is on; this one also
            // covers it being off, gated on the inviter being one of the buffer's toons.)
            Team.TeamRequest += (s, e) =>
            {
                try
                {
                    if (!_ctx.Config.BuffAcceptTeamInvite) return;
                    string who = DynelManager.Players.FirstOrDefault(p => p.Identity == e.Requester)?.Name
                                 ?? (Client.Chat.IdToNameMap.TryGetValue((uint)e.Requester.Instance, out string n) ? n : null);
                    if (string.IsNullOrEmpty(who) || !who.StartsWith(_ctx.Config.BufferTeamPrefix, StringComparison.OrdinalIgnoreCase)) return;
                    e.Accept();
                    _ctx.Log($"CHEWY: accepted team invite from {who}.");
                }
                catch { }
            };
        }

        // PetController.LoadShellNanos pattern: JObject.Parse, log-and-carry-on.
        private void Load()
        {
            try
            {
                var doc = JObject.Parse(File.ReadAllText(_file));
                foreach (var prof in doc["professions"])
                {
                    string pName = (string)prof["name"];
                    foreach (var b in prof["buffs"])
                    {
                        int? id = b["id"] == null || b["id"].Type == JTokenType.Null ? (int?)null : (int)b["id"];
                        var def = new BuffDef
                        {
                            Profession = pName,
                            Name = (string)b["name"],
                            Effect = (string)b["effect"] ?? "",
                            Ncu = (int)b["ncu"],
                            Id = id,
                            Ids = b["ids"]?.Select(t => (int)t).ToArray(),
                            LandIds = b["landIds"]?.Select(t => (int)t).ToArray(),
                            Tell = (string)b["tell"] ?? "",
                            Team = b["team"] != null && (bool)b["team"],
                        };
                        if (!string.IsNullOrEmpty(def.Tell)) _buffs.Add(def);
                    }
                }
                _ctx.Log($"CHEWY: loaded {_buffs.Count} buffs from ChewysBuffs.json.");
            }
            catch (Exception ex)
            {
                _loadFailed = true;
                _ctx.Log($"CHEWY: could not read {_file}: {ex.Message} — buff requests disabled.");
            }
        }

        // ------------------------------------------------------------------ categories

        // Which stat modifications count for which category. From the Stat enum / nano data (the ids the
        // nanos actually modify, verified against the OmniCell pack when the ids were resolved).
        private static readonly HashSet<int> OffenseStats = NewSet(
            0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, 0x70, 0x71, 0x72, 0x73, 0x74, // weapon skills
            0x76, 0x77, 0x78, 0x95,                                                                     // melee/ranged/physical/nano init
            0x79, 0x8E, 0x90, 0x92, 0x93, 0x94, 0x96, 0x97, 0xA7,                                       // specials (brawl, sneak, burst...)
            0x7A, 0x7F, 0x80, 0x81, 0x82, 0x83,                                                          // the six nano schools
            0x10, 0x13, 0x14,                                                                            // strength, intelligence, sense
            0x114, 0x116, 0x117, 0x118, 0x119, 0x11A, 0x137, 0x13B, 0x13C, 0x13D, 0x11C);               // add-all-off, damage mods, crit
        private static readonly HashSet<int> DefenseStats = NewSet(
            0x01, 0x11, 0x12, 0x15,                                                                      // max health, agility, stamina, psychic
            0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F, 0x60, 0x61,                                              // armor ACs
            0xCD, 0xCE, 0xCF, 0xD0, 0xD8, 0xD9, 0xDA, 0xDB, 0xE1,                                        // reflect ACs
            0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA,                                        // shield ACs
            0xEE, 0xEF, 0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6,                                        // absorb ACs
            0x8F, 0x91, 0x99, 0x9A, 0x9B, 0xA8, 0x115,                                                  // riposte, parry, evades, nano resist, add-all-def
            0x9C);                                                                                        // run speed: SPEED IS DEFENSE (owner, 2026-09-24) — without it,
                                                                                                          // a +RS/+Evades buff scored on its evades alone (+79 -> ~0.07,
                                                                                                          // always outcompeted) and no runspeed buff was ever asked
        private static readonly HashSet<int> SustainStats = NewSet(
            0x84, 0xDD, 0x157, 0x16C, 0x13E);                                                            // nano pool, max nano, heal delta, nano delta, nano cost
        // Pure tradeskills: chemistry, pharma tech, weapon smithing, nano programming, comp lit, psychology,
        // mechanical/electrical engineering, quantum field physics. Useless in a fight — a mission bot never
        // casts with them (First Aid / Treatment deliberately NOT here: they gate the bot's stims and
        // rechargers). A buff whose every stat lands here is category "tradeskill" and off by default
        // (BuffWeightTradeskill) — the owner already culled the tradeskill composites for exactly this reason.
        private static readonly HashSet<int> TradeskillStats = NewSet(0x7D, 0x7E, 0x9D, 0x9E, 0x9F, 0xA0, 0xA1, 0xA2, 0xA3);
        // Everything else (run speed, perception, XP...) is utility: filler at the average weight.

        private static HashSet<int> NewSet(params int[] v) => new HashSet<int>(v);

        // Per-category magnitude normalizers: how much of a thing one point of weight is "worth". Rough
        // by design — they only line the categories up onto comparable scores, they don't model the game.
        // Sustain is scaled LOW (owner call, 2026-09-24): a long HoT is continuous income and outranks a
        // max-health pool per NCU — Dr Hack 'n Quack (366/10s, 25 NCU) beats Superior Omni-Med Enhancement
        // (+920 pool, 45 NCU), so when NCU gets tight the HoT wins the slot.
        private const double ScaleOffense = 130, ScaleDefense = 750, ScaleSustain = 150, ScaleUtility = 150;
        private const int StatRunSpeed = 0x9C;   // Stat 156, the id BotContext.RunVelocity reads

        /// <summary>Resolve every buff's nano data (line, category, magnitude). Needs ItemData loaded;
        /// returns false to try again on a later tick. Text fallbacks keep the plan working without it.
        /// For buffs with landIds (umbral wranglers) the FIRST landed nano is the one resolved: it carries
        /// the actual stat modifications and the line (220 = the wrangler line), while the cast nano is a
        /// pure team-caster with no modifiers of its own.</summary>
        private bool Resolve()
        {
            if (_loadFailed) return false;
            bool all = true;
            foreach (var b in _buffs.Where(b => !b.Resolved))
            {
                int? id = b.LandIds?.FirstOrDefault() ?? b.Id ?? b.Ids?.FirstOrDefault();
                if (id.HasValue && ItemData.Find(id.Value, out NanoItem ni) && ni != null)
                {
                    b.Resolved = true;
                    b.Line = ni.NanoLine;
                    b.LineKey = "L" + (int)ni.NanoLine;
                    b.Cat = CategoryOf(b, ni);
                    b.Magnitude = MagnitudeOf(b, ni, b.Cat);
                    if (ni.Modifiers != null && ni.Modifiers.TryGetValue(SpellListType.Use, out var use2) && use2 != null)
                    {
                        b.OffenseStatIds = new HashSet<int>(use2.Where(kv => OffenseStats.Contains((int)kv.Key)).Select(kv => (int)kv.Key));
                        b.Movement = use2.Keys.Any(k => (int)k == StatRunSpeed);
                    }
                }
                else
                {
                    // No item data (yet, or ever for this id): fall back to the effect text.
                    b.Movement = Regex.IsMatch(b.Effect ?? "", @"\bRS\b|runspeed", RegexOptions.IgnoreCase);
                    b.LineKey = "T" + b.Tell;
                    b.Cat = CategoryOf(b, null);
                    b.Magnitude = MagnitudeOf(b, null, b.Cat);
                    if (id.HasValue) all = false;   // might resolve once ItemData is in
                }
                b.IsWrangler = b.Profession == "Trader" && b.Effect.IndexOf("Weap/Nano", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            if (!all && !_resolveLogged)
            {
                _resolveLogged = true;
                _ctx.Log("CHEWY: item data not fully available — categories from effect text for now.");
            }
            return true;
        }

        // ---------------------------------------------------------------- receiver fit

        /// <summary>
        /// Does the RECEIVER (us) satisfy a buff's target-side requirements? What Chewy casts on us can be
        /// refused — the umbral wranglers gate the receiver's LEVEL (205/195/185 on the nanos that LAND,
        /// not the trader's cast nano), Composite Teachings gates the receiver's Expansion flags. Scope
        /// tracking picks out exactly the receiver's gates: OnTarget-scoped criteria of a CAST nano (its
        /// plain criteria are the CASTER's — profession, skills — and never ours to fail), while a LANDED
        /// nano states its gates plainly. Profession/VisualProfession gates the caster either way, so they
        /// are always skipped: no buff ever needs the RECEIVER to be of a class. Returns null when the
        /// buff fits us, else a short human reason.
        /// </summary>
        private static string ReceiverBlockReason(LocalPlayer me, int[] nanoIds, bool landed)
        {
            if (nanoIds == null) return null;
            foreach (int id in nanoIds)
            {
                if (!ItemData.Find(id, out NanoItem ni) || ni?.Criteria == null) continue;
                if (!ni.Criteria.TryGetValue(ItemActionInfo.UseCriteria, out List<RequirementCriterion> criteria)) continue;
                bool? targetScope = landed ? (bool?)true : (bool?)false;   // cast nano: only OnTarget counts; landed: plain counts
                foreach (var c in criteria)
                {
                    if (c.Operator == UseCriteriaOperator.OnTarget) { targetScope = true; continue; }
                    if (c.Operator == UseCriteriaOperator.OnUser) { targetScope = false; continue; }
                    if (c.Operator == UseCriteriaOperator.OnFightingTarget) { targetScope = false; continue; }
                    if (targetScope == false) continue;   // caster-side — Chewy's business
                    // Profession gates: on a CAST nano they name the CASTER (who may cast it — e.g. Improved
                    // Semi-Sentient Augmentation Cloud's fixer/agent gate), so they are never ours to fail
                    // even if the data scope-mark says otherwise. On a LANDED nano a plain profession gate
                    // really is the receiver's (buffs with target-profession requirements exist — none in
                    // this list yet, but the JSON may grow some), so there it IS checked.
                    if (!landed && Enum.IsDefined(typeof(Stat), c.Param1)
                        && ((Stat)c.Param1 == Stat.Profession || (Stat)c.Param1 == Stat.VisualProfession)) continue;
                    if (c.Operator == UseCriteriaOperator.BitAnd || c.Operator == UseCriteriaOperator.NotBitAnd)
                    {
                        // Flag gates, e.g. Composite Teachings needs "Expansion has Shadowlands" —
                        // stat 389 BitAnd 2 on the TARGET (a froob receiver is refused). Missing stat
                        // reads as 0 flags, same convention as ReqChecker.CheckStat.
                        if (!Enum.IsDefined(typeof(Stat), c.Param1)) continue;
                        me.TryGetStat((Stat)c.Param1, out int flags);
                        bool bitOk = c.Operator == UseCriteriaOperator.BitAnd ? (flags & c.Param2) == c.Param2 : (flags & c.Param2) != c.Param2;
                        if (!bitOk)
                            return $"{ni.Name}: receiver needs {(Stat)c.Param1} {(c.Operator == UseCriteriaOperator.BitAnd ? "with" : "without")} flags 0x{c.Param2:X} (has 0x{flags:X})";
                        continue;
                    }
                    if (c.Operator != UseCriteriaOperator.GreaterThan && c.Operator != UseCriteriaOperator.EqualTo
                        && c.Operator != UseCriteriaOperator.LessThan && c.Operator != UseCriteriaOperator.Unequal) continue;
                    if (!Enum.IsDefined(typeof(Stat), c.Param1)) continue;
                    me.TryGetStat((Stat)c.Param1, out int cur);
                    // GreaterThan/LessThan are STRICT on the stored value: the umbral wranglers store 204/194/184
                    // and the game requires the receiver at 205/195/185 (owner-verified, 2026-09-24) — AO item
                    // data keeps the exclusive threshold and the game shows value+1.
                    bool ok = c.Operator == UseCriteriaOperator.GreaterThan ? cur > c.Param2
                            : c.Operator == UseCriteriaOperator.LessThan ? cur < c.Param2
                            : c.Operator == UseCriteriaOperator.EqualTo ? cur == c.Param2
                            : cur != c.Param2;
                    if (!ok)
                    {
                        int shown = c.Operator == UseCriteriaOperator.GreaterThan ? c.Param2 + 1
                                  : c.Operator == UseCriteriaOperator.LessThan ? c.Param2 - 1 : c.Param2;
                        return $"{ni.Name}: receiver needs {(Stat)c.Param1} {OpText(c.Operator)} {shown} (has {cur})";
                    }
                }
            }
            return null;
        }

        private static string OpText(UseCriteriaOperator op)
            => op == UseCriteriaOperator.GreaterThan ? ">=" : op == UseCriteriaOperator.LessThan ? "<=" : op == UseCriteriaOperator.EqualTo ? "==" : "!=";

        /// <summary>A hostile running nano (debuff): its modifiers read net-negative on the wearer — a
        /// snare's RunSpeed -289, resurrection sickness. Buffs only ever add. Unknown nanos with no data
        /// are treated as friendly: the server refuses a hostile removal anyway, so the mis-call is safe.</summary>
        private static bool IsHostile(NanoItem ni)
        {
            if (ni?.Modifiers == null || !ni.Modifiers.TryGetValue(SpellListType.Use, out var use) || use == null || use.Count == 0) return false;
            return use.Values.Sum() < 0;
        }

        /// <summary>The ids that must fit the receiver: what lands (landIds), else the nano itself.</summary>
        private static int[] ReceiverIds(BuffDef b) => b.LandIds ?? b.Ids ?? (b.Id.HasValue ? new[] { b.Id.Value } : null);

        // ---------------------------------------------------------------- weapon relevance

        // Stats that only help WITH a matching weapon: the weapon skills and the fired specials. An
        // offense buff that boosts nothing else is dead weight without such a weapon equipped ("Composite
        // Ranged Expertise on a 1h-blunt fighter"). Everything else (damage adds, crit, AAO, nano schools,
        // initiatives) helps whatever does the damage — including pets.
        private static readonly HashSet<int> WeaponishStats = NewSet(
            0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, 0x70, 0x71, 0x72, 0x73, 0x74, // weapon skills
            0x79,                                                                                                    // bow special attack
            0x85, 0x86,                                                                                              // ranged energy, multi ranged
            0x8E, 0x90, 0x92, 0x93, 0x94, 0x96, 0x97, 0xA7, 0x1E9);                                                  // brawl, dimach, sneak/fast/burst/fling/aimed/fullauto, backstab

        // Whose damage comes from PETS, not from a swung weapon: with nothing in hand their "weapon skill"
        // is none at all — their offense rides on the nano schools (stronger summons, pet buffs), which the
        // filter passes anyway. Everyone else bare-handed fights with fists = Martial Arts.
        private static readonly HashSet<int> PetProfessions = NewSet(3, 12);   // Engineer, Meta-Physicist

        // The weapon SIDE also accepts the nano schools as attack skills: MP summoned weapons fight with
        // Time&Space (the Viper Staff line — 'Attack skills: Time&Space 80% 1h Blunt 20%'), so a school in
        // a weapon's criteria IS its weapon skill. (Buff-side, schools stay un-weaponish: a school boost is
        // pet/summon power and always passes.)
        private static bool IsWeaponSideStat(int s)
            => WeaponishStats.Contains(s) || s == 0x7A || (s >= 0x7F && s <= 0x83);

        /// <summary>The stats our equipped weapon(s) actually use: wield skill + the specials they allow,
        /// read from the weapons' own criteria (CombatController.EquippedWeapons/AllowedWeaponSpecials —
        /// hand slots only, so HUD/belt/NCU junk doesn't count). A wielded weapon that names NO skill we
        /// recognize sets UnknownWeapon instead: the filter then stands down rather than guess, because
        /// such a weapon's skills are real but unreadable. No weapon at all + pet profession = empty
        /// (pets); no weapon otherwise = fists (Martial Arts).</summary>
        private static HashSet<int> EquippedWeaponStats(LocalPlayer me, out bool unknownWeapon, out string weaponNames)
        {
            var stats = new HashSet<int>();
            var weapons = CombatController.EquippedWeapons().ToList();
            unknownWeapon = false;
            weaponNames = string.Join(", ", weapons.Select(w => $"'{w.Name}'"));
            foreach (var w in weapons)
            {
                if (w.Criteria == null) continue;
                foreach (var kv in w.Criteria)
                    foreach (var c in kv.Value)
                        if (IsWeaponSideStat(c.Param1))
                            stats.Add(c.Param1);
            }
            foreach (Stat s in CombatController.AllowedWeaponSpecials()) stats.Add((int)s);
            if (stats.Count == 0)
            {
                if (weapons.Count > 0) unknownWeapon = true;   // wielding something unreadable — stand down
                else if (me != null && me.TryGetStat(Stat.Profession, out int prof) && !PetProfessions.Contains(prof))
                    stats.Add((int)Stat.MartialArts);   // bare fists
            }
            return stats;
        }

        /// <summary>Null when the buff's offense reaches whatever we fight with; else a short reason. Only
        /// offense buffs whose EVERY boosted stat is weapon-ish get judged — mixed buffs (schools + a
        /// weapon skill) and pure damage/crit/nano buffs always pass, so pet professions keep their
        /// summoning power. UnknownWeapon stands the whole check down.</summary>
        private static string WeaponBlockReason(HashSet<int> weaponStats, bool unknownWeapon, BuffDef b)
        {
            if (unknownWeapon) return null;
            if (b.Cat != "offense" || b.OffenseStatIds == null || b.OffenseStatIds.Count == 0) return null;
            var weaponish = b.OffenseStatIds.Where(WeaponishStats.Contains).ToList();
            if (weaponish.Count == 0 || weaponish.Count < b.OffenseStatIds.Count) return null;   // has non-weapon offense too
            return weaponish.Any(weaponStats.Contains) ? null
                : $"boosts {string.Join("/", weaponish.Select(s => ((Stat)s).ToString()))} — no such weapon equipped";
        }

        private static string CategoryOf(BuffDef b, NanoItem ni)
        {
            string eff = b.Effect ?? "";
            if (eff.IndexOf("Short HoT", StringComparison.OrdinalIgnoreCase) >= 0) return "excluded";
            if (string.Equals(b.Tell, "leet", StringComparison.OrdinalIgnoreCase)) return "excluded";
            if (string.Equals(b.Tell, "ncu", StringComparison.OrdinalIgnoreCase)) return "enabler";
            // HoT buffs tick rather than modify a stat; the text is the only place they say so.
            if (eff.IndexOf("HoT", StringComparison.OrdinalIgnoreCase) >= 0) return "sustain";
            if (ni?.Modifiers != null && ni.Modifiers.TryGetValue(SpellListType.Use, out var use) && use != null && use.Count > 0)
            {
                double off = 0, def = 0, sus = 0, ts = 0;
                foreach (var kv in use)
                {
                    int v = Math.Abs(kv.Value);
                    if (OffenseStats.Contains((int)kv.Key)) off += v;
                    else if (DefenseStats.Contains((int)kv.Key)) def += v;
                    else if (SustainStats.Contains((int)kv.Key)) sus += v;
                    else if (TradeskillStats.Contains((int)kv.Key)) ts += v;
                }
                if (off == 0 && def == 0 && sus == 0 && ts > 0 && use.Keys.All(k => TradeskillStats.Contains((int)k))) return "tradeskill";
                if (def >= off && def >= sus && def > 0) return "defense";
                if (off >= def && off >= sus && off > 0) return "offense";
                if (sus > 0) return "sustain";
                return "utility";
            }
            // Text fallback when no usable modifiers.
            if (Regex.IsMatch(eff, @"Max Health|AC|Shield|Evade|NR|Layers|Reflect", RegexOptions.IgnoreCase)) return "defense";
            if (Regex.IsMatch(eff, @"Skill|Damage|crit|init|Aimed|Rifle|Pistol|Burst", RegexOptions.IgnoreCase)) return "offense";
            if (Regex.IsMatch(eff, @"Nano Cost|Max Nano|delta", RegexOptions.IgnoreCase)) return "sustain";
            return "utility";
        }

        private static double MagnitudeOf(BuffDef b, NanoItem ni, string cat)
        {
            double raw = 0;
            if (ni?.Modifiers != null && ni.Modifiers.TryGetValue(SpellListType.Use, out var use) && use != null)
            {
                HashSet<int> set = cat == "offense" ? OffenseStats : cat == "defense" ? DefenseStats : cat == "sustain" ? SustainStats : null;
                foreach (var kv in use)
                    if ((set == null || set.Contains((int)kv.Key)) && kv.Value > 0)
                        raw += cat == "sustain" && (int)kv.Key == 0x13E ? Math.Abs(kv.Value) : kv.Value;
                if (raw <= 0) raw = use.Values.Where(v => v > 0).DefaultIfEmpty(1).Max();   // unknown stats: take the biggest
            }
            if (raw <= 0)
            {
                // Text: "+819 HP", "+466-502 HoT", "+131 Weap/Nano skills", "745 Layers" — take the largest number.
                foreach (Match m in Regex.Matches(b.Effect ?? "", @"\d+")) raw = Math.Max(raw, int.Parse(m.Value));
            }
            if (raw <= 0) raw = 1;
            switch (cat)
            {
                case "offense": return Math.Min(3.0, raw / ScaleOffense);
                case "defense": return Math.Min(3.0, raw / ScaleDefense);
                case "sustain": return Math.Min(3.0, raw / ScaleSustain);
                default: return Math.Min(3.0, raw / ScaleUtility);
            }
        }

        private double WeightOf(string cat)
        {
            switch (cat)
            {
                case "offense": return Math.Max(1, _wOff);
                case "defense": return Math.Max(1, _wDef);
                case "sustain": return Math.Max(1, _wSus);
                case "tradeskill": return _ctx.Config.BuffWeightTradeskill;           // 0 = never picked
                case "enabler": return Math.Max(_wOff, Math.Max(_wDef, _wSus)) * 2;   // NCU expansion first, always
                default: return (_wOff + _wDef + _wSus) / 3.0;                        // utility = average filler
            }
        }

        // ------------------------------------------------------------------ running buffs

        /// <summary>A buff of this line is already running with a healthy timer: don't re-ask.
        /// The 50%-remaining rule (BuffSkipRemainingPct) — a re-ask for something half-dead is wasted
        /// tells and a dip in NCU. Unknown timer (-1) counts as up, as NeedsBuff does.</summary>
        private bool RunningHealthy(LocalPlayer me, BuffDef b, bool force, out string why)
        {
            why = null;
            if (force) return false;
            Buff active = null;
            foreach (var x in me.Buffs)
            {
                if (b.Id == x.Id || (b.Ids != null && b.Ids.Contains(x.Id)) || (b.LandIds != null && b.LandIds.Contains(x.Id))) { active = x; break; }
                if (b.Resolved && x.NanoItem != null && x.NanoItem.NanoLine == b.Line) { active = x; break; }
            }
            if (active == null) return false;
            double rem = active.Cooldown?.RemainingTime ?? -1;
            if (rem <= 0) return false;   // expired or about to — re-ask
            double total = active.NanoItem?.TotalTime ?? 0;
            if (total > 0)
            {
                int pct = (int)(rem / total * 100);
                if (pct > _ctx.Config.BuffSkipRemainingPct) { why = $"{rem / 60:0} min left ({pct}%)"; return true; }
                return false;
            }
            if (rem > 300) { why = $"{rem / 60:0} min left"; return true; }   // unknown length but plenty left
            return false;
        }

        // ---- NCU expansion: budget projection ------------------------------------------
        // The NCU family is a ladder (+20/+40/+60/+150 Max NCU, one nano line — they overwrite). The plan
        // must (a) ASK whenever a better tier fits the bot's level, not only when none runs, and (b) compute
        // the fill against the PROJECTED Max NCU, else the extra space lands after the plan already filled
        // the smaller budget and goes to waste.

        /// <summary>The +MaxNCU the RUNNING tier gives (0 without one) — read from the landed nano's own
        /// modifiers, so it needs no stat readback.</summary>
        private static int NcuRunningGain(LocalPlayer me, BuffDef ncuDef)
        {
            int[] landed = ncuDef?.LandIds ?? ncuDef?.Ids;
            if (landed == null) return 0;
            Buff running = me.Buffs.FirstOrDefault(x => landed.Contains(x.Id));
            if (running == null) return 0;
            return running.NanoItem?.Modifiers != null
                   && running.NanoItem.Modifiers.TryGetValue(SpellListType.Use, out var use) && use != null
                   && use.TryGetValue(Stat.MaxNCU, out int gain) ? gain : 0;
        }

        /// <summary>The best tier's +MaxNCU the bot's own gates allow (level gates live on the landed
        /// nanos). 0 when no tier fits (or nothing resolves).</summary>
        private static int BestNcuGain(LocalPlayer me, BuffDef ncuDef)
        {
            int[] landed = ncuDef?.LandIds ?? ncuDef?.Ids;
            if (landed == null) return 0;
            int best = 0;
            foreach (int id in landed)
            {
                if (ReceiverBlockReason(me, new[] { id }, true) != null) continue;   // we can't hold this tier
                if (!ItemData.Find(id, out NanoItem ni) || ni?.Modifiers == null) continue;
                if (!ni.Modifiers.TryGetValue(SpellListType.Use, out var use) || use == null) continue;
                if (use.TryGetValue(Stat.MaxNCU, out int gain) && gain > best) best = gain;
            }
            return best;
        }

        private static int FreeNcu(LocalPlayer me)
        {
            me.TryGetStat(Stat.MaxNCU, out int max);
            me.TryGetStat(Stat.CurrentNCU, out int cur);
            // CurrentNCU reads 0 when the stat never arrives in the clientless payload — "free" then showed
            // the TOTAL (owner report, 2026-09-24: 228 shown free while buffs were running). The running
            // buffs' own NCU costs are the truth then; a real stat read (>= the sum) still wins.
            int used = 0;
            if (me.Buffs != null)
                foreach (var b in me.Buffs)
                    if (b.NanoItem != null) used += b.NanoItem.NCU;
            return Math.Max(0, max - Math.Max(cur, used));
        }

        // ------------------------------------------------------------------ the plan

        private sealed class Pick { public BuffDef Buff; }

        private sealed class Plan
        {
            public bool AskNcu;
            public int NcuGainPending;                                 // extra Max NCU the ask will add (tier upgrade)
            public bool NeedsTeam;                                     // a team-cast buff is in the ask
            public List<BuffDef> Reqs = new List<BuffDef>();          // stage 2 helpers
            public BuffDef Wrangle;                                    // null = no wrangle stage
            public List<int> WrangleTargets = new List<int>();         // self-cast ids needing it
            public List<int> DirectCasts = new List<int>();            // castable without a wrangle — queued by the ask
            public List<(Stat Stat, int Need)> Gaps = new List<(Stat, int)>();
            public List<Pick> Rest = new List<Pick>();
            public List<string> Skipped = new List<string>();
            public int FreeNcuAtPlan;
        }

        private Plan BuildPlan(LocalPlayer me, bool force)
        {
            var p = new Plan();
            // NCU budget PROJECTED through the expansion: ask for the best tier our level allows (even to
            // replace a weaker running one) and fit the fill into the space that leaves us with.
            BuffDef ncuDef = NcuDef();
            int runningGain = NcuRunningGain(me, ncuDef);
            int bestGain = BestNcuGain(me, ncuDef);
            p.AskNcu = bestGain > runningGain;
            p.NcuGainPending = Math.Max(0, bestGain - runningGain);
            p.FreeNcuAtPlan = FreeNcu(me) + p.NcuGainPending;

            // Wrangle stage inputs: configured self-casts that are NOT castable yet and NOT running. The
            // ones that ARE castable go on the direct list — the ask queues those, building a plan must
            // not cast anything (it is also the 'buffs plan' preview).
            foreach (int id in _ctx.Config.WrangleCastNanoIds)
            {
                if (me.Buffs.Find(id, out _)) continue;   // already up
                var shortfalls = ShortfallsOf(me, id);
                if (shortfalls.Count == 0) { p.DirectCasts.Add(id); continue; }
                p.WrangleTargets.Add(id);
                foreach (var s in shortfalls) p.Gaps.Add(s);
            }

            // Stage-2 helpers from the shortfalls: attributes -> Composite Attribute Boost, nano schools ->
            // Composite Nano Expertise (looked up by name so the tell codes come from the JSON).
            if (p.WrangleTargets.Count > 0)
            {
                if (p.Gaps.Any(s => IsAbility(s.Stat))) AddByName(p.Reqs, "Composite Attribute Boost");
                if (p.Gaps.Any(s => IsNanoSchool(s.Stat))) AddByName(p.Reqs, "Composite Nano Expertise");
            }

            // The wrangle itself: the biggest one (they're one line, they overwrite) — picked when the ask
            // runs, against the NCU free AFTER stages 1-2; here just remember the family exists.
            if (p.WrangleTargets.Count > 0)
                p.Wrangle = _buffs.Where(b => b.IsWrangler && !b.Team)
                                  .OrderByDescending(b => ParseFirst(b.Effect))
                                  .FirstOrDefault();

            // The fill: one pick per nano line, best score first, fitted into free NCU. A line whose every
            // variant runs healthy is skipped wholesale (asking again would only replace like with like);
            // session-excluded codes ('buffs off <code>') drop out here too.
            var byLine = _buffs.Where(b => b.Cat != "excluded" && b.Cat != "enabler" && !b.IsWrangler && !_offTells.Contains(b.Tell))
                               .GroupBy(b => b.LineKey)
                               .ToList();
            var candidates = new List<BuffDef>();
            HashSet<int> weaponStats = null;
            bool unknownWeapon = false;
            string weaponNames = null;
            bool weaponsRead = false;
            foreach (var line in byLine)
            {
                var alive = new List<BuffDef>();
                foreach (var b in line)
                {
                    if (RunningHealthy(me, b, force, out _)) continue;
                    if (b.Cat == "tradeskill" && _ctx.Config.BuffWeightTradeskill <= 0)
                    {
                        p.Skipped.Add($"{b.Name} (tradeskill — not for missions; BuffWeightTradeskill > 0 to allow)");
                        continue;
                    }
                    // Receiver fit: Chewy can only cast on us what OUR side satisfies (umbral wranglers:
                    // Level >= 205 — on the landed nanos, which is why landIds exist in the JSON).
                    string block = ReceiverBlockReason(me, ReceiverIds(b), b.LandIds != null);
                    if (block != null) { p.Skipped.Add($"{b.Name} ({block})"); continue; }
                    // Weapon relevance (weapon stats read once per plan): offense that only buffs weapon
                    // skills we don't wield is dead NCU.
                    if (!weaponsRead) { weaponStats = EquippedWeaponStats(me, out unknownWeapon, out weaponNames); weaponsRead = true; }
                    string wblock = WeaponBlockReason(weaponStats, unknownWeapon, b);
                    if (wblock != null) { p.Skipped.Add($"{b.Name} ({wblock})"); continue; }
                    alive.Add(b);
                }
                if (alive.Count > 0) candidates.AddRange(alive);
            }
            if (unknownWeapon)
                _ctx.Log($"CHEWY: wielded {weaponNames} names no attack skill in its data — weapon filter stands down (all offense allowed).");
            foreach (var g in byLine.Where(g => !candidates.Any(c => c.LineKey == g.Key)))
            {
                string why = null;
                RunningHealthy(me, g.First(), force, out why);
                p.Skipped.Add($"{g.First().Name}{(why != null ? " (" + why + ")" : " (line already covered)")}");
            }

            int free = p.FreeNcuAtPlan;

            // MOVEMENT: the runspeed line is RESERVED before the score fill (owner, 2026-09-24: there must
            // always be at least a smaller runspeed buff — speed is the best defensive layer). The BIGGEST
            // tier that fits the projected NCU — magnitude, not score, which divides by NCU and would
            // always pick the smallest rung — stepping down the ladder only when it is tight. A line
            // already running healthy was filtered out above, so this never re-asks what is up.
            var move = candidates.Where(b => b.Movement && b.Ncu <= free)
                                 .OrderByDescending(x => x.Magnitude)
                                 .FirstOrDefault();
            if (move != null) { p.Rest.Add(new Pick { Buff = move }); free -= move.Ncu; }

            foreach (var b in candidates.OrderByDescending(b => WeightOf(b.Cat) * b.Magnitude / Math.Max(1, b.Ncu)))
            {
                // One per line: earlier pick of the same line wins.
                if (p.Rest.Any(x => x.Buff.LineKey == b.LineKey)) continue;
                if (b.Ncu > free)
                {
                    // Try the next weaker variant of the same line (the essence/damage-shield ladders).
                    var weaker = candidates.Where(x => x.LineKey == b.LineKey && x.Ncu <= free && !p.Rest.Any(y => y.Buff.LineKey == x.LineKey))
                                           .OrderByDescending(x => WeightOf(x.Cat) * x.Magnitude / Math.Max(1, x.Ncu)).FirstOrDefault();
                    if (weaker != null) { p.Rest.Add(new Pick { Buff = weaker }); free -= weaker.Ncu; }
                    continue;
                }
                p.Rest.Add(new Pick { Buff = b });
                free -= b.Ncu;
            }
            p.NeedsTeam = p.AskNcu && NcuDef()?.Team == true || p.Rest.Any(x => x.Buff.Team) || p.Reqs.Any(r => r.Team);
            return p;
        }

        private BuffDef NcuDef() => _buffs.FirstOrDefault(b => string.Equals(b.Tell, "ncu", StringComparison.OrdinalIgnoreCase));

        private void AddByName(List<BuffDef> list, string name)
        {
            var b = _buffs.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (b != null && !list.Contains(b)) list.Add(b);
        }

        private static int ParseFirst(string s)
        {
            Match m = Regex.Match(s ?? "", @"\d+");
            return m.Success ? int.Parse(m.Value) : 0;
        }

        // Wrangle-gap analysis: which GreaterThan skill/ability criteria of the nano aren't met yet.
        // Only the UseCriteria list (the same one MeetsUseReqs evaluates), so wield/other gates don't
        // pollute the shortfall with stats no buff can raise.
        private static List<(Stat Stat, int Need)> ShortfallsOf(LocalPlayer me, int nanoId)
        {
            var result = new List<(Stat, int)>();
            if (!ItemData.Find(nanoId, out NanoItem ni) || ni?.Criteria == null) return result;
            if (!ni.Criteria.TryGetValue(ItemActionInfo.UseCriteria, out List<RequirementCriterion> criteria)) return result;
            foreach (var c in criteria)
                if (c.Operator == UseCriteriaOperator.GreaterThan && Enum.IsDefined(typeof(Stat), c.Param1))
                {
                    var st = (Stat)c.Param1;
                    if (!IsAbility(st) && !IsSkillStat(st)) continue;
                    me.TryGetStat(st, out int cur);
                    int need = c.Param2 + 1 - cur;   // strict: stored value needs cur strictly above it (see ReceiverBlockReason)
                    if (need > 0) result.Add((st, need));
                }
            return result;
        }

        private static bool IsAbility(Stat s) => s >= Stat.Strength && s <= Stat.Psychic;

        private static bool IsSkillStat(Stat s)
        {
            int v = (int)s;
            return v >= 0x64 && v <= 0xA7;   // weapon/utility/nano-school skills (schools included in the range)
        }

        private static bool IsNanoSchool(Stat s)
        {
            int v = (int)s;
            return v == 0x7A || (v >= 0x7F && v <= 0x83);
        }

        // ------------------------------------------------------------------ the ask (staged)

        private enum Stage { Idle, Travel, NcuWait, ReqWait, WrangleWait, SelfCast, UnwrangleWait, RestSend, Done }

        private Stage _stage = Stage.Idle;
        private double _stageTime;
        private Plan _plan;
        private bool _force;
        private int _wrangleIdAsked;                       // the nano id we asked Chewy to wrangle us with
        private readonly HashSet<int> _selfQueued = new HashSet<int>();
        private string _lastSummary = "never run";
        private bool _travelOurs;                          // we started the trip to the buff spot

        // The buff spot (BuffTravelX/Z/Playfield): close enough that Chewy's toons can cast on us there.
        private const float SpotMetres = 15f;

        public bool Asking => _stage != Stage.Idle && _stage != Stage.Done;

        /// <summary>One-shot startup ask (BuffAskOnStart): after the grace delay, once, not rez-sick.</summary>
        public void StartupTick(LocalPlayer me, double dt)
        {
            if (_loadFailed || !_ctx.Config.BuffAskOnStart || _started) return;
            if (me == null) return;
            _startupAccum += dt;
            if (_startupAccum < _ctx.Config.BuffStartupDelaySec) return;
            _started = true;
            if (SupportController.IsRezSick(me)) { _ctx.Log("CHEWY: rez-sick at startup — skipping the buff ask."); return; }
            BeginAsk(me, false, null);
        }
        private double _startupAccum;
        private bool _started;

        private bool AtSpot(LocalPlayer me)
            => (int)Playfield.ModelId == _ctx.Config.BuffTravelPlayfield
               && Movement.Flat(me.Transform.Position, _ctx.Config.BuffTravelX, _ctx.Config.BuffTravelZ) <= SpotMetres;

        /// <summary>Stage 0: travel to the buff spot (travelto does the routing; we just wait it out).
        /// Returns true when the trip is underway and the ask should continue from Tick.</summary>
        private bool StartTravel(LocalPlayer me, Action<string> reply)
        {
            var c = _ctx.Config;
            string[] args = { c.BuffTravelX.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                              c.BuffTravelZ.ToString("0", System.Globalization.CultureInfo.InvariantCulture),
                              c.BuffTravelPlayfield.ToString(System.Globalization.CultureInfo.InvariantCulture) };
            _travelOurs = true;
            _stage = Stage.Travel; _stageTime = 0; _teamRemindAt = -999;
            _overland.Command(args, s => { _ctx.Log("CHEWY: " + s); _tell?.Invoke(s); });
            if (!_overland.Active)
            {
                // travelto says "already there" (or it failed to plan): arrived instantly or not at all.
                _travelOurs = false;
                _stage = Stage.Idle;
                if (AtSpot(me)) return false;   // there — continue the ask right away
                Say(reply, $"Could not travel to the buff spot ({args[0]},{args[1]} pf {args[2]}) — see log.");
                return true;                    // abort: no point asking from where we stand
            }
            Say(reply, $"Travelling to {BufName}'s buff spot first ('buffs stop' cancels)...");
            return true;
        }

        public void BeginAsk(LocalPlayer me, bool force, Action<string> reply)
        {
            if (_loadFailed) { reply?.Invoke("Buff list failed to load — see log."); return; }
            if (!Resolve()) { reply?.Invoke("Item data not loaded yet — try again in a moment."); return; }
            if (Asking) { reply?.Invoke($"Already asking ({_stage}). 'buffs stop' to cancel."); return; }
            _force = force;

            // Stage 0: get to the buff spot first — Chewy's toons cast at range, and the owner put the
            // spot where all of them can reach (ICC by default).
            if (_ctx.Config.BuffTravelToSpot && !AtSpot(me))
            {
                _plan = BuildPlan(me, force);   // remember what we're going to ask for once there
                if (StartTravel(me, reply)) return;
            }
            ContinueAsk(me, reply);
        }

        private void ContinueAsk(LocalPlayer me, Action<string> reply)
        {
            _plan = BuildPlan(me, _force);

            // Self-casts that need no wrangle at all go straight through the shared cast queue.
            foreach (int id in _plan.DirectCasts)
                _support.QueueCast(new CastRequest { OnSelf = true, NanoId = id, Label = "chewy direct selfcast" });

            // Stage 1: NCU expansion — the best tier the bot's level allows, even over a weaker running one.
            if (_plan.AskNcu && !string.IsNullOrEmpty(TellOf("ncu")))
            {
                _ncuGainAtAsk = NcuRunningGain(me, NcuDef());
                SendTell("cast " + TellOf("ncu"));
                _stage = Stage.NcuWait; _stageTime = 0; _teamRemindAt = -999;
                Say(reply, $"Asking {BufName} for NCU space first (+{_plan.NcuGainPending} more, then {_plan.FreeNcuAtPlan} free)...");
                return;
            }
            AdvanceToReq(me, reply);
        }

        private int _ncuGainAtAsk;   // the running tier's gain when 'cast ncu' was sent (0 = none)

        private void AdvanceToReq(LocalPlayer me, Action<string> reply)
        {
            // Stage 2: requirement helpers (only when something needs a wrangle to be self-cast).
            if (_plan.Reqs.Count > 0)
            {
                SendTell("cast " + string.Join(" ", _plan.Reqs.Select(r => r.Tell)));
                _stage = Stage.ReqWait; _stageTime = 0; _teamRemindAt = -999;
                Say(reply, $"Requirement helpers: {string.Join(", ", _plan.Reqs.Select(r => r.Name))}.");
                return;
            }
            AdvanceToWrangle(me, reply);
        }

        private void AdvanceToWrangle(LocalPlayer me, Action<string> reply)
        {
            // Stage 3: the wrangle — biggest that fits the NCU free right now.
            if (_plan.WrangleTargets.Count == 0 || _plan.Wrangle == null) { AdvanceToRest(me, reply); return; }
            var pick = _buffs.Where(b => b.IsWrangler && !b.Team && b.Ncu <= Math.Max(1, FreeNcu(me) - 1))
                             .OrderByDescending(b => ParseFirst(b.Effect)).FirstOrDefault() ?? _plan.Wrangle;
            _wrangleIdAsked = pick.Id ?? 0;
            SendTell("cast " + pick.Tell);
            _stage = Stage.WrangleWait; _stageTime = 0; _teamRemindAt = -999;
            Say(reply, $"Wrangle ({pick.Name}, {pick.Ncu} NCU) for {string.Join(", ", _plan.WrangleTargets.Select(t => NanoName(t)))}.");
        }

        private void AdvanceToRest(LocalPlayer me, Action<string> reply)
        {
            // Stage 6 (no self-casts pending): straight to the fill.
            _stage = Stage.RestSend; _stageTime = 0; _teamRemindAt = -999;
            SendRest(me);
        }

        private void SendRest(LocalPlayer me)
        {
            // Recompute the fill NOW: NCU state changed since the plan (expansion, wrangle given back).
            var rest = BuildPlan(me, _force);
            var picks = rest.Rest;
            if (picks.Count == 0)
            {
                _tell?.Invoke("CHEWY: nothing (more) to ask for — plan is empty or everything is running.");
                Finish(); return;
            }
            string codes = string.Join(" ", picks.Select(x => x.Buff.Tell));
            SendTell("cast " + codes);
            _lastSummary = $"{picks.Count} buffs, {picks.Sum(x => x.Buff.Ncu)} NCU: " + string.Join(", ", picks.Select(x => $"{x.Buff.Name} [{x.Buff.Cat}]"));
            _ctx.Log("CHEWY: " + _lastSummary);
            _tell?.Invoke($"Asked {BufName} for {_lastSummary}.");
            foreach (var s in rest.Skipped.Take(6)) _ctx.Log($"CHEWY: skipped {s}");
            Finish();
        }

        /// <summary>Progress the staged ask. Bounded waits between stages; timeouts log and move on.</summary>
        public void Tick(LocalPlayer me, double dt, bool inCombat)
        {
            if (_stage == Stage.Idle || _stage == Stage.Done || me == null) return;
            _stageTime += dt;
            switch (_stage)
            {
                case Stage.Travel:
                    // Wait out the trip; when travel ends, we must BE at the spot, else the ask aborts
                    // (asking from mid-route wastes Chewy's NCU on a character that is leaving).
                    if (_overland.Active) { if (_stageTime > 600) { _overland.Stop("CHEWY: buff trip took too long"); } else break; }
                    _travelOurs = false;
                    if (AtSpot(me))
                    {
                        _ctx.Log("CHEWY: at the buff spot — starting the ask.");
                        _stage = Stage.Idle; _stageTime = 0; _teamRemindAt = -999;
                        ContinueAsk(me, null);
                    }
                    else
                    {
                        _ctx.Log("CHEWY: travel ended away from the buff spot — buff ask aborted.");
                        _tell?.Invoke("CHEWY: not at the buff spot, so no asking. 'buffs ask' to try the trip again.");
                        Finish();
                    }
                    break;

                case Stage.NcuWait:
                    // Landed when the running tier's gain IMPROVED over what ran when we asked (the ask may
                    // be an upgrade: +150 QuarkStor over a +20 Compressor). Team cast: without a team it can
                    // never land, so that case waits for Chewy's invite instead of the timeout.
                    {
                        var ncuDef = NcuDef();
                        bool landed = NcuRunningGain(me, ncuDef) > _ncuGainAtAsk;
                        bool needTeam = ncuDef?.Team == true && !Team.IsInTeam;
                        double cap = needTeam ? _ctx.Config.BuffTeamWaitSec : 90;
                        if (needTeam) TeamRemind(me, "the NCU buff is a team cast");
                        if (landed || _stageTime > cap)
                        {
                            if (!landed) _ctx.Log($"CHEWY: no better NCU tier after {_stageTime:0}s ({(needTeam ? "no team" : "not cast")}) — carrying on without it.");
                            AdvanceToReq(me, null);
                        }
                    }
                    break;

                case Stage.ReqWait:
                    if (_stageTime > 45) AdvanceToWrangle(me, null);   // helpers are best-effort
                    break;

                case Stage.WrangleWait:
                    bool got = me.Buffs.Find(_wrangleIdAsked, out _) ||
                               (_buffs.FirstOrDefault(b => b.Id == _wrangleIdAsked) is BuffDef w && w.Resolved &&
                                me.Buffs.Any(x => x.NanoItem != null && x.NanoItem.NanoLine == w.Line));
                    if (got || _stageTime > 90)
                    {
                        if (!got) _ctx.Log("CHEWY: no wrangle landed in 90s — skipping the self-casts this round.");
                        else { _stage = Stage.SelfCast; _stageTime = 0; _teamRemindAt = -999; _selfQueued.Clear(); break; }
                        AdvanceToRest(me, null);
                    }
                    break;

                case Stage.SelfCast:
                    if (inCombat) break;   // casts are serialized through the support queue; combat owns it
                    foreach (int id in _plan.WrangleTargets)
                    {
                        if (me.Buffs.Find(id, out _)) continue;             // landed
                        if (_selfQueued.Contains(id)) continue;            // in flight
                        if (!ItemData.Find(id, out NanoItem ni) || ni == null) continue;
                        if (!ni.MeetsUseReqs()) { _ctx.Log($"CHEWY: {ni.Name} still not castable even wrangled — skipped."); _selfQueued.Add(id); continue; }
                        if (me.IsCasting || _support.HasQueuedCasts) return;
                        _support.QueueCast(new CastRequest { OnSelf = true, NanoId = id, Label = "chewy wrangle selfcast" });
                        _selfQueued.Add(id);
                        _stageTime = 0;   // reset: each cast gets its own window
                    }
                    if (_selfQueued.SetEquals(_plan.WrangleTargets) && !_support.HasQueuedCasts && !me.IsCasting && _stageTime > 4)
                    {
                        // Drop the wrangle: its ~58 NCU back for the fill. Layout mirrors CastNano but no
                        // capture has verified it — watched below.
                        me.RemoveFriendlyNano(me.Identity, _wrangleIdAsked);
                        _ctx.Log($"CHEWY: sent nano removal for the wrangle ({_wrangleIdAsked}).");
                        _stage = Stage.UnwrangleWait; _stageTime = 0; _teamRemindAt = -999;
                    }
                    else if (_stageTime > 60) { _ctx.Log("CHEWY: self-cast stage timed out — keeping the wrangle and asking for the rest anyway."); AdvanceToRest(me, null); }
                    break;

                case Stage.UnwrangleWait:
                    if (!me.Buffs.Find(_wrangleIdAsked, out _) || _stageTime > 20)
                    {
                        if (me.Buffs.Find(_wrangleIdAsked, out _))
                        {
                            _ctx.Log("CHEWY: wrangle still there 20s after the removal — RemoveFriendlyNano layout likely wrong, capture one to fix.");
                            _tell?.Invoke("CHEWY: could not remove the wrangle myself (unverified packet) — ask Chewy to terminate it, then 'buffs ask' again.");
                        }
                        // settle a moment so the NCU readback reflects the removal before the fill is computed
                        _stage = Stage.RestSend; _stageTime = 0; _teamRemindAt = -999;
                    }
                    break;

                case Stage.RestSend:
                    if (_stageTime > 3)
                    {
                        // Team-cast picks in the fill need the team too — same invite wait as the NCU stage.
                        if (_plan != null && _plan.NeedsTeam && !Team.IsInTeam && _stageTime <= _ctx.Config.BuffTeamWaitSec)
                        {
                            TeamRemind(me, "team-cast buffs are waiting");
                            break;
                        }
                        SendRest(me);
                    }
                    break;
            }
        }

        // Remind (at most once a minute) that a team invite is the missing piece.
        private double _teamRemindAt = -999;
        private void TeamRemind(LocalPlayer me, string what)
        {
            if (_stageTime - _teamRemindAt < 60) return;
            _teamRemindAt = _stageTime;
            string msg = $"CHEWY: {what} — not in a team. {BufName}, invite me (invites from '{_ctx.Config.BufferTeamPrefix}*' are accepted).";
            _ctx.Log(msg);
            _tell?.Invoke(msg);
        }

        private void Finish() { SetStage(Stage.Idle); }

        private void SetStage(Stage s) { _stage = s; _stageTime = 0; _teamRemindAt = -999; }

        private string TellOf(string code) => _buffs.FirstOrDefault(b => string.Equals(b.Tell, code, StringComparison.OrdinalIgnoreCase))?.Tell;

        private static string NanoName(int id) => ItemData.Find(id, out NanoItem ni) && ni != null ? ni.Name : "nano " + id;

        private string BufName => _ctx.Config.BufferName;

        private void SendTell(string text)
        {
            try { Client.Chat.SendPrivateMessage(BufName, text); }
            catch (Exception ex) { _ctx.Log($"CHEWY: tell to {BufName} failed: {ex.Message}"); }
            _ctx.Log($"CHEWY: told {BufName}: {text}");
        }

        private void Say(Action<string> reply, string text)
        {
            _ctx.Log("CHEWY: " + text);
            _tell?.Invoke(text);
        }

        // ------------------------------------------------------------------ commands

        public static readonly string[] CommandWords = { "plan", "ask", "force", "stop", "status", "focus", "off", "on", "list", "clear", "nanos" };
        public static bool IsCommand(string word) => CommandWords.Contains(word?.ToLowerInvariant());

        public void Command(LocalPlayer me, string[] args, Action<string> reply)
        {
            string sub = args.Length > 0 ? args[0].ToLowerInvariant() : "";
            switch (sub)
            {
                case "plan":
                    if (!Resolve()) { reply("Item data not loaded yet."); return; }
                    var p = BuildPlan(me, false);
                    var lines = new List<string> { $"free NCU {p.FreeNcuAtPlan}; weights o{_wOff}/d{_wDef}/s{_wSus}" };
                    if (_ctx.Config.BuffTravelToSpot && !AtSpot(me))
                        lines.Add($"0. travel to the buff spot ({_ctx.Config.BuffTravelX:0},{_ctx.Config.BuffTravelZ:0} in pf {_ctx.Config.BuffTravelPlayfield}) first");
                    lines.Add(p.AskNcu
                        ? $"1. ncu (best tier for my level: +{p.NcuGainPending} more Max NCU, then {p.FreeNcuAtPlan} free)"
                        : "1. ncu: best tier for my level already running");
                    if (p.DirectCasts.Count > 0)
                        lines.Add("   self-cast now (no wrangle needed): " + string.Join(", ", p.DirectCasts.Select(NanoName)));
                    if (p.WrangleTargets.Count > 0)
                    {
                        lines.Add($"2. helpers: {(p.Reqs.Count > 0 ? string.Join(", ", p.Reqs.Select(r => r.Tell + " " + r.Name)) : "none needed")}");
                        lines.Add($"3. wrangle {p.Wrangle?.Tell} {p.Wrangle?.Name} -> self-cast {string.Join(", ", p.WrangleTargets.Select(NanoName))}");
                        if (p.Gaps.Count > 0)
                            lines.Add("   short: " + string.Join(", ", p.Gaps.GroupBy(s => s.Stat).Select(g => $"{g.Key} +{g.Max(x => x.Need)}")));
                        lines.Add("5. unwrangle, then:");
                    }
                    lines.Add($"{(p.WrangleTargets.Count > 0 ? "6" : "2")}. rest: " + (p.Rest.Count > 0
                        ? string.Join(", ", p.Rest.Select(x => $"{x.Buff.Tell} ({x.Buff.Name}, {x.Buff.Cat} {x.Buff.Magnitude:0.0}, {x.Buff.Ncu} NCU)"))
                        : "nothing"));
                    lines.Add($"spent {p.Rest.Sum(x => x.Buff.Ncu)} of {p.FreeNcuAtPlan} NCU; skipped {p.Skipped.Count}");
                    foreach (var skip in p.Skipped.Take(5)) lines.Add("  - " + skip);
                    reply(string.Join(Environment.NewLine, lines));
                    return;

                case "ask":
                    BeginAsk(me, false, reply);
                    return;

                case "force":
                    reply("Asking fresh — ignoring everything still running.");
                    BeginAsk(me, true, null);
                    return;

                case "stop":
                    if (_travelOurs && _overland.Active) _overland.Stop("buff ask stopped");
                    _travelOurs = false;
                    _stage = Stage.Idle;
                    reply("Buff ask stopped.");
                    return;

                case "status":
                    reply($"stage {_stage}; weights o{_wOff}/d{_wDef}/s{_wSus}; {_buffs.Count} buffs loaded; off: {string.Join(",", _offTells.DefaultIfEmpty("-"))}; last: {_lastSummary}");
                    return;

                case "focus":
                    if (args.Length >= 4 && int.TryParse(args[1], out int o) && int.TryParse(args[2], out int d) && int.TryParse(args[3], out int s))
                    {
                        _wOff = o; _wDef = d; _wSus = s;
                        reply($"Weights now offense {o} / defense {d} / sustain {s} (session only).");
                    }
                    else reply("buffs focus <offense> <defense> <sustain> — e.g. 'buffs focus 70 20 10'.");
                    return;

                case "off":
                case "on":
                    if (args.Length < 2) { reply($"buffs {sub} <code> — the tell code, e.g. 'buffs off leet'."); return; }
                    if (sub == "off") _offTells.Add(args[1]); else _offTells.Remove(args[1]);
                    reply($"{args[1]} {(sub == "off" ? "excluded" : "allowed")} for this session.");
                    return;

                case "clear":
                {
                    // Cancel every running nano except hostile ones: a hostile program (snare, resurrection
                    // sickness) reads as net-NEGATIVE modifiers on the wearer, a buff never does. This is
                    // also the live exercise of RemoveFriendlyNano — the click-off packet whose layout is
                    // still mirror-cast, so the log names each nano it asks the server to drop.
                    int sent = 0, kept = 0;
                    foreach (var b in me.Buffs.ToList())
                    {
                        if (IsHostile(b.NanoItem)) { kept++; continue; }
                        me.RemoveFriendlyNano(me.Identity, b.Id);
                        _ctx.Log($"CHEWY: clear -> remove {b.NanoItem?.Name ?? "nano"} ({b.Id})");
                        sent++;
                    }
                    reply(sent + kept == 0 ? "No nanos running."
                        : $"Asked the server to remove {sent} nano(s){(kept > 0 ? $", kept {kept} hostile one(s)" : "")}. Watch the log if the NCU doesn't drop.");
                    return;
                }

                case "nanos":
                {
                    // Every running nano, one per tell (the chat limiter paces them) — soonest-expiring
                    // first, so what needs re-asking is at the top.
                    var running = me.Buffs.OrderBy(b => b.Cooldown?.RemainingTime ?? double.MaxValue).ToList();
                    if (running.Count == 0) { reply("No nanos running."); return; }
                    reply($"active nanos: {running.Count}, free NCU {FreeNcu(me)} — soonest first:");
                    foreach (var b in running)
                    {
                        string name = b.NanoItem != null ? b.NanoItem.Name : "nano " + b.Id;
                        double rem = b.Cooldown?.RemainingTime ?? -1;
                        string time = rem < 0 ? "unknown time"
                            : rem >= 3600 ? $"{rem / 3600:0}h{(rem % 3600) / 60:00}m"
                            : rem >= 60 ? $"{rem / 60:0}m{rem % 60:00}s"
                            : $"{rem:0}s";
                        int ncu = b.NanoItem?.NCU ?? 0;
                        reply($"{name} — {time}{(ncu > 0 ? $", {ncu} NCU" : "")}");
                    }
                    return;
                }

                case "list":
                    if (args.Length > 1)
                    {
                        var prof = _buffs.Where(b => string.Equals(b.Profession, args[1], StringComparison.OrdinalIgnoreCase)).ToList();
                        if (prof.Count == 0) { reply($"No profession '{args[1]}' in the list."); return; }
                        reply(string.Join(Environment.NewLine, prof.Select(b => $"{b.Tell,-10} {b.Name} — {b.Effect} [{b.Ncu} NCU]")));
                    }
                    else
                        reply(string.Join(", ", _buffs.GroupBy(b => b.Profession).Select(g => $"{g.Key}: {g.Count()}")) + " — 'buffs list <profession>' for detail.");
                    return;

                default:
                    reply("buffs plan | ask | force | stop | status | focus <o> <d> <s> | off/on <code> | clear | nanos | list [prof]");
                    return;
            }
        }
    }
}