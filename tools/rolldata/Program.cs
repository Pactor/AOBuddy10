using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using AOBuddy;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using Newtonsoft.Json.Linq;

// rolldata <buildDir> <outDir> [itemnames.sql]
// For every profession: each nano crystal whose nano program that profession can use, with the crystal's QL (the
// QL missions offer), the nano's line, NCU, level and nano-skill requirements, and the crystal's icon id. Writes
// <outDir>/rollable-<slug>.json for AODB's Rollable pages. Source: the bot's own item data (GameData/ItemData.bin
// and ItemWantData.bin - crystal -> nano via its Upload function), names/icons from itemnames.sql.
class Program
{
    static readonly (int id, string slug, string name)[] Profs =
    {
        (1, "soldier", "Soldier"), (2, "martialartist", "Martial Artist"), (3, "engineer", "Engineer"), (4, "fixer", "Fixer"),
        (5, "agent", "Agent"), (6, "adventurer", "Adventurer"), (7, "trader", "Trader"), (8, "bureaucrat", "Bureaucrat"),
        (9, "enforcer", "Enforcer"), (10, "doctor", "Doctor"), (11, "nanotechnician", "Nano-Technician"),
        (12, "metaphysicist", "Meta-Physicist"), (14, "keeper", "Keeper"), (15, "shade", "Shade"),
    };

    // The nano skills the pages show (MM BM PM MC TS SI) and level.
    static readonly (Stat stat, string key)[] Skills =
    {
        (Stat.MaterialMetamorphosis, "MM"), (Stat.BiologicalMetamorphosis, "BM"), (Stat.PsychologicalModification, "PM"),
        (Stat.MaterialCreation, "MC"), (Stat.SpaceTime, "TS"), (Stat.SensoryImprovement, "SI"), (Stat.Level, "LVL"),
    };

    static int Main(string[] args)
    {
        if (args.Length < 2) { Console.WriteLine("rolldata <buildDir> <outDir> [itemnames.sql]"); return 1; }
        string build = Path.GetFullPath(args[0]), outDir = Path.GetFullPath(args[1]);
        string namesPath = args.Length > 2 ? args[2] : @"E:\Funcom\attic\extracted-client-data\itemnames.sql";
        Directory.SetCurrentDirectory(build);
        typeof(Client).GetField("Logger", BindingFlags.NonPublic | BindingFlags.Static)
                      .SetValue(null, new Serilog.LoggerConfiguration().CreateLogger());
        WantData.Load(build, Console.WriteLine);

        var icons = new Dictionary<int, int>();
        var rx = new Regex(@"\(\s*(\d+)\s*,\s*'(?:[^']|'')*'\s*,\s*'(?:[^']|'')*'\s*,\s*'(\d+)'", RegexOptions.Compiled);
        foreach (Match m in rx.Matches(File.ReadAllText(namesPath, System.Text.Encoding.Latin1)))
            icons[int.Parse(m.Groups[1].Value)] = int.Parse(m.Groups[2].Value);

        var rows = new List<JObject>();
        foreach (var kv in WantData.Crystals)
        {
            int crystal = kv.Key, nano = kv.Value;
            if (!ItemData.Find(crystal, out DummyItem cr) || cr == null) continue;
            if (!ItemData.Find(nano, out NanoItem ni) || ni == null) continue;
            var profs = WantList.NanoProfs(nano);
            var req = new JObject();
            if (ni.Criteria != null)
                foreach (var list in ni.Criteria.Values)
                    foreach (var c in list)
                        foreach (var (stat, key) in Skills)
                            if (c.Param1 == (int)stat && c.Param2 > ((int?)req[key] ?? 0)) req[key] = c.Param2 + 1;   // 'GreaterThan n' = n+1
            rows.Add(new JObject
            {
                ["crystal"] = crystal, ["name"] = cr.Name, ["ql"] = cr.Ql, ["nano"] = nano, ["nanoName"] = ni.Name,
                ["line"] = ni.NanoLine.ToString(), ["ncu"] = ni.NCU, ["req"] = req,
                ["icon"] = icons.TryGetValue(crystal, out int ic) ? ic : 0,
                ["nanoIcon"] = icons.TryGetValue(nano, out int ni2) ? ni2 : 0,
                ["profs"] = new JArray(profs.OrderBy(x => x)),
            });
        }
        Directory.CreateDirectory(outDir);
        foreach (var (id, slug, name) in Profs)
        {
            var mine = rows.Where(r => ((JArray)r["profs"]).Any(p => (int)p == id)).OrderBy(r => (string)r["line"]).ThenBy(r => (int)r["ql"]).ToList();
            var o = new JObject
            {
                ["profession"] = name, ["id"] = id, ["generatedUtc"] = DateTime.UtcNow.ToString("u"),
                ["source"] = "rolldata: AOBuddy GameData ItemData + ItemWantData (crystal -> nano Upload 53019); icons/names itemnames.sql. Crystal QL = the QL missions offer. req 'GreaterThan n' shown as n+1.",
                ["crystals"] = new JArray(mine),
            };
            File.WriteAllText(Path.Combine(outDir, $"rollable-{slug}.json"), o.ToString(Newtonsoft.Json.Formatting.None));
            Console.WriteLine($"{slug}: {mine.Count} crystals, {mine.Select(r => (int)r["nano"]).Distinct().Count()} nanos");
        }
        return 0;
    }
}
