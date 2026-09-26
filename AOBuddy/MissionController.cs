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
    /// It never sends StopAttack, takes every server SetPos while it runs (OnServerCorrection), and hands
    /// the body back the moment it stops.
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
        private enum Phase { Off, Plan, Walk, PressButton, AwaitTeleport, Act, AwaitComplete, Exit, PushOut, Done, PickLock }
        private float _pushed;                   // metres walked through the exit door so far
        private Phase _phase = Phase.Off;
        private bool _completed;                 // the objective is done; the rest is the walk out
        private bool _announced;                 // the owner has been told it is done
        private string _why = "";
        private List<Vector3> _path;
        /// <summary>The walk inside the building being followed now (for the API's /nav), or null.</summary>
        public Vector3[] CurrentPath { get { var p = _path; return p?.ToArray(); } }
        private int _pathIndex;
        private Identity? _pendingButton;
        private Vector3 _pressedFrom;
        private double _phaseTime;               // seconds in the current phase
        private Movement.StuckWatch _stuck;
        private int _replans, _presses, _acts;
        private Vector3? _lastCorrection;        // where the server last snapped us to during a walk
        private readonly HashSet<(int, int, int)> _blocked = new HashSet<(int, int, int)>();
        private Purpose _purpose;
        private enum Purpose { Button, Target, Entrance, Search, Clear }
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

        /// <summary>The blitz is selecting its target and waiting for the completion: nothing else may move the
        /// selection meanwhile (no combat retarget, cast, stim or pet command - each sends its own LookAt). The
        /// owner's client selected the item and sent nothing else until the completion 165 ms later (capture
        /// 20260924-074524 s9 08:01:45.575); the bot on a team run stood at the item and failed three tries.</summary>
        public bool HoldsSelection => (_phase == Phase.Act || _phase == Phase.AwaitComplete) && !_completed;
        public bool InMission => _grid != null;

        /// <summary>The composed mission's placement (zone-in packet), for the API's /nav — AOBuddyMonitor
        /// recomposes the identical floor plan from it via AOBuddyNav.ComposeMission. Null outside a mission.</summary>
        public AOBuddyNav.MissionLayout Layout => _nav?.Layout;

        /// <summary>The composed mission's placed rooms (floor numbering as the server sent it), for /nav's floors list.</summary>
        public List<NavDungeon.Room> NavDungeonRooms => _nav?.Dungeon?.Rooms;
        // For the recorder (MissionRecorder).
        public int Instance => _instance;
        public string BuildingName => _nav?.Name;
        public string RecordTypeName => _record?.TypeName;
        public bool Completed => _completed;

        /// <summary>RETURN ITEM: the template of the item picked up in the building, to hand in at the terminal; 0 = none.</summary>
        public int CarriedReturnItem { get; set; }
        private int ReturnItemTemplate() => _record?.TargetA.HasValue == true && (int)_record.TargetA.Value.Type == 0xC74E ? _record.TargetA.Value.Instance : 0;
        private Item ReturnItemInInventory() => FindReturnItem(ReturnItemTemplate(), _record?.Text);
        /// <summary>The item to return, in the main inventory: by template (low or high id), else by its name in the mission text.</summary>
        public static Item FindReturnItem(int template, string text)
        {
            var inv = Inventory.Items.Where(i => i != null && i.Slot.Type == IdentityType.Inventory).ToList();
            var it = template != 0 ? inv.FirstOrDefault(i => i.Id == template || i.HighId == template) : null;
            if (it == null && !string.IsNullOrEmpty(text))
                it = inv.Where(i => !string.IsNullOrEmpty(i.Name) && i.Name.Length >= 6 && text.IndexOf(i.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                        .OrderByDescending(i => i.Name.Length).FirstOrDefault();
            return it;
        }
        public string RoomAt(Vector3 p) => _grid?.RoomAt(p);
        public int? FloorAt(Vector3 p) => _grid?.FloorAt(p);
        public Newtonsoft.Json.Linq.JArray DoorsJson() => new Newtonsoft.Json.Linq.JArray(_doors.Select(kv => new Newtonsoft.Json.Linq.JObject
        {
            ["id"] = kv.Key.ToString(), ["pos"] = new Newtonsoft.Json.Linq.JArray(Math.Round(kv.Value.Pos.X, 2), Math.Round(kv.Value.Pos.Y, 2), Math.Round(kv.Value.Pos.Z, 2)),
            ["locked"] = kv.Value.Locked, ["picked"] = !kv.Value.Locked && _pickedDoors.Contains(kv.Key), ["unpickable"] = _unpickable.Contains(kv.Key),
        }));
        private readonly HashSet<Identity> _pickedDoors = new HashSet<Identity>();

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

        private double Now => _ctx.Clock.Seconds;

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

                case FormatFeedbackMessage _:
                {
                    // Read the text from the raw packet: the SDK's FormattedMessage getter calls into MSVCR100 and
                    // throws here ('??2@YAPAXI@Z' not found, 21 times in the first clear runs, 2026-09-25), so the
                    // clear % never got through. The text is plain ASCII from '~&' to the end of the string.
                    string txt = RawFormatted(m.RawPacket);
                    if (txt != null && TryClearPct(txt, out float pct))
                    {
                        ClearPct = pct;
                        _ctx.Log($"MISSION: cleared {pct:0.#}% of this mission's mobs.");
                    }
                    else if (txt != null && ClearMode && _grid != null)
                        _ctx.Log($"MISSION: server line '{txt}'");
                    break;
                }

                case FeedbackMessage fb:
                    // Whatever the server says while we wait on the objective (a refusal would show here).
                    if (_phase == Phase.AwaitComplete && IsMe(fb.Identity))
                        _ctx.Log($"MISSION: feedback {fb.CategoryId}/{fb.MessageId} while waiting for the completion.");
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

        /// <summary>A ChestFullUpdate, raw: 16-byte header, N3 type at 16, the container's Identity at 20,
        /// then a byte, version, owner identity, and its position as three big-endian floats at 41 (capture
        /// 20260923-201746 s12 seq 11: 51017:196604570 at (279.9, 5.1, 267.8)).</summary>
        public void OnChestRaw(byte[] b)
        {
            if (b == null || b.Length < 53) return;
            int type = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
            int inst = (b[24] << 24) | (b[25] << 16) | (b[26] << 8) | b[27];
            float F(int p) => BitConverter.ToSingle(new[] { b[p + 3], b[p + 2], b[p + 1], b[p] }, 0);
            var pos = new Vector3(F(41), F(45), F(49));
            if (float.IsNaN(pos.X) || Math.Abs(pos.X) > 100000) return;
            var cid = new Identity((IdentityType)type, inst);
            _items[cid] = new SeenItem { Template = 0, Pos = pos, Seen = Now };
            _chestRaw[cid] = b;
            if (_chestSaved < 12 && _instance != 0)
            {
                _chestSaved++;
                try
                {
                    string dir = System.IO.Path.Combine(_pluginDir, "missions");
                    System.IO.Directory.CreateDirectory(dir);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, $"chest-b{_instance}-{inst:X}.bin"), b);
                }
                catch { }
            }
        }

        private readonly Dictionary<Identity, byte[]> _chestRaw = new Dictionary<Identity, byte[]>();
        private int _chestSaved;

        /// <summary>The container whose update carries the item id (big-endian int), if any: a find-item target
        /// that names an item TYPE (0xC74E) sits inside one of the building's containers (2026-09-23 22:38: seven
        /// containers and no loose item were all the server sent).</summary>
        private Identity? ContainerHolding(int itemId)
        {
            foreach (var kv in _chestRaw)
            {
                var b = kv.Value;
                for (int i = 29; i + 4 <= b.Length; i++)
                    if (((b[i] << 24) | (b[i + 1] << 16) | (b[i + 2] << 8) | b[i + 3]) == itemId) return kv.Key;
            }
            return null;
        }

        private static bool IsMe(Identity id)
        {
            var me = DynelManager.LocalPlayer;
            return me != null && id.Instance == me.Identity.Instance;
        }

        private void OnZoneIn(byte[] raw)
        {
            if (_phase == Phase.PushOut) { _tell("Outside the mission. Mission mode off."); _ctx.Log("MISSION: walked out of the building."); }
            if (Active) Stop("zoned");
            _items.Clear(); _chestRaw.Clear(); _chestSaved = 0; _record = null; _grid = null; _nav = null; _instance = 0; _blocked.Clear(); _visited.Clear(); _lastPathFrom = null;
            _doors.Clear(); _unpickable.Clear(); _pickDoor = null;
            try
            {
                _nav = raw == null ? null : AOBuddyNav.LoadMission(_pluginDir, raw);
                if (_nav?.Layout == null) { _nav = null; return; }
                _instance = _nav.Layout.Instance;
                if (_clearInstance != _instance) { _clearInstance = _instance; ClearPct = -1; _clearVisited.Clear(); _clearPasses = 0; _clearGaveUp = false; }
                _grid = MissionGrid.Build(_nav, _doors);
                ParseRecord();
                _ctx.Log($"MISSION: in {_nav.Name} instance {_instance}, {_grid.Describe()}");
                if (_nav.Walls != null) _ctx.Log("MISSION: placement check: " + AOBuddyNav.DoorCheck);
                if (_grid.MappingErrors > 0) _ctx.Log($"MISSION: WARNING {_grid.MappingErrors} room cells did not map back to themselves");
            }
            catch (Exception ex) { _ctx.Log("MISSION: could not compose the instance: " + ex.Message); _grid = null; _nav = null; }
        }

        private readonly HashSet<int> _savedQuestFor = new HashSet<int>();
        private double _itemsLoggedAt = -99;

        private void ParseRecord()
        {
            var me = DynelManager.LocalPlayer;
            if (_lastQuestUpdate == null || me == null) return;
            var all = MissionRecords.Parse(_lastQuestUpdate, me.Identity.Instance);
            _record = all.FirstOrDefault(r => _instance != 0 && r.Building == _instance);
            if (_record != null)
                _ctx.Log($"MISSION: record {_record.TypeName} building {_record.Building} target {(_record.TargetA?.ToString() ?? "none (not the holder)")}{(_record.TargetB.HasValue ? " object " + _record.TargetB : "")}");
            // No target read for a mission the bot rolled itself (2026-09-23 21:55, find item): keep the raw quest
            // update so the record layout can be checked against it.
            if (_record != null && !_record.TargetA.HasValue && !_savedQuestFor.Contains(_record.Building))
            {
                _savedQuestFor.Add(_record.Building);
                try
                {
                    string dir = System.IO.Path.Combine(_pluginDir, "missions");
                    System.IO.Directory.CreateDirectory(dir);
                    string f = System.IO.Path.Combine(dir, $"questupdate-b{_record.Building}-{DateTime.Now:yyyyMMdd-HHmmss}.bin");
                    System.IO.File.WriteAllBytes(f, _lastQuestUpdate);
                    _ctx.Log($"MISSION: no target in the record; quest update saved ({_lastQuestUpdate.Length} bytes) to {f}");
                }
                catch (Exception ex) { _ctx.Log("MISSION: couldn't save the quest update: " + ex.Message); }
            }
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
                case "backoutside":
                case "out":
                    if (_grid == null) { reply("Not in a mission building."); break; }
                    GoOutside(reply);
                    break;
                default: reply("mission status | route | blitz | backoutside | stop"); break;
            }
        }

        public string Status()
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
            var path = PathFrom(me.MovementComponent.Position, hop.Value.Pos, out bool usedFallback);
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
            _completed = false; _announced = false; _rewards.Clear(); _replans = 0; _presses = 0; _acts = 0; _blocked.Clear(); _visited.Clear(); _lastCorrection = null;
            Enter(Phase.Plan, "starting");
            string target = _record == null ? "no quest record yet" : _record.TypeName;
            reply($"Blitz on: {target}. Floors {string.Join(",", _grid.Floors)}, boss room on floor {(_grid.BossFloor?.ToString() ?? "?")}. Send 'mission stop' to cancel.");
        }

        /// <summary>
        /// 'mission backoutside': walk out of the building from wherever the bot is, riding the up buttons,
        /// whether or not a blitz ran or finished (a team mission whose objective the owner completes, a blitz
        /// that gave up). The same walk the blitz does after the objective, without the completion message.
        /// </summary>
        private void GoOutside(Action<string> reply)
        {
            if (Active) Stop("owner said back outside");
            _completed = true; _announced = true; _rewards.Clear(); _replans = 0; _presses = 0; _blocked.Clear(); _lastCorrection = null;
            _path = null; _pendingButton = null;
            Enter(Phase.Exit, "owner said back outside");
            var ex = _nav.Exit;
            reply(ex != null ? $"Heading outside: the exit door on floor {ex.Floor} ({ex.X:0},{ex.Z:0}). Send 'mission stop' to cancel."
                             : "Heading to where I zoned in (this building's exit door is unknown). Send 'mission stop' to cancel.");
        }

        public void Stop(string why)
        {
            if (_phase == Phase.Off) return;
            var me = DynelManager.LocalPlayer;
            if (me != null) _move.Stop(me, _ctx.Config.SendIntervalMs);
            _ctx.Log($"MISSION: blitz stopped ({why}) in phase {_phase}.");
            _phase = Phase.Off; _path = null; _pendingButton = null;
        }

        /// <summary>
        /// A server position correction while a blitz owns the body. Follow ignores small ones while moving
        /// (ramp jitter), but in a mission they are rare and they mean the server stopped us: it held the
        /// bot at a doorframe while the local body walked on to the button (log 2026-09-23 22:31:18), and
        /// everything after that was planned from a position the server never agreed to. So take the
        /// server's position and plan again from there. Returns true when handled.
        /// </summary>
        public bool OnServerCorrection(LocalPlayer me, Vector3 serverPos)
        {
            if (!Active || me == null) return false;
            Vector3 local = me.MovementComponent.Position;
            float gap = Movement.Flat(local, serverPos);
            Movement.SetPose(me, serverPos, me.MovementComponent.Heading);
            _move.Reset();
            _ctx.Log($"MISSION: server put me at ({serverPos.X:0},{serverPos.Y:0},{serverPos.Z:0}), {gap:0.0} m from where I thought I was; planning from there.");
            if (_phase == Phase.Walk && gap > 2f)
            {
                // Snapped back to the same spot again: that is a wall the grid does not show, right ahead of
                // where the server holds us. Block the cells we kept trying to walk into, so the next route
                // takes another line instead of the same one (the first time can be plain lag, so not then).
                if (_lastCorrection.HasValue && Movement.Flat(_lastCorrection.Value, serverPos) < 1.5f && gap > 0.5f)
                {
                    var here = _grid.CellOf(serverPos);
                    float dx = (local.X - serverPos.X) / gap, dz = (local.Z - serverPos.Z) / gap;
                    var added = new List<string>();
                    for (float s = 1f; s <= 3f; s += 1f)
                    {
                        var c = _grid.CellOf(new Vector3(serverPos.X + dx * s, serverPos.Y, serverPos.Z + dz * s));
                        if (c.HasValue && !c.Equals(here) && _blocked.Add(c.Value)) added.Add($"{c.Value.Item2},{c.Value.Item3}");
                    }
                    if (added.Count > 0) _ctx.Log($"MISSION: snapped back to the same spot again, blocking cell(s) {string.Join(" ", added)} ahead of it.");
                }
                _lastCorrection = serverPos;
                _replans++;
                if (_replans > 12) { Fail("the server kept stopping me short"); return true; }
                _path = null;
                Enter(_completed ? Phase.Exit : Phase.Plan, "corrected by the server, planning again");
            }
            return true;
        }

        private void Enter(Phase p, string why)
        {
            if (p != _phase || why != _why) _ctx.Log($"MISSION: {_phase} -> {p}: {why}");
            _phase = p; _why = why; _phaseTime = 0; _stuck.Reset();
            _actStill = 0;
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
                    _move.Hold(me, _ctx.Config.SendIntervalMs);
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

                case Phase.PickLock:
                    PickTick(me);
                    return true;

                case Phase.PressButton:
                {
                    _move.Hold(me, _ctx.Config.SendIntervalMs);
                    if (!me.IsSpecialReady(Stat.Level))
                    {
                        _ctx.WalkState = "mission: waiting for the Level lock";
                        if (_phaseTime > 20) Fail("the Level lock never cleared");
                        return true;
                    }
                    if (_pendingButton == null) { Enter(Phase.Plan, "no button"); return true; }
                    // The server judges the press from where IT has us. Pressed 17 ms after arriving, with the
                    // server still 5.7 m back at the doorframe, every press was refused (feedback 110/172594057)
                    // and the bot pressed 12 times from the same spot (log 2026-09-23 22:31:20). So: stand still
                    // a moment so the server has our stop, and walk up again if a correction moved us off.
                    // (3.5 m: with wall data the route ends on the open cell nearest the button, up to 3 m off it.)
                    if (_items.TryGetValue(_pendingButton.Value, out var btn) && Movement.Flat(pos, btn.Pos) > 3.5f)
                    {
                        Enter(Phase.Plan, $"{Movement.Flat(pos, btn.Pos):0.0} m from the button, walking up to it");
                        return true;
                    }
                    if (_phaseTime < 0.6) { _ctx.WalkState = "mission: settling before the press"; return true; }
                    _presses++;
                    if (_presses > 12) { Fail("pressed buttons 12 times without getting anywhere"); return true; }
                    _pressedFrom = pos;
                    GameCommands.UseObject(me, _pendingButton.Value);
                    _ctx.Log($"MISSION: pressed {KindName(_items.TryGetValue(_pendingButton.Value, out var it) ? it.Template : 0)} button {_pendingButton.Value}.");
                    Enter(Phase.AwaitTeleport, "pressed the button");
                    return true;
                }

                case Phase.AwaitTeleport:
                    _move.Hold(me, _ctx.Config.SendIntervalMs);
                    if (Vector3.Distance(pos, _pressedFrom) > 10f)
                    {
                        _ctx.Log($"MISSION: rode to ({pos.X:0},{pos.Y:0},{pos.Z:0}), floor {_grid.FloorAt(pos)?.ToString() ?? "?"}.");
                        _pendingButton = null; _path = null;
                        Enter(_completed ? Phase.Exit : Phase.Plan, "arrived");
                        return true;
                    }
                    // No ride: most likely the server did not have us in range. Plan again from where we are
                    // now (a correction may have moved us); at the button that is a zero-length walk and a press.
                    if (_phaseTime > 4) { _path = null; Enter(Phase.Plan, "no teleport after 4 s, walking up to the button again"); }
                    return true;

                case Phase.PushOut:
                {
                    // Walk straight out through the exit door until the zone changes (OnZoneIn ends the blitz).
                    // The door is a wall gap, so this leg is not on the grid; it is at most 8 m.
                    var ex = _nav.Exit;
                    if (_pushed > 8f || _phaseTime > 6)
                    {
                        Fail($"walked {_pushed:0} m through the exit door at ({ex.X:0},{ex.Z:0}) and did not leave the building");
                        return true;
                    }
                    var dir = new Vector3((float)ex.Nx, 0, (float)ex.Nz);
                    float step = Math.Min((float)(_ctx.RunVelocity(me) * dt), _ctx.Config.MaxStep);
                    _pushed += step;
                    _ctx.WalkState = $"mission: out through the door {_pushed:0.0} m";
                    _move.Advance(me, new Vector3(pos.X + dir.X * step, pos.Y, pos.Z + dir.Z * step), Movement.SafeLook(dir, me.MovementComponent.Heading), run: true, dt, _ctx.Config.SendIntervalMs);
                    return true;
                }

                case Phase.Act:
                    if ((_record?.Type == TypeFindItem || _record?.Type == TypeReturnItem) && ApproachItem(me, dt)) return true;
                    _move.Hold(me, _ctx.Config.SendIntervalMs);
                    // Stand still a moment first so the server has our stop (the button lesson, 2026-09-23
                    // 22:31:20: acting the instant we arrived was judged from where the server still had us).
                    if ((_actStill += dt) < 0.6) { _ctx.WalkState = "mission: settling before the selection"; return true; }
                    DoObjective(me);
                    return true;

                case Phase.AwaitComplete:
                    _move.Hold(me, _ctx.Config.SendIntervalMs);
                    if (_completed) return true;
                    // RETURN ITEM: picked up = the item is in the inventory; the mission completes at the terminal, so
                    // the building is done - out with it (the run takes it to the terminal).
                    if (_record?.Type == TypeReturnItem && ReturnItemInInventory() != null)
                    {
                        CarriedReturnItem = ReturnItemTemplate();
                        _ctx.Log($"MISSION: picked up the item to return ({ReturnItemInInventory().Name}); taking it back to the terminal.");
                        _announced = true;
                        OnCompleted("carrying the return item");
                        return true;
                    }
                    if (_phaseTime > 5)
                    {
                        if (_acts >= 3) { Fail("the objective did not complete after 3 tries"); return true; }
                        Enter(Phase.Act, "no completion after 5 s, trying again");
                    }
                    return true;
            }
            return false;
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
            foreach (var id in _unpickable)
                if (_doors.TryGetValue(id, out var ud) && _grid.CellOf(ud.Pos) is (int, int, int) uc) _blocked.Add(uc);
            var hop = NextHop(me.MovementComponent.Position, out string why);
            if (hop == null)
            {
                // A floor's buttons arrive with the ride that brings us there; give them a moment.
                if (_phaseTime < 6) { _ctx.WalkState = "mission: " + why; return; }
                Fail(why);
                return;
            }
            var h = hop.Value;
            _path = PathFrom(me.MovementComponent.Position, h.Pos, out bool fb);
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
                // The building's own exit door (AOBuddyNav.Exit), approached from 1.5 m inside it; the landing
                // point only when the exit is unknown - it is the entrance only if we came in from outside.
                var ex = _nav.Exit;
                var land = ex != null ? new Vector3((float)(ex.X - ex.Nx * 1.5), (float)ex.Y, (float)(ex.Z - ex.Nz * 1.5))
                                      : new Vector3(_nav.Layout.LandX, _nav.Layout.LandY, _nav.Layout.LandZ);
                int? lf = _grid.FloorAt(land);
                if (!lf.HasValue) { why = "the entrance is not on any floor"; return null; }
                goalFloor = lf.Value; goalPos = land;
            }
            else
            {
                // CLEAR MODE: every room on every floor before the objective, until the server says 100%.
                if (Clearing)
                {
                    var ch = ClearHop(pos, myFloor.Value, out why);
                    if (ch.HasValue) return ch;
                }
                target = FindTarget(out Vector3? tpos, out string tw);
                if (target.HasValue && tpos.HasValue && _grid.FloorAt(tpos.Value) is int tf)
                {
                    goalFloor = tf; goalPos = tpos;
                }
                // Seen, but its position is on no floor: head for it on this floor rather than search rooms. The
                // walk step counted it 'in sight' and the plan went back to searching, every second, for 12 minutes
                // (03:30-03:42, 2026-09-25, Borealis Omnilab, target Terminal:EE6AF09).
                else if (target.HasValue && tpos.HasValue)
                {
                    if (_offFloorLogged != target) { _offFloorLogged = target; _ctx.Log($"MISSION: target {target} seen at ({tpos.Value.X:0.0},{tpos.Value.Y:0.0},{tpos.Value.Z:0.0}), on no floor; heading for it on floor {myFloor}."); }
                    goalFloor = myFloor.Value; goalPos = new Vector3(tpos.Value.X, _grid.HeightAt(tpos.Value, pos.Y) ?? pos.Y, tpos.Value.Z);
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

            return ButtonHop(pos, myFloor.Value, goalFloor, out why);
        }

        /// <summary>The button on this floor that leads toward goalFloor, nearest by path.</summary>
        private Hop? ButtonHop(Vector3 pos, int myFloor, int goalFloor, out string why)
        {
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
                var p = PathFrom(pos, b.Value.Pos, out _);
                if (p == null) continue;
                float len = 0; for (int i = 1; i < p.Count; i++) len += Vector3.Distance(p[i - 1], p[i]);
                if (len < bestLen) { bestLen = len; best = new Hop { Pos = b.Value.Pos, Purpose = Purpose.Button, Button = b.Key }; }
            }
            if (best == null) { why = $"a {KindName(want)} button is on floor {myFloor} but I cannot reach it"; return null; }
            why = $"to the {KindName(_items[best.Value.Button.Value].Template)} button (floor {myFloor} -> {goalFloor})";
            return best;
        }

        // ---- locked doors -------------------------------------------------------------------------------
        // Some mission doors are locked (owner, 2026-09-25). Capture 20260925-113057, two of them picked by the
        // owner with a Lock Pick (95577, QL1, kept after use) from inventory slot 89:
        //   * DoorFullUpdate (0xC748 identity): position at raw 41/45/49; a flag word 8 bytes after the 00 00 2F 4C
        //     marker whose low byte has 0x40 = LOCKED (0x43 on both locked doors, 0x03 after) and 0x80 = open
        //     (0x83/0x03 as people walk through); LockDifficulty 33 on the locked ones, 50 on the rest.
        //   * the pick: LookAt the door, then GenericCmd UseItemOnItem, source Inventory:89, target the door
        //     (client seq 202/205). Server: the GenericCmd echoed (Verification 1), DoorStatusUpdate Locked=0
        //     Open=0, ActionMessage action 115 by the picker on the door. Both opened on the first try; a
        //     failed try is assumed to be no action 115 (the owner: 'not opening is the fail').
        // B&E can take 20-30 tries, so: one try every 2.5 s, up to 40, then the door is blocked and we go round.
        
        private readonly Dictionary<Identity, DoorInfo> _doors = new Dictionary<Identity, DoorInfo>();
        private readonly HashSet<Identity> _unpickable = new HashSet<Identity>();
        private Identity? _pickDoor;
        private int _pickTries;
        private double _pickAt;
        private Phase _pickReturn;
        private bool _noPickTold;
        public static readonly int[] LockPicks = { 95577, 156639, 216380, 295999, 296000, 296001, 297014 };
        private const int DoorType = 0xC748, ActionUnlocked = 115, PickTries = 40;

        private static int BE32(byte[] b, int p) => (b[p] << 24) | (b[p + 1] << 16) | (b[p + 2] << 8) | b[p + 3];

        public void OnDoorRaw(byte[] b)
        {
            if (b == null || b.Length < 60 || BE32(b, 20) != DoorType) return;
            var id = new Identity((IdentityType)DoorType, BE32(b, 24));
            float F(int p) => BitConverter.ToSingle(new[] { b[p + 3], b[p + 2], b[p + 1], b[p] }, 0);
            var pos = new Vector3(F(41), F(45), F(49));
            bool locked = false; int diff = 0;
            for (int i = 53; i + 12 <= b.Length; i++)
                if (b[i] == 0 && b[i + 1] == 0 && b[i + 2] == 0x2F && b[i + 3] == 0x4C) { locked = (b[i + 11] & 0x40) != 0; break; }
            _doors[id] = new DoorInfo { Pos = pos, Locked = locked, Difficulty = diff };
            if (locked) _ctx.Log($"MISSION: locked door {id} at ({pos.X:0},{pos.Y:0},{pos.Z:0}).");
        }

        public void OnDoorActionRaw(byte[] b)
        {
            // ActionMessage raw: identity at 20 (the door), field mask at 29, action at 33, instigator at 37.
            if (b == null || b.Length < 37 || BE32(b, 20) != DoorType || BE32(b, 33) != ActionUnlocked) return;
            var id = new Identity((IdentityType)DoorType, BE32(b, 24));
            if (_doors.TryGetValue(id, out var d)) { if (d.Locked) _pickedDoors.Add(id); d.Locked = false; }
            // A picked door opens rooms the clear could not reach: at 21:39-21:40 (2026-09-25) clear gave up at 40%,
            // then the walk to the objective picked a locked door and the rooms behind it were never cleared.
            if (ClearMode && _clearGaveUp && !_completed)
            {
                _clearGaveUp = false; _clearPasses = 0; _clearVisited.Clear();
                _ctx.Log("MISSION: a door opened; back to clearing.");
            }
            _ctx.Log($"MISSION: door {id} unlocked (action {ActionUnlocked}).");
        }

        private Identity? LockedDoorNear(Vector3 pos, float r)
        {
            foreach (var kv in _doors)
                if (kv.Value.Locked && !_unpickable.Contains(kv.Key) && Movement.Flat(pos, kv.Value.Pos) < r && Math.Abs(pos.Y - kv.Value.Pos.Y) < 3f)
                    return kv.Key;
            return null;
        }

        private void PickTick(LocalPlayer me)
        {
            _move.Hold(me, _ctx.Config.SendIntervalMs);
            if (!_pickDoor.HasValue || !_doors.TryGetValue(_pickDoor.Value, out var door)) { Enter(_pickReturn, "the door is gone"); return; }
            if (!door.Locked)
            {
                _ctx.Log($"MISSION: picked {_pickDoor} in {_pickTries} tr{(_pickTries == 1 ? "y" : "ies")}.");
                Enter(_pickReturn, "door unlocked");
                return;
            }
            if (Now - _pickAt < 2.5) return;
            var pick = Inventory.Items.FirstOrDefault(i => i != null && i.Slot.Type == IdentityType.Inventory && LockPicks.Contains(i.Id));
            if (pick == null || _pickTries >= PickTries)
            {
                if (pick == null && !_noPickTold) { _noPickTold = true; _tell("A locked door and no lock pick in my inventory (Fair Trade tools terminal sells them); going round it."); }
                _ctx.Log($"MISSION: giving up on {_pickDoor} ({(pick == null ? "no lock pick" : _pickTries + " tries")}); blocking it.");
                _unpickable.Add(_pickDoor.Value);
                if (_grid.CellOf(door.Pos) is (int, int, int) c) _blocked.Add(c);
                Enter(_pickReturn, "round the locked door");
                return;
            }
            _pickAt = Now; _pickTries++;
            Client.Send(new LookAtMessage { Target = _pickDoor.Value, ReturnInfo = 0 });
            Client.Send(new GenericCmdMessage { Action = GenericCmdAction.UseItemOnItem, User = me.Identity, Source = pick.Slot, Target = _pickDoor.Value, Count = 1, Temp4 = 1 });
            _ctx.WalkState = $"mission: picking a lock, try {_pickTries}";
            if (_pickTries == 1 || _pickTries % 10 == 0) _ctx.Log($"MISSION: picking {_pickDoor} with {pick.Name}, try {_pickTries}.");
        }

        // ---- clear mode -----------------------------------------------------------------------------------
        // Kill every mob in the building before the objective: the XP, and for Omni and Clan a side token with
        // the reward. The server counts it for us: after each kill, FormatFeedback category 110 message 79979934
        // with one float, the share of the building's mobs dead (capture 20260925-110325: 38.5, 46.2 ... 100 in
        // steps of 1/13; at 100 the owner completed the find person and the Omni-Tek Mission Token came with the
        // reward, TemplateAction 87 into the overflow window). The person to find is not counted. The server only
        // sends mobs near us (61 spawns, 175 despawns in capture 20260923-114223), so every room gets walked.
        public bool ClearMode;
        public float ClearPct { get; private set; } = -1;
        public Func<NpcChar, bool> Fightable = _ => true;
        public bool Clearing => ClearMode && _grid != null && _record != null && !_completed && !_clearGaveUp && ClearPct < 99.9f;
        private readonly HashSet<int> _clearVisited = new HashSet<int>();
        private int _clearInstance, _clearPasses;
        private bool _clearGaveUp;
        private const int ClearCategory = 110, ClearMessage = 79979934;

        private static string RawFormatted(byte[] b)
        {
            if (b == null) return null;
            for (int i = 16; i + 1 < b.Length; i++)
                if (b[i] == (byte)'~' && b[i + 1] == (byte)'&')
                {
                    int e = i;
                    while (e < b.Length && b[e] >= 0x20 && b[e] < 0x7F) e++;
                    return System.Text.Encoding.ASCII.GetString(b, i, e - i);
                }
            return null;
        }

        /// <summary>'~&' + category and message id (5 base-85 chars each) + 'f' + a base-85 float.</summary>
        public static bool TryClearPct(string s, out float pct)
        {
            pct = 0;
            if (s == null || s.Length < 18 || s[0] != '~' || s[1] != '&') return false;
            if (B85(s, 2) != ClearCategory || B85(s, 7) != ClearMessage || s[12] != 'f') return false;
            pct = BitConverter.ToSingle(BitConverter.GetBytes((int)(uint)B85(s, 13)), 0);
            return pct >= 0 && pct <= 100.01f;
        }
        private static long B85(string s, int p)
        {
            long v = 0;
            for (int i = 0; i < 5; i++) { int c = s[p + i] - 33; if (c < 0 || c > 84) return -1; v = v * 85 + c; }
            return v;
        }

        /// <summary>The next point to walk to on the building's path from a to b (not through a wall), or null.</summary>
        public Vector3? StepToward(Vector3 a, Vector3 b)
        {
            if (_grid == null) return null;
            var p = PathFrom(a, b, out _);
            if (p == null || p.Count == 0) return null;
            foreach (var q in p) if (Movement.Flat(a, q) > 1.5f) return q;
            return b;
        }

        // Where he stands after a walk-up to a mob can be somewhere no path starts from (01:35, 2026-09-26, and 20:50
        // the night before: after a fight every room, the target and even the exit had 'no walkable path', and the
        // mission was dropped). Then plan from the last spot a path did start from, within 25 m, and walk back to it.
        private Vector3? _lastPathFrom;
        private List<Vector3> PathFrom(Vector3 a, Vector3 b, out bool usedFallback)
        {
            var p = _grid.FindPath(a, b, _blocked, out usedFallback);
            if (p != null) { _lastPathFrom = a; return p; }
            if (_lastPathFrom.HasValue)
            {
                float back = Movement.Flat(a, _lastPathFrom.Value);
                if (back > 0.5f && back < 25f)
                {
                    var q = _grid.FindPath(_lastPathFrom.Value, b, _blocked, out usedFallback);
                    if (q != null)
                    {
                        if (Now - _pathFromLogged > 10) { _pathFromLogged = Now; _ctx.Log($"MISSION: no path from where I stand; going back {back:0} m to ({_lastPathFrom.Value.X:0},{_lastPathFrom.Value.Z:0}) and on from there."); }
                        q.Insert(0, _lastPathFrom.Value); q.Insert(0, a);
                        return q;
                    }
                }
            }
            return null;
        }
        private double _pathFromLogged = -99;

        /// <summary>A path length through the building, or null (no building, no path).</summary>
        public float? PathLen(Vector3 a, Vector3 b)
        {
            if (_grid == null) return null;
            var p = PathFrom(a, b, out _);
            if (p == null) return null;
            float len = 0; for (int i = 1; i < p.Count; i++) len += Vector3.Distance(p[i - 1], p[i]);
            return len;
        }

        private string ClearText => ClearPct < 0 ? "no kill counted yet" : $"{ClearPct:0.#}% cleared";

        private Hop? ClearHop(Vector3 pos, int floor, out string why)
        {
            foreach (var r in _grid.RoomsOn(floor)) if (Movement.Flat(pos, r.Centre) < 6f) _clearVisited.Add(r.Index);
            // 1) the nearest room on this floor not walked yet
            Hop? best = null; float bestLen = float.MaxValue; string bestName = null;
            foreach (var r in _grid.RoomsOn(floor))
            {
                if (_clearVisited.Contains(r.Index)) continue;
                var p = PathFrom(pos, r.Centre, out _);
                if (p == null) { _clearVisited.Add(r.Index); continue; }
                float len = 0; for (int i = 1; i < p.Count; i++) len += Vector3.Distance(p[i - 1], p[i]);
                if (len < bestLen) { bestLen = len; bestName = r.Name; best = new Hop { Pos = r.Centre, Purpose = Purpose.Clear }; }
            }
            if (best != null) { why = $"clearing ({ClearText}): room '{bestName}' on floor {floor}"; return best; }
            // 2) a live mob in sight on this floor (a patrol, or one standing between rooms)
            var me = DynelManager.LocalPlayer;
            foreach (var n in DynelManager.Npcs.Where(n => n != null && !n.Owner.HasValue && (me == null || !me.Pets.Any(pp => pp.Identity == n.Identity))
                                                          && (!n.TryGetStat(Stat.Health, out int h) || h > 0) && n.Identity != FindPersonTarget
                                                          && _grid.FloorAt(n.Transform.Position) == floor && Fightable(n))
                                              .OrderBy(n => Movement.Flat(pos, n.Transform.Position)).Take(3))
            {
                var len = PathLen(pos, n.Transform.Position);
                if (len.HasValue) { why = $"clearing ({ClearText}): '{n.Name}' {len:0} m off"; return new Hop { Pos = n.Transform.Position, Purpose = Purpose.Clear }; }
            }
            // 3) another floor with rooms left, by a button seen here
            foreach (int f in _grid.Floors.OrderBy(f => Math.Abs(f - floor)))
            {
                if (f == floor || _grid.RoomsOn(f).All(r => _clearVisited.Contains(r.Index))) continue;
                var bh = ButtonHop(pos, floor, f, out string bw);
                if (bh.HasValue) { why = $"clearing ({ClearText}): floor {f} has rooms left; {bw}"; return bh; }
            }
            // 4) all walked and still short of 100%: once more round (mobs wander into walked rooms), then give up.
            if (_clearPasses == 0)
            {
                _clearPasses = 1; _clearVisited.Clear();
                _ctx.Log($"MISSION: walked every room I can reach, {ClearText}; one more round.");
                return ClearHop(pos, floor, out why);
            }
            _clearGaveUp = true;
            _ctx.Log($"MISSION: clear mode gave up at {ClearText} after two rounds of every reachable room; on to the objective.");
            _tell($"Couldn't clear this one ({ClearText}); doing the objective.");
            why = "clear mode gave up";
            return null;
        }

        private Hop? SearchHop(Vector3 pos, int floor, string tw, out string why)
        {
            foreach (var r in _grid.RoomsOn(floor)) if (Movement.Flat(pos, r.Centre) < 6f) _visited.Add(r.Index);
            Hop? best = null; float bestLen = float.MaxValue; string bestName = null;
            foreach (var r in _grid.RoomsOn(floor))
            {
                if (_visited.Contains(r.Index)) continue;
                var p = PathFrom(pos, r.Centre, out _);
                if (p == null) { _visited.Add(r.Index); continue; }
                float len = 0; for (int i = 1; i < p.Count; i++) len += Vector3.Distance(p[i - 1], p[i]);
                if (len < bestLen) { bestLen = len; bestName = r.Name; best = new Hop { Pos = r.Centre, Purpose = Purpose.Search }; }
            }
            if (best == null)
            {
                why = $"searched every room on floor {floor} and the target never showed ({tw})";
                _ctx.Log($"MISSION: gave up searching; target {(_record?.TargetA?.ToString() ?? "-")}; everything seen ({_items.Count}): "
                         + string.Join(", ", _items.Select(kv => $"{kv.Key} tpl {kv.Value.Template} '{ItemName(kv.Value.Template)}' at ({kv.Value.Pos.X:0},{kv.Value.Pos.Z:0})")));
                return null;
            }
            why = $"searching: target not in sight yet ({tw}), trying room '{bestName}'";
            return best;
        }

        // ---- offline test hooks (the harness replays captures through the planner; nothing calls these live)
        /// <summary>The person a find-person mission sends us to, if this building's record names one.</summary>
        public Identity? FindPersonTarget => _record != null && _record.TypeName == "find person" ? _record.TargetA : null;

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
            if (type != TypeFindPerson && type != TypeFindItem && type != TypeReturnItem) { how = _record.TypeName + " is not supported yet"; return null; }

            if (_record.TargetA.HasValue)
            {
                how = "the quest record's target";
                if (_record.TargetA.HasValue && (int)_record.TargetA.Value.Type == 0xC74E)
                {
                    // An item-type reference: the target is the seen item carrying that template.
                    foreach (var kv in _items)
                        if (kv.Value.Template == _record.TargetA.Value.Instance) { pos = kv.Value.Pos; return kv.Key; }
                    var holder = ContainerHolding(_record.TargetA.Value.Instance);
                    if (holder.HasValue && _items.TryGetValue(holder.Value, out var hc))
                    {
                        how = $"the container {holder.Value} holds item {_record.TargetA.Value.Instance}";
                        pos = hc.Pos; return holder.Value;
                    }
                    pos = null; return null;
                }
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
            if (Now - _itemsLoggedAt > 20)
            {
                _itemsLoggedAt = Now;
                _ctx.Log($"MISSION: items in sight ({candidates.Count}): {string.Join(", ", candidates.Select(kv => $"'{ItemName(kv.Value.Template)}' tpl {kv.Value.Template} at ({kv.Value.Pos.X:0},{kv.Value.Pos.Y:0},{kv.Value.Pos.Z:0})"))}");
            }
            return null;
        }

        private bool Locate(Identity? id, out Vector3? pos)
        {
            pos = null;
            if (!id.HasValue) return false;
            if (_items.TryGetValue(id.Value, out var it)) { pos = it.Pos; return true; }   // items and containers
            // 0xC74E is an item-TYPE reference (OmniCell RecordData: item records start 0xC74E), not an object in
            // the building: the record names WHICH item to find (e.g. FF865 = item 1046629, 'Leg Implant: Swimming,
            // Shiny'). The object is whatever seen item carries that template.
            if ((int)id.Value.Type == 0xC74E)
            {
                foreach (var kv in _items)
                    if (kv.Value.Template == id.Value.Instance) { pos = kv.Value.Pos; return true; }
            }
            if (DynelManager.Find(id.Value, out SimpleChar c)) { pos = c.Transform.Position; return true; }
            return false;
        }

        private double _actStill;
        private Identity? _offFloorLogged;                // seconds stood still in Phase.Act

        /// <summary>
        /// Find item: the route ends on the open cell nearest the item and arrives within 2.5 m of that, which can
        /// leave us several metres off it. The owner selected it from 2.7 m (capture 20260924-074524 s9, at
        /// (52.9,5.7,143.9), item at (53.5,5.1,146.5)). So walk straight at it until within 2 m, for at most 5 s -
        /// the last metres need not be on the grid. True while still walking.
        /// </summary>
        private bool ApproachItem(LocalPlayer me, double dt)
        {
            FindTarget(out Vector3? tp, out _);
            if (!tp.HasValue) return false;
            Vector3 pos = me.MovementComponent.Position;
            float d = Movement.Flat(pos, tp.Value);
            if (d <= 2f || _phaseTime > 5) return false;
            Vector3 dir = new Vector3(tp.Value.X - pos.X, 0, tp.Value.Z - pos.Z).Normalize();
            float step = Movement.CappedStep(_ctx.RunVelocity(me), dt, _ctx.Config.MaxStep, d - 1.5f);
            Vector3 next = new Vector3(pos.X + dir.X * step, pos.Y, pos.Z + dir.Z * step);
            next = new Vector3(next.X, _grid.HeightAt(next, pos.Y) ?? pos.Y, next.Z);
            _ctx.WalkState = $"mission: up to the item d={d:0.0}";
            _move.Advance(me, next, Movement.SafeLook(dir, me.MovementComponent.Heading), run: true, dt, _ctx.Config.SendIntervalMs);
            _actStill = 0;
            return true;
        }

        private void DoObjective(LocalPlayer me)
        {
            var target = FindTarget(out Vector3? tpos, out string how);
            if (!target.HasValue) { Enter(Phase.Plan, "lost sight of the target (" + how + ")"); return; }
            if (tpos.HasValue) how += $", {Movement.Flat(me.MovementComponent.Position, tpos.Value):0.0} m away";
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
                case TypeReturnItem:
                    // Picked up with a double click: LookAt, then GenericCmd Use on the item (owner, capture
                    // 20260926-135805 s10 seq 436/437, 14:10:01; the server answered ContainerAddItem from the item into
                    // the inventory). A container holding it is opened first, as for find item.
                    if ((int)target.Value.Type == (int)IdentityType.Container) GameCommands.OpenContainer(me, target.Value);
                    Client.Send(new LookAtMessage { Target = target.Value, ReturnInfo = 0 });
                    GameCommands.UseObject(me, target.Value);
                    _ctx.Log($"MISSION: picking up item {target.Value} to return ({how}).");
                    break;
                case TypeFindItem:
                    // One LookAt with ReturnInfo=0 on the floor item (capture 20260923-125821 s4 13:04:11.152;
                    // again 20260924-074524 s9 08:01:45.575, completion 165 ms later, nothing opened or picked up).
                    // A container holding the item is opened first (GenericCmd Use, the way the owner opened his
                    // backpack in capture 20260923-201746), then selected. UNVERIFIED which of the two completes it.
                    if ((int)target.Value.Type == (int)IdentityType.Container)
                        GameCommands.OpenContainer(me, target.Value);
                    Client.Send(new LookAtMessage { Target = target.Value, ReturnInfo = 0 });
                    _ctx.Log($"MISSION: selected item {target.Value} ({how}).");
                    break;
                case TypeRepair:
                {
                    // GenericCmd UseItemOnItem: the tool from the inventory, then the object (capture
                    // 20260910-203534 client seq 1292). The quest record lists the tool first.
                    // Which bag item is the tool: the one whose identity the quest record names (it names two - the
                    // object in the building and the tool), else the item the mission text names ("use the Targeted
                    // Radiation Extractor to ..."). The first version looked the tool up among items seen lying in
                    // the building and never found it (2026-09-23 21:40, three blitzes in a row).
                    var bag = Inventory.Items.Where(i => i != null && i.Slot.Type == IdentityType.Inventory).ToList();
                    var inv = bag.FirstOrDefault(i => (_record.TargetA.HasValue && i.UniqueIdentity == _record.TargetA.Value)
                                                   || (_record.TargetB.HasValue && i.UniqueIdentity == _record.TargetB.Value));
                    string text = _record.Text ?? "";
                    if (inv == null)
                        inv = bag.Where(i => !string.IsNullOrEmpty(i.Name) && i.Name.Length >= 6 && text.IndexOf(i.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                                 .OrderByDescending(i => i.Name.Length).FirstOrDefault();
                    if (inv == null)
                    {
                        _ctx.Log($"MISSION: no bag item matches the tool; record names {_record.TargetA} / {_record.TargetB}; bag: {string.Join(", ", bag.Select(i => $"'{i.Name}' {i.UniqueIdentity}"))}");
                        Fail("the repair tool is not in my inventory"); return;
                    }
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

            // Knocked off the path (a server correction, a ride we did not ask for): plan again. Measured from
            // the leg we are walking, not from its far end: smoothed legs run 20 m and more, and measuring to
            // the waypoint replanned every frame the moment the first point was reached, so the bot crept cell
            // by cell with a stop/start each frame (log 2026-09-23 22:11:52, "wp 2/6 d=23.4" for seconds).
            Vector3 legFrom = _path[Math.Max(0, _pathIndex - 1)], legTo = _path[Math.Min(_pathIndex, _path.Count - 1)];
            if (FlatToSegment(pos, legFrom, legTo) > 6f || Math.Abs(pos.Y - legTo.Y) > 6f)
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
            float d = Movement.Flat(pos, wp);
            bool last = _pathIndex == _path.Count - 1;
            // Corners are passed tightly: each step is clamped to land on the waypoint anyway, and a wide
            // radius cuts the corner into the door frame the waypoint was placed to clear.
            float arrive = last ? ArriveRadius() : 0.3f;
            if (d <= arrive)
            {
                _pathIndex++;
                _stuck.Reset();
                if (_pathIndex >= _path.Count) { Arrive(me); return true; }
                wp = _path[_pathIndex]; d = Movement.Flat(pos, wp);
            }

            // A LOCKED DOOR ahead (within 3.5 m, same level): stop and pick it.
            var locked = LockedDoorNear(pos, 3.5f);
            if (locked.HasValue)
            {
                _pickDoor = locked; _pickTries = 0; _pickAt = -9999; _pickReturn = _completed ? Phase.Exit : Phase.Plan;
                _path = null;
                Enter(Phase.PickLock, $"locked door {locked.Value} (lock difficulty {_doors[locked.Value].Difficulty})");
                return true;
            }

            // Stuck: no progress on this waypoint for 3 s means something the tiles do not show is in the
            // way. Block that cell and plan around it.
            if (_stuck.Tick(d, dt, 0.3f, 3.0))
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
            float step = Movement.CappedStep(_ctx.RunVelocity(me), dt, _ctx.Config.MaxStep, d);
            Vector3 next = new Vector3(pos.X + dir.X * step, pos.Y, pos.Z + dir.Z * step);
            float? y = _grid.HeightAt(next, pos.Y);
            next = new Vector3(next.X, y ?? wp.Y, next.Z);
            _ctx.WalkState = $"mission({_purpose}) wp {_pathIndex + 1}/{_path.Count} d={d:0.0}";
            _move.Advance(me, next, Movement.SafeLook(dir, me.MovementComponent.Heading), run: true, dt, _ctx.Config.SendIntervalMs);
            return true;
        }

        private float ArriveRadius() => _purpose == Purpose.Button ? 1.5f : _purpose == Purpose.Target ? 2.5f : _purpose == Purpose.Search || _purpose == Purpose.Clear ? 3f : 2.0f;

        private void Arrive(LocalPlayer me)
        {
            _move.Hold(me, _ctx.Config.SendIntervalMs);
            _path = null;
            switch (_purpose)
            {
                case Purpose.Button: Enter(Phase.PressButton, "at the button"); break;
                case Purpose.Target: Enter(Phase.Act, "at the target"); break;
                case Purpose.Search: Enter(Phase.Plan, "searched a room"); break;
                case Purpose.Clear: Enter(Phase.Plan, "cleared a room"); break;
                case Purpose.Entrance:
                    if (_nav.Exit == null)
                    {
                        _tell("Back at the mission entrance. Mission mode off.");
                        _ctx.Log("MISSION: back at the entrance, blitz done.");
                        _phase = Phase.Done;
                        break;
                    }
                    Enter(Phase.PushOut, "at the exit door, walking through it");
                    _pushed = 0;
                    break;
            }
        }

        private static float FlatToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            float abx = b.X - a.X, abz = b.Z - a.Z, len2 = abx * abx + abz * abz;
            float t = len2 < 1e-6f ? 0f : Math.Max(0f, Math.Min(1f, ((p.X - a.X) * abx + (p.Z - a.Z) * abz) / len2));
            return Movement.Flat(p, new Vector3(a.X + abx * t, a.Y, a.Z + abz * t));
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
                    // Items (0xC73D), characters (0xC350), and containers (0xC749, 0xC74E): a find-item target can be
                    // a container (quest update saved 2026-09-23 22:10: record target 0xC74E:FF866).
                    if ((t == 0xC73D || t == 0xC350 || t == 0xC749 || t == 0xC74E) && inst != 0 && inst != holder) { ids.Add(new Identity((IdentityType)t, inst)); j += 4; }
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

        // Floor cells touching a wall cell. The 2 m grid places a doorway's frame only to the cell, and the
        // server collides with the real frame: a route along the west edge of a 12 m opening was snapped back
        // to the same corner nine times running (log 2026-09-23 22:40:53, (70,138,181)). So routes pay extra
        // for edge cells, which keeps them in the middle of openings, and smoothing may not cut across an edge
        // cell the route itself avoided. Narrow passages are all edge; the route then uses them as it must.
        private readonly HashSet<(int, int, int)> _edge = new HashSet<(int, int, int)>();
        private const float EdgeCost = 2f;

        public static MissionGrid Build(AOBuddyNav nav, Dictionary<Identity, DoorInfo> doors)
        {
            var g = new MissionGrid();
            g.Doors = doors;
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
            foreach (var k in g._walk.Keys)
                for (int dx = -1; dx <= 1 && !g._edge.Contains(k); dx++)
                    for (int dz = -1; dz <= 1; dz++)
                        if (!g._walk.ContainsKey((k.Item1, k.Item2 + dx, k.Item3 + dz))) { g._edge.Add(k); break; }
            if (nav.Walls != null && nav.Walls.Length >= 9) g.BuildWalls(nav.Walls);
            return g;
        }

        public Dictionary<Identity, DoorInfo> Doors { get; set; }

        public sealed class RoomSpot { public int Index; public string Name; public int Floor; public Vector3 Centre; }
        private readonly List<RoomSpot> _spots = new List<RoomSpot>();
        public IEnumerable<RoomSpot> RoomsOn(int floor) => _spots.Where(r => r.Floor == floor);

        private static float kv0(List<(int tile, float y, string room)> l) { foreach (var c in l) if (c.tile != 0) return c.y; return l[0].y; }

        // ---- real walls (walls.bin) ---------------------------------------------------------------------
        // The tile grid cannot tell a doorway from a wall where two rooms meet: on the 2026-09-23 Ventil
        // mission it joined two corridors through a solid wall and sent the bot into the wrong room's door,
        // and the server stopped it on the wall every time. With the pool's wall
        // triangles placed into the mission (AOBuddyNav.PlaceWalls) the planner works on 0.5 m cells
        // instead: a cell is open when the 2 m tile model has floor there (or a shared doorway cell) and no
        // wall, sliced 1 m above that floor, comes within the body's radius of the cell's centre. A doorway
        // is simply where the wall has no triangles (1.6 m wide in that pool; the lintel at 3 m is above
        // the slice), and the radius keeps the route off the frames the server collides with.
        private const float Fine = 0.5f, BodyRadius = 0.35f, CutAbove = 1.0f;
        private HashSet<(int, int, int)> _fine;                  // open 0.5 m cells: (floor, fx, fz)
        private float[] _wallTris;
        private Dictionary<(int, int), List<int>> _wallHash;     // 2 m column -> wall triangle indices
        private int FinePer => (int)Math.Round(Cell / Fine);

        private void BuildWalls(float[] tris)
        {
            _wallTris = tris;
            _wallHash = new Dictionary<(int, int), List<int>>();
            for (int t = 0; t + 9 <= tris.Length; t += 9)
            {
                int x0 = (int)Math.Floor(Math.Min(tris[t], Math.Min(tris[t + 3], tris[t + 6])) / Cell), x1 = (int)Math.Floor(Math.Max(tris[t], Math.Max(tris[t + 3], tris[t + 6])) / Cell);
                int z0 = (int)Math.Floor(Math.Min(tris[t + 2], Math.Min(tris[t + 5], tris[t + 8])) / Cell), z1 = (int)Math.Floor(Math.Max(tris[t + 2], Math.Max(tris[t + 5], tris[t + 8])) / Cell);
                for (int x = x0; x <= x1; x++)
                    for (int z = z0; z <= z1; z++)
                    {
                        if (!_wallHash.TryGetValue((x, z), out var l)) _wallHash[(x, z)] = l = new List<int>();
                        l.Add(t);
                    }
            }
            // Floor for the fine layer: the tile model grown by one 2 m cell. The shared column of a doorway
            // often has no tile on either side (Ventil mission: the doorway between SmallB5 and BigB4 was
            // sealed that way), and a tile model 2 m coarse leaves floor near the walls uncovered. Growing
            // the floor is safe because the walls enclose each room: a cell grown past a room's edge is only
            // reachable through a real opening. A grown cell takes the height of the nearest real one.
            _fineY = new Dictionary<(int, int, int), float>();
            foreach (var k in _walk.Keys.Concat(_fallback.Keys)) _fineY[k] = ParentY(k);
            foreach (var k in _fineY.Keys.ToList())
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        var n = (k.Item1, k.Item2 + dx, k.Item3 + dz);
                        if (_fineY.ContainsKey(n)) continue;
                        (int, int, int)? src = null; int bestD = int.MaxValue;
                        for (int ex = -1; ex <= 1; ex++)
                            for (int ez = -1; ez <= 1; ez++)
                            {
                                var m = (n.Item1, n.Item2 + ex, n.Item3 + ez);
                                int dd = ex * ex + ez * ez;
                                if (dd > 0 && dd < bestD && (_walk.ContainsKey(m) || _fallback.ContainsKey(m))) { bestD = dd; src = m; }
                            }
                        if (src.HasValue) _fineY[n] = ParentY(src.Value);
                    }
            _fine = new HashSet<(int, int, int)>();
            int per = FinePer;
            foreach (var k in _fineY.Keys)
            {
                float y = _fineY[k];
                var segs = WallSegments(k.Item2 - 1, k.Item3 - 1, k.Item2 + 1, k.Item3 + 1, y + CutAbove);
                for (int sx = 0; sx < per; sx++)
                    for (int sz = 0; sz < per; sz++)
                    {
                        int fx = k.Item2 * per + sx, fz = k.Item3 * per + sz;
                        float cx = (fx + 0.5f) * Fine, cz = (fz + 0.5f) * Fine;
                        bool clear = true;
                        foreach (var sg in segs) if (DistToSeg(cx, cz, sg) < BodyRadius) { clear = false; break; }
                        if (clear) _fine.Add((k.Item1, fx, fz));
                    }
            }
        }

        private float ParentY((int, int, int) k) => _walk.TryGetValue(k, out float y) ? y : _fallback.TryGetValue(k, out y) ? y : 0f;
        private (int, int, int) ParentOf((int, int, int) f) => (f.Item1, FloorDiv(f.Item2, FinePer), FloorDiv(f.Item3, FinePer));
        private static int FloorDiv(int a, int b) => (int)Math.Floor(a / (double)b);
        private Dictionary<(int, int, int), float> _fineY;        // floor height per 2 m cell, grown (see BuildWalls)
        private float FineY((int, int, int) f) => _fineY.TryGetValue(ParentOf(f), out float y) ? y : 0f;
        private Vector3 FineCentre((int, int, int) f) => new Vector3((f.Item2 + 0.5f) * Fine, FineY(f), (f.Item3 + 0.5f) * Fine);

        /// <summary>The walls in 2 m columns [cx0..cx1] x [cz0..cz1], sliced by the plane y = cut: (x1, z1, x2, z2) each.</summary>
        private List<float[]> WallSegments(int cx0, int cz0, int cx1, int cz1, float cut)
        {
            var seen = new HashSet<int>(); var segs = new List<float[]>();
            float[] w = _wallTris;
            for (int x = cx0; x <= cx1; x++)
                for (int z = cz0; z <= cz1; z++)
                {
                    if (!_wallHash.TryGetValue((x, z), out var l)) continue;
                    foreach (int t in l)
                    {
                        if (!seen.Add(t)) continue;
                        float px = 0, pz = 0; int got = 0; var seg = new float[4];
                        for (int e = 0; e < 3 && got < 2; e++)
                        {
                            int p0 = t + e * 3, p1 = t + ((e + 1) % 3) * 3;
                            float y0 = w[p0 + 1] - cut, y1 = w[p1 + 1] - cut;
                            if ((y0 < 0) == (y1 < 0)) continue;
                            float s = y0 / (y0 - y1);
                            px = w[p0] + (w[p1] - w[p0]) * s; pz = w[p0 + 2] + (w[p1 + 2] - w[p0 + 2]) * s;
                            seg[got * 2] = px; seg[got * 2 + 1] = pz; got++;
                        }
                        if (got == 2) segs.Add(seg);
                    }
                }
            return segs;
        }

        private static float DistToSeg(float px, float pz, float[] s)
        {
            float ax = s[0], az = s[1], bx = s[2] - ax, bz = s[3] - az, l2 = bx * bx + bz * bz;
            float t = l2 < 1e-9f ? 0f : Math.Max(0f, Math.Min(1f, ((px - ax) * bx + (pz - az) * bz) / l2));
            float dx = px - (ax + bx * t), dz = pz - (az + bz * t);
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        private static float Orient(float ax, float az, float bx, float bz, float px, float pz) => (bx - ax) * (pz - az) - (bz - az) * (px - ax);

        /// <summary>True when a wall at body height stands between two points on the same floor.</summary>
        public bool WallBetween(Vector3 a, Vector3 b)
        {
            if (_wallTris == null) return false;
            float cut = Math.Min(a.Y, b.Y) + CutAbove;
            var segs = WallSegments((int)Math.Floor(Math.Min(a.X, b.X) / Cell), (int)Math.Floor(Math.Min(a.Z, b.Z) / Cell),
                                    (int)Math.Floor(Math.Max(a.X, b.X) / Cell), (int)Math.Floor(Math.Max(a.Z, b.Z) / Cell), cut);
            foreach (var s in segs)
            {
                float d1 = Orient(s[0], s[1], s[2], s[3], a.X, a.Z), d2 = Orient(s[0], s[1], s[2], s[3], b.X, b.Z);
                float d3 = Orient(a.X, a.Z, b.X, b.Z, s[0], s[1]), d4 = Orient(a.X, a.Z, b.X, b.Z, s[2], s[3]);
                if ((d1 > 0) != (d2 > 0) && (d3 > 0) != (d4 > 0)) return true;
            }
            return false;
        }

        private bool FineOpen((int, int, int) f, HashSet<(int, int, int)> blocked) =>
            _fine.Contains(f) && (blocked == null || !blocked.Contains(ParentOf(f)));

        // The open cell nearest p that p can see (no wall between): a button hangs on a wall, and the cell
        // straight behind that wall is nearer than the one in front of it. Not for the start: the bot is where
        // it is, and standing 0.3 m off a wall (where the server put it) every line crossed a wall face.
        private (int, int, int)? NearestFine(Vector3 p, int floor, float maxR, HashSet<(int, int, int)> blocked, bool needSight = true)
        {
            int px = (int)Math.Floor(p.X / Fine), pz = (int)Math.Floor(p.Z / Fine), rmax = (int)Math.Ceiling(maxR / Fine);
            (int, int, int)? best = null; float bd = float.MaxValue;
            for (int r = 0; r <= rmax; r++)
            {
                if (best.HasValue && (r - 1) * Fine > bd) break;
                for (int dx = -r; dx <= r; dx++)
                    for (int dz = -r; dz <= r; dz++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dz)) != r) continue;
                        var f = (floor, px + dx, pz + dz);
                        if (!FineOpen(f, blocked)) continue;
                        var c = FineCentre(f);
                        if (Math.Abs(c.Y - p.Y) > 3f) continue;
                        float d = (float)Math.Sqrt((c.X - p.X) * (c.X - p.X) + (c.Z - p.Z) * (c.Z - p.Z));
                        if (d >= bd || d > maxR) continue;
                        if (needSight && WallBetween(new Vector3(p.X, c.Y, p.Z), c)) continue;
                        bd = d; best = f;
                    }
            }
            return best;
        }

        private List<Vector3> FindPathWalls(Vector3 a, Vector3 b, int floor, HashSet<(int, int, int)> blocked, out bool usedFallback)
        {
            usedFallback = false;
            var s = NearestFine(a, floor, 2.5f, blocked, needSight: false);
            // Where he stands can be off the open cells or inside the cells snap-backs blocked round him - after a
            // walk-up to a mob, 20:50 2026-09-25: every room 'unreachable' at once, clear and search gave up in the
            // same tick and the mission was dropped. The start is only where the walk begins: look wider for it,
            // blocked cells allowed.
            if (!s.HasValue) s = NearestFine(a, floor, 6f, null, needSight: false);
            var g = NearestFine(b, floor, 3.0f, blocked);
            if (!s.HasValue || !g.HasValue) return null;
            var cells = FineAStar(s.Value, g.Value, blocked);
            if (cells == null) return null;
            usedFallback = cells.Any(c => !_walk.ContainsKey(ParentOf(c)));
            // Ends on the open cell nearest the goal, not the goal itself: a button or item often sits on
            // or against a wall, and walking into the wall is what the server stops.
            return SmoothFine(cells, blocked);
        }

        private List<(int, int, int)> FineAStar((int, int, int) s, (int, int, int) g, HashSet<(int, int, int)> blocked)
        {
            var open = new PriorityQueue<(int, int, int), float>();
            var cost = new Dictionary<(int, int, int), float> { [s] = 0 };
            var prev = new Dictionary<(int, int, int), (int, int, int)>();
            float H((int, int, int) k) => (float)Math.Sqrt((k.Item2 - g.Item2) * (k.Item2 - g.Item2) + (k.Item3 - g.Item3) * (k.Item3 - g.Item3));
            open.Enqueue(s, H(s));
            int guard = 0;
            while (open.Count > 0 && guard++ < 400000)
            {
                var k = open.Dequeue();
                if (k.Equals(g))
                {
                    var path = new List<(int, int, int)> { k };
                    while (prev.TryGetValue(k, out var p)) { k = p; path.Add(k); }
                    path.Reverse();
                    return path;
                }
                float c0 = cost[k], y0 = FineY(k);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        var n = (k.Item1, k.Item2 + dx, k.Item3 + dz);
                        if (!FineOpen(n, blocked)) continue;
                        if (Math.Abs(FineY(n) - y0) > MaxStepUp) continue;
                        if (dx != 0 && dz != 0 && (!FineOpen((k.Item1, k.Item2 + dx, k.Item3), blocked) || !FineOpen((k.Item1, k.Item2, k.Item3 + dz), blocked))) continue;
                        // Shared doorway cells are real floor once the walls decide; a small premium keeps
                        // routes on tiles where both are open.
                        float cc = _walk.ContainsKey(ParentOf(n)) ? 1f : 1.5f;
                        float nc = c0 + (dx != 0 && dz != 0 ? 1.4142f : 1f) * cc;
                        if (cost.TryGetValue(n, out float old) && old <= nc) continue;
                        cost[n] = nc; prev[n] = k;
                        open.Enqueue(n, nc + H(n));
                    }
            }
            return null;
        }

        // String-pull forward: from each kept cell, go as far along the route as a straight line stays on
        // open cells (the body-radius margin is already in 'open').
        private List<Vector3> SmoothFine(List<(int, int, int)> cells, HashSet<(int, int, int)> blocked)
        {
            var pts = new List<Vector3> { FineCentre(cells[0]) };
            int i = 0;
            while (i < cells.Count - 1)
            {
                int j = i + 1;
                while (j + 1 < cells.Count && ClearFine(cells[i], cells[j + 1], blocked)) j++;
                // if cell touches a doorway, use the center of the doorway to go through
                var door = Doors.Values.FirstOrDefault(x => Vector3.Distance(FineCentre(cells[j]), x.Pos) < 2f);
                if (door != null)
                {
                    pts.Add(door.Pos);
                }
                else
                {
                    pts.Add(FineCentre(cells[j]));
                }
                i = j;
            }
            return pts;
        }

        private bool ClearFine((int, int, int) a, (int, int, int) b, HashSet<(int, int, int)> blocked)
        {
            float ax = a.Item2 + 0.5f, az = a.Item3 + 0.5f, bx = b.Item2 + 0.5f, bz = b.Item3 + 0.5f;
            int n = (int)Math.Ceiling(Math.Max(Math.Abs(bx - ax), Math.Abs(bz - az)) * 4) + 1;
            float lastY = FineY(a);
            var lastCell = a;
            for (int s = 1; s <= n; s++)
            {
                float t = s / (float)n;
                var k = (a.Item1, (int)Math.Floor(ax + (bx - ax) * t), (int)Math.Floor(az + (bz - az) * t));
                if (k.Equals(lastCell)) continue;
                if (!FineOpen(k, blocked)) return false;
                float y = FineY(k);
                if (Math.Abs(y - lastY) > MaxStepUp) return false;
                if (k.Item2 != lastCell.Item2 && k.Item3 != lastCell.Item3 &&
                    (!FineOpen((k.Item1, k.Item2, lastCell.Item3), blocked) || !FineOpen((k.Item1, lastCell.Item2, k.Item3), blocked))) return false;
                lastY = y; lastCell = k;
            }
            return true;
        }

        public string Describe() => $"{Floors.Count} floor(s) {string.Join(",", Floors)}, {_walk.Count} floor cells, boss room {(BossFloor.HasValue ? $"'{BossRoomName}' on floor {BossFloor}" : "none")}, "
            + (_fine != null ? $"walls: {_wallTris.Length / 9} triangles, {_fine.Count} open {Fine} m cells" : "no wall data (routing on the tile grid alone)");

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
            if (_fine != null) return FindPathWalls(a, b, s.Value.Item1, blocked, out usedFallback);
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
                        float nc = c0 + (dx != 0 && dz != 0 ? 1.4142f : 1f) * cc + (_edge.Contains(n) ? EdgeCost : 0f);
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
            var route = new HashSet<(int, int, int)>(cells);
            int i = 0;
            while (i < cells.Count - 1)
            {
                int j = cells.Count - 1;
                while (j > i + 1 && !Clear(cells[i], cells[j], blocked, route)) j--;
                pts.Add(Centre(cells[j]));
                i = j;
            }
            return pts;
        }

        private bool Clear((int, int, int) a, (int, int, int) b, HashSet<(int, int, int)> blocked, HashSet<(int, int, int)> route)
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
                if (_edge.Contains(k) && !route.Contains(k)) return false;
                if (Math.Abs(y - lastY) > MaxStepUp) return false;
                // A diagonal hop between samples must not cut a corner.
                if (k.Item2 != lastCell.Item2 && k.Item3 != lastCell.Item3 &&
                    (!_walk.ContainsKey((k.Item1, k.Item2, lastCell.Item3)) || !_walk.ContainsKey((k.Item1, lastCell.Item2, k.Item3)))) return false;
                lastY = y; lastCell = k;
            }
            return true;
        }
    }
    
    public sealed class DoorInfo { public Vector3 Pos; public bool Locked; public int Difficulty; }
}
