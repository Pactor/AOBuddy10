using System;
using System.Collections.Generic;
using System.Diagnostics;
using AOSharp.Common.GameData;

namespace AOSharp.Clientless
{
    /// <summary>
    /// Every blow the server tells us about, kept for a minute: who struck whom, for how much, when.
    ///
    /// Wire (mission recording rec-2224975-20260929-055443, the Tac-V85 fight at 02:42-02:56):
    ///   AttackInfo        Identity = attacker, Target = the one hit, Amount = the damage it did
    ///   SpecialAttackInfo Identity = attacker, Target = the one hit, Amount = the damage it did
    ///   MissedAttackInfo  Attacker / Defender, no damage (kept as a 0 so a swing that missed still counts as a swing)
    ///   Attack            Identity = attacker, Target = who it goes for: the START of its swinging, kept as Amount -1
    ///                     (<see cref="Blow.IsEngage"/>). A rate timed from the first blow instead reads high: a Piercer
    ///                     Scorpiod attacked at 02:04.5 and landed its first blow at 02:08.0 (rec-2224977-20260929-064422);
    ///                     40 + 90 over the 2.8 s from that blow read 41/s, over the 6.3 s it had been swinging it is 21/s.
    /// The amounts are what landed: the mob's Health went 1472 -> 1366 -> 1283 on hits of 106 and 83, and 623 -> 508
    /// on 115. HP itself is not tracked here - the server pushes the absolute Health of the fighter and his target
    /// about every second (Stat 27) and the SDK already keeps it.
    /// </summary>
    public static class Blows
    {
        public readonly struct Blow
        {
            public readonly double Time;
            public readonly Identity Attacker, Target;
            public readonly int Amount;   // 0 = a miss, -1 = the Attack that started the swinging
            public Blow(double time, Identity attacker, Identity target, int amount) { Time = time; Attacker = attacker; Target = target; Amount = amount; }
            public bool IsEngage => Amount < 0;
            public int Damage => Math.Max(0, Amount);
        }

        public const double KeepSeconds = 60;
        private static readonly Stopwatch _clock = Stopwatch.StartNew();
        private static readonly List<Blow> _blows = new List<Blow>();
        private static readonly object _lock = new object();

        /// <summary>Seconds on the clock the blows are stamped with.</summary>
        public static double Now => _clock.Elapsed.TotalSeconds;

        internal static void Add(Identity attacker, Identity target, int amount) => Record(attacker, target, Math.Max(0, amount));
        internal static void Engage(Identity attacker, Identity target) => Record(attacker, target, -1);

        private static void Record(Identity attacker, Identity target, int amount)
        {
            if (attacker.Instance == 0 || target.Instance == 0) return;
            double now = Now;
            lock (_lock)
            {
                _blows.Add(new Blow(now, attacker, target, amount));
                int drop = 0;
                while (drop < _blows.Count && now - _blows[drop].Time > KeepSeconds) drop++;
                if (drop > 0) _blows.RemoveRange(0, drop);
            }
        }

        /// <summary>The blows since a time (on <see cref="Now"/>'s clock), oldest first.</summary>
        public static List<Blow> Since(double time)
        {
            lock (_lock) return _blows.FindAll(b => b.Time >= time);
        }

        public static void Clear() { lock (_lock) _blows.Clear(); }
    }
}
