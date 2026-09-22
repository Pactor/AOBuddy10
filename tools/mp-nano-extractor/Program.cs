using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

// Extracts every nano a Meta-Physicist (profession id 12) can cast from the
// local OmniCell client data, using OmniCell.Core.dll to parse nanos.dat.
//
// MP-castability criterion: the nano's own cast Actions (ActionType ToUse = 3,
// and any other action) carry a Requirement on the character's Profession stat
// (60) with value 12. Any nano that lists Profession==12 among its use criteria
// is castable by an MP. Ids/names/levels come only from local data.
internal static class Program
{
    // ---- Stat ids (from AOSharp Stat.cs, verified) ----
    const int STAT_LEVEL = 54;      // 0x36
    const int STAT_PROFESSION = 60; // 0x3C
    const int STAT_VISUALPROF = 368; // 0x170 VisualProfession (many nanos gate MP on this)
    const int STAT_NANOSTRAIN = 75; // 0x4B
    const int PROF_META = 12;
    const int ACTION_TOUSE = 3;
    const int OP_EQUALTO = 0;

    // FunctionType ids worth summarising (from OmniCell.Enums.FunctionType).
    static readonly Dictionary<int, string> FnNames = new()
    {
        {53045,"Modify"},{53184,"ModifyPercentage"},{53175,"ScalingModify"},
        {53002,"Hit"},{53073,"AreaHit"},{53185,"DrainHit"},{53196,"SpecialHit"},
        {53167,"SummonPet"},{53181,"SummonPets"},
        {53051,"CastNano"},{53087,"AreaCastNano"},{53206,"CastNanoIfPossible"},
        {53212,"CastNanoIfPossibleOnFightTarget"},{53066,"TeamCastNano"},{53089,"CastStunNano"},
        {53153,"Mezz"},{53127,"CharmNpc"},{53128,"Daze"},{53121,"Fear"},
        {53016,"Teleport"},{53059,"LineTeleport"},{53082,"TeleportProxy"},{53165,"ProxyTeleportWithPetHandling"},
        {53154,"SummonPlayer"},{53155,"SummonTeamMates"},
        {53028,"AddSkill"},{53012,"Skill"},{53033,"LockSkill"},
        {53224,"AddDefProc"},{53227,"AddOffProc"},
        {53201,"RemoveNano"},{53105,"RemoveNanoEffects"},{53236,"RemoveNanoStrain"},{53162,"ResistNanoStrain"},
        {53117,"TauntNpc"},{53213,"ControlHate"},{53178,"DisableDefenseShield"},{53019,"UploadNano"},
        {53064,"SpawnItem"},{53177,"ReduceNanoStrainDuration"},{53026,"Set"},
        {53208,"SetAnchor"},{53209,"RecallToAnchor"},{53110,"ChangeVariable"},
    };

    static Dictionary<int, string> StatNames = new();
    static Dictionary<int, string> ItemNames = new();
    static Dictionary<int, string> NanoLines = new();

    static string BIN = @"E:\Funcom\OmniCell\OmniCell\Libraries\Source\OmniCell.Core\bin\Release\net10.0";

    static object Get(object o, string name)
    {
        if (o == null) return null;
        var t = o.GetType();
        var p = t.GetProperty(name);
        if (p != null) return p.GetValue(o, null);
        var f = t.GetField(name);
        return f == null ? null : f.GetValue(o);
    }

    static int ToInt(object o)
    {
        if (o == null) return 0;
        if (o is int i) return i;
        // enums
        try { return Convert.ToInt32(o); } catch { }
        // MessagePackObject or other -> parse its ToString
        long v;
        if (long.TryParse(Convert.ToString(o, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out v)) return (int)v;
        return 0;
    }

    static void Main(string[] args)
    {
        string nanosPath = args.Length > 0 ? args[0] : @"E:\Funcom\attic\extracted-client-data\nanos.dat";
        string itemNamesPath = @"E:\Funcom\attic\extracted-client-data\itemnames.sql";
        string statCs = @"E:\Funcom\AOBuddy10\AOSharp.Common\GameData\Stat.cs";
        string nanoEnums = @"E:\Funcom\AOBuddy10\AOSharp.Common\GameData\NanoEnums.cs";
        string outDir = args.Length > 1 ? args[1] : Directory.GetCurrentDirectory();

        // Resolve OmniCell's sibling DLLs from its bin dir.
        AssemblyLoadContext.Default.Resolving += (ctx, an) =>
        {
            string cand = Path.Combine(BIN, an.Name + ".dll");
            return File.Exists(cand) ? ctx.LoadFromAssemblyPath(cand) : null;
        };

        StatNames = ParseEnumLike(statCs, "Stat");
        NanoLines = ParseEnumBlock(nanoEnums, "NanoLine");
        ItemNames = ParseItemNames(itemNamesPath);
        Console.WriteLine($"stat names: {StatNames.Count}, nano lines: {NanoLines.Count}, item names: {ItemNames.Count}");

        Assembly core = Assembly.LoadFrom(Path.Combine(BIN, "OmniCell.Core.dll"));
        Type loader = core.GetTypes().First(t => t.Name == "NanoLoader");
        loader.GetMethod("CacheAllNanos", new[] { typeof(string) }).Invoke(null, new object[] { nanosPath });
        var list = (IDictionary)loader.GetField("NanoList").GetValue(null);
        Console.WriteLine("nanos loaded: " + list.Count);

        // Diagnostic: dump requirements for specific ids (3rd arg = csv of ids).
        if (args.Length > 2)
        {
            foreach (var idsStr in args[2].Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                int did = int.Parse(idsStr.Trim());
                if (!list.Contains(did)) { Console.WriteLine($"{did}: NOT in nanos.ocp"); continue; }
                object n = list[did];
                var st = Get(n, "Stats") as IDictionary;
                int strn = st != null && st.Contains(STAT_NANOSTRAIN) ? ToInt(st[STAT_NANOSTRAIN]) : 0;
                Console.WriteLine($"{did} '{ItemNames.GetValueOrDefault(did, "?")}' strain={strn} ({NanoLines.GetValueOrDefault(strn, "?")})");
                var acts = Get(n, "Actions") as IEnumerable;
                foreach (object a in acts ?? Array.Empty<object>())
                {
                    int at = ToInt(Get(a, "ActionType"));
                    var rq = Get(a, "Requirements") as IEnumerable;
                    var parts = new List<string>();
                    foreach (object r in rq ?? Array.Empty<object>())
                        parts.Add($"{StatNames.GetValueOrDefault(ToInt(Get(r,"Statnumber")),"Stat"+ToInt(Get(r,"Statnumber")))} {OpStr(ToInt(Get(r,"Operator")))} {ToInt(Get(r,"Value"))}");
                    Console.WriteLine($"   action {at}: {string.Join("  ;  ", parts)}");
                }
            }
            return;
        }

        // Full FunctionType id->name for diagnostics.
        var fnTypeAll = ParseEnumLike(@"E:\Funcom\OmniCell\OmniCell\Libraries\Source\OmniCell.Enums\FunctionType.cs", "FunctionType");
        var allFnHist = new Dictionary<int, int>();
        var unmappedFnHist = new Dictionary<int, int>();

        var entries = new List<Entry>();
        var reviewLines = new List<string>();
        var opHistogram = new Dictionary<int, int>();

        foreach (DictionaryEntry de in list)
        {
            object nano = de.Value;
            int id = ToInt(Get(nano, "ID"));

            // Gather all requirements from all actions.
            var actions = Get(nano, "Actions") as IEnumerable;
            if (actions == null) continue;

            bool mpCastable = false;
            int profOp = -1;
            int profStat = 0;
            var toUseReqs = new List<Req>();
            foreach (object action in actions)
            {
                int atype = ToInt(Get(action, "ActionType"));
                var reqs = Get(action, "Requirements") as IEnumerable;
                if (reqs == null) continue;
                foreach (object r in reqs)
                {
                    int stat = ToInt(Get(r, "Statnumber"));
                    int val = ToInt(Get(r, "Value"));
                    int op = ToInt(Get(r, "Operator"));
                    if ((stat == STAT_PROFESSION || stat == STAT_VISUALPROF) && val == PROF_META && op == OP_EQUALTO)
                    {
                        mpCastable = true;
                        profOp = op;
                        profStat = stat;
                        int key = stat; // 60 or 368
                        if (!opHistogram.ContainsKey(key)) opHistogram[key] = 0;
                        opHistogram[key]++;
                    }
                    if (atype == ACTION_TOUSE)
                    {
                        toUseReqs.Add(new Req
                        {
                            stat = stat,
                            statName = StatNames.TryGetValue(stat, out var sn) ? sn : ("Stat" + stat),
                            op = op,
                            value = val
                        });
                    }
                }
            }
            if (!mpCastable) continue;

            // strain / nano line
            int strain = 0;
            var stats = Get(nano, "Stats") as IDictionary;
            if (stats != null && stats.Contains(STAT_NANOSTRAIN)) strain = ToInt(stats[STAT_NANOSTRAIN]);
            string lineName = NanoLines.TryGetValue(strain, out var ln) ? ln : (strain == 0 ? "(none)" : "Strain" + strain);

            // min level from ToUse level requirement
            int minLevel = 0;
            foreach (var r in toUseReqs)
                if (r.stat == STAT_LEVEL && (r.op == 2 /*Greater*/ || r.op == OP_EQUALTO)) minLevel = Math.Max(minLevel, r.value + (r.op == 2 ? 1 : 0));

            // effect summary
            var effects = new List<string>();
            var rawFnNames = new List<string>();
            int otherFns = 0;
            var events = Get(nano, "Events") as IEnumerable;
            if (events != null)
            {
                foreach (object ev in events)
                {
                    var fns = Get(ev, "Functions") as IEnumerable;
                    if (fns == null) continue;
                    foreach (object fn in fns)
                    {
                        int ftype = ToInt(Get(fn, "FunctionType"));
                        rawFnNames.Add(fnTypeAll.TryGetValue(ftype, out var rfn) ? rfn : ("Fn" + ftype));
                        var argVals = Get(Get(fn, "Arguments"), "Values") as IEnumerable;
                        var ints = new List<int>();
                        var raw = new List<string>();
                        if (argVals != null)
                            foreach (object a in argVals) { raw.Add(Convert.ToString(a, CultureInfo.InvariantCulture)); ints.Add(ToInt(a)); }

                        allFnHist[ftype] = allFnHist.GetValueOrDefault(ftype) + 1;
                        if (FnNames.TryGetValue(ftype, out var fname))
                            effects.Add(SummariseFn(fname, ftype, ints, raw));
                        else
                        {
                            otherFns++;
                            unmappedFnHist[ftype] = unmappedFnHist.GetValueOrDefault(ftype) + 1;
                        }
                    }
                }
            }
            if (otherFns > 0 && effects.Count == 0) effects.Add($"({otherFns} non-effect functions only)");

            entries.Add(new Entry
            {
                id = id,
                name = ItemNames.TryGetValue(id, out var nm) ? nm : "(name not in itemnames.sql)",
                strain = strain,
                nanoLine = lineName,
                minLevel = minLevel,
                professionReqOp = profOp,
                professionReqVia = profStat == STAT_VISUALPROF ? "VisualProfession(368)==12" : "Profession(60)==12",
                castReqs = toUseReqs
                    .Where(r => r.stat != STAT_NANOSTRAIN)
                    .Select(r => $"{r.statName} {OpStr(r.op)} {r.value}")
                    .ToList(),
                effectSummary = effects,
                category = Categorise(lineName, effects, rawFnNames),
                source = "local:nanos.ocp (OmniCell.Core) + name from itemnames.sql"
            });
            reviewLines.Add($"{id}\t{lineName}\t{minLevel}\t{ItemNames.GetValueOrDefault(id,"?")}\tfns:[{string.Join(",", rawFnNames.Distinct())}]");
        }

        Console.WriteLine("MP-castable nanos: " + entries.Count);
        Console.WriteLine("profession-req stat histogram (statId->count): " +
            string.Join(", ", opHistogram.Select(k => $"{(k.Key==STAT_VISUALPROF?"VisualProfession(368)":"Profession(60)")}:{k.Value}")));

        // group by category
        var byCat = entries
            .GroupBy(e => e.category)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.minLevel == 0 ? int.MaxValue : e.minLevel).ThenBy(e => e.name).ToList());

        Console.WriteLine("category counts:");
        foreach (var kv in byCat.OrderByDescending(k => k.Value.Count))
            Console.WriteLine($"  {kv.Key}: {kv.Value.Count}");

        var opt = new JsonSerializerOptions { WriteIndented = true };
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "entries-flat.json"),
            JsonSerializer.Serialize(entries.OrderBy(e => e.category).ThenBy(e => e.minLevel).ToList(), opt));
        File.WriteAllText(Path.Combine(outDir, "entries-bycat.json"),
            JsonSerializer.Serialize(byCat, opt));
        // strain histogram for review
        var strainHist = entries.GroupBy(e => e.nanoLine).OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key} ({g.First().strain}): {g.Count()}").ToList();
        File.WriteAllText(Path.Combine(outDir, "strain-histogram.txt"), string.Join("\n", strainHist));
        // function-type diagnostics
        string FnRow(KeyValuePair<int,int> k) => $"{k.Value,5}  {k.Key}  {(fnTypeAll.TryGetValue(k.Key, out var n) ? n : "?")}";
        File.WriteAllText(Path.Combine(outDir, "fn-all.txt"),
            string.Join("\n", allFnHist.OrderByDescending(k => k.Value).Select(FnRow)));
        File.WriteAllText(Path.Combine(outDir, "fn-unmapped.txt"),
            string.Join("\n", unmappedFnHist.OrderByDescending(k => k.Value).Select(FnRow)));
        File.WriteAllText(Path.Combine(outDir, "review.tsv"),
            string.Join("\n", reviewLines.OrderBy(x => x)));
        Console.WriteLine("wrote outputs to " + outDir);

        WriteDeliverables(nanosPath, list.Count, entries, byCat);
    }

    static void WriteDeliverables(string nanosPath, int nanosInData, List<Entry> entries,
        Dictionary<string, List<Entry>> byCat)
    {
        string jsonPath = @"E:\Funcom\AOBuddy10\AOBuddy\GameData\profiles\metaphysicist-nanos.json";
        string mdPath = @"E:\Funcom\AOBuddy10\AOBuddy\GameData\profiles\metaphysicist-nanos.md";
        Directory.CreateDirectory(Path.GetDirectoryName(jsonPath));

        int viaVisual = entries.Count(e => e.professionReqVia.StartsWith("Visual"));
        int viaProf = entries.Count - viaVisual;
        string now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // Order categories in a sensible functional order for output.
        string[] order = {
            "Pets - Attack","Pets - Mezz/Support","Pets - Heal","Pets - Charm",
            "Pet Buffs / Pet Utility","Summon Weapon / Shield (Creation)",
            "Self / Team Buffs","Nukes / Damage","Debuffs","Debuffs - Mind/Nano",
            "Mezz / Calm","Heals / HoT","Travel / Recall","Travel / Summon Utility","Misc / Utility"
        };
        var orderedCats = byCat.Keys.OrderBy(k => { int i = Array.IndexOf(order, k); return i < 0 ? 999 : i; }).ToList();

        var catCounts = orderedCats.ToDictionary(k => k, k => byCat[k].Count);

        var root = new Dictionary<string, object>
        {
            ["profile"] = "Metaphysicist castable nanos (for AOBuddy10 bot decision-making)",
            ["profession"] = new Dictionary<string, object> { ["id"] = 12, ["name"] = "Metaphysicist" },
            ["generatedUtc"] = now,
            ["dataSource"] = new Dictionary<string, object>
            {
                ["nanoData"] = nanosPath + " (OMNICELL-CONTENT v3 pack, client 18.8.50_EP1 extraction) loaded via OmniCell.Core NanoLoader.CacheAllNanos",
                ["itemNames"] = @"E:\Funcom\attic\extracted-client-data\itemnames.sql",
                ["stat/nanoLineEnums"] = @"E:\Funcom\AOBuddy10\AOSharp.Common\GameData\Stat.cs, NanoEnums.cs",
                ["extractor"] = @"E:\Funcom\AOBuddy10\tools\mp-nano-extractor (net10)",
                ["nanosInData"] = nanosInData
            },
            ["castabilityCriterion"] =
                "A nano is MP-castable when one of its cast Actions (OmniCell.Enums.ActionType.ToUse=3) carries an "
                + "Operator.EqualTo requirement on Profession(stat 60)==12 OR VisualProfession(stat 368)==12. "
                + "All matches use EqualTo. VisualProfession is how most older MP nanos are profession-locked; "
                + $"in this data {viaProf} gate via Profession(60) and {viaVisual} via VisualProfession(368).",
            ["statIds"] = new Dictionary<string, int>
            {
                ["Level"] = 54, ["Profession"] = 60, ["VisualProfession"] = 368, ["NanoStrain"] = 75,
                ["MaterialCreation"] = 130, ["MaterialMetamorphosis"] = 127, ["BiologicalMetamorphosis"] = 128,
                ["PsychologicalModification"] = 129, ["SensoryImprovement"] = 122, ["SpaceTime"] = 131
            },
            ["fieldNotes"] = new Dictionary<string, string>
            {
                ["minLevel"] = "Character Level requirement read from the ToUse action (stat 54). 0 = no explicit level requirement; the real gate is the nano-skill requirement in castReqs.",
                ["castReqs"] = "The full ToUse action requirement list, stat names from Stat.cs. '>' means the CellAO GreaterThan operator (stat >= value at runtime).",
                ["effectSummary"] = "Derived from the nano's Events->Functions in nanos.ocp. Modify shows 'stat +/-amount'; SummonPet shows the pet template arg; Hit shows [attackType,min,max,...]; other function types shown as Name[rawArgs].",
                ["nanoLine"] = "AOSharp NanoLine enum name for the nano's NanoStrain (stat 75)."
            },
            ["totalCount"] = entries.Count,
            ["categoryCounts"] = catCounts,
            ["categories"] = orderedCats.ToDictionary(k => k, k => byCat[k].Select(ToOut).ToList())
        };

        var opt = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(root, opt));

        // ---- Markdown ----
        var sb = new StringBuilder();
        sb.AppendLine("# Meta-Physicist (profession 12) — Castable Nano Reference");
        sb.AppendLine();
        sb.AppendLine($"Generated {now} for the AOBuddy10 bot. **{entries.Count} nanos.**");
        sb.AppendLine();
        sb.AppendLine("## Method / provenance");
        sb.AppendLine();
        sb.AppendLine("- **Nano data (ground truth):** `" + nanosPath + "` — OmniCell OMNICELL-CONTENT v3 pack (client 18.8.50_EP1 extraction), loaded through `OmniCell.Core` `NanoLoader.CacheAllNanos` (net10 DLL). " + nanosInData + " nano formulas in the pack.");
        sb.AppendLine("- **Names:** joined by nano id against `itemnames.sql` (`itemnames` table).");
        sb.AppendLine("- **Enums:** stat ids and nano-line names from `AOSharp.Common/GameData/Stat.cs` and `NanoEnums.cs`.");
        sb.AppendLine("- **Extractor source:** `E:\\Funcom\\AOBuddy10\\tools\\mp-nano-extractor` (re-runnable).");
        sb.AppendLine("- **No web data was used for any id, name or level.** A web check (AO wiki/aoitems) was used only to confirm the heal-pet line has ~10 strengths, which matches the 10 found locally.");
        sb.AppendLine();
        sb.AppendLine("## MP-castability criterion");
        sb.AppendLine();
        sb.AppendLine("A nano is included when one of its cast `Actions` (`ActionType.ToUse` = 3) has an `EqualTo` requirement on **`Profession`(stat 60) == 12** or **`VisualProfession`(stat 368) == 12**. VisualProfession is how most older MP nanos are profession-locked. Split: " + viaProf + " via Profession(60), " + viaVisual + " via VisualProfession(368). Every match uses the EqualTo operator.");
        sb.AppendLine();
        sb.AppendLine("## Category counts");
        sb.AppendLine();
        sb.AppendLine("| Category | Count |");
        sb.AppendLine("| --- | ---: |");
        foreach (var k in orderedCats) sb.AppendLine($"| {k} | {byCat[k].Count} |");
        sb.AppendLine($"| **Total** | **{entries.Count}** |");
        sb.AppendLine();
        foreach (var k in orderedCats)
        {
            sb.AppendLine($"## {k} ({byCat[k].Count})");
            sb.AppendLine();
            sb.AppendLine("| id | name | nano line (strain) | minLvl | cast reqs | effect |");
            sb.AppendLine("| ---: | --- | --- | ---: | --- | --- |");
            foreach (var e in byCat[k].OrderBy(x => x.minLevel == 0 ? int.MaxValue : x.minLevel).ThenBy(x => x.name))
            {
                string reqs = string.Join("; ", e.castReqs).Replace("|", "\\|");
                string eff = string.Join("; ", e.effectSummary).Replace("|", "\\|");
                string lvl = e.minLevel == 0 ? "-" : e.minLevel.ToString();
                sb.AppendLine($"| {e.id} | {e.name.Replace("|","\\|")} | {e.nanoLine} ({e.strain}) | {lvl} | {reqs} | {eff} |");
            }
            sb.AppendLine();
        }
        File.WriteAllText(mdPath, sb.ToString());
        Console.WriteLine("wrote deliverables:\n  " + jsonPath + "\n  " + mdPath);
    }

    static Dictionary<string, object> ToOut(Entry e) => new()
    {
        ["id"] = e.id,
        ["name"] = e.name,
        ["nanoStrain"] = e.strain,
        ["nanoLine"] = e.nanoLine,
        ["minLevel"] = e.minLevel,
        ["professionReqVia"] = e.professionReqVia,
        ["castReqs"] = e.castReqs,
        ["effectSummary"] = e.effectSummary,
        ["source"] = e.source
    };

    static string SummariseFn(string fname, int ftype, List<int> ints, List<string> raw)
    {
        switch (fname)
        {
            case "Modify":
            case "ModifyPercentage":
            case "ScalingModify":
                if (ints.Count >= 2)
                {
                    string sn = StatNames.TryGetValue(ints[0], out var s) ? s : ("Stat" + ints[0]);
                    return $"{fname} {sn} {(ints[1] >= 0 ? "+" : "")}{ints[1]}";
                }
                break;
            case "SummonPet":
            case "SummonPets":
                return $"{fname} template=[{string.Join(",", ints)}]";
        }
        return $"{fname}[{string.Join(",", raw)}]";
    }

    static string OpStr(int op) => op switch
    {
        0 => "==",
        1 => "<",
        2 => ">",
        24 => "!=",
        _ => "op" + op
    };

    // Assign a functional category using the nano line name (authoritative
    // strain) plus effect-function hints.
    static string Categorise(string line, List<string> effects, List<string> rawFns)
    {
        string l = line.ToLowerInvariant();
        bool HasFn(string s) => effects.Any(e => e.StartsWith(s, StringComparison.OrdinalIgnoreCase));
        bool HasRaw(string s) => rawFns.Any(e => string.Equals(e, s, StringComparison.OrdinalIgnoreCase));

        // Weapon/shield "Creation:" nanos spawn an item.
        if (HasRaw("SpawnItem")) return "Summon Weapon / Shield (Creation)";
        if (HasRaw("SetAnchor") || HasRaw("RecallToAnchor")) return "Travel / Recall";

        if (l == "attackpets") return "Pets - Attack";
        if (l == "healpets") return "Pets - Heal";
        if (l == "supportpets") return "Pets - Mezz/Support";
        if (l.Contains("charm")) return "Pets - Charm";
        if (l == "pethealing" || l == "petsacrifice") return "Pet Heals / Calling";
        if (l == "petwarp") return "Pet Warp";
        if (l.StartsWith("mppet") || l.StartsWith("petshortterm") || l.StartsWith("petproc")
            || l == "petaoesnare" || l == "damagetopet" || l == "petdebuffcleanse"
            || l == "petinitiative" || l.Contains("pet")) return "Pet Buffs / Pet Utility";

        if (l.Contains("mind") && l.Contains("debuff")) return "Debuffs - Mind/Nano";
        if (l.Contains("damagedebuff") || l == "nanodeltadebuff" || l == "proximityrangedebuff"
            || (l.Contains("debuff"))) return "Debuffs";

        if (l.Contains("nuke") || l == "mind" ) return "Nukes";

        if (l == "mezz" || l == "aoemezz") return "Mezz / Calm";
        if (l.Contains("root")) return "Root";
        if (l.Contains("snare")) return "Snare";
        if (l.Contains("calm")) return "Mezz / Calm";

        if (l.Contains("heal") ) return "Heals / HoT";

        // Self / team buff lines (nano-skill "Mochams"/Infuse, int, nanocost,
        // interrupt, evasion, weapon buffs, construct empowerments, false prof).
        if (l.Contains("buff") || l.Contains("empowerment") || l == "falseprofession"
            || l == "interruptmodifier" || l == "nanoprogrammingbuff" || l.StartsWith("psy_"))
            return "Self / Team Buffs";

        if (l.Contains("grid") || l == "summonitem" || l.Contains("map")) return "Travel / Summon Utility";

        // effect-based fallbacks
        if (HasFn("SummonPet")) return "Pets - Attack";
        if (HasFn("Mezz")) return "Mezz / Calm";
        if (HasFn("CharmNpc")) return "Pets - Charm";
        if (effects.Any(e => e.StartsWith("Hit") || e.StartsWith("AreaHit") || e.StartsWith("DrainHit") || e.StartsWith("SpecialHit"))) return "Nukes / Damage";
        // Remaining stat-modify with no line hint: treat positive as buff.
        if (HasFn("Modify -") ) return "Debuffs";
        if (HasFn("Modify")) return "Self / Team Buffs";

        return "Misc / Utility";
    }

    // ---- parsers ----
    static Dictionary<int, string> ParseItemNames(string path)
    {
        var d = new Dictionary<int, string>();
        string text = File.ReadAllText(path);
        var rx = new Regex(@"\(\s*(\d+)\s*,\s*'((?:[^']|'')*)'", RegexOptions.Compiled);
        foreach (Match m in rx.Matches(text))
        {
            int id = int.Parse(m.Groups[1].Value);
            string name = m.Groups[2].Value.Replace("''", "'");
            d[id] = name;
        }
        return d;
    }

    // Parse "Name = 0xNN," or "Name = NN," pairs anywhere in the file (Stat.cs).
    static Dictionary<int, string> ParseEnumLike(string path, string enumName)
    {
        var d = new Dictionary<int, string>();
        string text = File.ReadAllText(path);
        var rx = new Regex(@"([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(0x[0-9A-Fa-f]+|\d+)", RegexOptions.Compiled);
        foreach (Match m in rx.Matches(text))
        {
            string name = m.Groups[1].Value;
            string vs = m.Groups[2].Value;
            int val = vs.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToInt32(vs, 16) : int.Parse(vs);
            if (!d.ContainsKey(val)) d[val] = name; // first name wins
        }
        return d;
    }

    // Parse a specific enum block with C-style auto-increment (NanoLine).
    static Dictionary<int, string> ParseEnumBlock(string path, string enumName)
    {
        var d = new Dictionary<int, string>();
        string[] lines = File.ReadAllLines(path);
        int start = -1;
        for (int i = 0; i < lines.Length; i++)
            if (Regex.IsMatch(lines[i], @"enum\s+" + enumName + @"\b")) { start = i; break; }
        if (start < 0) return d;
        // find opening brace
        int i2 = start;
        while (i2 < lines.Length && !lines[i2].Contains("{")) i2++;
        i2++;
        int running = -1;
        var entryRx = new Regex(@"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*(=\s*(0x[0-9A-Fa-f]+|\d+))?", RegexOptions.Compiled);
        for (; i2 < lines.Length; i2++)
        {
            string ln = lines[i2];
            if (ln.Contains("}")) break;
            string trimmed = ln.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("//") || trimmed.StartsWith("/*") || trimmed.StartsWith("*")) continue;
            var m = entryRx.Match(ln);
            if (!m.Success) continue;
            string name = m.Groups[1].Value;
            if (name == "enum") continue;
            int val;
            if (m.Groups[3].Success)
            {
                string vs = m.Groups[3].Value;
                val = vs.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt32(vs, 16) : int.Parse(vs);
            }
            else val = running + 1;
            running = val;
            if (!d.ContainsKey(val)) d[val] = name;
        }
        return d;
    }

    class Req { public int stat; public string statName; public int op; public int value; }

    class Entry
    {
        public int id { get; set; }
        public string name { get; set; }
        public int strain { get; set; }
        public string nanoLine { get; set; }
        public int minLevel { get; set; }
        public int professionReqOp { get; set; }
        public string professionReqVia { get; set; }
        public List<string> castReqs { get; set; }
        public List<string> effectSummary { get; set; }
        public string category { get; set; }
        public string source { get; set; }
    }
}
