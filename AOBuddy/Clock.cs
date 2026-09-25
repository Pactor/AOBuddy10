using System.Diagnostics;

namespace AOBuddy
{
    /// <summary>
    /// THE bot clock (R2.1): one monotonic time base shared by every system through ctx.Clock. Four
    /// incompatible bases used to coexist — Stopwatch (Main/Follow/Vitals), Environment.TickCount64
    /// (Mission/Overland), DateTime.UtcNow (Combat's set-asides) and accumulated tick quanta
    /// (Support's AdvanceClocks) — which made "how long since X" mean a different thing per file.
    /// Seconds/Milliseconds count from process start; every comparison in the bot is a DIFFERENCE
    /// against a stamp made on the same clock, so the epoch carries no meaning. Deliberately NOT used
    /// for: wall-calendar filename stamps (DateTime.Now) and timestamps persisted across restarts
    /// (MissionRun's danger.json keeps DateTime.UtcNow — this clock dies with the process).
    /// IClock is the seam so tests / the replay harness (R6.x) can drive a fake.
    /// </summary>
    public interface IClock
    {
        /// <summary>Monotonic seconds since the clock was created.</summary>
        double Seconds { get; }
        /// <summary>Monotonic milliseconds since the clock was created.</summary>
        double Milliseconds { get; }
    }

    public sealed class Clock : IClock
    {
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        public double Seconds => _sw.Elapsed.TotalSeconds;
        public double Milliseconds => _sw.Elapsed.TotalMilliseconds;
    }
}