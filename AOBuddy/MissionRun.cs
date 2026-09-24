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
        private readonly Func<bool> _dead, _recovering, _buffing, _fighting, _needsRecovery, _inCombat;
        private readonly Func<int> _selfHp;
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
                          FollowController follow, string pluginDir, Action<string> tell, Func<bool> dead, Func<bool> recovering, Func<bool> buffing, Func<bool> fighting, Func<bool> needsRecovery, Func<bool> inCombat, Func<int> selfHp, CombatController combat)
        {
            _ctx = ctx; _roll = roll; _mission = mission; _overland = overland; _follow = follow;
            _pluginDir = pluginDir; _tell = tell; _dead = dead; _recovering = recovering; _buffing = buffing; _fighting = fighting; _needsRecovery = needsRecovery; _inCombat = inCombat; _selfHp = selfHp; _combat = combat;
            _roll.ListArrived += OnList;
        }

        // ---- Commands ----------------------------------------------------------------------------------

        public void Command(string args, Action<string> reply)
        {
            string a = (args ?? "").Trim().ToLowerInvariant();
            if (a == "stop") { if (Active) { Stop("owner said stop"); reply($"Mission run stopped after {_done} mission(s)."); } else reply("No mission run going."); return; }
            if (a == "status") { reply(Status()); return; }
            if (a.StartsWith("keep"))
            {
                KeepSet();
                string rest = args.Trim().Length > 4 ? args.Trim().Substring(4).Trim() : "";   // original case for the name
                if (rest.StartsWith("add ", StringComparison.OrdinalIgnoreCase)) { string nm = rest.Substring(4).Trim(); _keepAdded.Add(nm); SaveKeep(); reply($"Keeping '{nm}': never sold."); return; }
                if (rest.StartsWith("remove ", StringComparison.OrdinalIgnoreCase)) { string nm = rest.Substring(7).Trim(); reply(_keepAdded.Remove(nm) ? $"'{nm}' off the keep list." : $"'{nm}' isn't on the list I added to (config KeepItems is edited in config.json)."); SaveKeep(); return; }
                reply("Never sold: " + string.Join(", ", KeepSet().OrderBy(x => x)) + ". 'mission run keep add <exact item name>' / 'keep remove <name>'.");
                return;
            }
            if (a.StartsWith("shop"))
            {
                string v = a.Substring(4).Trim();
                if (v == "on" || v == "off") _ctx.Config.MissionShop = v == "on";
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
                    reply(bags.Count == 0 ? "No bags in my inventory." : string.Join(" | ", bags.Select((b, n) => $"{n + 1}) {b.Name}{(_personalBags.Contains(b.UniqueIdentity) ? " [personal]" : "")}")) + ". 'mission run shop personal <n>' toggles.");
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
                reply($"Housekeeping at Fair Trade when out of room: {(_ctx.Config.MissionShop ? "ON" : "off")} (test). Keeps {_ctx.Config.MissionCashReserve:N0} credits. 'mission run shop on|off|now|list|bags|personal <n>', 'mission run keep'.");
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
                    reply($"Difficulty for {_ctx.Config.MissionStyle} style set to {d}; from the next roll.");
                }
                else reply($"Difficulty: blitz {_ctx.Config.MissionDifficulty}, fight {Math.Max(_ctx.Config.MissionDifficulty, _ctx.Config.MissionFightDifficulty)} (now on {_ctx.Config.MissionStyle}). 'mission run difficulty <n>' sets it for the current style (captures: 1 easy, 6 his level, 11 hard).");
                return;
            }
            if (a.StartsWith("style"))
            {
                string st = a.Length > 5 ? a.Substring(5).Trim() : "";
                if (st == "fight" || st == "blitz") { _ctx.Config.MissionStyle = st; reply(st == "fight" ? "Style: fight - I'll stop and fight anything that attacks me." : $"Style: blitz - I run to the end and stim, and only stop to fight under {_ctx.Config.MissionFightBelowPercent}% with no stim ready."); }
                else reply($"Style is {_ctx.Config.MissionStyle}. 'mission run style fight' or 'mission run style blitz'.");
                return;
            }
            if (a == "skip")
            {
                if (!Active) { int n = DeleteHeldMissions(); reply($"Deleted {n} mission(s)."); return; }
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
        }

        private void OnList(IReadOnlyList<MissionInfo> list)
        {
            if (_phase != Phase.AwaitList) return;
            var ok = list.Where(Fits).ToList();
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
            var opt = new ZoneRouteOptions
            {
                Stat = id => me.TryGetStat((Stat)id, out int v) ? v : (int?)null,
                UnknownPasses = true,
                Filter = e => (e.Kind == ExitKind.ZoneLine || e.Kind == ExitKind.Scotty || e.ObjInstance != 0) && !BadExit(e),
            };
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
            if (moving && _phase != Phase.Fight && _bigSnaps.Count(t => _clock - t < 8) >= 2 && _clock - _lastHurt > 5)
            {
                _bigSnaps.Clear();
                // On the way somewhere it is a wall far more often than a root: ICC 07:02 (2026-09-24), pulled back
                // at the wall by the Grid, stood 15 s, then walked the same route into the same wall. The owner: "resetting
                // to last known good pos is a must". So walk the clean trail back and plan again from there.
                if (_phase == Phase.ToTerminal || _phase == Phase.ToDoor || _phase == Phase.Hike || (_phase == Phase.Shop && _shopStep == ShopStep.Travel))
                {
                    if (_overland.Active) _overland.Stop("pulled back");
                    _follow.ClearMovement();
                    _travelReturn = _phase == Phase.Hike ? _hikeReturn : _phase;   // Shop keeps its step (Travel)
                    _ctx.Log($"MISSIONRUN: the server keeps pulling me back during {_phase}; back to my last good spot and planning again.");
                    StartBackoff(me, "travel");
                    return false;
                }
                _heldUntil = _clock + 15;
                _fightStart = _clock; _fightHpMin = 100;
                _fightReturn = _phase;
                if (_mission.Active) _mission.Stop("held");
                if (_overland.Active) _overland.Stop("held");
                _follow.ClearMovement();
                _ctx.Log($"MISSIONRUN: the server keeps pulling me back during {_phase} (rooted or snared?); standing still 15 s.");
                Enter(Phase.Fight, "held");
                return false;
            }
            int hpTick = _selfHp();
            if (hpTick >= 0) { if (_prevHp >= 0 && hpTick < _prevHp) _lastHurt = _clock; _prevHp = hpTick; }
            if (moving && _fighting() && (_clock >= _fightIgnoreUntil || (hpTick >= 0 && hpTick < _ctx.Config.MissionFightBelowPercent)))
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
                    int hpNow = _selfHp();
                    if (hpNow >= 0) _fightHpMin = Math.Min(_fightHpMin, hpNow);
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
                            if (foe != null && me.DistanceFrom(foe) > 4f && _clock - _lastHurt < 5) { _phaseTime = 0; _follow.SetManualTarget(foe.Transform.Position); return true; }
                        }
                        // Stay until the fight is really over (not just back above the emergency line).
                        if (_inCombat()) { _follow.ClearManual(); _phaseTime = 0; return false; }
                        if (_clock < _heldUntil) return false;
                        if (_phaseTime < 3) return false;          // a moment for stragglers and loot
                        // Hurt or low on nano: stay put so the rest logic sits him down with a recharger (it starts
                        // 6 s after the last blow) instead of walking off into the next room half dead. At most a
                        // minute, in case the rest logic won't sit for a reason of its own.
                        // ...unless something is still hitting him with nothing left to fight (set-aside turrets,
                        // 06:33 2026-09-24): then get moving, stims on the way.
                        bool beingHit = _clock - _lastHurt < 5;
                        if (!beingHit && (_recovering() || _needsRecovery()) && _phaseTime < 60) return false;
                        if (beingHit) _ctx.Log("MISSIONRUN: still being hit with nothing I can fight; moving on.");
                        _ctx.Log("MISSIONRUN: fight over; carrying on.");
                    }
                }
                    switch (_fightReturn)
                    {
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
                    if (_dead()) { _phaseTime = 0; return false; }
                    if (_phaseTime < 3) return false;          // let the reclaim land
                    _afterDeath = true;
                    Enter(Phase.ToTerminal, "reclaimed; to the terminal to wait out rez sickness");
                    return false;

                case Phase.ToTerminal:
                    if (_recovering()) { _phaseTime = 0; return false; }
                    if ((int)Playfield.ModelId == _termPf && !_overland.Active)
                    {
                        // Travel stops a few metres short and the terminal's own body keeps us ~5 m from its centre
                        // (2026-09-23 21:29: 'Arrived' at 5.4 m, over and over). So finish on foot, straight at it,
                        // and roll from wherever that ends within the roll's 6 m.
                        float dT = Flat(me.Transform.Position, _termPos);
                        if (dT <= 12f)
                        {
                            _approach += dt;
                            if (dT <= 3.5f || _approach > 5)
                            {
                                _follow.ClearManual();
                                if (dT <= 6f) { Enter(Phase.Rolling, "at the terminal"); return false; }
                                _approach = 0;   // couldn't close in: travel again
                            }
                            else { _follow.SetManualTarget(_termPos); return true; }   // from the approach spot: clear of pads
                        }
                    }
                    return Travel(me, _termPf, TerminalApproach(), "the terminal");

                case Phase.Rolling:
                    if (_recovering()) { _phaseTime = 0; return false; }
                    if (_afterDeath)
                    {
                        // At the terminal after a death: sit out the sickness and let the rebuffs go on first.
                        // Buffed = nothing cast or queued for 15 s (the buff scan runs every 2 s and puts up one buff
                        // at a time, refilling nano between them) - not just a fixed pause after the sickness.
                        if (SupportController.IsRezSick(me) || me.IsCasting || _buffing()) { _phaseTime = 0; return false; }
                        if (_phaseTime < 3) return false;
                        _afterDeath = false;
                        _tell("Rez sickness is over and my buffs are back up; back to work.");
                        // Twice dead in the same mission (23:14 and 23:20, 2026-09-23: the same pack in the same corner
                        // of an Omnilab) is a bad mission: drop it and roll another.
                        if (_current != null && !_completed && _deathsHere >= 2) { Skip($"died {_deathsHere} times in it"); return false; }
                        if (_current != null && !_completed) { _travelTries = 0; _doorTries = 0; Enter(Phase.ToDoor, "back to the open mission"); return false; }
                    }
                    if (_phaseTime < 1.5) return false;
                    // Out of room: he always needs 4 free inventory slots (not bags, not items) to pull the mission
                    // keys and rewards (owner, 2026-09-23), and the stash has already filled every bag it could. He
                    // can't go on; buying bags, selling and banking nano crystals come later (MISSION-MODE-PLAN.md).
                    if (Inventory.NumFreeSlots < 4 && StartShop($"only {Inventory.NumFreeSlots} free slot(s)")) return false;
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
                    if (!((int)Playfield.ModelId == _termPf && Flat(me.Transform.Position, _termPos) <= 6f)) { Enter(Phase.ToTerminal, "not at the terminal"); return false; }
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
                    if (_recovering()) { _phaseTime = 0; return false; }
                    int pf = _current.Playfield.Instance;
                    Vector3 goal = new Vector3(_current.Location.X, _current.Location.Y, _current.Location.Z);
                    if (!_overland.Active && (int)Playfield.ModelId == pf && Flat(me.Transform.Position, goal) <= 12f)
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
                        // First side: the one we arrived from.
                        double ang = Math.Atan2(pos.Z - d.Z, pos.X - d.X);
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
                            if (Flat(pos, outside) <= 3.5f) { _doorStep = 2; _doorStepTime = 0; return false; }
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
                    if (!_mission.InMission && _backoffNext != "blitz" && _backoffNext != "travel") { _follow.ClearManual(); Enter(_backoffNext == "leave" ? Phase.ToTerminal : Phase.Blitz, "out"); return false; }
                    bool replaying = _follow.ReplayCount > 0 && !_follow.ManualActive;
                    bool done = _phaseTime > (replaying ? 25 : 4) || (_backoffTo.HasValue && Flat(me.Transform.Position, _backoffTo.Value) <= 1.2f);
                    if (!done && replaying) return true;
                    if (!done && _backoffTo.HasValue) { _follow.SetManualTarget(_backoffTo.Value); return true; }
                    _follow.ClearMovement();
                    switch (_backoffNext)
                    {
                        case "blitz": Enter(Phase.AwaitBlitz, "blitz again"); break;
                        case "travel": Enter(_travelReturn, "travel again from a good spot"); break;
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
            if (gap > 5f) { _bigSnaps.Add(_clock); if (_bigSnaps.Count > 20) _bigSnaps.RemoveAt(0); }
        }
        private readonly List<double> _bigSnaps = new List<double>();
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

        // The zone's walk grid (Algorithman's OverlandGrid outdoors, FloorGrid indoors), built off the frame
        // thread the way travel builds it.
        private System.Threading.Tasks.Task<IWalkGrid> _hikeGridTask;
        private int _hikeGridPf = -1, _hikeGridTaskPf = -1;
        private IWalkGrid _hikeGrid;

        private IWalkGrid HikeGrid()
        {
            int pf = (int)Playfield.ModelId;
            if (pf == _hikeGridPf) return _hikeGrid;
            if (_hikeGridTask == null || _hikeGridTaskPf != pf)
            {
                string dir = _pluginDir; var log = _ctx.Log; _hikeGridTaskPf = pf;
                _hikeGridTask = System.Threading.Tasks.Task.Run(() =>
                {
                    var nav = AOBuddyNav.Load(dir, pf);
                    return (IWalkGrid)OverlandGrid.Build(dir, pf, nav, log) ?? FloorGrid.Build(dir, pf, nav, log);
                });
                return null;
            }
            if (!_hikeGridTask.IsCompleted) return null;
            _hikeGrid = _hikeGridTask.IsFaulted ? null : _hikeGridTask.Result;
            _hikeGridTask = null; _hikeGridPf = pf;
            if (_hikeGrid == null) { _hikeRoute = new List<Vector3>(); }   // no grid: straight
            return _hikeGrid;
        }

        private bool StartHike(LocalPlayer me, int pf, Vector3 goal, string what)
        {
            if (_clock - _hikeLastHike < 20) return false;          // one attempt at a time
            int here = (int)Playfield.ModelId;
            if (here == pf) return false;                            // same zone: nothing to cross
            var opt = new ZoneRouteOptions
            {
                UseScotty = false, UnknownPasses = true,
                Stat = id => me.TryGetStat((Stat)id, out int v) ? v : (int?)null,
                Filter = e => (e.Kind == ExitKind.ZoneLine || e.ObjInstance != 0) && !BadExit(e),
            };
            ZoneRoute route;
            try { route = Zoning.FindRoute(here, me.Transform.Position, pf, goal, opt); } catch { route = null; }
            if (route == null || route.Hops.Count == 0) { _ctx.Log("MISSIONRUN: no zone route without Scotty either."); return false; }
            _hike = route.Hops[0]; _hikeFromPf = here; _hikeTargetPf = pf; _hikeGoal = goal; _hikeWhat = what;
            _hikeReturn = _phase; _hikeLastHike = _clock; _hikePass = -1; _hikePassStage = 0; _hikePassAt = _clock; _hikeUses = 0; _hikeUsedAt = -99; _hikeRoute = null; _hikeBackTo = null; _hikeCameFrom = null; _hikeOnAt = -1;
            if (_overland.Active) _overland.Stop("mission run walks this leg itself");
            var e = _hike.Exit;
            _ctx.Log($"MISSIONRUN: walking to the first exit myself: {e} at ({e.A.X:0},{e.A.Z:0}) ({route.Describe()}).");
            Enter(Phase.Hike, "walking to the exit myself");
            return true;
        }

        private bool HikeTick(LocalPlayer me)
        {
            if ((int)Playfield.ModelId != _hikeFromPf)
            {
                _follow.ClearMovement();
                _ctx.Log($"MISSIONRUN: through to {Zoning.Name((int)Playfield.ModelId)}; travel takes it from here.");
                Enter(_hikeReturn, "through the exit");
                return false;
            }
            if (_phaseTime > 150)
            {
                _follow.ClearMovement();
                _ctx.Log("MISSIONRUN: couldn't get through that exit on foot; back to travel.");
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
                List<Vector3> best = null; float bestLeft = float.MaxValue;
                foreach (float r in new[] { 0f, 4f, 8f, 12f, 16f, 24f })
                    for (int k = 0; k < (r == 0 ? 1 : 8); k++)
                    {
                        double t = k * Math.PI / 4;
                        var goal = new Vector3(at.X + (float)Math.Cos(t) * r, at.Y, at.Z + (float)Math.Sin(t) * r);
                        var path = grid.FindPath(pos, goal, null, 8f, 1.5f, out _);
                        if (path == null) continue;
                        float left = Flat(path[path.Count - 1], at);
                        if (left < bestLeft) { bestLeft = left; best = path; }
                    }
                if (best != null && best.Count > 1)
                {
                    _hikeRoute = best;
                    _follow.LoadReplay(best.Skip(1), false);
                    _ctx.Log($"MISSIONRUN: grid route to {bestLeft:0} m from the exit ({best.Count} points), then straight on.");
                }
                else _ctx.Log("MISSIONRUN: no grid route toward the exit; walking straight.");
            }
            if (_follow.ReplayCount > 0) return true;                 // still on the grid leg

            if (e.Kind == ExitKind.ZoneLine)
            {
                // Walk to the line, then on across it.
                Vector3 cross = _hike.CrossTo ?? at;
                _follow.SetManualTarget(Flat(pos, at) > 2f ? at : cross);
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
                if (Flat(pos, at) > 3f && _hikeUses == 0 && _phaseTime < 140) { _follow.SetManualTarget(at); return true; }
                _follow.ClearMovement();
                if (_clock - _hikeUsedAt < 4) return false;
                if (_hikeUses >= 3)
                {
                    MarkBadExit(e);
                    _ctx.Log($"MISSIONRUN: used {e} three times and it didn't take me.");
                    Enter(_hikeReturn, "hike failed");
                    return false;
                }
                _hikeUses++; _hikeUsedAt = _clock;
                Client.Send(new GenericCmdMessage { Action = GenericCmdAction.Use, User = me.Identity, Target = new Identity((IdentityType)e.ObjType, e.ObjInstance), Count = 1, Temp4 = 1 });
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
            if (_hikePass >= 4)
            {
                MarkBadExit(e);
                _follow.ClearMovement();
                _ctx.Log("MISSIONRUN: stood on the exit's centre four times and it didn't take me; leaving it.");
                Enter(_hikeReturn, "hike failed");
                return false;
            }
            double ang = (_hikePass / 2) * Math.PI / 2;
            var dir = new Vector3((float)(_hikeDir0.X * Math.Cos(ang) - _hikeDir0.Z * Math.Sin(ang)), 0, (float)(_hikeDir0.X * Math.Sin(ang) + _hikeDir0.Z * Math.Cos(ang)));
            // CONFIRMED 00:14 (2026-09-24): 0.3 m from the centre at 35.74 did nothing for 12 s; 0.2 m at 36.05
            // zoned him 0.6 s later. The pad's top first, then our data's height.
            // A door's recorded position is its centre, ~1.4 m up (Borealis Fair Trade door 68.49 over ground 67.07,
            // where the owner stood): stand on the ground. Only a whompa pad needs its top surface.
            float padY = e.Kind == ExitKind.Proxy ? pos.Y : e.A.Y + (_hikePass % 2 == 0 ? 0.285f : 0f);
            var start = new Vector3(e.A.X - dir.X * 5f, pos.Y, e.A.Z - dir.Z * 5f);
            // The walker stops 1.5 m short of its target: aim 1.2 m past the centre to stop ~0.3 m before it.
            var aim = new Vector3(e.A.X + dir.X * 1.2f, padY, e.A.Z + dir.Z * 1.2f);
            if (_hikePassStage == 0)
            {
                if (Flat(pos, start) > 1.2f && _clock - _hikePassAt < 8) { _follow.SetManualTarget(start); return true; }
                _hikePassStage = 1; _hikePassAt = _clock;
                _ctx.Log($"MISSIONRUN: stepping onto the centre of {e} (try {_hikePass + 1}/4, height {padY:0.00}).");
            }
            if (_hikePassStage == 1)
            {
                if (_follow.ManualActive || _clock - _hikePassAt < 0.3) { _follow.SetManualTarget(aim); if (_clock - _hikePassAt < 6) return true; }
                _follow.ClearMovement();
                _hikePassStage = 2; _hikePassAt = _clock;
                _ctx.Log($"MISSIONRUN: standing at ({pos.X:0.00},{pos.Y:0.00},{pos.Z:0.00}), {Flat(pos, e.A):0.0} m from the centre.");
            }
            if (_clock - _hikePassAt < 4) return false;   // standing on it: the zone comes after the stop
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
                        near = Math.Min(near, Flat(q, e.A));
                        if (e.Kind == ExitKind.ZoneLine) near = Math.Min(near, Flat(q, e.B));
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
        private bool BadExit(ZoneExit e) => _badExits.Contains(ExitKey(e));
        private void MarkBadExit(ZoneExit e)
        {
            if (_badExits.Add(ExitKey(e)))
                _ctx.Log($"MISSIONRUN: {e} didn't take me; routing round it until I restart.");
        }

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
        private enum ShopStep { Travel, Sell, OpenBank, TakeBag, FillBag, StoreBag, Buy, Exit }
        private ShopStep _shopStep;
        private double _shopStepAt, _shopTriedAt = -9999;
        private const int FairTradePf = 1187;
        private static readonly Vector3 ShopSpot = new Vector3(197.74f, 5.01f, 142.38f);
        // The bank terminal is looked up live: the id in the owner's capture (C73D:0EE5BBFF) didn't answer on another
        // game server, where the zone had 'Rubi-Ka Banking Service Terminal' Terminal:C00104A3 at (195.7,144.4)
        // (08:05, 2026-09-24). The captured id stays as the fallback.
        private static Identity BankTerminal
        {
            get
            {
                var me = DynelManager.LocalPlayer;
                var t = DynelManager.AllDynels.Where(d => d != null && d.Identity.Type == IdentityType.Terminal && d.Name != null
                                                          && d.Name.IndexOf("Bank", StringComparison.OrdinalIgnoreCase) >= 0)
                                              .OrderBy(d => me == null ? 0 : Vector3.Distance(me.Transform.Position, d.Transform.Position)).FirstOrDefault();
                return t != null ? t.Identity : new Identity(IdentityType.Terminal, 0x0EE5BBFF);
            }
        }
        private Vector3? _shopArrival;
        private Identity? _shopBag;
        private bool _shopBoughtForNanos, _shopBoughtForRoom;
        private Identity? _nanoSlot;
        private readonly HashSet<Identity> _shopKnownBags = new HashSet<Identity>();
        private readonly HashSet<Identity> _shopFullBags = new HashSet<Identity>();

        private int _sellRounds, _sellStage, _sellMoves;
        private List<Identity> _lastBatch;
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
            if (i?.Name == null || i.UniqueIdentity.Type == IdentityType.Container || IsNano(i) || keep.Contains(i.Name)) return false;
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
                try { if (File.Exists(KeepPath)) foreach (var t in JArray.Parse(File.ReadAllText(KeepPath))) _keepAdded.Add((string)t); } catch { }
            }
            var k = new HashSet<string>(_ctx.Config.KeepItems ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            k.UnionWith(_keepAdded);
            k.Add(_ctx.Config.ResupplyStimName); k.Add(_ctx.Config.ResupplyRechargerName);
            return k;
        }
        private void SaveKeep() { try { File.WriteAllText(KeepPath, new JArray(_keepAdded.ToArray()).ToString()); } catch { } }

        // PERSONAL BAGS: nothing in them is ever sold. Marked by the bag's own identity ('mission run shop bags'
        // lists them numbered, 'mission run shop personal <n>' toggles), saved in personalbags.json.
        private HashSet<Identity> _personalBags;
        private string PersonalPath => Path.Combine(_pluginDir, "personalbags.json");
        private void LoadPersonal()
        {
            if (_personalBags != null) return;
            _personalBags = new HashSet<Identity>();
            try { if (File.Exists(PersonalPath)) foreach (var t in JArray.Parse(File.ReadAllText(PersonalPath))) _personalBags.Add(new Identity((IdentityType)(int)t["type"], (int)t["id"])); } catch { }
        }
        private void SavePersonal() { try { File.WriteAllText(PersonalPath, new JArray(_personalBags.Select(b => new JObject { ["type"] = (int)b.Type, ["id"] = b.Instance })).ToString()); } catch { } }
        private List<Item> Bags() => Inventory.Items.Where(i => i != null && i.Slot.Type == IdentityType.Inventory && i.UniqueIdentity.Type == IdentityType.Container).OrderBy(i => i.Slot.Instance).ToList();
        private HashSet<int> _rewardHistory;
        private string RewardsPath => Path.Combine(_pluginDir, "rewardids.json");
        private string RewardNamesPath => Path.Combine(_pluginDir, "rewardnames.json");   // names too: seeded from the log's accepted rewards
        private void LoadRewards()
        {
            if (_rewardHistory != null) return;
            _rewardHistory = new HashSet<int>();
            _rewardNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try { if (File.Exists(RewardsPath)) foreach (var t in JArray.Parse(File.ReadAllText(RewardsPath))) _rewardHistory.Add((int)t); } catch { }
            try { if (File.Exists(RewardNamesPath)) foreach (var t in JArray.Parse(File.ReadAllText(RewardNamesPath))) _rewardNames.Add((string)t); } catch { }
        }
        private void RememberReward(int low, int high)
        {
            LoadRewards();
            bool added = _rewardHistory.Add(low) | _rewardHistory.Add(high);
            if (added) try { File.WriteAllText(RewardsPath, new JArray(_rewardHistory.ToArray()).ToString()); } catch { }
            if (ItemData.Find(low, out DummyItem it) && it?.Name != null && _rewardNames.Add(it.Name))
                try { File.WriteAllText(RewardNamesPath, new JArray(_rewardNames.ToArray()).ToString()); } catch { }
        }
        public string SellPreview()
        {
            var s = Sellable(); var b = SellableInBags();
            if (s.Count + b.Count == 0) return "Nothing I'd sell right now (open bags are checked; a bag not opened this session isn't).";
            return $"I'd sell {s.Count + b.Count} ({b.Count} from bags): " + string.Join(", ", s.Concat(b).Select(i => i.Name));
        }

        private static bool IsNano(Item i) => i?.Name != null && (i.Name.StartsWith("Nano Crystal", StringComparison.OrdinalIgnoreCase) || i.Name.StartsWith("NanoCrystal", StringComparison.OrdinalIgnoreCase));
        private static List<Item> InvNanos() => Inventory.Items.Where(i => i != null && i.Slot.Type == IdentityType.Inventory && IsNano(i)).ToList();
        private static Item InvItem(Identity? unique) => unique.HasValue ? Inventory.Items.FirstOrDefault(i => i != null && i.Slot.Type == IdentityType.Inventory && i.UniqueIdentity == unique.Value) : null;

        private bool StartShop(string why)
        {
            if (!_ctx.Config.MissionShop || Resupply == null || _clock - _shopTriedAt < 600) return false;
            _shopTriedAt = _clock;
            _shopBag = null; _shopBoughtForNanos = false; _shopBoughtForRoom = false; _shopArrival = null; _shopFullBags.Clear();
            _ctx.Log($"MISSIONRUN: housekeeping ({why}): off to Fair Trade. Selling is not built yet (no capture of a sale).");
            _tell($"Out of room ({why}); going to Fair Trade to bank my nano crystals and buy a bag. (Test switch: mission run shop on|off.)");
            _shopStep = ShopStep.Travel; _shopStepAt = _clock; _travelStarted = false; _travelTries = 0;
            Enter(Phase.Shop, "housekeeping");
            return true;
        }

        private void ShopNext(ShopStep s, string log) { _shopStep = s; _shopStepAt = _clock; _ctx.Log("MISSIONRUN: shop: " + log); }

        private bool ShopTick(LocalPlayer me)
        {
            double t = _clock - _shopStepAt;
            int pf = (int)Playfield.ModelId;
            if (pf == FairTradePf && !_shopArrival.HasValue) { _shopArrival = me.Transform.Position; _ctx.Log($"MISSIONRUN: shop: in Fair Trade at ({_shopArrival.Value.X:0.0},{_shopArrival.Value.Z:0.0})."); }
            switch (_shopStep)
            {
                case ShopStep.Travel:
                    if (pf != FairTradePf)
                    {
                        // Travel Uses a door and, when it doesn't take, replans to another city's door (07:58,
                        // 2026-09-24: Borealis -> Newland Desert -> Mort, never in). The hike stands on doors, so
                        // it goes first; travel only for the zones between.
                        if (!_overland.Active && StartHike(me, FairTradePf, ShopSpot, "Fair Trade")) return false;
                        return Travel(me, FairTradePf, ShopSpot, "Fair Trade");
                    }
                    if (_overland.Active) _overland.Stop("inside Fair Trade");
                    if (Flat(me.Transform.Position, ShopSpot) > 1.5f && t < 30) { _follow.SetManualTarget(ShopSpot); return true; }
                    _follow.ClearMovement();
                    _sellRounds = 0; _sellSentAt = -99; _sellStage = 0; _sellMoves = 0; _sellBagsOpened = false; _lastBatch = null; _refusedSlots.Clear();
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
                            Client.Send(new GenericCmdMessage { Action = GenericCmdAction.Use, User = me.Identity, Target = b.Slot, Count = 1, Temp4 = 0 });
                        _sellBagsOpened = true; _sellSentAt = _clock;
                        return false;
                    }
                    // What the last batch left behind was refused (08:04: three items offered eight times): skip them.
                    if (_lastBatch != null)
                    {
                        foreach (var slot in _lastBatch) if (Inventory.Items.Any(i => i != null && i.Slot == slot)) _refusedSlots.Add(slot);
                        if (_refusedSlots.Count > 0) _ctx.Log($"MISSIONRUN: shop: the shop refused {_refusedSlots.Count} item(s); leaving them.");
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
                    var vm = DynelManager.VendingMachines.OrderBy(v => me.DistanceFrom(v)).FirstOrDefault();
                    if (sell.Count == 0 || vm == null || _sellRounds >= 12)
                    {
                        if (vm == null && sell.Count > 0) _ctx.Log("MISSIONRUN: shop: no shop terminal in sight to sell to.");
                        var bank = BankTerminal;
                        Client.Send(new GenericCmdMessage { Action = GenericCmdAction.Use, User = me.Identity, Target = bank, Count = 1, Temp4 = 1 });
                        ShopNext(ShopStep.OpenBank, $"sold what I could; {Inventory.NumFreeSlots} free slot(s). Opening the bank ({bank}).");
                        return false;
                    }
                    if (_sellStage == 0)
                    {
                        Client.Send(new LookAtMessage { Target = vm.Identity, ReturnInfo = 0 });
                        Client.Send(new GenericCmdMessage { Action = GenericCmdAction.Use, User = me.Identity, Target = vm.Identity, Count = 1, Temp4 = 1 });
                        _sellStage = 1; _shopStepAt = _clock;
                        return false;
                    }
                    if (t < 1) return false;   // the window opens (ShopUpdate + Trade open)
                    var batch = sell.Take(5).ToList();
                    foreach (var it in batch)
                        Client.Send(new TradeMessage { Version = 2, Action = TradeAction.AddItem, Param1 = (int)me.Identity.Type, Param2 = me.Identity.Instance, Param3 = (int)it.Slot.Type, Param4 = it.Slot.Instance });
                    Client.Send(new TradeMessage { Version = 2, Action = TradeAction.Accept });
                    _ctx.Log($"MISSIONRUN: shop: selling {string.Join(", ", batch.Select(b => b.Name))} to '{vm.Name}'.");
                    _lastBatch = batch.Select(b => b.Slot).ToList();
                    _sellStage = 0; _sellRounds++; _sellSentAt = _clock;
                    return false;
                }

                case ShopStep.OpenBank:
                    if (!Inventory.Bank.IsOpen)
                    {
                        if (t < 5) return false;
                        _tell("The bank terminal didn't answer; skipping the banking.");
                        return ShopAfterNanos(me);
                    }
                    if (InvNanos().Count == 0) { _ctx.Log("MISSIONRUN: shop: no nano crystals to bank."); return ShopAfterNanos(me); }
                    return ShopPickBag(me);

                case ShopStep.TakeBag:
                {
                    // Out of the bank (MoveItem Bank:n -> 111, capture seq 27): wait for it in the inventory, then open it.
                    var bag = InvItem(_shopBag);
                    if (bag == null) { if (t < 5) return false; _tell("A bag didn't come out of the bank."); return ShopAfterNanos(me); }
                    Client.Send(new GenericCmdMessage { Action = GenericCmdAction.Use, User = me.Identity, Target = bag.Slot, Count = 1, Temp4 = 0 });
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
                    if (bought.Count == 0) { _tell("Couldn't buy a bag (see RESUPPLY in the log)."); ShopNext(ShopStep.Exit, "leaving."); return false; }
                    if (_shopBoughtForNanos && !_shopBoughtForRoom && InvNanos().Count > 0)
                    {
                        _shopBag = bought[0];
                        var bag = InvItem(_shopBag);
                        Client.Send(new GenericCmdMessage { Action = GenericCmdAction.Use, User = me.Identity, Target = bag.Slot, Count = 1, Temp4 = 0 });
                        _nanoSlot = null;
                        ShopNext(ShopStep.FillBag, "filling the new bag with nano crystals.");
                        return false;
                    }
                    return ShopAfterNanos(me);
                }

                case ShopStep.Exit:
                {
                    if (pf != FairTradePf) { _follow.ClearMovement(); _tell($"Housekeeping done: {Inventory.NumFreeSlots} free slot(s)."); Enter(Phase.ToTerminal, "back from Fair Trade"); return false; }
                    if (t > 30 || !_shopArrival.HasValue) { _follow.ClearMovement(); _tell("Couldn't walk out of Fair Trade."); Stop("stuck in Fair Trade"); return false; }
                    // Back the way he came in: to where he landed, then 3 m on past it, away from the shop spot.
                    Vector3 a = _shopArrival.Value;
                    var d = new Vector3(a.X - ShopSpot.X, 0, a.Z - ShopSpot.Z);
                    float len = d.Magnitude;
                    var target = len > 0.5f ? new Vector3(a.X + d.X / len * 3f, a.Y, a.Z + d.Z / len * 3f) : a;
                    _follow.SetManualTarget(Flat(me.Transform.Position, a) > 1.5f ? a : target);
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
            Client.Send(new GenericCmdMessage { Action = GenericCmdAction.Use, User = me.Identity, Target = bag.UniqueIdentity, Count = 1, Temp4 = 1 });
            bag.MoveToBank();
            ShopNext(ShopStep.StoreBag, $"'{bag.Name}' into the bank ({why}).");
        }

        private bool ShopAfterNanos(LocalPlayer me)
        {
            if (Inventory.NumFreeSlots < 4 && !_shopBoughtForRoom) return ShopBuy(me, forNanos: false);   // one more bag to carry
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
                float want = Math.Min(15f * (_backoffs - 1), 60f), got = 0;
                var back = new List<Vector3>();
                Vector3 last = pos;
                for (int i = _good.Count - 1; i >= 0 && got < want; i--)
                {
                    if (Vector3.Distance(_good[i], pos) < 2f && back.Count == 0) continue;
                    got += Flat(last, _good[i]); last = _good[i];
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
            _backoffTo = len > 0.1f ? pos - flat * (6f / len) : (Vector3?)null;
            _ctx.Log($"MISSIONRUN: stopped short; backing off {(len > 0.1f ? "6 m" : "0 m (no facing)")} before trying to {next} again.");
            Enter(Phase.Backoff, "backing off");
        }
        private bool _fullWarned, _rollWarned, _leaveWarned, _afterDeath;
        private int _blitzTries;
        private double _doorStepTime;

        // travelto, once per leg, through the command it already has; retried twice on failure.
        private bool Travel(LocalPlayer me, int pf, Vector3 goal, string what)
        {
            if (_overland.Active)
            {
                if (_phaseTime > TravelTimeout) { _overland.Stop("mission run: too long"); }
                // Waiting on Scotty: it has never warped this bot. Walk the planner's own route instead.
                else if (_overland.Status().Contains("scty") && _phaseTime > (NoScotty ? 1 : 40) && StartHike(me, pf, goal, what)) return false;   // Scotty's warp comes ~20 s after the tell (Algorithman, 2026-09-24): 40 s, then on foot
                return false;
            }
            if (_clock < _travelWaitUntil) return false;
            if (_travelStarted)
            {
                _travelStarted = false;
                bool there = (int)Playfield.ModelId == pf && Flat(me.Transform.Position, goal) <= 12f;
                if (!there && StartHike(me, pf, goal, what)) return false;
                if (there) { _backoffs = 0; _travelBacks = 0; }
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
                    if (_phase == Phase.ToDoor) { Skip($"can't get to its door ({_overland.Status()})"); return false; }
                    _tell($"I can't get to {what} ({_overland.Status()}); trying again in a minute.");
                    _travelWaitUntil = _clock + 60;
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
                Client.Send(new GenericCmdMessage { Action = GenericCmdAction.Use, User = me.Identity, Target = _bag.Slot, Count = 1, Temp4 = 0 });
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
            var best = doors.OrderBy(d => Flat(d, near)).FirstOrDefault();
            if (Flat(best, near) > 25f) return null;
            _ctx.Log($"MISSIONRUN: door at ({best.X:0},{best.Y:0},{best.Z:0}), {Flat(best, near):0.0} m from the terminal's spot.");
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
            try { File.WriteAllText(TerminalPath, new JObject { ["pf"] = _termPf, ["type"] = (int)_termId.Type, ["id"] = _termId.Instance, ["x"] = _termPos.X, ["y"] = _termPos.Y, ["z"] = _termPos.Z }.ToString()); }
            catch (Exception ex) { _ctx.Log("MISSIONRUN: couldn't save the terminal: " + ex.Message); }
        }

        private bool LoadTerminal()
        {
            try
            {
                if (!File.Exists(TerminalPath)) return false;
                var o = JObject.Parse(File.ReadAllText(TerminalPath));
                _termPf = (int)o["pf"]; _termId = new Identity((IdentityType)(int)o["type"], (int)o["id"]);
                _termPos = new Vector3((float)o["x"], (float)o["y"], (float)o["z"]);
                return true;
            }
            catch (Exception ex) { _ctx.Log("MISSIONRUN: couldn't read the saved terminal: " + ex.Message); return false; }
        }
        private byte[] _lastQuestLog;

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
            var pick = found.Where(f => FitsZone(f.pf))
                            .OrderBy(f => saved != null && saved.Playfield.Instance == f.pf && Flat(f.at, saved.Location) < 20 ? 0 : 1)
                            .Select(f => ((int, Vector3)?)f).FirstOrDefault();
            if (pick == null) { if (found.Count == 0) ClearSaved(); return null; }
            _ctx.Log($"MISSIONRUN: quest log has a mission in pf {pick.Value.Item1} at ({pick.Value.Item2.X:0},{pick.Value.Item2.Z:0}).");
            var m = saved != null && saved.Playfield.Instance == pick.Value.Item1 && Flat(pick.Value.Item2, saved.Location) < 20 ? saved : new MissionInfo
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
                File.WriteAllText(SavePath, o.ToString());
            }
            catch (Exception ex) { _ctx.Log("MISSIONRUN: couldn't save the mission: " + ex.Message); }
        }

        private void ClearSaved() { try { if (File.Exists(SavePath)) File.Delete(SavePath); } catch { } }

        private MissionInfo LoadSaved()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                var o = JObject.Parse(File.ReadAllText(SavePath));
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
            int mine = _selfHp();
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
            int flags = (int)n.Flags;
            if ((flags & (0x200000 | 0x800000 | 0x8000000)) != 0) return false;
            if (n is NpcChar npc && (npc.Owner.HasValue || npc.PetTypeId != 0)) return false;
            return (int)n.Side == 3;
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
            if (p == Phase.Leaving) _leaveWarned = false;
            if (p == Phase.ToTerminal || p == Phase.ToDoor) { _travelStarted = false; _travelTries = 0; }
            _approach = 0;
        }

        private static float Flat(Vector3 a, Vector3 b) { float dx = a.X - b.X, dz = a.Z - b.Z; return (float)Math.Sqrt(dx * dx + dz * dz); }
    }
}
