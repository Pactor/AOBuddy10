using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.RegularExpressions;

// Builds metaphysicist-nano-sources.json: for every nano id in
// metaphysicist-nanos.json, a "where to get" record derived only from local data.
//
// Authoritative signals:
//  * Expansion requirement (stat 389) on the nano program's ToUse action AND on
//    its nano-crystal item's actions in items.ocp. BitAnd(op 22) value 2 = ShadowLands,
//    8 = AlienInvasion, 32 = LostEden (AOSharp ExpansionFlags). SL nanos are not
//    mission-rollable.
//  * Local vendor data (OmniCell research-output vendors.csv) = shop presence for a
//    crystal. (Only researched playfields are present, e.g. Temple of Three Winds.)
//  * AO default rule for everything else: a non-shop, non-expansion profession nano
//    crystal is mission-rollable. This rule is cited, not invented per nano.
internal static class Program
{
    const int STAT_EXPANSION = 389;    // 0x185
    const int FT_UPLOADNANO = 53019;
    const int OP_BITAND = 22;
    // ActionTypes that can carry use/wield criteria on a crystal.
    static readonly int[] CRYSTAL_ACTIONS = { 3 /*ToUse*/, 8 /*ToWield*/, 6 /*ToWear*/ };

    static string BIN = @"E:\Funcom\OmniCell\OmniCell\Libraries\Source\OmniCell.Core\bin\Release\net10.0";
    static Dictionary<int, string> ItemNames = new();

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
        try { return Convert.ToInt32(o); } catch { }
        return long.TryParse(Convert.ToString(o, CultureInfo.InvariantCulture), out var v) ? (int)v : 0;
    }

    static void Main(string[] args)
    {
        string itemsOcp = @"E:\Funcom\OmniCell\OmniCell\Datafiles\items.ocp";
        string nanosOcp = @"E:\Funcom\OmniCell\OmniCell\Datafiles\nanos.ocp";
        string itemNamesPath = @"E:\Funcom\attic\extracted-client-data\itemnames.sql";
        string nanoProfile = @"E:\Funcom\AOBuddy10\AOBuddy\GameData\profiles\metaphysicist-nanos.json";
        string outPath = @"E:\Funcom\AOBuddy10\AOBuddy\GameData\profiles\metaphysicist-nano-sources.json";

        AssemblyLoadContext.Default.Resolving += (ctx, an) =>
        {
            string cand = Path.Combine(BIN, an.Name + ".dll");
            return File.Exists(cand) ? ctx.LoadFromAssemblyPath(cand) : null;
        };

        ItemNames = ParseItemNames(itemNamesPath);

        Assembly core = Assembly.LoadFrom(Path.Combine(BIN, "OmniCell.Core.dll"));
        Type nloader = core.GetTypes().First(t => t.Name == "NanoLoader");
        nloader.GetMethod("CacheAllNanos", new[] { typeof(string) }).Invoke(null, new object[] { nanosOcp });
        var nanoList = (IDictionary)nloader.GetField("NanoList").GetValue(null);
        Type iloader = core.GetTypes().First(t => t.Name == "ItemLoader");
        iloader.GetMethod("CacheAllItems", new[] { typeof(string) }).Invoke(null, new object[] { itemsOcp });
        var itemList = (IDictionary)iloader.GetField("ItemList").GetValue(null);
        Console.WriteLine($"nanos {nanoList.Count}, items {itemList.Count}, itemNames {ItemNames.Count}");

        // MP nano ids + names from the profile we already produced.
        var profile = JsonDocument.Parse(File.ReadAllText(nanoProfile)).RootElement;
        var mpIds = new List<int>();
        var mpName = new Dictionary<int, string>();
        foreach (var cat in profile.GetProperty("categories").EnumerateObject())
            foreach (var e in cat.Value.EnumerateArray())
            {
                int id = e.GetProperty("id").GetInt32();
                mpIds.Add(id); mpName[id] = e.GetProperty("name").GetString();
            }
        var mpSet = new HashSet<int>(mpIds);
        Console.WriteLine("MP nano ids: " + mpIds.Count);

        // Map nanoId -> crystal item ids, by scanning items for UploadNano(nanoId).
        var nanoToCrystals = new Dictionary<int, List<int>>();
        foreach (DictionaryEntry de in itemList)
        {
            object item = de.Value;
            int itemId = ToInt(Get(item, "ID"));
            var events = Get(item, "Events") as IEnumerable;
            if (events == null) continue;
            foreach (object ev in events)
            {
                var fns = Get(ev, "Functions") as IEnumerable;
                if (fns == null) continue;
                foreach (object fn in fns)
                {
                    if (ToInt(Get(fn, "FunctionType")) != FT_UPLOADNANO) continue;
                    var vals = Get(Get(fn, "Arguments"), "Values") as IEnumerable;
                    if (vals == null) continue;
                    foreach (object a in vals)
                    {
                        int nid = ToInt(a);
                        if (mpSet.Contains(nid))
                        {
                            if (!nanoToCrystals.TryGetValue(nid, out var l)) nanoToCrystals[nid] = l = new List<int>();
                            if (!l.Contains(itemId)) l.Add(itemId);
                        }
                    }
                }
            }
        }
        Console.WriteLine("MP nanos with a crystal item located: " + nanoToCrystals.Count);

        // Local vendor presence: item ids sold (research-output vendors.csv).
        var (vendorItems, vendorFiles) = LoadVendorItems();
        Console.WriteLine($"vendor item ids from local research-output: {vendorItems.Count} ({vendorFiles.Count} files)");

        // Expansion bit collector.
        int ExpansionBits(object holder)
        {
            int bits = 0;
            var actions = Get(holder, "Actions") as IEnumerable;
            if (actions == null) return 0;
            foreach (object a in actions)
            {
                var reqs = Get(a, "Requirements") as IEnumerable;
                if (reqs == null) continue;
                foreach (object r in reqs)
                    if (ToInt(Get(r, "Statnumber")) == STAT_EXPANSION && ToInt(Get(r, "Operator")) == OP_BITAND)
                        bits |= ToInt(Get(r, "Value"));
            }
            return bits;
        }

        var sources = new SortedDictionary<string, object>();
        var howCounts = new Dictionary<string, int>();
        int fromExpansionFlag = 0, fromVendor = 0, fromCrystalOnlyExpansion = 0, noCrystal = 0;

        foreach (int id in mpIds.OrderBy(x => x))
        {
            int bits = nanoList.Contains(id) ? ExpansionBits(nanoList[id]) : 0;
            int progBits = bits;
            var crystals = nanoToCrystals.TryGetValue(id, out var cl) ? cl : new List<int>();
            if (crystals.Count == 0) noCrystal++;
            foreach (int cid in crystals)
                if (itemList.Contains(cid)) bits |= ExpansionBits(itemList[cid]);
            if (bits != 0 && progBits == 0 && crystals.Count > 0) fromCrystalOnlyExpansion++;

            bool sl = (bits & 0b10) != 0;        // ShadowLands (bit1)
            bool ai = (bits & 0b1000) != 0;      // AlienInvasion (bit3)
            bool le = (bits & 0b100000) != 0;    // LostEden (bit5)
            string expansion = sl ? "Shadowlands" : ai ? "Alien Invasion" : le ? "Lost Eden" : null;
            if (expansion != null) fromExpansionFlag++;

            // Vendor presence for any crystal.
            int soldCrystal = crystals.FirstOrDefault(c => vendorItems.Contains(c));
            bool inVendor = crystals.Any(c => vendorItems.Contains(c));
            if (inVendor) fromVendor++;

            string how, where, tier = null, notes;
            string sig = $"crystalItem(s)=[{string.Join(",", crystals)}]; nanoProgramExpansionBits={progBits}; combinedExpansionBits={bits}";

            if (sl)
            {
                how = "shadowlands";
                where = null;
                notes = "Shadowlands-gated: Expansion(389) BitAnd ShadowLands bit set on " +
                        (progBits != 0 ? "the nano program" : "the nano-crystal item") +
                        "'s use criteria. SL nanos are NOT mission-rollable (obtained via garden/drop/quest); the specific SL source/tier is not present in local data. " + sig;
            }
            else if (inVendor)
            {
                how = "shop";
                where = "vendor (local research-output); crystal item " + soldCrystal;
                notes = "Nano-crystal item " + soldCrystal + " ('" + ItemNames.GetValueOrDefault(soldCrystal, "?") + "') found in local vendor data (" + string.Join(",", vendorFiles) + "). " + sig;
            }
            else if (ai || le)
            {
                how = "mission-roll";
                where = null;
                notes = $"{expansion}-expansion profession nano. No shop entry for its crystal in local vendor data; classified mission-rollable by the AO default rule for non-shop profession nano crystals. " + sig;
            }
            else
            {
                how = "mission-roll";
                where = null;
                notes = "No Expansion gate on nano program or crystal, and no shop entry in local vendor data (which only covers researched playfields). Classified mission-rollable by the AO default rule for non-shop, non-expansion profession nano crystals. "
                        + (crystals.Count == 0 ? "No nano-crystal item was located in items.ocp for this nano id. " : "") + sig;
            }

            howCounts[how] = howCounts.GetValueOrDefault(how) + 1;
            sources[id.ToString()] = new Dictionary<string, object>
            {
                ["name"] = mpName[id],
                ["how"] = how,
                ["where"] = where,
                ["expansion"] = expansion,
                ["tier"] = tier,
                ["notes"] = notes
            };
        }

        var root = new Dictionary<string, object>
        {
            ["profile"] = "Metaphysicist nano acquisition sources (companion to metaphysicist-nanos.json)",
            ["profession"] = new Dictionary<string, object> { ["id"] = 12, ["name"] = "Metaphysicist" },
            ["generatedUtc"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ["method"] = new Dictionary<string, object>
            {
                ["expansionGate"] = "Read the Expansion(stat 389) BitAnd requirement from the nano program's ToUse action AND from its nano-crystal item (items.ocp) use/wield criteria. bit1(2)=ShadowLands, bit3(8)=AlienInvasion, bit5(32)=LostEden (AOSharp ExpansionFlags). Authoritative from client data.",
                ["crystalLink"] = "nano id -> crystal item id via scanning items.ocp for an UploadNano(53019) function whose argument is the nano id.",
                ["vendorData"] = "Shop presence = crystal item id present in OmniCell research-output vendors.csv. NOTE: local vendor data currently covers only researched playfields (Temple of Three Winds), so general nano-shop buyability is NOT separable from local data; such low/mid nanos fall to the mission-roll default below.",
                ["defaultRule"] = "AO default (cited, not invented): a non-shop, non-expansion profession nano crystal is mission-rollable. Applied only when no Expansion gate and no local shop entry exist.",
                ["noInvention"] = "No vendor, mob, garden, or tier is asserted unless it comes from local data. Unknown SL sub-sources and tiers are left null."
            },
            ["howCounts"] = howCounts,
            ["diagnostics"] = new Dictionary<string, object>
            {
                ["totalNanos"] = mpIds.Count,
                ["setFromExpansionFlag"] = fromExpansionFlag,
                ["expansionFromCrystalOnly"] = fromCrystalOnlyExpansion,
                ["setFromLocalVendor"] = fromVendor,
                ["nanosWithNoCrystalLocated"] = noCrystal
            },
            ["sources"] = sources,
            ["_sources"] = new List<string>
            {
                "local:nanos.ocp (nano program ToUse Expansion criteria)",
                "local:items.ocp (nano-crystal UploadNano link + crystal Expansion criteria)",
                "local:itemnames.sql (names)",
                "local:OmniCell/research-output vendors.csv (shop presence, researched playfields only)",
                "rule:AO default - non-shop non-expansion profession nano crystals are mission-rollable"
            }
        };

        var opt = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        File.WriteAllText(outPath, JsonSerializer.Serialize(root, opt));
        Console.WriteLine("how counts: " + string.Join(", ", howCounts.Select(k => $"{k.Key}:{k.Value}")));
        Console.WriteLine($"expansion-flagged: {fromExpansionFlag} (crystal-only: {fromCrystalOnlyExpansion}), vendor: {fromVendor}, no-crystal: {noCrystal}");
        Console.WriteLine("wrote " + outPath);
    }

    static (HashSet<int>, List<string>) LoadVendorItems()
    {
        var ids = new HashSet<int>();
        var files = new List<string>();
        string root = @"E:\Funcom\OmniCell\research-output";
        if (!Directory.Exists(root)) return (ids, files);
        foreach (string f in Directory.GetFiles(root, "vendors.csv", SearchOption.AllDirectories))
        {
            files.Add(Path.GetFileName(Path.GetDirectoryName(f)) + "/vendors.csv");
            foreach (string line in File.ReadLines(f).Skip(1))
            {
                var parts = line.Split(',');
                if (parts.Length >= 4)
                {
                    if (int.TryParse(parts[2], out int lo)) ids.Add(lo);
                    if (int.TryParse(parts[3], out int hi)) ids.Add(hi);
                }
            }
        }
        return (ids, files);
    }

    static Dictionary<int, string> ParseItemNames(string path)
    {
        var d = new Dictionary<int, string>();
        string text = File.ReadAllText(path);
        var rx = new Regex(@"\(\s*(\d+)\s*,\s*'((?:[^']|'')*)'", RegexOptions.Compiled);
        foreach (Match m in rx.Matches(text))
            d[int.Parse(m.Groups[1].Value)] = m.Groups[2].Value.Replace("''", "'");
        return d;
    }
}
