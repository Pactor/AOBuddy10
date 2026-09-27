using System;
using System.Collections.Concurrent;
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
    /// CODEDOC — the RubiKa 2019 buff step (RK2019 has no Chewy; regular Rubi-Ka keeps ChewyBuffController).
    /// Evidence: the owner's sniffs marked-20260926-202941 and marked-20260927-104345 (MP Dadbod 227558166, lvl 25):
    ///   - tell 'Codedoc' "cast &lt;code&gt;" (one code per tell — the only form captured), the list of 281 codes in
    ///     GameData/CodedocBuffs.json (from 'help' and 'buffs 1'..'buffs 9', E:\Funcom\sniffs\codedoc-buffs-20260927.tsv);
    ///   - self code 'cast ct': Codesmp (52977454) casts 220331 straight on him ~1 s later, no tell, no team;
    ///   - refusal: tell "Failed to cast '&lt;name&gt;': Your level is too low." (cast cm, 10:46:23);
    ///   - team code 'cast hgvt': Enfocode (52977322) and Fixyourcode (52977411) EACH send a TeamJoinRequest (the zone
    ///     invite arrives ~0.1 s BEFORE their tell "...Please accept my invite and wait patiently.") and the client
    ///     answered ONE CharacterAction RequestReply Target=Enfocode P2=1 (the first to tell); Fixyourcode cast 162583
    ///     on him (SetNanoDuration 1440000), then all three left the team (CharacterAction 32).
    ///
    /// THE ASK, staged like the Chewy flow (ChewyBuffs.cs 'THE ASKING is staged'), owner's order (2026-09-27):
    ///   1. NCU       — the best Fixer Max-NCU tier the receiver fits, if better than what runs (mirrors Chewy stage 1,
    ///                  ChewyBuffs.cs BuildPlan NcuRunningGain/BestNcuGain).
    ///   2. PROFESSION — his own profession's section: per nano line the BEST variant he fits (receiver gates, NCU),
    ///                  lines that unblock a wanted nano first, then by Chewy's score (weight x magnitude / NCU).
    ///                  "Your level is too low" = fall back to the next lower variant of that line.
    ///   3. HELPERS   — whatever still blocks a nano he wants (the best pet nano per pet line he knows but can't cast,
    ///                  config WrangleCastNanoIds): the cheapest set of codes whose modifiers close every skill gap
    ///                  (Chewy stage 2 'requirement helpers', generalized). Composite Teachings for MP Dadbod: MM 156 ->
    ///                  181 makes Lesser Deranged Mindreaver (156127, MM>166 & T&S>166) castable again.
    ///   4. MOVEMENT  — every character: the biggest Fixer run-speed code he fits (Chewy's reserved movement line,
    ///                  ChewyBuffs.cs 'MOVEMENT: the runspeed line is RESERVED'). Its NCU is reserved from stage 2 on.
    ///   5. NANO REGEN — a Doctor-section code raising NanoDelta, only if his free NCU (after the NCU buff) holds it.
    ///   6. FILL      — the rest of his free NCU from the other sections, Chewy's rules (one per line, receiver fit,
    ///                  weapon relevance, no tradeskill/short HoT/leet), best variant per line.
    /// Every ask is one code per tell; each code at most once per episode; landed / refused / timed out, then the
    /// next. A refusal ("Failed to cast '...': reason") is a hard stop for that code (level: until he levels);
    /// a timeout backs the code off for 30 minutes. Free NCU is read from his stats (Chewy's FreeNcu) before each
    /// ask and a code that does not fit is never requested.
    /// </summary>
    public class CodedocBuffController
    {
        private readonly BotContext _ctx;
        private readonly string _file;
        private readonly List<Def> _buffs = new List<Def>();
        private bool _loadFailed, _resolved;

        private sealed class Def
        {
            public string Code, Section, Name, Effect, IdSource;
            public bool Team;
            public int Ncu;
            public int[] Ids;
            // resolved from item data
            public int Landed;                  // the nano that runs on the receiver (0 = unresolved)
            public bool LandedSeparate;         // team pair: the buffer casts a modifier-less team caster, this lands
            public NanoItem Nano;
            public string Unresolved;
            public string Cat;
            public double Mag;
            public int Raw;                     // sum of its positive Use modifiers: the ladder order within a line
            public HashSet<int> Off;
            public bool Movement;
            public int NcuGain;                 // +Max NCU it gives (0 = not an NCU buff)
            public bool Regen;                  // raises NanoDelta
            public string LineKey;
            public Profession[] Profs;          // section's profession(s); empty = Generic
        }

        public CodedocBuffController(BotContext ctx, string pluginDir)
        {
            _ctx = ctx;
            _file = Path.Combine(pluginDir, "GameData", "CodedocBuffs.json");
            Load();
            // Tells travel on the chat server (sniff: port 7106, type 30). Queue them; the update thread reads them.
            try { Client.Chat.PrivateMessageReceived += (s, m) => { try { _tells.Enqueue((m.SenderId, m.SenderName, m.Message ?? "")); } catch { } }; }
            catch (Exception ex) { _ctx.Log("CODEDOC: could not listen for tells: " + ex.Message); }
        }

        private void Load()
        {
            try
            {
                var doc = JObject.Parse(File.ReadAllText(_file));
                foreach (var b in doc["buffs"])
                {
                    var d = new Def
                    {
                        Code = (string)b["code"],
                        Section = (string)b["section"],
                        Name = (string)b["name"],
                        Effect = (string)b["effect"] ?? "",
                        Team = b["team"] != null && (bool)b["team"],
                        Ncu = b["ncu"] != null ? (int)b["ncu"] : 0,
                        Ids = b["ids"]?.Select(t => (int)t).ToArray() ?? new int[0],
                        IdSource = (string)b["idSource"],
                    };
                    d.Profs = SectionProfessions(d.Section);
                    if (!string.IsNullOrEmpty(d.Code)) _buffs.Add(d);
                }
                _ctx.Log($"CODEDOC: loaded {_buffs.Count} codes from CodedocBuffs.json.");
            }
            catch (Exception ex)
            {
                _loadFailed = true;
                _ctx.Log($"CODEDOC: could not read {_file}: {ex.Message} — Codedoc step disabled.");
            }
        }

        // Section names as Codedoc lists them ('buffs 1'..'buffs 9' page titles + 'Agent/Engineer Buffs').
        private static Profession[] SectionProfessions(string section)
        {
            var list = new List<Profession>();
            foreach (var part in (section ?? "").Replace(" Buffs", "").Split('/'))
                if (Enum.TryParse(part.Trim(), true, out Profession p) && p != Profession.Unknown) list.Add(p);
            return list.ToArray();
        }

        // ------------------------------------------------------------------ gate

        /// <summary>RubiKa 2019 only (Client.Dimension, set at login — the same test MissionRun.NoScotty uses).</summary>
        public bool Enabled => !_loadFailed && _ctx.Config.CodedocBuffs
                               && Client.Dimension == AOSharp.Clientless.Common.Dimension.RubiKa2019;

        public int SpotPf => _ctx.Config.CodedocPlayfield;
        public Vector3 Spot => new Vector3(_ctx.Config.CodedocX, _ctx.Config.CodedocY, _ctx.Config.CodedocZ);

        private bool _armed;
        private double _armedAt;
        private string _armWhy;
        private double _clock;

        /// <summary>Arm the step (mission run start, a death): the run asks Wants() when it is back at the terminal.</summary>
        public void Arm(string why)
        {
            if (!Enabled) return;
            _armed = true; _armedAt = _clock; _armWhy = why;
            _ctx.Log($"CODEDOC: armed ({why}) — buffs are checked when I'm back in {Zoning.Name(SpotPf)}.");
        }

        public void Disarm(string why)
        {
            if (_armed) _ctx.Log($"CODEDOC: disarmed ({why}).");
            _armed = false;
        }

        /// <summary>Own self-buffs wait while this is true (BotStatus.CodedocHold -> SupportController.KeepBuffs):
        /// armed and in the Codedoc zone (bounded to 10 minutes), or asking.</summary>
        public bool Hold => Enabled && (Busy || (_armed && (int)Playfield.ModelId == SpotPf && _clock - _armedAt < 600));

        public bool Busy => _stage != Stage.Idle;

        /// <summary>Armed, in the Codedoc zone, and something is missing that Codedoc can give. An empty plan
        /// disarms (logged) so the run never walks over for nothing.</summary>
        public bool Wants(LocalPlayer me)
        {
            if (!Enabled || !_armed || Busy || me == null) return false;
            if ((int)Playfield.ModelId != SpotPf) return false;
            if (!Resolve()) return false;   // item data not in yet: ask again next tick
            var notes = new List<string>();
            var preview = Preview(me, notes);
            if (preview.Count == 0)
            {
                Disarm("nothing Codedoc has is missing or fits");
                foreach (var n in notes.Take(8)) _ctx.Log("CODEDOC:   " + n);
                return false;
            }
            _ctx.Log($"CODEDOC: missing ({_armWhy}): {string.Join(", ", preview.Select(p => $"{p.stage}:{p.def.Code}"))}.");
            return true;
        }

        // ------------------------------------------------------------------ resolve

        private static bool HasUseMods(NanoItem ni)
            => ni?.Modifiers != null && ni.Modifiers.TryGetValue(SpellListType.Use, out var u) && u != null && u.Count > 0;

        private static Dictionary<Stat, int> UseMods(NanoItem ni)
            => ni?.Modifiers != null && ni.Modifiers.TryGetValue(SpellListType.Use, out var u) && u != null ? u : new Dictionary<Stat, int>();

        private static bool ProfessionGated(NanoItem ni, Profession[] profs)
        {
            if (ni?.Criteria == null || profs.Length == 0) return false;
            if (!ni.Criteria.TryGetValue(ItemActionInfo.UseCriteria, out List<RequirementCriterion> cr) || cr == null) return false;
            return cr.Any(c => c.Operator == UseCriteriaOperator.EqualTo
                               && (c.Param1 == (int)Stat.Profession || c.Param1 == (int)Stat.VisualProfession)
                               && profs.Any(p => (int)p == c.Param2));
        }

        /// <summary>Pick the nano each code puts on the receiver. One id, or the sniffed one: that. Several ids
        /// sharing the name: a team code whose pair is a modifier-less team caster + one nano with modifiers ->
        /// the one with modifiers lands (the Chewy NCU/umbral landIds structure, ChewyBuffs.cs Resolve); otherwise
        /// the one whose cast gate names the section's profession (ChewysBuffs.json _about rule); else left out.</summary>
        private bool Resolve()
        {
            if (_resolved) return true;
            int ok = 0;
            foreach (var d in _buffs)
            {
                var found = new List<NanoItem>();
                foreach (int id in d.Ids) if (ItemData.Find(id, out NanoItem ni) && ni != null) found.Add(ni);
                NanoItem pick = null;
                if (found.Count == 1) pick = found[0];
                else if (found.Count > 1)
                {
                    var modded = found.Where(HasUseMods).ToList();
                    if (d.Team && modded.Count == 1) { pick = modded[0]; d.LandedSeparate = true; }
                    else
                    {
                        var gated = found.Where(n => ProfessionGated(n, d.Profs)).ToList();
                        if (gated.Count == 1) pick = gated[0];
                    }
                }
                if (pick == null)
                {
                    d.Unresolved = d.Ids.Length == 0 ? "name not in the client data" : found.Count == 0 ? "ids not in item data" : $"{found.Count} nanos share the name";
                    continue;
                }
                d.Nano = pick; d.Landed = pick.Id; ok++;
                var cls = ChewyBuffController.Classify(d.Effect, d.Code, pick);
                d.Cat = cls.Cat; d.Mag = cls.Magnitude; d.Off = cls.OffenseStatIds; d.Movement = cls.Movement;
                var use = UseMods(pick);
                d.Raw = use.Values.Where(v => v > 0).Sum();
                d.NcuGain = use.TryGetValue(Stat.MaxNCU, out int g) && g > 0 ? g : 0;
                d.Regen = use.TryGetValue(Stat.NanoDelta, out int nd) && nd > 0;
                d.LineKey = pick.NanoLine == NanoLine.NOSTACKING ? "id" + pick.Id : "L" + (int)pick.NanoLine;
            }
            if (ok == 0) return false;   // item data not loaded yet (ItemData.Find finds nothing): try again later
            _resolved = true;
            _ctx.Log($"CODEDOC: resolved {ok} of {_buffs.Count} codes to nano data; left out: "
                     + string.Join(", ", _buffs.Where(b => b.Nano == null).Select(b => $"{b.Code} ({b.Unresolved})")));
            return true;
        }

        // ------------------------------------------------------------------ receiver state

        // Codes refused ("Failed to cast ...") — level refusals keep until he levels, others for the session;
        // timed-out codes back off for 30 minutes; codes asked in this episode are not asked again.
        private readonly Dictionary<string, int> _levelBlocked = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _refused = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, double> _backoffUntil = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _askedThisEpisode = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private const double BackoffSec = 1800;

        private static int Level(LocalPlayer me) => me != null && me.TryGetStat(Stat.Level, out int l) ? l : 0;

        private string BlockedWhy(LocalPlayer me, Def d)
        {
            if (_levelBlocked.TryGetValue(d.Code, out int lvl) && lvl == Level(me)) return $"Codedoc said level too low (at {lvl})";
            if (_refused.TryGetValue(d.Code, out string why)) return "Codedoc refused: " + why;
            if (_backoffUntil.TryGetValue(d.Code, out double t) && _clock < t) return $"no answer last time (back off {(t - _clock) / 60:0} min)";
            if (_askedThisEpisode.Contains(d.Code)) return "already asked this time";
            return null;
        }

        /// <summary>The running buff that covers this code's line: same id, or same line with equal/greater
        /// StackingOrder (SupportController.FindActiveBuff's rule, SupportController.cs), with more than
        /// BuffSkipRemainingPct of its time left (Chewy's RunningHealthy rule). Null = not carried.</summary>
        private Buff Carried(LocalPlayer me, Def d)
        {
            if (me?.Buffs == null || d.Nano == null) return null;
            foreach (var b in me.Buffs)
            {
                bool same = b.Id == d.Landed
                            || (d.Nano.NanoLine != NanoLine.NOSTACKING && b.NanoItem != null
                                && b.NanoItem.NanoLine == d.Nano.NanoLine && b.NanoItem.StackingOrder >= d.Nano.StackingOrder);
                if (!same) continue;
                double rem = b.Cooldown?.RemainingTime ?? -1;
                if (rem < 0) return b;                                   // timer unknown = up (NeedsBuff's rule)
                double total = b.NanoItem?.TotalTime ?? 0;
                if (total > 0 && rem / total * 100 <= _ctx.Config.BuffSkipRemainingPct) return null;
                return b;
            }
            return null;
        }

        /// <summary>The running same-line buff this code would replace (lower StackingOrder), or null.</summary>
        private static Buff Replaces(LocalPlayer me, Def d)
        {
            if (me?.Buffs == null || d.Nano == null || d.Nano.NanoLine == NanoLine.NOSTACKING) return null;
            return me.Buffs.FirstOrDefault(b => b.NanoItem != null && b.NanoItem.NanoLine == d.Nano.NanoLine && b.Id != d.Landed);
        }

        /// <summary>NCU the ask adds: its NCU (the tsv column, the nano's own if the column is empty) minus what
        /// the replaced same-line buff frees.</summary>
        private static int NcuCost(LocalPlayer me, Def d)
        {
            int ncu = d.Ncu > 0 ? d.Ncu : (d.Nano?.NCU ?? 0);
            var r = Replaces(me, d);
            return Math.Max(0, ncu - (r?.NanoItem?.NCU ?? 0));
        }

        /// <summary>Why this code can't go on him now (receiver gates, blocks, team state), or null.</summary>
        private string Unfit(LocalPlayer me, Def d)
        {
            if (d.Nano == null) return d.Unresolved ?? "unresolved";
            string b = BlockedWhy(me, d);
            if (b != null) return b;
            // Receiver fit — Chewy's check: a separate landed nano states its gates plainly, a cast nano's receiver
            // gates are its OnTarget criteria (ChewyBuffController.ReceiverBlockReason).
            string r = ChewyBuffController.ReceiverBlockReason(me, new[] { d.Landed }, d.LandedSeparate);
            if (r != null) return r;
            // A team code needs Codedoc's own team; already in one, the invite flow is not captured.
            if (d.Team && Team.IsInTeam) return "team code, and I'm already in a team (not captured how that goes)";
            return null;
        }

        // ------------------------------------------------------------------ what he wants cast (gaps)

        private static readonly NanoLine[] PetLines = { NanoLine.AttackPets, NanoLine.HealPets, NanoLine.SupportPets };

        /// <summary>Nanos he knows and wants but can't cast yet, with their skill/ability gaps: per pet line the
        /// nanos ranked above his best castable one (PetController.AutoSummons' rank: StackingOrder & 0xFFFFF),
        /// best first, and config WrangleCastNanoIds (Chewy's self-cast targets). ShortfallsOf = Chewy's gap reader.</summary>
        private List<(int Id, string Name, List<(Stat Stat, int Need)> Gaps)> WantedBlocked(LocalPlayer me)
        {
            var res = new List<(int, string, List<(Stat, int)>)>();
            int[] spells = me?.SpellList;
            if (spells != null)
                foreach (var line in PetLines)
                {
                    var known = new List<NanoItem>();
                    foreach (int id in spells) if (ItemData.Find(id, out NanoItem ni) && ni != null && ni.NanoLine == line) known.Add(ni);
                    if (known.Count == 0) continue;
                    long bestCast = -1;
                    foreach (var ni in known) { bool c; try { c = ni.MeetsUseReqs(me, false, true); } catch { c = false; } if (c) bestCast = Math.Max(bestCast, ni.StackingOrder & 0xFFFFF); }
                    foreach (var ni in known.Where(n => (n.StackingOrder & 0xFFFFF) > bestCast).OrderByDescending(n => n.StackingOrder & 0xFFFFF))
                    {
                        var gaps = ChewyBuffController.ShortfallsOf(me, ni.Id);
                        if (gaps.Count > 0) res.Add((ni.Id, ni.Name, gaps));
                    }
                }
            foreach (int id in _ctx.Config.WrangleCastNanoIds ?? new List<int>())
            {
                if (me.Buffs.Find(id, out _)) continue;
                var gaps = ChewyBuffController.ShortfallsOf(me, id);
                if (gaps.Count > 0 && ItemData.Find(id, out NanoItem wn)) res.Add((id, wn?.Name ?? "nano " + id, gaps));
            }
            return res;
        }

        /// <summary>What this code adds to a stat over what it would replace (same line swaps, it doesn't stack).</summary>
        private static int Gain(LocalPlayer me, Def d, Stat s)
        {
            int v = UseMods(d.Nano).TryGetValue(s, out int x) ? x : 0;
            var r = Replaces(me, d);
            int rv = r?.NanoItem != null && UseMods(r.NanoItem).TryGetValue(s, out int y) ? y : 0;
            return v - rv;
        }

        /// <summary>The cheapest codes (greedy: gap closed per NCU) that together close every gap of one wanted
        /// nano; null when they can't all be closed (then asking would be wasted).</summary>
        private List<Def> CloseGaps(LocalPlayer me, List<(Stat Stat, int Need)> gaps, IEnumerable<Def> pool)
        {
            var need = gaps.GroupBy(g => g.Stat).ToDictionary(g => g.Key, g => g.Max(x => x.Need));
            var picks = new List<Def>();
            var cand = pool.ToList();
            while (need.Values.Any(v => v > 0))
            {
                Def best = null; double bestScore = 0;
                foreach (var d in cand)
                {
                    if (picks.Any(p => p.LineKey == d.LineKey)) continue;
                    double closed = need.Where(kv => kv.Value > 0).Sum(kv => Math.Min(kv.Value, Math.Max(0, Gain(me, d, kv.Key))));
                    if (closed <= 0) continue;
                    double score = closed / Math.Max(1, NcuCost(me, d));
                    if (score > bestScore) { bestScore = score; best = d; }
                }
                if (best == null) return null;
                picks.Add(best);
                foreach (var k in need.Keys.ToList()) need[k] -= Math.Max(0, Gain(me, best, k));
            }
            return picks;
        }

        // ------------------------------------------------------------------ the stages

        private enum Stage { Idle, Ncu, Profession, Helpers, Movement, Regen, Fill }

        // Chewy's score: category weight (ChewyBuffController.WeightOf, same config weights) x magnitude / NCU.
        private double Score(Def d)
        {
            var c = _ctx.Config;
            double w;
            switch (d.Cat)
            {
                case "offense": w = Math.Max(1, c.BuffWeightOffense); break;
                case "defense": w = Math.Max(1, c.BuffWeightDefense); break;
                case "sustain": w = Math.Max(1, c.BuffWeightSustain); break;
                case "tradeskill": w = c.BuffWeightTradeskill; break;
                default: w = (c.BuffWeightOffense + c.BuffWeightDefense + c.BuffWeightSustain) / 3.0; break;
            }
            return w * d.Mag / Math.Max(1, d.Ncu);
        }

        private HashSet<int> _weaponStats; private bool _unknownWeapon;

        /// <summary>Chewy's fill filters (ChewyBuffs.cs BuildPlan): excluded (short HoT, leet), tradeskill off by
        /// default, offense that only raises weapon skills he doesn't wield.</summary>
        private string FillRuleOut(Def d)
        {
            if (d.Cat == "excluded") return "short HoT / leet — never asked (Chewy rule)";
            if (d.Cat == "tradeskill" && _ctx.Config.BuffWeightTradeskill <= 0) return "tradeskill (BuffWeightTradeskill 0)";
            if (d.NcuGain > 0) return "NCU line (stage 1)";
            return ChewyBuffController.WeaponBlockReasonFor(_weaponStats, _unknownWeapon, d.Cat, d.Off);
        }

        /// <summary>Per nano line, the best variant (Chewy magnitude, then StackingOrder) that fits him and the
        /// budget; a line he already carries is skipped. Returns the chosen codes in line-score order.</summary>
        private List<Def> BestPerLine(LocalPlayer me, IEnumerable<Def> pool, ref int budget, List<string> notes, Func<Def, bool> first = null)
        {
            var chosen = new List<Def>();
            var lines = pool.Where(d => d.Nano != null).GroupBy(d => d.LineKey).ToList();
            var order = new List<(Def top, List<Def> ladder)>();
            foreach (var g in lines)
            {
                // Best first. A variant he carries (it, or better, runs) covers everything below it: stop there.
                var ladder = new List<Def>();
                foreach (var d in Ladder(g))
                {
                    if (Carried(me, d) != null) { if (ladder.Count == 0) notes.Add($"{d.Code} ({d.Name}): carried"); break; }
                    string why = Unfit(me, d) ?? FillRuleOut(d);
                    if (why != null) { notes.Add($"{d.Code} ({d.Name}): {why}"); continue; }
                    ladder.Add(d);
                }
                if (ladder.Count > 0) order.Add((ladder[0], ladder));
            }
            foreach (var (top, ladder) in order.OrderByDescending(o => first != null && o.ladder.Any(first) ? 1 : 0).ThenByDescending(o => Score(o.top)))
            {
                int b = budget;
                var fit = ladder.FirstOrDefault(d => NcuCost(me, d) <= b);
                if (fit == null) { notes.Add($"{top.Code} ({top.Name}) line: no variant fits {budget} free NCU"); continue; }
                chosen.Add(fit);
                budget -= NcuCost(me, fit);
            }
            return chosen;
        }

        /// <summary>A line's variants best first: bigger effect (raw modifier sum — Chewy's magnitude caps at 3 and
        /// would tie Composite Infuse +90 with Composite Mochams +140), then StackingOrder, then longer duration
        /// (Composite Mochams 8 h over 4 h/2 h/1 h).</summary>
        private static IEnumerable<Def> Ladder(IEnumerable<Def> line)
            => line.Where(d => d.Nano != null).OrderByDescending(x => x.Raw).ThenByDescending(x => x.Nano.StackingOrder).ThenByDescending(x => x.Nano.TotalTime);

        private Profession MyProfession(LocalPlayer me)
            => me != null && me.TryGetStat(Stat.Profession, out int p) ? (Profession)p : Profession.Unknown;

        /// <summary>The codes of one stage against live state (budget = his free NCU now, Chewy's FreeNcu).</summary>
        private List<Def> StagePicks(LocalPlayer me, Stage st, List<string> notes, ref int budget, int reserve)
        {
            var picks = new List<Def>();
            var all = _buffs.Where(d => d.Nano != null).ToList();
            Profession prof = MyProfession(me);
            switch (st)
            {
                case Stage.Ncu:
                {
                    // The best tier he fits, if better than the running one (Chewy stage 1).
                    int running = 0;
                    foreach (var d in all.Where(x => x.NcuGain > 0))
                        if (me.Buffs.Any(b => b.Id == d.Landed)) running = Math.Max(running, d.NcuGain);
                    var best = all.Where(d => d.NcuGain > running && Unfit(me, d) == null)
                                  .OrderByDescending(d => d.NcuGain).FirstOrDefault();
                    if (best != null && NcuCost(me, best) <= budget) { picks.Add(best); budget -= NcuCost(me, best); }
                    else notes.Add(running > 0 ? $"NCU: +{running} tier running, nothing better fits" : "NCU: no tier fits me");
                    break;
                }
                case Stage.Profession:
                {
                    var wanted = WantedBlocked(me);
                    int b2 = budget - reserve;
                    picks = BestPerLine(me, all.Where(d => d.Profs.Contains(prof) && !d.Movement && !d.Regen), ref b2, notes,
                                        d => wanted.Any(w => w.Gaps.Any(g => Gain(me, d, g.Stat) > 0)));
                    budget = b2 + reserve;
                    break;
                }
                case Stage.Helpers:
                {
                    foreach (var w in WantedBlocked(me))
                    {
                        var set = CloseGaps(me, w.Gaps, all.Where(d => Unfit(me, d) == null && Carried(me, d) == null));
                        if (set == null) { notes.Add($"{w.Name} [{w.Id}]: gaps {string.Join(", ", w.Gaps.Select(g => $"{g.Stat} +{g.Need}"))} — Codedoc has nothing I fit that closes them all"); continue; }
                        int cost = set.Where(d => !picks.Contains(d)).Sum(d => NcuCost(me, d));
                        if (cost > budget - reserve) { notes.Add($"{w.Name}: closing needs {cost} NCU, {budget - reserve} free"); continue; }
                        foreach (var d in set.Where(d => !picks.Contains(d))) { picks.Add(d); budget -= NcuCost(me, d); }
                        _ctx.Log($"CODEDOC: {w.Name} [{w.Id}] needs {string.Join(", ", w.Gaps.Select(g => $"{g.Stat} +{g.Need}"))} -> {string.Join(", ", set.Select(d => d.Code))}.");
                        break;   // one wanted nano at a time, best first: the next plan sees the new stats
                    }
                    break;
                }
                case Stage.Movement:
                {
                    // Fixer run speed, biggest he fits (Chewy's movement reservation: magnitude, not score).
                    var fixer = all.Where(d => d.Movement && d.Profs.Contains(Profession.Fixer)).ToList();
                    if (fixer.Any(d => Carried(me, d) != null)) { notes.Add("run speed: a Fixer run-speed buff is running"); break; }
                    int b = budget;
                    var mv = fixer.Where(d => Unfit(me, d) == null && NcuCost(me, d) <= b)
                                  .OrderByDescending(d => d.Mag).ThenByDescending(d => d.Raw).FirstOrDefault();
                    if (mv != null) { picks.Add(mv); budget -= NcuCost(me, mv); }
                    else notes.Add($"run speed: no Fixer code fits me in {budget} free NCU");
                    break;
                }
                case Stage.Regen:
                {
                    var regen = all.Where(d => d.Regen && d.Profs.Contains(Profession.Doctor)).ToList();
                    if (regen.Count == 0) { notes.Add("nano regen: no Doctor code in the Codedoc list raises NanoDelta (item data)"); break; }
                    int b = budget - reserve;
                    var r = BestPerLine(me, regen, ref b, notes);
                    budget = b + reserve;
                    picks.AddRange(r);
                    break;
                }
                case Stage.Fill:
                {
                    int b = budget - reserve;
                    picks = BestPerLine(me, all.Where(d => !d.Profs.Contains(prof) && !d.Movement && !d.Regen), ref b, notes);
                    budget = b + reserve;
                    break;
                }
            }
            return picks;
        }

        private static readonly Stage[] Order = { Stage.Ncu, Stage.Profession, Stage.Helpers, Stage.Movement, Stage.Regen, Stage.Fill };

        /// <summary>A static look at every stage (NCU gain projected in, movement reserved), for Wants and 'codedoc plan'.</summary>
        private List<(Stage stage, Def def)> Preview(LocalPlayer me, List<string> notes)
        {
            var res = new List<(Stage, Def)>();
            PrepareWeapons(me);
            int budget = ChewyBuffController.FreeNcu(me);
            notes.Add($"free NCU {budget} (level {Level(me)}, {MyProfession(me)})");
            var seen = new HashSet<string>();
            int reserve = MovementReserve(me);
            foreach (var st in Order)
            {
                // Helpers (a pet nano he can't cast yet, e.g. the better heal pet) come before run speed (owner,
                // 2026-09-27: "better heal" - 13:37:11 the run-speed reserve left Calling of Salvinous 1 NCU short).
                int r = st == Stage.Movement || st == Stage.Ncu || st == Stage.Helpers ? 0 : reserve;
                foreach (var d in StagePicks(me, st, notes, ref budget, r))
                {
                    if (!seen.Add(d.LineKey)) continue;
                    res.Add((st, d));
                    if (st == Stage.Ncu) budget += d.NcuGain;
                }
                if (st == Stage.Movement) reserve = 0;
            }
            return res;
        }

        private int MovementReserve(LocalPlayer me)
        {
            var n = new List<string>();
            int b = ChewyBuffController.FreeNcu(me);   // the run-speed code that fits his free NCU now
            var mv = StagePicks(me, Stage.Movement, n, ref b, 0);
            return mv.Sum(d => NcuCost(me, d));
        }

        private void PrepareWeapons(LocalPlayer me)
        {
            _weaponStats = ChewyBuffController.EquippedWeaponStats(me, out _unknownWeapon, out _);
        }

        // ------------------------------------------------------------------ asking

        private Stage _stage = Stage.Idle;
        private int _stageIdx;
        private readonly Queue<Def> _queue = new Queue<Def>();
        private Def _current;
        private double _askedAt, _settleUntil, _stepStartedAt;
        private int _landedCount, _failedCount;
        private string _lastSummary = "never run";
        private readonly ConcurrentQueue<(uint Id, string Name, string Text)> _tells = new ConcurrentQueue<(uint, string, string)>();

        // Team invite (sniff: invite first, then the inviter's "accept my invite" tell; ONE RequestReply, to the
        // first inviter who told).
        private readonly object _inviteLock = new object();
        private readonly Dictionary<uint, TeamRequestEventArgs> _pendingInvites = new Dictionary<uint, TeamRequestEventArgs>();
        private readonly List<uint> _toldOrder = new List<uint>();
        private bool _teamWindow, _inviteAccepted;

        private const double SelfTimeout = 30, TeamTimeout = 60, SettleSec = 2;

        public string Summary => _lastSummary;
        public string StageName => _stage.ToString();

        /// <summary>Start the ask (the run calls this standing at the Codedoc spot). False = nothing to ask.</summary>
        public bool Begin(LocalPlayer me, string why)
        {
            if (!Enabled || me == null || Busy) return false;
            if (!Resolve()) { _ctx.Log("CODEDOC: item data not loaded — not asking yet."); return false; }
            _askedThisEpisode.Clear();
            _landedCount = 0; _failedCount = 0;
            _stageIdx = -1; _stepStartedAt = _clock;
            _ctx.Log($"CODEDOC: at the Codedoc spot ({why}); asking {_ctx.Config.CodedocName}, stage by stage (NCU, profession, helpers, run speed, nano regen, fill).");
            NextStage(me);
            return true;
        }

        public void Abort(string why)
        {
            if (Busy) _ctx.Log($"CODEDOC: step stopped ({why}) after {_landedCount} landed, {_failedCount} not.");
            CloseTeamWindow();
            _stage = Stage.Idle; _current = null; _queue.Clear();
            _askedThisEpisode.Clear();
            Disarm(why);
        }

        private void NextStage(LocalPlayer me)
        {
            _queue.Clear();
            while (++_stageIdx < Order.Length)
            {
                var st = Order[_stageIdx];
                var notes = new List<string>();
                PrepareWeapons(me);
                int budget = ChewyBuffController.FreeNcu(me);
                int reserve = st == Stage.Profession || st == Stage.Regen || st == Stage.Fill ? MovementReserve(me) : 0;   // not Helpers: better pets first (owner)
                var picks = StagePicks(me, st, notes, ref budget, reserve);
                _ctx.Log($"CODEDOC: stage {st}: {(picks.Count > 0 ? string.Join(", ", picks.Select(p => $"{p.Code} ({p.Name}, {NcuCost(me, p)} NCU)")) : "nothing")} — free NCU {ChewyBuffController.FreeNcu(me)}{(reserve > 0 ? $", {reserve} kept for run speed" : "")}.");
                foreach (var n in notes.Take(10)) _ctx.Log("CODEDOC:   " + n);
                if (picks.Count == 0) continue;
                _stage = st;
                foreach (var p in picks) _queue.Enqueue(p);
                return;
            }
            Finish();
        }

        private void Finish()
        {
            _lastSummary = $"{_landedCount} landed, {_failedCount} not ({_clock - _stepStartedAt:0}s)";
            _ctx.Log($"CODEDOC: done — {_lastSummary}. Own self-buffs next (AUTO-BUFF skips lines these cover).");
            CloseTeamWindow();
            _stage = Stage.Idle; _current = null;
            _askedThisEpisode.Clear();
            Disarm("step done");
        }

        private bool Landed(LocalPlayer me, Def d) => me.Buffs.Any(b => b.Id == d.Landed
            || (d.Nano.NanoLine != NanoLine.NOSTACKING && b.NanoItem != null && b.NanoItem.NanoLine == d.Nano.NanoLine
                && b.NanoItem.StackingOrder >= d.Nano.StackingOrder));

        /// <summary>Every frame (Main). Reads tells, answers the invite, advances the ask.</summary>
        public void Tick(LocalPlayer me, double dt)
        {
            _clock += dt;
            while (_tells.TryDequeue(out var t)) OnTell(me, t.Id, t.Name, t.Text);
            if (!Busy || me == null) return;
            AcceptInviteIfTold();
            if (_clock < _settleUntil) return;

            if (_current == null)
            {
                if (_queue.Count == 0) { NextStage(me); return; }
                var d = _queue.Dequeue();
                // Re-check against live state: an earlier ask may have changed NCU, lines, or blocked it.
                string why = Unfit(me, d);
                if (why == null && Carried(me, d) != null) why = "already carried";
                int free = ChewyBuffController.FreeNcu(me);
                if (why == null && NcuCost(me, d) > free) why = $"needs {NcuCost(me, d)} NCU, {free} free — not requested";
                if (why != null) { _ctx.Log($"CODEDOC: skip {d.Code} ({d.Name}): {why}."); return; }
                Ask(d);
                return;
            }

            double limit = _current.Team ? TeamTimeout : SelfTimeout;
            if (Landed(me, _current))
            {
                _ctx.Log($"CODEDOC: {_current.Code} landed — {_current.Name} [{_current.Landed}] after {_clock - _askedAt:0.0}s; free NCU now {ChewyBuffController.FreeNcu(me)}.");
                _landedCount++;
                EndCurrent();
                _settleUntil = _clock + SettleSec;   // stats (Max NCU, skills) settle before the next plan
                return;
            }
            if (_clock - _askedAt > limit)
            {
                _backoffUntil[_current.Code] = _clock + BackoffSec;
                _ctx.Log($"CODEDOC: {_current.Code} ({_current.Name}) did not land in {limit:0}s{(_current.Team ? (_inviteAccepted ? " (invite accepted)" : " (no invite I could match)") : "")} — backing off it for {BackoffSec / 60:0} min, carrying on.");
                _failedCount++;
                EndCurrent();
            }
        }

        private void Ask(Def d)
        {
            _current = d; _askedAt = _clock;
            _askedThisEpisode.Add(d.Code);
            if (d.Team) OpenTeamWindow();
            string text = "cast " + d.Code;
            try { Client.Chat.SendPrivateMessage(_ctx.Config.CodedocName, text); }
            catch (Exception ex) { _ctx.Log($"CODEDOC: tell to {_ctx.Config.CodedocName} failed: {ex.Message}"); }
            _ctx.Log($"CODEDOC: told {_ctx.Config.CodedocName}: {text} ({d.Name}{(d.Team ? ", team code — waiting for the invite" : "")}; stage {_stage}).");
        }

        private void EndCurrent()
        {
            if (_current?.Team == true) CloseTeamWindow();
            _current = null;
        }

        private static readonly Regex FailedRx = new Regex(@"Failed to cast '(?<name>.+?)':\s*(?<why>.+)$", RegexOptions.Compiled);

        private void OnTell(LocalPlayer me, uint senderId, string senderName, string raw)
        {
            if (!Busy) return;
            string text = Regex.Replace(raw ?? "", "<[^>]+>", "").Trim();
            var m = FailedRx.Match(text);
            if (m.Success)
            {
                string name = m.Groups["name"].Value.Trim(), why = m.Groups["why"].Value.Trim();
                var d = _current != null && string.Equals(_current.Name, name, StringComparison.OrdinalIgnoreCase) ? _current
                      : _buffs.FirstOrDefault(x => _askedThisEpisode.Contains(x.Code) && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (d == null) { _ctx.Log($"CODEDOC: refusal for '{name}' I didn't ask for ({senderName}/{senderId}): {why}"); return; }
                _failedCount++;
                if (why.IndexOf("level is too low", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _levelBlocked[d.Code] = Level(me);
                    _ctx.Log($"CODEDOC: {d.Code} refused ({senderName}/{senderId}): \"{why}\" — hard stop for it until I level; trying the next lower one of its line.");
                    // Fall back down the same line (owner: 'Your level is too low' = the next lower one).
                    if (d == _current && d.Nano != null)
                    {
                        var ladder = Ladder(_buffs.Where(x => x.LineKey == d.LineKey)).ToList();
                        int at = ladder.IndexOf(d);
                        var lower = ladder.Skip(at + 1).FirstOrDefault(x => Unfit(me, x) == null && Carried(me, x) == null
                                                                           && NcuCost(me, x) <= ChewyBuffController.FreeNcu(me));
                        if (lower != null)
                        {
                            var rest = _queue.ToList(); _queue.Clear(); _queue.Enqueue(lower); foreach (var r in rest) _queue.Enqueue(r);
                            _ctx.Log($"CODEDOC:   next down the line: {lower.Code} ({lower.Name}).");
                        }
                        else _ctx.Log("CODEDOC:   nothing lower in that line I fit.");
                    }
                }
                else
                {
                    // Any other refusal (the NCU-full text is NOT in the captures): a hard stop for that code, logged verbatim.
                    _refused[d.Code] = why;
                    _ctx.Log($"CODEDOC: {d.Code} refused ({senderName}/{senderId}): \"{why}\" — hard stop for it this session.");
                }
                if (d == _current) EndCurrent();
                return;
            }
            if (text.IndexOf("accept my invite", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                lock (_inviteLock) { if (_teamWindow && !_toldOrder.Contains(senderId)) _toldOrder.Add(senderId); }
                _ctx.Log($"CODEDOC: {senderName}/{senderId}: \"{text}\"");
                return;
            }
            if (text.Length > 0) _ctx.Log($"CODEDOC: tell from {senderName}/{senderId} while asking: \"{(text.Length > 160 ? text.Substring(0, 160) + "..." : text)}\"");
        }

        private void OpenTeamWindow()
        {
            lock (_inviteLock) { _teamWindow = true; _inviteAccepted = false; _pendingInvites.Clear(); _toldOrder.Clear(); }
        }

        private void CloseTeamWindow()
        {
            lock (_inviteLock) { _teamWindow = false; _pendingInvites.Clear(); _toldOrder.Clear(); }
        }

        /// <summary>Main's TeamRequest handler asks this first. While a team code is out, every invite (except the
        /// owner's) is Codedoc's: held until its sender tells "accept my invite", then exactly ONE is accepted —
        /// the first who told (sniff: RequestReply Target=Enfocode only). True = handled, Main must not accept.</summary>
        public bool OnTeamInvite(TeamRequestEventArgs e)
        {
            lock (_inviteLock)
            {
                if (!_teamWindow) return false;
                string who = DynelManager.Players.FirstOrDefault(p => p.Identity == e.Requester)?.Name
                             ?? (Client.Chat.IdToNameMap.TryGetValue((uint)e.Requester.Instance, out string n) ? n : null);
                if (!string.IsNullOrEmpty(who) && string.Equals(who, _ctx.Config.Owner, StringComparison.OrdinalIgnoreCase)) return false;
                uint id = (uint)e.Requester.Instance;
                if (_inviteAccepted) { _ctx.Log($"CODEDOC: second invite from {who ?? "?"}/{id} left unanswered (one accept, as the sniff)."); return true; }
                _pendingInvites[id] = e;
                _ctx.Log($"CODEDOC: team invite from {who ?? "?"}/{id} — held until that toon tells 'accept my invite'.");
                return true;
            }
        }

        private void AcceptInviteIfTold()
        {
            TeamRequestEventArgs accept = null; uint acceptId = 0;
            lock (_inviteLock)
            {
                if (!_teamWindow || _inviteAccepted) return;
                foreach (uint id in _toldOrder)
                    if (_pendingInvites.TryGetValue(id, out var e)) { accept = e; acceptId = id; break; }
                if (accept != null) _inviteAccepted = true;
            }
            if (accept == null) return;
            try { accept.Accept(); _ctx.Log($"CODEDOC: accepted the team invite from {acceptId} (RequestReply, the one the sniff sends)."); }
            catch (Exception ex) { _ctx.Log("CODEDOC: accepting the invite failed: " + ex.Message); }
        }

        // ------------------------------------------------------------------ commands

        public void Command(LocalPlayer me, string[] args, Action<string> reply)
        {
            string sub = args.Length > 0 ? args[0].ToLowerInvariant() : "";
            if (!Enabled && sub != "status")
            {
                reply(Client.Dimension != AOSharp.Clientless.Common.Dimension.RubiKa2019
                    ? "Codedoc is RubiKa 2019's buff bot; on this dimension the Chewy flow ('buffs ...') is used."
                    : "Codedoc step is off (config CodedocBuffs) or its list failed to load.");
                return;
            }
            switch (sub)
            {
                case "plan":
                {
                    if (me == null || !Resolve()) { reply("Item data not loaded yet."); return; }
                    var notes = new List<string>();
                    var p = Preview(me, notes);
                    reply(p.Count == 0 ? "Nothing to ask Codedoc for." : "Would ask: " + string.Join(", ", p.Select(x => $"{x.stage}:{x.def.Code} ({x.def.Name}, {NcuCost(me, x.def)} NCU)")));
                    foreach (var n in notes.Take(8)) reply("  " + n);
                    return;
                }
                case "ask":
                {
                    if (me == null) return;
                    if ((int)Playfield.ModelId != SpotPf || Movement.Flat(me.Transform.Position, Spot) > 10f)
                    { reply($"I ask from the Codedoc spot ({Spot.X:0},{Spot.Z:0} in {Zoning.Name(SpotPf)}); the mission run walks there itself."); return; }
                    reply(Begin(me, "owner asked") ? "Asking Codedoc — watch the log (CODEDOC:)." : "Not asking (busy, or item data not in).");
                    return;
                }
                case "stop":
                    Abort("owner said stop");
                    reply("Codedoc step stopped.");
                    return;
                default:
                    reply($"codedoc: {(Enabled ? "on" : "off")}, {(_armed ? "armed" : "not armed")}, stage {_stage}{(_current != null ? $" (waiting on {_current.Code})" : "")}; last: {_lastSummary}. 'codedoc plan|ask|stop'.");
                    return;
            }
        }
    }
}
