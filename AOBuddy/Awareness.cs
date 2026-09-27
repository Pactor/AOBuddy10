using System;
using System.Collections.Generic;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;

namespace AOBuddy
{
    /// <summary>
    /// AWARENESS (owner, 2026-09-27: "he should always know how many mobs are near him, how many follow him, he needs to
    /// be aware"). One picture of the hostiles round him, rebuilt twice a second, that every decision reads:
    ///   Near      - live hostiles within 40 m (a mob, or anything that has fought him or a pet this zone);
    ///   OnUs      - those fighting him or one of his pets (every blow marks who it is on, AttackInfo);
    ///   Following - those that keep up with him: over the last 4 s he moved 4 m or more, they moved 3 m or more,
    ///               and they are no further from him than they were (Wailing Wastes 06:32: two Watchers 5-15 m behind
    ///               for 30 s, which the monitor showed and the bot never read).
    /// A change is logged ("AWARE: ..."), and /status carries the summary.
    /// </summary>
    public sealed class Awareness
    {
        public sealed class Seen
        {
            public SimpleChar Mob; public float Dist; public int Level; public bool OnUs, Following;
        }

        private const float NearRange = 40f;
        private const double Window = 4.0;
        private readonly Action<string> _log;
        private readonly Queue<(double t, Vector3 me)> _myTrail = new Queue<(double, Vector3)>();
        private readonly Dictionary<Identity, Queue<(double t, Vector3 p, float d)>> _trail = new Dictionary<Identity, Queue<(double, Vector3, float)>>();
        private readonly HashSet<Identity> _foes = new HashSet<Identity>();   // fought him or a pet in this zone
        private int _pf = -1;
        private double _at = -1, _loggedAt = -99;
        private string _last = "";

        public List<Seen> Near { get; private set; } = new List<Seen>();
        public IEnumerable<Seen> OnUs => Near.Where(s => s.OnUs);
        public IEnumerable<Seen> Following => Near.Where(s => s.Following);
        public int OnUsCount => Near.Count(s => s.OnUs);
        public int FollowingCount => Near.Count(s => s.Following);

        public Awareness(Action<string> log) { _log = log; }

        public void Tick(LocalPlayer me, double clock, bool inMission)
        {
            if (me == null || clock - _at < 0.5) return;
            _at = clock;
            int pf = (int)Playfield.ModelId;
            if (pf != _pf) { _pf = pf; _foes.Clear(); _trail.Clear(); _myTrail.Clear(); _shadow.Clear(); }

            var mine = me.Transform.Position;
            _myTrail.Enqueue((clock, mine));
            while (_myTrail.Count > 0 && clock - _myTrail.Peek().t > Window + 0.6) _myTrail.Dequeue();
            var myThen = _myTrail.Peek();
            bool iMoved = clock - myThen.t >= Window - 0.6 && Movement.Flat(myThen.me, mine) >= 4f;

            var guard = CombatController.Guarded(me, null); guard.Add(me.Identity);
            int myLvl = MissionRun.Strength(me);   // his level or his strongest pet's
            var near = new List<Seen>();
            var here = new HashSet<Identity>();
            foreach (var n in DynelManager.Npcs)
            {
                if (n == null || n.Owner.HasValue || guard.Contains(n.Identity)) continue;
                if (n.TryGetStat(Stat.Health, out int hp) && hp <= 0) continue;
                bool onUs = n.FightingIdentity.HasValue && guard.Contains(n.FightingIdentity.Value);
                if (onUs) _foes.Add(n.Identity);
                if (!onUs && !_foes.Contains(n.Identity) && !MissionRun.IsMob(n, inMission)) continue;
                float d = me.DistanceFrom(n);
                if (d > NearRange)
                {
                    _trail.Remove(n.Identity);
                    // One he knows is after him stays known past 40 m: its position keeps updating from the server's
                    // paths and set-positions (DynelManager), and it stays on the list until it has been gone 20 s.
                    if (_shadow.TryGetValue(n.Identity, out var far) && clock < far.until)
                    {
                        far.seen = clock; far.pos = n.Transform.Position; here.Add(n.Identity);
                        n.TryGetStat(Stat.Level, out int fl);
                        near.Add(new Seen { Mob = n, Dist = d, Level = fl, OnUs = onUs, Following = true });
                    }
                    continue;
                }
                here.Add(n.Identity);
                if (!_trail.TryGetValue(n.Identity, out var q)) _trail[n.Identity] = q = new Queue<(double, Vector3, float)>();
                q.Enqueue((clock, n.Transform.Position, d));
                while (q.Count > 0 && clock - q.Peek().t > Window + 0.6) q.Dequeue();
                var then = q.Peek();
                bool keptUp = iMoved && clock - then.t >= Window - 0.6 && Movement.Flat(then.p, n.Transform.Position) >= 3f && d <= then.d + 1f && d <= 30f;
                bool following = Shadow(n.Identity, clock, mine, n.Transform.Position, d, keptUp);
                n.TryGetStat(Stat.Level, out int lvl);
                near.Add(new Seen { Mob = n, Dist = d, Level = lvl, OnUs = onUs, Following = following });
            }
            LogEngagements(me, guard);
            foreach (var id in _trail.Keys.ToList()) if (!here.Contains(id)) _trail.Remove(id);
            foreach (var id in _shadow.Keys.ToList()) if (clock - _shadow[id].seen > Forget) _shadow.Remove(id);
            Near = near.OrderBy(s => s.Dist).ToList();

            string now = Summary();
            if (now != _last && (clock - _loggedAt > 2 || OnUsCount + FollowingCount > 0))
            {
                _last = now; _loggedAt = clock;
                string who = string.Join(", ", Near.Where(s => s.OnUs || s.Following).Take(4)
                    .Select(s => $"'{s.Mob.Name}' lvl {s.Level} {s.Dist:0} m{(s.OnUs ? " on us" : "")}{(s.Following ? " following" : "")}{(s.Level > myLvl + 5 ? " (too strong)" : "")}"));
                _log($"AWARE: {now}{(who.Length > 0 ? " - " + who : "")}.");
            }
        }

        // SHADOWING (owner, 2026-09-27: "IF a mob stays within, or returns to a certain area around him, he is AWARE of
        // it"). Jeffery Joor stayed 4-10 m behind him for a minute, in and out of the building, and the 4-s rule above
        // flickered on and off. A mob is following once, over the last 20 s while he walked 10 m or more, it spent
        // 60% of the time within 15 m of him, or came back within 15 m twice; or the 4-s rule saw it keep up. It stays
        // following until it has been out of 40 m (or unseen) for 20 s.
        private const float Close = 15f;
        private const double Span = 20.0, Forget = 20.0;
        private sealed class Track { public Queue<(double t, Vector3 me, bool close)> S = new Queue<(double, Vector3, bool)>(); public double seen, until = -1; public Vector3 pos; public int returns; public bool wasClose; }
        private readonly Dictionary<Identity, Track> _shadow = new Dictionary<Identity, Track>();

        private bool Shadow(Identity id, double clock, Vector3 mine, Vector3 pos, float d, bool keptUp)
        {
            if (!_shadow.TryGetValue(id, out var tr)) _shadow[id] = tr = new Track();
            tr.seen = clock; tr.pos = pos;
            bool close = d <= Close;
            tr.S.Enqueue((clock, mine, close));
            while (tr.S.Count > 0 && clock - tr.S.Peek().t > Span) tr.S.Dequeue();
            if (close && !tr.wasClose && tr.S.Count > 1) tr.returns++;
            tr.wasClose = close;
            var first = tr.S.Peek();
            float walked = Movement.Flat(first.me, mine);
            double share = tr.S.Count(x => x.close) / (double)tr.S.Count;
            bool shadowing = walked >= 10f && clock - first.t >= Span * 0.5 && (share >= 0.6 || tr.returns >= 2);
            if (keptUp || shadowing) tr.until = clock + Forget;
            return clock < tr.until;
        }

        /// <summary>Where each mob he knows is after him was last seen, including ones out of sight (gone under 20 s).</summary>
        public IEnumerable<(Identity id, Vector3 pos, double ago)> Trackers(double clock) =>
            _shadow.Where(kv => clock < kv.Value.until).Select(kv => (kv.Key, kv.Value.pos, clock - kv.Value.seen));

        // WHO STARTED IT (owner, 2026-09-27): Longest Road mobs never aggro an Omni first, yet a Hammer Bull train built up
        // on him at 11:45 with no Attack from the bot; the pets' own swings never reached the log. Every change of fighting
        // target is logged for him and each pet (ENGAGE) and for each mob turning onto one of us (AGGRO), so the order
        // of the lines shows who opened.
        private readonly Dictionary<Identity, Identity?> _fighting = new Dictionary<Identity, Identity?>();

        private void LogEngagements(LocalPlayer me, HashSet<Identity> guard)
        {
            string Who(Identity id) => id == me.Identity ? "me" : (DynelManager.Characters.FirstOrDefault(c => c.Identity == id)?.Name ?? id.ToString());
            string Dist(Identity id) { var c = DynelManager.Characters.FirstOrDefault(x => x.Identity == id); return c != null ? $"{me.DistanceFrom(c):0} m" : "?"; }
            var seen = new HashSet<Identity>();
            foreach (var c in DynelManager.Characters)
            {
                if (c == null) continue;
                bool ours = guard.Contains(c.Identity);
                var ft = c.FightingIdentity;
                if (!ours && !(ft.HasValue && guard.Contains(ft.Value)) && !_fighting.ContainsKey(c.Identity)) continue;
                seen.Add(c.Identity);
                _fighting.TryGetValue(c.Identity, out var was);
                if (Nullable.Equals(was, ft)) continue;
                _fighting[c.Identity] = ft;
                if (ours && ft.HasValue) _log($"ENGAGE: {Who(c.Identity)} ({Dist(c.Identity)}) -> '{Who(ft.Value)}' ({Dist(ft.Value)}).");
                else if (!ours && ft.HasValue && guard.Contains(ft.Value)) _log($"AGGRO: '{c.Name}' ({Dist(c.Identity)}) on {Who(ft.Value)}.");
            }
            foreach (var id in _fighting.Keys.ToList()) if (!seen.Contains(id)) _fighting.Remove(id);
        }

        public string Summary() => $"{OnUsCount} on us, {FollowingCount} following, {Near.Count} near";

        /// <summary>The nearest one fighting him/a pet or following him, or null.</summary>
        public SimpleChar Chaser() => Near.FirstOrDefault(s => s.OnUs || s.Following)?.Mob;
    }
}
