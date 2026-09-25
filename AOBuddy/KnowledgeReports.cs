using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;

namespace AOBuddy
{
    /// <summary>
    /// KNOWLEDGE REPORTS — the read-only "what am I / what do I have" commands (R3.4, moved verbatim
    /// from Main's command switch): class/whoami, nanos, active buffs, learnable, stat, supplies,
    /// and the auto-buff plan listing. No behaviour of their own — they format live character state
    /// and the item DB into replies; every string is byte-identical to the old Main bodies.
    /// Owns the learnable-cache (a heavy full-DB scan, built on demand and reused; 'learnable
    /// refresh' drops it), which used to be a Main field.
    /// </summary>
    public class KnowledgeReports
    {
        private readonly BotContext _ctx;
        private readonly SupportController _support;
        private readonly OwnerTracker _owner;

        private List<NanoItem> _learnableCache;   // built on demand (heavy DB scan), reused

        public KnowledgeReports(BotContext ctx, SupportController support, OwnerTracker owner)
        {
            _ctx = ctx;
            _support = support;
            _owner = owner;
        }

        // ---- class / whoami ------------------------------------------------------

        public string ClassLine()
        {
            LocalPlayer me = DynelManager.LocalPlayer;
            if (me == null) return "No character loaded.";
            me.TryGetStat(Stat.Level, out int lvl);
            me.TryGetStat(Stat.Health, out int hp);
            me.TryGetStat(Stat.MaxHealth, out int maxhp);
            me.TryGetStat(Stat.CurrentNCU, out int ncu);
            me.TryGetStat(Stat.MaxNCU, out int maxncu);
            me.TryGetStat(Stat.Breed, out int breed);
            me.TryGetStat(Stat.Strength, out int str);
            me.TryGetStat(Stat.Agility, out int agi);
            me.TryGetStat(Stat.Stamina, out int sta);
            me.TryGetStat(Stat.Intelligence, out int intel);
            me.TryGetStat(Stat.Sense, out int sen);
            me.TryGetStat(Stat.Psychic, out int psy);
            int uploaded = me.SpellList?.Length ?? 0;
            return $"{me.Name}: {me.Profession} lvl {lvl} ({(Breed)breed}). HP {hp}/{maxhp}, NCU {ncu}/{maxncu} used. " +
                   $"Str {str} Agi {agi} Sta {sta} Int {intel} Sen {sen} Psy {psy}. Uploaded nanos: {uploaded}.";
        }

        // ---- nanos ---------------------------------------------------------------

        public void ReportNanos(string filter, Action<string> reply)
        {
            LocalPlayer me = DynelManager.LocalPlayer;
            if (me?.SpellList == null || me.SpellList.Length == 0) { reply("No uploaded nanos found."); return; }

            var names = new List<string>();
            foreach (int id in me.SpellList)
            {
                if (ItemData.Find(id, out NanoItem ni) && ni != null)
                {
                    if (filter.Length > 0 && ni.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    names.Add($"{ni.Name} [{id}]");
                }
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            if (names.Count == 0) { reply(filter.Length > 0 ? $"No uploaded nanos match '{filter}'." : "No named nanos resolved."); return; }
            reply($"Uploaded ({names.Count}): " + HelpPages.Truncate(string.Join(", ", names), 440));
        }

        public void ReportActive(Action<string> reply)
        {
            LocalPlayer me = DynelManager.LocalPlayer;
            if (me == null) { reply("No character loaded."); return; }
            var buffs = me.Buffs;
            if (buffs == null || buffs.Count == 0) { reply("No active nanos."); return; }

            var parts = new List<string>();
            foreach (var b in buffs.OrderBy(b => b.Cooldown?.RemainingTime ?? 0))
            {
                string name = b.NanoItem != null ? b.NanoItem.Name : $"nano {b.Id}";
                double rem = b.Cooldown?.RemainingTime ?? 0;
                parts.Add($"{name} [{b.Id}] {FormatTime(rem)}");
            }
            reply($"Active ({parts.Count}): " + HelpPages.Truncate(string.Join(", ", parts), 440));
        }

        private static string FormatTime(double seconds)
        {
            if (seconds <= 0) return "—";
            int s = (int)seconds;
            return s >= 60 ? $"{s / 60}m{s % 60:00}s" : $"{s}s";
        }

        public void ReportLearnable(string arg, Action<string> reply)
        {
            LocalPlayer me = DynelManager.LocalPlayer;
            if (me == null) { reply("No character loaded."); return; }

            if (arg == "refresh") { _learnableCache = null; arg = ""; }

            if (_learnableCache == null)
            {
                var have = new HashSet<int>(me.SpellList ?? new int[0]);
                var found = new List<NanoItem>();
                foreach (int id in ItemData.AllNanoIds())
                {
                    if (have.Contains(id)) continue;
                    if (!ItemData.Find(id, out NanoItem ni) || ni == null) continue;
                    if (ni.MeetsUseReqs(null, true)) found.Add(ni);
                }
                _learnableCache = found;
            }

            IEnumerable<NanoItem> list = _learnableCache;
            if (arg.Length > 0)
                list = list.Where(n => n.Name.IndexOf(arg, StringComparison.OrdinalIgnoreCase) >= 0);

            var shown = list.OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
                            .Select(n => n.Name).Distinct().ToList();
            if (shown.Count == 0) { reply(arg.Length > 0 ? $"No qualifying nanos match '{arg}'." : "No qualifying nanos found."); return; }

            int cap = 30;
            string head = $"Can learn/cast ({shown.Count}{(arg.Length > 0 ? $" matching '{arg}'" : "")}): ";
            reply(head + HelpPages.Truncate(string.Join(", ", shown.Take(cap)), 400) + (shown.Count > cap ? $" …(+{shown.Count - cap}, filter with 'learnable <text>')" : ""));
        }

        // ---- stat ----------------------------------------------------------------

        // Read-only: any stat by name or number, as the server last sent it.
        public static string StatCommand(string arg)
        {
            var me = DynelManager.LocalPlayer;
            if (me == null) return "Not in play yet.";
            if (string.IsNullOrWhiteSpace(arg)) return "Usage: stat <name or number>, e.g. stat 349";
            Stat stat;
            if (int.TryParse(arg.Trim(), out int id)) stat = (Stat)id;
            else if (!Enum.TryParse(arg.Trim(), true, out stat)) return $"No stat named '{arg}'.";
            return me.TryGetStat(stat, out int v) ? $"{stat} ({(int)stat}) = {v} (0x{v:X})" : $"{stat} ({(int)stat}) has not been sent to me.";
        }

        // ---- supplies ------------------------------------------------------------

        /// <summary>
        /// What he is carrying, per QL, and which of it his skill can actually reach. Two lines, one per kind:
        /// the bag, then the verdict and the skill behind it — so a number that looks wrong can be checked
        /// against what is in the bag without reading the log.
        /// </summary>
        public void ReportSupplies(Action<string> reply)
        {
            LocalPlayer me = DynelManager.LocalPlayer;
            if (me == null) { reply("Not in play yet."); return; }

            reply(SupplyLine("stims", _ctx.Config.StimKeyword, _ctx.Config.StimItemName, Stat.FirstAid, "First Aid", me));
            reply(SupplyLine("rechargers", _ctx.Config.RechargerKeyword, _ctx.Config.RechargerItemName, Stat.Treatment, "Treatment", me));
        }

        private string SupplyLine(string what, string keyword, string exactName, Stat skill, string skillName, LocalPlayer me)
        {
            List<Item> carried = _support.HealItemPoolUnfiltered(keyword, exactName).ToList();
            if (carried.Count == 0) return $"No {what} at all.";

            // Group by QL so it reads the way he counts them: "24 QL7, 38 QL9".
            var byQl = carried.GroupBy(it => it.Ql).OrderBy(g => g.Key)
                .Select(g => new { Ql = g.Key, Count = g.Sum(it => Math.Max(1, it.Count)), Usable = g.Any(it => SupportController.MeetsHealReqs(it, me)) })
                .ToList();

            int total = byQl.Sum(g => g.Count);
            int usable = byQl.Where(g => g.Usable).Sum(g => g.Count);
            string skillHave = me.TryGetStat(skill, out int sv) ? sv.ToString() : "unknown";
            string bag = string.Join(", ", byQl.Select(g => $"{g.Count} QL{g.Ql}"));

            if (usable == total)
                return $"{bag} — {total} {what}, all usable with my {skillName} of {skillHave}.";

            string canUse = string.Join(", ", byQl.Where(g => g.Usable).Select(g => $"{g.Count} QL{g.Ql}"));
            string tooHigh = string.Join(", ", byQl.Where(g => !g.Usable).Select(g => $"QL{g.Ql}"));
            return $"{bag} — {total} {what}. {(usable == 0 ? "None" : canUse)} usable with my {skillName} "
                   + $"of {skillHave}; {tooHigh} need more.";
        }

        // ---- auto-buff plan (the autobuff/keepup command) -------------------------

        public void ReportBuffPlans(Action<string> reply)
        {
            LocalPlayer meB = DynelManager.LocalPlayer;
            if (meB == null) { reply("No character loaded."); return; }
            List<string> pl = _support.DescribeBuffPlans(meB, _owner.Find());
            if (pl.Count == 0) { reply("Auto-buff: no learned nanos classified as keep-up buffs."); return; }
            reply($"Auto-buff {(_ctx.Config.AutoBuff ? "ON" : "OFF")} (self={_ctx.Config.BuffSelf} owner={_ctx.Config.BuffOwner} team={_ctx.Config.BuffTeam}, margin {_ctx.Config.RebuffMarginSeconds:0}s): {pl.Count} buffs.");
            foreach (string s in pl.Take(20)) reply(s);
            if (pl.Count > 20) reply($"…(+{pl.Count - 20} more)");
        }
    }
}