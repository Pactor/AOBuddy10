using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AOSharp.Common.GameData;
using Newtonsoft.Json;

namespace AOBuddy
{
    /// <summary>
    /// WHAT WALKING TAUGHT US, for the outdoor planner (owner, 2026-09-26: "the road is 55% longer, unless he gets
    /// snapped back 100 times"). The shortest line kept taking him over hills the server doesn't let him cross
    /// (Borealis -> Holes in the Wall: his 434 m over the hill, the owner's road 673 m, pulled back 10 m twice
    /// at the start), so the grid's cost learns from two things the map can't show:
    ///   * ROADS: every path the owner recorded (paths/*.json, 'record' + 'savepath') is ground a person walked
    ///     cleanly; its cells cost less. The files carry no zone, so a point counts in a zone only where its
    ///     height sits on one of that zone's floors.
    ///   * SNAP-BACKS: every spot the server pulled him back outdoors, kept per zone across restarts
    ///     (snapbacks.json); the cells round it cost more, more for each time it happened.
    /// The grid rebuilds its learned costs whenever Version changes.
    /// </summary>
    public static class LearnedGround
    {
        public sealed class Snap { public float X, Z; public int N; public DateTime Last; }

        private static readonly object Gate = new object();
        private static string _dir;
        private static Dictionary<int, List<Snap>> _snaps = new Dictionary<int, List<Snap>>();
        private static List<List<Vector3>> _roads = new List<List<Vector3>>();
        private static DateTime _roadsAt = DateTime.MinValue;
        public static int Version { get; private set; }

        private const float Merge = 4f;           // snap-backs this close are one spot
        private const int Days = 14;              // a spot not hit again in this long is forgotten

        public static void Init(string pluginDir)
        {
            lock (Gate)
            {
                if (_dir != null) return;
                _dir = pluginDir;
                try
                {
                    string p = Path.Combine(_dir, "snapbacks.json");
                    if (File.Exists(p)) _snaps = JsonConvert.DeserializeObject<Dictionary<int, List<Snap>>>(File.ReadAllText(p)) ?? new Dictionary<int, List<Snap>>();
                    foreach (var k in _snaps.Keys.ToList()) _snaps[k] = _snaps[k].Where(s => (DateTime.UtcNow - s.Last).TotalDays < Days).ToList();
                }
                catch { _snaps = new Dictionary<int, List<Snap>>(); }
                LoadRoads();
                Version++;
            }
        }

        /// <summary>Re-read paths/*.json when one was saved since the last read (cheap: a directory listing).</summary>
        public static void RefreshRoads()
        {
            lock (Gate)
            {
                if (_dir == null) return;
                string d = Path.Combine(_dir, "paths");
                if (!Directory.Exists(d)) return;
                var newest = Directory.GetFiles(d, "*.json").Select(File.GetLastWriteTimeUtc).DefaultIfEmpty(DateTime.MinValue).Max();
                if (newest <= _roadsAt) return;
                LoadRoads();
                Version++;
            }
        }

        private static void LoadRoads()
        {
            _roads = new List<List<Vector3>>();
            string d = Path.Combine(_dir, "paths");
            if (!Directory.Exists(d)) return;
            foreach (var f in Directory.GetFiles(d, "*.json"))
            {
                try
                {
                    var pts = JsonConvert.DeserializeObject<List<float[]>>(File.ReadAllText(f));
                    if (pts != null) _roads.Add(pts.Where(a => a != null && a.Length >= 3).Select(a => new Vector3(a[0], a[1], a[2])).ToList());
                    _roadsAt = new[] { _roadsAt, File.GetLastWriteTimeUtc(f) }.Max();
                }
                catch { }
            }
        }

        /// <summary>A server pull-back outdoors at (x, z) in zone pf.</summary>
        public static void NoteSnap(int pf, float x, float z)
        {
            lock (Gate)
            {
                if (_dir == null) return;
                if (!_snaps.TryGetValue(pf, out var list)) _snaps[pf] = list = new List<Snap>();
                var s = list.FirstOrDefault(q => Math.Abs(q.X - x) < Merge && Math.Abs(q.Z - z) < Merge);
                if (s == null) list.Add(s = new Snap { X = x, Z = z });
                s.N++; s.Last = DateTime.UtcNow;
                Version++;
                try { File.WriteAllText(Path.Combine(_dir, "snapbacks.json"), JsonConvert.SerializeObject(_snaps, Formatting.Indented)); } catch { }
            }
        }

        public static List<Snap> SnapsIn(int pf) { lock (Gate) return _snaps.TryGetValue(pf, out var l) ? l.ToList() : new List<Snap>(); }
        public static List<List<Vector3>> Roads() { lock (Gate) return _roads.Select(r => r.ToList()).ToList(); }
    }
}
