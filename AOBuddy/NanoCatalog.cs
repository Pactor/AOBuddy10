using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using Newtonsoft.Json;

namespace AOBuddy
{
    /// <summary>
    /// Deep-dive tool: export the FULL nano catalog from the offline item data (ItemData.AllNanoIds +
    /// Find&lt;NanoItem&gt;) to JSON — every nano, with the professions that can LEARN it (a nano with no
    /// profession requirement is castable by ALL classes), its line, NCU, stacking, effects, requirements, and
    /// duration (parsed from the name where present — duration isn't in the offline data). This is the ground
    /// truth for building buff/pet logic instead of only what a character happens to have uploaded.
    /// Writes nano_catalog_all.json plus one file per profession.
    /// </summary>
    public static class NanoCatalog
    {
        // AO profession ids (Stat.Profession value). Game constants, not per-character.
        private static readonly Dictionary<string, int> ProfByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "soldier", 1 }, { "ma", 2 }, { "engineer", 3 }, { "fixer", 4 }, { "agent", 5 },
            { "adventurer", 6 }, { "trader", 7 }, { "bureaucrat", 8 }, { "enforcer", 9 }, { "doctor", 10 },
            { "nanotechnician", 11 }, { "metaphysicist", 12 }, { "keeper", 14 }, { "shade", 15 }
        };
        private static readonly Dictionary<int, string> NameByProf = new Dictionary<int, string>
        {
            { 1, "Soldier" }, { 2, "MartialArtist" }, { 3, "Engineer" }, { 4, "Fixer" }, { 5, "Agent" },
            { 6, "Adventurer" }, { 7, "Trader" }, { 8, "Bureaucrat" }, { 9, "Enforcer" }, { 10, "Doctor" },
            { 11, "NanoTechnician" }, { 12, "MetaPhysicist" }, { 14, "Keeper" }, { 15, "Shade" }
        };
        // Aliases accepted on the command line.
        private static readonly Dictionary<string, int> Aliases = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        { { "mp", 12 }, { "crat", 8 }, { "engi", 3 }, { "enf", 9 }, { "doc", 10 }, { "nt", 11 }, { "adv", 6 } };

        private static readonly Regex DurationInName = new Regex(@"\((\d+)\s*(hour|hours|hr|min|minute|minutes|sec|seconds)\)", RegexOptions.IgnoreCase);

        private sealed class Req { public string Stat; public string Op; public int Value; }
        private sealed class Entry
        {
            public int Id; public string Name; public int Ql; public string Line;
            public int StackingOrder; public int Ncu; public string DurationText;
            public List<string> Professions;   // ["All"] = no profession restriction
            public List<Req> Requirements; public Dictionary<string, int> Effects;
        }

        /// <summary>Export the whole catalog + a per-profession file for each. Returns (nanoCount, dir) or (−1, reason).</summary>
        public static Tuple<int, string> ExportAll(string dir, Action<string> log = null)
        {
            var all = new List<Entry>();

            IReadOnlyList<int> ids;
            try { ids = ItemData.AllNanoIds(); }
            catch (Exception ex) { return Tuple.Create(-1, "item data not available: " + ex.Message); }
            if (ids == null || ids.Count == 0) return Tuple.Create(-1, "no nano ids (item data not loaded yet)");

            foreach (int id in ids)
            {
                if (!ItemData.Find(id, out NanoItem ni) || ni == null) continue;
                all.Add(new Entry
                {
                    Id = id,
                    Name = ni.Name,
                    Ql = ni.Ql,
                    Line = ni.NanoLine.ToString(),
                    StackingOrder = ni.StackingOrder,
                    Ncu = ni.NCU,
                    DurationText = ParseDuration(ni.Name),
                    Professions = AllowedProfessions(ni),
                    Requirements = ReadReqs(ni),
                    Effects = ReadEffects(ni)
                });
            }

            all = all.OrderBy(e => e.Line).ThenByDescending(e => e.StackingOrder).ThenBy(e => e.Name).ToList();
            Directory.CreateDirectory(dir);
            string allFile = Path.Combine(dir, "nano_catalog_all.json");
            if (!JsonStore.Save(allFile, JsonConvert.SerializeObject(all, Formatting.Indented), log))
                return Tuple.Create(-1, $"couldn't write {allFile} (see log)");

            // Per-profession files: nanos that profession can learn (its own + all "All" nanos).
            foreach (var kv in NameByProf)
            {
                string profName = kv.Value;
                var forProf = all.Where(e => e.Professions.Contains("All") || e.Professions.Contains(profName)).ToList();
                string file = Path.Combine(dir, $"nano_catalog_{profName.ToLowerInvariant()}.json");
                if (!JsonStore.Save(file, JsonConvert.SerializeObject(forProf, Formatting.Indented), log))
                    return Tuple.Create(-1, $"couldn't write {file} (see log)");
            }
            return Tuple.Create(all.Count, dir);
        }

        private static string ParseDuration(string name)
        {
            Match m = DurationInName.Match(name ?? "");
            return m.Success ? m.Value.Trim('(', ')') : null;
        }

        // Professions that may learn this nano. The gate is the VisualProfession (or Profession) criterion set
        // to a profession id; with none, it is castable by ALL classes (general nanos, nano cans, etc.).
        private static List<string> AllowedProfessions(NanoItem ni)
        {
            int profStat = (int)Stat.Profession;
            int visStat = (int)Stat.VisualProfession;
            var profs = new HashSet<int>();
            if (ni.Criteria != null)
                foreach (var list in ni.Criteria.Values)
                    foreach (RequirementCriterion c in list)
                        if ((c.Param1 == profStat || c.Param1 == visStat) && NameByProf.ContainsKey(c.Param2))
                            profs.Add(c.Param2);
            if (profs.Count == 0) return new List<string> { "All" };
            return profs.Select(p => NameByProf[p]).ToList();
        }

        private static List<Req> ReadReqs(NanoItem ni)
        {
            var reqs = new List<Req>();
            if (ni.Criteria == null) return reqs;
            foreach (var list in ni.Criteria.Values)
                foreach (RequirementCriterion c in list)
                {
                    string statName = Enum.IsDefined(typeof(Stat), c.Param1) ? ((Stat)c.Param1).ToString() : c.Param1.ToString();
                    reqs.Add(new Req { Stat = statName, Op = c.Operator.ToString(), Value = c.Param2 });
                }
            return reqs;
        }

        private static Dictionary<string, int> ReadEffects(NanoItem ni)
        {
            var eff = new Dictionary<string, int>();
            if (ni.Modifiers != null && ni.Modifiers.TryGetValue(SpellListType.Use, out var use) && use != null)
                foreach (var kv in use)
                    eff[kv.Key.ToString()] = kv.Value;
            return eff;
        }
    }
}
