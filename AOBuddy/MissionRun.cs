using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using Newtonsoft.Json.Linq;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOBuddy
{
    /// <summary>
    /// MISSION RUN — the solo loop, off until the owner says `mission run` with the bot standing at a mission
    /// terminal. That terminal is the one it keeps using: roll (MissionRoll) until a mission it can do in an
    /// allowed zone comes up, accept it, travel to the door (OverlandController, `travelto`), walk into the door
    /// (capture 20260923-201746: no message, the server moves you in), blitz (MissionController), and once it
    /// has walked back out, stash the reward in a backpack and travel back to the terminal. Again, until
    /// `mission run stop`. It never gives up on its own: a mission it can't finish (door, travel, blitz) is deleted
    /// and another rolled; only the owner stops the run.
    ///
    /// It drives the other controllers only through what they already offer (their commands and Active flags),
    /// so it changes nothing about how travel or blitz behave. While it runs the bot is on its own: no follow,
    /// no owner-lost chasing (Main checks Active), and it fights whatever attacks it or its pets.
    ///
    /// Death: after the reclaim, back to the terminal, wait out rez sickness and rebuff there, then go back to
    /// the mission it was on (still in the quest log) and finish it; with none open, roll a new one.
    /// </summary>
    public class MissionRun
    {
        private readonly BotContext _ctx;
        private readonly MissionRoll _roll;
        private readonly MissionController _mission;
        private readonly OverlandController _overland;
        private readonly FollowController _follow;
        private readonly Action<string> _tell;
        private readonly CombatController _combat;
        private double _fightStart, _fightIgnoreUntil = -1;
        private int _fightHpMin = 100, _prevHp = -1;
        private double _lastHurt = -999;
        private readonly string _pluginDir;

        private enum Phase { Off, ToTerminal, Rolling, AwaitList, Accepting, ToDoor, EnterDoor, AwaitBlitz, Blitz, Stash, Dead, Leaving, Backoff, Hike, ExitStand, Fight, Shop }
        private Phase _phase = Phase.Off;
        private double _phaseTime, _clock;
        public bool Active => _phase != Phase.Off;

        // The terminal, remembered where the run was started.
        private int _termPf;
        private Identity _termId;
        private Vector3 _termPos;

        // The mission in hand.
        private MissionInfo _current;
        private bool _completed;
        private int _rolls, _done, _travelTries, _doorTries;
        private bool _travelStarted;
        private Vector3? _door;
        private readonly HashSet<(int, int)> _rewardIds = new HashSet<(int, int)>();

        // Stashing.
        private readonly Queue<Item> _bagsToTry = new Queue<Item>();
        private Item _bag;
        private double _lastMove;

        private const int MaxRolls = 40;
        private const double ListTimeout = 6, TravelTimeout = 900, DoorTimeout = 20, BlitzTimeout = 1200;

        public MissionRun(BotContext ctx, MissionRoll roll, MissionController mission, OverlandController overland,
                          FollowController follow, string pluginDir, Action<string> tell, CombatController combat)
        {
            _ctx = ctx; _roll = roll; _mission = mission; _overland = overland; _follow = follow;
            _pluginDir = pluginDir; _tell = tell; _combat = combat;
            _roll.ListArrived += OnList;
        }

        // ---- Live status, read off the shared read-model (R2.2) ------------------------------------------
        // 'Buffing' is this system's own composition of three status facts (the 15 s window is the run's
        // policy); the raw facts come from ctx.Status, refreshed by Main each tick before Walk.
        private bool Buffing => _ctx.Status.HasPendingCasts || _ctx.Status.Resting || _ctx.Status.SecondsSinceCast < 15;

        // FIGHT-OR-RUN POLICY (R2.2: was a ctor lambda closing over Main's privates — decision logic,
        // not status, so it lives here as the named policy it is). Stop to fight only in an EMERGENCY
        // (the owner's call: run to the end and heal with stims): in a fight and HP under
        // MissionFightBelowPercent. Otherwise keep going; stims and pets carry on.
        private bool FightOrRun()
        {
            var lp = DynelManager.LocalPlayer;
            if (lp == null || !_ctx.Status.InCombat) return false;   // InCombat = combat OR hostiles engaged (us or the owner)
            // fight style: anything that attacks - INSIDE a mission. On the way (06:51, 2026-09-24) it stopped
            // him for a level 50 Male Watcher 39 m off in The Longest Road and he died there; outdoors the
            // blitz rules apply whatever the style.
            if (Fleeing) return false;   // running from a pack (in or out of a mission): no turning to fight
            if (string.Equals(_ctx.Config.MissionStyle, "fight", StringComparison.OrdinalIgnoreCase) && _mission.InMission) return true;
            // Earlier than 'under 40% with no stim' (23:14, 2026-09-23): four mobs chased him while blitz
            // searched rooms and snagged on walls, 100% -> 10% in 12 s; the stim at 58% bought 3 s and the
            // 40% trigger fired 4 s before he died. So: a pack on him, or HP falling, and he turns and fights.
            int onMe = DynelManager.Characters.Count(c => c.FightingIdentity.HasValue && c.FightingIdentity.Value == lp.Identity
                                                      && c.Identity != lp.Identity && !_combat.IsSetAside(c.Identity)
                                                      && c is NpcChar && IsMob(c, _mission.InMission)
                                                      && (!c.TryGetStat(Stat.Health, out int ch) || ch > 0));
            if (onMe >= _ctx.Config.MissionFightAttackers) return true;
            int hp = _ctx.Status.SelfHpPct;
            if (hp == SupportController.Unknown) return false;
            if (hp < _ctx.Config.MissionFightBelowPercent) return true;
            // stims share the FirstAid lock (40 s after each use)
            return hp < _ctx.Config.MissionFightNoStimBelowPercent && !lp.IsSpecialReady(Stat.FirstAid);
        }

        // ---- Commands ----------------------------------------------------------------------------------

        public void Command(string args, Action<string> reply)
        {
            string a = (args ?? "").Trim().ToLowerInvariant();
            if (a == "stop") { if (Active) { Stop("owner said stop"); reply($"Mission run stopped after {_done} mission(s)."); } else reply("No mission run going."); return; }
            if (a == "status") { reply(Status()); return; }
            if (a == "avoid" || a.StartsWith("avoid "))
            {
                // 'mission run avoid' lists; 'mission run avoid <playfield id>' adds or removes one.
                var arg = a.Substring(5).Trim();
                var list = _ctx.Config.MissionAvoidZones ?? (_ctx.Config.MissionAvoidZones = new List<int>());
                if (int.TryParse(arg, out int apf))
                {
                    if (!list.Remove(apf)) list.Add(apf);
                    SaveConfigValue("MissionAvoidZones", new JArray(list));
                }
                reply(list.Count == 0 ? "No zones avoided." : "Avoiding: " + string.Join(", ", list.Select(z => $"{Zoning.Name(z)} ({z})")));
                return;
            }
            if (a.StartsWith("tune"))
            {
                var w = args.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (w.Length >= 2 && w[1].Equals("reset", StringComparison.OrdinalIgnoreCase))
                {
                    if (w.Length >= 3) _tune.Remove(w[2].ToLowerInvariant()); else _tune.Clear();
                    SaveTune(); reply("Back to defaults: " + TuneText()); return;
                }
                if (w.Length >= 3)
                {
                    string k = w[1].ToLowerInvariant();
                    if (!TuneDefaults.ContainsKey(k)) { reply($"No '{k}'. " + TuneText()); return; }
                    if (!float.TryParse(w[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v)) { reply($"'{w[2]}' isn't a number."); return; }
                    _tune[k] = v; SaveTune();
                    _ctx.Log($"MISSIONRUN: tune {k} = {v} (default {TuneDefaults[k].def}).");
                    reply($"{k} = {v} (default {TuneDefaults[k].def}): {TuneDefaults[k].what}.");
                    return;
                }
                reply(TuneText() + " | 'mission run tune <name> <value>', 'mission run tune reset [name]'.");
                return;
            }
            if (a.StartsWith("keep"))
            {
                KeepSet();
                string rest = args.Trim().Length > 4 ? args.Trim().Substring(4).Trim() : "";   // original case for the name
                if (rest.StartsWith("ql ", StringComparison.OrdinalIgnoreCase))
                {
                    var w = rest.Substring(3).Trim();
                    int sp = w.IndexOf(' ');
                    string first = sp > 0 ? w.Substring(0, sp) : w, nm = sp > 0 ? w.Substring(sp + 1).Trim() : "";
                    if (nm.Length == 0) { reply("'mission run keep ql <ql> <exact name>' or 'keep ql off <exact name>'."); return; }
                    if (first.Equals("off", StringComparison.OrdinalIgnoreCase)) { reply(NameRules().Remove(nm) ? $"'{nm}' is sold like anything else again." : $"No rule for '{nm}'."); SaveBankRules(); return; }
                    if (!int.TryParse(first, out int q) || q < 1) { reply($"'{first}' isn't a QL."); return; }
                    NameRules()[nm] = q; SaveBankRules();
                    reply($"'{nm}' QL {q}+ is kept and banked with the nanos.");
                    return;
                }
                if (rest.StartsWith("implant", StringComparison.OrdinalIgnoreCase))
                {
                    string q = rest.Substring(7).Trim().TrimStart('s').Trim();
                    if (q.Equals("off", StringComparison.OrdinalIgnoreCase)) { SetImplantMinQl(0); reply("Implants are sold like anything else again."); return; }
                    if (int.TryParse(q, out int ql) && ql > 0) { SetImplantMinQl(ql); reply($"Implants QL {ql}+ are kept and banked with the nanos."); return; }
                    reply(ImplantMinQl() > 0 ? $"Implants QL {ImplantMinQl()}+ are kept and banked. 'mission run keep implant <ql>|off'." : "Implants aren't kept. 'mission run keep implant <ql>' keeps and banks them from that QL up.");
                    return;
                }
                if (rest.StartsWith("add ", StringComparison.OrdinalIgnoreCase)) { string nm = rest.Substring(4).Trim(); _keepAdded.Add(nm); SaveKeep(); reply($"Keeping '{nm}': never sold."); return; }
                if (rest.StartsWith("remove ", StringComparison.OrdinalIgnoreCase)) { string nm = rest.Substring(7).Trim(); reply(_keepAdded.Remove(nm) ? $"'{nm}' off the keep list." : $"'{nm}' isn't on the list I added to (config KeepItems is edited in config.json)."); SaveKeep(); return; }
                var banked = NameRules().Select(kv => $"{kv.Key} QL {kv.Value}+").ToList();
                if (ImplantMinQl() > 0) banked.Insert(0, $"implants QL {ImplantMinQl()}+");
                reply("Never sold: " + string.Join(", ", KeepSet().OrderBy(x => x)) + (banked.Count > 0 ? ". Kept and banked: nanos, " + string.Join(", ", banked) : "")
                      + ". 'mission run keep add <exact item name>' / 'keep remove <name>' / 'keep ql <ql> <name>' / 'keep implant <ql>'.");
                return;
            }
            if (a.StartsWith("shop"))
            {
                string v = a.Substring(4).Trim();
                if (v == "on" || v == "off") { _ctx.Config.MissionShop = v == "on"; SaveConfigValue("MissionShop", v == "on"); }
                if (v == "now")
                {
                    // Starts the run first if it isn't going (07:56, 2026-09-24: 'shop now' on a fresh start did nothing).
                    if (!Active) Command("", reply);
                    if (!Active) return;
                    _shopTriedAt = -9999; bool was = _ctx.Config.MissionShop; _ctx.Config.MissionShop = true;
                    bool went = StartShop("owner asked"); _ctx.Config.MissionShop = was;
                    if (!went) reply("Couldn't start the housekeeping (see the log).");
                    return;
                }
                if (v == "list") { reply(SellPreview()); return; }
                if (v == "bags")
                {
                    LoadPersonal();
                    var bags = Bags();
                    // Bags all have the same name ('Large Backpack'), so each line says where it sits and what's in
                    // it (owner, 2026-09-24). A bag not opened this session is opened now; ask again for its contents.
                    var me0 = DynelManager.LocalPlayer;
                    int opened = 0;
                    foreach (var b in bags)
                        if (me0 != null && !Inventory.Containers.Any(c => c?.Items != null && c.Identity == b.UniqueIdentity))
                        { Client.Send(new GenericCmdMessage { Action = GenericCmdAction.Use, User = me0.Identity, Target = b.Slot, Count = 1, Temp4 = 0 }); opened++; }
                    string Line(Item b, int n)
                    {
                        var c = Inventory.Containers.FirstOrDefault(x => x?.Items != null && x.Identity == b.UniqueIdentity);
                        string what = c == null ? "not opened yet" : c.Items.Count == 0 ? "empty"
                            : $"{c.Items.Count} item(s): " + string.Join(", ", c.Items.Where(i => i?.Name != null).Select(i => i.Name).Take(4)) + (c.Items.Count > 4 ? ", ..." : "");
                        return $"{n + 1}) {b.Name} in slot {b.Slot.Instance}{(_personalBags.Contains(b.UniqueIdentity) ? " [personal]" : "")} - {what}";
                    }
                    reply(bags.Count == 0 ? "No bags in my inventory." : string.Join(" | ", bags.Select((b, n) => Line(b, n)))
                          + (opened > 0 ? $". Opened {opened} bag(s) just now: ask again for their contents." : "") + " 'mission run shop personal <n>' toggles.");
                    return;
                }
                if (v.StartsWith("personal"))
                {
                    LoadPersonal();
                    var bags = Bags();
                    if (!int.TryParse(v.Substring(8).Trim(), out int n) || n < 1 || n > bags.Count) { reply("Which bag? 'mission run shop bags' lists them."); return; }
                    var id = bags[n - 1].UniqueIdentity;
                    bool on = !_personalBags.Remove(id); if (on) _personalBags.Add(id);
                    SavePersonal();
                    reply($"Bag {n} ({bags[n - 1].Name}) is {(on ? "personal: nothing in it is ever sold" : "no longer personal")}.");
                    return;
                }
                reply($"Housekeeping at Fair Trade when out of room: {(_ctx.Config.MissionShop ? "ON" : "off")} (test). Keeps {_ctx.Config.MissionCashReserve:N0} credits. 'mission run shop on|off|now|list|bags|personal <n>', 'mission run keep', 'mission run tune'.");
                return;
            }
            if (a.StartsWith("difficulty"))
            {
                // The terminal's difficulty for the style he's on: blitz uses MissionDifficulty, fight rolls at least
                // MissionFightDifficulty. Takes effect on the next roll.
                bool fightStyle = string.Equals(_ctx.Config.MissionStyle, "fight", StringComparison.OrdinalIgnoreCase);
                string v = a.Substring("difficulty".Length).Trim();
                if (int.TryParse(v, out int d) && d >= 0 && d <= 255)
                {
                    if (fightStyle) _ctx.Config.MissionFightDifficulty = d; else _ctx.Config.MissionDifficulty = d;
                    SaveConfigValue(fightStyle ? "MissionFightDifficulty" : "MissionDifficulty", d);
                    reply($"Difficulty for {_ctx.Config.MissionStyle} style set to {d}; from the next roll.");
                }
                else reply($"Difficulty: blitz {_ctx.Config.MissionDifficulty}, fight {Math.Max(_ctx.Config.MissionDifficulty, _ctx.Config.MissionFightDifficulty)} (now on {_ctx.Config.MissionStyle}). 'mission run difficulty <n>' sets it for the current style (captures: 1 easy, 6 his level, 11 hard).");
                return;
            }
            if (a.StartsWith("style"))
            {
                string st = a.Length > 5 ? a.Substring(5).Trim() : "";
                if (st == "fight" || st == "blitz") { _ctx.Config.MissionStyle = st; SaveConfigValue("MissionStyle", st); reply(st == "fight" ? "Style: fight - I'll stop and fight anything that attacks me." : $"Style: blitz - I run to the end and stim, and only stop to fight under {_ctx.Config.MissionFightBelowPercent}% with no stim ready."); }
                else reply($"Style is {_ctx.Config.MissionStyle}. 'mission run style fight' or 'mission run style blitz'.");
                return;
            }
            if (a == "skip")
            {
                if (!Active) { int n = DeleteHeldMissions(); reply($"Deleted {n} mission(s)."); return; }
                // A skipped mission's zone is left alone for a while, so the next roll doesn't send him straight back.
                if (_current != null && !_completed) MarkDanger(_current.Playfield.Instance);
                Skip("owner said skip"); reply("Skipping the mission I'm on."); return;
            }
            bool fresh = a == "new";
            if (a.Length > 0 && !fresh) { reply("mission run | mission run new (ignore a held mission) | mission run skip (delete it and go on) | mission run style fight|blitz | mission run difficulty <n> | mission run stop | mission run status"); return; }
            if (Active) { reply("Already running: " + Status()); return; }

            var me = DynelManager.LocalPlayer;
            if (me == null) { reply("Not in game."); return; }
            // The terminal his team status calls for (team terminal in a team, solo terminal alone), among the
            // terminals standing where he is - they stand side by side.
            var term = MissionRoll.MatchingTerminal(me, me.Transform.Position, TerminalRowMetres);
            var nearAny = DynelManager.AllDynels.Where(d => d != null && d.Identity.Type == IdentityType.MissionTerminal)
                                                .OrderBy(d => me.DistanceFrom(d)).FirstOrDefault();
            if (term == null && nearAny != null && me.DistanceFrom(nearAny) <= 6f)
            {
                reply($"I'm {(MissionRoll.WantTeam(me) ? "" : "not ")}in a team, so I need a {(MissionRoll.WantTeam(me) ? "team" : "solo")} mission terminal, and there's none next to '{nearAny.Name}'. Stand me by one.");
                return;
            }
            if (term != null)
            {
                // Standing at one: this is the terminal from now on (remembered across restarts).
                _termPf = (int)Playfield.ModelId; _termId = term.Identity; _termPos = term.Transform.Position;
                SaveTerminal();
            }
            else if (!LoadTerminal())
            { reply("Stand me next to the mission terminal you want me to use (within 6 m), then say 'mission run'. After that I remember it."); return; }
            _done = 0; _current = null;
            // Resume only a mission the quest log actually holds (one from a mission terminal). With none held
            // the server sends no quest log at all, so a saved copy then means a mission deleted or done
            // elsewhere (2026-09-23 21:36: it walked back to a door it had no key for).
            bool held = HeldMissionIds().Count > 0;
            if (fresh || !held) ClearSaved();
            var saved = fresh || !held ? null : FromQuestLog() ?? LoadSaved();
            string termZone = Playfield.TryGetPlayfieldNameFromId(_termPf, out string tz) ? tz : _termPf.ToString();
            string zones = _ctx.Config.MissionZones != null && _ctx.Config.MissionZones.Count > 0 ? string.Join(", ", _ctx.Config.MissionZones) : "any zone";
            _ctx.Log($"MISSIONRUN: start; terminal {_termId} in {termZone} ({_termPos.X:0},{_termPos.Z:0}); zones: {zones}.");
            reply($"Running missions from the terminal in {termZone} ({zones}), {_ctx.Config.MissionStyle} style. 'mission run stop' to stop, 'mission run status' for where I am.");
            if (_mission.InMission && !held && !fresh)
            {
                // Inside a building with no mission to do (already done, or deleted): just walk out and carry on.
                reply("I'm inside a mission building with nothing left to do here: walking out first.");
                _mission.Command("backoutside", OnOutsideReply);
                Enter(Phase.Leaving, "started inside, nothing to do");
                return;
            }
            if (_mission.InMission)
            {
                // Already inside a mission building (e.g. logged in there): blitz it, then carry on.
                _current = fresh || !held ? null : (FromQuestLog() ?? LoadSaved());
                if (_current == null) _current = new MissionInfo { MissionIdentity = new Identity(IdentityType.Mission, 0), Playfield = new Identity(IdentityType.Playfield2, 0), MissionItemData = new MissionItemReward[0] };
                _completed = false; _rewardIds.Clear();
                foreach (var rw in _current.MissionItemData ?? new MissionItemReward[0]) _rewardIds.Add((rw.LowId, rw.HighId));
                reply("I'm inside a mission: finishing it first.");
                Enter(Phase.AwaitBlitz, "started inside a mission");
                return;
            }
            if (saved != null)
            {
                // A mission taken before a restart is still in the quest log: finish it first.
                _current = saved; _completed = false; _rewardIds.Clear();
                foreach (var rw in saved.MissionItemData ?? new MissionItemReward[0]) _rewardIds.Add((rw.LowId, rw.HighId));
                reply($"First finishing the mission I already have: {(saved.MissionIcon == 0 ? $"in {MissionRoll.Zone(saved)} ({saved.Location.X:0},{saved.Location.Z:0})" : MissionRoll.Line(saved))}. ('mission run new' ignores it.)");
                Enter(Phase.ToDoor, "resuming the saved mission");
                return;
            }
            Enter(Phase.Rolling, "start");
        }

        public void Stop(string why)
        {
            if (!Active) return;
            if (_phase == Phase.Blitz && _mission.Active) _mission.Stop("mission run stopped");
            if (_overland.Active) _overland.Stop("mission run stopped");
            _follow.ClearManual();
            _ctx.Log($"MISSIONRUN: stopped ({why}) after {_done} mission(s).");
            _phase = Phase.Off;
        }

        public string Status() => !Active ? "No mission run going." :
            $"{_phase} ({_phaseTime:0}s); {_done} done; {(_current == null ? "no mission in hand" : "on " + MissionRoll.Line(_current))}.";

        public void OnDied()
        {
            if (!Active) return;
            if (_current != null && !_completed) _deathsHere++;
            // DANGER ZONES (09:33 and 09:39, 2026-09-24): twice killed by the same Hammer Broodling pack in Mutant
            // Domain on the way to one mission's door; the death count was lost on a restart and he went back a
            // third time. A death out in the open marks the zone: no missions there for 'dangermins', and the
            // mission he was walking to is dropped.
            if (!_mission.InMission)
            {
                int pf = (int)Playfield.ModelId;
                // Killed on sight: from 90%+ HP to dead within 5 s, no fight (West Athens 21:28, 2026-09-24: 100% at
                // 21:28:24, dead at 21:28:27 beside a level-200 'Vanguard Watcher'; he is Omni, it is a Clan town).
                // That zone is avoided for good (config MissionAvoidZones), not for a while.
                var avoid = _ctx.Config.MissionAvoidZones ?? (_ctx.Config.MissionAvoidZones = new List<int>());
                if (_clock - _hpHighAt < 5 && !avoid.Contains(pf))
                {
                    avoid.Add(pf);
                    SaveConfigValue("MissionAvoidZones", new JArray(avoid));
                    _tell($"Killed on sight in {Zoning.Name(pf)}; I won't go there again ('mission run avoid {pf}' to undo).");
                }
                MarkDanger(pf);
                if (_current != null && !_completed && (_phase == Phase.ToDoor || _phase == Phase.Hike || _phase == Phase.Backoff || _phase == Phase.Fight)) _diedOnWay = true;
                _ctx.Log($"MISSIONRUN: died out in {Zoning.Name(pf)}; no missions or routes there for {DangerMinutes(_danger[pf].n):0} minutes (mark {_danger[pf].n}).");
            }
            _ctx.Log($"MISSIONRUN: died{(_deathsHere > 1 ? $" ({_deathsHere} times in this mission)" : "")}; waiting for the reclaim, rez sickness and buffs, then back to it.");
            if (_overland.Active) _overland.Stop("died");
            _follow.ClearManual();
            Enter(Phase.Dead, "died");
        }

        // ---- Wire --------------------------------------------------------------------------------------

        public void OnMessage(Message m)
        {
            if (m?.Body is QuestFullUpdateMessage qfu)
            {
                if (m.RawPacket != null) _lastQuestLog = m.RawPacket;
                if (qfu.Quests != null) foreach (var q in qfu.Quests) _quests[q.QuestId] = q;
            }
            if (m?.Body is QuestMessage gone && gone.Action == QuestAction.Delete) _quests.Remove(gone.Mission);
            // Bank diagnosis: every server message while the bank is being opened (the SDK drops what it can't read).
            if (Active && _phase == Phase.Shop && _shopStep == ShopStep.OpenBank && m != null)
            {
                string what = m.Body?.GetType().Name ?? "(unreadable)";
                if (what != "CharDCMoveMessage" && what != "FollowTargetMessage")
                    _ctx.Log($"MISSIONRUN: shop: server sent {what} during the bank open{(m.RawPacket != null ? $" ({m.RawPacket.Length} bytes)" : "")}"
                             + (what == "GenericCmdMessage" && m.RawPacket != null ? ": " + BitConverter.ToString(m.RawPacket) : "") + ".");
            }
            if (!Active || m?.Body == null) return;
            var me = DynelManager.LocalPlayer;
            if (me == null) return;
            // Completion, as MissionController reads it: CharacterAction MissionChanged (0x3B) for the holder,
            // or the quest removal.
            if (m.Body is CharacterActionMessage ca && (int)ca.Action == 0x3B && ca.Identity.Instance == me.Identity.Instance && _current != null
                && (_phase == Phase.Blitz || _phase == Phase.AwaitBlitz))
                MarkDone("MissionChanged");
            else if (m.Body is QuestMessage qm && qm.Identity.Instance == me.Identity.Instance && _current != null
                     && (_phase == Phase.Blitz || _phase == Phase.AwaitBlitz))
                MarkDone("quest removed");
        }

        private void MarkDone(string how)
        {
            if (_completed) return;
            _completed = true;
            _ctx.Log($"MISSIONRUN: mission complete ({how}).");
            if (_current != null) RememberDone(_current.Playfield.Instance, _current.Location);
        }

        private void OnList(IReadOnlyList<MissionInfo> list)
        {
            if (_phase != Phase.AwaitList) return;
            var ok = list.Where(Fits).ToList();
            int dangerous = ok.RemoveAll(m => Dangerous(m.Playfield.Instance));
            if (dangerous > 0) _ctx.Log($"MISSIONRUN: roll {_rolls}: left {dangerous} mission(s) in zones I died in lately.");
            int hostile = ok.RemoveAll(m => HostileAt(m.Playfield.Instance, m.Location.X, m.Location.Z) != null);
            if (hostile > 0) _ctx.Log($"MISSIONRUN: roll {_rolls}: left {hostile} mission(s) by the other side's guards.");
            if (ok.Count == 0) { _ctx.Log($"MISSIONRUN: roll {_rolls}: nothing I can take."); Enter(Phase.Rolling, "nothing suitable"); return; }
            var me = DynelManager.LocalPlayer;
            // The cheapest trip to the door wins: the zone router's cost (metres of walking plus a fixed cost per
            // crossing), with the options travel plans with, so the weight is the route he will actually take.
            // A door with no route at all is left alone.
            var weighed = ok.Select(x => (m: x, cost: TravelCost(me, x))).ToList();
            _ctx.Log($"MISSIONRUN: roll {_rolls} travel weights: {string.Join("; ", weighed.Select(w => $"{Zoning.Name(w.m.Playfield.Instance)} ({w.m.Location.X:0},{w.m.Location.Z:0}) {(w.cost.HasValue ? w.cost.Value.ToString("0") : "no route")}"))}");
            weighed = weighed.Where(w => w.cost.HasValue).ToList();
            if (weighed.Count == 0) { Enter(Phase.Rolling, "no route to any door offered"); return; }
            var pick = weighed.OrderBy(w => w.cost.Value).First().m;
            _current = pick; _completed = false; _travelTries = 0; _travelBacks = 0; _deathsHere = 0; _doorTries = 0; _door = null;
            _rewardIds.Clear();
            foreach (var r in pick.MissionItemData ?? new MissionItemReward[0]) { _rewardIds.Add((r.LowId, r.HighId)); RememberReward(r.LowId, r.HighId); }
            _roll.Accept(pick, s => _ctx.Log("MISSIONRUN: " + s));
            Save(pick);
            _cashBefore = DynelManager.LocalPlayer != null && DynelManager.LocalPlayer.TryGetStat(Stat.Cash, out int cb) ? cb : (int?)null;
            _tookLine = $"Took: {MissionRoll.Line(pick)} (after {_rolls} roll(s))";
            Enter(Phase.Accepting, "accepted");
        }

        /// <summary>What travel to a mission's door weighs, from where he stands: Zoning.FindRoute's cost (the route
        /// Zoning.Waypoints hands travel), with OverlandController's options plus the exits that failed him this
        /// session. Null when there is no route.</summary>
        private double? TravelCost(LocalPlayer me, MissionInfo m)
        {
            if (me == null) return null;
            var opt = Zoning.RouteOptions(me);
            opt.UseScotty = !NoScotty;   // RubiKa2019 has no Scotty at all (owner): cost the trip as walked
            opt.Filter = e => (e.Kind == ExitKind.ZoneLine || (e.Kind == ExitKind.Scotty && !NoScotty) || e.ObjInstance != 0) && !BadExit(e)
                              && !HostileExit(e) && !(Dangerous(e.ToPf) && e.ToPf != m.Playfield.Instance);
            try
            {
                var r = Zoning.FindRoute((int)Playfield.ModelId, me.Transform.Position, m.Playfield.Instance, new Vector3(m.Location.X, 0f, m.Location.Z), opt);
                return r?.Cost;
            }
            catch { return null; }
        }

        private bool Fits(MissionInfo m)
        {
            if (!MissionRoll.BlitzCan(m.MissionIcon)) return false;
            var types = _ctx.Config.MissionTypes;
            if (types != null && types.Count > 0 && !types.Any(t => string.Equals(t?.Trim(), MissionRoll.TypeName(m.MissionIcon), StringComparison.OrdinalIgnoreCase))) return false;
            var zones = _ctx.Config.MissionZones;
            if (zones == null || zones.Count == 0) return true;   // default: any zone (the pick still prefers the terminal's own)
            return _roll.Allowed(m, out _);
        }

        // ---- Tick ----------------------------------------------------------------------------------------

        /// <summary>One frame. Returns true while the run is walking the bot itself (into a door); the caller then
        /// lets follow's manual walker move it. Travel and blitz move the bot through their own Ticks.</summary>
        public bool Tick(LocalPlayer me, double dt)
        {
            _clock += dt; _phaseTime += dt;
            if (!Active || me == null) return false;
            RecordGood(me);

            // FIGHT FIRST. Walking on while mobs hit him is what killed him twice (21:56, 22:11): blitz marched
            // from room to room with two mobs on his back, melee weapon swinging at nothing. Anything moving him
            // stops; he stands and fights (combat + stims + pets as usual), and picks up where he was 3 s after.
            bool moving = _phase == Phase.Blitz || _phase == Phase.ToDoor || _phase == Phase.ToTerminal || _phase == Phase.Hike
                          || _phase == Phase.EnterDoor || _phase == Phase.Leaving || _phase == Phase.Backoff || _phase == Phase.ExitStand
                          || _phase == Phase.Stash || (_phase == Phase.Shop && (_shopStep == ShopStep.Travel || _shopStep == ShopStep.Exit));
            // SNARED: the walker moves at the snared speed now (BotContext.RunVelocity counts a negative Stat 156).
            // ROOTED (or snared in a way the stat doesn't show): there is no stat to read, but the server says
            // it: pulled back more than 5 m twice within 8 s. Stand still 15 s and try again, instead of walking
            // into the snap-back for minutes (2026-09-23 23:01). The owner: roots and snares both happen.
            // ...but never while something is hurting him: at a Longest Road door (07:00, 2026-09-24) he stood 15 s
            // 'held' while mobs beat him from 100% to 71%, then died 4 s after moving on.
            // And never while overland is SWIMMING: afloat the server corrects often (the surface trues our
            // float Y) — that is the mode working, not a pull (Newland lake, 2026-09-24).
            if (moving && _phase != Phase.Fight && !_overland.Swimming && _bigSnaps.Count(t => _clock - t < T("pullsecs")) >= T("pulls") && _clock - _lastHurt > 5)
            {
                _bigSnaps.Clear();
                // On the way somewhere it is a wall far more often than a root: ICC 07:02 (2026-09-24), pulled back
                // at the wall by the Grid, stood 15 s, then walked the same route into the same wall. The owner: "resetting
                // to last known good pos is a must". So walk the clean trail back and plan again from there.
                if (_phase == Phase.ToTerminal || _phase == Phase.ToDoor || _phase == Phase.Hike || (_phase == Phase.Shop && _shopStep == ShopStep.Travel))
                {
                    if (_overland.Active) _overland.Stop("pulled back");
                    _follow.ClearMovement();
                    // Pulled back AT a zone line is that line refusing him: Galway Shire's border, 10:35-10:37
                    // (2026-09-24), three pull-backs within 60 m of the line and the same line planned each time.
                    if (_phase == Phase.Hike && _hike?.Exit != null && _hike.Exit.Kind == ExitKind.ZoneLine
                        && Movement.Flat(me.Transform.Position, _hike.WalkTo ?? _hike.Exit.A) < 60f)
                        MarkBadExit(_hike.Exit);
                    // ...and any exit whose way there pulls him back three times, wherever (05:53-06:02, 2026-09-25,
                    // Wartorn Valley: pulled back ~85 m short of the Aegean line at (980,290), backed off, and planned
                    // the same line again for nine minutes).
                    else if (_phase == Phase.Hike && _hike?.Exit != null)
                    {
                        string k = ExitKey(_hike.Exit);
                        _exitPulls[k] = (_exitPulls.TryGetValue(k, out int np) ? np : 0) + 1;
                        if (_exitPulls[k] >= 3) MarkBadExit(_hike.Exit);
                    }
                    _travelReturn = _phase == Phase.Hike ? _hikeReturn : _phase;   // Shop keeps its step (Travel)
                    _ctx.Log($"MISSIONRUN: the server keeps pulling me back during {_phase}; back to my last good spot and planning again.");
                    StartBackoff(me, "travel");
                    return false;
                }
                // Held once at a spot inside a mission; pulled back there again, it is a wall, not a root: leave it
                // to the blitz, which blocks the cells it keeps walking into and gives up after 12 re-plans.
                // Holding again restarted its walk out (backoutside), which cleared those blocks every time: 19:55-
                // 20:02 (2026-09-24), ACD building, 18 holds at (53,5,237) re-planning the same 69 m route.
                // Inside the building, whichever step (blitz or exit stand): 02:19-02:22 (2026-09-25) the exit
                // stand set it back each time, and he was held at (35,5,17) over and over.
                if (_mission.InMission && _heldAt.HasValue && Movement.Flat(_heldAt.Value, me.Transform.Position) < 4f)
                    return false;
                _heldAt = _mission.InMission ? me.Transform.Position : (Vector3?)null;
                _heldUntil = _clock + T("held");
                _fightStart = _clock; _fightHpMin = 100;
                _fightReturn = _phase;
                if (_mission.Active) _mission.Stop("held");
                if (_overland.Active) _overland.Stop("held");
                _follow.ClearMovement();
                _ctx.Log($"MISSIONRUN: the server keeps pulling me back during {_phase} (rooted or snared?); standing still 15 s.");
                Enter(Phase.Fight, "held");
                return false;
            }
            int hpTick = _ctx.Status.SelfHpPct;
            if (hpTick >= 90) _hpHighAt = _clock;
            if (hpTick >= 0) { if (_prevHp >= 0 && hpTick < _prevHp) _lastHurt = _clock; _prevHp = hpTick; }
            // PINNED WHILE FLEEING (22:53, 2026-09-24, Holes in the Wall): he fled at 38% from three mobs, the server
            // held him at (41,6,87) - rooted - and he stood 11 s not fighting back, 38% -> dead. Not getting away
            // (under 3 m in 3 s) and still being hit: turn and fight, and no fleeing again for a while.
            // Any walk away - a flee, or walking out of a building after one (03:24, 2026-09-25, Borealis: leaving,
            // held at (57,5,164) by pull-backs, 58% -> dead in 10 s with two mobs on him and the flee's time
            // already up) - measured over the last 3 s.
            bool away = Fleeing || _phase == Phase.Leaving || _phase == Phase.Backoff;
            bool pinned = false;
            if (!away) { _pinSamplePos = me.Transform.Position; _pinSampleAt = _clock; }
            else if (_clock - _pinSampleAt >= 3)
            {
                pinned = _clock - _lastHurt < 3 && _clock >= _noFleeUntil && Movement.Flat(me.Transform.Position, _pinSamplePos) < 3f;
                _pinSamplePos = me.Transform.Position; _pinSampleAt = _clock;
            }
            // Outrun: 5 s into a flee and still being hit (04:21-04:22, 2026-09-25, Aegean: two Young Scab Hyenas
            // bit him all along two 81 m flees, 25% -> 0-11% -> dead; running only stopped him hitting back).
            if (Fleeing && _clock - _fleeStartedAt > 5 && _clock - _lastHurt < 1.5 && _clock >= _noFleeUntil) pinned = true;
            if (pinned)
            {
                StartFightBack(me, $"can't get away (at ({me.Transform.Position.X:0},{me.Transform.Position.Z:0}), {hpTick}% HP)");
                return false;
            }
            if (moving && _clock >= _fleeUntil && FightOrRun() && (_clock >= _fightIgnoreUntil || (hpTick >= 0 && hpTick < _ctx.Config.MissionFightBelowPercent)))
            {
                _fightStart = _clock; _fightHpMin = 100;
                _fightReturn = _phase;
                if (_mission.Active) _mission.Stop("fighting");
                if (_overland.Active) _overland.Stop("fighting");
                _follow.ClearMovement();
                _ctx.Log(string.Equals(_ctx.Config.MissionStyle, "fight", StringComparison.OrdinalIgnoreCase)
                    ? $"MISSIONRUN: attacked during {_phase}; standing to fight (fight style)."
                    : $"MISSIONRUN: turning to fight during {_phase} ({_ctx.Config.MissionFightAttackers}+ mobs on me, HP under {_ctx.Config.MissionFightBelowPercent}%, or under {_ctx.Config.MissionFightNoStimBelowPercent}% with no stim).");
                Enter(Phase.Fight, "fighting");
                return false;
            }

            switch (_phase)
            {
                case Phase.Fight:
                {
                    int hpNow = _ctx.Status.SelfHpPct;
                    if (hpNow >= 0) _fightHpMin = Math.Min(_fightHpMin, hpNow);
                    // FLEE (09:33, 2026-09-24): crossing Mutant Domain to a mission door, a pack of Hammer Broodlings
                    // (26-29) and Minibulls (30) caught him; he stood and fought, 100% -> 8% in 23 s with one stim,
                    // and died. Outside a mission, losing: drop the fight and run back along the trail he came by.
                    if (!_mission.InMission && hpNow >= 0 && hpNow < T("fleehp") && _clock - _lastHurt < 3 && _clock >= _noFleeUntil && StartFlee(me)) return true;
                    // INSIDE, losing (12:37, 2026-09-24, fight style): eight Aquaans and Junkbots (29-33) at a clan
                    // building's entrance held him at 1-7% HP for 10 s with the stim on its lock, and he died there.
                    // Drop the fight and walk out the exit; the mission is dropped unless it's already done.
                    if (_mission.InMission && hpNow >= 0 && hpNow < T("fleehp") && _clock - _lastHurt < 3 && _clock >= _fleeUntil && _clock >= _noFleeUntil)
                    {
                        var from = DynelManager.Npcs.Where(x => x != null && x.FightingIdentity.HasValue && x.FightingIdentity.Value == me.Identity).ToList();
                        foreach (var x in from) _combat.SetAside(me, x.Identity, T("fleesecs"));
                        FleeStarted(me, from);
                        if (me.IsAttacking) me.StopAttack();
                        _fleeUntil = _clock + T("fleesecs");
                        _ctx.Log($"MISSIONRUN: fleeing the mission at {hpNow}% HP from {from.Count} mob(s) ({string.Join(", ", from.Select(x => x.Name).Distinct())}): walking out.");
                        if (_completed) { _mission.Command("backoutside", OnOutsideReply); Enter(Phase.Leaving, "fleeing out"); }
                        else Skip("losing a fight inside it");
                        return false;
                    }
                    // A fight that isn't one: 12 minutes 'fighting' Kirby Schatz, the person a find-person mission
                    // sent him to (23:38-23:51, 2026-09-23), HP at 100% throughout and every blow answered with
                    // feedback 110. Combat never ends, so neither did this pause. 30 s without dropping under 90%
                    // HP: carry on, and don't stop for a fight again for a minute unless HP falls.
                    // Measured over the last 30 s, not the whole fight: at 00:22 (2026-09-24) one early hit to 68%
                    // kept him 'fighting' Levi McDannold, a find-person target 34 m off, for 15 minutes at 100% HP.
                    if (_clock - _fightStart > 30 && _clock - _lastHurt > 30 && hpNow >= 90 && _clock >= _heldUntil)
                    {
                        _fightIgnoreUntil = _clock + 60;
                        _ctx.Log("MISSIONRUN: 30 s of 'fighting' and nothing hurts me; carrying on.");
                    }
                    else
                    {
                        // CLOSE IN: he fights with a melee weapon and used to stand swinging at a mob 11-16 m away that
                        // shot him to death (06:33, 2026-09-24). Walk up to what we're fighting.
                        if (me.FightingIdentity.HasValue && !_combat.IsSetAside(me.FightingIdentity.Value))
                        {
                            var foe = DynelManager.Npcs.FirstOrDefault(n => n != null && n.Identity == me.FightingIdentity.Value);
                            // Only one that is hurting us: a mob that merely shows as fighting us from afar (the level
                            // 50 Watcher, 06:51) is not walked up to.
                            // ...but not into a nest: at 12:33 (2026-09-24) walking up to a level 27 Bloodcreeper took him
                            // down a slope among Bileswarm Defenders of level 116-118, dead in 3 s. Outside a mission,
                            // nothing too strong for him within 30 m of the foe, or he stays where he is.
                            bool nest = !_mission.InMission && DynelManager.Npcs.Any(n => n != null && n.Identity != foe?.Identity && TooStrong(me, n)
                                            && (!n.TryGetStat(Stat.Health, out int nh) || nh > 0) && foe != null && Vector3.Distance(n.Transform.Position, foe.Transform.Position) < 30f);
                            if (foe != null && !nest && me.DistanceFrom(foe) > 4f && _clock - _lastHurt < 5) { _phaseTime = 0; _follow.SetManualTarget(foe.Transform.Position); return true; }
                        }
                        // Stay until the fight is really over (not just back above the emergency line).
                        if (_ctx.Status.InCombat) { _follow.ClearManual(); _phaseTime = 0; return false; }
                        if (_clock < _heldUntil) return false;
                        if (_phaseTime < 3) return false;          // a moment for stragglers and loot
                        // Hurt or low on nano: stay put so the rest logic sits him down with a recharger (it starts
                        // 6 s after the last blow) instead of walking off into the next room half dead. At most a
                        // minute, in case the rest logic won't sit for a reason of its own.
                        // ...unless something is still hitting him with nothing left to fight (set-aside turrets,
                        // 06:33 2026-09-24): then get moving, stims on the way.
                        bool beingHit = _clock - _lastHurt < 5;
                        if (!beingHit && (_ctx.Status.Resting || _ctx.Status.NeedsRecovery) && _phaseTime < 60) return false;
                        if (beingHit) _ctx.Log("MISSIONRUN: still being hit with nothing I can fight; moving on.");
                        _ctx.Log("MISSIONRUN: fight over; carrying on.");
                    }
                }
                    switch (_fightReturn)
                    {
                        case Phase.Backoff when !_mission.InMission && (_travelReturn == Phase.ToDoor || _travelReturn == Phase.ToTerminal || _travelReturn == Phase.Shop):
                            // Held while backing off from a travel snag (08:36:59, 2026-09-24): back to that travel,
                            // not to the terminal - he had a mission in hand and went to roll another.
                            Enter(_travelReturn, "back to travel");
                            break;
                        case Phase.Blitz: case Phase.Backoff: case Phase.ExitStand:
                            if (_completed && _mission.InMission) { _mission.Command("backoutside", OnOutsideReply); Enter(Phase.Blitz, "back to walking out"); }
                            else if (_mission.InMission) { _resumeBlitz = true; Enter(Phase.AwaitBlitz, "back to the blitz"); }
                            else Enter(Phase.ToTerminal, "fight over");
                            break;
                        case Phase.Leaving: _mission.Command("backoutside", OnOutsideReply); Enter(Phase.Leaving, "back to leaving"); break;
                        case Phase.Hike: Enter(_hikeReturn, "back to travel"); break;
                        case Phase.Stash: Enter(Phase.Stash, "back to stashing"); break;
                        default: Enter(_fightReturn, "back to it"); break;
                    }
                    return false;

                case Phase.Dead:
                    // The owner's order after a death: run back to the mission terminal and wait out the rez
                    // sickness there (and rebuff); then go back to the open mission, or roll a new one.
                    if (_ctx.Status.Dead) { _phaseTime = 0; return false; }
                    if (_phaseTime < 3) return false;          // let the reclaim land
                    _afterDeath = true;
                    Enter(Phase.ToTerminal, "reclaimed; to the terminal to wait out rez sickness");
                    return false;

                case Phase.ToTerminal:
                    if (_shopAfterOut && !_mission.InMission) { _shopAfterOut = false; _shopTriedAt = -9999; if (StartShop("owner asked")) return false; }
                    if (_ctx.Status.Resting) { _phaseTime = 0; return false; }
                    if ((int)Playfield.ModelId == _termPf && !_overland.Active)
                    {
                        // Travel stops a few metres short and the terminal's own body keeps us ~5 m from its centre
                        // (2026-09-23 21:29: 'Arrived' at 5.4 m, over and over). So finish on foot, straight at it,
                        // and roll from wherever that ends within the roll's 6 m.
                        float dT = Movement.Flat(me.Transform.Position, _termPos);
                        if (dT <= T("termnear"))
                        {
                            _approach += dt;
                            if (dT <= T("termstop") || _approach > 5)
                            {
                                _follow.ClearManual();
                                if (dT <= T("termroll")) { Enter(Phase.Rolling, "at the terminal"); return false; }
                                _approach = 0;   // couldn't close in: travel again
                            }
                            else { _follow.SetManualTarget(_termPos); return true; }   // from the approach spot: clear of pads
                        }
                    }
                    return Travel(me, _termPf, TerminalApproach(), "the terminal");

                case Phase.Rolling:
                    if (_ctx.Status.Resting) { _phaseTime = 0; return false; }
                    if (_afterDeath)
                    {
                        // At the terminal after a death: sit out the sickness and let the rebuffs go on first.
                        // Buffed = nothing cast or queued for 15 s (the buff scan runs every 2 s and puts up one buff
                        // at a time, refilling nano between them) - not just a fixed pause after the sickness.
                        if (SupportController.IsRezSick(me) || me.IsCasting || Buffing) { _phaseTime = 0; return false; }
                        if (_phaseTime < 3) return false;
                        _afterDeath = false;
                        _tell("Rez sickness is over and my buffs are back up; back to work.");
                        // Twice dead in the same mission (23:14 and 23:20, 2026-09-23: the same pack in the same corner
                        // of an Omnilab) is a bad mission: drop it and roll another.
                        if (_current != null && !_completed && _deathsHere >= 2) { Skip($"died {_deathsHere} times in it"); return false; }
                        if (_current != null && !_completed && _diedOnWay) { _diedOnWay = false; Skip("I died on the way to its door"); return false; }
                        if (_current != null && !_completed) { _travelTries = 0; _doorTries = 0; Enter(Phase.ToDoor, "back to the open mission"); return false; }
                    }
                    if (_phaseTime < 1.5) return false;
                    // Out of room: he always needs 4 free inventory slots (not bags, not items) to pull the mission
                    // keys and rewards (owner, 2026-09-23), and the stash has already filled every bag it could. He
                    // can't go on; buying bags, selling and banking nano crystals come later (MISSION-MODE-PLAN.md).
                    if (Inventory.NumFreeSlots < 4 && StartShop($"only {Inventory.NumFreeSlots} free slot(s)")) return false;
                    // Low on stims or rechargers he can USE (owner, 2026-09-24: 'Low on stims (5 left)' - the rest a
                    // QL over his First Aid): off to Fair Trade to buy some, as the owner would if he logged him on.
                    if (Resupply != null && Resupply.NeedsResupply() && StartShop("low on stims I can use")) return false;
                    if (Inventory.NumFreeSlots < 4)
                    {
                        _tell($"I'm out of room: {Inventory.NumFreeSlots} free inventory slot(s) and no bag with space. Stopping the mission run after {_done} mission(s); clear some space and say 'mission run' again.");
                        Stop("out of inventory room");
                        return false;
                    }
                    if (_rolls >= MaxRolls)
                    {
                        // Nothing suitable for a long stretch: say so once, wait a minute, and keep rolling.
                        if (_phaseTime < 60) { if (!_rollWarned) { _rollWarned = true; _tell($"{MaxRolls} rolls and nothing I can take here; waiting a minute and rolling on."); } return false; }
                        _rolls = 0;
                    }
                    if (!((int)Playfield.ModelId == _termPf && Movement.Flat(me.Transform.Position, _termPos) <= 6f)) { Enter(Phase.ToTerminal, "not at the terminal"); return false; }
                    if (!TerminalFitsTeam(me)) return false;
                    // Rolling with no mission in hand: anything the quest log still holds is stale (failed, died in,
                    // or left from before a restart). The owner found three at 23:24 and cleared them by hand; the
                    // keys go with them.
                    if (_current == null && HeldMissionIds().Count > 0)
                    {
                        int n = DeleteHeldMissions();
                        _tell($"Cleared {n} old mission(s) from my log before rolling.");
                        return false;
                    }
                    _rolls++;
                    _roll.Roll(s => { });
                    Enter(Phase.AwaitList, "rolled");
                    return false;

                case Phase.AwaitList:
                    if (_phaseTime > ListTimeout) { _ctx.Log("MISSIONRUN: no list came back; rolling again."); Enter(Phase.Rolling, "no answer"); }
                    return false;

                case Phase.Accepting:
                    if (_phaseTime < 2) return false;
                    // Taking a mission costs credits, and he can run out (owner): say what he has once the server
                    // has charged it (the 2 s this phase waits).
                    {
                        string cash = me.TryGetStat(Stat.Cash, out int c)
                            ? $" Credits: {c:N0}" + (_cashBefore.HasValue && _cashBefore.Value != c ? $" ({c - _cashBefore.Value:+#,0;-#,0})." : ".")
                            : "";
                        _tell(_tookLine + "." + cash);
                    }
                    _rolls = 0;
                    Enter(Phase.ToDoor, "going to the door");
                    return false;

                case Phase.ToDoor:
                {
                    if (_ctx.Status.Resting) { _phaseTime = 0; return false; }
                    int pf = _current.Playfield.Instance;
                    Vector3 goal = new Vector3(_current.Location.X, _current.Location.Y, _current.Location.Z);
                    if (!_overland.Active && (int)Playfield.ModelId == pf && Movement.Flat(me.Transform.Position, goal) <= 12f)
                    { _door = FindDoor(pf, goal); _follow.ClearMovement(); _doorDir = -1; Enter(Phase.EnterDoor, "at the door"); return false; }
                    return Travel(me, pf, goal, "the mission door");
                }

                case Phase.EnterDoor:
                {
                    if (_mission.InMission) { Enter(Phase.AwaitBlitz, "inside"); _follow.ClearMovement(); return false; }
                    // The data gives the door's position but not which way it faces, so walking at its centre can
                    // run into the frame (2026-09-23 21:03: stuck 2 m off its side). Try it from each side in turn:
                    // travel to a spot 5 m out on a real route, then walk straight through the centre to the far
                    // side. The side it opens to takes us in; the walls stop the others.
                    Vector3 d = _door ?? new Vector3(_current.Location.X, _current.Location.Y, _current.Location.Z);
                    Vector3 pos = me.Transform.Position;
                    if (_doorDir < 0)
                    {
                        // First side: the door's FRONT when the zone's walls data can see it (open ground at
                        // the doorway — walking in from the side doesn't take), else the one we arrived from.
                        Vector3? front = _overland.FrontOf(d, pos);
                        double ang = front.HasValue ? Math.Atan2(front.Value.Z, front.Value.X)
                                                    : Math.Atan2(pos.Z - d.Z, pos.X - d.X);
                        _doorStart = (int)Math.Round(ang / (Math.PI / 4)) & 7;
                        _doorDir = 0; _doorStep = 0; _doorStepTime = 0;
                    }
                    _doorStepTime += dt;
                    if (_doorDir >= 8)
                    {
                        _follow.ClearMovement();
                        if (++_doorTries >= 2) { _doorTries = 0; Skip("can't get through its door from any side"); return false; }
                        _doorDir = -1;
                        return false;
                    }
                    int k = (_doorStart + (_doorDir % 2 == 0 ? _doorDir / 2 : 8 - (_doorDir + 1) / 2)) & 7;   // alternate either side of the first
                    double t = k * Math.PI / 4;
                    var side = new Vector3((float)Math.Cos(t), 0, (float)Math.Sin(t));
                    Vector3 outside = new Vector3(d.X + side.X * 5f, d.Y, d.Z + side.Z * 5f);
                    // Onto the door's own spot, not through it: the owner's client stopped within 0.2 m of the door's
                    // position and the server moved him in 0.4 s later (capture 20260923-201746, 20:30:49; the exit
                    // at 20:31:10 the same way). Follow's walker stops 1.5 m short of its target, so aim 1.2 m past.
                    Vector3 through = new Vector3(d.X - side.X * 1.2f, d.Y, d.Z - side.Z * 1.2f);
                    if (_doorStep == 0)
                    {
                        // Get to this side's spot on a real route (travelto: the zone's floor and wall grid), so a bot
                        // wedged in the door frame paths out first instead of pushing into the wall.
                        _follow.ClearManual();
                        if (!_overland.Active)
                        {
                            if (Movement.Flat(pos, outside) <= 3.5f) { _doorStep = 2; _doorStepTime = 0; return false; }
                            var args = new[] { outside.X.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), outside.Z.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), ((int)Playfield.ModelId).ToString() };
                            _overland.Command(args, s2 => _ctx.Log("MISSIONRUN: door approach: " + s2));
                            _doorStep = 1; _doorStepTime = 0;
                        }
                        return false;
                    }
                    if (_doorStep == 1)
                    {
                        if (_overland.Active && _doorStepTime < 30) return false;
                        if (_overland.Active) _overland.Stop("door approach too long");
                        _doorStep = 2; _doorStepTime = 0;
                        return false;
                    }
                    // Last few metres: onto the door's spot, then stand still and let the server take us.
                    if (_doorStep == 2) { _follow.SetManualTarget(through); _doorStep = 3; }
                    if (_doorStepTime > 5)
                    {
                        _ctx.Log($"MISSIONRUN: door side {k} didn't take me in; next side.");
                        _follow.ClearManual();
                        _doorDir++; _doorStep = 0; _doorStepTime = 0;
                        return false;
                    }
                    return _follow.ManualActive;   // walking onto it, then standing on it
                }

                case Phase.AwaitBlitz:
                {
                    if (_phaseTime < 2.5) return false;       // the quest update arrives just after the zone-in
                    // The owner's rule: judge the mission at the entrance, where leaving is one step away. Blitz's own
                    // planner says whether it has a walkable path to the target; none, or no way to look for it, and
                    // the mission is dropped right here instead of being fought over deep inside.
                    if (_blitzTries == 0 && !_resumeBlitz)
                    {
                        string report = null;
                        _mission.Command("route", r => report = r);
                        _ctx.Log("MISSIONRUN: at the entrance: " + report);
                        bool noPath = report != null && report.IndexOf("no walkable path", StringComparison.OrdinalIgnoreCase) >= 0;
                        bool noRoute = report != null && report.StartsWith("No route", StringComparison.OrdinalIgnoreCase)
                                       && report.IndexOf("search", StringComparison.OrdinalIgnoreCase) < 0;
                        if (noPath || noRoute) { Skip("I can't do it from the entrance: " + report); return false; }
                    }
                    _resumeBlitz = false;
                    _mission.Command("blitz", s => _ctx.Log("MISSIONRUN: blitz: " + s));
                    Enter(Phase.Blitz, "blitzing");
                    return false;
                }

                case Phase.Blitz:
                    if (_mission.Active)
                    {
                        if (_phaseTime > BlitzTimeout) Skip("it was taking too long");
                        return false;
                    }
                    if (_mission.InMission)
                    {
                        if (_completed) { if (!StartExitStand(me, Phase.Blitz)) StartBackoff(me, "walk out"); return false; }
                        // A fresh blitz clears the cells a run of server snap-backs blocked (2026-09-23 21:31: two
                        // snaps at the start blocked the only way and it gave up), so try again before giving up.
                        if (++_blitzTries <= 1 && _phaseTime > 3)
                        {
                            _ctx.Log($"MISSIONRUN: blitz ended before the mission was done; trying once more.");
                            StartBackoff(me, "blitz");
                            return false;
                        }
                        if (_blitzTries <= 1) return false;
                        Skip("blitz failed twice - check my log (MISSION: lines)");
                        return false;
                    }
                    if (!_completed) { Skip("I ended up outside without completing it"); return false; }
                    _done++;
                    _tell($"Mission {_done} done.");
                    _current = null;
                    ClearSaved();
                    StartStash();
                    return false;

                case Phase.Stash:
                    StashTick(me);
                    return false;

                case Phase.Backoff:
                {
                    // The owner's way out of a spot the server keeps stopping you at: turn round, run back to the
                    // edge of the room, turn again and go. Up to 6 m back along our own facing, then retry.
                    if (!_mission.InMission && _backoffNext != "blitz" && _backoffNext != "travel" && _backoffNext != "flee") { _follow.ClearManual(); Enter(_backoffNext == "leave" ? Phase.ToTerminal : Phase.Blitz, "out"); return false; }
                    bool replaying = _follow.ReplayCount > 0 && !_follow.ManualActive;
                    bool done = _phaseTime > (replaying ? 25 : 4) || (_backoffTo.HasValue && Movement.Flat(me.Transform.Position, _backoffTo.Value) <= 1.2f);
                    if (!done && replaying) return true;
                    if (!done && _backoffTo.HasValue) { _follow.SetManualTarget(_backoffTo.Value); return true; }
                    _follow.ClearMovement();
                    switch (_backoffNext)
                    {
                        case "blitz": Enter(Phase.AwaitBlitz, "blitz again"); break;
                        case "travel": Enter(_travelReturn, "travel again from a good spot"); break;
                        case "flee":
                            // Not away while still being bitten: stand and fight what followed (04:22, 2026-09-25).
                            if (_clock - _lastHurt < 5) { StartFightBack(me, "still hit at the end of the flee"); return false; }
                            if (_travelReturn == Phase.ToDoor && _current != null && !_completed) { Skip("a pack I couldn't beat is on the way to its door"); break; }
                            Enter(_travelReturn == Phase.ToDoor || _travelReturn == Phase.ToTerminal || _travelReturn == Phase.Shop ? _travelReturn : Phase.ToTerminal, "got away");
                            break;
                        case "leave": _mission.Command("backoutside", OnOutsideReply); Enter(Phase.Leaving, "walking out again"); break;
                        default: _mission.Command("backoutside", OnOutsideReply); Enter(Phase.Blitz, "walking out"); break;
                    }
                    return false;
                }

                case Phase.Hike:
                    return HikeTick(me);

                case Phase.Shop:
                    return ShopTick(me);

                case Phase.ExitStand:
                {
                    if (!_mission.InMission) { _follow.ClearMovement(); Enter(_exitReturn == Phase.Leaving ? Phase.ToTerminal : Phase.Blitz, "out through the exit"); return false; }
                    if (_exitStep == 0) { _follow.SetManualTarget(_exitAim); _exitStep = 1; }
                    if (_phaseTime > 7)
                    {
                        _follow.ClearMovement();
                        _ctx.Log("MISSIONRUN: standing on the exit didn't take me out.");
                        if (_exitReturn == Phase.Leaving) Enter(Phase.Leaving, "exit stand failed"); else StartBackoff(me, "walk out");
                        return false;
                    }
                    return _follow.ManualActive;
                }

                case Phase.Leaving:
                    if (!_mission.InMission) { Enter(Phase.ToTerminal, "outside"); return false; }
                    if (!_mission.Active)
                    {
                        if (_phaseTime > 60 && !_leaveWarned) { _leaveWarned = true; _tell("I'm having trouble walking out of the mission building; still trying."); }
                        if (_phaseTime > 5) { if (!StartExitStand(me, Phase.Leaving)) StartBackoff(me, "leave"); return false; }
                    }
                    return false;
            }
            return false;
        }

        private int _doorDir = -1, _doorStart, _doorStep;
        private double _approach, _travelWaitUntil;
        private int _travelBacks, _deathsHere;
        private int? _cashBefore;
        private string _tookLine = "";
        private Phase _travelReturn;
        private Phase _fightReturn;
        private bool _resumeBlitz;
        private Vector3? _backoffTo;
        private string _backoffNext;

        // GOOD POSITIONS: inside a building, every 1.5 m walked without a server correction in the last 2 s is
        // kept (up to 200). They are ground the server has accepted, so walking them backwards cannot hit a wall.
        private readonly List<Vector3> _good = new List<Vector3>();
        private double _lastCorrection = -99;
        private int _backoffs;

        /// <summary>Main: the server corrected our position (SetPos).</summary>
        public void OnServerCorrection(float gap)
        {
            _lastCorrection = _clock;
            if (gap > T("pullgap")) { _bigSnaps.Add(_clock); if (_bigSnaps.Count > 20) _bigSnaps.RemoveAt(0); }
        }
        private readonly List<double> _bigSnaps = new List<double>();
        private Vector3? _heldAt;
        private double _heldUntil = -1;
        private int _goodPf = -1;
        private bool _goodInMission;

        private void RecordGood(LocalPlayer me)
        {
            int pfNow = (int)Playfield.ModelId; bool inM = _mission.InMission;
            if (pfNow != _goodPf || inM != _goodInMission) { _good.Clear(); _backoffs = 0; _goodPf = pfNow; _goodInMission = inM; }
            if (_clock - _lastCorrection < 2) return;
            Vector3 p = me.Transform.Position;
            if (_good.Count == 0 || Vector3.Distance(_good[_good.Count - 1], p) >= 1.5f)
            {
                _good.Add(p);
                if (_good.Count > 200) _good.RemoveAt(0);
            }
        }

        // ---- Hike: walk to the first exit of the zone route ourselves ----------------------------------------
        // Travel plans the zone route well (ICC -> Newland whompa -> Borealis whompa), but its walking grid can
        // call the way to the first exit 'walled off' (ICC, pf 655, 4 m cells: 2026-09-23 21:58) and then fall back
        // on Scotty, who has never answered this bot. So: take the planner's route without Scotty, walk straight to
        // its first exit, step onto it / use it / cross it, and once the zone changes hand back to travel.
        private ZoneHop _hike;
        private int _hikeFromPf, _hikeTargetPf;
        private Vector3 _hikeGoal;
        private string _hikeWhat;
        private Phase _hikeReturn;
        private double _hikeUsedAt = -99, _hikeLastHike = -99;
        private List<Vector3> _hikeRoute;
        private Vector3? _hikeBackTo, _hikeCameFrom;
        private double _hikeBackAt, _hikeOnAt = -1;
        private int _hikePass = -1, _hikePassStage;
        private double _hikePassAt;
        private bool _hikeUsedHere;
        private int _hikeUses;
        private Vector3 _hikeDir0;

        // The zone's walk grid (Algorithman's OverlandGrid outdoors, FloorGrid indoors), off the frame
        // thread — the ONE cache shared with overland travel (both only ever ask for the playfield they
        // stand in), so a zone builds once per visit and persists through GridCache.
        private NavGridCache _hikeNav => _ctx.NavGrid;
        private int _hikeGridPf = -1;
        private IWalkGrid _hikeGrid;

        private IWalkGrid HikeGrid()
        {
            int pf = (int)Playfield.ModelId;
            if (!_hikeNav.Request(pf, _pluginDir, _ctx.Log, "MISSIONRUN")) return null;
            if (_hikeGridPf != pf)
            {
                _hikeGridPf = pf;
                _hikeGrid = _hikeNav.Grid;
                _hikeGround = _hikeNav.Nav?.Ground; _hikeGroundPf = pf;
                if (_hikeGrid == null) { _hikeRoute = new List<Vector3>(); }   // no grid: straight
            }
            return _hikeGrid;
        }

        private int _hikeChain;
        private double _hikeAtExitAt = -1;
        private int _hikeChainFrom = -1;
        private volatile NavGround _hikeGround;
        private volatile int _hikeGroundPf = -1;

        // ON THE GROUND BETWEEN POINTS (2026-09-24): the grid's route is smoothed into long straight legs, and the
        // walker moves along each leg in a straight 3D line, so over a hill it walks INSIDE the hill. In Galway
        // Shire it sent heights of 27 and 25 where the ground is 37 and 33 - and our ground data matched the
        // server there (919,1055: 37.3 vs 37; 929,1054: 33.0 vs 33) - so the server snapped him back again and
        // again. Every leg is cut into 3 m steps with the ground's height at each, where both ends of the leg
        // are on the ground (within 3 m of it); legs off the ground (floors, bridges) are left straight.
        private List<Vector3> OnGround(IEnumerable<Vector3> pts, Vector3 from)
        {
            var list = pts.ToList();
            var g = _hikeGroundPf == (int)Playfield.ModelId ? _hikeGround : null;
            if (g == null || list.Count == 0) return list;
            var outp = new List<Vector3>();
            Vector3 a = from;
            foreach (var b in list)
            {
                double ha = g.HeightAt(a.X, a.Z), hb = g.HeightAt(b.X, b.Z);
                bool ground = !double.IsNaN(ha) && !double.IsNaN(hb) && Math.Abs(a.Y - ha) < 3 && Math.Abs(b.Y - hb) < 3;
                float len = Movement.Flat(a, b);
                int n = ground ? (int)(len / 3f) : 0;
                for (int k = 1; k <= n; k++)
                {
                    float t = k / (float)(n + 1);
                    float x = a.X + (b.X - a.X) * t, z = a.Z + (b.Z - a.Z) * t;
                    double h = g.HeightAt(x, z);
                    outp.Add(new Vector3(x, double.IsNaN(h) ? a.Y + (b.Y - a.Y) * t : (float)h, z));
                }
                outp.Add(ground ? new Vector3(b.X, (float)hb, b.Z) : b);
                a = b;
            }
            return outp;
        }

        private bool StartHike(LocalPlayer me, int pf, Vector3 goal, string what)
        {
            if (_clock - _hikeLastHike < 20) return false;          // one attempt at a time
            int here = (int)Playfield.ModelId;
            if (here == pf) return false;                            // same zone: nothing to cross
            var opt = Zoning.RouteOptions(me);
            // opt.UseScotty = false;   // the hike crosses on foot: doors stood on, not Scotty
            // Not through the Grid: its lifts and exits are gated by Computer Literacy and none took him
            // (four Grid lines stood on 4 times each, 08:33-08:36, 2026-09-24). The owner: the whompa is the best bet.
            opt.Filter = e => (e.Kind == ExitKind.ZoneLine || e.ObjInstance != 0) && !BadExit(e) && e.ToPf != 152 && e.FromPf != 152;
            ZoneRoute route;
            // Round zones he died in lately when there is another way (The Longest Road, 12:33, 2026-09-24: marked
            // at 12:14, then walked through again on the way to Athen Shire and killed there).
            var plainOpen = opt.Filter;
            Func<ZoneExit, bool> plain = e => (plainOpen == null || plainOpen(e)) && !HostileExit(e);
            // Never straight back into the zone this chain just came from (15:39-15:40, 2026-09-24: ten crossings
            // between Stret West Bank and Holes in the Wall - each landing sits by the line, and from there the way
            // back over it looked cheapest). Tried first; dropped only if it leaves no route.
            int back = _hikeChain > 0 ? _hikeChainFrom : -1;
            opt.Filter = e => plain(e) && !(Dangerous(e.ToPf) && e.ToPf != pf) && e.ToPf != back;
            try { route = Zoning.FindRoute(here, me.Transform.Position, pf, goal, opt); } catch { route = null; }
            if (route == null || route.Hops.Count == 0)
            {
                opt.Filter = e => plain(e) && e.ToPf != back;
                try { route = Zoning.FindRoute(here, me.Transform.Position, pf, goal, opt); } catch { route = null; }
            }
            if (route == null || route.Hops.Count == 0)
            {
                opt.Filter = plain;
                try { route = Zoning.FindRoute(here, me.Transform.Position, pf, goal, opt); } catch { route = null; }
            }
            if (route == null || route.Hops.Count == 0) { _hikeLastHike = _clock; _ctx.Log("MISSIONRUN: no zone route without Scotty either."); return false; }
            _hike = route.Hops[0]; _hikeFromPf = here; _hikeTargetPf = pf; _hikeGoal = goal; _hikeWhat = what;
            _hikeReturn = _phase; _hikeLastHike = _clock; _hikePass = -1; _hikePassStage = 0; _hikePassAt = _clock; _hikeUses = 0; _hikeUsedAt = -99; _hikeRoute = null; _hikeAtExitAt = -1; _hikeBackTo = null; _hikeCameFrom = null; _hikeOnAt = -1;
            if (_overland.Active) _overland.Stop("mission run walks this leg itself");
            var e = _hike.Exit;
            _ctx.Log($"MISSIONRUN: walking to the first exit myself: {e} at ({e.A.X:0},{e.A.Z:0}) ({route.Describe()}).");
            Enter(Phase.Hike, "walking to the exit myself");
            return true;
        }

        private bool HikeTick(LocalPlayer me)
        {
            // A teleporter inside the zone (Lush Fields 695 -> 695) never changes the playfield: through it = far
            // from it after a use. 09:21:51 (2026-09-24): the third use took him 2 km, and the hike called it a
            // failure and marked the teleporter bad.
            bool jumped = _hike.Exit.ToPf == _hikeFromPf && (int)Playfield.ModelId == _hikeFromPf && _hikeUses > 0
                          && Movement.Flat(me.Transform.Position, _hike.Exit.A) > 60f;
            if ((int)Playfield.ModelId != _hikeFromPf || jumped)
            {
                _follow.ClearMovement();
                int now = (int)Playfield.ModelId;
                // EVERY crossing on foot, not just the first (08:25-08:30, 2026-09-24): handed to travel in the ICC
                // (Andromeda 655), it tried the Jobe, Tir and Omni-1 Trade whompas at ground height, three tries
                // each, detoured through the Grid and came back to the same whompas. Chain the next hike at once
                // (the 20 s gap between hikes is for failed ones); after 10 crossings travel takes over.
                if (now != _hikeTargetPf && ++_hikeChain <= T("chain"))
                {
                    _hikeChainFrom = _hikeFromPf;
                    _ctx.Log($"MISSIONRUN: through to {Zoning.Name(now)}; on to the next crossing myself ({_hikeChain}; stand try {_hikePass + 1}, use try {_hikeUses}, {StandTune()}).");
                    Enter(_hikeReturn, "through the exit");
                    _hikeLastHike = -99;
                    if (StartHike(me, _hikeTargetPf, _hikeGoal, _hikeWhat)) return false;
                    return false;
                }
                _hikeChain = 0;
                _ctx.Log($"MISSIONRUN: through to {Zoning.Name(now)}; travel takes it from here.");
                Enter(_hikeReturn, "through the exit");
                return false;
            }
            // 150 s AT the exit, not from the start: the walk there can be over a kilometre (Eastern Fouls Plains,
            // 13:09-13:15, 2026-09-24: two good lines timed out mid-walk, marked bad, 4-5 km detours). The walk
            // itself gets 15 minutes.
            if ((_hikeAtExitAt >= 0 && _clock - _hikeAtExitAt > 150) || _phaseTime > 900)
            {
                _follow.ClearMovement();
                MarkBadExit(_hike.Exit);   // or the next plan picks the same exit (four times, 09:50-10:00)
                _ctx.Log("MISSIONRUN: couldn't get through that exit on foot; routing round it.");
                Enter(_hikeReturn, "hike failed");
                return false;
            }
            var e = _hike.Exit;
            Vector3 pos = me.Transform.Position;
            Vector3 at = _hike.WalkTo ?? e.A;

            // First leg on the zone's walk grid: to the reachable ground nearest the exit. ICC (pf 655): the
            // whompa stands in a pocket whose opening is narrower than the grid's 4 m cells, so the exit itself is
            // unreachable on the grid while the ground around the reclaim connects for 60 m (probe 2026-09-23).
            if (_hikeRoute == null)
            {
                var grid = HikeGrid();
                if (grid == null) return false;                       // still building (a few seconds)
                _hikeRoute = new List<Vector3>();
                var best = NearestPath(grid, pos, at, out float bestLeft);
                if (best != null && best.Count > 1)
                {
                    _hikeRoute = best;
                    _follow.LoadReplay(OnGround(best.Skip(1), pos), false);
                    _ctx.Log($"MISSIONRUN: grid route to {bestLeft:0} m from the exit ({best.Count} points), then straight on.");
                }
                else _ctx.Log("MISSIONRUN: no grid route toward the exit; walking straight.");
            }
            // STALLED: not a metre in 30 s on the way to the exit (01:30-01:32, 2026-09-25, Aegean: the grid leg's
            // last point skipped as blocked 14 m short of the Stret West Bank line, and he stood there - the walk
            // to an exit may take 15 minutes). That exit is out; plan another.
            // (reset after any time away from this step - a fight, a hold - so only a stall inside the hike counts)
            if (!_hikeStillAt.HasValue || Movement.Flat(pos, _hikeStillAt.Value) > 1f || _clock - _hikeStillTick > 2) { _hikeStillAt = pos; _hikeStillSince = _clock; }
            _hikeStillTick = _clock;
            if (_clock - _hikeStillSince > 30 && _hikePassStage == 0 && _hikeUses == 0)
            {
                _follow.ClearMovement();
                _ctx.Log($"MISSIONRUN: stood still 30 s {Movement.Flat(pos, at):0} m from {e}; trying another way.");
                MarkBadExit(e);
                _hikeStillAt = null;
                Enter(_hikeReturn, "hike stalled");
                return false;
            }
            if (_follow.ReplayCount > 0) return true;                 // still on the grid leg
            if (_hikeAtExitAt < 0) _hikeAtExitAt = _clock;

            if (e.Kind == ExitKind.ZoneLine)
            {
                // Walk to the line, then on across it: 10 m past it, not 3 (the walker stops 1.5 m short of its
                // target, so aiming 3 m past from 1.7 m off never moved him: Galway Shire -> Galway County,
                // 09:50-10:00, 2026-09-24, four 150 s tries standing on the line). Which side is 'past' is a guess
                // from the line's geometry, so every 12 s the other side is tried.
                if (Movement.Flat(pos, at) > 2f && _hikePassStage == 0) { _follow.SetManualTarget(at); return true; }
                if (_hikePassStage == 0) { _hikePassStage = 1; _hikePassAt = _clock; }
                Vector3 c = _hike.CrossTo ?? at;
                var d = new Vector3(c.X - at.X, 0, c.Z - at.Z);
                if (d.Magnitude < 0.5f) { var ab = e.B - e.A; d = new Vector3(-ab.Z, 0, ab.X); }
                if (d.Magnitude < 0.1f) d = new Vector3(at.X - pos.X, 0, at.Z - pos.Z);
                if (d.Magnitude < 0.1f) d = new Vector3(1, 0, 0);
                d = d * (1f / d.Magnitude);
                int side = ((int)((_clock - _hikePassAt) / 12)) % 2 == 0 ? 1 : -1;
                _follow.SetManualTarget(new Vector3(at.X + d.X * 10f * side, at.Y, at.Z + d.Z * 10f * side));
                return true;
            }
            // An object that is USED (the Grid terminal, a proxy): walk up to it, stand, and use it, the way travel
            // does it (ICC Grid terminal, first try, 23:18 and 23:37). Three tries 4 s apart.
            // Only a TERMINAL is used (the Grid terminal, 23:18 and 23:37). A DOOR (type 51016, whompas and shop
            // doors alike) is entered by standing on it: the owner walked onto the Borealis Fair Trade door
            // (649.96,611.42 by its centre 650.25,612.86) and was zoned (capture 20260923-234203); the Newland Desert
            // Fair Trade door was Used six times with no zone (07:59, 2026-09-24).
            if (e.Kind != ExitKind.Line && e.ObjType != 51016)
            {
                if (Movement.Flat(pos, at) > 3f && _hikeUses == 0 && _clock - _hikeAtExitAt < 140) { _follow.SetManualTarget(at); return true; }
                _follow.ClearMovement();
                if (_clock - _hikeUsedAt < 4) return false;
                if (_hikeUses >= T("usetries"))
                {
                    MarkBadExit(e);
                    _ctx.Log($"MISSIONRUN: used {e} three times and it didn't take me.");
                    Enter(_hikeReturn, "hike failed");
                    return false;
                }
                _hikeUses++; _hikeUsedAt = _clock;
                GameCommands.UseObject(me, new Identity((IdentityType)e.ObjType, e.ObjInstance));
                _ctx.Log($"MISSIONRUN: used {e} (try {_hikeUses}).");
                return false;
            }
            // STOP ON ITS CENTRE. The owner's client at the ICC Newland whompa (capture 20260923-234203, 23:43:59):
            // walked in from 3165.6,868.0 and stopped at 3173.45,865.94 - 0.1 m from the centre in our data
            // (3173.47,866.01) - reporting height 36.175, the pad's top, 0.285 m above the 35.89 in our data; the
            // server zoned him right after that stop. No Use. The bot stood 3.5 m off it (travel, 23:17 and
            // 23:37) or ran across it without stopping (this hike, 23:36) and was never taken. So: from 5 m out,
            // walk onto the centre and stop, then wait 4 s. Odd tries stand at our data's height, even ones at
            // the owner's pad height (+0.285, measured at this whompa only); the log says which one took him.
            if (_hikePass < 0)
            {
                Vector3 from = _hikeCameFrom ?? pos;
                var d0 = new Vector3(e.A.X - from.X, 0, e.A.Z - from.Z);
                if (d0.Magnitude < 0.5f) d0 = new Vector3(1, 0, 0);
                _hikeDir0 = d0 * (1f / d0.Magnitude);
                _hikePass = 0; _hikePassStage = 0; _hikePassAt = _clock;
            }
            if (_hikePass >= T("standtries"))
            {
                MarkBadExit(e);
                _follow.ClearMovement();
                _ctx.Log($"MISSIONRUN: stood on the exit's centre {_hikePass} times and it didn't take me; leaving it ({StandTune()}).");
                Enter(_hikeReturn, "hike failed");
                return false;
            }
            double ang = (_hikePass / 2) * Math.PI / 2;
            var dir = new Vector3((float)(_hikeDir0.X * Math.Cos(ang) - _hikeDir0.Z * Math.Sin(ang)), 0, (float)(_hikeDir0.X * Math.Sin(ang) + _hikeDir0.Z * Math.Cos(ang)));
            // CONFIRMED 00:14 (2026-09-24): 0.3 m from the centre at 35.74 did nothing for 12 s; 0.2 m at 36.05
            // zoned him 0.6 s later. The pad's top first, then our data's height.
            // A door's recorded position is its centre, ~1.4 m up (Borealis Fair Trade door 68.49 over ground 67.07,
            // where the owner stood): stand on the ground. Only a whompa pad needs its top surface.
            float padY = e.Kind == ExitKind.Proxy ? pos.Y : e.A.Y + (_hikePass % 2 == 0 ? T("padtop") : 0f);
            var start = new Vector3(e.A.X - dir.X * 5f, pos.Y, e.A.Z - dir.Z * 5f);
            // The walker stops 1.5 m short of its target: aim 1.2 m past the centre to stop ~0.3 m before it.
            var aim = new Vector3(e.A.X + dir.X * T("aimpast"), padY, e.A.Z + dir.Z * T("aimpast"));
            if (_hikePassStage == 0)
            {
                if (Movement.Flat(pos, start) > 1.2f && _clock - _hikePassAt < 8) { _follow.SetManualTarget(start); return true; }
                _hikePassStage = 1; _hikePassAt = _clock;
                _ctx.Log($"MISSIONRUN: stepping onto the centre of {e} (try {_hikePass + 1}/4, height {padY:0.00}).");
            }
            if (_hikePassStage == 1)
            {
                if (_follow.ManualActive || _clock - _hikePassAt < 0.3) { _follow.SetManualTarget(aim); if (_clock - _hikePassAt < 6) return true; }
                _follow.ClearMovement();
                _hikePassStage = 2; _hikePassAt = _clock;
                _ctx.Log($"MISSIONRUN: standing at ({pos.X:0.00},{pos.Y:0.00},{pos.Z:0.00}), {Movement.Flat(pos, e.A):0.0} m from the centre.");
            }
            if (_clock - _hikePassAt < T("standwait")) return false;   // standing on it: the zone comes after the stop
            _hikePass++; _hikePassStage = 0; _hikePassAt = _clock;
            return false;
        }

        // THE EXIT, the owner's way: walk onto the exit door's spot and stand still. His client stopped 0.8 m
        // short of the door's position and the server moved him out 0.2 s later (capture 20260923-201746,
        // 20:31:10); blitz pushes 8 m through the door instead and sometimes doesn't leave. The door's
        // position is the one blitz names in its 'Heading outside: the exit door on floor F (x,z)' reply.
        private Vector3? _exitDoor;
        private Vector3 _exitAim;
        private int _exitStep, _exitStands;
        private Phase _exitReturn;

        private void OnOutsideReply(string text)
        {
            _ctx.Log("MISSIONRUN: " + text);
            var m = System.Text.RegularExpressions.Regex.Match(text ?? "", @"exit door on floor -?\d+ \((-?\d+),(-?\d+)\)");
            if (m.Success) _exitDoor = new Vector3(float.Parse(m.Groups[1].Value), 0, float.Parse(m.Groups[2].Value));
        }

        private bool StartExitStand(LocalPlayer me, Phase back)
        {
            if (!_exitDoor.HasValue || ++_exitStands > 3) return false;
            Vector3 pos = me.Transform.Position, d = _exitDoor.Value;
            var dir = new Vector3(d.X - pos.X, 0, d.Z - pos.Z);
            float len = dir.Magnitude;
            _exitAim = len > 0.1f ? new Vector3(d.X + dir.X / len * 1.2f, pos.Y, d.Z + dir.Z / len * 1.2f) : new Vector3(d.X, pos.Y, d.Z);
            _exitStep = 0; _exitReturn = back;
            _ctx.Log($"MISSIONRUN: walking onto the exit door at ({d.X:0},{d.Z:0}) and standing on it.");
            Enter(Phase.ExitStand, "onto the exit door");
            return true;
        }

        // The spot 4 m from the terminal to travel to: the side farthest from any pad or zone line, and whose last
        // few metres to the terminal pass no closer than 3 m to one. Borealis Backyard 5's entry pad sits 10 m from
        // the terminal on the straight way in, and the bot walked onto it (2026-09-23 22:45).
        private Vector3 TerminalApproach()
        {
            var lines = Zoning.ExitsFrom(_termPf).Where(e => e.Kind == ExitKind.Line || e.Kind == ExitKind.ZoneLine || e.Kind == ExitKind.Teleport).ToList();
            if (lines.Count == 0) return _termPos;
            Vector3 best = _termPos; float bestScore = float.MinValue;
            for (int k = 0; k < 8; k++)
            {
                double t = k * Math.PI / 4;
                var c = new Vector3(_termPos.X + (float)Math.Cos(t) * 4f, _termPos.Y, _termPos.Z + (float)Math.Sin(t) * 4f);
                float near = float.MaxValue;
                foreach (var e in lines)
                    for (int i = 0; i <= 4; i++)
                    {
                        float f = i / 4f;   // along the approach segment spot -> terminal
                        var q = new Vector3(c.X + (_termPos.X - c.X) * f, c.Y, c.Z + (_termPos.Z - c.Z) * f);
                        near = Math.Min(near, Movement.Flat(q, e.A));
                        if (e.Kind == ExitKind.ZoneLine) near = Math.Min(near, Movement.Flat(q, e.B));
                    }
                if (near > bestScore) { bestScore = near; best = c; }
            }
            return best;
        }

        // Exits that failed him this session (a whompa walked across from all four sides without a zone: ICC
        // Newland whompa, 23:36, 2026-09-23); the hike routes round them until a restart. Not saved: the whompa is
        // the route of choice (the Grid needs Computer Literacy, and some Grid exits are over his skill - the
        // planner checks those Reqs), and its failure is ours to fix, not the whompa's.
        private readonly HashSet<string> _badExits = new HashSet<string>();
        private static string ExitKey(ZoneExit e) => $"{e.FromPf}:{e.ObjType}:{e.ObjInstance}:{e.A.X:0}:{e.A.Z:0}";
        private bool BadExit(ZoneExit e) => _badExits.Contains(ExitKey(e)) || (e.Kind == ExitKind.ZoneLine && _badBorders.Contains((e.FromPf, e.ToPf)));
        private void MarkBadExit(ZoneExit e)
        {
            if (_badExits.Add(ExitKey(e)))
                _ctx.Log($"MISSIONRUN: {e} didn't take me; routing round it until I restart.");
            // A BORDER is a chain of zone-line segments: the Galway Shire -> Galway County border is 19 of them and
            // pulled him back at every one tried (09:46-10:09, 2026-09-24), the hike trying them one by one at up
            // to 150 s each. Two failed segments of one border close the whole border.
            if (e.Kind == ExitKind.ZoneLine)
            {
                var key = (e.FromPf, e.ToPf);
                _borderFails[key] = (_borderFails.TryGetValue(key, out int n) ? n : 0) + 1;
                if (_borderFails[key] >= 2 && _badBorders.Add(key))
                    _ctx.Log($"MISSIONRUN: the {Zoning.Name(e.FromPf)} -> {Zoning.Name(e.ToPf)} border failed twice; routing round all of it until I restart.");
            }
        }
        private readonly HashSet<(int, int)> _badBorders = new HashSet<(int, int)>();
        private readonly Dictionary<(int, int), int> _borderFails = new Dictionary<(int, int), int>();

        // RubiKa2019 has no Scotty (owner, 2026-09-24): a Scotty leg there is a wait for nobody.
        private static bool NoScotty => Client.Dimension == AOSharp.Clientless.Common.Dimension.RubiKa2019;

        // ---- Shop: housekeeping at Fair Trade (testing switch MissionShop) ---------------------------------------
        // The owner's design (2026-09-24, Algorithman agreed): bags full -> sell what isn't a nano crystal or on the
        // keep list (NOT BUILT: selling isn't captured yet) -> nanos go into a bag in the bank (buy one if no bank bag
        // has room, keeping MissionCashReserve) -> one more bag to carry if still short of room. From capture
        // 20260923-234203 (owner, Fair Trade): the proxy into playfield 1187 'Neutral Supermarket Advanced' at
        // (650,613) in Borealis; he stood at (197.7,142.4) inside and used both the container terminal
        // (VendingMachine 42685979 at (199,129)) and the bank terminal (C73D:0EE5BBFF) from there. The way out is
        // back the way he came in (owner): where he landed on zoning in, then on through it.
        public ResupplyController Resupply;
        private enum ShopStep { Travel, Sell, OpenBank, TakeBag, FillBag, StoreBag, Buy, Stims, Exit }
        private ShopStep _shopStep;
        private double _shopStepAt, _shopTriedAt = -9999;
        private const int FairTradePf = 1187;
        private static readonly Vector3 ShopSpot = new Vector3(197.74f, 5.01f, 142.38f);
        // The bank terminal is the nearest static 'Rubi-Ka Banking Service Terminal' (where to stand); the id to use
        // is its live one from the zone-in packet (Playfield.LiveIdentity). Capture 20260924-192208 (the alt): his
        // client used C73D:0EE4CB08 in Borealis Fair Trade and 0EE73632 in Newland's - the static id (C00104A3) and
        // the old capture's 0EE5BBFF were refused (GenericCmd echo Verification 2) every time.
        private static Dynel BankDynel
        {
            get
            {
                var me = DynelManager.LocalPlayer;
                return DynelManager.AllDynels.Where(d => d != null && d.Identity.Type == IdentityType.Terminal && d.Name != null
                                                         && d.Name.IndexOf("Bank", StringComparison.OrdinalIgnoreCase) >= 0)
                                             .OrderBy(d => me == null ? 0 : Vector3.Distance(me.Transform.Position, d.Transform.Position)).FirstOrDefault();
            }
        }
        private static Identity BankTerminal { get { var d = BankDynel; return d != null ? Playfield.LiveIdentity(d.Identity) : Identity.None; } }
        private Vector3? _shopArrival;
        private int _bankUses;
        private int _bankPulls;
        private Vector3? _hikeStillAt;
        private readonly Dictionary<string, int> _exitPulls = new Dictionary<string, int>();
        private double _hikeStillSince, _hikeStillTick = -99;
        private bool? _bankBuffWas;
        private double _bankQuietAt = -99;
        private double _bankPulledAt = -99;
        private double _bankUsedAt = -99;
        private Identity? _shopBag;
        private bool _shopBoughtForNanos, _shopBoughtForRoom;
        private Identity? _nanoSlot;
        private readonly HashSet<Identity> _shopKnownBags = new HashSet<Identity>();
        private readonly HashSet<Identity> _shopFullBags = new HashSet<Identity>();

        private int _sellRounds, _sellStage, _sellMoves;
        private List<Identity> _lastBatch;
        private Identity? _lastVendor;
        private int _wholeRefusals;
        private Identity? _sellWalkTo;
        private double _ftInAt = -1;
        private double _sellWalkAt = -99;
        private readonly HashSet<Identity> _badVendors = new HashSet<Identity>();
        private readonly HashSet<Identity> _refusedSlots = new HashSet<Identity>();
        private bool _sellBagsOpened;
        private double _sellSentAt = -99;

        // What housekeeping may sell: only items the run got as mission rewards (their ids are recorded at each
        // accept - so gear, keys and supplies never are), in the main inventory, not a bag, not a nano crystal, not
        // on KeepItems. Items inside bags aren't sold yet: that needs a capture of selling out of a bag.
        private List<Item> Sellable()
        {
            LoadRewards();
            var keep = KeepSet();
            return Inventory.Items.Where(i => i != null && i.Slot.Type == IdentityType.Inventory && SellableItem(i, keep)).ToList();
        }
        // THE OWNER'S RULES (2026-09-24): sell everything in the inventory and bags EXCEPT
        //   bags; nano crystals (they go to the bank); stims and rechargers; ammo when he fights at range (a melee
        //   loadout sells it - PetController.IsMeleeLoadout, from the weapon's AttackRange stat; ammo = the item data's
        //   'Ammo: Box of ...' names); anything in a bag marked personal; anything on the keep list (exact names).
        //   Equipped items are never looked at (only inventory and bag slots). Also kept, to be safe: names with
        //   'key' or 'mission' in them (a mission key's name isn't in the item data).
        private bool SellableItem(Item i, HashSet<string> keep)
        {
            if (i?.Name == null || i.UniqueIdentity.Type == IdentityType.Container || Bankable(i) || keep.Contains(i.Name)) return false;
            // NODROP is never sold (owner, 2026-09-24): the item data's Flags bit 26.
            if (ItemValues.IsNoDrop(i.Id, i.HighId)) return false;
            if (i.Name.IndexOf("key", StringComparison.OrdinalIgnoreCase) >= 0 || i.Name.IndexOf("mission", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (i.Name.StartsWith("Ammo:", StringComparison.OrdinalIgnoreCase) && !PetController.IsMeleeLoadout(DynelManager.LocalPlayer)) return false;
            return true;
        }

        // Sellable items still inside bags: they can't be sold from there (owner), so they're moved to the
        // inventory a few at a time first. The SDK names a bag item Backpack:(bag handle << 16 | slot).
        private List<Item> SellableInBags()
        {
            var keep = KeepSet();
            LoadPersonal();
            return Inventory.Containers.Where(c => c?.Items != null && !_personalBags.Contains(c.Identity)).SelectMany(c => c.Items).Where(i => i != null && SellableItem(i, keep)).ToList();
        }
        private HashSet<string> _rewardNames;

        // KEEP LIST: exact item names never sold - config KeepItems plus what the owner adds by command
        // ('mission run keep add <name>'), saved in keepitems.json. Stims and rechargers always.
        private HashSet<string> _keepAdded;
        private string KeepPath => Path.Combine(_pluginDir, "keepitems.json");
        private HashSet<string> KeepSet()
        {
            if (_keepAdded == null)
            {
                _keepAdded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try { foreach (var t in JsonStore.Load<JArray>(KeepPath, _ctx.Log) ?? new JArray()) _keepAdded.Add((string)t); } catch { }
            }
            var k = new HashSet<string>(_ctx.Config.KeepItems ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            k.UnionWith(_keepAdded);
            k.Add(_ctx.Config.ResupplyStimName); k.Add(_ctx.Config.ResupplyRechargerName);
            return k;
        }
        private void SaveKeep() { JsonStore.Save(KeepPath, new JArray(_keepAdded.ToArray()).ToString(), _ctx.Log); }

        // PERSONAL BAGS: nothing in them is ever sold. Marked by the bag's own identity ('mission run shop bags'
        // lists them numbered, 'mission run shop personal <n>' toggles), saved in personalbags.json.
        private HashSet<Identity> _personalBags;
        private string PersonalPath => Path.Combine(_pluginDir, "personalbags.json");
        private void LoadPersonal()
        {
            if (_personalBags != null) return;
            _personalBags = new HashSet<Identity>();
            try { foreach (var t in JsonStore.Load<JArray>(PersonalPath, _ctx.Log) ?? new JArray()) _personalBags.Add(new Identity((IdentityType)(int)t["type"], (int)t["id"])); } catch { }
        }
        private void SavePersonal() { JsonStore.Save(PersonalPath, new JArray(_personalBags.Select(b => new JObject { ["type"] = (int)b.Type, ["id"] = b.Instance })).ToString(), _ctx.Log); }
        private List<Item> Bags() => Inventory.Items.Where(i => i != null && i.Slot.Type == IdentityType.Inventory && i.UniqueIdentity.Type == IdentityType.Container).OrderBy(i => i.Slot.Instance).ToList();
        private HashSet<int> _rewardHistory;
        private string RewardsPath => Path.Combine(_pluginDir, "rewardids.json");
        private string RewardNamesPath => Path.Combine(_pluginDir, "rewardnames.json");   // names too: seeded from the log's accepted rewards
        private void LoadRewards()
        {
            if (_rewardHistory != null) return;
            _rewardHistory = new HashSet<int>();
            _rewardNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try { foreach (var t in JsonStore.Load<JArray>(RewardsPath, _ctx.Log) ?? new JArray()) _rewardHistory.Add((int)t); } catch { }
            try { foreach (var t in JsonStore.Load<JArray>(RewardNamesPath, _ctx.Log) ?? new JArray()) _rewardNames.Add((string)t); } catch { }
        }
        private void RememberReward(int low, int high)
        {
            LoadRewards();
            bool added = _rewardHistory.Add(low) | _rewardHistory.Add(high);
            if (added) JsonStore.Save(RewardsPath, new JArray(_rewardHistory.ToArray()).ToString(), _ctx.Log);
            if (ItemData.Find(low, out DummyItem it) && it?.Name != null && _rewardNames.Add(it.Name))
                JsonStore.Save(RewardNamesPath, new JArray(_rewardNames.ToArray()).ToString(), _ctx.Log);
        }
        public string SellPreview()
        {
            var s = Sellable(); var b = SellableInBags();
            if (s.Count + b.Count == 0) return "Nothing I'd sell right now (open bags are checked; a bag not opened this session isn't).";
            return $"I'd sell {s.Count + b.Count} ({b.Count} from bags): " + string.Join(", ", s.Concat(b).Select(i => i.Name));
        }

        private static bool IsNano(Item i) => i?.Name != null && (i.Name.StartsWith("Nano Crystal", StringComparison.OrdinalIgnoreCase) || i.Name.StartsWith("NanoCrystal", StringComparison.OrdinalIgnoreCase));
        private List<Item> InvNanos() => Inventory.Items.Where(i => i != null && i.Slot.Type == IdentityType.Inventory && Bankable(i)).ToList();

        // KEPT AND BANKED with the nanos (owner, 2026-09-24, for his 220 main): implants from QL <n> up.
        // 'mission run keep implant <ql>' / 'keep implant off'; saved in bankrules.json (per bot folder).
        // ...and items by exact name from a QL up (owner, 2026-09-24: 'QL 200+ Robot Junk is also a keeper').
        // 'mission run keep ql <ql> <exact name>' / 'keep ql off <exact name>'.
        private bool Bankable(Item i) => IsNano(i) || (i != null && ImplantMinQl() > 0 && i.Ql >= ImplantMinQl() && ItemValues.IsImplant(i.Id, i.HighId))
                                         || (i?.Name != null && NameRules().TryGetValue(i.Name, out int nq) && i.Ql >= nq);
        private int _implantMinQl = -1;
        private Dictionary<string, int> _nameRules;
        private string BankRulesPath => Path.Combine(_pluginDir, "bankrules.json");
        private void LoadBankRules()
        {
            if (_nameRules != null) return;
            _implantMinQl = 0; _nameRules = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var o = JsonStore.Load<JObject>(BankRulesPath, _ctx.Log);
                if (o == null) return;
                _implantMinQl = (int?)o["implantMinQl"] ?? 0;
                if (o["names"] is JObject n) foreach (var kv in n) _nameRules[kv.Key] = (int)kv.Value;
            }
            catch { }
        }
        private void SaveBankRules()
        {
            var n = new JObject(); foreach (var kv in _nameRules) n[kv.Key] = kv.Value;
            JsonStore.Save(BankRulesPath, new JObject { ["implantMinQl"] = _implantMinQl, ["names"] = n }.ToString(), _ctx.Log);
        }
        private int ImplantMinQl() { LoadBankRules(); return _implantMinQl; }
        private Dictionary<string, int> NameRules() { LoadBankRules(); return _nameRules; }
        private void SetImplantMinQl(int ql) { LoadBankRules(); _implantMinQl = ql; SaveBankRules(); }
        private static Item InvItem(Identity? unique) => unique.HasValue ? Inventory.Items.FirstOrDefault(i => i != null && i.Slot.Type == IdentityType.Inventory && i.UniqueIdentity == unique.Value) : null;

        private bool StartShop(string why)
        {
            if (!_ctx.Config.MissionShop || Resupply == null || _clock - _shopTriedAt < 600) return false;
            _shopTriedAt = _clock;
            _shopBag = null; _shopBoughtForNanos = false; _shopBoughtForRoom = false; _shopArrival = null; _shopFullBags.Clear(); _shopStimsTried = false;
            _ctx.Log($"MISSIONRUN: housekeeping ({why}): off to Fair Trade to sell, bank the keepers and make room.");
            _tell($"Going to Fair Trade ({why}): sell, bank my nano crystals, buy what I need. (Test switch: mission run shop on|off.)");
            _shopStep = ShopStep.Travel; _shopStepAt = _clock; _travelStarted = false; _travelTries = 0;
            Enter(Phase.Shop, "housekeeping");
            return true;
        }

        private void ShopNext(ShopStep s, string log)
        {
            // Auto-buff back on once the bank step is over (it is paused for the bank open).
            if (s != ShopStep.OpenBank && _bankBuffWas.HasValue) { _ctx.Config.AutoBuff = _bankBuffWas.Value; _bankBuffWas = null; }
            _shopStep = s; _shopStepAt = _clock; _ctx.Log("MISSIONRUN: shop: " + log);
        }

        private int _shopPrevPf = -1;
        private bool _shopAfterOut;
        private string FairTradePath => Path.Combine(_pluginDir, "fairtrade.json");
        private Vector3? LoadFairTradeLanding()
        {
            try
            {
                var o = JsonStore.Load<JObject>(FairTradePath, _ctx.Log);
                if (o == null) return null;
                return new Vector3((float)o["x"], (float)o["y"], (float)o["z"]);
            }
            catch { return null; }
        }
        private void SaveFairTradeLanding(Vector3 v)
        {
            JsonStore.Save(FairTradePath, new JObject { ["x"] = v.X, ["y"] = v.Y, ["z"] = v.Z }.ToString(), _ctx.Log);
        }

        private bool ShopTick(LocalPlayer me)
        {
            double t = _clock - _shopStepAt;
            int pf = (int)Playfield.ModelId;
            if (pf == FairTradePf && !_shopArrival.HasValue)
            {
                // Zoned in just now (the tick before was elsewhere): that's the door's landing spot - keep it. Logged in
                // inside (a restart, 13:25 2026-09-24): where he stands is no door; use the landing spot kept from before.
                bool zonedIn = _shopPrevPf > 0 && _shopPrevPf != FairTradePf;
                var kept = LoadFairTradeLanding();
                _shopArrival = zonedIn || !kept.HasValue ? me.Transform.Position : kept.Value;
                if (zonedIn) SaveFairTradeLanding(_shopArrival.Value);
                _ctx.Log($"MISSIONRUN: shop: in Fair Trade at ({me.Transform.Position.X:0.0},{me.Transform.Position.Z:0.0}); the way out is at ({_shopArrival.Value.X:0.0},{_shopArrival.Value.Z:0.0}){(zonedIn ? "" : " (kept from an earlier visit)")}.");
            }
            _shopPrevPf = pf;
            switch (_shopStep)
            {
                case ShopStep.Travel:
                    // Inside a mission building there's no route anywhere: walk out first, then set off (17:00 and
                    // 16:12, 2026-09-24: a shop trip started inside a finished mission stood there 'no zone route').
                    if (_mission.InMission)
                    {
                        _shopAfterOut = true;
                        _mission.Command("backoutside", OnOutsideReply);
                        Enter(Phase.Leaving, "out of the building before the shop");
                        return false;
                    }
                    if (pf != FairTradePf)
                    {
                        // Travel Uses a door and, when it doesn't take, replans to another city's door (07:58,
                        // 2026-09-24: Borealis -> Newland Desert -> Mort, never in). The hike stands on doors, so
                        // it goes first; travel only for the zones between.
                        if (!_overland.Active && StartHike(me, FairTradePf, ShopSpot, "Fair Trade")) return false;
                        return Travel(me, FairTradePf, ShopSpot, "Fair Trade");
                    }
                    if (_overland.Active) _overland.Stop("inside Fair Trade");
                    // 30 s from walking IN, not from the start of the trip (16:07, 2026-09-24: the trip there took 70 s,
                    // so he sold from the door, out of reach of every terminal).
                    if (_ftInAt < 0) _ftInAt = _clock;
                    if (Movement.Flat(me.Transform.Position, ShopSpot) > 1.5f && _clock - _ftInAt < 30) { _follow.SetManualTarget(ShopSpot); return true; }
                    _ftInAt = -1;
                    _follow.ClearMovement();
                    _sellRounds = 0; _sellSentAt = -99; _sellStage = 0; _sellMoves = 0; _sellBagsOpened = false; _lastBatch = null; _refusedSlots.Clear(); _badVendors.Clear(); _lastVendor = null; _wholeRefusals = 0;
                    ShopNext(ShopStep.Sell, $"{Sellable().Count} item(s) to sell.");
                    return false;

                case ShopStep.Sell:
                {
                    // SELL (capture 20260924-074329, owner, 07:44-07:45): LookAt + Use the shop terminal, Trade AddItem
                    // with HIMSELF as target and Inventory:<slot> as the container (one per item; two in one trade),
                    // then Trade Accept with no target. The server pays (Cash stat) and closes the window, so every
                    // batch opens the terminal again.
                    if (_sellSentAt > 0 && _clock - _sellSentAt < 2.5) return false;
                    if (!_sellBagsOpened)
                    {
                        // Open every bag so its contents are known (the stash's proven open: Use with Temp4 0).
                        foreach (var b in Inventory.Items.Where(i => i != null && i.Slot.Type == IdentityType.Inventory && i.UniqueIdentity.Type == IdentityType.Container))
                            GameCommands.OpenContainer(me, b.Slot);
                        _sellBagsOpened = true; _sellSentAt = _clock;
                        return false;
                    }
                    // What the last batch left behind was refused (08:04: three items offered eight times): skip them.
                    if (_lastBatch != null)
                    {
                        var left = _lastBatch.Where(slot => Inventory.Items.Any(i => i != null && i.Slot == slot)).ToList();
                        // A whole batch refused is the TERMINAL, not the items (13:24, 2026-09-24: 'Superior ICC
                        // Accessories' took none of 14; at 08:04 'Basic ICC Armor' bought the same kinds). Leave that
                        // terminal and try the next nearest; only items left over from a partly sold batch are refused.
                        // ...but the same items refused by two terminals are the ITEMS (15:45, 2026-09-24: an Omni-Med Cloak
                        // and a Trimmer offered to eight terminals in a row).
                        if (left.Count == _lastBatch.Count && _lastVendor.HasValue && ++_wholeRefusals < 2 && _badVendors.Add(_lastVendor.Value))
                            _ctx.Log($"MISSIONRUN: shop: that terminal took none of {left.Count}; trying another.");
                        else
                        {
                            foreach (var slot in left) _refusedSlots.Add(slot);
                            _wholeRefusals = 0;
                            if (left.Count > 0) _ctx.Log($"MISSIONRUN: shop: the shop refused {left.Count} item(s); leaving them.");
                        }
                        _lastBatch = null;
                    }
                    var sell = Sellable().Where(i => !_refusedSlots.Contains(i.Slot)).ToList();
                    if (sell.Count == 0 && _sellStage == 0)
                    {
                        // Nothing loose: bring a few out of the bags (owner: you can't sell from a backpack).
                        var inBags = SellableInBags();
                        int room = Inventory.NumFreeSlots - 1;
                        if (inBags.Count > 0 && room > 0 && _sellMoves < 20)
                        {
                            foreach (var it in inBags.Take(Math.Min(5, room)))
                            {
                                Item.MoveItemToInventory(it.Slot, 0x6F);
                                _ctx.Log($"MISSIONRUN: shop: '{it.Name}' out of a bag ({it.Slot}).");
                            }
                            _sellMoves++; _sellSentAt = _clock;
                            return false;
                        }
                    }
                    var vm = DynelManager.VendingMachines.Where(v => !_badVendors.Contains(v.Identity) && me.DistanceFrom(v) < 40f).OrderBy(v => me.DistanceFrom(v)).FirstOrDefault();
                    if (sell.Count == 0 || vm == null || _sellRounds >= 12)
                    {
                        if (vm == null && sell.Count > 0) _ctx.Log("MISSIONRUN: shop: no shop terminal in sight to sell to.");
                        var bank = BankTerminal;
                        _bankUses = 0; _bankUsedAt = -99; _bankPulls = 0; _bankPulledAt = -99; _ctx.Log("MISSIONRUN: shop: bank terminals here: " + string.Join(", ", DynelManager.AllDynels.Where(d => d?.Name != null && d.Name.IndexOf("Bank", StringComparison.OrdinalIgnoreCase) >= 0).Select(d => $"{d.Name} {d.Identity} at ({d.Transform.Position.X:0.0},{d.Transform.Position.Z:0.0})")));
                        ShopNext(ShopStep.OpenBank, $"sold what I could; {Inventory.NumFreeSlots} free slot(s). Opening the bank ({bank}).");
                        return false;
                    }
                    if (_sellStage == 0)
                    {
                        // Up to it first (16:05, 2026-09-24: 'took none' from terminal after terminal, used from where the
                        // last one left him; the sales that worked were the ones that happened to be close).
                        if (_sellWalkTo != vm.Identity) { _sellWalkTo = vm.Identity; _sellWalkAt = _clock; }
                        if (me.DistanceFrom(vm) > 3f && _clock - _sellWalkAt < 15)
                        {
                            _follow.SetManualTarget(vm.Transform.Position);
                            return true;
                        }
                        _follow.ClearMovement(); _sellWalkTo = null;
                        Client.Send(new LookAtMessage { Target = vm.Identity, ReturnInfo = 0 });
                        GameCommands.UseObject(me, vm.Identity);
                        _sellStage = 1; _shopStepAt = _clock;
                        return false;
                    }
                    if (t < 1) return false;   // the window opens (ShopUpdate + Trade open)
                    var batch = sell.Take(5).ToList();
                    foreach (var it in batch)
                        Client.Send(new TradeMessage { Version = 2, Action = TradeAction.AddItem, Param1 = (int)me.Identity.Type, Param2 = me.Identity.Instance, Param3 = (int)it.Slot.Type, Param4 = it.Slot.Instance });
                    Client.Send(new TradeMessage { Version = 2, Action = TradeAction.Accept });
                    _ctx.Log($"MISSIONRUN: shop: selling {string.Join(", ", batch.Select(b => b.Name))} to '{vm.Name}'.");
                    _lastBatch = batch.Select(b => b.Slot).ToList(); _lastVendor = vm.Identity;
                    _sellStage = 0; _sellRounds++; _sellSentAt = _clock;
                    return false;
                }

                case ShopStep.OpenBank:
                    if (!Inventory.Bank.IsOpen)
                    {
                        // Walk up to it, face it, use it (13:26 and 15:45, 2026-09-24: used from up to 40 m off, where the
                        // last sale left him, and the server never answered; in the owner's capture he stood at it).
                        var bankDyn = BankDynel;
                        var bankId = BankTerminal;
                        if (bankDyn == null) { _tell("No bank terminal here; skipping the banking."); return ShopAfterNanos(me); }
                        if (bankDyn != null && me.DistanceFrom(bankDyn) > 3f && t < 20) { _follow.SetManualTarget(bankDyn.Transform.Position); return true; }
                        _follow.ClearMovement();
                        // Not while casting (16:49, 2026-09-24: auto-buff and pet casts landed between every try, and the
                        // server echoed each use but never opened the bank). Auto-buff paused for the bank step; each
                        // use waits for 2 s with nothing being cast.
                        if (_bankBuffWas == null) { _bankBuffWas = _ctx.Config.AutoBuff; _ctx.Config.AutoBuff = false; }
                        if (t < 40 && (me.IsCasting || Buffing)) { _bankQuietAt = _clock; return false; }
                        if (_clock - _bankQuietAt < 2) return false;
                        if (_bankUses < 3 && _clock - _bankUsedAt > 3)
                        {
                            // As his client did (capture 20260924-192208 s5 seq 15-16): LookAt the terminal, then Use it
                            // (Count 1, Temp4 1) by its live id; the server answers BankMessage, then the echo.
                            Client.Send(new LookAtMessage { Target = bankId, ReturnInfo = 0 });
                            GameCommands.UseObject(me, bankId);
                            _bankUses++; _bankUsedAt = _clock;
                            _ctx.Log($"MISSIONRUN: shop: using the bank ({bankId}) from {(bankDyn != null ? me.DistanceFrom(bankDyn) : -1):0.0} m (try {_bankUses}).");
                            return false;
                        }
                        if (_bankUses < 3 || _clock - _bankUsedAt < 4) return false;
                        _tell("The bank terminal didn't answer; skipping the banking.");
                        _ctx.Log("MISSIONRUN: shop: the bank didn't answer three uses; skipping the banking.");
                        return ShopAfterNanos(me);
                    }
                    // Keepers sit in the bags (the stash puts every reward there): bring them out to the inventory, a few
                    // at a time, so they can go to the bank (the SDK names a bag item Backpack:(bag << 16 | slot)).
                    if (InvNanos().Count == 0 && _bankPulls < 10)
                    {
                        var inBags = Inventory.Containers.Where(c => c?.Items != null && !(_personalBags?.Contains(c.Identity) ?? false))
                                                         .SelectMany(c => c.Items).Where(i => i != null && Bankable(i)).ToList();
                        int room = Inventory.NumFreeSlots - 4;
                        if (inBags.Count > 0 && room > 0)
                        {
                            if (_clock - _bankPulledAt < 2) return false;
                            foreach (var it in inBags.Take(Math.Min(room, 8))) { Item.MoveItemToInventory(it.Slot, 0x6F); _ctx.Log($"MISSIONRUN: shop: '{it.Name}' out of a bag for the bank."); }
                            _bankPulls++; _bankPulledAt = _clock;
                            return false;
                        }
                    }
                    if (_clock - _bankPulledAt < 2) return false;
                    if (InvNanos().Count == 0) { _ctx.Log("MISSIONRUN: shop: nothing to bank."); return ShopAfterNanos(me); }
                    return ShopPickBag(me);

                case ShopStep.TakeBag:
                {
                    // Out of the bank (MoveItem Bank:n -> 111, capture seq 27): wait for it in the inventory, then open it.
                    var bag = InvItem(_shopBag);
                    if (bag == null) { if (t < 5) return false; _tell("A bag didn't come out of the bank."); return ShopAfterNanos(me); }
                    GameCommands.OpenContainer(me, bag.Slot);
                    _nanoSlot = null;
                    ShopNext(ShopStep.FillBag, $"filling '{bag.Name}' with nano crystals.");
                    return false;
                }

                case ShopStep.FillBag:
                {
                    if (t < 0.8) return false;
                    var bag = InvItem(_shopBag);
                    var cont = Inventory.Containers.FirstOrDefault(c => c.Identity == _shopBag);
                    if (bag == null || cont == null) { if (t < 5) return false; _tell("Couldn't open the bag for the nanos."); return ShopAfterNanos(me); }
                    var nanos = InvNanos();
                    // The last move still in the inventory 2 s later: the bag refused it, it's full (as in the stash).
                    if (_nanoSlot.HasValue)
                    {
                        if (t < 2) return false;
                        bool stuck = nanos.Any(n => n.Slot == _nanoSlot.Value);
                        _nanoSlot = null;
                        if (stuck) { ShopStore(me, bag, "full"); return false; }
                    }
                    if (nanos.Count == 0) { ShopStore(me, bag, "all nanos in"); return false; }
                    Item.ContainerAddItem(nanos[0].Slot, cont.Identity);
                    _nanoSlot = nanos[0].Slot; _shopStepAt = _clock;
                    _ctx.Log($"MISSIONRUN: shop: '{nanos[0].Name}' into '{bag.Name}'.");
                    return false;
                }

                case ShopStep.StoreBag:
                    if (t < 1.5) return false;
                    if (InvNanos().Count > 0) return ShopPickBag(me);   // that bag was full: the next one
                    return ShopAfterNanos(me);

                case ShopStep.Buy:
                {
                    if (Resupply.Active || t < 1) return false;
                    var bought = Inventory.Items.Where(i => i != null && i.Slot.Type == IdentityType.Inventory && i.UniqueIdentity.Type == IdentityType.Container)
                                                .Select(i => i.UniqueIdentity).Where(id => !_shopKnownBags.Contains(id)).ToList();
                    if (bought.Count == 0 && t < 5) return false;   // the bag's full update lands just after the trade
                    if (bought.Count == 0) { _tell("Couldn't buy a bag (see RESUPPLY in the log)."); ShopNext(ShopStep.Exit, "leaving (no new bag seen)."); return false; }
                    if (_shopBoughtForNanos && !_shopBoughtForRoom && InvNanos().Count > 0)
                    {
                        _shopBag = bought[0];
                        var bag = InvItem(_shopBag);
                        GameCommands.OpenContainer(me, bag.Slot);
                        _nanoSlot = null;
                        ShopNext(ShopStep.FillBag, "filling the new bag with nano crystals.");
                        return false;
                    }
                    return ShopAfterNanos(me);
                }

                case ShopStep.Stims:
                    if (Resupply.Active || t < 1) return false;
                    // Make sure he can use them before going back to missions (owner, 2026-09-24): the count is of
                    // stims his First Aid reaches. Still low: say so and stop rather than fight without them.
                    if (Resupply.NeedsResupply())
                    {
                        _tell("I still have too few stims I can use after shopping; stopping the mission run. Resupply me, then 'mission run'.");
                        Stop("no usable stims");
                        return false;
                    }
                    _ctx.Log("MISSIONRUN: shop: stocked with stims I can use; back to missions after this.");
                    return ShopAfterNanos(me);

                case ShopStep.Exit:
                {
                    if (pf != FairTradePf) { _follow.ClearMovement(); _tell($"Housekeeping done: {Inventory.NumFreeSlots} free slot(s)."); Enter(Phase.ToTerminal, "back from Fair Trade"); return false; }
                    // No landing spot (logged in inside, 13:26 2026-09-24) or it didn't take him out: the hike's own way
                    // out - the nearest door, stood on - toward the terminal. It hands back to this step once out.
                    if (t > 30 || !_shopArrival.HasValue)
                    {
                        _follow.ClearMovement();
                        _hikeLastHike = -99;
                        if (StartHike(me, _termPf, _termPos, "the terminal")) return false;
                        _tell("Couldn't walk out of Fair Trade."); Stop("stuck in Fair Trade"); return false;
                    }
                    // Back the way he came in: to where he landed, then 3 m on past it, away from the shop spot.
                    Vector3 a = _shopArrival.Value;
                    var d = new Vector3(a.X - ShopSpot.X, 0, a.Z - ShopSpot.Z);
                    float len = d.Magnitude;
                    var target = len > 0.5f ? new Vector3(a.X + d.X / len * 3f, a.Y, a.Z + d.Z / len * 3f) : a;
                    _follow.SetManualTarget(Movement.Flat(me.Transform.Position, a) > 1.5f ? a : target);
                    return true;
                }
            }
            return false;
        }

        // A bag from the bank that isn't known full, or buy one.
        private bool ShopPickBag(LocalPlayer me)
        {
            var seen = new HashSet<int>();   // the SDK appends each BankMessage without clearing: one per bank slot
            var bankBags = Inventory.Bank.Items.Where(i => i != null && seen.Add(i.Slot.Instance) && i.UniqueIdentity.Type == IdentityType.Container
                                                          && !_shopFullBags.Contains(i.UniqueIdentity) && InvItem(i.UniqueIdentity) == null).ToList();
            if (bankBags.Count > 0)
            {
                var b = bankBags[0];
                _shopBag = b.UniqueIdentity;
                Item.MoveItemToInventory(new Identity(IdentityType.BankByRef, b.Slot.Instance), 0x6F);
                ShopNext(ShopStep.TakeBag, $"taking '{b.Name}' out of the bank.");
                return false;
            }
            return ShopBuy(me, forNanos: true);
        }

        private void ShopStore(LocalPlayer me, Item bag, string why)
        {
            if (why == "full") _shopFullBags.Add(bag.UniqueIdentity);
            // Back into the bank (capture seq 35-36: Use on the bag, then ClientContainerAddItem to Bank 0xDEAD:<me>).
            GameCommands.UseObject(me, bag.UniqueIdentity);
            bag.MoveToBank();
            ShopNext(ShopStep.StoreBag, $"'{bag.Name}' into the bank ({why}).");
        }

        private bool _shopStimsTried;
        private bool ShopAfterNanos(LocalPlayer me)
        {
            if (Inventory.NumFreeSlots < 4 && !_shopBoughtForRoom) return ShopBuy(me, forNanos: false);   // one more bag to carry
            // Stims and rechargers at the QL his skills can use: ResupplyController picks the fitting QL and the
            // terminals here (Algorithman's), as the owner would buy them.
            if (!_shopStimsTried && Resupply.NeedsResupply())
            {
                _shopStimsTried = true;
                Resupply.Start(me, s => _ctx.Log("MISSIONRUN: shop: " + s));
                ShopNext(ShopStep.Stims, "buying stims/rechargers I can use.");
                return false;
            }
            ShopNext(ShopStep.Exit, "leaving the way I came in.");
            return false;
        }

        private bool ShopBuy(LocalPlayer me, bool forNanos)
        {
            me.TryGetStat(Stat.Cash, out int cash);
            if (cash < _ctx.Config.MissionCashReserve)
            {
                _tell($"Not buying a bag: {cash:N0} credits, and I keep {_ctx.Config.MissionCashReserve:N0} for missions.");
                ShopNext(ShopStep.Exit, "leaving (credits).");
                return false;
            }
            _shopKnownBags.Clear();
            foreach (var i in Inventory.Items.Where(i => i != null && i.UniqueIdentity.Type == IdentityType.Container)) _shopKnownBags.Add(i.UniqueIdentity);
            if (forNanos) _shopBoughtForNanos = true; else _shopBoughtForRoom = true;
            int keep = _ctx.Config.ResupplyCashReserve;
            _ctx.Config.ResupplyCashReserve = Math.Max(keep, _ctx.Config.MissionCashReserve);
            Resupply.StartContainers(me, 1, s => _ctx.Log("MISSIONRUN: shop: " + s));
            _ctx.Config.ResupplyCashReserve = keep;
            ShopNext(ShopStep.Buy, forNanos ? "buying a bag for the nano crystals." : "buying a bag to carry.");
            return false;
        }

        private void StartBackoff(LocalPlayer me, string next)
        {
            Vector3 pos = me.Transform.Position;
            _backoffNext = next;
            _backoffTo = null;
            _backoffs++;
            if (_backoffs >= 2 && _good.Count >= 2)
            {
                // Second time and after: walk our own clean trail back ~15 m (further each time) to a spot the
                // server accepted, then try again from there.
                float want = Math.Min(T("trailstep") * (_backoffs - 1), T("trailmax")), got = 0;
                var back = new List<Vector3>();
                Vector3 last = pos;
                for (int i = _good.Count - 1; i >= 0 && got < want; i--)
                {
                    if (Vector3.Distance(_good[i], pos) < 2f && back.Count == 0) continue;
                    // Never across a jump: a teleporter or zone in the trail is not walkable back (09:16:43,
                    // 2026-09-24: 'walking my clean trail back 2030 m' through Lush Fields' teleporter).
                    if (Movement.Flat(last, _good[i]) > 20f) break;
                    got += Movement.Flat(last, _good[i]); last = _good[i];
                    back.Add(_good[i]);
                }
                if (back.Count > 0)
                {
                    _follow.LoadReplay(back, false);
                    _backoffTo = back[back.Count - 1];
                    _ctx.Log($"MISSIONRUN: stopped short again; walking my clean trail back {got:0} m to ({_backoffTo.Value.X:0},{_backoffTo.Value.Z:0}), then trying to {next} again.");
                    Enter(Phase.Backoff, "back to a good position");
                    return;
                }
            }
            Vector3 fwd = me.MovementComponent.Heading.Forward;
            var flat = new Vector3(fwd.X, 0, fwd.Z);
            float len = flat.Magnitude;
            _backoffTo = len > 0.1f ? pos - flat * (T("backoff") / len) : (Vector3?)null;
            _ctx.Log($"MISSIONRUN: stopped short; backing off {(len > 0.1f ? "6 m" : "0 m (no facing)")} before trying to {next} again.");
            Enter(Phase.Backoff, "backing off");
        }
        private bool _fullWarned, _rollWarned, _leaveWarned, _afterDeath;
        private int _straightTries;

        // On the hike's walk grid to the reachable ground nearest the goal, with the ground's height every 3 m, then
        // straight. Same zone only; a far goal only when the grid has a way. The first version only set a target
        // and returned false, which stops the walker: he stood at the whompa for 50 s (08:47-08:48).
        // A walk inside one zone never aims within 8 m of its zone lines (00:51, 2026-09-25: into Holes in the Wall
        // at (1084,1949) by the Stret West Bank line, the walk to the door's first point sat on that line, and 15 s
        // later he was back in Stret West Bank). The last point, the goal, is kept.
        private static List<Vector3> OffZoneLines(IEnumerable<Vector3> pts, int pf, Vector3? from = null)
        {
            var list = pts.ToList();
            var lines = Zoning.ExitsFrom(pf).Where(e => e.Kind == ExitKind.ZoneLine).ToList();
            if (lines.Count == 0 || list.Count < 2) return list;
            float Dist(Vector3 p, Vector3 a, Vector3 b)
            {
                float dx = b.X - a.X, dz = b.Z - a.Z, l2 = dx * dx + dz * dz;
                float t = l2 <= 0 ? 0 : Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Z - a.Z) * dz) / l2));
                float ex = a.X + t * dx - p.X, ez = a.Z + t * dz - p.Z;
                return (float)Math.Sqrt(ex * ex + ez * ez);
            }
            var kept = list.Take(list.Count - 1).Where(p => lines.All(e => Dist(p, e.A, e.B) >= 8f)).ToList();
            kept.Add(list[list.Count - 1]);
            // Standing on a line (landed 1.8 m in): first step 10 m straight away from its nearest point.
            if (from.HasValue)
            {
                Vector3 f = from.Value;
                var near = lines.OrderBy(e => Dist(f, e.A, e.B)).First();
                float d = Dist(f, near.A, near.B);
                if (d < 8f)
                {
                    float dx = near.B.X - near.A.X, dz = near.B.Z - near.A.Z, l2 = dx * dx + dz * dz;
                    float t = l2 <= 0 ? 0 : Math.Max(0, Math.Min(1, ((f.X - near.A.X) * dx + (f.Z - near.A.Z) * dz) / l2));
                    float nx = f.X - (near.A.X + t * dx), nz = f.Z - (near.A.Z + t * dz), n = (float)Math.Sqrt(nx * nx + nz * nz);
                    if (n > 0.1f) kept.Insert(0, new Vector3(f.X + nx / n * 10f, f.Y, f.Z + nz / n * 10f));
                }
            }
            return kept;
        }

        private bool TryWalkMyself(LocalPlayer me, int pf, Vector3 goal, string what, string why)
        {
            if ((int)Playfield.ModelId != pf || _straightTries >= T("walktries")) return false;
            float far = Movement.Flat(me.Transform.Position, goal);
            var grid = HikeGrid();
            if (far >= T("walkto") && grid == null) return false;
            var path = grid == null ? null : NearestPath(grid, me.Transform.Position, goal, out float left);
            if (path == null && far >= T("walkto")) return false;   // far and no grid way: no straight walk
            _straightTries++;
            _straightGoal = goal; _straightUntil = _clock + Math.Max(30, far / 5f + 20);
            if (path != null && path.Count > 1) _follow.LoadReplay(OnGround(OffZoneLines(path.Skip(1), pf, me.Transform.Position), me.Transform.Position), false);
            _ctx.Log($"MISSIONRUN: {why} to {what} {far:0} m off; walking to it myself ({(path != null ? $"grid, {path.Count} points" : "straight")}, try {_straightTries}).");
            return true;
        }
        // Zones he died in / couldn't reach / skipped, with the (UTC) time: kept in danger.json so a restart
        // doesn't send him straight back (he is restarted often while the run is being fixed).
        // Each mark in the same zone doubles how long it lasts (The Longest Road: died 12:14, 12:33, and 13:52 -
        // the hour from the second had just run out and the route to Athen Shire went through it again, past level
        // 70-120 Bileswarm and Shade-Y44). dangermins x 2^(marks-1), kept in danger.json with the count.
        private Dictionary<int, (DateTime at, int n)> _dangerStore;
        private Dictionary<int, (DateTime at, int n)> _danger
        {
            get
            {
                if (_dangerStore != null) return _dangerStore;
                _dangerStore = new Dictionary<int, (DateTime, int)>();
                try
                {
                    foreach (var kv in JsonStore.Load<JObject>(DangerPath, _ctx.Log) ?? new JObject())
                        _dangerStore[int.Parse(kv.Key)] = kv.Value is JObject e ? ((DateTime)e["at"], (int?)e["n"] ?? 1) : ((DateTime)kv.Value, 1);
                }
                catch { }
                return _dangerStore;
            }
        }
        private string DangerPath => Path.Combine(_pluginDir, "danger.json");
        private void MarkDanger(int pf)
        {
            int n = _danger.TryGetValue(pf, out var old) ? old.n + 1 : 1;
            _danger[pf] = (DateTime.UtcNow, n);
            var o = new JObject(); foreach (var kv in _danger) o[kv.Key.ToString()] = new JObject { ["at"] = kv.Value.at, ["n"] = kv.Value.n };
            JsonStore.Save(DangerPath, o.ToString(), _ctx.Log);
        }
        private double DangerMinutes(int n) => T("dangermins") * Math.Pow(2, Math.Min(6, Math.Max(0, n - 1)));
        private bool Dangerous(int pf) => (_ctx.Config.MissionAvoidZones?.Contains(pf) ?? false)
                                          || _danger.TryGetValue(pf, out var d) && (DateTime.UtcNow - d.at).TotalMinutes < DangerMinutes(d.n);
        private double _hpHighAt = -99;

        // THE OTHER SIDE'S GROUND (owner, 2026-09-24: the alt is Omni; West Athens' Vanguard Watcher killed him from
        // full HP in 3 s). GameData/FactionAreas.json: Clan cities (whole zones) and each side's whompa stations from
        // the owner's Saavik's map, placed with Zoning.json's whompa positions. His side is his Side stat (1 Clan,
        // 2 Omni); a neutral avoids none. Missions there are not taken, and no route uses those whompas or zones.
        private sealed class FactionArea { public int side, pf; public float x, z, r; public string name; }
        private List<FactionArea> _factionAreas;
        private List<FactionArea> FactionAreas
        {
            get
            {
                if (_factionAreas != null) return _factionAreas;
                _factionAreas = new List<FactionArea>();
                try { _factionAreas = JObject.Parse(File.ReadAllText(Path.Combine(_pluginDir, "GameData", "FactionAreas.json")))["areas"].ToObject<List<FactionArea>>(); }
                catch (Exception ex) { _ctx.Log($"MISSIONRUN: no faction areas ({ex.Message})."); }
                return _factionAreas;
            }
        }
        private int MySide { get { var me = DynelManager.LocalPlayer; return me != null && me.TryGetStat(Stat.Side, out int s) ? s : 0; } }
        /// <summary>The other side's area at this spot (whole zone or round a station), or null.</summary>
        private string HostileAt(int pf, float x, float z)
        {
            if (_ctx.Config.MissionAvoidZones?.Contains(pf) ?? false) return Zoning.Name(pf);
            int side = MySide;
            if (side != 1 && side != 2) return null;
            foreach (var a in FactionAreas)
                if (a.side != side && a.pf == pf && (a.r <= 0 || Math.Sqrt((a.x - x) * (a.x - x) + (a.z - z) * (a.z - z)) < a.r)) return a.name;
            return null;
        }
        private bool HostileExit(ZoneExit e) => HostileAt(e.FromPf, e.A.X, e.A.Z) != null
                                                || (e.Arrival.HasValue ? HostileAt(e.ToPf, e.Arrival.Value.X, e.Arrival.Value.Z) != null
                                                                       : HostileAt(e.ToPf, float.NaN, float.NaN) != null);   // arrival unknown: whole zones only
        private bool _diedOnWay;
        private double _fleeUntil = -99;
        private double _fleeStartedAt = -99, _noFleeUntil = -99;
        private Vector3? _fleeAt;
        private Vector3 _pinSamplePos;
        private double _pinSampleAt = -99;
        private readonly List<Identity> _fleeFrom = new List<Identity>();
        private void StartFightBack(LocalPlayer me, string why)
        {
            _fleeUntil = _clock; _fleeAt = null; _noFleeUntil = _clock + 30;
            foreach (var id in _fleeFrom) _combat.ClearAside(id);
            if (_mission.Active) _mission.Stop("fighting back");
            if (_overland.Active) _overland.Stop("fighting back");
            _follow.ClearMovement();
            _fightStart = _clock; _fightHpMin = 100; _fightReturn = _phase == Phase.Backoff ? _travelReturn : _phase;
            _ctx.Log($"MISSIONRUN: {why}; fighting back.");
            Enter(Phase.Fight, "fighting back");
        }
        private void FleeStarted(LocalPlayer me, IEnumerable<SimpleChar> from)
        {
            _fleeAt = me.Transform.Position; _fleeStartedAt = _clock;
            _fleeFrom.Clear(); _fleeFrom.AddRange(from.Select(n => n.Identity));
        }
        public bool Fleeing => Active && _clock < _fleeUntil;

        private bool StartFlee(LocalPlayer me)
        {
            Vector3 pos = me.Transform.Position;
            var back = new List<Vector3>();
            float got = 0; Vector3 last = pos;
            for (int i = _good.Count - 1; i >= 0 && got < T("fleedist"); i--)
            {
                if (Movement.Flat(last, _good[i]) > 20f) break;       // never across a teleport or zone jump
                if (Movement.Flat(_good[i], pos) < 2f && back.Count == 0) continue;
                got += Movement.Flat(last, _good[i]); last = _good[i];
                back.Add(_good[i]);
            }
            if (back.Count == 0 || got < 15f) return false;  // nowhere to run: keep fighting
            var from = DynelManager.Npcs.Where(n => n != null && n.FightingIdentity.HasValue && n.FightingIdentity.Value == me.Identity).ToList();
            foreach (var n in from) _combat.SetAside(me, n.Identity, T("fleesecs"));
            FleeStarted(me, from);
            if (me.IsAttacking) me.StopAttack();
            _fleeUntil = _clock + T("fleesecs");
            _travelReturn = _fightReturn == Phase.Hike ? _hikeReturn : _fightReturn;
            _backoffNext = "flee"; _backoffTo = back[back.Count - 1];
            _follow.ClearMovement();
            _follow.LoadReplay(back, false);
            _ctx.Log($"MISSIONRUN: fleeing at {_ctx.Status.SelfHpPct}% HP from {from.Count} mob(s) ({string.Join(", ", from.Select(n => n.Name).Distinct())}): running {got:0} m back the way I came.");
            Enter(Phase.Backoff, "fleeing");
            return true;
        }

        // ---- TUNE (owner, 2026-09-24): the distances and waits of OUR walking (hike, walk-to-it, terminal and
        // door stands, pull-back and backoff), changeable live with 'mission run tune <name> <value>' and kept in
        // tune.json, so what works where can be learned from the log (each change is logged, and the crossing
        // lines carry the values they used). Algorithman's travel planner is not touched by any of these.
        private static readonly Dictionary<string, (float def, string what)> TuneDefaults = new Dictionary<string, (float, string)>
        {
            ["reach"]     = (1.5f,  "metres from a goal the walk grid counts as arrived (hike, walk-to-it)"),
            ["snap"]      = (8f,    "metres the walk grid looks for open ground under my feet"),
            ["ring"]      = (24f,   "metres out round a blocked goal the walk grid looks for reachable ground"),
            ["ringstep"]  = (4f,    "metres between those rings"),
            ["padtop"]    = (0.285f,"metres above a whompa's recorded height I stand (the pad's top)"),
            ["aimpast"]   = (1.2f,  "metres past a whompa's/door's centre I aim, so the walker stops on it"),
            ["standwait"] = (4f,    "seconds I stand on a whompa before stepping on again"),
            ["standtries"]= (4f,    "times I stand on a whompa/line before leaving it"),
            ["usetries"]  = (3f,    "times I use a terminal/teleporter before leaving it"),
            ["termnear"]  = (12f,   "metres from the terminal I finish on foot, straight at it"),
            ["termstop"]  = (3.5f,  "metres from the terminal I stop"),
            ["termroll"]  = (6f,    "metres from the terminal I may roll"),
            ["pullgap"]   = (5f,    "metres a server snap-back must move me to count"),
            ["pulls"]     = (2f,    "snap-backs within pullsecs that mean 'pulled back'"),
            ["pullsecs"]  = (8f,    "seconds the snap-backs are counted over"),
            ["held"]      = (15f,   "seconds I stand still when held (rooted/snared)"),
            ["backoff"]   = (6f,    "metres of the first backoff"),
            ["trailstep"] = (15f,   "metres more of my trail walked back each further backoff"),
            ["trailmax"]  = (60f,   "most metres of trail walked back"),
            ["walkto"]    = (120f,  "metres within which I walk to a goal myself when travel finds no way"),
            ["walktries"] = (2f,    "walk-to-it tries per arrival"),
            ["chain"]     = (10f,   "zone crossings I hike in a row before travel takes over"),
            ["fleehp"]    = (40f,   "HP % under which, still being hit outside a mission, I break off and run back the way I came (0 = never)"),
            ["fleedist"]  = (80f,   "metres of my trail I run back when fleeing"),
            ["fleesecs"]  = (30f,   "seconds the mobs I flee from are left alone"),
            ["dangermins"]= (60f,   "minutes I take no missions in a zone I died in out in the open"),
        };
        private Dictionary<string, float> _tuneStore;
        private Dictionary<string, float> _tune
        {
            get
            {
                if (_tuneStore != null) return _tuneStore;
                _tuneStore = new Dictionary<string, float>();
                try { foreach (var kv in JsonStore.Load<JObject>(TunePath, _ctx.Log) ?? new JObject()) if (TuneDefaults.ContainsKey(kv.Key)) _tuneStore[kv.Key] = (float)kv.Value; } catch { }
                return _tuneStore;
            }
        }
        private string TunePath => Path.Combine(_pluginDir, "tune.json");
        private float T(string k) => _tune.TryGetValue(k, out float v) ? v : TuneDefaults[k].def;
        // Owner settings given by tell survive a restart (2026-09-24: 'difficulty 5' was lost to one): written into
        // the plugin's config.json, key by key, leaving the rest of the file as it is.
        private void SaveConfigValue(string key, JToken value)
        {
            string path = Path.Combine(_pluginDir, "config.json");
            var o = JsonStore.Load<JObject>(path, _ctx.Log) ?? new JObject();
            o[key] = value;
            JsonStore.Save(path, o.ToString(), _ctx.Log);
        }
        private void SaveTune() { var o = new JObject(); foreach (var kv in _tune) o[kv.Key] = kv.Value; JsonStore.Save(TunePath, o.ToString(), _ctx.Log); }
        private string TuneText() => string.Join(", ", TuneDefaults.Select(kv => $"{kv.Key}={T(kv.Key)}{(_tune.ContainsKey(kv.Key) ? "*" : "")}"));
        private string StandTune() => $"padtop={T("padtop")} aimpast={T("aimpast")} standwait={T("standwait")}";
        private double _straightUntil;
        private Vector3 _straightGoal;

        // A walk-grid path to the reachable ground nearest 'at' (rings out to 24 m round it).
        private List<Vector3> NearestPath(IWalkGrid grid, Vector3 pos, Vector3 at, out float bestLeft)
        {
            List<Vector3> best = null; bestLeft = float.MaxValue;
            float step = Math.Max(0.5f, T("ringstep")), max = Math.Max(0f, T("ring"));
            for (float r = 0; r <= max + 0.01f; r += step)
                for (int k = 0; k < (r == 0 ? 1 : 8); k++)
                {
                    double t = k * Math.PI / 4;
                    var goal = new Vector3(at.X + (float)Math.Cos(t) * r, at.Y, at.Z + (float)Math.Sin(t) * r);
                    var path = grid.FindPath(pos, goal, null, T("snap"), T("reach"), out _);
                    if (path == null) continue;
                    float left = Movement.Flat(path[path.Count - 1], at);
                    if (left < bestLeft) { bestLeft = left; best = path; }
                }
            return best;
        }
        private int _blitzTries;
        private double _doorStepTime;

        // travelto, once per leg, through the command it already has; retried twice on failure.
        private bool Travel(LocalPlayer me, int pf, Vector3 goal, string what)
        {
            // Inside Fair Trade the zone data has no ways out: leave by the shop's own exit (its landing spot) first.
            if ((int)Playfield.ModelId == FairTradePf && _phase != Phase.Shop)
            {
                _shopStep = ShopStep.Exit; _shopStepAt = _clock;
                Enter(Phase.Shop, "out of Fair Trade first");
                return false;
            }
            _hikeChain = 0;   // a chain of hikes never passes through here; any other trip starts its count afresh
            if (_overland.Active)
            {
                if (_phaseTime > TravelTimeout) { _overland.Stop("mission run: too long"); }
                // Waiting on Scotty: it has never warped this bot. Walk the planner's own route instead.
                else if (_overland.Status().Contains("scty") && _phaseTime > (NoScotty ? 1 : 40) && StartHike(me, pf, goal, what)) return false;
                // Same zone on RubiKa2019: no Scotty and no crossing for the hike, so walk it myself (Athen Shire,
                // 15:04-15:09, 2026-09-24: travel planned four Scotty warps in a row, 90 s each, and none came).
                else if (_overland.Status().Contains("scty") && NoScotty && (int)Playfield.ModelId == pf)
                {
                    _overland.Stop("no Scotty on RubiKa2019");
                    if (TryWalkMyself(me, pf, goal, what, "no Scotty here")) return true;
                }   // Scotty's warp comes ~20 s after the tell (Algorithman, 2026-09-24): 40 s, then on foot
                return false;
            }
            if (_clock < _straightUntil)
            {
                if (_follow.ReplayCount > 0) return true;
                if (Movement.Flat(me.Transform.Position, _straightGoal) > 2f) { _follow.SetManualTarget(_straightGoal); return true; }
                _follow.ClearManual(); _straightUntil = 0;
            }
            if (_clock < _travelWaitUntil) return false;
            if (_travelStarted)
            {
                _travelStarted = false;
                bool there = (int)Playfield.ModelId == pf && Movement.Flat(me.Transform.Position, goal) <= 12f;
                if (!there && StartHike(me, pf, goal, what)) return false;
                if (there) { _backoffs = 0; _travelBacks = 0; _straightTries = 0; }
                // Same zone and close, but travel found no way (Andromeda's terminal at (3233,921), 'walled off: no
                // open ground within 3 m', 08:37-08:40, 2026-09-24 - he had rolled there five minutes before): walk
                // straight at it, twice, before the backoffs.
                // Far goals too, on the grid only (Holes in the Wall, 12:53, 2026-09-24: the door at (441,1512)
                // 'walled off' 1.5 km away, and the mission was dropped without a try on foot).
                if (!there && TryWalkMyself(me, pf, goal, what, "travel found no way")) return true;
                // The walk grid still loading (a few seconds after a login or a zone): wait for it rather than back
                // off - the backoff at 15:09:48 (2026-09-24) stepped him back over the zone line he had just crossed.
                if (!there && (int)Playfield.ModelId == pf && _straightTries < T("walktries") && HikeGrid() == null) { _travelStarted = true; return false; }
                // The owner's rule: go back to the last known good spot and try another way. Travel said 'walled
                // off' from a spot the snap-backs left him on (664,499, 23:02:58), where two minutes before, 40 m
                // back, it had planned the same trip fine.
                if (!there && (int)Playfield.ModelId == pf && !_mission.InMission && _travelBacks < 3)
                {
                    _travelBacks++;
                    _travelReturn = _phase;
                    StartBackoff(me, "travel");
                    if (_phase == Phase.Backoff) return false;
                }
                if (!there && ++_travelTries >= 3)
                {
                    _travelTries = 0;
                    if (_phase == Phase.ToDoor)
                    {
                        // Unreachable from here: leave the zone alone for a while too (Galway County, 09:41-10:06,
                        // 2026-09-24: every point of the Galway Shire border pulled him back, for travel and hike).
                        if (_current != null) MarkDanger(_current.Playfield.Instance);
                        Skip($"can't get to its door ({_overland.Status()})"); return false;
                    }
                    _tell($"I can't get to {what} ({_overland.Status()}); trying again in a minute.");
                    _travelWaitUntil = _clock + 60;
                    // ...and walking it myself again then: the tries were spent in the backoffs, and at 01:07-01:12
                    // (2026-09-25) he stood at (720,637) in Borealis 5 minutes while travel said 'walled off' each minute.
                    _straightTries = 0;
                    return false;
                }
                if (there) return false;                  // the phase check picks it up next frame
            }
            // Another zone: the hike takes the first crossing. Travel stands on a whompa at ground height and Uses
            // doors, which never takes (08:12, 2026-09-24: Stret West Bank's Borealis whompa, 3 tries, then a 5-leg
            // detour); the hike stops on a pad's top and stands on doors, and hands back to travel after the zone.
            if ((int)Playfield.ModelId != pf && StartHike(me, pf, goal, what)) return false;
            var args = new[] { goal.X.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), goal.Z.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), pf.ToString() };
            _ctx.Log($"MISSIONRUN: travelto {string.Join(" ", args)} ({what}).");
            // Pulled back on travel's own walk in this zone already (Deep Artery Valley, 13:45-13:48, 2026-09-24:
            // Scotty never came, travel's walk snapped back in 2 s, backed off, the same again every 2 minutes):
            // walk it myself on the grid with the ground's heights instead of handing travel the same walk.
            if (_backoffs >= 1 && TryWalkMyself(me, pf, goal, what, "pulled back on travel's walk")) return true;
            _overland.Command(args, s => _ctx.Log("MISSIONRUN: travel: " + s));
            _travelStarted = true;
            return false;
        }

        // ---- Stash -----------------------------------------------------------------------------------------
        // Capture 20260923-201746: a bag is opened by Using it (Flag 0); the server answers with the bag's
        // contents (InventoryUpdate: 21 slots, the entries, open), which the SDK keeps as a Container; each item
        // is then ClientContainerAddItem'd into the bag and the server confirms it with ContainerAddItem.

        private void StartStash()
        {
            _bagsToTry.Clear(); _bag = null; _movedSlot = null;
            foreach (var b in Inventory.Items.Where(i => i != null && i.Slot.Type == IdentityType.Inventory && i.UniqueIdentity.Type == IdentityType.Container))
                _bagsToTry.Enqueue(b);
            Enter(Phase.Stash, "stashing the reward");
        }

        private List<Item> Rewards() => Inventory.Items.Where(i => i != null && i.Slot.Type == IdentityType.Inventory
            && _rewardIds.Any(r => i.Id == r.Item1 || i.HighId == r.Item2 || i.Id == r.Item2)).ToList();

        private void StashTick(LocalPlayer me)
        {
            var rewards = Rewards();
            if (rewards.Count == 0 || _phaseTime > 60)
            {
                if (rewards.Count > 0) _tell($"Couldn't stash {rewards.Count} reward item(s); they stay in my inventory.");
                _rewardIds.Clear();
                Enter(Phase.ToTerminal, "back to the terminal");
                return;
            }
            if (_bag == null)
            {
                if (_bagsToTry.Count == 0)
                {
                    _rewardIds.Clear();
                    if (StartShop("no bag has room for the reward")) return;
                    _tell("No backpack with room for the reward; it stays in my inventory.");
                    Enter(Phase.ToTerminal, "no bag room"); return;
                }
                _bag = _bagsToTry.Dequeue();
                _bagOpenedAt = _clock;
                GameCommands.OpenContainer(me, _bag.Slot);
                _ctx.Log($"MISSIONRUN: opening bag '{_bag.Name}' at {_bag.Slot}.");
                return;
            }
            var cont = Inventory.Containers.FirstOrDefault(c => c.Identity == _bag.UniqueIdentity);
            if (cont == null)
            {
                if (_clock - _bagOpenedAt > 4) { _ctx.Log($"MISSIONRUN: bag '{_bag.Name}' did not open."); _bag = null; }
                return;
            }
            if (cont.IsFull) { _ctx.Log($"MISSIONRUN: bag '{_bag.Name}' is full."); _bag = null; return; }
            var item = rewards[0];
            // The SDK's view of a bag's contents is from when it was opened, so a bag our own last move filled
            // still reads as having room (2026-09-23 22:44: 97 refused moves into a full Large Backpack). If the
            // item we moved is still in the inventory 2 s later, the server refused it: that bag is full.
            if (_movedSlot.HasValue)
            {
                if (_clock - _lastMove < 2) return;
                bool stillThere = rewards.Any(r => r.Slot == _movedSlot.Value);
                _movedSlot = null;
                if (stillThere) { _ctx.Log($"MISSIONRUN: bag '{_bag.Name}' refused the item; it must be full."); _bag = null; return; }
                return;   // it went in; pick the next reward on the next tick
            }
            item.MoveToContainer(cont);
            _lastMove = _clock; _movedSlot = item.Slot;
            _ctx.Log($"MISSIONRUN: moving '{item.Name}' into '{_bag.Name}'.");
        }

        private double _bagOpenedAt;
        private Identity? _movedSlot;

        // ---- Doors -----------------------------------------------------------------------------------------

        private Dictionary<int, List<Vector3>> _doors;

        /// <summary>The mission door (Zoning.json missionEntrances) nearest the spot the terminal gave.</summary>
        private Vector3? FindDoor(int pf, Vector3 near)
        {
            if (_doors == null)
            {
                _doors = new Dictionary<int, List<Vector3>>();
                try
                {
                    var root = JObject.Parse(File.ReadAllText(Path.Combine(_pluginDir, "GameData", "Zoning.json")));
                    foreach (var p in (JObject)root["playfields"])
                    {
                        var list = new List<Vector3>();
                        foreach (var e in (JArray)p.Value["missionEntrances"] ?? new JArray())
                        { var q = e["pos"]; list.Add(new Vector3((float)q[0], (float)q[1], (float)q[2])); }
                        if (list.Count > 0) _doors[int.Parse(p.Key)] = list;
                    }
                }
                catch (Exception ex) { _ctx.Log("MISSIONRUN: couldn't read mission doors from Zoning.json: " + ex.Message); }
            }
            if (!_doors.TryGetValue(pf, out var doors)) return null;
            var best = doors.OrderBy(d => Movement.Flat(d, near)).FirstOrDefault();
            if (Movement.Flat(best, near) > 25f) return null;
            _ctx.Log($"MISSIONRUN: door at ({best.X:0},{best.Y:0},{best.Z:0}), {Movement.Flat(best, near):0.0} m from the terminal's spot.");
            return best;
        }

        // ---- The mission in hand, kept across restarts ------------------------------------------------------
        // The quest log does not carry the door's position in a form we read, so the run writes down what the
        // terminal said about the mission it took (missionrun.json beside the plugin) and clears it when done.

        private string SavePath => Path.Combine(_pluginDir, "missionrun.json");
        private readonly Dictionary<Identity, Quest> _quests = new Dictionary<Identity, Quest>();

        // Deleting a mission, as the owner's client does it (capture 20260910-200346, client seq 54):
        // QuestMessage Action=Delete (1) with the quest identity from the quest log. Only quests that came from a
        // mission terminal (the quest log names the terminal: UnknownId1 is a MissionTerminal identity) are
        // touched - never an ordinary quest.
        private int DeleteHeldMissions()
        {
            int n = 0;
            foreach (var id in HeldMissionIds())
            {
                Client.Send(new QuestMessage { Action = QuestAction.Delete, Mission = id });
                _ctx.Log($"MISSIONRUN: deleted mission {id}.");
                _quests.Remove(id);
                n++;
            }
            _deleted.UnionWith(HeldMissionIds());
            ClearSaved();
            return n;
        }

        private readonly HashSet<Identity> _deleted = new HashSet<Identity>();

        /// <summary>The missions from a mission terminal in the quest log. The SDK's own decode of the quest log
        /// came back empty live (21:40, "deleted 0"), so this reads the raw message the way MissionRecords does:
        /// each quest starts with its Mission identity (0xDAC3); a quest from a terminal carries the terminal's
        /// identity (0xDAC1) before the next quest starts (capture 20260923-201746).</summary>
        private List<Identity> HeldMissionIds()
        {
            var ids = new List<Identity>();
            foreach (var q in _quests.Values) if (q.UnknownId1.Type == IdentityType.MissionTerminal) ids.Add(q.QuestId);
            var b = _lastQuestLog;
            if (b != null)
            {
                var starts = new List<(int at, int inst)>();
                for (int i = 0; i + 8 <= b.Length; i++)
                    if (b[i] == 0 && b[i + 1] == 0 && b[i + 2] == 0xDA && b[i + 3] == 0xC3)
                    {
                        int inst = (b[i + 4] << 24) | (b[i + 5] << 16) | (b[i + 6] << 8) | b[i + 7];
                        if (!starts.Any(x => x.inst == inst)) starts.Add((i, inst));
                    }
                for (int k = 0; k < starts.Count; k++)
                {
                    int end = k + 1 < starts.Count ? starts[k + 1].at : b.Length;
                    bool fromTerminal = false;
                    for (int i = starts[k].at + 8; i + 4 <= end && !fromTerminal; i++)
                        fromTerminal = b[i] == 0 && b[i + 1] == 0 && b[i + 2] == 0xDA && b[i + 3] == 0xC1;
                    // The login quest log names the terminal as a 0xC350 identity with the terminal's instance, not
                    // as 0xDAC1 (capture 20260923-232359: all three held missions, none with 0xDAC1), so missions
                    // from before a restart were invisible here and never deleted. This terminal has shown up as
                    // C0000320 and C0010320 (bot log 23:13 and the same capture), so the third byte is not compared.
                    if (_termId.Instance != 0)
                        for (int i = starts[k].at + 8; i + 8 <= end && !fromTerminal; i++)
                            fromTerminal = b[i] == 0 && b[i + 1] == 0 && b[i + 2] == 0xC3 && b[i + 3] == 0x50
                                && b[i + 4] == (byte)(_termId.Instance >> 24) && b[i + 6] == (byte)(_termId.Instance >> 8) && b[i + 7] == (byte)_termId.Instance;
                    var id = new Identity(IdentityType.Mission, starts[k].inst);
                    if (fromTerminal && !ids.Contains(id)) ids.Add(id);
                }
            }
            ids.RemoveAll(x => _deleted.Contains(x));
            return ids;
        }

        /// <summary>Give up on the mission in hand: delete it, walk out if inside, and carry on rolling.</summary>
        private void Skip(string why)
        {
            int n = DeleteHeldMissions();
            _tell($"Skipping this mission ({why}); deleted {n}.");
            _current = null; _completed = false;
            if (_mission.Active) _mission.Stop("skipping the mission");
            if (_mission.InMission) { _mission.Command("backoutside", OnOutsideReply); Enter(Phase.Leaving, "walking out to skip it"); }
            else Enter(Phase.ToTerminal, "skipped");
        }
        private string TerminalPath => Path.Combine(_pluginDir, "missionterminal.json");

        private void SaveTerminal()
        {
            JsonStore.Save(TerminalPath, new JObject { ["pf"] = _termPf, ["type"] = (int)_termId.Type, ["id"] = _termId.Instance, ["x"] = _termPos.X, ["y"] = _termPos.Y, ["z"] = _termPos.Z }.ToString(), _ctx.Log);
        }

        private bool LoadTerminal()
        {
            try
            {
                var o = JsonStore.Load<JObject>(TerminalPath, _ctx.Log);
                if (o == null) return false;
                _termPf = (int)o["pf"]; _termId = new Identity((IdentityType)(int)o["type"], (int)o["id"]);
                _termPos = new Vector3((float)o["x"], (float)o["y"], (float)o["z"]);
                return true;
            }
            catch (Exception ex) { _ctx.Log("MISSIONRUN: couldn't read the saved terminal: " + ex.Message); return false; }
        }
        private byte[] _lastQuestLog;

        // Missions finished lately (door pf + spot, UTC), in done.json: the quest log is the zone-in snapshot, and it
        // still lists a mission finished after it. After a 'mission run' restart he walked back to the finished
        // one's door, couldn't get in, and deleted it (01:12 Borealis, 01:32 Athen Shire, 2026-09-25).
        private string DonePath => Path.Combine(_pluginDir, "done.json");
        private List<(int pf, Vector3 at, DateTime when)> _doneStore;
        private List<(int pf, Vector3 at, DateTime when)> Done
        {
            get
            {
                if (_doneStore != null) return _doneStore;
                _doneStore = new List<(int, Vector3, DateTime)>();
                try
                {
                    if (File.Exists(DonePath))
                        foreach (JObject o in JArray.Parse(File.ReadAllText(DonePath)))
                            _doneStore.Add(((int)o["pf"], new Vector3((float)o["x"], 0, (float)o["z"]), (DateTime)o["when"]));
                }
                catch { }
                return _doneStore;
            }
        }
        private void RememberDone(int pf, Vector3 at)
        {
            Done.RemoveAll(d => (DateTime.UtcNow - d.when).TotalHours > 1);
            Done.Add((pf, at, DateTime.UtcNow));
            try { File.WriteAllText(DonePath, new JArray(Done.Select(d => new JObject { ["pf"] = d.pf, ["x"] = d.at.X, ["z"] = d.at.Z, ["when"] = d.when })).ToString()); } catch { }
        }
        private bool DoneLately(int pf, Vector3 at) => Done.Any(d => d.pf == pf && Movement.Flat(d.at, at) < 20 && (DateTime.UtcNow - d.when).TotalHours < 1);

        // The quest log (QuestFullUpdate, sent at every zone-in) carries each mission's destination the same way
        // the terminal list does: Identity(Playfield2 0x9C50, pf), 8 bytes, then the door's x, y, z as floats.
        // Checked on capture 20260923-201746: mission 55EE1C4C, offered at pf 570 (748,35,1721), sits in the
        // quest log at offset 675 as 9C50:570, then (748.19, 35.28, 1720.71). This is what "upload to map" shows.
        private MissionInfo FromQuestLog()
        {
            var b = _lastQuestLog;
            if (b == null) return null;
            var found = new List<(int pf, Vector3 at)>();
            for (int i = 0; i + 28 <= b.Length; i++)
            {
                if (b[i] != 0 || b[i + 1] != 0 || b[i + 2] != 0x9C || b[i + 3] != 0x50) continue;
                int pf = (b[i + 4] << 24) | (b[i + 5] << 16) | (b[i + 6] << 8) | b[i + 7];
                if (pf <= 0 || pf > 20000) continue;
                float x = BeFloat(b, i + 16), y = BeFloat(b, i + 20), z = BeFloat(b, i + 24);
                if (!(x > 0 && x < 10000 && z > 0 && z < 10000 && y > -500 && y < 3000)) continue;
                found.Add((pf, new Vector3(x, y, z)));
            }
            var saved = LoadSaved();
            var pick = found.Where(f => FitsZone(f.pf) && !DoneLately(f.pf, f.at))
                            .OrderBy(f => saved != null && saved.Playfield.Instance == f.pf && Movement.Flat(f.at, saved.Location) < 20 ? 0 : 1)
                            .Select(f => ((int, Vector3)?)f).FirstOrDefault();
            if (pick == null) { if (found.Count == 0) ClearSaved(); return null; }
            _ctx.Log($"MISSIONRUN: quest log has a mission in pf {pick.Value.Item1} at ({pick.Value.Item2.X:0},{pick.Value.Item2.Z:0}).");
            var m = saved != null && saved.Playfield.Instance == pick.Value.Item1 && Movement.Flat(pick.Value.Item2, saved.Location) < 20 ? saved : new MissionInfo
            {
                MissionIdentity = new Identity(IdentityType.Mission, 0), MissionIcon = 0, Credits = 0,
                MissionItemData = new MissionItemReward[0],
                Playfield = new Identity(IdentityType.Playfield2, pick.Value.Item1),
            };
            m.Location = pick.Value.Item2;
            return m;
        }

        private static float BeFloat(byte[] b, int i) => BitConverter.ToSingle(new[] { b[i + 3], b[i + 2], b[i + 1], b[i] }, 0);

        private bool FitsZone(int pf)
        {
            var zones = _ctx.Config.MissionZones;
            if (zones == null || zones.Count == 0) return true;
            string name = Playfield.TryGetPlayfieldNameFromId(pf, out string n) ? n : "";
            return zones.Any(z => string.Equals(z?.Trim(), name, StringComparison.OrdinalIgnoreCase) || (int.TryParse(z, out int id) && id == pf));
        }

        private const float TerminalRowMetres = 15f;   // how far apart terminals standing side by side may be
        private bool _termKindWarned;

        /// <summary>Before a roll: the terminal must be the kind his team status calls for - joining or leaving a
        /// team since the run started changes it. Switches to one of the right kind standing by the terminal, or
        /// says once that there is none and waits (false).</summary>
        private bool TerminalFitsTeam(LocalPlayer me)
        {
            bool team = MissionRoll.WantTeam(me);
            var cur = DynelManager.AllDynels.FirstOrDefault(d => d != null && d.Identity == _termId);
            if (cur != null && MissionRoll.IsTeamTerminal(cur) == team) { _termKindWarned = false; return true; }
            var other = MissionRoll.MatchingTerminal(me, _termPos, TerminalRowMetres);
            if (other != null)
            {
                _ctx.Log($"MISSIONRUN: {(team ? "in" : "not in")} a team: switching from '{cur?.Name}' {_termId} to '{other.Name}' {other.Identity}.");
                _termId = other.Identity; _termPos = other.Transform.Position; SaveTerminal();
                _termKindWarned = false;
                Enter(Phase.ToTerminal, "to the " + (team ? "team" : "solo") + " terminal");
                return false;
            }
            if (!_termKindWarned)
            {
                _termKindWarned = true;
                _tell($"I'm {(team ? "" : "not ")}in a team, but there's no {(team ? "team" : "solo")} mission terminal here. Waiting until that changes.");
            }
            return false;
        }

        private void Save(MissionInfo m)
        {
            try
            {
                var o = new JObject
                {
                    ["id"] = m.MissionIdentity.Instance, ["type"] = m.MissionIcon, ["pf"] = m.Playfield.Instance,
                    ["x"] = m.Location.X, ["y"] = m.Location.Y, ["z"] = m.Location.Z, ["credits"] = m.Credits,
                    ["rewards"] = new JArray((m.MissionItemData ?? new MissionItemReward[0]).Select(r => new JArray(r.LowId, r.HighId, r.Ql))),
                };
                JsonStore.Save(SavePath, o.ToString(), _ctx.Log);
            }
            catch (Exception ex) { _ctx.Log("MISSIONRUN: couldn't save the mission: " + ex.Message); }
        }

        private void ClearSaved() { try { if (File.Exists(SavePath)) File.Delete(SavePath); } catch { } }

        private MissionInfo LoadSaved()
        {
            try
            {
                var o = JsonStore.Load<JObject>(SavePath, _ctx.Log);
                if (o == null) return null;
                return new MissionInfo
                {
                    MissionIdentity = new Identity(IdentityType.Mission, (int)o["id"]),
                    MissionIcon = (int)o["type"],
                    Playfield = new Identity(IdentityType.Playfield2, (int)o["pf"]),
                    Location = new Vector3((float)o["x"], (float)o["y"], (float)o["z"]),
                    Credits = (int)o["credits"],
                    MissionItemData = ((JArray)o["rewards"]).Select(r => new MissionItemReward { LowId = (int)r[0], HighId = (int)r[1], Ql = (int)r[2] }).ToArray(),
                };
            }
            catch (Exception ex) { _ctx.Log("MISSIONRUN: couldn't read the saved mission: " + ex.Message); return null; }
        }

        // ---- Helpers ---------------------------------------------------------------------------------------

        /// <summary>The nearest NPC fighting the bot or one of its pets: what a solo run defends against.</summary>
        public SimpleChar Attacker(LocalPlayer me)
        {
            if (!Active || me == null) return null;
            if (_clock < _fleeUntil) return null;
            var pets = new HashSet<Identity>(me.Pets.Select(p => p.Identity));
            // THE PERSON WE CAME TO FIND is never an enemy. The moment the bot selects him and the mission
            // completes, the server shows him 'fighting' the bot (Kirby Schatz 23:38, Levi McDannold 00:22:18,
            // 0.3 s after completion) though he never lands a blow; the bot then swung at him for 12 and 70+
            // minutes. Set aside for 10 minutes: no swings, and he doesn't count as a mob on us.
            var findTarget = _mission.FindPersonTarget;
            if (findTarget.HasValue && !_combat.IsSetAside(findTarget.Value))
            {
                var fp = DynelManager.Npcs.FirstOrDefault(n => n != null && n.Identity == findTarget.Value);
                if (fp != null && fp.FightingIdentity.HasValue && fp.FightingIdentity.Value == me.Identity)
                {
                    _ctx.Log($"MISSIONRUN: '{fp.Name}' is the person this mission sent me to find, not an enemy; not fighting him.");
                    _combat.SetAside(me, fp.Identity, 600);
                }
            }
            // STATIONARY SHOOTERS: guard turrets don't follow, just run past them (owner, 2026-09-24; the bot stood
            // 4 minutes swinging a melee weapon at a Guard Turret 12.6 m off until it died). No flag marks them
            // (same flags as a summoned pet), so by behaviour: attacking us from more than 6 m and not moved at
            // all in 5 s. Set aside for a minute; it doesn't count as a mob on him either (Main).
            foreach (var n in DynelManager.Npcs)
            {
                if (n == null || !n.FightingIdentity.HasValue || n.FightingIdentity.Value != me.Identity || _combat.IsSetAside(n.Identity)) { if (n != null) _still.Remove(n.Identity); continue; }
                var p = n.Transform.Position;
                if (!_still.TryGetValue(n.Identity, out var st) || Vector3.Distance(st.pos, p) > 0.5f) { _still[n.Identity] = (p, _clock); continue; }
                // OFF (06:31-06:36, 2026-09-24): ordinary ranged mobs stand still while they shoot too (Rollerrats,
                // Blubbags, Probes); setting them all aside meant he never fought back and died twice. Mobs out of
                // reach are now walked up to (Fight phase); one his blows can't hurt is dropped after 20 s.
                if (false && _clock - st.since > 5 && me.DistanceFrom(n) > 6f)
                {
                    _ctx.Log($"MISSIONRUN: '{n.Name}' shoots from {me.DistanceFrom(n):0} m and hasn't moved in {(_clock - st.since):0} s: a stationary shooter; running past it.");
                    _combat.SetAside(me, n.Identity, 60);
                    _still.Remove(n.Identity);
                }
            }
            var a = DynelManager.Npcs
                .Where(n => n != null && n.FightingIdentity.HasValue && (n.FightingIdentity.Value == me.Identity || pets.Contains(n.FightingIdentity.Value))
                            && !n.Owner.HasValue && (!n.TryGetStat(Stat.Health, out int hp) || hp > 0)
                            && !_combat.IsSetAside(n.Identity)
                            && !TooStrong(me, n)
                            && IsMob(n, _mission.InMission)
                            && me.DistanceFrom(n) <= _ctx.Config.AssistMaxDistance)
                .OrderBy(n => me.DistanceFrom(n)).FirstOrDefault();
            if (a == null) { _defId = null; return null; }
            // A 'fight' that goes nowhere: Kirby Schatz, the person a find-person mission sent him to, 'fought'
            // him for 12 minutes (23:38-23:51, 2026-09-23): his HP never moved, ours never moved, and every blow
            // came back as feedback 110. When our blows don't lower a mob's HP, it is set aside for 5 minutes.
            bool readable = a.TryGetStat(Stat.Health, out int ahp);
            int mine = _ctx.Status.SelfHpPct;
            if (_defId != a.Identity) { _defId = a.Identity; _defSince = _clock; _defHp = ahp; _defMyMin = mine < 0 ? 100 : mine; }
            else
            {
                if (mine >= 0) _defMyMin = Math.Min(_defMyMin, mine);
                if (readable && ahp < _defHp) { _defHp = ahp; _defSince = _clock; }
                // Its HP unreadable: judge by us. 30 s on it without being hurt once (Levi McDannold, 00:22-00:37).
                else if (!readable && _clock - _defSince > 30 && _clock - _lastHurt > 30)
                {
                    _ctx.Log($"MISSIONRUN: {(_clock - _defSince):0} s on '{a.Name}' (HP unreadable) and nothing has hurt me for 30 s; leaving it alone for 5 minutes.");
                    _combat.SetAside(me, a.Identity, 300);
                    _defId = null;
                    return null;
                }
                // ...and one we can't hurt while it hurts us: a Guard Turret 12.6 m off, the bot standing with a
                // melee weapon for 4 minutes until it died (00:08-00:12, 2026-09-24), the mission already done.
                // 20 s of fighting it without its HP dropping at all: leave it.
                else if (readable && _clock - _defSince > 20)
                {
                    _ctx.Log($"MISSIONRUN: {(_clock - _defSince):0} s on '{a.Name}' and its HP hasn't moved ({ahp}); leaving it alone for 5 minutes.");
                    _combat.SetAside(me, a.Identity, 300);
                    _defId = null;
                    return null;
                }
            }
            return a;
        }
        /// <summary>What the run may fight. Outside a mission building only a real mob: Side 3 (Monster) and no
        /// vendor/talk/pet flags - the hunt command's rule from 33 captures (HuntController.IsHuntable). NPCs are
        /// not fought even when they show as fighting him: on the way to a Longest Road door (06:58, 2026-09-24)
        /// he attacked a Male Watcher, an NPC, 37 m off (owner: "stop him from attacking the npcs on the way").
        /// Inside a building every NPC left is fair game.</summary>
        public static bool IsMob(SimpleChar n, bool inMission)
        {
            if (n == null) return false;
            if (inMission) return true;
            return MobFilter.IsFightableKind(n) && (int)n.Side == MobFilter.SideMonster;
        }

        // A mob far above his level is never fought: run (a level 50 Male Watcher killed him at 36, 06:51).
        private static bool TooStrong(LocalPlayer me, SimpleChar n)
            => me.TryGetStat(Stat.Level, out int mine) && n.TryGetStat(Stat.Level, out int theirs) && theirs > mine + 5;

        private readonly Dictionary<Identity, (Vector3 pos, double since)> _still = new Dictionary<Identity, (Vector3 pos, double since)>();
        private Identity? _defId;
        private double _defSince;
        private int _defHp, _defMyMin = 100;

        private void Enter(Phase p, string why)
        {
            if (p != _phase) _ctx.Log($"MISSIONRUN: {_phase} -> {p} ({why})");
            _phase = p; _phaseTime = 0;
            if (p == Phase.ToDoor || p == Phase.Rolling) { _blitzTries = 0; _exitStands = 0; _exitDoor = null; }
            if (p == Phase.Rolling) _rollWarned = false;
            // The walk-to-it tries are per arrival: he reaches the terminal through its own approach (not travel's
            // 'there'), so without this the count stayed at 2 and the next trip had none (09:23-09:28, 2026-09-24).
            if (p == Phase.Rolling || p == Phase.EnterDoor || p == Phase.Hike) _straightTries = 0;
            if (p == Phase.Leaving) _leaveWarned = false;
            if (p == Phase.ToTerminal || p == Phase.ToDoor) { _travelStarted = false; _travelTries = 0; }
            _approach = 0;
        }

    }
}
