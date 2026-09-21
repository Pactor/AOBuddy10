using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AOSharp.Common.GameData;

namespace AOBuddy
{
    /// <summary>
    /// Loads Tyrbot's perks.sql and turns a character's trained perk list into a
    /// permanent-stat bonus map (Stat -&gt; amount) that the SDK folds into GetStat.
    ///
    /// perks.sql shape (plain INSERT statements, parsed by regex):
    ///   perk(id, name)
    ///   perk_level(id, perk_id, number, min_level)      number = the Nth trained level
    ///   perk_level_buffs(perk_level_id, skill, amount)  skill = display name, e.g. "Body development"
    ///
    /// A character who has trained N levels of a perk gets the SUM of perk_level_buffs
    /// across that perk's levels 1..N. Skill display names are mapped to the AOSharp
    /// <see cref="Stat"/> enum; anything unmapped is reported (never silently dropped).
    /// </summary>
    public class PerkData
    {
        // perk name (normalized) -> perk id
        private readonly Dictionary<string, int> _perkIdByName = new Dictionary<string, int>();
        // perk id -> display name (for messages)
        private readonly Dictionary<int, string> _perkNameById = new Dictionary<int, string>();
        // (perkId, number) -> perk_level id
        private readonly Dictionary<long, int> _levelId = new Dictionary<long, int>();
        // highest trained "number" available per perk (so we can clamp / report)
        private readonly Dictionary<int, int> _maxNumber = new Dictionary<int, int>();
        // perk_level id -> list of (skillDisplayName, amount)
        private readonly Dictionary<int, List<KeyValuePair<string, int>>> _levelBuffs = new Dictionary<int, List<KeyValuePair<string, int>>>();

        // normalized skill display name -> Stat
        private static readonly Dictionary<string, Stat> _skillToStat = BuildSkillMap();

        // --- Perks.xml: PacketID -> (line name, rank) --------------------------------
        // The wire (FullCharacter.ResearchGoals / SDK LocalPlayer.Perks[].SkillId) identifies
        // each trained perk STEP by its PacketID. Perks.xml maps that id to a name like
        // "Enhance DNA 2", from which we recover the line ("Enhance DNA") and rank (2). The
        // character's trained rank of a line = the highest rank whose id is present.
        private readonly Dictionary<int, KeyValuePair<string, int>> _lineByPacketId = new Dictionary<int, KeyValuePair<string, int>>();

        public bool PerkXmlLoaded { get; private set; }
        public int PerkXmlCount => _lineByPacketId.Count;

        public bool Loaded { get; private set; }
        public int PerkCount => _perkIdByName.Count;

        /// <summary>
        /// Load Perks.xml (PacketID -&gt; AOID -&gt; "Name rank"). Splits the trailing rank number
        /// off the display name so a PacketID resolves to (lineName, rank). Returns false on
        /// any failure and leaves the map empty.
        /// </summary>
        public bool LoadPerkXml(string xmlPath)
        {
            try
            {
                var doc = XDocument.Load(xmlPath);
                foreach (var el in doc.Descendants("Perk"))
                {
                    var pidAttr = el.Attribute("PacketID");
                    var nameAttr = el.Attribute("Name");
                    if (pidAttr == null || nameAttr == null) continue;
                    if (!int.TryParse(pidAttr.Value.TrimStart('0').Length == 0 ? "0" : pidAttr.Value.TrimStart('0'), out int pid)) continue;

                    // "Enhance DNA 2" -> line "Enhance DNA", rank 2. A name with no trailing
                    // number is rank 1.
                    string name = nameAttr.Value.Trim();
                    int rank = 1;
                    var m = Regex.Match(name, @"^(.*?)\s+(\d+)$");
                    string line;
                    if (m.Success) { line = m.Groups[1].Value.Trim(); int.TryParse(m.Groups[2].Value, out rank); }
                    else line = name;

                    _lineByPacketId[pid] = new KeyValuePair<string, int>(line, rank);
                }

                PerkXmlLoaded = _lineByPacketId.Count > 0;
                return PerkXmlLoaded;
            }
            catch
            {
                PerkXmlLoaded = false;
                return false;
            }
        }

        /// <summary>
        /// Turn the raw trained-perk PacketIDs from the wire into "Name:maxRank" lines that
        /// <see cref="ComputeBonuses"/> understands. Ids that Perks.xml does not know (research
        /// lines, unmapped perks) are returned in <paramref name="unresolvedIds"/>.
        /// </summary>
        public List<string> DetectPerkLines(IEnumerable<int> packetIds, out List<int> unresolvedIds)
        {
            unresolvedIds = new List<int>();
            var maxRank = new Dictionary<string, int>();
            if (packetIds != null)
            {
                foreach (int id in packetIds)
                {
                    if (id == 0) continue;
                    if (_lineByPacketId.TryGetValue(id, out var lr))
                    {
                        if (!maxRank.TryGetValue(lr.Key, out int cur) || lr.Value > cur)
                            maxRank[lr.Key] = lr.Value;
                    }
                    else
                    {
                        unresolvedIds.Add(id);
                    }
                }
            }

            return maxRank.OrderBy(k => k.Key).Select(kv => kv.Key + ":" + kv.Value).ToList();
        }

        private static long LevelKey(int perkId, int number) => ((long)perkId << 20) | (uint)number;

        private static string Norm(string s) =>
            new string((s ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        /// <summary>Parse perks.sql from disk. Returns false (and stays empty) on any failure.</summary>
        public bool Load(string sqlPath)
        {
            try
            {
                string text = File.ReadAllText(sqlPath);

                // perk(id, name)
                foreach (Match m in Regex.Matches(text, @"INSERT INTO perk\s*\(id,\s*name\)\s*VALUES\s*\((\d+),'((?:[^']|'')*)'\)"))
                {
                    int id = int.Parse(m.Groups[1].Value);
                    string name = m.Groups[2].Value.Replace("''", "'");
                    _perkIdByName[Norm(name)] = id;
                    _perkNameById[id] = name;
                }

                // perk_level(id, perk_id, number, min_level)
                foreach (Match m in Regex.Matches(text, @"INSERT INTO perk_level\s*\(id,\s*perk_id,\s*number,\s*min_level\)\s*VALUES\s*\((\d+),(\d+),(\d+),(-?\d+)\)"))
                {
                    int id = int.Parse(m.Groups[1].Value);
                    int perkId = int.Parse(m.Groups[2].Value);
                    int number = int.Parse(m.Groups[3].Value);
                    _levelId[LevelKey(perkId, number)] = id;
                    if (!_maxNumber.TryGetValue(perkId, out int cur) || number > cur)
                        _maxNumber[perkId] = number;
                }

                // perk_level_buffs(perk_level_id, skill, amount)
                foreach (Match m in Regex.Matches(text, @"INSERT INTO perk_level_buffs\s*\(perk_level_id,\s*skill,\s*amount\)\s*VALUES\s*\((\d+),'((?:[^']|'')*)',(-?\d+)\)"))
                {
                    int levelId = int.Parse(m.Groups[1].Value);
                    string skill = m.Groups[2].Value.Replace("''", "'");
                    int amount = int.Parse(m.Groups[3].Value);
                    if (!_levelBuffs.TryGetValue(levelId, out var list))
                        _levelBuffs[levelId] = list = new List<KeyValuePair<string, int>>();
                    list.Add(new KeyValuePair<string, int>(skill, amount));
                }

                Loaded = _perkIdByName.Count > 0 && _levelBuffs.Count > 0;
                return Loaded;
            }
            catch
            {
                Loaded = false;
                return false;
            }
        }

        /// <summary>
        /// Sum the permanent bonuses for a list of "Perk Name:level" lines (level = highest
        /// trained level of that perk). Outputs a Stat-&gt;amount map, plus human-readable
        /// diagnostics: which perks were unknown and which skill names could not be mapped.
        /// </summary>
        public Dictionary<Stat, int> ComputeBonuses(IEnumerable<string> perkLines, out List<string> unknownPerks, out List<string> unmappedSkills, out List<string> applied)
        {
            var result = new Dictionary<Stat, int>();
            unknownPerks = new List<string>();
            unmappedSkills = new List<string>();
            applied = new List<string>();
            var unmappedSet = new HashSet<string>();

            if (perkLines == null) return result;

            foreach (string raw in perkLines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;

                // "Name:level"  — level defaults to 1 if omitted.
                int colon = raw.LastIndexOf(':');
                string namePart = colon >= 0 ? raw.Substring(0, colon) : raw;
                int level = 1;
                if (colon >= 0) int.TryParse(raw.Substring(colon + 1).Trim(), out level);
                if (level < 1) level = 1;

                if (!_perkIdByName.TryGetValue(Norm(namePart), out int perkId))
                {
                    unknownPerks.Add(raw.Trim());
                    continue;
                }

                // Clamp to the perk's real max level so a typo can't over-apply.
                if (_maxNumber.TryGetValue(perkId, out int max) && level > max) level = max;

                int perkTotalSkills = 0;
                for (int number = 1; number <= level; number++)
                {
                    if (!_levelId.TryGetValue(LevelKey(perkId, number), out int levelId)) continue;
                    if (!_levelBuffs.TryGetValue(levelId, out var buffs)) continue;

                    foreach (var kv in buffs)
                    {
                        if (_skillToStat.TryGetValue(Norm(kv.Key), out Stat stat))
                        {
                            result[stat] = (result.TryGetValue(stat, out int cur) ? cur : 0) + kv.Value;
                            perkTotalSkills++;
                        }
                        else
                        {
                            unmappedSet.Add(kv.Key);
                        }
                    }
                }

                applied.Add($"{_perkNameById[perkId]} x{level}");
            }

            unmappedSkills = unmappedSet.OrderBy(s => s).ToList();
            return result;
        }

        /// <summary>Resolve a display / enum stat name (case/space-insensitive) to a Stat.</summary>
        public static bool TryResolveStat(string name, out Stat stat) =>
            _skillToStat.TryGetValue(Norm(name), out stat);

        // ---- Skill display name -> Stat ------------------------------------------

        private static Dictionary<string, Stat> BuildSkillMap()
        {
            var map = new Dictionary<string, Stat>();

            // 1) Every Stat member, matched by normalized name (handles Strength, FirstAid,
            //    BodyDevelopment, MaxHealth, NanoPool, RangedInit, etc.).
            foreach (Stat s in Enum.GetValues(typeof(Stat)))
            {
                string key = Norm(s.ToString());
                if (!map.ContainsKey(key)) map[key] = s;
            }

            // 2) Aliases for display names that don't normalize onto an enum member 1:1
            //    (enum spelling differs, or the display name is an AO alias). Keys are
            //    normalized display names; values are Stat enum member names.
            var aliases = new Dictionary<string, string>
            {
                { "2hedged", "Skill2hEdged" },
                { "breakingandentry", "BreakingEntry" },
                { "duckexplosives", "DuckExp" },
                { "evadeclose", "EvadeClsC" },
                { "maxnano", "MaxNanoEnergy" },
                { "offensemodifier", "AddAllOff" },
                { "defensemodifier", "AddAllDef" },
                { "psychologicalmodifications", "PsychologicalModification" },
                { "quantumphysics", "QuantumFT" },
                { "smg", "MGSMG" },
                { "trapdisarming", "TrapDisarm" },
                { "timeandspace", "SpaceTime" },
                { "mattercreation", "MaterialCreation" },
                { "mattermetamorphosis", "MaterialMetamorphosis" },
                { "criticalchance", "CriticalIncrease" },
                { "criticaldecrease", "CritialResistance" }, // enum member is (sic) spelled this way
                { "ncumemory", "MaxNCU" },
            };
            foreach (var kv in aliases)
            {
                if (Enum.TryParse(kv.Value, out Stat s))
                    map[kv.Key] = s;
            }

            return map;
        }
    }
}
