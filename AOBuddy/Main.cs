using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Clientless.Logging;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
using Newtonsoft.Json;

namespace AOBuddy
{
    /// <summary>
    /// A single-owner teammate. Obeys only the configured owner's tells, silent on public channels,
    /// auto-accepts the owner's team invite. Its behaviour is split into four ISOLATED systems, each
    /// in its own file, so working on one never breaks the others:
    ///   FOLLOW   (FollowController)  — walk the owner's recorded footsteps (incl. up/down ramps).
    ///   COMBAT   (CombatController)  — assist ONLY the owner's fights; never moves the bot.
    ///   TRAVEL   (TravelController)  — ride the same lift/button/grid/whompa the owner used.
    ///   SUPPORT  (SupportController) — stims (both of you), rest, auras, self-vitals, supplies.
    /// Main is only the wiring: the tick loop, chat commands, the perk/knowledge helpers, and two
    /// thin coordinators — Walk() (who moves this frame) and Decide() (the fight/heal/rest ladder).
    /// </summary>
    public class Main : ClientlessPluginEntry
    {
        private enum Mode { Assist, Solo, Idle }

        private BuddyConfig _config = new BuddyConfig();
        private Mode _mode = Mode.Assist;
        private bool _stoodUp;               // set once we've made the one login stand-up decision
        private bool _optionsLogged;         // set once stat 349 (the player-option flags) has been logged at login
        private bool _greeted;
        private int _lastLevel;              // last seen Stat.Level; watch it to announce a ding (0 = not baselined)

        // Death state (driven by the real Client.Died wire signal, not HP reads).
        private bool _dead;
        private double _deadSeconds;
        private Vector3? _deathPos;

        // Server's CurrentMovementMode (Stat 173) values — OmniCell MoveModes enum. A character in a
        // SEATED mode cannot move: the server rejects every move packet and snaps him back to where he
        // sat. You log out by sitting, so you can log back in seated; but Ctrl+C'ing the bot leaves it
        // STANDING, so its login mode is inconsistent. The stand-up wire action is a TOGGLE, and stat 173
        // is only set from the login FullCharacter and NEVER updates after — so we read it ONCE to learn
        // the login mode, send at most one stand-up, and never re-toggle (re-reading a stuck 8=Sit and
        // re-sending made him sit/stand in a loop). See the stand-up logic in OnUpdate.
        private const int MoveModeSit = 8;
        private const int MoveModeSleep = 11;
        private const int MoveModeLounge = 12;

        // While moving we ignore SetPos corrections smaller than this (ramp/Y jitter) to avoid rubber-band,
        // but apply anything this size or larger so a genuine desync can't run away and make him vanish.
        private const float ResyncGapMeters = 10f;

        // The isolated systems + their shared low-level movement and blackboard.
        private BotContext _ctx;
        private OwnerTracker _owner;         // who the owner is, where he is, where he's going (R3.1)
        private ZoneEpisode _episode;        // did the owner follow me through that zone? (R3.2)
        private Movement _move;
        private FollowController _follow;
        private CombatController _combat;
        private PetController _pets;
        private TravelController _travel;
        private SupportController _support;
        private ResupplyController _resupply;
        private NavController _nav;
        private MissionController _mission;
        private HuntController _hunt;
        private ChewyBuffController _chewy;
        private MissionRoll _roll;
        private MissionRun _run;
        private bool _missionWasActive;
        private OverlandController _overland;
        private bool _overlandWasActive;

        private string _pathsDir;
        private string _pluginDir;
        private AOBuddyNav _navData;           // test-only reader for GameData/Nav, see the 'navdata' command
        private byte[] _lastZoneInPacket;      // raw PlayfieldAnarchyF of the current zone; carries a mission's room placements
        private WireCapture _capture;          // 'missiondbg' wire diagnostics (R3.5)
        private string _logFile;

        // Tick / diagnostics state that belongs to Main's coordination, not to any one system.
        private double _hbAccum;             // heartbeat throttle
        private int _lastHpPct = -1;         // to catch death / big HP drops
        private float _effAttackRange = 8f;  // her own weapon's reach (for the heartbeat log)
        private double _decisionAccum;       // decision-tick throttle

        // Self-position bookkeeping (the owner's visibility/position state lives in OwnerTracker, R3.1).
        private Vector3? _lastFramePos;      // to detect server teleports/zones (big position jumps)
        private Vector3? _serverAnchor;      // the last position the server confirmed for us (from SetPos)
        private double _serverAnchorAge;     // seconds since that correction (leash only enforced while fresh)
        private Vector3? _zoneCrossPos;      // our position the instant before a real teleport (the zone line)
        private double _zoneCrossAge = 999;  // seconds since that teleport (a nav transition needs one to be recent)
        private int _diagOwnerMoves, _diagSelfMoves;   // DIAG: CharDCMove messages received per heartbeat (owner vs self)
        private Identity? _lastUsedObj;      // the object the owner most recently USED (button/lift/terminal)
        private Vector3? _lastUsedObjPos;
        private double _lastUsedObjAge = 999; // seconds since that use (a warp needs a teleport right after)
        private bool _navReplaying;          // we handed FOLLOW a recorded nav route (stop it on reacquire)

        // Permanent stat bonuses (PERK + RESEARCH), one wiring line per lifecycle point (R3.3).
        private PerkBonuses _perkBonuses;

        // The knowledge reports (class/nanos/active/learnable/stat/supplies) and path persistence,
        // split out of the command switch (R3.4).
        private KnowledgeReports _know;
        private PathStore _paths;

        public override void Init(string pluginDir)
        {
            LoadConfig(pluginDir);
            _pluginDir = pluginDir;
            _pathsDir = Path.Combine(pluginDir, "paths");
            try { Directory.CreateDirectory(_pathsDir); } catch { }
            _logFile = Path.Combine(pluginDir, "aobuddy.log");
            _mode = ParseMode(_config.DefaultMode);
            _perkBonuses = new PerkBonuses(pluginDir, _config, Log);
            _perkBonuses.Init();
            ItemValues.Load(pluginDir, Log);
            Zoning.Load(pluginDir, Log);

            _ctx = new BotContext(_config, Log, new Clock());
            _owner = new OwnerTracker(_ctx);
            _episode = new ZoneEpisode(_ctx);
            _ctx.Vitals = new VitalsTracker(_ctx);
            _ctx.NavGrid = new NavGridCache();
            // MOVEDBG-OUT (2026-09-25, the Wailing Wastes rubberband): every movement packet we SEND while
            // an overland walk owns the body — movetype, exact coordinates, and the elapsed-ms field as they
            // go on the wire (the echo's decoded ms proved unreliable; this is the send-side truth). Gated
            // behind MissionDebug ('missiondbg on') so it costs nothing in normal play.
            _move = new Movement();
            _move.Sent += m =>
            {
                if (_config.MissionDebug && _overland != null && _overland.Active)
                    Log($"MOVEDBG-OUT: mt={(byte)m.MoveType} ({m.Position.X:0.00},{m.Position.Y:0.00},{m.Position.Z:0.00}) +{m.DeltaTime}ms");
            };
            _follow = new FollowController(_ctx, _move);
            _combat = new CombatController(_ctx);
            _pets = new PetController(_ctx, pluginDir, _owner);
            _travel = new TravelController(_ctx, _move);
            _support = new SupportController(_ctx, _move, pluginDir);
            _resupply = new ResupplyController(_ctx, _move, pluginDir);
            _know = new KnowledgeReports(_ctx, _support, _owner);
            _paths = new PathStore(_pathsDir, Log);
            _capture = new WireCapture(_config, pluginDir, Log);
            _nav = new NavController(_ctx, pluginDir);
            _mission = new MissionController(_ctx, _move, pluginDir,
                _ctx.TellOwner);
            _overland = new OverlandController(_ctx, _move, pluginDir,
                _ctx.TellOwner);

            _hunt = new HuntController(_ctx, () => _ctx.Status.InMission);
            _chewy = new ChewyBuffController(_ctx, _support, _overland, pluginDir,
                _ctx.TellOwner);
            Client.ChestFullUpdateRaw += raw => { try { _mission.OnChestRaw(raw); } catch { } };
            _roll = new MissionRoll(_ctx);
            // No condition lambdas anymore (R2.2): MissionRun reads ctx.Status, and its fight-or-run
            // POLICY lives in its own file (FightOrRun) instead of a closure over Main's privates.
            _run = new MissionRun(_ctx, _roll, _mission, _overland, _follow, pluginDir,
                _ctx.TellOwner,
                _combat);
            _run.Resupply = _resupply;
            BuildCommands();

            Log($"=== Init owner='{_config.Owner}' mode={_mode} ===");

            // Local control API for the aobuddy MCP server (127.0.0.1 only; BotApiPort 0 turns it off).
            // The handler only ENQUEUES: HandleCommand mutates controller state, so it must run on the
            // update thread (drained at the top of OnUpdate), not on the API listener thread.
            _api = new BotApi(_config.BotApiPort, _ctx.Clock, Log, ApiStatus, (text, reply) => _apiCommands.Enqueue((text, reply)));
            _api.Start();
            Logger.Information($"AOBuddy::Init owner='{_config.Owner}' mode={_mode}");

            Client.OnUpdate += OnUpdate;

            // ---- Message wiring (R3.6): a flat list, one line per feed, each handler isolated by
            // SafeSubscribe — a throwing feed is caught and logged with its tag and every other
            // feed still runs for that message (the old second handler's one outer catch silently
            // swallowed instead). The tags are the old log-line prefixes verbatim, so log greps
            // keep matching; DCMOVE/ZONEIN/WIRECAPTURE are new tags for feeds that used to be
            // silent. Subscription order = run order per message.
            ClientEvents.SafeSubscribe(m => _ctx.Vitals.OnMessage(m), "VITALS feed error", Log);
            ClientEvents.SafeSubscribe(m => _resupply.OnMessage(m), "RESUPPLY feed error", Log);
            ClientEvents.SafeSubscribe(m => _roll.OnMessage(m), "MISSIONROLL", Log);
            ClientEvents.SafeSubscribe(m => _run.OnMessage(m), "MISSIONRUN", Log);
            ClientEvents.SafeSubscribe(m => _mission.OnMessage(m), "MISSION feed error", Log);
            ClientEvents.SafeSubscribe(m => OnServerMovedMe(m), "SERVER MOVE error", Log);
            // DIAG: count CharDCMove messages that actually deserialize+arrive, split owner vs self, to
            // prove whether the owner's movement reaches us per-move (smooth) or only in bursts (dropped);
            // feeds the tracker's keyframes and the stacked-on-him MIRROR.
            ClientEvents.SafeSubscribe(OnCharDCMove, "DCMOVE", Log);
            // The raw zone-in packet of the current playfield (a mission's room placements), for the
            // 'navdata' command's mission-layout fallback.
            ClientEvents.SafeSubscribe(OnZoneIn, "ZONEIN", Log);
            // Mission wire diagnostics ('missiondbg on'): MISSIONDBG lines + missions/*.bin captures.
            ClientEvents.SafeSubscribe(_capture.OnMessage, "WIRECAPTURE", Log);

            // Player trades: the owner handing us credits when we asked for them (see ResupplyController).
            Trade.TradeStatusChanged += (who, status) =>
            {
                try { _resupply.OnTradeStatus(who, status); }
                catch (Exception ex) { Log("RESUPPLY trade error: " + ex.Message); }
            };

            Client.Chat.PrivateMessageReceived += (s, msg) =>
            {
                // Tells from anyone else are never obeyed, but they are logged: Scotty answers warp requests by tell,
                // and those answers were invisible while travel waited on warps that never came (2026-09-23).
                if (!_owner.IsOwnerSender(msg.SenderName, msg.SenderId)) { Log($"TELL (not obeyed) from {msg.SenderName} (id={msg.SenderId}): {msg.Message}"); return; }
                Log($"CMD from {msg.SenderName}: '{msg.Message}'");
                try { HandleCommand(msg.Message, text => Client.SendPrivateMessage(msg.SenderId, text)); }
                catch (Exception ex) { Logger.Error($"command error: {ex.Message}"); Log($"COMMAND EXCEPTION: {ex}"); }
            };

            Team.TeamRequest += (s, e) =>
            {
                if (_config.AutoAcceptOwnerTeamInvite) { e.Accept(); Logger.Information("Accepted team invite."); }
            };

            // Watch the owner use objects (lifts, grid, whompas, mission door-buttons) so TRAVEL can
            // ride the same one when he zones off it.
            DynelManager.DynelUsed += OnDynelUsed;

            // The server SetPos-corrects our position. Apply it ONLY when stopped — see OnServerCorrectedMe.
            DynelManager.LocalPlayerCorrected += OnServerCorrectedMe;

            // Real death signal from the wire (CharacterAction Death) — not HP guessing. The SDK auto-sends
            // 'Die' ~5s later to reclaim us to the save terminal; we just stop everything and then recover.
            Client.Died += OnDeath;

            // Pet ownership, stated by the server rather than inferred from what is in view. A pet that
            // wanders out of broadcast range used to read as dead and start a resummon it could not land.
            // The server echoing our sit/stand back. It is what tells the rest cycle it is really seated, so a
            // sit-only recharger is pressed at the right moment instead of after a hopeful delay.
            Client.PostureToggled += id =>
            {
                LocalPlayer lp = DynelManager.LocalPlayer;
                if (lp != null && id.Instance == lp.Identity.Instance) _support.OnPostureToggled();
            };

            Client.PetAdded += id => _pets.OnPetAdded(id);
            Client.PetRemoved += id => _pets.OnPetRemoved(id);

            // Why the server refused something. Category 110 is its feedback channel; the id identifies the
            // message. Logged with whatever we last asked for, so a refusal stops being invisible - six
            // rechargers were once logged as "used" while the server rejected every one of them.
            Client.Feedback += (category, messageId) =>
            {
                if (category != 110) return;
                Log($"SERVER SAYS: feedback {category}/{messageId}"
                    + (_lastActionLabel != null ? $" — right after: {_lastActionLabel}" : ""));
            };
        }

        // Moves the server makes to our own body. The SDK applies none of them to the local player, so a grid lift beam
        // carried the bot up in everyone else's view while its own position stayed on the pad; every step after that
        // was sent from the wrong place and refused (log 2026-09-24 01:20). An in-playfield N3Teleport jumps us; a
        // FollowTarget NpcPath carries us along its waypoints: take the end of it and let overland travel wait out the
        // ride. While standing on a pad every message about us is logged, so whatever a beam really sends shows up.
        private readonly HashSet<string> _padSeen = new HashSet<string>();
        private void OnServerMovedMe(Message m)
        {
            LocalPlayer lp = DynelManager.LocalPlayer;
            if (lp == null || !(m?.Body is N3Message n3) || n3.Identity.Instance != lp.Identity.Instance) return;
            if (_overland.OnPad) { if (_padSeen.Add(n3.GetType().Name)) Log($"PAD: server sent {n3.GetType().Name} about me while I wait on the pad."); }
            else _padSeen.Clear();

            switch (m.Body)
            {
                case N3TeleportMessage t:
                    Log($"SERVER MOVE: N3Teleport to ({t.Destination.X:0},{t.Destination.Y:0},{t.Destination.Z:0}) pf {t.Playfield.Instance} (in {(int)Playfield.ModelId}), change {t.ChangePlayfield.Instance}.");
                    if (t.Playfield.Instance != (int)Playfield.ModelId) return;   // a zone change: the zone-in places us
                    _move.Reset();
                    Movement.SetPose(lp, t.Destination, t.Heading);
                    _overland.OnServerMoved(1.0);
                    break;
                case FollowTargetMessage ft:
                    if (!(ft.Info is FollowTargetMessage.PathInfo pi) || pi.Waypoints == null || pi.Waypoints.Length == 0)
                    {
                        Log($"SERVER MOVE: FollowTarget {ft.Type} (not a path), ignored.");
                        return;
                    }
                    float len = 0;
                    for (int i = 1; i < pi.Waypoints.Length; i++) len += Vector3.Distance(pi.Waypoints[i - 1], pi.Waypoints[i]);
                    Vector3 end = pi.End;
                    Log($"SERVER MOVE: FollowTarget path {string.Join(" ", pi.Waypoints.Select(w => $"({w.X:0},{w.Y:0},{w.Z:0})"))}, {len:0} m, mode {ft.MoveMode}; taking its end.");
                    _move.Reset();
                    Movement.SetPose(lp, end, lp.MovementComponent.Heading);
                    _overland.OnServerMoved(1.0 + len / 8.0);   // speed unknown: generous, the SetPos when we stop corrects the rest
                    break;
            }
        }

        // The CharDCMove feed (R3.6: was the second inline MessageReceived handler): owner keyframes
        // into the tracker, the stacked-on-him MIRROR, and the DIAG counters for the heartbeat.
        private void OnCharDCMove(Message m)
        {
            if (m == null || !(m.Body is CharDCMoveMessage cm)) return;
            if (cm.Identity.Instance == _owner.ChatId)
            {
                _diagOwnerMoves++;
                // Capture the owner's movement keyframe and derive his velocity for interpolation
                // (R3.1: the state and the math live in the tracker).
                _owner.OnKeyframe(cm);

                // MIRROR: stacked on him — his move is our move, sent the instant it arrives.
                if (_follow.MirrorLocked && Movement.IsMirrorable(cm.MoveType))
                {
                    LocalPlayer lp = DynelManager.LocalPlayer;
                    if (lp != null) _move.Mirror(lp, cm);
                }
            }
            else
            {
                LocalPlayer lp = DynelManager.LocalPlayer;
                if (lp != null && cm.Identity.Instance == lp.Identity.Instance)
                {
                    _diagSelfMoves++;
                }
            }
        }

        // Keep the raw zone-in packet of the current playfield (a mission's room placements) for the
        // 'navdata' command's mission-layout fallback. Deliberately NOT part of WireCapture: it runs
        // whether or not missiondbg is on, and Main's NavDataCommand is its only reader.
        private void OnZoneIn(Message m)
        {
            if (m != null && m.Body is PlayfieldAnarchyFMessage && m.RawPacket != null) _lastZoneInPacket = m.RawPacket;
        }

        // The last thing we asked the server to do, for pairing a refusal with its cause.
        private string _lastActionLabel;
        public void NoteAction(string what) => _lastActionLabel = what;

        // The server's SetPos is a position correction. The clientless controller OWNS its position (it
        // drives the body via SetPose along the owner's valid-ground breadcrumbs), so we must NOT snap it
        // back while it is actively moving — on any Y change (ramp, whompa exit, incline) the server sends
        // routine corrections, and applying each one snapped him back down the slope; with the near crumbs
        // already consumed he re-beelined up and got snapped again = the "locked into trying" rubber-band
        // ("any Y change breaks follow"). So: while MOVING, ignore the correction (the controller's path is
        // authoritative). Only when STOPPED/idle do we accept it, to re-sync a genuine reposition. This does
        // NOT affect zoning: a zone crossing arrives as a playfield teleport (big POSITION JUMP -> ClearNav),
        // never through this SetPos handler (verified: no large-gap corrections in the logs).
        private void OnServerCorrectedMe(object sender, Vector3 pos)
        {
            LocalPlayer me = DynelManager.LocalPlayer;
            if (me == null) return;
            _serverAnchor = pos;          // the server's confirmed position — the leash anchor
            _serverAnchorAge = 0;
            Vector3 local = me.MovementComponent.Position;
            float gap = Vector3.Distance(local, pos);

            // A mission blitz always takes the server's word (MissionController.OnServerCorrection).
            // Flat: a correction that only fixes our height is not a pull-back (22:06-22:11, 2026-09-24: at a
            // Stret West Bank mission door our walk put him 8 m up at the door's height, the server kept him at
            // y 0, and each 8 m 'pull' held him 15 s - 14 holds, 5 minutes at the door).
            _run.OnServerCorrection(Movement.Flat(local, pos));
            if (_mission.OnServerCorrection(me, pos)) { _follow.BreakMirror(); return; }
            // ...and so does overland travel: its route is planned on data that can miss a surface the server has.
            if (_overland.OnServerCorrection(me, pos)) { _follow.BreakMirror(); return; }

            // While MOVING, ignore SMALL corrections (ramp/Y jitter, a few metres) so we don't rubber-band
            // on slopes. But a correction that keeps GROWING means the server has genuinely rejected our
            // path and is holding us back — if we keep ignoring it the gap runs away (seen: 10->38m) until
            // we're so far behind server-side that we vanish from the owner's view. So once the gap passes
            // ResyncGap we APPLY it even while moving, capping the desync and snapping us back onto the
            // server's position. Stopped, we always apply. This keeps the ramp fix AND stops the vanish.
            if (_move.Moving && gap < ResyncGapMeters)
            {
                Log($"SETPOS IGNORED (moving): server ({pos.X:0},{pos.Y:0},{pos.Z:0}) vs local ({local.X:0},{local.Y:0},{local.Z:0}) gap={gap:0.0}m — controller drives its own path.");
                return;
            }

            bool wasMoving = _move.Moving;
            Movement.SetPose(me, pos, me.MovementComponent.Heading);   // accept the server's position
            _move.Reset();
            _follow.BreakMirror();   // replaying his packets from here would just repeat the rejected move
            Log($"SETPOS APPLIED ({(wasMoving ? "resync" : "stopped")}): snapped to server ({pos.X:0},{pos.Y:0},{pos.Z:0}) from ({local.X:0},{local.Y:0},{local.Z:0}) gap={gap:0.0}m.");

            // A big resync while sweeping a "zone line" means the server rejected the climb — he's blocked by
            // terrain (a ramp crest / wall), NOT on a zone line (which would teleport him, a POSITION JUMP).
            // Cancel the sweep so he holds instead of ramming up the ramp and rubber-banding back and forth.
            if (wasMoving && _follow.ZoneSweeping)
            {
                _follow.CancelZoneSweep();
                Log("ZONE SWEEP: cancelled by resync — blocked terrain (ramp/wall), not a zone line; holding.");
            }
        }

        // Real death from the wire. Stop everything; the SDK reclaims us ~5s later.
        private void OnDeath()
        {
            if (_dead) return;
            _dead = true;
            _deadSeconds = 0;
            LocalPlayer me = DynelManager.LocalPlayer;
            _deathPos = me?.MovementComponent.Position;
            Log($"DIED (server death signal) at ({_deathPos?.X ?? 0:0},{_deathPos?.Y ?? 0:0},{_deathPos?.Z ?? 0:0}) — stopping all activity; reclaim in ~5s.");
            // No StopAttack here: the server ends the fight on death by itself, and the SDK already sends
            // one from its own death handler. The standing rule is that while he has a mob to attack he
            // never issues it — only an explicit 'idle'/'stop' from the owner does.
            try { if (me != null) _move.Stop(me, _config.SendIntervalMs); } catch { }
            _resupply.Stop(me, "died");
            _mission.Stop("died");
            _overland.Stop("died");
            _hunt.Stop("died");
            _run.OnDied();
            ClearNav();
            _combat.Reset();
            _support.OnDeathResetBuffs();   // buffs drop on death — allow rebuff after reclaim
            // Told BY NAME so it reaches the owner wherever he is (a solo mission run is usually out of his sight).
            string where = $"{Playfield.Name} ({_deathPos?.X ?? 0:0},{_deathPos?.Z ?? 0:0})";
            string next = _run.Active ? "I'll reclaim, go back to the mission terminal, wait out rez sickness there and carry on."
                                      : "Reclaiming; I'll hold at the reclaim point - come to me or send 'come' when you're close.";
            _ctx.TellOwner($"I died in {where}. {next}");
        }

        // While dead: hold still and wait for the reclaim teleport, then recover and tell the owner where.
        private void HandleDeadState(LocalPlayer me, double dt)
        {
            _deadSeconds += dt;
            try { _move.Stop(me, _config.SendIntervalMs); } catch { }

            Vector3 pos = me.MovementComponent.Position;
            bool reclaimed = _deathPos.HasValue && Vector3.Distance(pos, _deathPos.Value) > _config.ZoneJumpThreshold;
            if (!reclaimed && _deadSeconds < 25.0)
                return;   // still waiting on the reclaim teleport (failsafe recovers after 25s regardless)

            _dead = false;
            _deathPos = null;
            _lastFramePos = pos;          // don't let the reclaim jump re-trigger the zone-jump reset
            ClearNav();
            _support.OnZone();            // re-grace buffs/rest after the reclaim (stats read stale a moment)
            _ctx.Vitals.Clear();
            Log($"RECLAIMED/alive at ({pos.X:0},{pos.Y:0},{pos.Z:0}) after {_deadSeconds:0}s dead — resuming.");
            PlayerChar owner = _owner.Find();
            if (owner != null)
                try { Client.SendPrivateMessage(owner.Identity.Instance, $"Back up at the reclaim point ({pos.X:0},{pos.Y:0},{pos.Z:0}). Waiting for you — send 'come' when close or walk to me."); } catch { }
        }

        // The owner used an object — hand it to TRAVEL, which arms a ride if he then zones off it.
        private void OnDynelUsed(Identity user, Identity target)
        {
            try
            {
                PlayerChar owner = _owner.Find();
                if (owner == null || user.Instance != owner.Identity.Instance) return;
                Vector3? pos = DynelManager.Find(target, out Dynel obj) ? (Vector3?)obj.Transform.Position : owner.Transform.Position;
                _travel.OnOwnerUsed(target, pos);
                _lastUsedObj = target; _lastUsedObjPos = pos; _lastUsedObjAge = 0;   // for NAV warp learning
            }
            catch (Exception ex) { Log("OnDynelUsed error: " + ex.Message); }
        }

        private void OnUpdate(object _, double dt)
        {
            // API commands run HERE, on the update thread — never on the API listener thread, which
            // would race the tick for the same controller state (R0.1). They arrive via the queue the
            // BotApi handshake enqueues into (see Init); replies still travel their own delegate.
            while (_apiCommands.TryDequeue(out (string Text, Action<string> Reply) cmd))
            {
                Log($"API CMD: '{cmd.Text}' (upd #{Environment.CurrentManagedThreadId})");
                try { HandleCommand(cmd.Text, cmd.Reply); }
                catch (Exception ex) { Log($"COMMAND EXCEPTION (api): {ex}"); try { cmd.Reply("error: " + ex.Message); } catch { } }
            }

            LocalPlayer me = DynelManager.LocalPlayer;
            if (me == null)
                return;

            try
            {
                // DEAD: do nothing but wait out the reclaim. The SDK sends 'Die' ~5s after death which
                // reclaims us to the save terminal; we detect that teleport and recover. No fighting,
                // stimming, buffing or following while dead (server ignores a corpse's packets anyway).
                if (_dead) { HandleDeadState(me, dt); return; }

                // Auto-detect trained perks from the wire and fold the permanent PERK/RESEARCH bonus
                // map into this LocalPlayer (re-applied when the instance is re-created on a full update).
                _perkBonuses.Tick(me);

                // Decide the stand-up ONCE, from the authoritative LOGIN movement mode. The stand-up
                // action (CharacterActionType.StandUp) is a sit/stand TOGGLE: firing it blind sits a
                // standing character (that froze the bot — logged in standing, blind toggle sat him, and a
                // seated char can't move so the server snapped every step to spawn). Stat 173 is set from
                // the login FullCharacter and NEVER updates afterward, so we CAN'T poll it to confirm we
                // stood — re-reading a stuck 8=Sit and re-toggling made him sit/stand in a loop. So: read
                // it once, send at most one stand-up if the login mode was seated, then never toggle again.
                if (!_stoodUp && me.TryGetStat(Stat.CurrentMovementMode, out int moveMode) && moveMode > 0)
                {
                    bool seated = moveMode == MoveModeSit || moveMode == MoveModeSleep || moveMode == MoveModeLounge;
                    if (seated)
                    {
                        me.MovementComponent.ChangeMovement(MovementAction.LeaveSit);  // one StandUp toggle
                        Log($"STAND: login mode={moveMode} (seated) — sent one stand-up.");
                    }
                    else
                    {
                        Log($"STAND: login mode={moveMode} (standing) — no stand-up needed.");
                    }
                    _stoodUp = true;   // decided from the login mode; do not toggle again (173 won't update)
                }

                // Player options. The client's XP on/off toggle sends CharacterAction 165 carrying the whole
                // option mask: 5 with XP on, 21 (0x15) with XP off (sniff marked-20260923-121514). Stat 349
                // (named AutoAttackFlags, default 5) held 5 in every earlier login, so it is the likely store.
                // Logged once so a restart shows whether XP-off survived into the clientless login.
                if (!_optionsLogged && me.TryGetStat(Stat.AutoAttackFlags, out int opts))
                {
                    Log($"OPTIONS: stat 349 = {opts} (0x{opts:X}), bit 0x10 {((opts & 0x10) != 0 ? "SET (XP off if 349 is the option store)" : "clear")}");
                    _optionsLogged = true;
                }

                // DING watch. Level is now kept live by the SDK's NewLevel handler; when it climbs, tell the
                // owner BY NAME (reaches him even out of view). Baseline silently on the first read so we
                // never announce the login level.
                if (me.TryGetStat(Stat.Level, out int lvlNow) && lvlNow > 0)
                {
                    if (_lastLevel > 0 && lvlNow > _lastLevel)
                    {
                        Log($"LEVEL UP: {_lastLevel} -> {lvlNow}");
                        _ctx.TellOwner($"Ding! I hit level {lvlNow}.");
                    }
                    _lastLevel = lvlNow;
                }

                // A big position jump we didn't make = the server teleported/zoned us. Reset all nav, and
                // remember the PRE-jump spot (the real zone line) so a nav transition is logged there — and
                // only when a teleport actually happened (a bare playfield-id flip with no jump is a desync).
                _zoneCrossAge += dt;
                _lastUsedObjAge += dt;
                Vector3 curPos = me.MovementComponent.Position;
                if (_lastFramePos.HasValue && Vector3.Distance(curPos, _lastFramePos.Value) > _config.ZoneJumpThreshold)
                {
                    Log($"POSITION JUMP {Vector3.Distance(curPos, _lastFramePos.Value):0}m ({_lastFramePos.Value.X:0},{_lastFramePos.Value.Y:0},{_lastFramePos.Value.Z:0})->({curPos.X:0},{curPos.Y:0},{curPos.Z:0}) — treating as zone/teleport.");
                    _zoneCrossPos = _lastFramePos; _zoneCrossAge = 0;
                    // A correction from before the jump is in the OLD place's coordinates: leashing the first steps here
                    // to it dragged the bot back to mission coordinates in Borealis (2026-09-23 21:11:06).
                    _serverAnchor = null;
                    // If the owner USED an object just before this teleport, learn it as a WARP (floor button /
                    // lift) — stored in the CURRENT playfield's file before ClearNav, so same-floor buttons
                    // (which don't change the playfield id) are captured too.
                    if (_lastUsedObjAge < 5.0 && _lastUsedObjPos.HasValue && _lastUsedObj.HasValue)
                    {
                        _nav.RecordWarp(_lastUsedObjPos.Value, curPos, (int)Playfield.ModelId, (int)_lastUsedObj.Value.Type, _lastUsedObj.Value.Instance);
                        _lastUsedObjAge = 999;
                    }
                    ClearNav();
                }

                PlayerChar owner = _owner.Find();

                // NAV: keep the persistent per-playfield memory pointed at the current zone (updates on
                // zone-in, records the crossing), and autosave the file while it's dirty.
                _nav.SetPlayfield((int)Playfield.ModelId, Playfield.Name, _zoneCrossPos, _zoneCrossAge < 1.5);
                _nav.Tick(dt);

                if (owner != null && !_greeted)
                {
                    _greeted = true;
                    try { Client.SendPrivateMessage(owner.Identity.Instance, "AOBuddy online — send 'help' for commands."); } catch { }
                    Logger.Information("Greeted owner.");
                    Log($"=== CONFIG: owner={_config.Owner} mode={_config.DefaultMode} tick={_config.TickMs}ms send={_config.SendIntervalMs}ms | " +
                        $"follow(dist={_config.FollowDistance} speed={_config.FollowSpeed} arrive={_config.CrumbArrive} maxStep={_config.MaxStep} zoneJump={_config.ZoneJumpThreshold}) | " +
                        $"assist(max={_config.AssistMaxDistance} leash={_config.CombatLeashMeters} range={_config.AttackRange}) | " +
                        $"heal(ownerBelow={_config.HealOwnerBelowPercent}% selfBelow={_config.HealSelfBelowPercent}% interval={_config.HealIntervalSec}s stimRange={_config.StimOwnerRange}) | " +
                        $"stim(kw='{_config.StimKeyword}' name='{_config.StimItemName}') recharger(kw='{_config.RechargerKeyword}') | " +
                        $"nanos(heal={_config.HealNanoId} self={_config.SelfHealNanoId} team={_config.TeamHealNanoId} auras={_config.HealAuraNanoIds.Count}) ===");
                }

                // Owner-visibility bookkeeping: FOLLOW records his footsteps while he's visible and
                // clears a stale trail on a far reacquire; TRAVEL arms a ride when he vanishes right
                // after using an object. The tracker (R3.1) keeps Visible/LastPos/LostSeconds and the
                // id caches; Main runs the edge orchestration — reacquire hands FOLLOW a clean slate,
                // losing him hands TRAVEL its shot.
                _travel.Age(dt);
                bool wasVisible = _owner.Visible;
                bool ownerVisible = _owner.UpdateVisible(owner, dt);
                if (ownerVisible)
                {
                    if (!wasVisible)
                    {
                        _follow.OnReacquired(owner);
                        if (_navReplaying) { _follow.StopReplay(); _navReplaying = false; }   // back in view — drop the fallback route, follow live
                    }
                    _follow.RecordAt(_owner.PredictedPos(owner));   // interpolated owner position → smooth trail between sparse keyframes
                    if (_follow.Recording) _follow.RecordPathPoint(owner);

                    // NAV record: only while following cleanly (Assist, follow on, NOT combat/rest/sweep/
                    // travel) — so a break / go-retrieve-him episode never becomes part of a saved segment.
                    bool navClean = _mode == Mode.Assist && _config.Follow && !_combat.InCombat
                                    && !_support.Resting && !_follow.ZoneSweeping && !_travel.Active;
                    _nav.RecordOwner(owner.Transform.Position, navClean);
                }
                else if (wasVisible)
                {
                    // He just dropped out of view. Give TRAVEL its shot (rode an object?).
                    _travel.OnOwnerLost(_owner.LastPos);
                }

                // ZONE-ARRIVAL watch (R3.2): after a teleport/zone, if the owner never turns up next to us,
                // say so instead of standing silent in an empty zone. Overland travel / a mission run cross
                // zones on purpose — an empty arrival there is not a loss.
                _episode.Tick(me, ownerVisible, _overland.Active || _run.Active, dt);

                // CATCH-UP when the owner is out of view and the live breadcrumbs are used up. On ground we
                // have ALREADY recorded, walk the clean recorded route (it carries the real up/down ramp Y)
                // toward his last-seen spot — no guessing. Nav only supplies the route; FOLLOW's replay walker
                // moves the body, so this can't break follow/zone/combat.
                // (The AUTO zone-sweep ladder that used to live below — sweep a vanished owner's crossing
                // spot, give up after 3 tries / 75 s — was unreachable dead code: its gate read two fields
                // that were never assigned. Deleted in R3.2, not silently re-enabled; crossing a line is the
                // manual 'zone'/'forward' commands' job, or travel/overland on purpose. See RESTRUCTURE R3.2.)
                if (_navReplaying && !_follow.HasWork) _navReplaying = false;   // recorded route finished — re-evaluate

                // NAV CATCH-UP: when we're well behind the owner on ground we've ALREADY recorded, replay the
                // clean recorded run (dense, real up/down ramp Y) toward him — instead of beelining sparse live
                // crumbs up a ramp, which the server rejects and snaps us back down (the ramp rubberband). Only
                // engages when BOTH we and the owner are near the same recorded run (RouteToward enforces it),
                // so it can't send us the wrong way; off recorded ground it returns null and normal follow /
                // the zone-sweep handle it. Nav only supplies the points; FOLLOW's replay walker moves the body.
                bool navEligible = _config.NavUse && !_navReplaying && !_travel.Active && !_combat.InCombat && !_mission.Active && !_overland.Active && !_run.Active
                    && _config.Follow && _mode == Mode.Assist && !me.IsCasting && !_support.Resting && !_follow.ZoneSweeping;
                if (navEligible)
                {
                    Vector3? tgt = ownerVisible ? (Vector3?)owner.Transform.Position : _owner.LastPos;
                    // Defer to TRAVEL and zoning: when the owner just blinked out (rode a button / crossed a
                    // zone line), that's travel's/the sweep's job — don't fire nav until he's been genuinely
                    // lost a couple seconds. When he's VISIBLE but far, catch up immediately (the ramp case).
                    bool needCatchup = (ownerVisible && owner != null && me.DistanceFrom(owner) > _config.NavCatchupMeters)
                                       || (!ownerVisible && _owner.LostSeconds > 2.0);
                    if (needCatchup && tgt.HasValue)
                    {
                        List<Vector3> route = _nav.RouteToward(me.MovementComponent.Position, tgt.Value);
                        if (route != null && route.Count >= 2)
                        {
                            _follow.LoadReplay(route, false);   // no zone-push: a catch-up, not a line crossing
                            _navReplaying = true;
                            Log($"NAV: catch-up on recorded route ({route.Count} pts) toward ({tgt.Value.X:0},{tgt.Value.Y:0},{tgt.Value.Z:0}) dist={(owner != null ? me.DistanceFrom(owner) : 0):0}.");
                        }
                    }
                }

                // He's close and my queued path leads AWAY from where he now is — he walked back to me, so
                // stop instead of running PAST him to finish an old path. Guarded so it only triggers when
                // he's within FollowDistance AND the next point is farther from him than I already am; that
                // never interrupts a normal lead-follow (there the next crumb is toward him, not away).
                if (ownerVisible)
                {
                    float dOwner = me.DistanceFrom(owner);
                    Vector3? t = _follow.CurrentTarget();
                    if (dOwner <= Math.Max(_config.FollowDistance, 4f) && t.HasValue   // floor: stack mode has distance 0
                        && Vector3.Distance(t.Value, owner.Transform.Position) > dOwner + 0.5f)
                    {
                        _follow.ClearMovement();
                        _navReplaying = false;
                    }
                }

                // RECOVERY BEFORE WANDERING. If the bot needs HP/nano and isn't fighting, healing wins — it
                // does NOT wander after a lost owner (a manual zone-sweep / catch-up). Cancel any sweep
                // already running so he sits and recovers instead of pacing back and forth. Owner-independent
                // by design.
                bool needRecovery = !_combat.InCombat && _support.NeedsRecovery(me);
                if (needRecovery && _follow.ZoneSweeping) _follow.CancelZoneSweep();

                _support.UpdateVitals(me, dt, _combat.InCombat);
                _ctx.Vitals.Poll(me, owner);
                // Keep pets up — summons go through the SHARED cast queue so they serialize with buffs (no
                // interruption), and only when nothing else is queued/casting.
                _pets.MaintainPets(me, dt, _support.HasPendingCasts, _support.QueueCast);

                // Feed the movement leash: anchor to the server's confirmed position, but ONLY while a
                // correction is fresh (server actively disagreeing). Stale = server happy = no leash, so
                // ordinary free movement is never clamped.
                _serverAnchorAge += dt;
                _move.SetLeash(_serverAnchorAge < _config.MoveLeashWindowSec ? _serverAnchor : (Vector3?)null, _config.MoveLeashMeters);

                _roll.Tick(dt);
                _chewy.StartupTick(me, dt);
                _chewy.Tick(me, dt, _combat.InCombat);
                RefreshStatus(me, owner);
                Walk(me, owner, dt);

                _decisionAccum += dt;
                if (_decisionAccum >= _config.TickMs / 1000.0)
                {
                    _decisionAccum = 0;
                    Decide(me, owner);
                }

                _lastFramePos = me.MovementComponent.Position;

                Heartbeat(me, owner, dt);
                _support.CheckSupplies(owner, dt);
            }
            catch (Exception ex) { Logger.Error($"update error: {ex.Message}"); Log($"UPDATE EXCEPTION: {ex}"); }
        }

        // ---- Cross-system status read-model (R2.2) --------------------------------
        // One refresh per tick, JUST BEFORE Walk — after Chewy has queued this frame's casts so
        // HasPendingCasts/SecondsSinceCast are current for the systems that yield to casting, and
        // before anything downstream (MissionRun ticks inside Walk) reads them. The values are the
        // same expressions the old MissionRun ctor lambdas evaluated lazily a few statements later.
        private void RefreshStatus(LocalPlayer me, PlayerChar owner)
        {
            BotStatus s = _ctx.Status;
            s.Dead = _dead;
            s.Resting = _support.Resting;
            s.HasPendingCasts = _support.HasPendingCasts;
            s.SecondsSinceCast = _support.SecondsSinceCast;
            s.InCombat = _combat.InCombat || _combat.HostilesEngaged(me, owner);
            s.NeedsRecovery = _support.NeedsRecovery(me);
            s.SelfHpPct = _support.SelfHpPct(me);
            s.Casting = me.IsCasting;
            s.InMission = _mission.InMission;
            s.OwnerVisible = owner != null;
            s.OwnerDistance = owner != null ? me.DistanceFrom(owner) : 0f;
        }

        // ---- MOVE: exactly one system moves the body, every single frame --------
        // FOLLOW is the default and runs unless one of exactly three things is true, and only these three
        // are genuinely exclusive with walking:
        //   * a nano cast is in flight  — the server roots you for the cast; moving would abort it
        //   * we are sitting to rest    — out of combat only, and only while the owner is settled nearby
        //   * use-object travel         — travel is walking us to a lift/terminal to ride it
        // Everything else the bot does — swinging, weapon specials, using a stim, commanding pets — is a
        // packet, not a posture, and none of it stops the legs. That is the point: "he must always be next
        // to the player" means follow is first in the frame, not last behind a ladder of actions.
        private void Walk(LocalPlayer me, PlayerChar owner, double dt)
        {
            if (me.IsCasting) { _follow.BreakMirror(); _move.Stop(me, _config.SendIntervalMs); return; }
            if (_support.Resting) { _follow.BreakMirror(); _move.Stop(me, _config.SendIntervalMs); return; }
            if (_resupply.Tick(me, dt)) { _follow.BreakMirror(); return; }
            // MISSION RUN (solo loop, off unless the owner started it): drives travel and blitz below; walks the
            // bot itself only into a mission door (follow's manual walker).
            bool runWalks = _run.Tick(me, dt);
            // MISSION blitz (off unless the owner started it): owns the body until it is done or stopped.
            if (_mission.Tick(me, dt)) { _follow.BreakMirror(); _missionWasActive = true; return; }
            if (_missionWasActive) { _missionWasActive = false; _follow.ClearMovement(); }   // hand back to follow clean
            // OVERLAND travel ('travelto', off unless the owner started it): owns the body until it arrives or stops.
            if (_overland.Tick(me, dt)) { _follow.BreakMirror(); _overlandWasActive = true; return; }
            if (_overlandWasActive) { _overlandWasActive = false; _follow.ClearMovement(); }
            if (_run.Active)
            {
                _follow.BreakMirror();
                if (runWalks && (_follow.ManualActive || _follow.ReplayCount > 0)) _follow.WalkTick(me, null, dt);   // its manual/replay walker only - never the owner-lost chase
                else _move.Stop(me, _config.SendIntervalMs);
                return;   // on its own: no following, no owner-lost chasing
            }
            if (_travel.Tick(me, dt, owner != null, _owner.LostSeconds)) { _follow.BreakMirror(); return; }
            _follow.WalkTick(me, owner, dt);
        }

        // ---- Decision ladder (throttled): cast queue > survival heal > fight > rest > follow/idle --
        private void Decide(LocalPlayer me, PlayerChar owner)
        {
            _support.UpdateOwnerSpeed(owner);

            // A mission blitz is selecting its target and waiting for the completion: no cast, stim, combat
            // retarget or pet command until it is done - each of them sends a LookAt of its own.
            if (_mission.HoldsSelection) { _ctx.SetBehavior("Mission: holding the selection"); return; }

            // Shopping holds everything else: no casting, sitting, fighting or following until it's done.
            if (_resupply.Active) { _support.SetIdleState(me); _ctx.SetBehavior("Resupplying"); return; }

            // Queued casts (auto/manual buffs, heals) first, when free and not mid manual-move. This also
            // drives the nano refill (sit/stim) when we can't afford the next buff, so it holds the tick.
            if (_support.TryDrainCast(me, _follow.ManualActive, _combat.InCombat)) return;
            if (me.IsCasting) return;

            // Keep healing-aura / HoT buffs running.
            _support.KeepAuras(me);

            // Her own weapon reach (for the heartbeat log; the server enforces the real reach).
            _effAttackRange = _config.AttackRange;
            if (me.TryGetStat(Stat.AttackRange, out int rawRange) && rawRange > 0)
                _effAttackRange = Math.Max(rawRange / 100f, 1.5f);

            if (_mode == Mode.Solo) { _support.SetIdleState(me); _combat.DoSolo(me); _ctx.SetBehavior("Solo"); return; }
            if (_mode == Mode.Idle) { _support.SetIdleState(me); _combat.Reset(); if (me.IsAttacking) me.StopAttack(); _ctx.SetBehavior("Idle"); return; }

            _support.AdvanceClocks();

            // ---- ACT --------------------------------------------------------------------------------
            // These steps are a SEQUENCE, not a ladder: each one runs and none of them returns out of the
            // tick. The old version returned after the first rung that did anything, so a queued cast or a
            // stim meant no target selection and no pet command that tick — which is what "he pauses combat
            // to heal" actually was. Nothing here stops the legs either; MOVE already ran this frame.

            // 1) WHO ARE WE FIGHTING. COMBAT takes the owner's target and issues Attack once for it.
            SimpleChar target = _combat.SelectAndEngage(me, owner, _run.Attacker(me));
            bool fighting = target != null;

            // HUNT (off unless the owner started it): with no fight of the owner's, the PETS go after the
            // nearest huntable mob around the bot. The bot itself neither moves nor swings for it.
            SimpleChar petHunt = null;
            if (!fighting && _hunt.Active && !_mission.Active && !_overland.Active && !_run.Active)
                petHunt = _hunt.Tick(me, owner, _config.TickMs / 1000.0);

            // 2) PETS ATTACK THE SAME MOB, at the same moment he does — once per target, and only the
            //    attack/mezz pets, so the heal pet is left healing.
            bool retargeted = false;
            if (fighting) retargeted |= _pets.EngageTarget(me, target, _config.TickMs / 1000.0);
            else if (petHunt != null) _pets.EngageTarget(me, petHunt, _config.TickMs / 1000.0);

            // 3) HEAL — stims work in combat and do NOT interrupt it: Item.Use is a GenericCmd, it never
            //    sets IsCasting, so the bot keeps swinging and keeps walking through it. Himself first.
            bool healed = _support.TryEmergencyHeal(me, owner, fighting);
            retargeted |= healed;

            // 4) HEAL PET keeps the right ally alive — the master himself when he is melee, the attack pet
            //    when he is ranged, read from the equipped weapon's reach. Issued once per summon.
            retargeted |= _pets.MaintainHealPet(me, fighting ? target : petHunt, _config.TickMs / 1000.0);

            // 5) PUT THE TARGET BACK. Every action above can move the bot's target: a stim retargets to the
            //    recipient, a pet command retargets to what the pet must act on. Targeting a friendly does
            //    not stop the swing, but LEAVING the target off the mob does — so restore it here, in one
            //    place, for all of them. SetTarget only, never a re-Attack: that would reset the swing timer.
            if (retargeted && fighting && target != null) Targeting.SetTarget(target.Identity);

            // Call the pets back in. A pet left where its fight ended keeps pulling mobs, and a pet in
            // combat holds US in combat - which suppresses health and nano regen and makes the server
            // refuse heal items. One straggler can stop the bot recovering entirely.
            _pets.RecallStragglers(me, fighting || petHunt != null);   // a hunting pet is far away on purpose

            if (fighting)
            {
                _support.OnFight(me);
                _ctx.SetBehavior(healed ? "Fighting (healing)" : "Fighting");
                return;   // the out-of-combat work below (buffs, rest) has no business running mid-fight
            }
            _combat.Disengage(me, owner);
            if (healed) { _ctx.SetBehavior("Healing"); return; }

            // 3) AUTO-BUFF — keep learned buffs up on self/owner/team (out of combat). Only ENQUEUES; the
            // cast + nano refill happen via TryDrainCast at the top of the next tick.
            _support.KeepBuffs(me, owner, _combat.InCombat);

            // 4) REST — between fights, SIT and recharger-recover BOTH HP and nano (if either is low). combatLull
            // only blocks STARTING a rest (don't sit mid-pull); an in-progress rest rides out a flapping lull
            // instead of being yanked to a stand each tick. (While actively buffing, the recharge-for-a-cast is
            // handled inside TryDrainCast; stims stay combat HP heals.)
            bool combatLull = _combat.SinceCombat < _config.CombatRestCooldownSec || _combat.HostilesEngaged(me, owner);
            // Never sit while running from a pack (03:10, 2026-09-25, The Longest Road: fleeing at 28% with three
            // mobs behind him, the chasers set aside and HP rising from a stim, he sat to recharge and died).
            if (_support.RestTick(me, owner, _combat.InCombat || _run.Fleeing, combatLull)) return;

            // 4) FOLLOW / IDLE.
            _support.SetIdleState(me);
            bool active = (owner != null && me.DistanceFrom(owner) > Math.Max(_config.FollowDistance, 0.5f)) || _follow.HasWork;
            _ctx.SetBehavior(active ? "Following" : "Idle");
        }

        // Reset all navigation state on a detected zone/teleport so no system chases old coordinates.
        private void ClearNav()
        {
            _follow.Reset();
            _travel.Reset();
            _resupply.OnZone();
            _combat.Reset();
            _pets.Reset();          // zoning drops pet tasking server-side — re-issue attack/heal after the zone
            _support.OnZone();
            _ctx.Vitals.Clear();    // readings from the old playfield say nothing about anyone here
            _move.Reset();
            _owner.ResetOnZone();
            _episode.ResetOnZone();  // start the "did he follow me here?" clock
            _navReplaying = false;
            Logger.Information("Zone/teleport detected — navigation reset.");
        }

        // Once-a-second diagnostic line: what she's doing, vitals, owner state, and the movement/
        // combat flags, so a problem is readable from the log without guessing.
        /// <summary>A percentage for the log: "?" when we have no reading, so a missing stat is visible
        /// as missing instead of being printed as a healthy-looking number.</summary>
        private static string Pct(int pct) => pct == SupportController.Unknown ? "?" : pct + "%";

        private void Heartbeat(LocalPlayer me, PlayerChar owner, double dt)
        {
            int hp = _support.SelfHpPct(me);
            // Real death is handled by Client.Died (OnDeath); HP reads are unreliable when the owner is out
            // of view, so we only note big drops as a diagnostic, never infer death from them. An unknown
            // reading is not a drop.
            if (_lastHpPct >= 0 && hp != SupportController.Unknown && _lastHpPct - hp >= 25)
                Log($"HP DROP {_lastHpPct}%->{hp}% (owner {(owner == null ? "not visible" : "visible")}).");
            _lastHpPct = hp;

            _hbAccum += dt;
            if (_hbAccum < 1.0) return;
            _hbAccum = 0;

            Vector3 p = me.MovementComponent.Position;
            string od = owner != null ? me.DistanceFrom(owner).ToString("0.0") : "n/a";
            string ohp = _ctx.Vitals.Describe(owner);
            Log($"hb [{_ctx.Behavior}] mode={_mode} hp={Pct(hp)} nano={Pct(_support.SelfNanoPct(me))} ohp={ohp} pos=({p.X:0},{p.Y:0},{p.Z:0}) owner={(owner == null ? "LOST" : "ok")} dist={od} ospd={_support.OwnerSpeed:0.0} " +
                $"wp={_follow.TrailCount} replay={_follow.ReplayCount} zc={_follow.ZoneCrossing} combat={_combat.InCombat} rest={_support.Resting} sit={_support.Sitting} rng={_effAttackRange:0.0} runspd={(me.TryGetStat(Stat.RunSpeed, out int _rs) ? _rs : -1)} movemode={(me.TryGetStat(Stat.CurrentMovementMode, out int _mm) ? _mm : -1)} moving={_move.Moving} leash={_move.Leashed} rez={SupportController.IsRezSick(me)} atk={me.IsAttacking} dcmove(own={_diagOwnerMoves}/self={_diagSelfMoves}) | walk[{_ctx.WalkState}]");
            _diagOwnerMoves = 0; _diagSelfMoves = 0;
        }

        private void Log(string line)
        {
            try { File.AppendAllText(_logFile, $"{DateTime.Now:HH:mm:ss.fff} {line}{Environment.NewLine}"); } catch { }
        }

        // ---- Commands ------------------------------------------------------------

        private BotApi _api;
        // Commands from the local control API, waiting to run on the update thread (R0.1).
        private readonly System.Collections.Concurrent.ConcurrentQueue<(string Text, Action<string> Reply)> _apiCommands
            = new System.Collections.Concurrent.ConcurrentQueue<(string, Action<string>)>();

        // What the MCP server's bot_status returns: the heartbeat line plus the facts worth reading at a glance.
        private Newtonsoft.Json.Linq.JObject ApiStatus()
        {
            var o = new Newtonsoft.Json.Linq.JObject();
            try
            {
                var me = DynelManager.LocalPlayer;
                o["heartbeat"] = StatusLine();
                o["missionRun"] = _run.Status();
                o["playfield"] = (int)Playfield.ModelId;
                o["zone"] = Playfield.TryGetPlayfieldNameFromId((int)Playfield.ModelId, out string zn) ? zn : Playfield.ModelId.ToString();
                if (me != null)
                {
                    var p = me.Transform.Position;
                    o["position"] = $"{p.X:0.0} {p.Y:0.0} {p.Z:0.0}";
                    o["hpPct"] = _support.SelfHpPct(me);
                    if (me.TryGetStat(Stat.Cash, out int cash)) o["credits"] = cash;
                    if (me.TryGetStat(Stat.Level, out int lvl)) o["level"] = lvl;
                }
                o["freeSlots"] = Inventory.NumFreeSlots;
                o["dead"] = _dead;
            }
            catch (Exception ex) { o["error"] = ex.Message; }
            return o;
        }

        private void HandleCommand(string message, Action<string> reply)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            string[] parts = message.Trim().TrimStart('!').Split(' ');
            string cmd = parts[0].ToLowerInvariant();
            if (_commands != null && _commands.TryGetValue(cmd, out var handler)) handler(reply, parts);
            else reply($"Unknown command '{cmd}'. Try 'help'.");
        }

        // ---- Command table (R3.4) -------------------------------------------------
        // Every command the owner can send, as word -> handler(reply, parts), built once at Init.
        // The systems own their commands (PetController, ResupplyController, KnowledgeReports,
        // _hunt/_roll/_overland/_run/_chewy, HelpPages); what stays here are the entries wired over
        // MAIN-owned state (mode, follow toggles, nav record, the zone-line walker) — small closures
        // over Main, the win is locality not purity. Bodies and replies are verbatim from the old
        // switch; Arg() is the old switch's lowercased parts[1].
        private Dictionary<string, Action<Action<string>, string[]>> _commands;

        private void BuildCommands()
        {
            string Arg(string[] p) => p.Length > 1 ? p[1].ToLowerInvariant() : "";
            var t = new Dictionary<string, Action<Action<string>, string[]>>();

            // -- mode / follow / the body --
            t["assist"] = (reply, p) => { _mode = Mode.Assist; reply("Mode: Assist."); };
            t["hunt"] = (reply, p) =>
            {
                if (_mode != Mode.Assist && !(p.Length > 1 && (Arg(p) == "off" || Arg(p) == "status"))) { _mode = Mode.Assist; }
                _hunt.Command(p.Length > 1 ? p[1] : "", reply);
            };
            t["solo"] = (reply, p) => { _mode = Mode.Solo; reply("Mode: Solo."); };
            t["stop"] = t["idle"] = (reply, p) =>
            {
                _mode = Mode.Idle; _hunt.Stop("stop command"); _run.Stop("stop command"); _follow.ClearMovement(); _combat.Reset();
                _resupply.Stop(DynelManager.LocalPlayer, "stop command");
                _overland.Stop("stop command");
                { LocalPlayer lp = DynelManager.LocalPlayer; if (lp != null) { _move.Stop(lp, _config.SendIntervalMs); if (lp.IsAttacking) lp.StopAttack(); } }
                reply("Mode: Idle. Standing down.");
            };
            t["follow"] = (reply, p) =>
            {
                _config.Follow = true;
                if (_mode == Mode.Idle) _mode = Mode.Assist;
                reply($"Following on (mode {_mode}).");
            };
            t["stay"] = (reply, p) => { _config.Follow = false; reply("Staying put (follow off)."); };
            t["come"] = (reply, p) =>
            {
                PlayerChar o = _owner.Find();
                if (o != null) { _follow.SetManualTarget(o.Transform.Position); reply("On my way."); }
                else reply("Can't see you (out of range?).");
            };
            // 'zone' used to reset the auto-sweep attempt budget before sharing this body; that ladder
            // was unreachable dead code (R3.2) and WorkTheZoneLine never read the budget — all three
            // are the same ask now.
            t["forward"] = t["run"] = t["zone"] = (reply, p) =>
            {
                LocalPlayer lp = DynelManager.LocalPlayer;
                if (lp == null) return;
                WorkTheZoneLine(lp, reply);
            };
            t["stand"] = (reply, p) => { DynelManager.LocalPlayer?.MovementComponent.ChangeMovement(MovementAction.LeaveSit); reply("Standing up."); };
            t["sit"] = (reply, p) => { DynelManager.LocalPlayer?.MovementComponent.ChangeMovement(MovementAction.SwitchToSit); reply("Sitting down."); };
            t["specials"] = (reply, p) =>
            {
                _config.UseSpecials = !_config.UseSpecials;
                var meS = DynelManager.LocalPlayer;
                string known = meS != null && meS.KnownSpecials.Count > 0 ? string.Join(", ", meS.KnownSpecials) : "none learned yet";
                reply($"Special attacks {(_config.UseSpecials ? "ON" : "OFF")}. Known: {known}.");
            };
            t["weapon"] = t["weapons"] = (reply, p) =>
            {
                int n = _combat.DumpWeapons(DynelManager.LocalPlayer);
                reply($"Dumped {n} equipped weapon(s) to aobuddy.log (WEAPON: lines).");
            };
            t["missiondbg"] = (reply, p) => { _config.MissionDebug = !_config.MissionDebug; reply($"Mission debug {(_config.MissionDebug ? "ON" : "OFF")} (logs to aobuddy.log)."); };
            t["nanodump"] = (reply, p) =>
            {
                LocalPlayer mn = DynelManager.LocalPlayer;
                int n = mn != null ? _support.DumpNanos(mn) : 0;
                reply($"Dumped {n} nanos to aobuddy.log.");
            };
            t["catalog"] = (reply, p) =>
            {
                string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_logFile) ?? ".", "catalog");
                var r = NanoCatalog.ExportAll(dir, Log);
                reply(r.Item1 >= 0 ? $"Exported {r.Item1} nanos to catalog/ (all + per-class json)." : $"Catalog failed: {r.Item2}");
            };
            t["buff"] = (reply, p) => QueueBuffs(Arg(p), reply);
            t["heal"] = (reply, p) =>
            {
                PlayerChar ht = _owner.Find();
                if (_config.HealNanoId > 0 && ht != null) { _support.QueueCast(new CastRequest { Target = ht.Identity, NanoId = _config.HealNanoId, Label = "heal owner" }); reply("Queued a heal."); }
                else reply(ht == null ? "Can't see you (out of range?)." : "No heal nano configured (HealNanoId).");
            };

            // -- pets (PetController owns them, R3.4) --
            foreach (string w in new[] { "pets", "petdbg", "resummon", "petbuffs", "petfollow",
                                         "petkill", "petterminate", "petdismiss", "petsummon", "petresummon",
                                         "pethealme", "petheal", "pethealself", "pethealme2", "pethealpet",
                                         "pethealauto", "pethealtarget", "pethealmytarget", "petstatus", "petattack" })
                t[w] = (reply, p) => _pets.Command(p, reply);

            // -- recorded paths (FOLLOW walks them, PathStore keeps the files) --
            t["record"] = (reply, p) =>
            {
                string arg = Arg(p);
                if (arg == "stop") { _follow.StopRecording(); reply($"Recorded {_follow.RecordCount} points. Save with 'savepath <name>'."); }
                else { _follow.StartRecording(); reply("Recording your path — walk the route, then 'record stop'."); }
            };
            t["savepath"] = t["save"] = (reply, p) =>
            {
                string arg = Arg(p);
                if (string.IsNullOrEmpty(arg)) { reply("Usage: savepath <name>"); return; }
                if (_follow.RecordCount == 0) { reply("Nothing recorded. Use 'record' first."); return; }
                try { if (_paths.Save(arg, _follow.RecordBuffer)) reply($"Saved '{arg}' ({_follow.RecordCount} points)."); else reply("Save failed (see log)."); }
                catch (Exception ex) { reply("Save failed: " + ex.Message); }
            };
            t["path"] = (reply, p) =>
            {
                string arg = Arg(p);
                if (arg == "stop") { _follow.StopReplay(); reply("Path playback stopped."); return; }
                if (string.IsNullOrEmpty(arg)) { reply("Usage: path <name> | path stop"); return; }
                try
                {
                    List<Vector3> pts = _paths.Load(arg);
                    _follow.LoadReplay(pts);
                    reply($"Replaying '{arg}' ({pts.Count} points).");
                }
                catch (Exception ex) { reply("Load failed: " + ex.Message); }
            };
            t["paths"] = (reply, p) => reply("Saved paths: " + _paths.List());

            // -- nav / diagnostics / status --
            t["nav"] = (reply, p) =>
            {
                switch (Arg(p))
                {
                    case "save": _nav.Save(); reply($"Nav saved (pf {_nav.PlayfieldId})."); break;
                    case "on": _config.NavRecord = true; reply("Nav recording ON."); break;
                    case "off": _config.NavRecord = false; reply("Nav recording OFF."); break;
                    case "use": _config.NavUse = !_config.NavUse; reply($"Nav lost-fallback {(_config.NavUse ? "ON" : "OFF")}."); break;
                    default: reply(_nav.Status()); break;
                }
            };
            t["status"] = (reply, p) => reply(StatusLine());
            t["pos"] = (reply, p) =>
            {
                LocalPlayer meP = DynelManager.LocalPlayer;
                if (meP == null) { reply("Not in game."); return; }
                Vector3 v = meP.Transform.Position;
                // Map coordinates as the game shows them (x, then z as the map's y), height after - the same
                // order 'travelto <x> <y> <playfield>' takes.
                reply($"{Playfield.Name} ({(int)Playfield.ModelId}): {v.X:0} {v.Z:0}, height {v.Y:0}");
            };
            t["navdata"] = (reply, p) => reply(NavDataCommand(Arg(p)));
            t["mission"] = (reply, p) =>
            {
                string arg = Arg(p);
                if (arg == "run") { if (_mode != Mode.Assist) _mode = Mode.Assist; _run.Command(p.Length > 2 ? string.Join(" ", p.Skip(2)) : "", reply); }   // all of it: "style fight" was cut to "style"
                else if (!_roll.Command(arg, p.Length > 2 ? p[2] : "", reply))
                    _mission.Command(p.Length > 1 ? p[1] : "", reply);
            };
            t["travelto"] = (reply, p) => _overland.Command(p.Skip(1).Where(x => x.Length > 0).ToArray(), reply);
            t["stat"] = (reply, p) => reply(KnowledgeReports.StatCommand(p.Length > 1 ? p[1] : ""));

            // -- knowledge / reports (KnowledgeReports owns them, R3.4) --
            t["class"] = t["whoami"] = (reply, p) => reply(_know.ClassLine());
            t["nanos"] = (reply, p) => _know.ReportNanos(Arg(p), reply);
            t["active"] = (reply, p) => _know.ReportActive(reply);
            t["buffs"] = (reply, p) =>
            {
                // Bare 'buffs' = the running buffs; 'buffs plan/ask/...' = the Chewy request planner.
                if (p.Length > 1 && ChewyBuffController.IsCommand(p[1]))
                {
                    LocalPlayer meC = DynelManager.LocalPlayer;
                    if (meC == null) { reply("Not in game."); return; }
                    _chewy.Command(meC, p.Skip(1).ToArray(), reply);
                }
                else _know.ReportActive(reply);
            };
            t["autobuff"] = t["keepup"] = (reply, p) => _know.ReportBuffPlans(reply);
            t["supplies"] = t["stims"] = (reply, p) => _know.ReportSupplies(reply);
            t["learnable"] = t["learn"] = (reply, p) => _know.ReportLearnable(Arg(p), reply);
            t["perks"] = t["perk"] = (reply, p) => _perkBonuses.Report(reply);

            // -- shopping (ResupplyController owns them, R3.4) --
            t["resupply"] = (reply, p) => _resupply.Command(p, reply);
            t["vendordebug"] = (reply, p) => _resupply.Survey(p, reply);
            t["shop"] = t["sell"] = t["buy"] = (reply, p) => reply("Only 'resupply' (stims and rechargers) so far; general vendor buy/sell isn't implemented yet.");
            t["whompa"] = t["travel"] = (reply, p) => reply("Use 'travelto <x> <y> <playfield>' (or 'travelto <playfield>'); it plans the zone lines, whompas and Scotty warps.");
            t["help"] = t["commands"] = (reply, p) => reply(HelpPages.For(Arg(p)));

            _commands = t;
        }

        // FORWARD/RUN and ZONE share this body (R0.5): head for a RECORDED zone line — the spot we came
        // in through (the entry point) or where the owner crossed out (a transition), aimed at his
        // last-seen spot so we pick the line he actually used — on our recorded route, and cross it.
        // Do NOT sweep along the bot's stale facing (that once pointed him away from the line and he
        // ran the wrong way). On/near the line but unable to route to it, sweep ACROSS it, aimed at the
        // line. With nothing recorded, sweep along the owner's TRAVEL direction (the same source the
        // auto-sweep uses), and last resort only the bot's facing.
        private void WorkTheZoneLine(LocalPlayer p, Action<string> reply)
        {
            Vector3 mypos = p.MovementComponent.Position;
            Vector3? line = _nav.NearestTransition(_owner.LastPos ?? mypos) ?? _nav.EntryPoint();
            if (line.HasValue)
            {
                List<Vector3> route = _nav.RouteToward(mypos, line.Value);
                if (route != null && route.Count >= 2)
                {
                    _follow.LoadReplay(route, true);   // zone-push: walk the recorded route to the line and cross
                    _navReplaying = true;
                    reply($"Heading to the zone line at ({line.Value.X:0},{line.Value.Y:0},{line.Value.Z:0}).");
                    return;
                }
                // On/near the line but can't route to it — sweep across it, AIMED at the line.
                Vector3 toLine = line.Value - mypos;
                if (toLine.Length() > 0.5f)
                {
                    _follow.StartManualSweep(mypos, toLine);
                    reply($"Working the zone line at ({line.Value.X:0},{line.Value.Y:0},{line.Value.Z:0}).");
                    return;
                }
            }

            // No recorded line here: fall back to the owner's TRAVEL direction, then, last resort only,
            // the bot's facing.
            if (!_follow.StartZoneSweep(mypos))
            {
                Vector3 dir = (_owner.LastPos.HasValue && (_owner.LastPos.Value - mypos).Length() > 0.5f)
                                ? _owner.LastPos.Value - mypos
                                : p.MovementComponent.Heading.Forward;
                _follow.StartManualSweep(mypos, dir);
            }
            reply("Working the zone line (sweeping back and forth).");
        }

        private void QueueBuffs(string which, Action<string> reply)
        {
            if (which == "") which = "all";
            int queued = 0;
            if (which == "self" || which == "all")
                foreach (int id in _config.SelfBuffNanoIds) { _support.QueueCast(new CastRequest { OnSelf = true, NanoId = id, Label = "self buff" }); queued++; }
            if (which == "owner" || which == "all")
            {
                PlayerChar owner = _owner.Find();
                if (owner != null) foreach (int id in _config.OwnerBuffNanoIds) { _support.QueueCast(new CastRequest { Target = owner.Identity, NanoId = id, Label = "owner buff" }); queued++; }
            }
            if (which == "team" || which == "all")
                foreach (TeamMember member in Team.Members)
                    foreach (int id in _config.TeamBuffNanoIds) { _support.QueueCast(new CastRequest { Target = member.Identity, NanoId = id, Label = "team buff" }); queued++; }
            reply(queued > 0 ? $"Queued {queued} buff cast(s)." : "No matching buff nano ids configured.");
        }

        // ---- Knowledge: profession & nanos ---------------------------------------

        // ---- Helpers -------------------------------------------------------------

        private string StatusLine()
        {
            LocalPlayer me = DynelManager.LocalPlayer;
            string target = me?.IsAttacking == true && me.FightingTarget != null ? me.FightingTarget.Name : "none";
            int hpPct = me != null ? _support.SelfHpPct(me) : SupportController.Unknown;
            string hp = hpPct == SupportController.Unknown ? "?" : hpPct + "%";
            string lvl = (me != null && me.TryGetStat(Stat.Level, out int l) && l > 0) ? l.ToString() : "?";
            return $"Lvl: {lvl}. Mode: {_mode}. Follow: {_config.Follow}. HP: {hp}. Target: {target}. Waypoints: {_follow.TrailCount}.";
        }

        private static Mode ParseMode(string s)
        {
            switch ((s ?? "").ToLowerInvariant())
            {
                case "solo": return Mode.Solo;
                case "idle": return Mode.Idle;
                default: return Mode.Assist;
            }
        }

        // ---- Permanent stats: PERK & RESEARCH ------------------------------------

        // ---- navdata: read-only check of GameData/Nav against the live character (nothing uses it yet) ----
        //   navdata            floor data under the bot's own feet vs his real Y (in a mission: composed from the
        //                      zone-in packet's room placements and the template pool)
        //   navdata <x> <z>    the same for any point in the current playfield
        //   navdata verify     every point in nav/<pf>.json against the data (the acceptance test)
        //   navdata unload     drop the loaded data
        private string NavDataCommand(string arg)
        {
            int pf = (int)Playfield.ModelId;
            if (pf <= 0) return "Not in a playfield.";
            if (arg == "unload") { _navData = null; return "Nav data unloaded."; }
            try
            {
                if (_navData == null || _navData.Playfield != pf)
                {
                    double loadStart = _ctx.Clock.Milliseconds;
                    _navData = AOBuddyNav.Load(_pluginDir, pf);
                    if (_navData == null && _lastZoneInPacket != null)
                        _navData = AOBuddyNav.LoadMission(_pluginDir, _lastZoneInPacket);   // an instance: compose it from its pool
                    if (_navData == null) return $"No nav data folder for pf {pf} ({AOBuddyNav.FolderFor(_pluginDir, pf)}) and no mission layout in the zone-in packet.";
                    Log($"NAVDATA: loaded pf {pf} {_navData.Kind} ground={(_navData.Ground != null ? _navData.Ground.SamplesX + "x" + _navData.Ground.SamplesZ : "-")} rooms={(_navData.Dungeon != null ? _navData.Dungeon.Rooms.Count : 0)} collision={(_navData.Collision != null ? _navData.Collision.Triangles : 0)} tris in {(long)(_ctx.Clock.Milliseconds - loadStart)} ms");
                }
                if (arg == "verify") return _navData.SelfTest(Path.Combine(_pluginDir, "nav", pf + ".json"));
                LocalPlayer me = DynelManager.LocalPlayer;
                if (me == null) return "No local player.";
                Vector3 p = me.MovementComponent.Position;
                double x = p.X, y = p.Y, z = p.Z;
                var parts = (arg ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && double.TryParse(parts[0], out double ax) && double.TryParse(parts[1], out double az)) { x = ax; z = az; }
                string text = _navData.Explain(x, y, z);
                Log("NAVDATA: " + text);
                return text;
            }
            catch (Exception ex) { return "navdata failed: " + ex.Message; }
        }

        private void LoadConfig(string pluginDir)
        {
            string path = Path.Combine(pluginDir, "config.json");
            if (!File.Exists(path)) path = Path.Combine(pluginDir, "config.example.json");
            if (File.Exists(path))
            {
                try
                {
                    // A list in config.json REPLACES the C# defaults. Newtonsoft's default would POPULATE
                    // the existing default list (append), so a user's shorter list silently unioned with
                    // the built-ins and duplicates accumulated; Replace makes "what you write is the list".
                    var settings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };
                    _config = JsonConvert.DeserializeObject<BuddyConfig>(File.ReadAllText(path), settings) ?? new BuddyConfig();
                    Logger.Information($"Loaded config from {path}");
                }
                catch (Exception ex) { Logger.Error($"Failed to read {path}: {ex.Message}; using defaults."); }
            }
            else Logger.Warning($"No config.json in {pluginDir}; using defaults (owner unset).");
        }
    }
}
