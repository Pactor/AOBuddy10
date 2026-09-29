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
    ///   HealthDamage      Identity = the one hit, Source = who did it, Delta = the signed change, SourceItem = the weapon's
    ///                     instance, or 0. A weapon's damage comes as an AttackInfo AND a HealthDamage carrying that
    ///                     AttackInfo's WeaponInstance (1,682 of 1,769 in the bot recordings, 2,245 of 2,586 in 50 retail
    ///                     captures); every other one carries 0 (87 and 341) - nukes, DoT ticks, procs - and has no
    ///                     AttackInfo at all. Those are kept too (<see cref="Blow.IsNano"/>); the weapon echoes are not
    ///                     (AttackInfo already has them).
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
            public readonly bool IsNano;  // damage with no weapon behind it (HealthDamage, SourceItem 0)
            public Blow(double time, Identity attacker, Identity target, int amount, bool nano = false) { Time = time; Attacker = attacker; Target = target; Amount = amount; IsNano = nano; }
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
        internal static void AddNano(Identity attacker, Identity target, int amount) => Record(attacker, target, Math.Max(0, amount), true);

        private static void Record(Identity attacker, Identity target, int amount, bool nano = false)
        {
            if (attacker.Instance == 0 || target.Instance == 0) return;
            double now = Now;
            lock (_lock)
            {
                _blows.Add(new Blow(now, attacker, target, amount, nano));
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
