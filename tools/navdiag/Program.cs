using System;
using System.Collections.Generic;
using System.IO;
using AOBuddy;
using AOSharp.Common.GameData;

// NAVDIAG — scratch diagnosis harness (2026-09-25, "walked on air and stood on top of the Newland
// City wompah"): replays the overland planner's own queries for one zone offline, with the exact
// positions from that session's log, so a nav fix can be checked without logging the bot in.
//
//   navdiag [pluginDir] [pf]

internal static class Program
{
    private static int Main(string[] args)
    {
        string pluginDir = args.Length > 0 ? args[0]
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Build", "Plugins", "AOBuddy"));
        int pf = args.Length > 1 ? int.Parse(args[1]) : 566;

        try { Zoning.Load(pluginDir, m => { }); } catch { }
        var nav = AOBuddyNav.Load(pluginDir, pf);
        if (nav?.Ground == null) { Console.WriteLine($"no ground data for pf {pf} under {pluginDir}"); return 1; }

        Console.WriteLine($"== pf {pf} {Zoning.Name(pf)}: the planner's own numbers ==");
        foreach (var (x, z, label) in new[] {
            (272.5, 325.5, "mission-terminal area (13:36 plan)"),
            (326.0, 306.0, "16:14 login spot"),
            (385.0, 301.0, "16:15 plan spot, 6 m out"),
            (391.01, 301.73, "the wompah centre (to 655)"),
        })
        {
            double h = nav.Ground.HeightAt(x, z);
            double sw = nav.Ground.SwimY(x, z, 0.3);
            Console.WriteLine($"  ({x,7:0.0},{z,7:0.0}) {label,-36} terrainY={(double.IsNaN(h) ? "none" : h.ToString("0.00"))}  water={(double.IsNaN(sw) ? "dry" : sw.ToString("0.00"))}");
        }

        var grid = (IWalkGrid)OverlandGrid.Build(pluginDir, pf, nav, m => { }) ?? FloorGrid.Build(pluginDir, pf, nav, m => { });
        Console.WriteLine($"  walk grid: {grid?.GetType().Name ?? "none"}");

        ZoneExit toAndromeda = null;
        foreach (var e in Zoning.ExitsFrom(pf))
            if (e.ToPf == 655) { toAndromeda = e; break; }
        Console.WriteLine($"  exit to 655: kind={toAndromeda?.Kind} A={toAndromeda?.A}");
        if (toAndromeda == null || grid == null) return 0;

        Console.WriteLine($"== today's two planner decisions ==");
        foreach (var (fx, fy, fz, label) in new[] {
            (272.5f, 32.0f, 325.5f, "13:36 from the terminal area"),
            (385.0f, 32.4f, 301.0f, "16:15 from the street above the wompah"),
        })
        {
            var from = new Vector3(fx, fy, fz);
            double groundHere = nav.Ground.HeightAt(from.X, from.Z);
            bool offGrid = grid is OverlandGrid && !double.IsNaN(groundHere) && Math.Abs(from.Y - groundHere) > 2;
            float gap = Math.Abs(toAndromeda.A.Y - from.Y);
            Console.WriteLine($"  {label}: y={from.Y:0.0} groundHere={groundHere:0.0} offGrid={offGrid} goalY={toAndromeda.A.Y:0.00} gap={gap:0.0} m");
            if (!offGrid)
            {
                var route = grid.FindPath(from, toAndromeda.A, new HashSet<int>(), 8f, 1.5f, out string why);
                Console.WriteLine($"    on-grid FindPath -> {(route == null ? "REFUSED: " + why : route.Count + " pts")}");
            }
            else
            {
                // The StraightMaxDrop guard (2026-09-25): off-grid straight walks are LEVEL-only.
                Console.WriteLine($"    off-grid straight walk -> {(gap > 2f ? "REFUSED by the guard: goal " + gap.ToString("0.0") + " m off our height, no ramp in the data (was: air-walk onto the wompah's roof)" : "level, allowed")}");
            }
        }

        Console.WriteLine($"== control: a whompa that WORKS (Andromeda -> Tir, stood and zoned 15:14:36) ==");
        var nav655 = AOBuddyNav.Load(pluginDir, 655);
        ZoneExit toTir = null;
        foreach (var e in Zoning.ExitsFrom(655))
            if (e.ToPf == 640) { toTir = e; break; }
        if (nav655?.Ground != null && toTir != null)
        {
            var from = new Vector3(3178f, 36.0f, 861f);
            double groundHere = nav655.Ground.HeightAt(from.X, from.Z);
            bool offGrid = !double.IsNaN(groundHere) && Math.Abs(from.Y - groundHere) > 2;
            float gap = Math.Abs(toTir.A.Y - from.Y);
            Console.WriteLine($"  from y={from.Y:0.0} groundHere={groundHere:0.0} offGrid={offGrid} goalY={toTir.A.Y:0.00} gap={gap:0.0} m -> {(gap > 2f ? "REFUSED (REGRESSION!)" : "level, allowed (correct)")}");
        }
        return 0;
    }
}