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
        private MissionRoll _roll;
        private MissionRun _run;
        private bool _missionWasActive;
        private OverlandController _overland;
        private bool _overlandWasActive;

        private string _pathsDir;
        private string _pluginDir;
        private AOBuddyNav _navData;           // test-only reader for GameData/Nav, see the 'navdata' command
        private byte[] _lastZoneInPacket;      // raw PlayfieldAnarchyF of the current zone; carries a mission's room placements
        private string _logFile;

        // Tick / diagnostics state that belongs to Main's coordination, not to any one system.
        private double _hbAccum;             // heartbeat throttle
        private int _lastHpPct = -1;         // to catch death / big HP drops
        private float _effAttackRange = 8f;  // her own weapon's reach (for the heartbeat log)
        private double _decisionAccum;       // decision-tick throttle

        // Owner visibility bookkeeping (drives follow record/reacquire and travel arming).
        private Vector3? _lastFramePos;      // to detect server teleports/zones (big position jumps)
        private bool _ownerVisibleLast;
        private Vector3? _lastOwnerPos;
        private double _ownerLostSeconds;    // how long the owner has been continuously out of view
        private Vector3? _serverAnchor;      // the last position the server confirmed for us (from SetPos)
        private double _serverAnchorAge;     // seconds since that correction (leash only enforced while fresh)
        private Vector3? _zoneCrossPos;      // our position the instant before a real teleport (the zone line)
        private double _zoneCrossAge = 999;  // seconds since that teleport (a nav transition needs one to be recent)
        private int _diagOwnerMoves, _diagSelfMoves;   // DIAG: CharDCMove messages received per heartbeat (owner vs self)
        // Owner-interpolation state: his latest movement keyframe + derived velocity, so we can predict his
        // position between the server's sparse (~1/s) keyframes (see PredictOwnerPos).
        private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
        private Vector3? _ownerKeyPos;
        private double _ownerKeyTime;
        private Vector3 _ownerVel;
        private Quaternion _ownerKeyHeading;
        private bool _ownerMovingKey;
        private Identity? _lastUsedObj;      // the object the owner most recently USED (button/lift/terminal)
        private Vector3? _lastUsedObjPos;
        private double _lastUsedObjAge = 999; // seconds since that use (a warp needs a teleport right after)
        private bool _zoneSweepTried;        // one auto zone-sweep attempt per owner-loss episode (reset on reacquire)
        // ---- Zone crossing, per owner-loss episode ------------------------------
        // The owner leaving our sight is one of three quite different things, and the bot used to treat
        // them all the same and get one 16-second sweep to sort it out:
        //   * he rode a lift/terminal      -> TRAVEL rides it too (armed from the object he used)
        //   * he walked a zone line        -> we have to cross it ourselves, which is what this is for
        //   * he simply outran us on open ground -> nothing to cross; keep walking his queued waypoints
        // We can tell the third from the second because a zone line is something he walked THROUGH from
        // close by while moving, not something he vanished from at forty metres. Sweeping on open ground
        // is what made the bot pace back and forth in a field.
        private float _ownerLostDist;        // how far off he was at the moment he vanished
        private bool _ownerLostMoving;       // ...and whether he was actually travelling at the time
        private int _zoneAttempts;           // sweeps made this episode
        private double _zoneEpisodeElapsed;  // time since he vanished, for the overall give-up
        private bool _zoneGaveUp;            // told him we lost him; stop retrying until he is back
        private const float ZoneLossMeters = 30f;     // vanishing further off than this is range, not a line
        private const int ZoneMaxAttempts = 3;        // sweeps before we admit we cannot cross
        private const double ZoneEpisodeSeconds = 75; // ...and the wall-clock cap on trying

        // Counts up after a zone/teleport until the owner is seen. Crossing a line SEPARATELY from him is
        // normal for a second or two while he loads, but if he never appears we have crossed into somewhere
        // he is not — say so rather than standing in an empty zone waiting.
        private double _arrivedAlone;
        private const double ArrivedAloneSeconds = 12.0;
        private bool _navReplaying;          // we handed FOLLOW a recorded nav route (stop it on reacquire)

        // Permanent stat bonuses (PERK + RESEARCH) computed once at init and folded into GetStat.
        private readonly PerkData _perkData = new PerkData();
        private Dictionary<Stat, int> _permanentBonuses = new Dictionary<Stat, int>();
        private LocalPlayer _bonusTarget;
        private string _perkDiag = "Permanent bonuses not initialised.";
        private bool _perkOverride;
        private string _lastPerkSig = null;

        private List<NanoItem> _learnableCache;   // built on demand (heavy DB scan), reused

        public override void Init(string pluginDir)
        {
            LoadConfig(pluginDir);
            _pluginDir = pluginDir;
            _pathsDir = Path.Combine(pluginDir, "paths");
            try { Directory.CreateDirectory(_pathsDir); } catch { }
            _logFile = Path.Combine(pluginDir, "aobuddy.log");
            _mode = ParseMode(_config.DefaultMode);
            InitPermanentBonuses(pluginDir);
            ItemValues.Load(pluginDir, Log);
            Zoning.Load(pluginDir, Log);

            _ctx = new BotContext(_config, Log);
            _ctx.Vitals = new VitalsTracker(_ctx);
            _move = new Movement();
            _follow = new FollowController(_ctx, _move);
            _combat = new CombatController(_ctx);
            _pets = new PetController(_ctx, pluginDir);
            _travel = new TravelController(_ctx, _move);
            _support = new SupportController(_ctx, _move);
            _resupply = new ResupplyController(_ctx, _move, pluginDir);
            _nav = new NavController(_ctx, pluginDir);
            _mission = new MissionController(_ctx, _move, pluginDir,
                text => { try { Client.Chat.SendPrivateMessage(_config.Owner, text); } catch { } });
            _overland = new OverlandController(_ctx, _move, pluginDir,
                text => { try { Client.Chat.SendPrivateMessage(_config.Owner, text); } catch { } });

            _hunt = new HuntController(_ctx, () => _mission.InMission);
            Client.ChestFullUpdateRaw += raw => { try { _mission.OnChestRaw(raw); } catch { } };
            _roll = new MissionRoll(_ctx);
            _run = new MissionRun(_ctx, _roll, _mission, _overland, _follow, pluginDir,
                text => { try { Client.Chat.SendPrivateMessage(_config.Owner, text); } catch { } },
                () => _dead,
                () => _support.Resting,    // only while actually sitting: a low HP the rest logic won't sit for must not park the run
                () => _support.HasPendingCasts || _support.Resting || _support.SecondsSinceCast < 15);

            Log($"=== Init owner='{_config.Owner}' mode={_mode} ===");
            Logger.Information($"AOBuddy::Init owner='{_config.Owner}' mode={_mode}");

            Client.OnUpdate += OnUpdate;

            Client.MessageReceived += (s, m) =>
            {
                try { _ctx.Vitals.OnMessage(m); }
                catch (Exception ex) { Log("VITALS feed error: " + ex.Message); }
                try { _resupply.OnMessage(m); }
                catch (Exception ex) { Log("RESUPPLY feed error: " + ex.Message); }
                try { _roll.OnMessage(m); } catch (Exception e) { Log("MISSIONROLL: " + e.Message); }
                try { _run.OnMessage(m); } catch (Exception e) { Log("MISSIONRUN: " + e.Message); }
                try { _mission.OnMessage(m); }
                catch (Exception ex) { Log("MISSION feed error: " + ex.Message); }
                try { OnServerMovedMe(m); }
                catch (Exception ex) { Log("SERVER MOVE error: " + ex.Message); }
            };

            // Player trades: the owner handing us credits when we asked for them (see ResupplyController).
            Trade.TradeStatusChanged += (who, status) =>
            {
                try { _resupply.OnTradeStatus(who, status); }
                catch (Exception ex) { Log("RESUPPLY trade error: " + ex.Message); }
            };

            // DIAG: count CharDCMove messages that actually deserialize+arrive, split owner vs self, to prove
            // whether the owner's movement is reaching us per-move (smooth) or only in bursts (dropped).
            Client.MessageReceived += (s, m) =>
            {
                try
                {
                    if (m != null && m.Body is CharDCMoveMessage cm)
                    {
                        if (cm.Identity.Instance == _ctx.OwnerCharId)
                        {
                            _diagOwnerMoves++;
                            // Capture the owner's movement keyframe and derive his velocity for interpolation.
                            double now = _clock.Elapsed.TotalSeconds;
                            Vector3 p = cm.Position;
                            // Speed is only measured between two MOVING keyframes: across a stop->start gap the
                            // displacement/time is ~0 and would leave him unpredicted for his whole first second.
                            if (_ownerKeyPos.HasValue && _ownerMovingKey)
                            {
                                double d = now - _ownerKeyTime;
                                if (d > 0.03)
                                {
                                    Vector3 v = (p - _ownerKeyPos.Value) / (float)d;
                                    if (v.Magnitude <= 20f) _ownerVel = v;   // ignore teleport-sized jumps
                                }
                            }
                            _ownerKeyPos = p; _ownerKeyTime = now;
                            _ownerKeyHeading = cm.Heading;
                            _ownerMovingKey = IsMovingMove(cm.MoveType);

                            // MIRROR: stacked on him — his move is our move, sent the instant it arrives.
                            if (_follow.MirrorLocked && Movement.IsMirrorable(cm.MoveType))
                            {
                                LocalPlayer lp = DynelManager.LocalPlayer;
                                if (lp != null) _move.Mirror(lp, cm);
                            }
                        }
                        else { LocalPlayer lp = DynelManager.LocalPlayer; if (lp != null && cm.Identity.Instance == lp.Identity.Instance) _diagSelfMoves++; }
                    }

                    // MISSION DEBUG — how much the server tells us about a mission's location/playfield.
                    // PlayfieldAnarchyF fires on zone-in (incl. entering a mission): playfield id, our landing
                    // coords, and the placed dynels (mobs/objects) = the mission layout the server hands us.
                    if (m != null && m.Body is PlayfieldAnarchyFMessage && m.RawPacket != null) _lastZoneInPacket = m.RawPacket;
                    if (_config.MissionDebug && m != null && m.Body is PlayfieldAnarchyFMessage pfm)
                    {
                        Log($"MISSIONDBG: PlayfieldAnarchyF pf={pfm.PlayfieldId1.Instance} land=({pfm.CharacterCoordinates.X:0},{pfm.CharacterCoordinates.Y:0},{pfm.CharacterCoordinates.Z:0}) dynels={(pfm.Dynels != null ? pfm.Dynels.Length : 0)}");
                        // The same packet carries the instanced building's room placement list
                        // (BuildingGeneratorData: template playfield, then room/floor/x/z/rotation per room),
                        // which the SDK does not parse yet. Keep the raw bytes so it can be decoded offline
                        // against the walk recorded in the mission. See NAV-CLIENTDATA.md.
                        if (m.RawPacket != null)
                        {
                            try
                            {
                                string dir = Path.Combine(_pluginDir, "missions");
                                Directory.CreateDirectory(dir);
                                string file = Path.Combine(dir, $"zonein-pf{pfm.PlayfieldId1.Instance}-{DateTime.Now:yyyyMMdd-HHmmss}.bin");
                                File.WriteAllBytes(file, m.RawPacket);
                                Log($"MISSIONDBG: zone-in packet saved ({m.RawPacket.Length} bytes) to {file}");
                            }
                            catch (Exception ex) { Log("MISSIONDBG: could not save zone-in packet: " + ex.Message); }
                        }
                    }

                    // The mission-terminal list (type 0x5C436609) is NOT SDK-typed — read it raw. Per mission
                    // it carries the destination playfield + entrance X/Z (see aobuddy-mission-wire-data).
                    if (_config.MissionDebug && m != null && m.RawPacket != null && m.RawPacket.Length >= 0x33)
                    {
                        byte[] raw = m.RawPacket;
                        uint sig = (uint)((raw[16] << 24) | (raw[17] << 16) | (raw[18] << 8) | raw[19]);
                        if (sig == 0x5C436609u)
                            Log($"MISSIONDBG: mission-list raw bytes={raw.Length} missions={raw[0x32]} diff={raw[0x1E]} (playfield + entrance per mission are in here; decode via ClickSaver offsets).");
                    }
                }
                catch { }
            };

            Client.Chat.PrivateMessageReceived += (s, msg) =>
            {
                // Tells from anyone else are never obeyed, but they are logged: Scotty answers warp requests by tell,
                // and those answers were invisible while travel waited on warps that never came (2026-09-23).
                if (!IsOwnerSender(msg.SenderName, msg.SenderId)) { Log($"TELL (not obeyed) from {msg.SenderName} (id={msg.SenderId}): {msg.Message}"); return; }
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
            _run.OnServerCorrection();
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
            try { Client.Chat.SendPrivateMessage(_config.Owner, $"I died in {where}. {next}"); } catch { }
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
            PlayerChar owner = FindOwner();
            if (owner != null)
                try { Client.SendPrivateMessage(owner.Identity.Instance, $"Back up at the reclaim point ({pos.X:0},{pos.Y:0},{pos.Z:0}). Waiting for you — send 'come' when close or walk to me."); } catch { }
        }

        // The owner used an object — hand it to TRAVEL, which arms a ride if he then zones off it.
        private void OnDynelUsed(Identity user, Identity target)
        {
            try
            {
                PlayerChar owner = FindOwner();
                if (owner == null || user.Instance != owner.Identity.Instance) return;
                Vector3? pos = DynelManager.Find(target, out Dynel obj) ? (Vector3?)obj.Transform.Position : owner.Transform.Position;
                _travel.OnOwnerUsed(target, pos);
                _lastUsedObj = target; _lastUsedObjPos = pos; _lastUsedObjAge = 0;   // for NAV warp learning
            }
            catch (Exception ex) { Log("OnDynelUsed error: " + ex.Message); }
        }

        private void OnUpdate(object _, double dt)
        {
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
                RefreshPerksFromWire(me);
                ApplyPermanentBonuses(me);

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
                        try { Client.Chat.SendPrivateMessage(_config.Owner, $"Ding! I hit level {lvlNow}."); } catch { }
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

                PlayerChar owner = FindOwner();

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
                // after using an object.
                _travel.Age(dt);
                _ownerLostSeconds = owner == null ? _ownerLostSeconds + dt : 0;

                bool ownerVisible = owner != null;
                if (ownerVisible)
                {
                    _ctx.OwnerCharId = owner.Identity.Instance;
                    if (!_ownerVisibleLast)
                    {
                        _follow.OnReacquired(owner);
                        if (_navReplaying) { _follow.StopReplay(); _navReplaying = false; }   // back in view — drop the fallback route, follow live
                    }
                    _follow.RecordAt(PredictOwnerPos(owner));   // interpolated owner position → smooth trail between sparse keyframes
                    if (_follow.Recording) _follow.RecordPathPoint(owner);

                    // NAV record: only while following cleanly (Assist, follow on, NOT combat/rest/sweep/
                    // travel) — so a break / go-retrieve-him episode never becomes part of a saved segment.
                    bool navClean = _mode == Mode.Assist && _config.Follow && !_combat.InCombat
                                    && !_support.Resting && !_follow.ZoneSweeping && !_travel.Active;
                    _nav.RecordOwner(owner.Transform.Position, navClean);

                    _lastOwnerPos = owner.Transform.Position;
                    _zoneSweepTried = false;   // he's back in view — allow a fresh zone attempt next time he's lost
                }
                else if (_ownerVisibleLast)
                {
                    // He just dropped out of view. Give TRAVEL its shot (rode an object?).
                    _travel.OnOwnerLost(_lastOwnerPos);
                }

                if (ownerVisible || _overland.Active || _run.Active) _arrivedAlone = 0;   // zoning alone is the point of overland travel
                else if (_arrivedAlone > 0)
                {
                    _arrivedAlone += dt;
                    if (_arrivedAlone > ArrivedAloneSeconds)
                    {
                        _arrivedAlone = 0;   // once per arrival
                        Vector3 ap = me.MovementComponent.Position;
                        Log($"ZONE: arrived in {Playfield.Name} at ({ap.X:0},{ap.Y:0},{ap.Z:0}) and you are not here after {ArrivedAloneSeconds:0}s.");
                        try
                        {
                            Client.Chat.SendPrivateMessage(_config.Owner,
                                $"I zoned into {Playfield.Name} at ({ap.X:0},{ap.Y:0},{ap.Z:0}) but you're not here. Holding.");
                        }
                        catch { }
                    }
                }

                // AUTO ZONE (follow-based): when he vanishes while we're following him on foot (not fighting,
                // not riding an object), he walked a zone line. We do NOT sweep the instant he vanishes — if
                // he outran us we're still back on his trail. Instead we let the breadcrumb trail walk us all
                // the way to where he vanished (the line), and only once the trail is exhausted (HasWork == false,
                // i.e. we're AT the line) do we sweep across it. Tight back-and-forth nudges along his travel
                // axis until the server heartbeat catches us in the wall band and zones us — no per-zone coords,
                // works entering or exiting. One attempt per loss episode (reset when he's back in view).
                // CATCH-UP when the owner is out of view and the live breadcrumbs are used up. On ground we
                // have ALREADY recorded, walk the clean recorded route (it carries the real up/down ramp Y)
                // toward his last-seen spot — no guessing, and NO zone-sweep ramming the ramp crest (that
                // sweep IS the ramp rubberband). Only when we're NOT on a recorded route do we fall back to
                // the proven one-shot zone-sweep (unknown ground, or a genuine zone line). Nav only supplies
                // the route; FOLLOW's replay walker moves the body, so this can't break follow/zone/combat.
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
                    Vector3? tgt = ownerVisible ? (Vector3?)owner.Transform.Position : _lastOwnerPos;
                    // Defer to TRAVEL and zoning: when the owner just blinked out (rode a button / crossed a
                    // zone line), that's travel's/the sweep's job — don't fire nav until he's been genuinely
                    // lost a couple seconds. When he's VISIBLE but far, catch up immediately (the ramp case).
                    bool needCatchup = (ownerVisible && owner != null && me.DistanceFrom(owner) > _config.NavCatchupMeters)
                                       || (!ownerVisible && _ownerLostSeconds > 2.0);
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
                // does NOT wander after a lost owner (zone-sweep / catch-up). Cancel any sweep already running
                // so he sits and recovers instead of pacing back and forth. Owner-independent by design.
                bool needRecovery = !_combat.InCombat && _support.NeedsRecovery(me);
                if (needRecovery && _follow.ZoneSweeping) _follow.CancelZoneSweep();

                // ZONE-SWEEP: only OFF recorded ground and only once the live trail is exhausted (unknown
                // ground / a genuine zone line). Unchanged, now subordinate to nav on recorded ground — and
                // never while he needs to recover (don't wander off instead of healing).
                // He vanished CLOSE and MOVING, we have walked his whole recorded route and then on to the
                // spot he disappeared from, and he is still gone: he crossed something. Work the line.
                bool crossingLikely = _ownerLostDist <= ZoneLossMeters && _ownerLostMoving;
                if (!ownerVisible && !needRecovery && crossingLikely && !_zoneGaveUp && !_mission.Active && !_overland.Active && !_run.Active
                    && !_follow.ZoneSweeping && !_navReplaying
                    && !_travel.Active && !_combat.InCombat && _config.Follow && _mode == Mode.Assist
                    && _lastOwnerPos.HasValue && !_follow.HasWork)
                {
                    if (_zoneAttempts >= ZoneMaxAttempts || _zoneEpisodeElapsed > ZoneEpisodeSeconds)
                    {
                        // Out of attempts. Say where we lost him rather than standing there silently — he
                        // can walk back, or send 'zone' to make us work the same spot again.
                        _zoneGaveUp = true;
                        Vector3 lp = _lastOwnerPos.Value;
                        Log($"ZONE: gave up after {_zoneAttempts} attempt(s) / {_zoneEpisodeElapsed:0}s at ({lp.X:0},{lp.Y:0},{lp.Z:0}) in {Playfield.Name}.");
                        try
                        {
                            Client.Chat.SendPrivateMessage(_config.Owner,
                                $"I lost you at ({lp.X:0},{lp.Y:0},{lp.Z:0}) in {Playfield.Name} and can't get across. Holding here — walk back or send 'zone'.");
                        }
                        catch { }
                    }
                    else
                    {
                        // Sweep his crossing spot. If we have crossed out of this playfield before and the
                        // recorded transition is near where he vanished, sweep THAT instead: it is a spot
                        // that provably works, rather than our best guess at where the line runs.
                        Vector3 centre = me.MovementComponent.Position;
                        Vector3? known = _config.NavUse ? _nav.NearestTransition(_lastOwnerPos.Value) : null;
                        string why = "owner vanished ahead while following (not combat)";
                        if (known.HasValue && Vector3.Distance(known.Value, _lastOwnerPos.Value) <= _config.NavSnapMeters)
                        {
                            centre = known.Value;
                            why = "a crossing we have made here before";
                        }

                        // Each retry steps sideways along the line: 0, then +3m, then -3m.
                        float offset = _zoneAttempts == 0 ? 0f : (_zoneAttempts == 1 ? 3f : -3f);
                        if (_follow.StartZoneSweep(centre))
                            _zoneAttempts++;
                        else
                            _zoneGaveUp = true;   // no usable direction from his path; stop trying this episode
                    }
                }
                _ownerVisibleLast = ownerVisible;

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
            if (_travel.Tick(me, dt, owner != null, _ownerLostSeconds)) { _follow.BreakMirror(); return; }
            _follow.WalkTick(me, owner, dt);
        }

        // ---- Decision ladder (throttled): cast queue > survival heal > fight > rest > follow/idle --
        private void Decide(LocalPlayer me, PlayerChar owner)
        {
            _support.UpdateOwnerSpeed(owner);

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
            if (_support.RestTick(me, owner, _combat.InCombat, combatLull)) return;

            // 4) FOLLOW / IDLE.
            _support.SetIdleState(me);
            bool active = (owner != null && me.DistanceFrom(owner) > Math.Max(_config.FollowDistance, 0.5f)) || _follow.HasWork;
            _ctx.SetBehavior(active ? "Following" : "Idle");
        }

        // A movement keyframe that means the owner is still moving (vs. a stop/sit).
        private static bool IsMovingMove(MovementAction mt) =>
            mt == MovementAction.ForwardStart || mt == MovementAction.Update
            || mt == MovementAction.SwitchToWalk || mt == MovementAction.SwitchToRun;

        // The owner's position, PREDICTED forward from his latest keyframe using his own reported velocity —
        // fills the gap between the server's sparse (~1/s) owner keyframes so the trail is smooth and the bot
        // keeps up. Extrapolates only while his last keyframe was "moving", only horizontally, only on flat
        // ground (no vertical guess on ramps), and only for a capped time (bounds any stop-overshoot). Falls
        // back to his raw position otherwise. Feeds crumbs, which stay wall-safe (it's his own motion).
        private Vector3 PredictOwnerPos(PlayerChar owner)
        {
            if (!_config.OwnerInterp || !_ownerKeyPos.HasValue) return owner.Transform.Position;
            double dtSince = _clock.Elapsed.TotalSeconds - _ownerKeyTime;
            if (_ownerMovingKey && dtSince > 0)
            {
                // Past the cap, HOLD at the capped point rather than snapping back to the keyframe — snapping
                // back put the target behind the bot, which then turned round to walk back to it.
                float t = (float)Math.Min(dtSince, _config.OwnerInterpMaxSec);
                Vector3 kp = _ownerKeyPos.Value;
                // Direction from the heading HE REPORTED in that keyframe (he runs where he faces), not from
                // the previous keyframe-to-keyframe delta: that one points along his OLD leg, so every turn he
                // made sent the prediction — and the bot — shooting off past the corner. Speed is his own
                // measured horizontal speed; Y keeps his measured climb rate so ramps stay on the surface.
                Vector3 fwd = _ownerKeyHeading.Forward;
                Vector3 flat = new Vector3(fwd.X, 0f, fwd.Z);
                Vector3 velFlat = new Vector3(_ownerVel.X, 0f, _ownerVel.Z);
                float speed = velFlat.Magnitude;
                if (flat.Magnitude < 0.001f || speed < 0.1f) return kp;
                // Backpedalling / strafing: he isn't moving where he faces, so trust his measured direction.
                Vector3 h = Vector3.Dot(flat.Normalize(), velFlat) > 0.5f * speed ? flat.Normalize() * speed : velFlat;
                return new Vector3(kp.X + h.X * t, kp.Y + _ownerVel.Y * t, kp.Z + h.Z * t);
            }
            return owner.Transform.Position;
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
            _ownerLostSeconds = 0;
            _navReplaying = false;
            _zoneAttempts = 0; _zoneEpisodeElapsed = 0; _zoneGaveUp = false;
            _arrivedAlone = 0.001;   // start the "did he follow me here?" clock
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

        // Read-only: any stat by name or number, as the server last sent it.
        private static string StatCommand(string arg)
        {
            var me = DynelManager.LocalPlayer;
            if (me == null) return "Not in play yet.";
            if (string.IsNullOrWhiteSpace(arg)) return "Usage: stat <name or number>, e.g. stat 349";
            Stat stat;
            if (int.TryParse(arg.Trim(), out int id)) stat = (Stat)id;
            else if (!Enum.TryParse(arg.Trim(), true, out stat)) return $"No stat named '{arg}'.";
            return me.TryGetStat(stat, out int v) ? $"{stat} ({(int)stat}) = {v} (0x{v:X})" : $"{stat} ({(int)stat}) has not been sent to me.";
        }

        private void HandleCommand(string message, Action<string> reply)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            string[] parts = message.Trim().TrimStart('!').Split(' ');
            string cmd = parts[0].ToLowerInvariant();
            string arg = parts.Length > 1 ? parts[1].ToLowerInvariant() : "";

            switch (cmd)
            {
                case "assist": _mode = Mode.Assist; reply("Mode: Assist."); break;
                case "hunt":
                    if (_mode != Mode.Assist && !(parts.Length > 1 && (arg == "off" || arg == "status"))) { _mode = Mode.Assist; }
                    _hunt.Command(parts.Length > 1 ? parts[1] : "", reply);
                    break;
                case "solo": _mode = Mode.Solo; reply("Mode: Solo."); break;
                case "stop":
                case "idle":
                    _mode = Mode.Idle; _hunt.Stop("stop command"); _run.Stop("stop command"); _follow.ClearMovement(); _combat.Reset();
                    _resupply.Stop(DynelManager.LocalPlayer, "stop command");
                    _overland.Stop("stop command");
                    { LocalPlayer lp = DynelManager.LocalPlayer; if (lp != null) { _move.Stop(lp, _config.SendIntervalMs); if (lp.IsAttacking) lp.StopAttack(); } }
                    reply("Mode: Idle. Standing down.");
                    break;
                case "follow":
                    _config.Follow = true;
                    if (_mode == Mode.Idle) _mode = Mode.Assist;
                    reply($"Following on (mode {_mode}).");
                    break;
                case "stay": _config.Follow = false; reply("Staying put (follow off)."); break;
                case "come":
                {
                    PlayerChar o = FindOwner();
                    if (o != null) { _follow.SetManualTarget(o.Transform.Position); reply("On my way."); }
                    else reply("Can't see you (out of range?).");
                    break;
                }
                case "forward":
                case "run":
                {
                 LocalPlayer p = DynelManager.LocalPlayer;
                    if (p == null) break;
                    Vector3 mypos = p.MovementComponent.Position;

                    // We RECORDED where this playfield's zone line is — the spot we came in through (the
                    // entry point) / where the owner crossed out (a transition), in this zone's own coords.
                    // HEAD THERE on our recorded route and cross. Do NOT sweep along the bot's stale facing
                    // (that pointed him away from the line and he ran the wrong way). Aim the lookup at the
                    // owner's last-seen spot so we pick the line he actually used.
                    Vector3? line = _nav.NearestTransition(_lastOwnerPos ?? mypos) ?? _nav.EntryPoint();
                    if (line.HasValue)
                    {
                        List<Vector3> route = _nav.RouteToward(mypos, line.Value);
                        if (route != null && route.Count >= 2)
                        {
                            _follow.LoadReplay(route, true);   // zone-push: walk the recorded route to the line and cross
                            _navReplaying = true;
                            reply($"Heading to the zone line at ({line.Value.X:0},{line.Value.Y:0},{line.Value.Z:0}).");
                            break;
                        }
                        // On/near the line but can't route to it — sweep across it, AIMED at the line.
                        Vector3 toLine = line.Value - mypos;
                        if (toLine.Length() > 0.5f)
                        {
                            _follow.StartManualSweep(mypos, toLine);
                            reply($"Working the zone line at ({line.Value.X:0},{line.Value.Y:0},{line.Value.Z:0}).");
                            break;
                        }
                    }

                    // No recorded line here: fall back to the owner's TRAVEL direction (same source the
                    // auto-sweep uses), then, last resort only, the bot's facing.
                    if (!_follow.StartZoneSweep(mypos))
                    {
                        Vector3 dir = (_lastOwnerPos.HasValue && (_lastOwnerPos.Value - mypos).Length() > 0.5f)
                                        ? _lastOwnerPos.Value - mypos
                                        : p.MovementComponent.Heading.Forward;
                        _follow.StartManualSweep(mypos, dir);
                    }
                    reply("Working the zone line (sweeping back and forth).");
                    break;
                }
                case "zone":
                {
                    LocalPlayer p = DynelManager.LocalPlayer;
                    if (p == null) break;
                    Vector3 mypos = p.MovementComponent.Position;
                    // He is asking again by hand, so the automatic attempts start over: a fresh budget and
                    // no "gave up" latch, otherwise this command would do nothing after the bot had already
                    // given up on the same spot.
                    _zoneAttempts = 0; _zoneEpisodeElapsed = 0; _zoneGaveUp = false;

                    // We RECORDED where this playfield's zone line is — the spot we came in through (the
                    // entry point) / where the owner crossed out (a transition), in this zone's own coords.
                    // HEAD THERE on our recorded route and cross. Do NOT sweep along the bot's stale facing
                    // (that pointed him away from the line and he ran the wrong way). Aim the lookup at the
                    // owner's last-seen spot so we pick the line he actually used.
                    Vector3? line = _nav.NearestTransition(_lastOwnerPos ?? mypos) ?? _nav.EntryPoint();
                    if (line.HasValue)
                    {
                        List<Vector3> route = _nav.RouteToward(mypos, line.Value);
                        if (route != null && route.Count >= 2)
                        {
                            _follow.LoadReplay(route, true);   // zone-push: walk the recorded route to the line and cross
                            _navReplaying = true;
                            reply($"Heading to the zone line at ({line.Value.X:0},{line.Value.Y:0},{line.Value.Z:0}).");
                            break;
                        }
                        // On/near the line but can't route to it — sweep across it, AIMED at the line.
                        Vector3 toLine = line.Value - mypos;
                        if (toLine.Length() > 0.5f)
                        {
                            _follow.StartManualSweep(mypos, toLine);
                            reply($"Working the zone line at ({line.Value.X:0},{line.Value.Y:0},{line.Value.Z:0}).");
                            break;
                        }
                    }

                    // No recorded line here: fall back to the owner's TRAVEL direction (same source the
                    // auto-sweep uses), then, last resort only, the bot's facing.
                    if (!_follow.StartZoneSweep(mypos))
                    {
                        Vector3 dir = (_lastOwnerPos.HasValue && (_lastOwnerPos.Value - mypos).Length() > 0.5f)
                                        ? _lastOwnerPos.Value - mypos
                                        : p.MovementComponent.Heading.Forward;
                        _follow.StartManualSweep(mypos, dir);
                    }
                    reply("Working the zone line (sweeping back and forth).");
                    break;
                }
                case "stand": DynelManager.LocalPlayer?.MovementComponent.ChangeMovement(MovementAction.LeaveSit); reply("Standing up."); break;
                case "sit": DynelManager.LocalPlayer?.MovementComponent.ChangeMovement(MovementAction.SwitchToSit); reply("Sitting down."); break;
                case "specials":
                {
                    _config.UseSpecials = !_config.UseSpecials;
                    var meS = DynelManager.LocalPlayer;
                    string known = meS != null && meS.KnownSpecials.Count > 0 ? string.Join(", ", meS.KnownSpecials) : "none learned yet";
                    reply($"Special attacks {(_config.UseSpecials ? "ON" : "OFF")}. Known: {known}.");
                    break;
                }
                case "weapon":
                case "weapons":
                {
                    int n = _combat.DumpWeapons(DynelManager.LocalPlayer);
                    reply($"Dumped {n} equipped weapon(s) to aobuddy.log (WEAPON: lines).");
                    break;
                }
                case "pets":
                {
                    _config.UsePets = !_config.UsePets;
                    LocalPlayer mp = DynelManager.LocalPlayer;
                    reply($"Pets {(_config.UsePets ? "ON" : "OFF")}. Up now: {(mp != null ? mp.Pets.Count() : 0)}.");
                    break;
                }
                case "petdbg":
                {
                    _pets.DumpPets(DynelManager.LocalPlayer);
                    reply("Dumped pet/owner diagnostics to aobuddy.log (PETDBG: lines).");
                    break;
                }
                case "resummon": _config.AutoResummon = !_config.AutoResummon; reply($"Auto-resummon {(_config.AutoResummon ? "ON" : "OFF")}."); break;
                case "petbuffs": _config.BuffPets = !_config.BuffPets; reply($"Pet buffs {(_config.BuffPets ? "ON" : "OFF")}."); break;
                case "petfollow": _pets.FollowMaster(DynelManager.LocalPlayer); reply("Pets: follow me."); break;

                case "petkill":
                case "petterminate":
                case "petdismiss":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer;
                    if (mp == null) { reply("Not in play yet."); break; }
                    int had = _pets.OwnedCount(mp);
                    _pets.Dismiss(mp);
                    reply($"Dismissed {had} pet(s). 'petsummon' brings them back.");
                    break;
                }

                case "petsummon":
                case "petresummon":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer;
                    if (mp == null) { reply("Not in play yet."); break; }
                    if (!_config.UsePets) { _config.UsePets = true; reply("Pets were off — turning them on."); }
                    _config.AutoResummon = true;
                    _pets.ResummonAll(mp);
                    reply("Resummoning the full set — one at a time as nano allows.");
                    break;
                }

                case "pethealme":
                case "petheal":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer; PlayerChar po = FindOwner();
                    if (mp == null || po == null) { reply("Can't — I don't see you."); break; }
                    if (!_pets.HealTargetLatched(mp, po.Identity, "you")) { reply("No heal pet up."); break; }
                    reply("Heal pet is on you.");
                    break;
                }

                case "pethealself":
                case "pethealme2":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer;
                    if (mp == null) { reply("Not in play yet."); break; }
                    if (!_pets.HealTargetLatched(mp, mp.Identity, "myself")) { reply("No heal pet up."); break; }
                    reply("Heal pet is on me.");
                    break;
                }

                case "pethealpet":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer;
                    if (mp == null) { reply("Not in play yet."); break; }
                    NpcChar tank = PetController.AttackPet(mp);
                    if (tank == null) { reply("No attack pet up to heal."); break; }
                    if (!_pets.HealTargetLatched(mp, tank.Identity, $"my attack pet ({tank.Name})")) { reply("No heal pet up."); break; }
                    reply($"Heal pet is on {tank.Name}.");
                    break;
                }

                case "pethealauto":
                {
                    _pets.HealAuto();
                    reply("Heal pet back to automatic: me when I'm melee, my attack pet when I'm ranged.");
                    break;
                }

                case "pethealtarget":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer;
                    if (mp == null) { reply("Not in play yet."); break; }
                    if (string.IsNullOrWhiteSpace(arg)) { reply($"Who? 'pethealtarget <name>', or {HealWhoHint()}"); break; }
                    SimpleChar who = ResolveHealSubject(arg, mp);
                    if (who == null) { reply($"I can't see anyone called '{arg}'."); break; }
                    if (!_pets.HealTargetLatched(mp, who.Identity, who.Name)) { reply("No heal pet up."); break; }
                    reply($"Heal pet is on {who.Name}.");
                    break;
                }

                case "pethealmytarget":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer; PlayerChar po = FindOwner();
                    if (mp == null || po == null) { reply("Can't — I don't see you."); break; }
                    // Only what you are FIGHTING is on the wire. A selection you have merely clicked is not:
                    // LookAtMessage, which carries a target change, is only ever sent with the sender's own
                    // identity — in every capture under E:\Funcom\sniffs and E:\Funcom\captures, not once
                    // relayed for another character. So outside combat there is nothing here to read, and
                    // saying "no target" alone just leaves you stuck.
                    SimpleChar t = po.FightingTarget;
                    if (t == null)
                    {
                        reply("I can only see what you're FIGHTING — the server never tells me what you've "
                              + $"merely clicked on. So name it instead: {HealWhoHint()}");
                        break;
                    }
                    if (!_pets.HealTargetLatched(mp, t.Identity, t.Name)) { reply("No heal pet up."); break; }
                    reply($"Heal pet is on your target, {t.Name}.");
                    break;
                }

                case "petstatus":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer;
                    if (mp == null) { reply("Not in play yet."); break; }
                    var roster = mp.Pets.Select(p => $"{p.Name} ({p.Role}, {mp.DistanceFrom(p):0}m)").ToList();
                    reply($"Owned {_pets.OwnedCount(mp)}, visible {roster.Count}"
                          + (roster.Count > 0 ? ": " + string.Join(", ", roster) : "")
                          + $". Heal pet targets {_pets.HealTargetDescription(mp)}.");
                    break;
                }
                case "petattack":
                {
                    LocalPlayer mp = DynelManager.LocalPlayer; PlayerChar po = FindOwner();
                    if (mp == null || po == null || po.FightingTarget == null) { reply("No target — are you fighting?"); break; }
                    _pets.EngageTarget(mp, po.FightingTarget, 999.0);   // force it through now
                    reply($"Pets: attacking {po.FightingTarget.Name}.");
                    break;
                }
                case "missiondbg": _config.MissionDebug = !_config.MissionDebug; reply($"Mission debug {(_config.MissionDebug ? "ON" : "OFF")} (logs to aobuddy.log)."); break;
                case "nanodump":
                {
                    LocalPlayer mn = DynelManager.LocalPlayer;
                    int n = mn != null ? _support.DumpNanos(mn) : 0;
                    reply($"Dumped {n} nanos to aobuddy.log.");
                    break;
                }
                case "catalog":
                {
                    string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_logFile) ?? ".", "catalog");
                    var r = NanoCatalog.ExportAll(dir);
                    reply(r.Item1 >= 0 ? $"Exported {r.Item1} nanos to catalog/ (all + per-class json)." : $"Catalog failed: {r.Item2}");
                    break;
                }
                case "buff": QueueBuffs(arg, reply); break;
                case "heal":
                    PlayerChar ht = FindOwner();
                    if (_config.HealNanoId > 0 && ht != null) { _support.QueueCast(new CastRequest { Target = ht.Identity, NanoId = _config.HealNanoId, Label = "heal owner" }); reply("Queued a heal."); }
                    else reply(ht == null ? "Can't see you (out of range?)." : "No heal nano configured (HealNanoId).");
                    break;
                case "record":
                    if (arg == "stop") { _follow.StopRecording(); reply($"Recorded {_follow.RecordCount} points. Save with 'savepath <name>'."); }
                    else { _follow.StartRecording(); reply("Recording your path — walk the route, then 'record stop'."); }
                    break;
                case "savepath":
                case "save":
                    if (string.IsNullOrEmpty(arg)) { reply("Usage: savepath <name>"); break; }
                    if (_follow.RecordCount == 0) { reply("Nothing recorded. Use 'record' first."); break; }
                    try { SavePath(arg, _follow.RecordBuffer); reply($"Saved '{arg}' ({_follow.RecordCount} points)."); }
                    catch (Exception ex) { reply("Save failed: " + ex.Message); }
                    break;
                case "path":
                    if (arg == "stop") { _follow.StopReplay(); reply("Path playback stopped."); break; }
                    if (string.IsNullOrEmpty(arg)) { reply("Usage: path <name> | path stop"); break; }
                    try
                    {
                        List<Vector3> pts = LoadPath(arg);
                        _follow.LoadReplay(pts);
                        reply($"Replaying '{arg}' ({pts.Count} points).");
                    }
                    catch (Exception ex) { reply("Load failed: " + ex.Message); }
                    break;
                case "paths": reply("Saved paths: " + ListPaths()); break;
                case "nav":
                    switch (arg)
                    {
                        case "save": _nav.Save(); reply($"Nav saved (pf {_nav.PlayfieldId})."); break;
                        case "on": _config.NavRecord = true; reply("Nav recording ON."); break;
                        case "off": _config.NavRecord = false; reply("Nav recording OFF."); break;
                        case "use": _config.NavUse = !_config.NavUse; reply($"Nav lost-fallback {(_config.NavUse ? "ON" : "OFF")}."); break;
                        default: reply(_nav.Status()); break;
                    }
                    break;
                case "status": reply(StatusLine()); break;
                case "navdata": reply(NavDataCommand(arg)); break;
                case "mission":
                    if (arg == "run") { if (_mode != Mode.Assist) _mode = Mode.Assist; _run.Command(parts.Length > 2 ? parts[2] : "", reply); }
                    else if (!_roll.Command(arg, parts.Length > 2 ? parts[2] : "", reply))
                        _mission.Command(parts.Length > 1 ? parts[1] : "", reply);
                    break;
                case "travelto": _overland.Command(parts.Skip(1).Where(p => p.Length > 0).ToArray(), reply); break;
                case "stat": reply(StatCommand(parts.Length > 1 ? parts[1] : "")); break;

                // ---- Knowledge (profession / nanos) ----
                case "class":
                case "whoami": reply(ClassLine()); break;
                case "nanos": ReportNanos(arg, reply); break;
                case "active":
                case "buffs": ReportActive(reply); break;
                case "autobuff":
                case "keepup":
                {
                    LocalPlayer meB = DynelManager.LocalPlayer;
                    if (meB == null) { reply("No character loaded."); break; }
                    List<string> pl = _support.DescribeBuffPlans(meB, FindOwner());
                    if (pl.Count == 0) { reply("Auto-buff: no learned nanos classified as keep-up buffs."); break; }
                    reply($"Auto-buff {(_config.AutoBuff ? "ON" : "OFF")} (self={_config.BuffSelf} owner={_config.BuffOwner} team={_config.BuffTeam}, margin {_config.RebuffMarginSeconds:0}s): {pl.Count} buffs.");
                    foreach (string s in pl.Take(20)) reply(s);
                    if (pl.Count > 20) reply($"…(+{pl.Count - 20} more)");
                    break;
                }
                case "supplies":
                case "stims": ReportSupplies(reply); break;
                case "learnable":
                case "learn": ReportLearnable(arg, reply); break;
                case "perks":
                case "perk": ReportPerks(reply); break;

                case "resupply":
                {
                    LocalPlayer rp = DynelManager.LocalPlayer;
                    if (rp == null) { reply("No character loaded."); break; }
                    switch (arg)
                    {
                        case "stop": if (_resupply.Active) { _resupply.Stop(rp, "owner"); reply("Resupply stopped."); } else reply("Not resupplying."); break;
                        case "status": reply("Resupply: " + _resupply.Describe()); break;
                        case "forget": _resupply.Forget(); reply("Forgot which terminals sell what; the next resupply checks them all again."); break;
                        case "machines":
                        {
                            List<string> ml = _resupply.DescribeMachines(rp);
                            if (ml.Count == 0) { reply($"No terminals within {_config.ResupplySearchRadius:0}m."); break; }
                            foreach (string l in ml.Take(15)) reply(Truncate(l, 440));
                            if (ml.Count > 15) reply($"…(+{ml.Count - 15} more, all in the log)");
                            break;
                        }
                        default: _resupply.Start(rp, reply); break;
                    }
                    break;
                }
                // TEMPORARY: open every matching terminal in the zone and log it (ResupplyController.StartSurvey).
                case "vendordebug":
                {
                    LocalPlayer vp = DynelManager.LocalPlayer;
                    if (vp == null) { reply("No character loaded."); break; }
                    _resupply.StartSurvey(vp, parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "", reply);
                    break;
                }
                case "shop":
                case "sell":
                case "buy": reply("Only 'resupply' (stims and rechargers) so far; general vendor buy/sell isn't implemented yet."); break;
                case "whompa":
                case "travel": reply("Use 'travelto <x> <y> <playfield>' (or 'travelto <playfield>'); it plans the zone lines, whompas and Scotty warps."); break;

                case "help":
                case "commands":
                    reply(HelpPages.For(arg));
                    break;
                default: reply($"Unknown command '{cmd}'. Try 'help'."); break;
            }
        }

        private void QueueBuffs(string which, Action<string> reply)
        {
            if (which == "") which = "all";
            int queued = 0;
            if (which == "self" || which == "all")
                foreach (int id in _config.SelfBuffNanoIds) { _support.QueueCast(new CastRequest { OnSelf = true, NanoId = id, Label = "self buff" }); queued++; }
            if (which == "owner" || which == "all")
            {
                PlayerChar owner = FindOwner();
                if (owner != null) foreach (int id in _config.OwnerBuffNanoIds) { _support.QueueCast(new CastRequest { Target = owner.Identity, NanoId = id, Label = "owner buff" }); queued++; }
            }
            if (which == "team" || which == "all")
                foreach (TeamMember member in Team.Members)
                    foreach (int id in _config.TeamBuffNanoIds) { _support.QueueCast(new CastRequest { Target = member.Identity, NanoId = id, Label = "team buff" }); queued++; }
            reply(queued > 0 ? $"Queued {queued} buff cast(s)." : "No matching buff nano ids configured.");
        }

        // ---- Knowledge: profession & nanos ---------------------------------------

        private string ClassLine()
        {
            LocalPlayer me = DynelManager.LocalPlayer;
            if (me == null) return "No character loaded.";
            me.TryGetStat(Stat.Level, out int lvl);
            me.TryGetStat(Stat.Health, out int hp);
            me.TryGetStat(Stat.MaxHealth, out int maxhp);
            me.TryGetStat(Stat.CurrentNCU, out int ncu);
            me.TryGetStat(Stat.MaxNCU, out int maxncu);
            me.TryGetStat(Stat.Breed, out int breed);
            me.TryGetStat(Stat.Strength, out int str);
            me.TryGetStat(Stat.Agility, out int agi);
            me.TryGetStat(Stat.Stamina, out int sta);
            me.TryGetStat(Stat.Intelligence, out int intel);
            me.TryGetStat(Stat.Sense, out int sen);
            me.TryGetStat(Stat.Psychic, out int psy);
            int uploaded = me.SpellList?.Length ?? 0;
            return $"{me.Name}: {me.Profession} lvl {lvl} ({(Breed)breed}). HP {hp}/{maxhp}, NCU {ncu}/{maxncu} used. " +
                   $"Str {str} Agi {agi} Sta {sta} Int {intel} Sen {sen} Psy {psy}. Uploaded nanos: {uploaded}.";
        }

        private void ReportNanos(string filter, Action<string> reply)
        {
            LocalPlayer me = DynelManager.LocalPlayer;
            if (me?.SpellList == null || me.SpellList.Length == 0) { reply("No uploaded nanos found."); return; }

            var names = new List<string>();
            foreach (int id in me.SpellList)
            {
                if (ItemData.Find(id, out NanoItem ni) && ni != null)
                {
                    if (filter.Length > 0 && ni.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    names.Add($"{ni.Name} [{id}]");
                }
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            if (names.Count == 0) { reply(filter.Length > 0 ? $"No uploaded nanos match '{filter}'." : "No named nanos resolved."); return; }
            reply($"Uploaded ({names.Count}): " + Truncate(string.Join(", ", names), 440));
        }

        private void ReportActive(Action<string> reply)
        {
            LocalPlayer me = DynelManager.LocalPlayer;
            if (me == null) { reply("No character loaded."); return; }
            var buffs = me.Buffs;
            if (buffs == null || buffs.Count == 0) { reply("No active nanos."); return; }

            var parts = new List<string>();
            foreach (var b in buffs.OrderBy(b => b.Cooldown?.RemainingTime ?? 0))
            {
                string name = b.NanoItem != null ? b.NanoItem.Name : $"nano {b.Id}";
                double rem = b.Cooldown?.RemainingTime ?? 0;
                parts.Add($"{name} [{b.Id}] {FormatTime(rem)}");
            }
            reply($"Active ({parts.Count}): " + Truncate(string.Join(", ", parts), 440));
        }

        /// <summary>
        /// What he is carrying, per QL, and which of it his skill can actually reach. Two lines, one per kind:
        /// the bag, then the verdict and the skill behind it — so a number that looks wrong can be checked
        /// against what is in the bag without reading the log.
        /// </summary>
        private void ReportSupplies(Action<string> reply)
        {
            LocalPlayer me = DynelManager.LocalPlayer;
            if (me == null) { reply("Not in play yet."); return; }

            reply(SupplyLine("stims", _config.StimKeyword, _config.StimItemName, Stat.FirstAid, "First Aid", me));
            reply(SupplyLine("rechargers", _config.RechargerKeyword, _config.RechargerItemName, Stat.Treatment, "Treatment", me));
        }

        private string SupplyLine(string what, string keyword, string exactName, Stat skill, string skillName, LocalPlayer me)
        {
            List<Item> carried = _support.HealItemPoolUnfiltered(keyword, exactName).ToList();
            if (carried.Count == 0) return $"No {what} at all.";

            // Group by QL so it reads the way he counts them: "24 QL7, 38 QL9".
            var byQl = carried.GroupBy(it => it.Ql).OrderBy(g => g.Key)
                .Select(g => new { Ql = g.Key, Count = g.Sum(it => Math.Max(1, it.Count)), Usable = g.Any(it => SupportController.MeetsHealReqs(it, me)) })
                .ToList();

            int total = byQl.Sum(g => g.Count);
            int usable = byQl.Where(g => g.Usable).Sum(g => g.Count);
            string skillHave = me.TryGetStat(skill, out int sv) ? sv.ToString() : "unknown";
            string bag = string.Join(", ", byQl.Select(g => $"{g.Count} QL{g.Ql}"));

            if (usable == total)
                return $"{bag} — {total} {what}, all usable with my {skillName} of {skillHave}.";

            string canUse = string.Join(", ", byQl.Where(g => g.Usable).Select(g => $"{g.Count} QL{g.Ql}"));
            string tooHigh = string.Join(", ", byQl.Where(g => !g.Usable).Select(g => $"QL{g.Ql}"));
            return $"{bag} — {total} {what}. {(usable == 0 ? "None" : canUse)} usable with my {skillName} "
                   + $"of {skillHave}; {tooHigh} need more.";
        }

        private static string FormatTime(double seconds)
        {
            if (seconds <= 0) return "—";
            int s = (int)seconds;
            return s >= 60 ? $"{s / 60}m{s % 60:00}s" : $"{s}s";
        }

        private void ReportLearnable(string arg, Action<string> reply)
        {
            LocalPlayer me = DynelManager.LocalPlayer;
            if (me == null) { reply("No character loaded."); return; }

            if (arg == "refresh") { _learnableCache = null; arg = ""; }

            if (_learnableCache == null)
            {
                var have = new HashSet<int>(me.SpellList ?? new int[0]);
                var found = new List<NanoItem>();
                foreach (int id in ItemData.AllNanoIds())
                {
                    if (have.Contains(id)) continue;
                    if (!ItemData.Find(id, out NanoItem ni) || ni == null) continue;
                    if (ni.MeetsUseReqs(null, true)) found.Add(ni);
                }
                _learnableCache = found;
            }

            IEnumerable<NanoItem> list = _learnableCache;
            if (arg.Length > 0)
                list = list.Where(n => n.Name.IndexOf(arg, StringComparison.OrdinalIgnoreCase) >= 0);

            var shown = list.OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
                            .Select(n => n.Name).Distinct().ToList();
            if (shown.Count == 0) { reply(arg.Length > 0 ? $"No qualifying nanos match '{arg}'." : "No qualifying nanos found."); return; }

            int cap = 30;
            string head = $"Can learn/cast ({shown.Count}{(arg.Length > 0 ? $" matching '{arg}'" : "")}): ";
            reply(head + Truncate(string.Join(", ", shown.Take(cap)), 400) + (shown.Count > cap ? $" …(+{shown.Count - cap}, filter with 'learnable <text>')" : ""));
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max) + "…";

        // ---- Helpers -------------------------------------------------------------

        private string PathFile(string name) => Path.Combine(_pathsDir, name + ".json");

        private void SavePath(string name, List<Vector3> pts)
        {
            var data = pts.Select(p => new[] { p.X, p.Y, p.Z }).ToList();
            File.WriteAllText(PathFile(name), JsonConvert.SerializeObject(data));
        }

        private List<Vector3> LoadPath(string name)
        {
            var data = JsonConvert.DeserializeObject<List<float[]>>(File.ReadAllText(PathFile(name)));
            return data.Select(a => new Vector3(a[0], a[1], a[2])).ToList();
        }

        private string ListPaths()
        {
            try
            {
                string[] files = Directory.GetFiles(_pathsDir, "*.json");
                return files.Length == 0 ? "(none)" : string.Join(", ", files.Select(Path.GetFileNameWithoutExtension));
            }
            catch { return "(none)"; }
        }

        /// <summary>A visible character by name, for commands that name someone (case-insensitive, and a
        /// unique prefix will do so you need not type a full name in a tell).</summary>
        private static SimpleChar FindCharByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            name = name.Trim();
            var all = DynelManager.Characters.Where(c => !string.IsNullOrEmpty(c.Name)).ToList();
            SimpleChar exact = all.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;
            var starts = all.Where(c => c.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase)).ToList();
            return starts.Count == 1 ? starts[0] : null;
        }

        /// <summary>
        /// Who a heal-pet command means. A name works, and so do the words you'd actually type in a tell:
        /// "me"/"you" is the owner, "him"/"self"/"bot" is the bot, "pet" is the attack pet. The two the owner
        /// reaches for most — the bot and himself — are exactly the two that cannot be picked by clicking,
        /// because a selection is never broadcast (see pethealmytarget).
        /// </summary>
        private SimpleChar ResolveHealSubject(string who, LocalPlayer me)
        {
            switch ((who ?? "").Trim().ToLowerInvariant())
            {
                case "me":
                case "you":
                case "owner":
                    return FindOwner();
                case "him":
                case "he":
                case "self":
                case "bot":
                case "himself":
                    return me;
                case "pet":
                case "attackpet":
                    return PetController.AttackPet(me);
            }
            return FindCharByName(who);
        }

        /// <summary>The ways to name a heal-pet subject, for a reply that would otherwise be a dead end.</summary>
        private string HealWhoHint() =>
            "'pethealme' (you), 'pethealself' (me), 'pethealpet' (my attack pet), "
            + "or 'pethealtarget <name>' for anyone else I can see.";

        private PlayerChar FindOwner() =>
            DynelManager.Players.FirstOrDefault(p => string.Equals(p.Name, _config.Owner, StringComparison.OrdinalIgnoreCase));

        private bool IsOwner(string name) =>
            !string.IsNullOrEmpty(name) && string.Equals(name, _config.Owner, StringComparison.OrdinalIgnoreCase);

        private uint _ownerChatId;   // sender id proven to be the owner, kept for when the name map is empty

        /// <summary>
        /// Owner check for an incoming tell. A tell's SenderName is looked up in ChatClient.IdToNameMap and is
        /// literally "&lt;Unknown&gt;" until that map knows the id — so a name-only check silently DROPS the
        /// owner's own commands. Fall back to the sender id: remembered once seen, else the owner's dynel id.
        /// </summary>
        private bool IsOwnerSender(string name, uint senderId)
        {
            if (IsOwner(name))
            {
                if (senderId != 0) _ownerChatId = senderId;
                return true;
            }
            if (senderId != 0 && senderId == _ownerChatId) return true;
            PlayerChar owner = FindOwner();
            return owner != null && senderId != 0 && (uint)owner.Identity.Instance == senderId;
        }

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
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    _navData = AOBuddyNav.Load(_pluginDir, pf);
                    if (_navData == null && _lastZoneInPacket != null)
                        _navData = AOBuddyNav.LoadMission(_pluginDir, _lastZoneInPacket);   // an instance: compose it from its pool
                    if (_navData == null) return $"No nav data folder for pf {pf} ({AOBuddyNav.FolderFor(_pluginDir, pf)}) and no mission layout in the zone-in packet.";
                    Log($"NAVDATA: loaded pf {pf} {_navData.Kind} ground={(_navData.Ground != null ? _navData.Ground.SamplesX + "x" + _navData.Ground.SamplesZ : "-")} rooms={(_navData.Dungeon != null ? _navData.Dungeon.Rooms.Count : 0)} collision={(_navData.Collision != null ? _navData.Collision.Triangles : 0)} tris in {sw.ElapsedMilliseconds} ms");
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

        private void InitPermanentBonuses(string pluginDir)
        {
            try
            {
                string sqlPath = Path.Combine(pluginDir, "GameData", "perks.sql");
                if (!File.Exists(sqlPath)) sqlPath = Path.Combine("GameData", "perks.sql");
                string xmlPath = Path.Combine(pluginDir, "GameData", "Perks.xml");
                if (!File.Exists(xmlPath)) xmlPath = Path.Combine("GameData", "Perks.xml");

                bool sqlOk = _perkData.Load(sqlPath);
                bool xmlOk = _perkData.LoadPerkXml(xmlPath);

                if (!sqlOk)
                {
                    _perkDiag = $"perks.sql not loaded (looked in {sqlPath}). Perk bonuses OFF.";
                    Logger.Warning("AOBuddy: " + _perkDiag);
                    Log(_perkDiag);
                    return;
                }

                _perkOverride = _config.PerkLines != null && _config.PerkLines.Count > 0;

                if (_perkOverride)
                {
                    RecomputePerkBonuses(_config.PerkLines, "config override");
                }
                else
                {
                    _permanentBonuses = MergeResearch(new Dictionary<Stat, int>(), out _);
                    _perkDiag =
                        $"perks.sql {(sqlOk ? _perkData.PerkCount + " perks" : "FAIL")}, " +
                        $"Perks.xml {(xmlOk ? _perkData.PerkXmlCount + " ids" : "FAIL — auto-detect OFF")}. " +
                        $"Auto-detecting trained perks from wire at login.";
                }

                Logger.Information("AOBuddy: " + _perkDiag);
                Log(_perkDiag);
            }
            catch (Exception ex)
            {
                _perkDiag = "Permanent-bonus init failed: " + ex.Message;
                Logger.Error("AOBuddy: " + _perkDiag);
                Log(_perkDiag);
            }
        }

        private void RefreshPerksFromWire(LocalPlayer me)
        {
            if (_perkOverride || !_perkData.PerkXmlLoaded) return;

            var perks = me.Perks;
            if (perks == null || perks.Length == 0) return;

            var ids = perks.Select(p => p.SkillId).Where(id => id != 0).OrderBy(id => id).ToList();
            if (ids.Count == 0) return;

            string sig = string.Join(",", ids);
            if (sig == _lastPerkSig) return;
            _lastPerkSig = sig;

            var lines = _perkData.DetectPerkLines(ids, out var unresolved);
            RecomputePerkBonuses(lines, $"wire ({ids.Count} ids, {unresolved.Count} unresolved)");
        }

        private void RecomputePerkBonuses(IEnumerable<string> perkLines, string source)
        {
            var bonuses = _perkData.ComputeBonuses(perkLines, out var unknown, out var unmapped, out var applied);
            bonuses = MergeResearch(bonuses, out var researchUnknown);

            _permanentBonuses = bonuses;
            _bonusTarget = null;

            string sumStr = bonuses.Count == 0 ? "(none)" :
                string.Join(", ", bonuses.OrderBy(b => b.Key.ToString()).Select(b => $"{b.Key}+{b.Value}"));
            _perkDiag =
                $"Perks [{source}]: [{(applied.Count > 0 ? string.Join(", ", applied) : "none")}]. " +
                $"Bonuses: {sumStr}." +
                (unknown.Count > 0 ? $" UNKNOWN perk names: [{string.Join(", ", unknown)}]." : "") +
                (unmapped.Count > 0 ? $" UNMAPPED skills: [{string.Join(", ", unmapped)}]." : "") +
                (researchUnknown.Count > 0 ? $" UNKNOWN research stats: [{string.Join(", ", researchUnknown)}]." : "");
            Logger.Information("AOBuddy: " + _perkDiag);
            Log(_perkDiag);
        }

        private Dictionary<Stat, int> MergeResearch(Dictionary<Stat, int> bonuses, out List<string> researchUnknown)
        {
            researchUnknown = new List<string>();
            if (_config.ResearchBonuses != null)
            {
                foreach (var kv in _config.ResearchBonuses)
                {
                    if (kv.Value == 0) continue;
                    if (PerkData.TryResolveStat(kv.Key, out Stat stat))
                        bonuses[stat] = (bonuses.TryGetValue(stat, out int cur) ? cur : 0) + kv.Value;
                    else
                        researchUnknown.Add(kv.Key);
                }
            }
            return bonuses;
        }

        private void ApplyPermanentBonuses(LocalPlayer me)
        {
            if (ReferenceEquals(_bonusTarget, me)) return;
            me.SetPermanentBonuses(new Dictionary<Stat, int>(_permanentBonuses));
            _bonusTarget = me;
            Log($"PERM BONUSES applied to LocalPlayer ({_permanentBonuses.Count} stats).");
        }

        private void ReportPerks(Action<string> reply)
        {
            reply(Truncate(_perkDiag, 440));

            LocalPlayer me = DynelManager.LocalPlayer;
            if (me != null)
            {
                me.TryGetStat(Stat.Strength, out int str);
                me.TryGetStat((Stat)123, out int fa);
                int strBonus = me.PermanentBonuses.TryGetValue(Stat.Strength, out int sb) ? sb : 0;
                reply($"Now: Strength={str} (perm +{strBonus}), FirstAid={fa}. Perm layer holds {me.PermanentBonuses.Count} stats.");

                var perks = me.Perks;
                if (perks != null && perks.Length > 0)
                {
                    var ids = perks.Select(p => p.SkillId).Where(id => id != 0).OrderBy(id => id).ToList();
                    var lines = _perkData.DetectPerkLines(ids, out var unresolved);
                    reply(Truncate($"wire perk ids [{ids.Count}] -> detected: [{(lines.Count > 0 ? string.Join(", ", lines) : "none")}]" +
                        (unresolved.Count > 0 ? $"; {unresolved.Count} unresolved ids (research/unknown): {string.Join(",", unresolved.Take(20))}" : ""), 440));
                }
                else
                {
                    reply("wire perks: empty (no FullCharacter yet, or none trained). " +
                        (_perkOverride ? "Manual override active (config PerkLines)." : "Auto-detect waiting."));
                }
            }
        }

        private void LoadConfig(string pluginDir)
        {
            string path = Path.Combine(pluginDir, "config.json");
            if (!File.Exists(path)) path = Path.Combine(pluginDir, "config.example.json");
            if (File.Exists(path))
            {
                try { _config = JsonConvert.DeserializeObject<BuddyConfig>(File.ReadAllText(path)) ?? new BuddyConfig(); Logger.Information($"Loaded config from {path}"); }
                catch (Exception ex) { Logger.Error($"Failed to read {path}: {ex.Message}; using defaults."); }
            }
            else Logger.Warning($"No config.json in {pluginDir}; using defaults (owner unset).");
        }
    }
}
