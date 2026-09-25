using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AOBuddy;

// GRIDWARMUP — build every zone's walk grid ahead of the bot's first run (owner, 2026-09-25: "the bot
// should create all navdata on the first run"). Walks the plugin's GameData/Nav folders, skips zones
// whose GridCache file is already valid, and builds + saves the rest exactly the way the bot would when
// entering the zone — so a freshly set-up bot starts with every grid on disk and never waits out the
// 0.6-3.8 s build at a zone border. Re-run it any time; it only pays for zones that changed.
//
//   gridwarmup [pluginDir] [--only <pf> <pf> ...]
//
// pluginDir defaults to the repo's Build\Plugins\AOBuddy (four up from the exe's bin output).

internal static class Program
{
    private static int Main(string[] args)
    {
        string pluginDir = null;
        var only = new HashSet<int>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--only")
            {
                for (int j = i + 1; j < args.Length && int.TryParse(args[j], out int p); j++) only.Add(p);
                break;
            }
            pluginDir = args[i];
        }
        if (pluginDir == null)
            pluginDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Build", "Plugins", "AOBuddy"));

        string navRoot = Path.Combine(pluginDir, "GameData", "Nav");
        if (!Directory.Exists(navRoot))
        {
            Console.Error.WriteLine($"No GameData/Nav under {pluginDir} — pass the bot's plugin folder as the argument.");
            return 1;
        }

        try { Zoning.Load(pluginDir, m => { }); } catch { }   // zone names, best effort

        List<int> zones = Directory.GetDirectories(navRoot)
            .Select(Path.GetFileName)
            .Where(n => int.TryParse(n, out _))
            .Select(int.Parse)
            .OrderBy(p => p)
            .ToList();
        if (only.Count > 0) zones = zones.Where(only.Contains).ToList();

        int built = 0, kept = 0, none = 0, noData = 0;
        var all = Stopwatch.StartNew();
        foreach (int pf in zones)
        {
            var nav = AOBuddyNav.Load(pluginDir, pf);
            if (nav == null) { noData++; Console.WriteLine($"pf {pf,-5} {Name(pf)}: no nav data"); continue; }

            if (GridCache.TryLoad(pluginDir, pf, nav, null) != null)
            {
                kept++;
                Console.WriteLine($"pf {pf,-5} {Name(pf)}: cache already valid");
                continue;
            }

            var sw = Stopwatch.StartNew();
            IWalkGrid grid = (IWalkGrid)OverlandGrid.Build(pluginDir, pf, nav, null) ?? FloorGrid.Build(pluginDir, pf, nav, null);
            if (grid == null) { none++; Console.WriteLine($"pf {pf,-5} {Name(pf)}: no walk grid from this data"); continue; }
            GridCache.Save(pluginDir, pf, grid, null);
            built++;
            Console.WriteLine($"pf {pf,-5} {Name(pf)}: built in {sw.ElapsedMilliseconds} ms");
        }

        Console.WriteLine();
        Console.WriteLine($"{zones.Count} zone(s): {built} built, {kept} already cached, {none} with no grid, {noData} with no data — {all.Elapsed.TotalMinutes:0.0} min.");
        return 0;
    }

    private static string Name(int pf)
    {
        string n = Zoning.Name(pf);
        return string.IsNullOrEmpty(n) ? "?" : n;
    }
}