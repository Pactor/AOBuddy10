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
    /// WANT LIST (owner, 2026-09-25): what the mission run rolls for. One list per bot, wants.json in its plugin
    /// folder, changeable while it runs (commands, or the file edited by hand - re-read when it changes). Entries are
    /// an exact item name, or a query: kind (nano, implant, weapon, armor, gear = weapon or armor, spirit, any),
    /// optionally a profession (nanos: the nano program's profession requirement) and a QL band.
    /// Mode 'always' = a standing filter; 'list' = collect everything on it, then say done and stop.
    /// Learned nanos do NOT count as had (the owner rolls for other professions); held items (inventory, bags, the
    /// bank as last seen) and the got record do.
    /// Matching is by template: a nano crystal's nano program comes from WantData (its Upload function), its
    /// profession from that program's requirements, gear and implants from their ItemClass.
    /// </summary>
    public sealed class WantList
    {
        public sealed class Entry
        {
            public string Name;              // exact item name, or null for a query
            public string Kind = "any";      // nano | implant | weapon | armor | gear | spirit | any
            public int Prof;                 // Stat.Profession value, 0 = any
            public int QlMin, QlMax = 1000;
            public override string ToString()
            {
                if (Name != null) return "'" + Name + "'";
                string p = Prof > 0 ? " " + ProfName(Prof) : "";
                string q = QlMin <= 0 && QlMax >= 1000 ? "" : QlMax >= 1000 ? $" QL {QlMin}+" : $" QL {QlMin}-{QlMax}";
                return Kind + p + q;
            }
        }

        public string Mode = "list";
        public readonly List<Entry> Entries = new List<Entry>();
        public readonly HashSet<int> Got = new HashSet<int>();   // templates (low id) collected by want runs

        private readonly string _path;
        private readonly Action<string> _log;
        private DateTime _stamp;

        public WantList(string pluginDir, Action<string> log) { _path = Path.Combine(pluginDir, "wants.json"); _log = log; Reload(true); }

        // ---- file --------------------------------------------------------------------------------------------

        /// <summary>Re-read wants.json when it changed on disk (or when forced). True when it was (re)loaded.</summary>
        public bool Reload(bool force = false)
        {
            try
            {
                if (!File.Exists(_path)) { if (force) { Entries.Clear(); Got.Clear(); } return false; }
                var t = File.GetLastWriteTimeUtc(_path);
                if (!force && t == _stamp) return false;
                _stamp = t;
                var o = JObject.Parse(File.ReadAllText(_path));
                Mode = (string)o["mode"] ?? "list";
                Entries.Clear();
                foreach (JObject e in (JArray)o["entries"] ?? new JArray())
                    Entries.Add(new Entry { Name = (string)e["name"], Kind = (string)e["kind"] ?? "any", Prof = (int?)e["prof"] ?? 0, QlMin = (int?)e["qlMin"] ?? 0, QlMax = (int?)e["qlMax"] ?? 1000 });
                Got.Clear();
                foreach (var g in (JArray)o["got"] ?? new JArray()) Got.Add((int)g);
                _log?.Invoke($"WANTS: loaded {Entries.Count} entr{(Entries.Count == 1 ? "y" : "ies")} ({Mode}).");
                return true;
            }
            catch (Exception ex) { _log?.Invoke("WANTS: couldn't read wants.json: " + ex.Message); return false; }
        }

        public void Save()
        {
            var o = new JObject
            {
                ["mode"] = Mode,
                ["entries"] = new JArray(Entries.Select(e => e.Name != null ? new JObject { ["name"] = e.Name }
                    : new JObject { ["kind"] = e.Kind, ["prof"] = e.Prof, ["qlMin"] = e.QlMin, ["qlMax"] = e.QlMax })),
                ["got"] = new JArray(Got.OrderBy(x => x)),
            };
            if (JsonStore.Save(_path, o.ToString(), _log)) { try { _stamp = File.GetLastWriteTimeUtc(_path); } catch { } }
        }

        // ---- parsing ('mission run want add ...') --------------------------------------------------------------

        private static readonly Dictionary<string, int> Profs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "soldier", 1 }, { "sol", 1 }, { "martialartist", 2 }, { "ma", 2 }, { "engineer", 3 }, { "engi", 3 }, { "fixer", 4 },
            { "agent", 5 }, { "adventurer", 6 }, { "adv", 6 }, { "trader", 7 }, { "bureaucrat", 8 }, { "crat", 8 },
            { "enforcer", 9 }, { "enf", 9 }, { "doctor", 10 }, { "doc", 10 }, { "nanotechnician", 11 }, { "nt", 11 },
            { "metaphysicist", 12 }, { "mp", 12 }, { "keeper", 14 }, { "shade", 15 }
        };
        public static string ProfName(int p) => Profs.Where(kv => kv.Value == p).OrderByDescending(kv => kv.Key.Length).Select(kv => kv.Key).FirstOrDefault() ?? p.ToString();
        private static readonly Dictionary<string, string> KindWords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "nano", "nano" }, { "nanos", "nano" }, { "implant", "implant" }, { "implants", "implant" }, { "weapon", "weapon" },
            { "weapons", "weapon" }, { "armor", "armor" }, { "armour", "armor" }, { "gear", "gear" }, { "spirit", "spirit" },
            { "spirits", "spirit" }, { "any", "any" }
        };

        /// <summary>'nano engineer ql 20-30', 'implant ql 200+', 'gear', or an exact item name.</summary>
        public static Entry Parse(string text)
        {
            text = (text ?? "").Trim();
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return null;
            if (!KindWords.TryGetValue(words[0], out string kind)) return new Entry { Name = text };
            var e = new Entry { Kind = kind };
            var m = Regex.Match(text, @"ql\s*(\d+)\s*(?:-\s*(\d+)|(\+))?", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                e.QlMin = int.Parse(m.Groups[1].Value);
                e.QlMax = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : m.Groups[3].Success ? 1000 : e.QlMin;
            }
            foreach (var w in words.Skip(1)) if (Profs.TryGetValue(w, out int p)) e.Prof = p;
            return e;
        }

        // ---- matching ------------------------------------------------------------------------------------------

        /// <summary>The nano program's professions (its Profession / VisualProfession requirements); empty = any.</summary>
        public static HashSet<int> NanoProfs(int nanoId)
        {
            var set = new HashSet<int>();
            if (!ItemData.Find(nanoId, out NanoItem ni) || ni?.Criteria == null) return set;
            foreach (var list in ni.Criteria.Values)
                foreach (var c in list)
                    if ((c.Param1 == (int)Stat.Profession || c.Param1 == (int)Stat.VisualProfession) && c.Param2 > 0 && c.Param2 < 20) set.Add(c.Param2);
            return set;
        }

        public static string NameOf(int template) => ItemData.Find(template, out DummyItem d) && d?.Name != null ? d.Name : null;

        /// <summary>Does this reward (template, QL) fit the entry?</summary>
        public static bool Fits(Entry e, int low, int ql)
        {
            if (e.Name != null) return string.Equals(NameOf(low), e.Name, StringComparison.OrdinalIgnoreCase);
            if (ql < e.QlMin || ql > e.QlMax) return false;
            int cls = WantData.ClassOf(low);
            switch (e.Kind)
            {
                case "nano":
                    int nano = WantData.NanoOf(low);
                    if (nano == 0) return false;
                    return e.Prof == 0 || NanoProfs(nano).Contains(e.Prof);
                case "implant": return cls == WantData.Implant;
                case "weapon": return cls == WantData.Weapon;
                case "armor": return cls == WantData.Armor;
                case "gear": return cls == WantData.Weapon || cls == WantData.Armor;
                case "spirit": return cls == WantData.Spirit;
                default: return cls != WantData.NpcEquip;
            }
        }

        /// <summary>
        /// Is this reward wanted now: fits an entry and is not had yet. In 'list' mode a nano query wants each of its
        /// nanos once, a named item once; a gear/implant/spirit query stays open (it has no fixed set).
        /// </summary>
        public Entry Wanted(int low, int ql, ISet<int> held)
        {
            foreach (var e in Entries)
            {
                if (!Fits(e, low, ql)) continue;
                if (Mode == "list" && (e.Name != null || e.Kind == "nano") && (Got.Contains(low) || held.Contains(low))) continue;
                return e;
            }
            return null;
        }

        /// <summary>The crystals a nano query stands for: every crystal whose nano fits it.</summary>
        public static List<int> CrystalsFor(Entry e)
        {
            var list = new List<int>();
            if (e.Name != null || e.Kind != "nano") return list;
            foreach (var kv in WantData.Crystals)
            {
                // The QL is the crystal's (Shatter Bone: crystal QL 37, what the terminal offers); the nano
                // program itself reads QL 1 in the item data.
                if (!ItemData.Find(kv.Key, out DummyItem cr) || cr == null) continue;
                if (cr.Ql < e.QlMin || cr.Ql > e.QlMax) continue;
                if (e.Prof != 0 && !NanoProfs(kv.Value).Contains(e.Prof)) continue;
                list.Add(kv.Key);
            }
            return list;
        }

        /// <summary>Still to collect, per entry: templates left (named item: -1 while not had); open queries: null.</summary>
        public List<(Entry e, List<int> left)> Remaining(ISet<int> held)
        {
            var r = new List<(Entry, List<int>)>();
            foreach (var e in Entries)
            {
                if (e.Kind == "nano" && e.Name == null) r.Add((e, CrystalsFor(e).Where(c => !Got.Contains(c) && !held.Contains(c)).ToList()));
                else if (e.Name != null)
                {
                    bool have = Got.Any(g => string.Equals(NameOf(g), e.Name, StringComparison.OrdinalIgnoreCase))
                                || held.Any(h => string.Equals(NameOf(h), e.Name, StringComparison.OrdinalIgnoreCase));
                    r.Add((e, have ? new List<int>() : new List<int> { -1 }));
                }
                else r.Add((e, null));
            }
            return r;
        }
    }
}
