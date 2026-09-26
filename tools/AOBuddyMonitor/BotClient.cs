using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace AOBuddyMonitor
{
    /// <summary>
    /// The bot's local control API as plain objects (BotApi: /status /nav /log /inventory, POST /command).
    /// Loose JObject parsing on purpose: the bot adds keys as it grows and the monitor should keep
    /// rendering whatever it understands, not fail a whole poll over one missing field. Everything is
    /// fetched synchronously by the poller thread; nothing here touches the UI.
    /// </summary>
    public sealed class BotClient
    {
        private readonly HttpClient _http;
        private readonly string _base;

        public BotClient(MonitorConfig cfg)
        {
            _base = cfg.Base;
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
        }

        // ---- the shapes -----------------------------------------------------------------------------

        public sealed class Status
        {
            public string Behavior, Heartbeat, Zone;
            public int Pf = -1;
            public TaskInfo Task = new TaskInfo();
            public float[] Pos;                       // [x,y,z]
            public float[] Hdg;                       // ground-plane forward [x,z]
            public int HpPct = -1, HpCur, HpMax, NanoPct = -1, NanoCur, NanoMax;
            public int Level, XpInto, XpPctNext = -1;
            public long Credits = -1;
            public int FreeSlots = -1;
            public bool Dead, Resting, InCombat, Casting, InMission;
            public List<Pet> Pets = new List<Pet>();
            public TargetInfo Target;                     // present while he is fighting something
        }

        public sealed class TargetInfo
        {
            public string Name = "?";
            public int HpPct = -1, NanoPct = -1;
            public float Dist;
        }

        public sealed class TaskInfo
        {
            public string Kind = "?", Phase, Text = "", Detail;
        }

        public sealed class Pet
        {
            public string Name, Role;
            public int HpPct = -1;
            public float Dist;
        }

        public sealed class Nav
        {
            public string Time, Zone, Phase;
            public bool Active, InMission;
            public int Pf = -1;
            public float[] Pos, Hdg;
            public Hike Hike;
            public List<float[]> MissionPath;          // the walk inside the building
            public MissionInfo Mission;
            public List<Snap> Snaps = new List<Snap>();
            public List<TrailPoint> Trail = new List<TrailPoint>();
            public List<Npc> Npcs = new List<Npc>();       // the SCFUs around him: mobs on the map
            public string Error;
        }

        public sealed class Npc
        {
            public string Name = "?";
            public float[] Pos;
            public int HpPct = -1;
            public bool Fighting;                          // it is on the bot
            public float Dist;
        }

        public sealed class Hike
        {
            public int FromPf = -1, TargetPf = -1;
            public Exit Exit;
            public List<float[]> Route;
        }

        public sealed class Exit
        {
            public string Kind, Text;
            public int ToPf = -1;
            public float[] A, B;
        }

        public sealed class MissionInfo
        {
            public string Line;
            public int Pf = -1;
            public float[] Door;
            public MissionLayout Layout;       // present while inside a mission building
            public int? Floor;                 // the floor the bot is on (server numbering, can be negative)
            public int[] Floors = new int[0];  // every floor the building has
        }

        /// <summary>The zone-in placement verbatim (mirrors AOBuddyNav.MissionLayout): which pool room went
        /// to which floor/slot/rotation. The monitor recomposes the identical floor plan from its own
        /// GameData with AOBuddyNav.ComposeMission — no geometry travels the API.</summary>
        public sealed class MissionLayout
        {
            public int Instance, PoolPf, Width, Height, WorldHeight;
            public float LandX, LandY, LandZ;
            public List<int[]> Rooms = new List<int[]>();   // [roomIdx, floor, x, z, rot]
        }

        public sealed class Snap
        {
            public DateTime T;
            public int Pf;
            public float Gap;
            public string Phase;
            public float[] Local, Server;
        }

        public sealed class TrailPoint
        {
            public DateTime T;
            public int Pf;
            public bool Inside;
            public float[] P;
        }

        public sealed class Inventory
        {
            public int FreeSlots = -1;
            public List<InvItem> Items = new List<InvItem>();
            public List<Bag> Bags = new List<Bag>();
        }

        public sealed class InvItem
        {
            public int Slot, Ql, Count = 1;
            public string Name;
        }

        public sealed class Bag
        {
            public string Name;
            public bool Known = true;               // false = the bot hasn't opened this bag, contents unknown
            public int Free = -1;
            public List<InvItem> Items = new List<InvItem>();
        }

        // ---- fetches --------------------------------------------------------------------------------

        public Status GetStatus() => ParseStatus(GetJson("/status"));
        public Nav GetNav() => ParseNav(GetJson("/nav"));

        /// <summary>The log ring past "after". Returns (highWater, new lines) — the caller keeps the
        /// high-water seq and asks again; a high-water that went DOWN means the bot restarted.</summary>
        public (int Seq, List<(int Seq, string T, string Line)> Lines) GetLog(int after)
        {
            var o = GetJson("/log?after=" + after);
            var lines = new List<(int, string, string)>();
            if (o == null) return (-1, lines);
            int seq = o.Value<int?>("seq") ?? -1;
            if (o["lines"] is JArray a)
                foreach (var e in a)
                    lines.Add(((int?)e?["seq"] ?? -1, (string)e?["t"] ?? "", (string)e?["line"] ?? ""));
            return (seq, lines);
        }

        public Inventory GetInventory() => ParseInventory(GetJson("/inventory"));

        /// <summary>Run one owner tell; the bot collects its replies for up to ~2.5 s.</summary>
        public List<string> Command(string text)
        {
            var o = PostJson("/command", text);
            var replies = new List<string>();
            if (o?["replies"] is JArray a)
                foreach (var r in a) if (r != null) replies.Add((string)r);
            return replies;
        }

        // ---- plumbing -------------------------------------------------------------------------------

        private JObject GetJson(string path)
        {
            try { return JObject.Parse(_http.GetStringAsync(_base + path).GetAwaiter().GetResult()); }
            catch { return null; }                     // bot down or unparseable: the poller shows "waiting"
        }

        private JObject PostJson(string path, string body)
        {
            try
            {
                var resp = _http.PostAsync(_base + path, new StringContent(body, Encoding.UTF8, "text/plain")).GetAwaiter().GetResult();
                return JObject.Parse(resp.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }
            catch { return null; }
        }

        private static float[] Vec(JToken t) => t is JArray a && a.Count >= 2
            ? new[] { (float?)a[0] ?? 0, a.Count > 1 ? (float?)a[1] ?? 0 : 0, a.Count > 2 ? (float?)a[2] ?? 0 : 0 }
            : null;

        private static List<float[]> VecList(JToken t)
        {
            var list = new List<float[]>();
            if (t is JArray a) foreach (var e in a) { var v = Vec(e); if (v != null) list.Add(v); }
            return list;
        }

        private static Status ParseStatus(JObject o)
        {
            if (o == null) return null;
            var s = new Status
            {
                Behavior = (string)o["behavior"] ?? (string)o["heartbeat"] ?? "",
                Heartbeat = (string)o["heartbeat"] ?? "",
                Zone = (string)o["zone"] ?? "",
                Pf = (int?)o["playfield"] ?? -1,
                Pos = Vec(o["pos"]),
                Hdg = Vec(o["hdg"]),
                Credits = (long?)o["credits"] ?? -1,
                FreeSlots = (int?)o["freeSlots"] ?? -1,
                Dead = (bool?)o["dead"] ?? false,
                Resting = (bool?)o["resting"] ?? false,
                InCombat = (bool?)o["inCombat"] ?? false,
                Casting = (bool?)o["casting"] ?? false,
                InMission = (bool?)o["inMission"] ?? false,
            };
            if (o["task"] is JObject t)
            {
                s.Task.Kind = (string)t["kind"] ?? "?";
                s.Task.Phase = (string)t["phase"];
                s.Task.Text = (string)t["text"] ?? "";
                s.Task.Detail = (string)t["detail"];
            }
            if (o["hp"] is JObject hp)
            {
                s.HpPct = (int?)hp["pct"] ?? -1;
                s.HpCur = (int?)hp["cur"] ?? 0;
                s.HpMax = (int?)hp["max"] ?? 0;
            }
            if (o["nano"] is JObject na)
            {
                s.NanoPct = (int?)na["pct"] ?? -1;
                s.NanoCur = (int?)na["cur"] ?? 0;
                s.NanoMax = (int?)na["max"] ?? 0;
            }
            if (o["xp"] is JObject xp)
            {
                s.Level = (int?)xp["level"] ?? 0;
                s.XpInto = (int?)xp["into"] ?? 0;
                s.XpPctNext = (int?)xp["pctNext"] ?? -1;
            }
            if (o["pets"] is JArray pets)
                foreach (var p in pets.OfType<JObject>())
                    s.Pets.Add(new Pet
                    {
                        Name = (string)p["name"] ?? "?",
                        Role = (string)p["role"] ?? "?",
                        HpPct = (int?)p["hpPct"] ?? -1,
                        Dist = (float?)p["dist"] ?? 0,
                    });
            if (o["target"] is JObject tg)
                s.Target = new TargetInfo
                {
                    Name = (string)tg["name"] ?? "?",
                    HpPct = (int?)tg["hpPct"] ?? -1,
                    NanoPct = (int?)tg["nanoPct"] ?? -1,
                    Dist = (float?)tg["dist"] ?? 0,
                };
            return s;
        }

        private static DateTime Time(string t) => DateTime.TryParse(t, out var d) ? d : DateTime.MinValue;

        private static Nav ParseNav(JObject o)
        {
            if (o == null) return null;
            var n = new Nav
            {
                Time = (string)o["time"] ?? "",
                Phase = (string)o["phase"] ?? "",
                Active = (bool?)o["active"] ?? false,
                Pf = (int?)o["pf"] ?? -1,
                Zone = (string)o["zone"] ?? "",
                InMission = (bool?)o["inMission"] ?? false,
                Pos = Vec(o["pos"]),
                Hdg = Vec(o["hdg"]),
                Error = (string)o["error"],
            };
            if (o["hike"] is JObject h)
            {
                n.Hike = new Hike
                {
                    FromPf = (int?)h["fromPf"] ?? -1,
                    TargetPf = (int?)h["targetPf"] ?? -1,
                    Exit = (h["exit"] is JObject e) ? new Exit
                    {
                        Kind = (string)e["kind"] ?? "",
                        ToPf = (int?)e["toPf"] ?? -1,
                        A = Vec(e["a"]),
                        B = Vec(e["b"]),
                        Text = (string)e["text"] ?? "",
                    } : null,
                    Route = VecList(h["route"]),
                };
            }
            n.MissionPath = VecList(o["missionPath"]);
            if (o["mission"] is JObject m)
            {
                n.Mission = new MissionInfo
                {
                    Line = (string)m["line"] ?? "",
                    Pf = (int?)m["pf"] ?? -1,
                    Door = Vec(m["door"]),
                    Floor = (int?)m["floor"],
                };
                if (m["floors"] is JArray fl) n.Mission.Floors = fl.Select(x => (int?)x ?? 0).ToArray();
                if (m["layout"] is JObject l)
                {
                    var lay = new MissionLayout
                    {
                        Instance = (int?)l["instance"] ?? 0,
                        PoolPf = (int?)l["poolPf"] ?? 0,
                        Width = (int?)l["width"] ?? 0,
                        Height = (int?)l["height"] ?? 0,
                        WorldHeight = (int?)l["worldHeight"] ?? 0,
                    };
                    var land = Vec(l["land"]);
                    if (land != null) { lay.LandX = land[0]; lay.LandY = land[1]; lay.LandZ = land[2]; }
                    if (l["rooms"] is JArray ra)
                        foreach (var r in ra.OfType<JArray>())
                        {
                            var five = new int[5];
                            for (int i = 0; i < 5 && i < r.Count; i++) five[i] = (int?)r[i] ?? 0;
                            lay.Rooms.Add(five);
                        }
                    n.Mission.Layout = lay;
                }
            }
            if (o["snaps"] is JArray snaps)
                foreach (var e in snaps.OfType<JObject>())
                    n.Snaps.Add(new Snap
                    {
                        T = Time((string)e["t"]),
                        Pf = (int?)e["pf"] ?? -1,
                        Gap = (float?)e["gap"] ?? 0,
                        Phase = (string)e["phase"] ?? "",
                        Local = Vec(e["local"]),
                        Server = Vec(e["server"]),
                    });
            if (o["trail"] is JArray trail)
                foreach (var e in trail.OfType<JObject>())
                    n.Trail.Add(new TrailPoint
                    {
                        T = Time((string)e["t"]),
                        Pf = (int?)e["pf"] ?? -1,
                        Inside = (bool?)e["inside"] ?? false,
                        P = Vec(e["p"]),
                    });
            if (o["npcs"] is JArray npcs)
                foreach (var e in npcs.OfType<JObject>())
                    n.Npcs.Add(new Npc
                    {
                        Name = (string)e["name"] ?? "?",
                        Pos = Vec(e["p"]),
                        HpPct = (int?)e["hpPct"] ?? -1,
                        Fighting = (bool?)e["fighting"] ?? false,
                        Dist = (float?)e["dist"] ?? 0,
                    });
            return n;
        }

        private static Inventory ParseInventory(JObject o)
        {
            if (o == null) return null;
            var inv = new Inventory { FreeSlots = (int?)o["freeSlots"] ?? -1 };
            if (o["items"] is JArray items)
                foreach (var i in items.OfType<JObject>())
                    inv.Items.Add(new InvItem { Slot = (int?)i["slot"] ?? 0, Name = (string)i["name"] ?? "?", Ql = (int?)i["ql"] ?? 0, Count = (int?)i["count"] ?? 1 });
            if (o["bags"] is JArray bags)
                foreach (var b in bags.OfType<JObject>())
                {
                    var bag = new Bag { Name = (string)b["name"] ?? "backpack", Known = (bool?)b["known"] ?? true, Free = (int?)b["free"] ?? -1 };
                    if (b["items"] is JArray bi)
                        foreach (var i in bi.OfType<JObject>())
                            bag.Items.Add(new InvItem { Slot = (int?)i["slot"] ?? 0, Name = (string)i["name"] ?? "?", Ql = (int?)i["ql"] ?? 0, Count = (int?)i["count"] ?? 1 });
                    inv.Bags.Add(bag);
                }
            return inv;
        }
    }
}
