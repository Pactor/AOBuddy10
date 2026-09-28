using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using AOSharp.Clientless;
using Newtonsoft.Json.Linq;

namespace AOBuddy
{
    /// <summary>
    /// DEAD-CONNECTION WATCHDOG (2026-09-27). At 21:00:31 that day the zone server stopped sending anything - no
    /// spawns, no SetPos, no N3Teleport, no pet updates - while the TCP link stayed "up", so the SDK's own
    /// reconnect (NetworkSession: Trigger.Disconnect -> Reconnect(), only on a socket drop) never ran. The bot
    /// simulated locally for 25 minutes and every zone line "didn't take"; a process restart fixed it.
    ///
    /// Every packet from the server (Client.PacketRaw with fromServer=true - raised for ALL of them, including
    /// the ChestFullUpdate/SpellList ones that never reach MessageReceived) stamps the clock. While in play, a
    /// silence longer than WatchdogSeconds fires: the log says so, the mission run's state goes to
    /// watchdog.json, and the process exits with code 75 for tools\run-bot.ps1 to restart it. On the next login
    /// the saved state resumes the mission run by itself.
    ///
    /// Why exit instead of an in-process relogin: the SDK has no public relogin (Client.Disconnect() is a full
    /// teardown: stops the update loop and disposes the logger), and a process restart is the one recovery
    /// actually seen to work (2026-09-27).
    /// </summary>
    public sealed class Watchdog
    {
        public const int ExitCode = 75;
        private const double CooldownSeconds = 300;   // never more than once per 5 minutes, across restarts too
        private const double ResumeDelaySeconds = 5;  // after entering play, like restart.ps1's playfield + 3 s wait

        private readonly BuddyConfig _config;
        private readonly Action<string> _log;
        private readonly string _path;
        private readonly Stopwatch _sw = Stopwatch.StartNew();

        private long _lastRxTicks;          // _sw ticks of the last server packet (network/update thread writes)
        private bool _wasInPlay;
        private double _inPlaySince = -1;

        // Persisted (watchdog.json).
        private int _fires;
        private DateTime _lastFireUtc = DateTime.MinValue;
        private bool _resumePending, _resumeWant;
        private double _lastGap;

        public Watchdog(BuddyConfig config, string pluginDir, Action<string> log)
        {
            _config = config; _log = log;
            _path = Path.Combine(pluginDir, "watchdog.json");
            Stamp();
            Load();
            Client.PacketRaw += (packet, fromServer) => { if (fromServer) Stamp(); };
            Client.CharacterInPlay += _ => Stamp();
        }

        private void Stamp() => Interlocked.Exchange(ref _lastRxTicks, _sw.ElapsedTicks);

        public double SecondsSinceLastMessage =>
            (_sw.ElapsedTicks - Interlocked.Read(ref _lastRxTicks)) / (double)Stopwatch.Frequency;

        /// <summary>
        /// Update thread, every frame. runActive/wantRun describe the mission run right now (saved if it fires);
        /// resume(cmd) runs a command as the owner would, for the post-restart resume.
        /// </summary>
        public void Tick(bool runActive, bool wantRun, Action<string> resume)
        {
            bool inPlay = Client.InPlay;
            if (!inPlay)
            {
                // Loading / zoning / not logged in: never fire; the clock restarts when play resumes.
                _wasInPlay = false; _inPlaySince = -1;
                return;
            }
            double now = _sw.Elapsed.TotalSeconds;
            if (!_wasInPlay) { _wasInPlay = true; _inPlaySince = now; Stamp(); }

            if (_resumePending && now - _inPlaySince >= ResumeDelaySeconds && DynelManager.LocalPlayer != null && (int)Playfield.ModelId != 0)
            {
                _resumePending = false;
                Save();
                string cmd = _resumeWant ? "mission run want" : "mission run";
                if (runActive) _log("WATCHDOG: mission run already going after the restart; nothing to resume.");
                else
                {
                    _log($"WATCHDOG: resuming after the watchdog restart: '{cmd}'.");
                    try { resume(cmd); } catch (Exception ex) { _log("WATCHDOG: resume failed: " + ex.Message); }
                }
            }

            int limit = _config.WatchdogSeconds;
            if (limit <= 0) return;
            double gap = SecondsSinceLastMessage;
            if (gap < limit) return;
            if ((DateTime.UtcNow - _lastFireUtc).TotalSeconds < CooldownSeconds) return;

            _fires++; _lastFireUtc = DateTime.UtcNow; _lastGap = gap;
            bool restart = _config.WatchdogRestart;
            _resumePending = restart && runActive; _resumeWant = wantRun;
            Save();
            _log($"WATCHDOG: no server message for {gap:0}s (threshold {limit}s, fire #{_fires}); mission run {(runActive ? (wantRun ? "active (want)" : "active") : "off")}"
                 + (restart ? $" - exiting with code {ExitCode} for the supervisor (tools\\run-bot.ps1) to restart and relogin." : " - WatchdogRestart is off, only logging."));
            if (!restart) { Stamp(); return; }   // log-only: re-arm from now (the 5-minute cooldown still applies)
            Environment.Exit(ExitCode);
        }

        public string Status()
        {
            double since = SecondsSinceLastMessage;
            return $"Watchdog: {(Client.InPlay ? $"{since:0.0}s since the last server message" : "not in play (not counting)")}, threshold "
                   + (_config.WatchdogSeconds > 0 ? $"{_config.WatchdogSeconds}s" : "OFF")
                   + $", action {(_config.WatchdogRestart ? $"exit {ExitCode} + restart" : "log only")}, fired {_fires} time(s)"
                   + (_lastFireUtc != DateTime.MinValue ? $", last {_lastFireUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss} after {_lastGap:0}s silent" : "")
                   + (_resumePending ? $", will resume '{(_resumeWant ? "mission run want" : "mission run")}' after login" : "") + ".";
        }

        public JObject Json() => new JObject
        {
            ["secondsSinceServerMessage"] = Math.Round(SecondsSinceLastMessage, 1),
            ["thresholdSeconds"] = _config.WatchdogSeconds,
            ["restart"] = _config.WatchdogRestart,
            ["fires"] = _fires,
            ["lastFire"] = _lastFireUtc == DateTime.MinValue ? null : _lastFireUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
        };

        private void Load()
        {
            try
            {
                if (!File.Exists(_path)) return;
                var o = JObject.Parse(File.ReadAllText(_path));
                _fires = (int?)o["fires"] ?? 0;
                _lastFireUtc = (DateTime?)o["lastFireUtc"] ?? DateTime.MinValue;
                _lastGap = (double?)o["lastGapSeconds"] ?? 0;
                _resumePending = (bool?)o["resume"] ?? false;
                _resumeWant = (bool?)o["resumeWant"] ?? false;
                // A resume is only for the restart right after a firing, not a manual start hours later.
                if (_resumePending && (DateTime.UtcNow - _lastFireUtc).TotalMinutes > 15)
                {
                    _log($"WATCHDOG: saved resume from {_lastFireUtc.ToLocalTime():HH:mm:ss} is stale; not resuming.");
                    _resumePending = false; Save();
                }
                else if (_resumePending)
                    _log($"WATCHDOG: restarted by the watchdog at {_lastFireUtc.ToLocalTime():HH:mm:ss}; will resume '{(_resumeWant ? "mission run want" : "mission run")}' after login.");
            }
            catch (Exception ex) { _log("WATCHDOG: couldn't read watchdog.json: " + ex.Message); }
        }

        private void Save()
        {
            try
            {
                var o = new JObject
                {
                    ["fires"] = _fires,
                    ["lastFireUtc"] = _lastFireUtc == DateTime.MinValue ? null : (JToken)_lastFireUtc,
                    ["lastGapSeconds"] = Math.Round(_lastGap, 1),
                    ["resume"] = _resumePending,
                    ["resumeWant"] = _resumeWant,
                };
                File.WriteAllText(_path, o.ToString());
            }
            catch (Exception ex) { _log("WATCHDOG: couldn't write watchdog.json: " + ex.Message); }
        }
    }
}
