using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using Newtonsoft.Json;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOBuddy
{
    /// <summary>Settings for the key hand-off, kept in keyhand.json in the plugin folder ('keyhand ...' commands).</summary>
    public sealed class KeyHandSettings
    {
        public bool Enabled;
        /// <summary>Who gets the key copy. Empty = the bot's configured Owner.</summary>
        public string Owner = "";
        /// <summary>Mission rooms the bot can't walk (owner, 2026-09-27). A pool room matches by its exact name or by
        /// ending in '_' + the entry: the Subway - Ventil pool (351) names them Subway_ramp_2 and Subway_Vent1.</summary>
        public List<string> ProblemRooms = new List<string> { "ramp_2", "Vent1" };
        public double WaitMinutes = 20;      // for the owner to come to the door
        public double DoneMinutes = 30;      // inside, for his 'done'
        public double PeekMinutes = 0;       // 0 = only his 'here' tell; >0 = also step out this often to look for him
        /// <summary>The duplicator's template, for buying it (a shop line carries only template ids). Seeded from capture
        /// 20260926-135805 (FullCharacter slot 68: identity type 0xC76E MissionKeyDuplicator, ItemLowId 28564, item data
        /// 'Mission Key Duplicator'); re-learned from any duplicator seen in the inventory.</summary>
        public int DuplicatorItemId = 28564;
        /// <summary>Orders the terminals tried when buying it (name containing this first); all in reach are tried.</summary>
        public string ShopKeyword = "Tool";
    }

    /// <summary>
    /// KEY HAND-OFF (owner, 2026-09-27). A mission whose layout has a room the bot can't walk (KeyHandSettings.ProblemRooms)
    /// is shared with the owner: on the way in the bot copies the mission key with a Mission Key Duplicator, tells the
    /// owner the zone and door, waits inside at the door, steps out when he is there, trades him the copy, invites him to
    /// the team, goes back in and clears what it can reach while he hides, then waits inside for his 'done' and carries
    /// on as usual. Every message mirrors the owner's own capture 20260926-135805 (stream 4 = the giver, stream 0 = the
    /// receiver):
    ///   copy   GenericCmd UseItemOnItem Count 2 Temp4 1, Source Inventory:68 (the duplicator), Target Inventory:69 (the
    ///          key) - client seq 18, 14:01:01. The copy came as SimpleItemFullUpdate 'Mission key to ...' (identity
    ///          0xC76D, a new instance) into the overflow, then ContainerAddItem Overflow -> the next free slot (70).
    ///          The duplicator is not used up (still in slot 68 in the mission's FullCharacter, stream 10).
    ///   trade  InfoRequest + LookAt(ReturnInfo 1) on him (seq 31-32), Trade Open naming him (33), Trade AddItem naming
    ///          ourselves with Inventory:70 (36, +3.4 s), Trade action 3 with no partner (37, +0.9 s). The receiver sent 3
    ///          then 1 (stream 0, 14:01:50 / 14:01:53); the server passed us only his 1 (seq 254 'action 1, partner
    ///          him'), then we sent 1 with no partner (38, 14:01:54.8, +1.0 s) and the server closed it with action 4.
    ///   team   CharacterAction TeamRequest (0x1A) on him, Parameter2 1 (seq 41, 14:02:26) - SDK Team.Invite sends exactly
    ///          that; he answered 0x1C (stream 0, 14:02:29) and TeamMember rows followed.
    /// </summary>
    public partial class MissionRun
    {
        private enum KeyStep { None, Copy, WaitOwner, Outside, Trade, Invite, BackIn, Clearing, WaitDone }
        private KeyStep _key = KeyStep.None;
        private KeyHandSettings _khStore;
        private string KeyHandPath => Path.Combine(_pluginDir, "keyhand.json");
        private KeyHandSettings KH => _khStore ?? (_khStore = JsonStore.Load<KeyHandSettings>(KeyHandPath, _ctx.Log) ?? new KeyHandSettings());
        private void SaveKH() => JsonStore.Save(KeyHandPath, JsonConvert.SerializeObject(KH, Formatting.Indented), _ctx.Log);
        private string KhOwner => !string.IsNullOrWhiteSpace(KH.Owner) ? KH.Owner.Trim() : (_ctx.Config.Owner ?? "").Trim();

        private MissionInfo _keyFor;              // the mission a hand-off was considered for (once per mission)
        private KeyStep _keyBackTo;               // after walking back in: WaitOwner (a peek) or Clearing
        private bool _keyOut, _keyPeek, _keyHere, _keyDone, _keyToldOutside, _dupMoved;
        private double _keyAt, _keyWaitStart, _keyLastPeek, _keyUsedAt = -1, _dupShopAt = -9999;
        private int _keyTries;
        private HashSet<Identity> _keysBefore = new HashSet<Identity>();
        private Identity? _keyCopy;
        private Identity _keyCopySlot;
        private int _keyOwnerId;
        // trade
        private int _tradeStep, _tradeTries;
        private double _tradeAt;
        private bool _tradeOpened, _ownerFinal, _tradeDone, _tradeDeclined;
        // team
        private int _invites;
        private double _inviteAt;
        // shop
        private bool _shopToolOnly, _shopToolTried;

        /// <summary>While the bot is trading the key copy: Main keeps ResupplyController's owner-trade answers out of it.</summary>
        public bool KeyHandTrading => Active && _key == KeyStep.Trade;
        /// <summary>Out of the building for the hand-off and going back in (the recorder keeps the building's file open).</summary>
        private bool KeyAway => _keyOut || _key == KeyStep.Outside || _key == KeyStep.Trade || _key == KeyStep.Invite || _key == KeyStep.BackIn;

        private void KLog(string s) => _ctx.Log("KEYHAND: " + s);
        private void KTell(string s)
        {
            string to = KhOwner;
            KLog($"tell {to}: {s}");
            if (string.IsNullOrEmpty(to)) return;
            try { Client.Chat.SendPrivateMessage(to, s); } catch (Exception ex) { KLog("tell failed: " + ex.Message); }
        }
        private void KeyStepTo(KeyStep s, string why) { KLog($"{_key} -> {s} ({why})"); _key = s; _keyAt = _clock; }

        private static bool RoomMatches(string pool, string entry)
            => !string.IsNullOrEmpty(pool) && !string.IsNullOrWhiteSpace(entry)
               && (string.Equals(pool, entry.Trim(), StringComparison.OrdinalIgnoreCase) || pool.EndsWith("_" + entry.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <summary>The composed mission's pool rooms that are on the problem list (from the zone-in layout).</summary>
        private List<string> ProblemRoomsHere()
        {
            var rooms = _mission.NavDungeonRooms;
            if (rooms == null) return new List<string>();
            return rooms.Select(r => r.PoolName).Where(n => KH.ProblemRooms.Any(p => RoomMatches(n, p))).Distinct().ToList();
        }

        private bool IsDuplicator(Item i)
            => i != null && (i.UniqueIdentity.Type == IdentityType.MissionKeyDuplicator
                             || (KH.DuplicatorItemId != 0 && (i.Id == KH.DuplicatorItemId || i.HighId == KH.DuplicatorItemId)));

        /// <summary>A duplicator in the main inventory, else in a bag (unless mainOnly).</summary>
        private Item Duplicator(bool mainOnly = false)
        {
            var inv = Inventory.Items.FirstOrDefault(i => i != null && i.Slot.Type == IdentityType.Inventory && IsDuplicator(i));
            if (inv != null && inv.UniqueIdentity.Type == IdentityType.MissionKeyDuplicator && inv.Id != 0 && inv.Id != KH.DuplicatorItemId)
            {
                KLog($"the duplicator in my inventory is template {inv.Id}; remembering that (was {KH.DuplicatorItemId}).");
                KH.DuplicatorItemId = inv.Id; SaveKH();
            }
            if (inv != null || mainOnly) return inv;
            return Inventory.Containers.Where(c => c?.Items != null).SelectMany(c => c.Items).FirstOrDefault(IsDuplicator);
        }

        private static List<Item> MissionKeys()
            => Inventory.Items.Where(i => i != null && i.Slot.Type == IdentityType.Inventory && i.UniqueIdentity.Type == IdentityType.MissionKey).ToList();

        private bool KeyHandNeedsDuplicator() => KH.Enabled && Resupply != null && KH.DuplicatorItemId != 0 && Duplicator() == null;

        private PlayerChar KeyOwnerDynel()
        {
            string n = KhOwner;
            return string.IsNullOrEmpty(n) ? null : DynelManager.Players.FirstOrDefault(p => p != null && string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase));
        }

        private bool OwnerInTeam()
            => _keyOwnerId != 0 && Team.Members.Any(m => m != null && m.Identity.Instance == _keyOwnerId);

        // ---- Commands and tells ---------------------------------------------------------------------------

        public void KeyHandCommand(string[] a, Action<string> reply)
        {
            string sub = a.Length > 0 ? a[0].ToLowerInvariant() : "status";
            string rest = a.Length > 1 ? string.Join(" ", a.Skip(1)).Trim() : "";
            double Num(string s) => double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : -1;
            switch (sub)
            {
                case "on": case "off":
                    KH.Enabled = sub == "on"; SaveKH();
                    if (KH.Enabled && string.IsNullOrEmpty(KhOwner)) reply("Key hand-off ON, but no owner: 'keyhand owner <name>'.");
                    else reply(KeyHandStatus());
                    return;
                case "owner":
                    if (rest.Length == 0) { reply($"Key owner: {(string.IsNullOrEmpty(KH.Owner) ? $"not set (using my owner '{_ctx.Config.Owner}')" : KH.Owner)}. 'keyhand owner <name>'."); return; }
                    KH.Owner = rest; SaveKH(); reply($"Key copies go to {KH.Owner}."); return;
                case "wait": case "donewait": case "peek":
                {
                    double v = Num(rest);
                    if (v < 0) { reply($"'keyhand {sub} <minutes>'."); return; }
                    if (sub == "wait") KH.WaitMinutes = v; else if (sub == "donewait") KH.DoneMinutes = v; else KH.PeekMinutes = v;
                    SaveKH(); reply(KeyHandStatus()); return;
                }
                case "rooms":
                {
                    var w = rest.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (w.Length == 2 && w[0].Equals("add", StringComparison.OrdinalIgnoreCase))
                    { if (!KH.ProblemRooms.Any(x => x.Equals(w[1], StringComparison.OrdinalIgnoreCase))) KH.ProblemRooms.Add(w[1]); SaveKH(); }
                    else if (w.Length == 2 && w[0].Equals("remove", StringComparison.OrdinalIgnoreCase))
                    { KH.ProblemRooms.RemoveAll(x => x.Equals(w[1], StringComparison.OrdinalIgnoreCase)); SaveKH(); }
                    else if (w.Length > 0) { reply("'keyhand rooms', 'keyhand rooms add <room>', 'keyhand rooms remove <room>'."); return; }
                    reply("Problem rooms: " + (KH.ProblemRooms.Count == 0 ? "none" : string.Join(", ", KH.ProblemRooms)) + " (a room matches by its name or by ending in _<name>).");
                    return;
                }
                case "check":
                {
                    if (!_mission.InMission) { reply("Not in a mission building."); return; }
                    var hits = ProblemRoomsHere();
                    reply(hits.Count > 0 ? $"This building has problem room(s): {string.Join(", ", hits)}." : "No problem rooms in this building.");
                    return;
                }
                case "here": case "done":
                    reply(KeyHandTell(KhOwner, sub) ? $"Got '{sub}'." : $"Not waiting for '{sub}' (now: {_key}).");
                    return;
                case "cancel":
                    if (_key == KeyStep.None) { reply("No key hand-off going."); return; }
                    KeyGiveUp("owner said cancel"); reply("Key hand-off cancelled; carrying on with the mission."); return;
                case "status": case "":
                    reply(KeyHandStatus()); return;
                default:
                    reply("keyhand on|off|status | owner <name> | wait <min> | donewait <min> | peek <min> | rooms [add|remove <room>] | check | cancel");
                    return;
            }
        }

        private string KeyHandStatus()
            => $"Key hand-off {(KH.Enabled ? "ON" : "off")}; owner {(string.IsNullOrEmpty(KhOwner) ? "not set" : KhOwner)}; problem rooms {string.Join(", ", KH.ProblemRooms)}; "
               + $"waits {KH.WaitMinutes:0.#} min for you, {KH.DoneMinutes:0.#} min for 'done'; peek {(KH.PeekMinutes > 0 ? $"every {KH.PeekMinutes:0.#} min" : "off")}; "
               + $"duplicator: {(Duplicator() != null ? "have one" : $"none (buys template {KH.DuplicatorItemId} at Fair Trade before the next roll)")}; now: {(_key == KeyStep.None ? "idle" : _key.ToString())}.";

        /// <summary>'here' / 'done' from the key owner while a hand-off waits for it. Main asks this before its owner gate
        /// (the key owner need not be the bot's owner). True when the tell was taken.</summary>
        public bool KeyHandTell(string sender, string text)
        {
            if (_key == KeyStep.None || string.IsNullOrEmpty(KhOwner) || !string.Equals((sender ?? "").Trim(), KhOwner, StringComparison.OrdinalIgnoreCase)) return false;
            string t = (text ?? "").Trim().TrimEnd('.', '!').ToLowerInvariant();
            if (t == "here" && (_key == KeyStep.WaitOwner || _key == KeyStep.Outside)) { _keyHere = true; KLog($"{sender} says he's at the door."); return true; }
            if (t == "done" && (_key == KeyStep.WaitDone || _key == KeyStep.Clearing)) { _keyDone = true; KLog($"{sender} says done."); return true; }
            return false;
        }

        // ---- Wire -------------------------------------------------------------------------------------------

        /// <summary>The server's trade messages to us while we trade the key (read as they come, not through the SDK's
        /// Trade status, so the steps match the capture's server side one for one).</summary>
        private void KeyTradeMessage(TradeMessage tm, LocalPlayer me)
        {
            if (_key != KeyStep.Trade || tm.Identity.Instance != me.Identity.Instance) return;
            var partner = new Identity((IdentityType)tm.Param1, tm.Param2);
            KLog($"trade rx action {(int)tm.Action} ({tm.Action}) partner={partner} p3={tm.Param3:X} p4={tm.Param4}");
            switch (tm.Action)
            {
                case TradeAction.Open: if (partner.Instance == _keyOwnerId) _tradeOpened = true; break;
                case TradeAction.Accept: if (partner.Instance == _keyOwnerId) _ownerFinal = true; break;   // capture seq 254
                case TradeAction.Complete: _tradeDone = true; break;                                       // capture seq 255
                case TradeAction.Decline: _tradeDeclined = true; break;
            }
        }

        // ---- The run's hooks --------------------------------------------------------------------------------

        /// <summary>AwaitBlitz, at the entrance. True when the hand-off took the frame (the caller returns).</summary>
        private bool KeyAtEntrance(LocalPlayer me)
        {
            switch (_key)
            {
                case KeyStep.BackIn:
                    if (_keyBackTo == KeyStep.WaitOwner) { _resumeBlitz = false; KeyStepTo(KeyStep.WaitOwner, "back in after looking for the owner"); Enter(Phase.KeyHand, "waiting for the key owner"); return true; }
                    KeyStepTo(KeyStep.Clearing, "back in with the owner on the team");
                    _resumeBlitz = true;   // no entrance route check: the building is known to have rooms the planner can't do
                    KTell("Back inside. Clearing what I can reach - stay hidden. Tell me 'done' when you've finished.");
                    return false;          // on to the blitz, in clear mode (Tick sets ClearMode while Clearing)
                case KeyStep.WaitOwner:
                case KeyStep.WaitDone:
                    _resumeBlitz = false;   // back in after a heal-out: keep waiting
                    Enter(Phase.KeyHand, "back to waiting (key hand-off)");
                    return true;
                case KeyStep.None:
                    break;
                default:
                    return false;
            }
            if (!KH.Enabled || _resumeBlitz || _blitzTries != 0 || _completed || _current == null || ReferenceEquals(_keyFor, _current)) return false;
            var hits = ProblemRoomsHere();
            if (hits.Count == 0) return false;
            _keyFor = _current;
            string where = $"{Zoning.Name(_current.Playfield.Instance)} ({_current.Location.X:0},{_current.Location.Z:0})";
            if (string.IsNullOrEmpty(KhOwner)) { KLog($"problem rooms {string.Join(", ", hits)} in {where}, but no key owner set; doing it as usual."); return false; }
            if (_current.Location.X == 0 && _current.Location.Z == 0) { KLog($"problem rooms {string.Join(", ", hits)}, but I don't know this mission's door; doing it as usual."); return false; }
            KLog($"problem mission: rooms {string.Join(", ", hits)} in the building at {where}; copying the key for {KhOwner}.");
            _keyTries = 0; _keyUsedAt = -1; _dupMoved = false; _keyCopy = null; _keysBefore.Clear();
            _keyHere = _keyDone = _keyPeek = _keyToldOutside = false; _keyOwnerId = 0;
            _follow.ClearMovement();
            KeyStepTo(KeyStep.Copy, "problem rooms");
            Enter(Phase.KeyHand, "key hand-off");
            return true;
        }

        /// <summary>Blitz phase, while clearing for the owner: when the blitz ends (or the mission completes), stop and wait
        /// inside for 'done'. True when it took the frame.</summary>
        private bool KeyAfterClear()
        {
            if (_key != KeyStep.Clearing || !_mission.InMission || (!_completed && _mission.Active)) return false;
            if (_mission.Active) _mission.Stop("key hand-off: waiting for the owner's 'done'");
            _follow.ClearMovement();
            KeyStepTo(KeyStep.WaitDone, _completed ? "the mission completed" : "the blitz ended");
            KTell(_completed ? "The mission is complete. Waiting inside - tell me 'done' when you're out of the rooms."
                             : $"Cleared what I can reach{(_mission.ClearPct >= 0 ? $" ({_mission.ClearPct:0}% of the mobs)" : "")}. Waiting inside for your 'done'.");
            Enter(Phase.KeyHand, "waiting for 'done'");
            return true;
        }

        /// <summary>Give the hand-off up and carry on with the mission as usual.</summary>
        private void KeyGiveUp(string why)
        {
            KLog($"giving up the hand-off ({why}); carrying on with the mission as usual.");
            if (Trade.IsTrading && _key == KeyStep.Trade && _keyOwnerId != 0)
                Client.Send(new TradeMessage { Version = 2, Action = TradeAction.Decline, Param1 = (int)IdentityType.SimpleChar, Param2 = _keyOwnerId });
            _key = KeyStep.None; _keyOut = false;
            _follow.ClearMovement();
            if (!Active) return;
            if (_mission.InMission) { _resumeBlitz = false; Enter(Phase.AwaitBlitz, "key hand-off given up"); }
            else if (_current != null) Enter(Phase.ToDoor, "key hand-off given up; back in");
            else Enter(Phase.ToTerminal, "key hand-off given up");
        }

        private void KeyReset() { _key = KeyStep.None; _keyOut = false; }

        private void KeyBackIn(KeyStep next, string why)
        {
            _keyBackTo = next;
            KeyStepTo(KeyStep.BackIn, why);
            _follow.ClearMovement();
            Enter(Phase.ToDoor, "key hand-off: back in");
        }

        // ---- The phase --------------------------------------------------------------------------------------

        private bool KeyHandTick(LocalPlayer me)
        {
            double t = _clock - _keyAt;
            if (_current == null && _key != KeyStep.WaitDone) { KeyReset(); Enter(_mission.InMission ? Phase.AwaitBlitz : Phase.ToTerminal, "key hand-off: no mission in hand"); return false; }
            switch (_key)
            {
                case KeyStep.Copy:
                {
                    var dup = Duplicator();
                    if (dup == null) { KTell("A problem mission, but I have no Mission Key Duplicator; doing it alone."); KeyGiveUp("no duplicator"); return false; }
                    if (dup.Slot.Type != IdentityType.Inventory)
                    {
                        // In a bag: out to the inventory first (the use in the capture was from an inventory slot).
                        if (!_dupMoved) { Item.MoveItemToInventory(dup.Slot, 0x6F); _dupMoved = true; KLog($"duplicator out of a bag ({dup.Slot})."); }
                        if (t > 8) KeyGiveUp("the duplicator didn't come out of its bag");
                        return false;
                    }
                    var keys = MissionKeys();
                    if (_keyUsedAt < 0)
                    {
                        if (keys.Count != 1) { KeyGiveUp(keys.Count == 0 ? "no mission key in my inventory" : $"{keys.Count} mission keys in my inventory, can't tell which is this one's"); return false; }
                        _keysBefore = new HashSet<Identity>(keys.Select(k => k.UniqueIdentity));
                        Client.Send(new GenericCmdMessage { Action = GenericCmdAction.UseItemOnItem, User = me.Identity, Source = dup.Slot, Target = keys[0].Slot, Count = 2, Temp4 = 1 });
                        _keyUsedAt = _clock; _keyTries++;
                        KLog($"using {dup.Name ?? "the duplicator"} ({dup.Slot}) on {keys[0].Name ?? "the key"} ({keys[0].Slot}, {keys[0].UniqueIdentity}), try {_keyTries}.");
                        return false;
                    }
                    var copy = keys.FirstOrDefault(k => !_keysBefore.Contains(k.UniqueIdentity));
                    if (copy != null)
                    {
                        _keyCopy = copy.UniqueIdentity;
                        KLog($"key copy {copy.UniqueIdentity} in {copy.Slot}.");
                        _keyWaitStart = _keyLastPeek = _clock;
                        KeyStepTo(KeyStep.WaitOwner, "key copied");
                        KTell($"Problem mission: {Zoning.Name(_current.Playfield.Instance)} ({_current.Location.X:0},{_current.Location.Z:0}) - come get a key. I'm waiting inside at the door; tell me 'here' when you're outside it.");
                        return false;
                    }
                    if (_clock - _keyUsedAt > 6)
                    {
                        if (_keyTries < 3) { KLog("no key copy came back; trying again."); _keyUsedAt = -1; }
                        else KeyGiveUp("the duplicator gave no copy in 3 tries");
                    }
                    return false;
                }

                case KeyStep.WaitOwner:
                {
                    if (!_mission.InMission) { KeyStepTo(KeyStep.Outside, "found myself outside"); _keyPeek = false; return false; }
                    _follow.ClearManual();
                    if (_clock - _keyWaitStart > KH.WaitMinutes * 60)
                    {
                        KTell($"You didn't come in {KH.WaitMinutes:0} minutes; doing the mission alone.");
                        KeyGiveUp($"no owner in {KH.WaitMinutes:0} min");
                        return false;
                    }
                    bool peek = !_keyHere && KH.PeekMinutes > 0 && _clock - _keyLastPeek > KH.PeekMinutes * 60;
                    if (!_keyHere && !peek) return false;
                    _keyPeek = peek; _keyHere = false; _keyToldOutside = false;
                    KLog(peek ? "stepping out to look for the owner." : "stepping out to the owner.");
                    _keyOut = true;
                    if (_mission.Active) _mission.Stop("key hand-off: out to the owner");
                    _mission.Command("backoutside", OnOutsideReply);
                    KeyStepTo(KeyStep.Outside, peek ? "peek" : "owner is here");
                    Enter(Phase.Leaving, "key hand-off: out to the owner");
                    return false;
                }

                case KeyStep.Outside:
                {
                    if (_mission.InMission) return false;   // still walking out
                    if (_keyOut) { _keyOut = false; _keyAt = _clock; KLog($"outside the door at ({me.Transform.Position.X:0},{me.Transform.Position.Z:0})."); return false; }
                    var o = KeyOwnerDynel();
                    var door = new Vector3(_current.Location.X, me.Transform.Position.Y, _current.Location.Z);
                    if (o != null && Movement.Flat(o.Transform.Position, door) > 40f) o = null;   // near the door only
                    if (o == null)
                    {
                        if (_keyPeek && t > 15) { KLog("the owner isn't outside yet; back in to wait."); _keyLastPeek = _clock; KeyBackIn(KeyStep.WaitOwner, "peek: nobody"); return false; }
                        if (_clock - _keyWaitStart > KH.WaitMinutes * 60) { KTell($"You didn't come in {KH.WaitMinutes:0} minutes; doing the mission alone."); KeyGiveUp("no owner at the door"); return false; }
                        if (!_keyPeek && t > 60 && !_keyToldOutside) { _keyToldOutside = true; KTell($"I'm outside the door at ({_current.Location.X:0},{_current.Location.Z:0}) and can't see you."); }
                        return false;
                    }
                    _keyOwnerId = o.Identity.Instance;
                    if (me.DistanceFrom(o) > 3f && t < 120) { _follow.SetManualTarget(o.Transform.Position); return true; }
                    _follow.ClearMovement();
                    _tradeStep = 0; _tradeTries = 0; _tradeAt = _clock;
                    KeyStepTo(KeyStep.Trade, $"owner {o.Name} {me.DistanceFrom(o):0.0} m off");
                    return false;
                }

                case KeyStep.Trade:
                    return KeyTradeTick(me);

                case KeyStep.Invite:
                {
                    if (OwnerInTeam()) { KLog("the owner is on my team."); KeyBackIn(KeyStep.Clearing, "owner on the team"); return false; }
                    var o = KeyOwnerDynel();
                    if (_invites < 3 && _clock - _inviteAt > 20)
                    {
                        if (o == null) { if (t > 30) { KLog("can't see the owner to invite him."); KeyBackIn(KeyStep.Clearing, "no owner to invite"); } return false; }
                        Team.Invite(o.Identity);   // CharacterAction TeamRequest 0x1A, Parameter2 1 (capture seq 41)
                        _invites++; _inviteAt = _clock;
                        KLog($"team invite to {o.Name} ({o.Identity}), try {_invites}.");
                        return false;
                    }
                    if (_invites >= 3 && _clock - _inviteAt > 20) { KTell("You didn't join the team; going in anyway."); KeyBackIn(KeyStep.Clearing, "no team after 3 invites"); }
                    return false;
                }

                case KeyStep.BackIn:
                    // ToDoor / EnterDoor take him in; AwaitBlitz picks it up (KeyAtEntrance).
                    Enter(Phase.ToDoor, "key hand-off: back in");
                    return false;

                case KeyStep.Clearing:
                    Enter(Phase.AwaitBlitz, "clearing for the owner");
                    return false;

                case KeyStep.WaitDone:
                {
                    if (!_mission.InMission) { KeyReset(); Enter(_completed && _current != null ? Phase.Blitz : Phase.ToTerminal, "outside while waiting for 'done'"); return false; }
                    _follow.ClearManual();
                    bool timeUp = _clock - _keyAt > KH.DoneMinutes * 60;
                    if (!_keyDone && !timeUp) return false;
                    KLog(_keyDone ? "owner is done." : $"no 'done' in {KH.DoneMinutes:0} min; carrying on.");
                    if (timeUp && !_keyDone) KTell($"No 'done' from you in {KH.DoneMinutes:0} minutes; carrying on.");
                    KeyReset();
                    if (_completed) { _mission.Command("backoutside", OnOutsideReply); Enter(Phase.Blitz, "owner done; walking out"); }
                    else { _resumeBlitz = true; Enter(Phase.AwaitBlitz, "owner done; finishing it myself"); }
                    return false;
                }
            }
            Enter(_mission.InMission ? Phase.AwaitBlitz : Phase.ToTerminal, "no key hand-off step");
            return false;
        }

        private bool KeyTradeTick(LocalPlayer me)
        {
            double t = _clock - _tradeAt;
            var o = KeyOwnerDynel();
            if (_tradeDone)
            {
                KLog("trade complete: the key copy is his.");
                KTell("That's your key. Inviting you to the team - hide by the entrance once you're in.");
                _invites = 0; _inviteAt = -99;
                KeyStepTo(KeyStep.Invite, "key traded");
                return false;
            }
            if (_tradeDeclined || (o == null && _tradeStep > 0 && t > 10) || (_tradeStep == 4 && t > 15) || (_tradeStep == 3 && t > 120) || (_tradeStep == 1 && t > 10))
            {
                string why = _tradeDeclined ? "declined" : o == null ? "he's gone" : $"no answer at step {_tradeStep}";
                if (!_tradeDeclined && _keyOwnerId != 0) Client.Send(new TradeMessage { Version = 2, Action = TradeAction.Decline, Param1 = (int)IdentityType.SimpleChar, Param2 = _keyOwnerId });
                if (++_tradeTries >= 3) { KTell("The trade didn't go through; doing the mission alone."); KeyGiveUp("trade failed 3 times: " + why); return false; }
                KLog($"trade failed ({why}); again in 3 s.");
                _tradeStep = 0; _tradeAt = _clock + 3; _tradeOpened = _ownerFinal = _tradeDone = _tradeDeclined = false;
                return false;
            }
            switch (_tradeStep)
            {
                case 0:
                {
                    if (t < 0) return false;
                    if (o == null) { if (t > 10) { KeyStepTo(KeyStep.Outside, "owner not in sight for the trade"); } return false; }
                    var copy = Inventory.Items.FirstOrDefault(i => i != null && i.Slot.Type == IdentityType.Inventory && _keyCopy.HasValue && i.UniqueIdentity == _keyCopy.Value);
                    if (copy == null) { KTell("I lost track of the key copy; doing the mission alone."); KeyGiveUp("key copy not in my inventory"); return false; }
                    _keyCopySlot = copy.Slot;
                    _tradeOpened = _ownerFinal = _tradeDone = _tradeDeclined = false;
                    // Capture seq 31-33: InfoRequest and LookAt (ReturnInfo 1) on him, then Trade Open naming him.
                    Client.Send(new CharacterActionMessage { Action = SmokeLounge.AOtomation.Messaging.GameData.CharacterActionType.InfoRequest, Target = o.Identity });
                    Client.Send(new LookAtMessage { Target = o.Identity, ReturnInfo = 1 });
                    Trade.Open(o.Identity);
                    KLog($"trade open to {o.Name} ({o.Identity}); the copy is in {_keyCopySlot}.");
                    _tradeStep = 1; _tradeAt = _clock;
                    return false;
                }
                case 1:
                    if (!_tradeOpened || t < 1.5) return false;
                    // Capture seq 36: AddItem naming ourselves, the copy's inventory slot.
                    Client.Send(new TradeMessage { Version = 2, Action = TradeAction.AddItem, Param1 = (int)me.Identity.Type, Param2 = me.Identity.Instance, Param3 = (int)_keyCopySlot.Type, Param4 = _keyCopySlot.Instance });
                    KLog($"trade add {_keyCopySlot}.");
                    _tradeStep = 2; _tradeAt = _clock;
                    return false;
                case 2:
                    if (t < 1) return false;
                    // Capture seq 37: action 3 with no partner.
                    Client.Send(new TradeMessage { Version = 2, Action = (TradeAction)3 });
                    KLog("trade action 3 (my accept); waiting for his.");
                    _tradeStep = 3; _tradeAt = _clock;
                    return false;
                case 3:
                    if (!_ownerFinal) return false;
                    if (t < 1) return false;   // not in the same beat as his (capture: +1.0 s)
                    // Capture seq 38: action 1 with no partner, after the server passed on his.
                    Client.Send(new TradeMessage { Version = 2, Action = (TradeAction)1 });
                    KLog("trade action 1 (my confirm) after his.");
                    _tradeStep = 4; _tradeAt = _clock;
                    return false;
            }
            return false;
        }

        // ---- Shopping for the duplicator ---------------------------------------------------------------------

        /// <summary>A trip to Fair Trade for the duplicator alone (no selling or banking, whatever MissionShop says).</summary>
        private bool StartToolShop(string why)
        {
            if (Resupply == null) return false;
            _dupShopAt = _clock;
            _shopTriedAt = _clock;
            _shopBag = null; _shopBoughtForNanos = false; _shopBoughtForRoom = false; _shopArrival = null; _shopFullBags.Clear(); _shopStimsTried = false;
            _shopToolOnly = true; _shopToolTried = false;
            KLog($"off to Fair Trade: {why}.");
            _shopStep = ShopStep.Travel; _shopStepAt = _clock; _travelStarted = false; _travelTries = 0;
            Enter(Phase.Shop, "buying a Mission Key Duplicator");
            return true;
        }

        private bool ShopBuyTool(LocalPlayer me)
        {
            _shopToolTried = true;
            if (Duplicator() != null) { ShopNext(ShopStep.Exit, "have a duplicator already; leaving."); return false; }
            Resupply.StartTool(me, KH.DuplicatorItemId, 1, KH.ShopKeyword, s => _ctx.Log("MISSIONRUN: shop: " + s));
            ShopNext(ShopStep.Tool, "buying a Mission Key Duplicator (key hand-off).");
            return false;
        }

        private bool ShopToolTick(LocalPlayer me, double t)
        {
            if (Resupply.Active || t < 1) return false;
            if (Duplicator() == null && t < 5) return false;   // the item lands just after the trade
            KLog(Duplicator() != null ? "bought a Mission Key Duplicator." : "couldn't buy a Mission Key Duplicator here (see the RESUPPLY lines).");
            if (_shopToolOnly) { ShopNext(ShopStep.Exit, "leaving the way I came in."); return false; }
            return ShopAfterNanos(me);
        }
    }
}
