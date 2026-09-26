using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using Newtonsoft.Json.Linq;
using SmokeLounge.AOtomation.Messaging.Messages;

namespace AOBuddy
{
    /// <summary>
    /// MISSION RECORDER (config MissionRecord; owner, 2026-09-25: "we need to record all we can"). For the
    /// OmniCell emulator's mission work: every packet of every mission building the bot runs, both ways, raw,
    /// from the zone-in on, plus a readable index - the roll that made it, the building, the mission, and every
    /// mob as first seen (name, level, side, position, room, floor, and how far it moved while in sight), the
    /// doors with their lock flag, and the server's clear % (which gives the building's mob count).
    ///
    /// Files, in Plugins/AOBuddy/missions/records/:
    ///   rec-&lt;instance&gt;-&lt;yyyyMMdd-HHmmss&gt;.pkt   records of [1 byte dir: 0 server, 1 client][8 bytes ms since
    ///                                             the zone-in][4 bytes length][the packet], big-endian, the
    ///                                             packet exactly as on the wire after decompression
    ///   rec-&lt;instance&gt;-&lt;yyyyMMdd-HHmmss&gt;.json  the index
    /// The server only sends what is near the bot, so a clear (every room walked) is close to a full survey.
    /// </summary>
    public sealed class MissionRecorder
    {
        private readonly BotContext _ctx;
        private readonly MissionController _mission;
        private readonly string _dir;
        private readonly Func<string> _missionLine;
        private readonly Func<int> _rollDifficulty;
        private readonly Func<bool> _keepOpen;
        // Out of the building to heal and coming back (MissionRun.HealingOut): the file stays open, outdoor packets
        // are not written, and on the way back in the packets since that zone-in follow on, times still counted
        // from the first zone-in.
        private bool _paused;
        private long _offMs;
        private readonly object _lock = new object();

        // Everything since the last zone-in, so the zone-in and the first burst are in the file even though the
        // building is only known a moment later. Capped: outdoors it is dropped at the next zone-in anyway.
        private readonly List<(bool server, long ms, byte[] p)> _pre = new List<(bool, long, byte[])>();
        private DateTime _zoneAt = DateTime.UtcNow;
        private const int PreCap = 50000;

        private FileStream _fs;
        private string _base;
        private int _instance, _packets;
        private DateTime _openedAt;
        private readonly Dictionary<Identity, JObject> _mobs = new Dictionary<Identity, JObject>();
        private readonly Dictionary<Identity, Vector3> _mobFirst = new Dictionary<Identity, Vector3>();
        private float _lastPct = -1;
        // Read while inside: the zone-out that ends a recording has already reset the controller (first index,
        // 13:56 2026-09-25: building, type and doors came out empty).
        private string _building, _type;
        private bool _completed;
        private JArray _doors = new JArray();
        private double _scanAt;

        public MissionRecorder(BotContext ctx, MissionController mission, string pluginDir, Func<string> missionLine, Func<int> rollDifficulty, Func<bool> keepOpen = null)
        {
            _ctx = ctx; _mission = mission; _missionLine = missionLine; _rollDifficulty = rollDifficulty; _keepOpen = keepOpen;
            _dir = Path.Combine(pluginDir, "missions", "records");
        }

        private static int BE32(byte[] b, int p) => (b[p] << 24) | (b[p + 1] << 16) | (b[p + 2] << 8) | b[p + 3];

        /// <summary>Network thread: every packet, both ways.</summary>
        public void OnPacket(byte[] p, bool server)
        {
            if (p == null || !_ctx.Config.MissionRecord) return;
            lock (_lock)
            {
                bool zoneIn = server && p.Length > 20 && p[2] == 0 && p[3] == 0x0A && BE32(p, 16) == (int)N3MessageType.PlayfieldAnarchyF;
                if (zoneIn)
                {
                    _pre.Clear();
                    _zoneAt = DateTime.UtcNow;
                }
                long ms = (long)(DateTime.UtcNow - _zoneAt).TotalMilliseconds;
                if (_fs != null && !_paused && !zoneIn) { Write(server, ms + _offMs, p); return; }
                if (_pre.Count < PreCap) _pre.Add((server, ms, (byte[])p.Clone()));
            }
        }

        private void Write(bool server, long ms, byte[] p)
        {
            try
            {
                var h = new byte[13];
                h[0] = (byte)(server ? 0 : 1);
                for (int i = 0; i < 8; i++) h[1 + i] = (byte)(ms >> (56 - 8 * i));
                for (int i = 0; i < 4; i++) h[9 + i] = (byte)(p.Length >> (24 - 8 * i));
                _fs.Write(h, 0, h.Length); _fs.Write(p, 0, p.Length);
                _packets++;
            }
            catch (Exception ex) { _ctx.Log("MISSIONREC: write failed: " + ex.Message); CloseFile(); }
        }

        /// <summary>Update thread, every frame.</summary>
        public void Tick(LocalPlayer me)
        {
            bool inside = _mission.InMission && _ctx.Config.MissionRecord;
            if (inside && _fs != null && _mission.Instance != _instance) Close();   // straight into another building
            if (inside && _fs != null && _paused) Resume();
            if (inside && _fs == null) Open();
            if (!inside && _fs != null)
            {
                if (_keepOpen != null && _keepOpen()) { if (!_paused) { _paused = true; _ctx.Log("MISSIONREC: out to heal; keeping the file open."); } return; }
                Close(); return;
            }
            if (_fs == null || me == null) return;
            if (_mission.ClearPct >= 0) _lastPct = _mission.ClearPct;
            _building = _mission.BuildingName ?? _building; _type = _mission.RecordTypeName ?? _type; _completed |= _mission.Completed;
            if (_ctx.Clock.Seconds - _scanAt < 0.5) return;
            _scanAt = _ctx.Clock.Seconds;
            try { _doors = _mission.DoorsJson(); } catch { }
            foreach (var n in DynelManager.Npcs)
            {
                if (n == null) continue;
                var pos = n.Transform.Position;
                if (!_mobs.TryGetValue(n.Identity, out var o))
                {
                    n.TryGetStat(Stat.Level, out int lvl);
                    n.TryGetStat(Stat.Side, out int side);
                    n.TryGetStat(Stat.MaxHealth, out int maxHp);
                    o = new JObject
                    {
                        ["id"] = n.Identity.ToString(), ["name"] = n.Name, ["level"] = lvl, ["side"] = side, ["maxHp"] = maxHp,
                        ["pet"] = n.Owner.HasValue,
                        ["first"] = new JArray(Math.Round(pos.X, 2), Math.Round(pos.Y, 2), Math.Round(pos.Z, 2)),
                        ["firstSeenS"] = Math.Round((DateTime.UtcNow - _openedAt).TotalSeconds, 1),
                        ["room"] = _mission.RoomAt(pos), ["floor"] = _mission.FloorAt(pos),
                        ["distWhenSeen"] = Math.Round(me.DistanceFrom(n), 1), ["moved"] = 0.0,
                    };
                    _mobs[n.Identity] = o; _mobFirst[n.Identity] = pos;
                }
                else
                {
                    double moved = Vector3.Distance(_mobFirst[n.Identity], pos);
                    if (moved > (double)o["moved"]) o["moved"] = Math.Round(moved, 1);
                    if (n.TryGetStat(Stat.Health, out int hp) && hp <= 0 && o["diedS"] == null)
                        o["diedS"] = Math.Round((DateTime.UtcNow - _openedAt).TotalSeconds, 1);
                }
            }
        }

        private void Open()
        {
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(_dir);
                    _instance = _mission.Instance;
                    _base = Path.Combine(_dir, $"rec-{_instance}-{DateTime.Now:yyyyMMdd-HHmmss}");
                    _fs = new FileStream(_base + ".pkt", FileMode.Create, FileAccess.Write, FileShare.Read);
                    _packets = 0; _openedAt = _zoneAt; _mobs.Clear(); _mobFirst.Clear(); _lastPct = -1; _building = null; _type = null; _completed = false; _doors = new JArray();
                    foreach (var e in _pre) Write(e.server, e.ms, e.p);
                    _pre.Clear();
                    _ctx.Log($"MISSIONREC: recording {_mission.BuildingName} instance {_instance} to {Path.GetFileName(_base)}.pkt ({_packets} packets since the zone-in).");
                }
                catch (Exception ex) { _ctx.Log("MISSIONREC: could not start: " + ex.Message); CloseFile(); }
            }
        }

        private void Resume()
        {
            lock (_lock)
            {
                _offMs = (long)(_zoneAt - _openedAt).TotalMilliseconds;
                foreach (var e in _pre) Write(e.server, e.ms + _offMs, e.p);
                _pre.Clear();
                _paused = false;
                _ctx.Log($"MISSIONREC: back in; carrying on in {Path.GetFileName(_base)}.pkt.");
            }
        }

        private void Close()
        {
            _paused = false; _offMs = 0;
            string line = null;
            try { line = _missionLine?.Invoke(); } catch { }
            var c = _ctx.Config;
            var idx = new JObject
            {
                ["instance"] = _instance,
                ["building"] = _building,
                ["missionType"] = _type,
                ["mission"] = line,
                ["roll"] = new JObject
                {
                    ["difficulty"] = _rollDifficulty?.Invoke() ?? 0,
                    ["goodBad"] = c.MissionSliderGoodBad, ["orderChaos"] = c.MissionSliderOrderChaos, ["openHidden"] = c.MissionSliderOpenHidden,
                    ["physicalMystical"] = c.MissionSliderPhysicalMystical, ["headonStealth"] = c.MissionSliderHeadonStealth, ["creditsXp"] = c.MissionSliderCreditsXp,
                },
                ["completed"] = _completed,
                ["clearPct"] = _lastPct,
                ["seconds"] = Math.Round((DateTime.UtcNow - _openedAt).TotalSeconds),
                ["packets"] = _packets,
                ["mobs"] = new JArray(_mobs.Values),
                ["doors"] = _doors,
            };
            // The clear % moves in steps of 100/N: with two or more kills counted, N = the building's mobs.
            lock (_lock) CloseFile();
            try { File.WriteAllText(_base + ".json", idx.ToString()); } catch (Exception ex) { _ctx.Log("MISSIONREC: index not written: " + ex.Message); }
            _ctx.Log($"MISSIONREC: closed {Path.GetFileName(_base)}: {idx["packets"]} packets, {_mobs.Count} NPCs seen, clear {_lastPct:0.#}%.");
        }

        private void CloseFile()
        {
            try { _fs?.Dispose(); } catch { }
            _fs = null;
        }
    }
}
