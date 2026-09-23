using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using CharacterActionType = SmokeLounge.AOtomation.Messaging.GameData.CharacterActionType;

namespace AOBuddy
{
    /// <summary>
    /// MISSION MODE - run a rolled mission as a blitz: route through the composed instance, ride the floor
    /// buttons, complete the objective, walk back to the entrance. Off unless the owner says 'mission blitz'.
    /// Plan and evidence: MISSION-MODE-PLAN.md. Every rule here names the capture it came from.
    ///
    /// What it reads (passively, always; cheap):
    ///   * the zone-in packet: the building (AOBuddyNav.LoadMission) and the entrance (landing point)
    ///   * SimpleItemFullUpdate for type 0xC73D items: floor buttons by template, mission items. The server
    ///     sends a floor's buttons when you arrive on that floor, up to 270 m away (captures 20260923-114223,
    ///     -120056, -125821), so the bot never has to explore for them.
    ///   * QuestFullUpdate: the mission's type code and, for the mission holder, its target identity
    ///   * the Level lock: the SDK records the server's SpecialUsed(Level, 10 s) / SpecialAvailable(Level)
    ///     as a cooldown on the local player (Client.SpecialUsedAction); a button press waits for it.
    ///
    /// What it moves: nothing unless a blitz is running. Then Tick() owns the body through Movement.Advance
    /// (the same small run steps follow uses) and Main.Walk() gives it the frame before travel and follow.
    /// It never sends StopAttack, never applies SetPos, and hands the body back the moment it stops.
    /// </summary>
    public class MissionController
    {
        private readonly BotContext _ctx;
        private readonly Movement _move;
        private readonly string _pluginDir;
        private readonly Action<string> _tell;

        // ---- per-instance knowledge ---------------------------------------------------------------------
        private AOBuddyNav _nav;                 // the composed instance (null outside a mission)
        private MissionGrid _grid;
        private int _instance;                   // BuildingGeneratorData instance == the mission's building id
        private readonly Dictionary<Identity, SeenItem> _items = new Dictionary<Identity, SeenItem>();
        private MissionRecord _record;           // the quest record for this building, from QuestFullUpdate
        private byte[] _lastQuestUpdate;         // raw, re-parsed when the zone-in names a new building
        private readonly List<string> _rewards = new List<string>();

        // ---- blitz state ----------------------------------------------------------------------------------
        private enum Phase { Off, Plan, Walk, PressButton, AwaitTeleport, Act, AwaitComplete, Exit, Done }
        private Phase _phase = Phase.Off;
        private bool _completed;                 // the objective is done; the rest is the walk out
        private bool _announced;                 // the owner has been told it is done
        private string _why = "";
        private List<Vector3> _path;
        private int _pathIndex;
        private Identity? _pendingButton;
        private Vector3 _pressedFrom;
        private double _phaseTime;               // seconds in the current phase
        private double _stuckTime;
        private float _bestDist;
        private int _replans, _presses, _acts;
        private readonly HashSet<(int, int, int)> _blocked = new HashSet<(int, int, int)>();
        private Purpose _purpose;
        private enum Purpose { Button, Target, Entrance, Search }
        private readonly HashSet<int> _visited = new HashSet<int>();   // rooms searched for an unseen target
        private double _lookAccum;

        // Floor button templates, from the client's item table (itemnames 159862-159869, 2026-09-23).
        public const int ButtonDown = 159863, ButtonBoss = 159864, ButtonUp = 159869;
        private static bool IsPlatformOrButton(int tpl) => tpl >= 159862 && tpl <= 159869;
        private const int IdentityTypeItem = 0xC73D;       // SimpleItem statics: buttons, mission items
        private const int IdentityTypeChar = 0xC350;
        private const int MissionChangedAction = 0x3B;     // CharacterAction the holder gets on completion

        // Mission type codes, the same the terminal list uses (ClickSaver MissionTypes.FromCode).
        public const int TypeRepair = 0x2C4E, TypeReturnItem = 0x2C41, TypeFindPerson = 0x2C47, TypeFindItem = 0x2C49, TypeKillPerson = 0x2C42;

        public bool Active => _phase != Phase.Off && _phase != Phase.Done;
        public bool InMission => _grid != null;

        public MissionController(BotContext ctx, Movement move, string pluginDir, Action<string> tell)
        {
            _ctx = ctx; _move = move; _pluginDir = pluginDir; _tell = tell;
        }

        private sealed class SeenItem
        {
            public int Template;
            public Vector3 Pos;
            public double Seen;
        }

        public sealed class MissionRecord
        {
            public int Type;
            public Identity? TargetA, TargetB;   // find item/person: A is the target; repair: A tool, B object
            public int Building;
            public string Text = "";             // every readable string in the quest update, for name matching
            public string TypeName => TypeNames.TryGetValue(Type, out var n) ? n : "0x" + Type.ToString("X");
        }

        private static readonly Dictionary<int, string> TypeNames = new Dictionary<int, string>
        {
            { TypeRepair, "repair" }, { TypeReturnItem, "return item" }, { TypeFindPerson, "find person" },
            { TypeFindItem, "find item" }, { TypeKillPerson, "kill person" },
        };

        private static double Now => Environment.TickCount64 / 1000.0;

        // =====================================================================================================
        // Wire feed
        // =====================================================================================================

        public void OnMessage(Message m)
        {
            if (m?.Body == null) return;
            switch (m.Body)
            {
                case PlayfieldAnarchyFMessage _:
                    OnZoneIn(m.RawPacket);
                    break;

                case SimpleItemFullUpdateMessage sifu:
                    if ((int)sifu.Identity.Type != IdentityTypeItem || !sifu.Position.HasValue || sifu.Stats == null) break;
                    int tpl = 0;
                    foreach (var st in sifu.Stats) if (st.Value1 == Stat.ACGItemTemplateID) { tpl = st.Value2; break; }
                    _items[sifu.Identity] = new SeenItem { Template = tpl, Pos = sifu.Position.Value, Seen = Now };
                    break;

                case QuestFullUpdateMessage _:
                    if (m.RawPacket != null) { _lastQuestUpdate = m.RawPacket; ParseRecord(); }
                    break;

                case CharacterActionMessage ca:
                    if ((int)ca.Action == MissionChangedAction && IsMe(ca.Identity)) OnCompleted("MissionChanged");
                    break;

                case QuestMessage qm:
                    // A team member gets only the quest removal, no MissionChanged (capture 20260923-114223 s4
                    // 11:50:35.787). Count it only once we have done the objective, so an unrelated quest
                    // message mid-run cannot end the blitz early.
                    if (IsMe(qm.Identity) && _grid != null && (_phase == Phase.AwaitComplete || _phase == Phase.Act || !Active))
                        OnCompleted($"quest {qm.Mission} removed");
                    break;

                case TemplateActionMessage ta:
                    // A reward lands in the overflow window as TemplateAction 87 right before the completion
                    // messages (captures 20260910-203534 seq 19745, 20260923-114223 seq 4727/4737).
                    if (ta.Action == 87 && IsMe(ta.Identity) && _grid != null)
                        _rewards.Add(ItemName(ta.ItemLowId) + (ta.Quality > 0 ? " QL" + ta.Quality : ""));
                    break;
            }
        }

        private static bool IsMe(Identity id)
        {
            var me = DynelManager.LocalPlayer;
            return me != null && id.Instance == me.Identity.Instance;
        }

        private void OnZoneIn(byte[] raw)
        {
            if (Active) Stop("zoned");
            _items.Clear(); _record = null; _grid = null; _nav = null; _instance = 0; _blocked.Clear(); _visited.Clear();
            try
            {
                _nav = raw == null ? null : AOBuddyNav.LoadMission(_pluginDir, raw);
                if (_nav?.Layout == null) { _nav = null; return; }
                _instance = _nav.Layout.Instance;
                _grid = MissionGrid.Build(_nav);
                ParseRecord();
                _ctx.Log($"MISSION: in {_nav.Name} instance {_instance}, {_grid.Describe()}");
                if (_grid.MappingErrors > 0) _ctx.Log($"MISSION: WARNING {_grid.MappingErrors} room cells did not map back to themselves");
            }
            catch (Exception ex) { _ctx.Log("MISSION: could not compose the instance: " + ex.Message); _grid = null; _nav = null; }
        }

        private void ParseRecord()
        {
            var me = DynelManager.LocalPlayer;
            if (_lastQuestUpdate == null || me == null) return;
            var all = MissionRecords.Parse(_lastQuestUpdate, me.Identity.Instance);
            _record = all.FirstOrDefault(r => _instance != 0 && r.Building == _instance);
            if (_record != null)
                _ctx.Log($"MISSION: record {_record.TypeName} building {_record.Building} target {(_record.TargetA?.ToString() ?? "none (not the holder)")}{(_record.TargetB.HasValue ? " object " + _record.TargetB : "")}");
        }

        // =====================================================================================================
        // Commands
        // =====================================================================================================

        public void Command(string args, Action<string> reply)
        {
            string sub = (args ?? "").Trim().ToLowerInvariant();
            switch (sub)
            {
                case "":
                case "status": reply(Status()); break;
                case "route": reply(RouteReport()); break;
                case "blitz":
                    if (_grid == null) { reply("Not in a mission building."); break; }
                    Start(reply);
                    break;
                case "stop": Stop("owner said stop"); reply("Mission mode off."); break;
                default: reply("mission status | route | blitz | stop"); break;
            }
        }

        private string Status()
        {
            var me = DynelManager.LocalPlayer;
            if (_grid == null) return "Not in a mission building.";
            var sb = new StringBuilder();
            var pos = me?.MovementComponent.Position ?? new Vector3(0, 0, 0);
            int? f = _grid.FloorAt(pos);
            sb.Append($"{_nav.Name} ({_instance}): floors {string.Join(",", _grid.Floors)}, boss room ");
            sb.Append(_grid.BossFloor.HasValue ? $"'{_grid.BossRoomName}' on floor {_grid.BossFloor}" : "not in the layout");
            sb.Append($". Me: floor {(f.HasValue ? f.ToString() : "?")} room '{_grid.RoomAt(pos) ?? "?"}'. Buttons seen: ");
            var btns = Buttons().Select(b => $"{KindName(b.Value.Template)}@f{_grid.FloorAt(b.Value.Pos)?.ToString() ?? "?"}").ToList();
            sb.Append(btns.Count == 0 ? "none" : string.Join(" ", btns));
            sb.Append(". Mission: ");
            sb.Append(_record == null ? "no quest record for this building" : _record.TypeName + (_record.TargetA.HasValue ? " target " + _record.TargetA : " (target by name)"));
            if (me != null) sb.Append($". Level lock: {(me.IsSpecialReady(Stat.Level) ? "free" : "locked")}");
            if (Active) sb.Append($". Blitz: {_phase} ({_why})");
            return sb.ToString();
        }

        private string RouteReport()
        {
            var me = DynelManager.LocalPlayer;
            if (_grid == null || me == null) return "Not in a mission building.";
            var hop = NextHop(me.MovementComponent.Position, out string why);
            if (hop == null) return "No route: " + why;
            var path = _grid.FindPath(me.MovementComponent.Position, hop.Value.Pos, _blocked, out bool usedFallback);
            if (path == null) return $"Next: {why}, but no walkable path to it.";
            var rooms = new List<string>();
            foreach (var p in path) { string r = _grid.RoomAt(p); if (r != null && (rooms.Count == 0 || rooms[rooms.Count - 1] != r)) rooms.Add(r); }
            float len = 0; for (int i = 1; i < path.Count; i++) len += Vector3.Distance(path[i - 1], path[i]);
            return $"Next: {why}. {len:0} m through {string.Join(" > ", rooms)}{(usedFallback ? " (through an unmarked doorway)" : "")}.";
        }

        // =====================================================================================================
        // Blitz
        // =====================================================================================================

        private void Start(Action<string> reply)
        {
            _completed = false; _announced = false; _rewards.Clear(); _replans = 0; _presses = 0; _acts = 0; _blocked.Clear(); _visited.Clear();
            Enter(Phase.Plan, "starting");
            string target = _record == null ? "no quest record yet" : _record.TypeName;
            reply($"Blitz on: {target}. Floors {string.Join(",", _grid.Floors)}, boss room on floor {(_grid.BossFloor?.ToString() ?? "?")}. Send 'mission stop' to cancel.");
        }

        public void Stop(string why)
        {
            if (_phase == Phase.Off) return;
            var me = DynelManager.LocalPlayer;
            if (me != null) _move.Stop(me, _ctx.Config.SendIntervalMs);
            _ctx.Log($"MISSION: blitz stopped ({why}) in phase {_phase}.");
            _phase = Phase.Off; _path = null; _pendingButton = null;
        }

        private void Enter(Phase p, string why)
        {
            if (p != _phase || why != _why) _ctx.Log($"MISSION: {_phase} -> {p}: {why}");
            _phase = p; _why = why; _phaseTime = 0; _stuckTime = 0; _bestDist = float.MaxValue;
        }

        private void Fail(string why)
        {
            _ctx.Log("MISSION: giving up - " + why);
            _tell("Mission blitz stopped: " + why);
            Stop(why);
        }

        private void OnCompleted(string how)
        {
            if (_completed || _grid == null) return;
            _completed = true;
            _ctx.Log($"MISSION: completed ({how}).");
            if (Active)
            {
                // Rewards arrive in the same burst as the completion; give the burst a moment in Tick.
                Enter(Phase.Exit, "objective done, heading back to the entrance");
                _path = null;
            }
        }

        /// <summary>One frame. Returns true when mission mode owns the body this frame.</summary>
        public bool Tick(LocalPlayer me, double dt)
        {
            if (!Active) return false;
            if (_grid == null) { Stop("left the mission building"); return false; }
            _phaseTime += dt;
            Vector3 pos = me.MovementComponent.Position;

            switch (_phase)
            {
                case Phase.Plan:
                case Phase.Exit:
                    Hold(me);
                    if (_phase == Phase.Exit && !_announced)
                    {
                        if (_phaseTime < 1.5) return true;   // let the reward burst land
                        _tell("Mission complete." + (_rewards.Count > 0 ? " Rewards: " + string.Join(", ", _rewards) + "." : "") + " Heading back to the entrance.");
                        _rewards.Clear(); _announced = true; _phaseTime = 0;
                    }
                    PlanNext(me);
                    return true;

                case Phase.Walk:
                    return WalkTick(me, dt);

                case Phase.PressButton:
                {
                    Hold(me);
                    if (!me.IsSpecialReady(Stat.Level))
                    {
                        _ctx.WalkState = "mission: waiting for the Level lock";
                        if (_phaseTime > 20) Fail("the Level lock never cleared");
                        return true;
                    }
                    if (_pendingButton == null) { Enter(Phase.Plan, "no button"); return true; }
                    _presses++;
                    if (_presses > 12) { Fail("pressed buttons 12 times without getting anywhere"); return true; }
                    _pressedFrom = pos;
                    Client.Send(new GenericCmdMessage
                    {
                        Action = GenericCmdAction.Use,
                        User = me.Identity,
                        Target = _pendingButton.Value,
                        Count = 1,
                        Temp4 = 1,
                    });
                    _ctx.Log($"MISSION: pressed {KindName(_items.TryGetValue(_pendingButton.Value, out var it) ? it.Template : 0)} button {_pendingButton.Value}.");
                    Enter(Phase.AwaitTeleport, "pressed the button");
                    return true;
                }

                case Phase.AwaitTeleport:
                    Hold(me);
                    if (Vector3.Distance(pos, _pressedFrom) > 10f)
                    {
                        _ctx.Log($"MISSION: rode to ({pos.X:0},{pos.Y:0},{pos.Z:0}), floor {_grid.FloorAt(pos)?.ToString() ?? "?"}.");
                        _pendingButton = null; _path = null;
                        Enter(_completed ? Phase.Exit : Phase.Plan, "arrived");
                        return true;
                    }
                    if (_phaseTime > 4) Enter(Phase.PressButton, "no teleport after 4 s, pressing again");
                    return true;

                case Phase.Act:
                    Hold(me);
                    DoObjective(me);
                    return true;

                case Phase.AwaitComplete:
                    Hold(me);
                    if (_completed) return true;
                    if (_phaseTime > 5)
                    {
                        if (_acts >= 3) { Fail("the objective did not complete after 3 tries"); return true; }
                        Enter(Phase.Act, "no completion after 5 s, trying again");
                    }
                    return true;
            }
            return false;
        }

        private void Hold(LocalPlayer me)
        {
            if (_move.Moving) _move.Stop(me, _ctx.Config.SendIntervalMs);
        }

        // ---- planning -------------------------------------------------------------------------------------

        private struct Hop
        {
            public Vector3 Pos;
            public Purpose Purpose;
            public Identity? Button;
            public Identity? Target;
        }

        private void PlanNext(LocalPlayer me)
        {
            var hop = NextHop(me.MovementComponent.Position, out string why);
            if (hop == null)
            {
                // A floor's buttons arrive with the ride that brings us there; give them a moment.
                if (_phaseTime < 6) { _ctx.WalkState = "mission: " + why; return; }
                Fail(why);
                return;
            }
            var h = hop.Value;
            _path = _grid.FindPath(me.MovementComponent.Position, h.Pos, _blocked, out bool fb);
            if (_path == null)
            {
                if (_phaseTime < 6) return;
                Fail($"{why}, but there is no walkable path to it");
                return;
            }
            _pathIndex = 0; _purpose = h.Purpose; _pendingButton = h.Button;
            float len = 0; for (int i = 1; i < _path.Count; i++) len += Vector3.Distance(_path[i - 1], _path[i]);
            Enter(Phase.Walk, $"{why}: {len:0} m, {_path.Count} points{(fb ? ", through an unmarked doorway" : "")}");
        }

        private Hop? NextHop(Vector3 pos, out string why)
        {
            int? myFloor = _grid.FloorAt(pos);
            if (!myFloor.HasValue) { why = "I am not on any floor of this building"; return null; }

            int goalFloor;
            Vector3? goalPos = null;
            Identity? target = null;
            if (_completed)
            {
                var land = new Vector3(_nav.Layout.LandX, _nav.Layout.LandY, _nav.Layout.LandZ);
                int? lf = _grid.FloorAt(land);
                if (!lf.HasValue) { why = "the entrance is not on any floor"; return null; }
                goalFloor = lf.Value; goalPos = land;
            }
            else
            {
                target = FindTarget(out Vector3? tpos, out string tw);
                if (target.HasValue && tpos.HasValue && _grid.FloorAt(tpos.Value) is int tf)
                {
                    goalFloor = tf; goalPos = tpos;
                }
                else if (_grid.BossFloor.HasValue && myFloor != _grid.BossFloor)
                {
                    goalFloor = _grid.BossFloor.Value;
                }
                else
                {
                    // In the boss room with the target not yet sent, or a building with no boss room: the
                    // server sends a target once we are near it (the repair chamber of capture 20260910-203534
                    // arrived at 88 m; the boss-room targets of 2026-09-23 arrived with the ride in). Search
                    // the rooms of this floor, nearest first, until it shows up.
                    return SearchHop(pos, myFloor.Value, tw, out why);
                }
            }

            if (myFloor == goalFloor && goalPos.HasValue)
            {
                why = _completed ? "walking to the entrance" : "walking to the target";
                return new Hop { Pos = goalPos.Value, Purpose = _completed ? Purpose.Entrance : Purpose.Target, Target = target };
            }

            // Another floor: pick a button on this one. Button (boss) joins the boss room to the floor next to
            // it; up/down join the ordinary floors (all three runs 2026-09-23).
            var here = Buttons().Where(b => _grid.FloorAt(b.Value.Pos) == myFloor).ToList();
            int want;
            if (myFloor == _grid.BossFloor) want = 0;                                  // the one way out
            else if (goalFloor == _grid.BossFloor && here.Any(b => b.Value.Template == ButtonBoss)) want = ButtonBoss;
            else want = goalFloor > myFloor ? ButtonUp : ButtonDown;

            var choices = here.Where(b => want == 0 || b.Value.Template == want).ToList();
            if (choices.Count == 0)
            {
                why = $"no {(want == 0 ? "" : KindName(want) + " ")}button seen on floor {myFloor} yet (want floor {goalFloor})";
                return null;
            }
            // Nearest by path.
            Hop? best = null; float bestLen = float.MaxValue;
            foreach (var b in choices)
            {
                var p = _grid.FindPath(pos, b.Value.Pos, _blocked, out _);
                if (p == null) continue;
                float len = 0; for (int i = 1; i < p.Count; i++) len += Vector3.Distance(p[i - 1], p[i]);
                if (len < bestLen) { bestLen = len; best = new Hop { Pos = b.Value.Pos, Purpose = Purpose.Button, Button = b.Key }; }
            }
            if (best == null) { why = $"a {KindName(want)} button is on floor {myFloor} but I cannot reach it"; return null; }
            why = $"to the {KindName(_items[best.Value.Button.Value].Template)} button (floor {myFloor} -> {goalFloor})";
            return best;
        }

        private Hop? SearchHop(Vector3 pos, int floor, string tw, out string why)
        {
            foreach (var r in _grid.RoomsOn(floor)) if (Flat(pos, r.Centre) < 6f) _visited.Add(r.Index);
            Hop? best = null; float bestLen = float.MaxValue; string bestName = null;
            foreach (var r in _grid.RoomsOn(floor))
            {
                if (_visited.Contains(r.Index)) continue;
                var p = _grid.FindPath(pos, r.Centre, _blocked, out _);
                if (p == null) { _visited.Add(r.Index); continue; }
                float len = 0; for (int i = 1; i < p.Count; i++) len += Vector3.Distance(p[i - 1], p[i]);
                if (len < bestLen) { bestLen = len; bestName = r.Name; best = new Hop { Pos = r.Centre, Purpose = Purpose.Search }; }
            }
            if (best == null) { why = $"searched every room on floor {floor} and the target never showed ({tw})"; return null; }
            why = $"searching: target not in sight yet ({tw}), trying room '{bestName}'";
            return best;
        }

        // ---- offline test hooks (the harness replays captures through the planner; nothing calls these live)
        public void TestLoad(byte[] zoneIn, MissionRecord record) { OnZoneIn(zoneIn); _record = record; }
        public void TestSeeItem(Identity id, int template, Vector3 pos) => _items[id] = new SeenItem { Template = template, Pos = pos, Seen = Now };
        public void TestSetCompleted(bool done) => _completed = done;
        public string TestNextHop(Vector3 pos, out Identity? button, out Vector3? goal)
        {
            var h = NextHop(pos, out string why);
            button = h?.Button; goal = h?.Pos;
            return why;
        }

        private IEnumerable<KeyValuePair<Identity, SeenItem>> Buttons() =>
            _items.Where(kv => kv.Value.Template == ButtonDown || kv.Value.Template == ButtonUp || kv.Value.Template == ButtonBoss);

        private static string KindName(int tpl) => tpl == ButtonDown ? "down" : tpl == ButtonUp ? "up" : tpl == ButtonBoss ? "boss" : "any";

        // ---- the objective --------------------------------------------------------------------------------

        /// <summary>
        /// The mission target: the identity in the holder's quest record when we hold the mission (all three
        /// 2026-09-23 runs); otherwise, as a team member whose record carries no target (capture -114223 s4),
        /// the NPC or item whose name is in the mission text.
        /// </summary>
        private Identity? FindTarget(out Vector3? pos, out string how)
        {
            pos = null;
            if (_record == null) { how = "no quest record for this building"; return null; }
            int type = _record.Type;
            if (type == TypeRepair) { var t = _record.TargetB; how = "the repair object"; return Locate(t, out pos) ? t : null; }
            if (type != TypeFindPerson && type != TypeFindItem) { how = _record.TypeName + " is not supported yet"; return null; }

            if (_record.TargetA.HasValue)
            {
                how = "the quest record's target";
                return Locate(_record.TargetA, out pos) ? _record.TargetA : null;
            }

            string text = _record.Text;
            if (type == TypeFindPerson)
            {
                foreach (var npc in DynelManager.Npcs)
                {
                    if (string.IsNullOrEmpty(npc.Name) || npc.Name.Length < 4 || npc.Owner.HasValue) continue;
                    if (text.IndexOf(npc.Name, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    how = $"'{npc.Name}', named in the mission text";
                    pos = npc.Transform.Position;
                    return npc.Identity;
                }
                how = "nobody in sight is named in the mission text";
                return null;
            }

            // Find item as a team member: the item whose name is in the text, else the only candidate left.
            var candidates = _items.Where(kv => !IsPlatformOrButton(kv.Value.Template) && !MissionRecords.Mentions(_lastQuestUpdate, kv.Key)).ToList();
            foreach (var kv in candidates)
            {
                string name = ItemName(kv.Value.Template);
                if (name.Length >= 4 && text.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    how = $"'{name}', named in the mission text"; pos = kv.Value.Pos; return kv.Key;
                }
            }
            how = "no item in sight is named in the mission text";
            return null;
        }

        private bool Locate(Identity? id, out Vector3? pos)
        {
            pos = null;
            if (!id.HasValue) return false;
            if ((int)id.Value.Type == IdentityTypeItem && _items.TryGetValue(id.Value, out var it)) { pos = it.Pos; return true; }
            if (DynelManager.Find(id.Value, out SimpleChar c)) { pos = c.Transform.Position; return true; }
            return false;
        }

        private void DoObjective(LocalPlayer me)
        {
            var target = FindTarget(out _, out string how);
            if (!target.HasValue) { Enter(Phase.Plan, "lost sight of the target (" + how + ")"); return; }
            _acts++;
            switch (_record.Type)
            {
                case TypeFindPerson:
                    // What the client sent: InfoRequest, then LookAt with ReturnInfo=1 (capture 20260923-114223
                    // s5 11:50:35.558); the mission completed 0.47 s later.
                    Client.InfoRequest(target.Value);
                    Client.Send(new LookAtMessage { Target = target.Value, ReturnInfo = 1 });
                    _ctx.Log($"MISSION: selected {target.Value} ({how}).");
                    break;
                case TypeFindItem:
                    // One LookAt with ReturnInfo=0 on the floor item (capture 20260923-125821 s4 13:04:11.152).
                    Client.Send(new LookAtMessage { Target = target.Value, ReturnInfo = 0 });
                    _ctx.Log($"MISSION: selected item {target.Value} ({how}).");
                    break;
                case TypeRepair:
                {
                    // GenericCmd UseItemOnItem: the tool from the inventory, then the object (capture
                    // 20260910-203534 client seq 1292). The quest record lists the tool first.
                    int toolTpl = _record.TargetA.HasValue && _items.TryGetValue(_record.TargetA.Value, out var tool) ? tool.Template : 0;
                    var inv = toolTpl == 0 ? null : Inventory.Items.FirstOrDefault(i => i.Id == toolTpl || i.HighId == toolTpl);
                    if (inv == null) { Fail("the repair tool is not in my inventory"); return; }
                    Client.Send(new GenericCmdMessage
                    {
                        Action = GenericCmdAction.UseItemOnItem,
                        User = me.Identity,
                        Source = inv.Slot,
                        Target = target.Value,
                        Count = 1,
                        Temp4 = 1,
                    });
                    _ctx.Log($"MISSION: used {inv.Name} ({inv.Slot}) on {target.Value}.");
                    break;
                }
                default:
                    Fail(_record.TypeName + " is not supported yet");
                    return;
            }
            Enter(Phase.AwaitComplete, "objective sent");
        }

        // ---- walking --------------------------------------------------------------------------------------

        private bool WalkTick(LocalPlayer me, double dt)
        {
            if (_path == null || _pathIndex >= _path.Count) { Arrive(me); return true; }
            Vector3 pos = me.MovementComponent.Position;

            // Knocked off the path (a server correction, a ride we did not ask for): plan again.
            if (Vector3.Distance(pos, _path[Math.Min(_pathIndex, _path.Count - 1)]) > 12f)
            {
                _path = null;
                Enter(_completed ? Phase.Exit : Phase.Plan, "off the path, planning again");
                return true;
            }

            if (_purpose == Purpose.Search && (_lookAccum += dt) >= 1.0)
            {
                _lookAccum = 0;
                if (FindTarget(out Vector3? tp, out _).HasValue && tp.HasValue) { _path = null; Enter(Phase.Plan, "the target came into sight"); return true; }
            }

            Vector3 wp = _path[_pathIndex];
            float d = Flat(pos, wp);
            bool last = _pathIndex == _path.Count - 1;
            float arrive = last ? ArriveRadius() : 0.8f;
            if (d <= arrive)
            {
                _pathIndex++;
                _bestDist = float.MaxValue; _stuckTime = 0;
                if (_pathIndex >= _path.Count) { Arrive(me); return true; }
                wp = _path[_pathIndex]; d = Flat(pos, wp);
            }

            // Stuck: no progress on this waypoint for 3 s means something the tiles do not show is in the
            // way. Block that cell and plan around it.
            if (d < _bestDist - 0.3f) { _bestDist = d; _stuckTime = 0; }
            else _stuckTime += dt;
            if (_stuckTime > 3.0)
            {
                _replans++;
                var cell = _grid.CellOf(wp);
                if (cell.HasValue) _blocked.Add(cell.Value);
                _ctx.Log($"MISSION: no progress toward ({wp.X:0},{wp.Y:0},{wp.Z:0}) for 3 s, blocking that cell (replan {_replans}).");
                if (_replans > 6) { Fail("kept getting stuck"); return true; }
                _path = null;
                Enter(_completed ? Phase.Exit : Phase.Plan, "stuck, planning around it");
                return true;
            }

            Vector3 dir = new Vector3(wp.X - pos.X, 0, wp.Z - pos.Z).Normalize();
            float step = Math.Min((float)(MoveSpeed(me) * dt), _ctx.Config.MaxStep);
            step = Math.Min(step, d);
            Vector3 next = new Vector3(pos.X + dir.X * step, pos.Y, pos.Z + dir.Z * step);
            float? y = _grid.HeightAt(next, pos.Y);
            next = new Vector3(next.X, y ?? wp.Y, next.Z);
            _ctx.WalkState = $"mission({_purpose}) wp {_pathIndex + 1}/{_path.Count} d={d:0.0}";
            _move.Advance(me, next, Movement.SafeLook(dir, me.MovementComponent.Heading), run: true, dt, _ctx.Config.SendIntervalMs);
            return true;
        }

        private float ArriveRadius() => _purpose == Purpose.Button ? 1.5f : _purpose == Purpose.Target ? 2.5f : _purpose == Purpose.Search ? 3f : 2.0f;

        private void Arrive(LocalPlayer me)
        {
            Hold(me);
            _path = null;
            switch (_purpose)
            {
                case Purpose.Button: Enter(Phase.PressButton, "at the button"); break;
                case Purpose.Target: Enter(Phase.Act, "at the target"); break;
                case Purpose.Search: Enter(Phase.Plan, "searched a room"); break;
                case Purpose.Entrance:
                    _tell("Back at the mission entrance. Mission mode off.");
                    _ctx.Log("MISSION: back at the entrance, blitz done.");
                    _phase = Phase.Done;
                    break;
            }
        }

        private static float Flat(Vector3 a, Vector3 b) { float dx = a.X - b.X, dz = a.Z - b.Z; return (float)Math.Sqrt(dx * dx + dz * dz); }

        // The client's own run speed (FollowController.MoveSpeed, CLAUDE.md "MOVE SPEED").
        private float MoveSpeed(LocalPlayer me)
        {
            if (me.TryGetStat(Stat.RunSpeed, out int rs) && rs >= 0) return Math.Min(15.5f, 5.5f + rs / 230f);
            return _ctx.Config.FollowSpeed;
        }

        private static string ItemName(int template)
        {
            try { if (template > 0 && ItemData.Find(template, out DummyItem it) && !string.IsNullOrEmpty(it.Name)) return it.Name; }
            catch { }
            return "item " + template;
        }
    }

    /// <summary>
    /// The mission records inside a raw QuestFullUpdate. Read raw because the SDK's Quest class does not
    /// match the wire. Layout, from four captures (20260910-203534, 20260923-114223, -120056, -125821):
    ///   Identity(0xC350, holder) i32 type(0x2Cxx) i32 0xB40 i32 0xB40 i32 0x7E2 i32 n  then n-dependent slots:
    ///     find item   n=15: Identity(0xC73D item)
    ///     find person n=16: 16 bytes of 0, Identity(0xC350 npc)
    ///     repair      n=8:  Identity(0xC73D tool) Identity(0xC73D object)
    ///     a teammate's copy n=0: no identities at all
    ///   ... about 230 bytes on, Identity(0xC79F, building instance) - the same id as the zone-in packet's.
    /// </summary>
    public static class MissionRecords
    {
        public static List<MissionController.MissionRecord> Parse(byte[] b, int holder)
        {
            var list = new List<MissionController.MissionRecord>();
            string text = Strings(b);
            for (int i = 0; i + 40 <= b.Length; i++)
            {
                if (I32(b, i) != 0xC350 || I32(b, i + 4) != holder) continue;
                int type = I32(b, i + 8);
                if ((type & 0xFF00) != 0x2C00 || I32(b, i + 12) != 0xB40 || I32(b, i + 16) != 0xB40) continue;
                var r = new MissionController.MissionRecord { Type = type, Text = text };
                var ids = new List<Identity>();
                for (int j = i + 24; j + 8 <= b.Length && j < i + 24 + 56; j += 4)
                {
                    int t = I32(b, j), inst = I32(b, j + 4);
                    if ((t == 0xC73D || t == 0xC350) && inst != 0 && inst != holder) { ids.Add(new Identity((IdentityType)t, inst)); j += 4; }
                }
                if (ids.Count > 0) r.TargetA = ids[0];
                if (ids.Count > 1) r.TargetB = ids[1];
                // The building: the first Identity(0xC79F, x) after the record and before the next record.
                for (int j = i + 24; j + 8 <= b.Length && j < i + 400; j += 1)
                {
                    if (I32(b, j) == 0xC350 && I32(b, j + 4) == holder && (I32(b, j + 8) & 0xFF00) == 0x2C00 && j > i) break;
                    if (I32(b, j) == 0xC79F) { r.Building = I32(b, j + 4); break; }
                }
                list.Add(r);
            }
            return list;
        }

        /// <summary>True when an identity appears anywhere in the quest update (another mission's item).</summary>
        public static bool Mentions(byte[] b, Identity id)
        {
            if (b == null) return false;
            for (int i = 0; i + 8 <= b.Length; i++)
                if (I32(b, i) == (int)id.Type && I32(b, i + 4) == id.Instance) return true;
            return false;
        }

        private static int I32(byte[] b, int p) => (b[p] << 24) | (b[p + 1] << 16) | (b[p + 2] << 8) | b[p + 3];

        private static string Strings(byte[] b)
        {
            var sb = new StringBuilder(); int run = 0; int start = 0;
            for (int i = 0; i <= b.Length; i++)
            {
                bool ok = i < b.Length && b[i] >= 32 && b[i] < 127;
                if (ok) { if (run == 0) start = i; run++; continue; }
                if (run >= 6) sb.Append(Encoding.ASCII.GetString(b, start, run)).Append('\n');
                run = 0;
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// The walkable grid of a composed mission: every room's 2 m cells placed in the world, per floor.
    /// Rule, from eight walked missions (1,828 points, 1,002 steps; 97.9% of steps connect, the rest through
    /// the fallback): a cell is floor when ANY room covering it has a tile there. Each room's last row and
    /// column are the cell it shares with its neighbour and hold only 0 (wall) or 0x80 (doorway), and the
    /// owner walked through shared cells where one side said 0 about as often as through plain floor, while
    /// cells where every side said 0 were almost never crossed. Those all-zero shared cells are kept as a
    /// costly fallback, used only when nothing else connects (one leg of capture 20260923-120056 needed it).
    /// Steps between neighbouring cells may climb 2 m (ramps up to 1.9 m were walked).
    /// </summary>
    public sealed class MissionGrid
    {
        private readonly Dictionary<(int, int, int), float> _walk = new Dictionary<(int, int, int), float>();
        private readonly Dictionary<(int, int, int), float> _fallback = new Dictionary<(int, int, int), float>();
        private readonly Dictionary<(int, int, int), string> _room = new Dictionary<(int, int, int), string>();
        private readonly Dictionary<int, float> _floorY = new Dictionary<int, float>();
        public float Cell = 2f;
        public List<int> Floors = new List<int>();
        public int? BossFloor;
        public string BossRoomName;
        public int MappingErrors;
        private const float MaxStepUp = 2.0f;
        private const float FallbackCost = 6f;

        public static MissionGrid Build(AOBuddyNav nav)
        {
            var g = new MissionGrid();
            var d = nav.Dungeon;
            g.Cell = d.Cell;
            var cover = new Dictionary<(int, int, int), List<(int tile, float y, string room)>>();
            foreach (var rm in d.Rooms)
            {
                int x1 = rm.Rect[0], z1 = rm.Rect[1], x2 = rm.Rect[2];
                double cx = (x1 + x2 + 1) / 2.0, cz = (z1 + rm.Rect[3] + 1) / 2.0;
                int turns = ((-rm.Rot) % 4 + 4) % 4;
                for (int b = 0; b < rm.Tile.Length; b++)
                    for (int a = 0; a < rm.Tile[b].Length; a++)
                    {
                        double dx = (x1 + a + 0.5 - cx) * d.Cell, dz = (z1 + b + 0.5 - cz) * d.Cell;
                        // Inverse of NavDungeon.CellOf, which turns a world offset by (dx,dz)->(dz,-dx) 'turns' times.
                        for (int i = 0; i < turns; i++) { double t = dx; dx = -dz; dz = t; }
                        double wx = rm.Pos[0] + dx, wz = rm.Pos[2] + dz;
                        if (!d.CellOf(rm, wx, wz, out int ca, out int cb) || ca != a || cb != b) g.MappingErrors++;
                        float y = rm.Pos[1] + (rm.Height[b][a] - rm.HeightBase) * d.HeightScale;
                        var key = (rm.Floor, (int)Math.Floor(wx / d.Cell), (int)Math.Floor(wz / d.Cell));
                        if (!cover.TryGetValue(key, out var l)) cover[key] = l = new List<(int, float, string)>();
                        l.Add((rm.Tile[b][a], y, rm.PoolName));
                    }
                if (!g.Floors.Contains(rm.Floor)) g.Floors.Add(rm.Floor);
                if (!g._floorY.ContainsKey(rm.Floor)) g._floorY[rm.Floor] = rm.Pos[1];
                if (rm.PoolName.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0) { g.BossFloor = rm.Floor; g.BossRoomName = rm.PoolName; }
            }
            g.Floors.Sort();
            // Each room's walkable cell nearest its centre, for searching.
            foreach (var rm in d.Rooms)
            {
                var centre = new Vector3(rm.Pos[0], rm.Pos[1], rm.Pos[2]);
                (int, int, int)? bestK = null; float bd = float.MaxValue;
                foreach (var kv in cover)
                {
                    if (kv.Key.Item1 != rm.Floor || !kv.Value.Any(c => c.tile != 0 && c.room == rm.PoolName)) continue;
                    float cx2 = (kv.Key.Item2 + 0.5f) * d.Cell - centre.X, cz2 = (kv.Key.Item3 + 0.5f) * d.Cell - centre.Z;
                    float dd = cx2 * cx2 + cz2 * cz2;
                    if (dd < bd) { bd = dd; bestK = kv.Key; }
                }
                if (bestK.HasValue)
                    g._spots.Add(new RoomSpot { Index = rm.Index, Name = rm.PoolName, Floor = rm.Floor,
                        Centre = new Vector3((bestK.Value.Item2 + 0.5f) * d.Cell, kv0(cover[bestK.Value]), (bestK.Value.Item3 + 0.5f) * d.Cell) });
            }
            foreach (var kv in cover)
            {
                float best = float.MinValue; string room = null;
                foreach (var c in kv.Value) if (c.tile != 0 && c.y > best) { best = c.y; room = c.room; }
                if (room != null) { g._walk[kv.Key] = best; g._room[kv.Key] = room; }
                else if (kv.Value.Count >= 2) { g._fallback[kv.Key] = kv.Value.Max(c => c.y); g._room[kv.Key] = kv.Value[0].room; }
            }
            return g;
        }

        public sealed class RoomSpot { public int Index; public string Name; public int Floor; public Vector3 Centre; }
        private readonly List<RoomSpot> _spots = new List<RoomSpot>();
        public IEnumerable<RoomSpot> RoomsOn(int floor) => _spots.Where(r => r.Floor == floor);

        private static float kv0(List<(int tile, float y, string room)> l) { foreach (var c in l) if (c.tile != 0) return c.y; return l[0].y; }

        public string Describe() => $"{Floors.Count} floor(s) {string.Join(",", Floors)}, {_walk.Count} floor cells, boss room {(BossFloor.HasValue ? $"'{BossRoomName}' on floor {BossFloor}" : "none")}";

        /// <summary>The floor under a point: the cell whose height is nearest, within 3 m.</summary>
        public int? FloorAt(Vector3 p)
        {
            var k = Snap(p, 3f, 2, false);
            if (k.HasValue) return k.Value.Item1;
            // Off the tiles (a button standing on a platform, say): the floor whose rooms sit nearest in height.
            int? best = null; float bd = 8f;
            foreach (var kv in _floorY) { float dd = Math.Abs(kv.Value - p.Y); if (dd < bd) { bd = dd; best = kv.Key; } }
            return best;
        }

        public string RoomAt(Vector3 p) { var k = Snap(p, 3f, 1, true); return k.HasValue && _room.TryGetValue(k.Value, out var r) ? r : null; }

        public (int, int, int)? CellOf(Vector3 p) => Snap(p, 3f, 0, true);

        public float? HeightAt(Vector3 p, float nearY)
        {
            var k = Snap(p, 3f, 0, true);
            if (k.HasValue && _walk.TryGetValue(k.Value, out float y)) return y;
            if (k.HasValue && _fallback.TryGetValue(k.Value, out y)) return y;
            return null;
        }

        private (int, int, int)? Snap(Vector3 p, float tol, int radius, bool includeFallback)
        {
            int ix = (int)Math.Floor(p.X / Cell), iz = (int)Math.Floor(p.Z / Cell);
            (int, int, int)? best = null; float bestScore = float.MaxValue;
            for (int r = 0; r <= radius && best == null; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dz = -r; dz <= r; dz++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dz)) != r) continue;
                        foreach (int f in Floors)
                        {
                            var k = (f, ix + dx, iz + dz);
                            float y;
                            if (!_walk.TryGetValue(k, out y) && !(includeFallback && _fallback.TryGetValue(k, out y))) continue;
                            float s = Math.Abs(y - p.Y) + (dx * dx + dz * dz) * 0.01f;
                            if (Math.Abs(y - p.Y) <= tol && s < bestScore) { bestScore = s; best = k; }
                        }
                    }
            return best;
        }

        /// <summary>A walkable path from a to b (world points on the floor), or null. Tiles first; the
        /// all-zero shared cells only when the tiles alone do not connect.</summary>
        public List<Vector3> FindPath(Vector3 a, Vector3 b, HashSet<(int, int, int)> blocked, out bool usedFallback)
        {
            usedFallback = false;
            var s = Snap(a, 3f, 2, true); var g = Snap(b, 3f, 3, true);
            if (!s.HasValue || !g.HasValue || s.Value.Item1 != g.Value.Item1) return null;
            var cells = AStar(s.Value, g.Value, blocked, false) ?? AStar(s.Value, g.Value, blocked, true);
            if (cells == null) return null;
            usedFallback = cells.Any(c => !_walk.ContainsKey(c));
            var pts = Smooth(cells, blocked);
            // End on the goal itself so the last step lands on the button / target, not the cell centre.
            pts[pts.Count - 1] = new Vector3(b.X, pts[pts.Count - 1].Y, b.Z);
            return pts;
        }

        private bool Open((int, int, int) k, HashSet<(int, int, int)> blocked, bool fb, out float y, out float cost)
        {
            cost = 1f;
            if (blocked != null && blocked.Contains(k)) { y = 0; return false; }
            if (_walk.TryGetValue(k, out y)) return true;
            if (fb && _fallback.TryGetValue(k, out y)) { cost = FallbackCost; return true; }
            return false;
        }

        private List<(int, int, int)> AStar((int, int, int) s, (int, int, int) g, HashSet<(int, int, int)> blocked, bool fb)
        {
            var open = new PriorityQueue<(int, int, int), float>();
            var cost = new Dictionary<(int, int, int), float> { [s] = 0 };
            var prev = new Dictionary<(int, int, int), (int, int, int)>();
            float H((int, int, int) k) => (float)Math.Sqrt((k.Item2 - g.Item2) * (k.Item2 - g.Item2) + (k.Item3 - g.Item3) * (k.Item3 - g.Item3));
            open.Enqueue(s, H(s));
            int guard = 0;
            while (open.Count > 0 && guard++ < 200000)
            {
                var k = open.Dequeue();
                if (k.Equals(g))
                {
                    var path = new List<(int, int, int)> { k };
                    while (prev.TryGetValue(k, out var p)) { k = p; path.Add(k); }
                    path.Reverse();
                    return path;
                }
                float c0 = cost[k];
                float y0 = _walk.TryGetValue(k, out float wy) ? wy : (_fallback.TryGetValue(k, out float fy) ? fy : 0);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        var n = (k.Item1, k.Item2 + dx, k.Item3 + dz);
                        if (!n.Equals(g) && !Open(n, blocked, fb, out _, out _)) continue;
                        if (!Open(n, blocked, fb, out float y1, out float cc)) { if (!n.Equals(g)) continue; y1 = y0; cc = 1; }
                        if (Math.Abs(y1 - y0) > MaxStepUp) continue;
                        if (dx != 0 && dz != 0 && (!Open((k.Item1, k.Item2 + dx, k.Item3), blocked, fb, out _, out _) || !Open((k.Item1, k.Item2, k.Item3 + dz), blocked, fb, out _, out _))) continue;
                        float nc = c0 + (dx != 0 && dz != 0 ? 1.4142f : 1f) * cc;
                        if (cost.TryGetValue(n, out float old) && old <= nc) continue;
                        cost[n] = nc; prev[n] = k;
                        open.Enqueue(n, nc + H(n));
                    }
            }
            return null;
        }

        private Vector3 Centre((int, int, int) k)
        {
            float y = _walk.TryGetValue(k, out float wy) ? wy : (_fallback.TryGetValue(k, out float fy) ? fy : 0);
            return new Vector3((k.Item2 + 0.5f) * Cell, y, (k.Item3 + 0.5f) * Cell);
        }

        // Drop waypoints the straight line between two kept ones covers: sampled every half cell, every
        // sample must be on an open cell of the same floor with no step over 2 m.
        private List<Vector3> Smooth(List<(int, int, int)> cells, HashSet<(int, int, int)> blocked)
        {
            var pts = new List<Vector3> { Centre(cells[0]) };
            int i = 0;
            while (i < cells.Count - 1)
            {
                int j = cells.Count - 1;
                while (j > i + 1 && !Clear(cells[i], cells[j], blocked)) j--;
                pts.Add(Centre(cells[j]));
                i = j;
            }
            return pts;
        }

        private bool Clear((int, int, int) a, (int, int, int) b, HashSet<(int, int, int)> blocked)
        {
            float ax = a.Item2 + 0.5f, az = a.Item3 + 0.5f, bx = b.Item2 + 0.5f, bz = b.Item3 + 0.5f;
            int n = (int)Math.Ceiling(Math.Max(Math.Abs(bx - ax), Math.Abs(bz - az)) * 4) + 1;
            float lastY = Centre(a).Y;
            (int, int, int) lastCell = a;
            for (int s = 1; s <= n; s++)
            {
                float t = s / (float)n;
                var k = (a.Item1, (int)Math.Floor(ax + (bx - ax) * t), (int)Math.Floor(az + (bz - az) * t));
                if (k.Equals(lastCell)) continue;
                if (!_walk.TryGetValue(k, out float y) || (blocked != null && blocked.Contains(k))) return false;
                if (Math.Abs(y - lastY) > MaxStepUp) return false;
                // A diagonal hop between samples must not cut a corner.
                if (k.Item2 != lastCell.Item2 && k.Item3 != lastCell.Item3 &&
                    (!_walk.ContainsKey((k.Item1, k.Item2, lastCell.Item3)) || !_walk.ContainsKey((k.Item1, lastCell.Item2, k.Item3)))) return false;
                lastY = y; lastCell = k;
            }
            return true;
        }
    }
}
