using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using Newtonsoft.Json.Linq;

namespace AOBuddy
{
    /// <summary>
    /// MOB ATLAS (Algorithman, 2026-09-26: "I want a database for mob spawns and patrol routes"). Outdoors, every
    /// NPC the server sends is followed while in range: where it was first seen (its spawn, when it appears near
    /// us), its level, and its track - a point each time it has moved 1 m. When it dies or is gone for 10 s the
    /// sighting is appended, one JSON line, to Plugins/AOBuddy/mobs/&lt;pf&gt;.jsonl:
    ///   {"t":"2026-09-26T15:02:11Z","pf":790,"id":"SimpleChar:EA7200D","name":"Swift Claw","lvl":41,"side":3,
    ///    "first":[x,y,z],"firstSeenAt":"...","popIn":true,"died":false,"secs":42,"track":[[s,x,y,z],...]}
    /// popIn = it appeared within 60 m of us (a spawn point, not a mob we walked up to). Many sightings of the
    /// same mob name in a zone give its spawn points and patrol routes; aggregation is for the tools.
    /// Mission buildings are left to the mission recorder (their ids and coordinates are per instance).
    /// </summary>
    public sealed class MobAtlas
    {
        private sealed class Seen
        {
            public string Name; public int Lvl, Side; public Vector3 First; public DateTime FirstAt, LastAt;
            public bool PopIn, Died; public Vector3 Last; public JArray Track = new JArray();
        }

        private readonly string _dir;
        private readonly Action<string> _log;
        private readonly Dictionary<Identity, Seen> _seen = new Dictionary<Identity, Seen>();
        private int _pf = -1;
        private DateTime _scanAt = DateTime.MinValue;
        private int _written;
        private const int MaxTrack = 600;
        private const float PopInRange = 60f;

        public MobAtlas(string pluginDir, Action<string> log)
        {
            _dir = Path.Combine(pluginDir, "mobs");
            _log = log;
        }

        public void Tick(LocalPlayer me, bool inMission)
        {
            if (me == null) return;
            var now = DateTime.UtcNow;
            if ((now - _scanAt).TotalSeconds < 1) return;
            _scanAt = now;
            int pf = (int)Playfield.ModelId;
            if (pf != _pf || inMission || pf >= 100000) { FlushAll(); if (pf != _pf) _zoneAt = now; _pf = pf; if (inMission || pf >= 100000) return; }

            var mine = me.Transform.Position;
            var here = new HashSet<Identity>();
            foreach (var n in DynelManager.Npcs)
            {
                if (n == null || n.Owner.HasValue) continue;
                here.Add(n.Identity);
                var p = n.Transform.Position;
                if (!_seen.TryGetValue(n.Identity, out var s))
                {
                    n.TryGetStat(Stat.Level, out int lvl);
                    n.TryGetStat(Stat.Side, out int side);
                    _seen[n.Identity] = s = new Seen
                    {
                        Name = n.Name, Lvl = lvl, Side = side, First = p, Last = p, FirstAt = now, LastAt = now,
                        PopIn = Vector3.Distance(mine, p) < PopInRange && _pf == pf && (now - _zoneAt).TotalSeconds > 5,
                    };
                    s.Track.Add(Pt(0, p));
                }
                s.LastAt = now;
                if (n.TryGetStat(Stat.Health, out int hp) && hp <= 0) { s.Died = true; Flush(n.Identity); continue; }
                if (Vector3.Distance(s.Last, p) >= 1f && s.Track.Count < MaxTrack)
                {
                    s.Last = p;
                    s.Track.Add(Pt((now - s.FirstAt).TotalSeconds, p));
                }
            }
            foreach (var id in _seen.Keys.ToList())
                if (!here.Contains(id) && (now - _seen[id].LastAt).TotalSeconds > 10) Flush(id);
        }

        private DateTime _zoneAt = DateTime.UtcNow;
        /// <summary>A zone change: everything seen so far is written out, and pop-ins wait out the first burst.</summary>
        public void OnZone() { FlushAll(); _zoneAt = DateTime.UtcNow; }

        private static JArray Pt(double secs, Vector3 p) => new JArray(Math.Round(secs, 1), Math.Round(p.X, 1), Math.Round(p.Y, 1), Math.Round(p.Z, 1));

        private void FlushAll() { foreach (var id in _seen.Keys.ToList()) Flush(id); }

        private void Flush(Identity id)
        {
            if (!_seen.TryGetValue(id, out var s)) return;
            _seen.Remove(id);
            if (_pf < 0 || _pf >= 100000) return;
            var o = new JObject
            {
                ["t"] = s.FirstAt.ToString("o"), ["pf"] = _pf, ["id"] = id.ToString(), ["name"] = s.Name, ["lvl"] = s.Lvl, ["side"] = s.Side,
                ["first"] = new JArray(Math.Round(s.First.X, 1), Math.Round(s.First.Y, 1), Math.Round(s.First.Z, 1)),
                ["popIn"] = s.PopIn, ["died"] = s.Died, ["secs"] = Math.Round((s.LastAt - s.FirstAt).TotalSeconds),
                ["track"] = s.Track,
            };
            try
            {
                Directory.CreateDirectory(_dir);
                File.AppendAllText(Path.Combine(_dir, _pf + ".jsonl"), o.ToString(Newtonsoft.Json.Formatting.None) + "\n");
                if (++_written % 200 == 0) _log($"MOBATLAS: {_written} sightings written.");
            }
            catch (Exception ex) { _log("MOBATLAS: write failed: " + ex.Message); }
        }
    }
}
