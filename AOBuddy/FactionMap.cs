using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace AOBuddy
{
    /// <summary>
    /// THE OTHER SIDE'S GROUND, for every route planner (moved out of MissionRun, 2026-09-27, so travel reads the
    /// same map). GameData/FactionAreas.json: Clan cities (whole zones, r 0) and each side's whompa stations (r
    /// metres round the spot) from the owner's Saavik's map, placed with Zoning.json's whompa positions.
    /// Side is the Side stat (1 Clan, 2 Omni); a neutral (or unread side) avoids none.
    ///
    /// Why travel needs it: 20:11-20:14 (2026-09-27) travelto from the Longest Road Omni town to a door in the same
    /// zone planned Broken Shores -> Rome Park -> Jobe -> Old Athen (540, Clan city) -> the Bliss (Clan) whompa,
    /// because travel's options had no faction filter; the mission run stopped it at Old Athen and it repeated.
    /// </summary>
    public static class FactionMap
    {
        public sealed class Area { public int side, pf; public float x, z, r; public string name; }

        private static readonly object Gate = new object();
        private static List<Area> _areas;

        /// <summary>The areas, read once from pluginDir/GameData/FactionAreas.json (empty, logged, when unreadable).</summary>
        public static List<Area> Areas(string pluginDir, Action<string> log)
        {
            lock (Gate)
            {
                if (_areas != null) return _areas;
                _areas = new List<Area>();
                try { _areas = JObject.Parse(File.ReadAllText(Path.Combine(pluginDir, "GameData", "FactionAreas.json")))["areas"].ToObject<List<Area>>(); }
                catch (Exception ex) { log?.Invoke($"FACTIONS: no faction areas ({ex.Message})."); }
                return _areas;
            }
        }

        /// <summary>The other side's area at this spot (whole zone, or round a station when x/z are known), or null.
        /// NaN x/z (spot unknown) matches whole zones only.</summary>
        public static string HostileAt(string pluginDir, Action<string> log, int side, int pf, float x, float z)
        {
            if (side != 1 && side != 2) return null;
            foreach (var a in Areas(pluginDir, log))
                if (a.side != side && a.side > 0 && a.pf == pf && (a.r <= 0 || Math.Sqrt((a.x - x) * (a.x - x) + (a.z - z) * (a.z - z)) < a.r)) return a.name;
            return null;
        }

        /// <summary>The exit's start counts only round a station (r > 0): a whole hostile zone must never trap him inside it.
        /// 4 Holes (760) is on the avoid list, and in it every exit read 'hostile' - no way out for 9 minutes
        /// (23:49-00:00, 2026-09-25/26).</summary>
        public static bool HostileStart(string pluginDir, Action<string> log, int side, int pf, float x, float z)
        {
            if (side != 1 && side != 2) return false;
            foreach (var a in Areas(pluginDir, log))
                if (a.side != side && a.pf == pf && a.r > 0 && Math.Sqrt((a.x - x) * (a.x - x) + (a.z - z) * (a.z - z)) < a.r) return true;
            return false;
        }

        /// <summary>
        /// An exit that starts round the other side's station or comes out on the other side's ground. 'avoided'
        /// (optional) adds zones treated as hostile as a whole (the mission run's avoid list). The arrival point
        /// is checked when known; when it isn't, whole zones only.
        /// </summary>
        public static bool HostileExit(string pluginDir, Action<string> log, int side, ZoneExit e, Func<int, bool> avoided = null)
        {
            if (e == null) return false;
            if (HostileStart(pluginDir, log, side, e.FromPf, e.A.X, e.A.Z)) return true;
            if (avoided != null && avoided(e.ToPf)) return true;
            return e.Arrival.HasValue ? HostileAt(pluginDir, log, side, e.ToPf, e.Arrival.Value.X, e.Arrival.Value.Z) != null
                                      : HostileAt(pluginDir, log, side, e.ToPf, float.NaN, float.NaN) != null;
        }
    }
}
