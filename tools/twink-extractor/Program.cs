using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;

// Everything twinking rests on, read out of the local client data.
//
// Nothing here is authored. Nanos come from nanos.ocp through OmniCell.Core's
// NanoLoader, items from items.ocp through ItemLoader, and names from
// itemnames.sql. Where a number is not in the data the tool says so rather
// than filling it in.
//
//   TwinkExtract probe   --name <regex> [--nanos|--items] [--limit N]
//   TwinkExtract probe   --id <id>[,<id>...]
//   TwinkExtract modifies --stat <id> [--nanos|--items] [--limit N]
//   TwinkExtract requires --stat <id> [--nanos|--items] [--limit N]
//   TwinkExtract stats   --id <id>      dump one template's stat dictionary
internal static class Program
{
    const int ACTION_TOUSE = 3;
    const int ACTION_TOWEAR = 6;
    const int ACTION_TOWIELD = 8;

    static string BIN = @"E:\Funcom\OmniCell\OmniCell\Libraries\Source\OmniCell.Core\bin\Release\net10.0";
    static string NANOS = @"E:\Funcom\OmniCell\OmniCell\Datafiles\nanos.ocp";
    static string ITEMS = @"E:\Funcom\OmniCell\OmniCell\Datafiles\items.ocp";
    static string NAMES = @"E:\Funcom\attic\extracted-client-data\itemnames.sql";
    static string STATCS = @"E:\Funcom\AOBuddy10\AOSharp.Common\GameData\Stat.cs";
    static string FNCS = @"E:\Funcom\OmniCell\OmniCell\Libraries\Source\OmniCell.Enums\FunctionType.cs";
    static string OPCS = @"E:\Funcom\OmniCell\OmniCell\Libraries\Source\OmniCell.Enums\Operator.cs";

    static Dictionary<int, string> StatNames = new();
    static Dictionary<int, string> FnNames = new();
    static Dictionary<int, string> OpNames = new();
    static Dictionary<int, string> ItemNames = new();

    static IDictionary Nanos;
    static IDictionary Items;

    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("usage: TwinkExtract <probe|modifies|requires|stats> [options]");
            return 1;
        }

        string mode = args[0];
        string nameRx = null;
        var ids = new List<int>();
        int stat = -1;
        int limit = 60;
        bool wantNanos = false, wantItems = false;
        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--name": nameRx = args[++i]; break;
                case "--id":
                    foreach (string s in args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries))
                        ids.Add(int.Parse(s.Trim(), CultureInfo.InvariantCulture));
                    break;
                case "--stat": stat = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "--limit": limit = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                case "--nanos": wantNanos = true; break;
                case "--items": wantItems = true; break;
                default: Console.Error.WriteLine("unknown: " + args[i]); return 1;
            }
        }

        if (!wantNanos && !wantItems) { wantNanos = true; wantItems = true; }

        AssemblyLoadContext.Default.Resolving += (ctx, an) =>
        {
            string cand = Path.Combine(BIN, an.Name + ".dll");
            return File.Exists(cand) ? ctx.LoadFromAssemblyPath(cand) : null;
        };

        StatNames = ParseEnum(STATCS, "Stat");
        FnNames = ParseEnum(FNCS, "FunctionType");
        OpNames = ParseEnum(OPCS, "Operator");
        ItemNames = ParseNames(NAMES);

        Assembly core = Assembly.LoadFrom(Path.Combine(BIN, "OmniCell.Core.dll"));
        if (wantNanos)
        {
            Type nl = core.GetTypes().First(t => t.Name == "NanoLoader");
            nl.GetMethod("CacheAllNanos", new[] { typeof(string) }).Invoke(null, new object[] { NANOS });
            Nanos = (IDictionary)nl.GetField("NanoList").GetValue(null);
        }

        if (wantItems)
        {
            Type il = core.GetTypes().First(t => t.Name == "ItemLoader");
            il.GetMethod("CacheAllItems", new[] { typeof(string) }).Invoke(null, new object[] { ITEMS });
            Items = (IDictionary)il.GetField("ItemList").GetValue(null);
        }

        Console.Error.WriteLine($"names {ItemNames.Count}, nanos {(Nanos?.Count ?? 0)}, items {(Items?.Count ?? 0)}");

        switch (mode)
        {
            case "probe": return Probe(nameRx, ids, limit);
            case "modifies": return Modifies(stat, limit);
            case "requires": return Requires(stat, limit);
            case "stats": return StatsOf(ids);
            default: Console.Error.WriteLine("unknown mode " + mode); return 1;
        }
    }

    // ---------------------------------------------------------------- probes

    static int Probe(string nameRx, List<int> ids, int limit)
    {
        var rx = nameRx == null ? null : new Regex(nameRx, RegexOptions.IgnoreCase);
        var hits = new List<(int Id, string Name, bool Nano)>();
        if (ids.Count > 0)
        {
            foreach (int id in ids)
            {
                bool isNano = Nanos != null && Nanos.Contains(id);
                hits.Add((id, Name(id), isNano));
            }
        }
        else
        {
            foreach (var kv in ItemNames)
            {
                if (rx == null || !rx.IsMatch(kv.Value)) continue;
                bool isNano = Nanos != null && Nanos.Contains(kv.Key);
                bool isItem = Items != null && Items.Contains(kv.Key);
                if (!isNano && !isItem) continue;
                hits.Add((kv.Key, kv.Value, isNano));
                if (hits.Count >= limit) break;
            }
        }

        foreach (var h in hits.OrderBy(h => h.Name, StringComparer.OrdinalIgnoreCase).ThenBy(h => h.Id))
        {
            object t = h.Nano ? Nanos[h.Id] : (Items != null && Items.Contains(h.Id) ? Items[h.Id] : null);
            if (t == null) { Console.WriteLine($"{h.Id}  {h.Name}  (not in either pack)"); continue; }
            Console.WriteLine($"=== {h.Id}  {h.Name}   [{(h.Nano ? "nano" : "item")}] ql={Get(t, "Quality")}");
            Dump(t);
            Console.WriteLine();
        }

        Console.Error.WriteLine($"{hits.Count} matched");
        return 0;
    }

    static int StatsOf(List<int> ids)
    {
        foreach (int id in ids)
        {
            object t = Nanos != null && Nanos.Contains(id) ? Nanos[id]
                     : Items != null && Items.Contains(id) ? Items[id] : null;
            if (t == null) { Console.WriteLine($"{id}: not in either pack"); continue; }
            Console.WriteLine($"=== {id}  {Name(id)}");
            var st = Get(t, "Stats") as IDictionary;
            if (st != null)
                foreach (DictionaryEntry e in st)
                    Console.WriteLine($"    {SName(ToInt(e.Key))} = {ToInt(e.Value)}");
        }

        return 0;
    }

    /// <summary>Everything whose effect functions change this stat.</summary>
    static int Modifies(int stat, int limit)
    {
        if (stat < 0) { Console.Error.WriteLine("--stat required"); return 1; }
        Console.WriteLine($"# things that modify {SName(stat)} ({stat})");
        Console.WriteLine(
            "kind\tid\tname\tql\tamount\tfunction\ttarget\tticks\tfnreqs\tncu\tslot\titemclass\twear\tuse");
        int shown = 0;
        foreach (var src in new[] { ("nano", Nanos), ("item", Items) })
        {
            if (src.Item2 == null) continue;
            foreach (DictionaryEntry de in src.Item2)
            {
                object t = de.Value;
                foreach (string row in Effects(t, stat))
                {
                    var st = Get(t, "Stats") as IDictionary;
                    int ncu = st != null && st.Contains(54) ? ToInt(st[54]) : 0;
                    int slot = st != null && st.Contains(298) ? ToInt(st[298]) : 0;
                    int cls = st != null && st.Contains(76) ? ToInt(st[76]) : 0;
                    Console.WriteLine(
                        $"{src.Item1}\t{ToInt(Get(t, "ID"))}\t{Name(ToInt(Get(t, "ID")))}\t{Get(t, "Quality")}\t{row}"
                        + $"\t{ncu}\t{slot}\t{cls}\t{Reqs(t, ACTION_TOWEAR) + Reqs(t, ACTION_TOWIELD)}\t{Reqs(t, ACTION_TOUSE)}");
                    shown++;
                }
            }
        }

        Console.Error.WriteLine($"{shown} rows");
        return 0;
    }

    /// <summary>Everything that asks for this stat before it can be used or worn.</summary>
    static int Requires(int stat, int limit)
    {
        if (stat < 0) { Console.Error.WriteLine("--stat required"); return 1; }
        Console.WriteLine($"# things that require {SName(stat)} ({stat})");
        Console.WriteLine("kind\tid\tname\tql\taction\toperator\tvalue");
        int shown = 0;
        foreach (var src in new[] { ("nano", Nanos), ("item", Items) })
        {
            if (src.Item2 == null) continue;
            foreach (DictionaryEntry de in src.Item2)
            {
                object t = de.Value;
                var actions = Get(t, "Actions") as IEnumerable;
                if (actions == null) continue;
                foreach (object a in actions)
                {
                    int at = ToInt(Get(a, "ActionType"));
                    var reqs = Get(a, "Requirements") as IEnumerable;
                    if (reqs == null) continue;
                    foreach (object r in reqs)
                    {
                        if (ToInt(Get(r, "Statnumber")) != stat) continue;
                        Console.WriteLine(
                            $"{src.Item1}\t{ToInt(Get(t, "ID"))}\t{Name(ToInt(Get(t, "ID")))}\t{Get(t, "Quality")}\t{at}\t{OName(ToInt(Get(r, "Operator")))}\t{ToInt(Get(r, "Value"))}");
                        shown++;
                    }
                }
            }
        }

        Console.Error.WriteLine($"{shown} rows");
        return 0;
    }

    /// <summary>One action's requirements, flattened onto a line.</summary>
    static string Reqs(object t, int actionType)
    {
        var actions = Get(t, "Actions") as IEnumerable;
        var parts = new List<string>();
        foreach (object a in actions ?? Array.Empty<object>())
        {
            if (ToInt(Get(a, "ActionType")) != actionType) continue;
            var reqs = Get(a, "Requirements") as IEnumerable;
            foreach (object r in reqs ?? Array.Empty<object>())
                parts.Add($"{SName(ToInt(Get(r, "Statnumber")))} {OName(ToInt(Get(r, "Operator")))} {ToInt(Get(r, "Value"))}");
        }

        return string.Join(" ; ", parts);
    }

    /// <summary>The effect rows of one template that touch a stat.</summary>
    static IEnumerable<string> Effects(object t, int stat)
    {
        var events = Get(t, "Events") as IEnumerable;
        if (events == null) yield break;
        foreach (object ev in events)
        {
            var fns = Get(ev, "Functions") as IEnumerable;
            if (fns == null) continue;
            foreach (object fn in fns)
            {
                var vals = Get(Get(fn, "Arguments"), "Values") as IEnumerable;
                var raw = new List<string>();
                foreach (object v in vals ?? Array.Empty<object>())
                    raw.Add(Convert.ToString(v, CultureInfo.InvariantCulture));

                // A Modify-style function names the stat in its first argument.
                if (raw.Count < 2) continue;
                int first;
                if (!int.TryParse(raw[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out first)) continue;
                if (first != stat) continue;

                int ft = ToInt(Get(fn, "FunctionType"));
                var frq = Get(fn, "Requirements") as IEnumerable;
                var parts = new List<string>();
                foreach (object r in frq ?? Array.Empty<object>())
                    parts.Add($"{SName(ToInt(Get(r, "Statnumber")))} {OName(ToInt(Get(r, "Operator")))} {ToInt(Get(r, "Value"))}");

                yield return string.Join(
                    "\t",
                    raw[1],
                    FName(ft),
                    ToInt(Get(fn, "Target")),
                    ToInt(Get(fn, "TickCount")) + "/" + ToInt(Get(fn, "TickInterval")),
                    string.Join(" ; ", parts));
            }
        }
    }

    static void Dump(object t)
    {
        var st = Get(t, "Stats") as IDictionary;
        if (st != null)
        {
            var interesting = new List<string>();
            foreach (DictionaryEntry e in st) interesting.Add($"{SName(ToInt(e.Key))}={ToInt(e.Value)}");
            Console.WriteLine("  stats: " + string.Join("  ", interesting));
        }

        var actions = Get(t, "Actions") as IEnumerable;
        foreach (object a in actions ?? Array.Empty<object>())
        {
            var reqs = Get(a, "Requirements") as IEnumerable;
            var parts = new List<string>();
            foreach (object r in reqs ?? Array.Empty<object>())
                parts.Add($"{SName(ToInt(Get(r, "Statnumber")))} {OName(ToInt(Get(r, "Operator")))} {ToInt(Get(r, "Value"))}");
            Console.WriteLine($"  action {ToInt(Get(a, "ActionType"))}: {string.Join("  ;  ", parts)}");
        }

        var events = Get(t, "Events") as IEnumerable;
        foreach (object ev in events ?? Array.Empty<object>())
        {
            Console.WriteLine($"  event {ToInt(Get(ev, "EventType"))}:");
            var fns = Get(ev, "Functions") as IEnumerable;
            foreach (object fn in fns ?? Array.Empty<object>())
            {
                var vals = Get(Get(fn, "Arguments"), "Values") as IEnumerable;
                var raw = new List<string>();
                foreach (object v in vals ?? Array.Empty<object>())
                    raw.Add(Convert.ToString(v, CultureInfo.InvariantCulture));
                var frq = Get(fn, "Requirements") as IEnumerable;
                var parts = new List<string>();
                foreach (object r in frq ?? Array.Empty<object>())
                    parts.Add($"{SName(ToInt(Get(r, "Statnumber")))} {OName(ToInt(Get(r, "Operator")))} {ToInt(Get(r, "Value"))}");
                string args2 = string.Join(",", raw);
                if (raw.Count > 0)
                {
                    int s0;
                    if (int.TryParse(raw[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out s0)
                        && StatNames.ContainsKey(s0))
                    {
                        args2 = SName(s0) + (raw.Count > 1 ? "," + string.Join(",", raw.Skip(1)) : string.Empty);
                    }
                }

                Console.WriteLine(
                    $"    {FName(ToInt(Get(fn, "FunctionType")))} target={ToInt(Get(fn, "Target"))} ticks={ToInt(Get(fn, "TickCount"))}/{ToInt(Get(fn, "TickInterval"))} args=[{args2}]"
                    + (parts.Count > 0 ? " reqs=[" + string.Join(" ; ", parts) + "]" : string.Empty));
            }
        }
    }

    // --------------------------------------------------------------- helpers

    static string Name(int id) => ItemNames.TryGetValue(id, out var n) ? n : "(not in itemnames.sql)";

    static string SName(int s) => StatNames.TryGetValue(s, out var n) ? n : "Stat" + s;

    static string FName(int f) => FnNames.TryGetValue(f, out var n) ? n : "Fn" + f;

    static string OName(int o) => OpNames.TryGetValue(o, out var n) ? n : "Op" + o;

    static object Get(object o, string prop)
    {
        if (o == null) return null;
        Type t = o.GetType();
        PropertyInfo p = t.GetProperty(prop);
        if (p != null) return p.GetValue(o);
        FieldInfo f = t.GetField(prop);
        return f?.GetValue(o);
    }

    static int ToInt(object o)
    {
        if (o == null) return 0;
        if (o is int i) return i;
        if (o is uint u) return (int)u;
        if (o is short s) return s;
        if (o is Enum) return Convert.ToInt32(o, CultureInfo.InvariantCulture);
        int v;
        return int.TryParse(Convert.ToString(o, CultureInfo.InvariantCulture),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : 0;
    }

    static Dictionary<int, string> ParseEnum(string path, string which)
    {
        var map = new Dictionary<int, string>();
        if (!File.Exists(path)) return map;
        foreach (Match m in Regex.Matches(
            File.ReadAllText(path), @"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(0x[0-9A-Fa-f]+|\d+)\s*,",
            RegexOptions.Multiline))
        {
            string v = m.Groups[2].Value;
            int n = v.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? Convert.ToInt32(v, 16)
                        : int.Parse(v, CultureInfo.InvariantCulture);
            if (!map.ContainsKey(n)) map[n] = m.Groups[1].Value;
        }

        return map;
    }

    static Dictionary<int, string> ParseNames(string path)
    {
        var map = new Dictionary<int, string>();
        if (!File.Exists(path)) return map;
        foreach (string line in File.ReadLines(path))
        {
            foreach (Match m in Regex.Matches(line, @"\(\s*(\d+)\s*,\s*'((?:[^']|'')*)'"))
            {
                int id = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                if (!map.ContainsKey(id)) map[id] = m.Groups[2].Value.Replace("''", "'");
            }
        }

        return map;
    }
}
