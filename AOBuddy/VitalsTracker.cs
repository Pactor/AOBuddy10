using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;
using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;

namespace AOBuddy
{
    /// <summary>
    /// VITALS — how the owner and teammates are actually doing, and how sure we are of it.
    ///
    /// Another player's HP on his dynel is a snapshot: the spawn packet sets it, a HealthDamage moves it when a
    /// hit or heal lands, and nothing ever reports his regen (and nothing at all reports his nano). So the dynel
    /// kept saying "owner 49%" for 22 minutes while he stood in town at full health, and the bot stimmed him on
    /// that number over and over. Here every report is stamped with WHEN it arrived, from whichever source:
    ///   * team window (TeamMemberInfoMessage) — HP and nano, sent as they move, only when we are teamed
    ///   * Stat / HealthDamage                  — whenever the server happens to push them
    ///   * InfoPacket                           — the reply to our own InfoRequest, HP only, works solo
    /// and a character we can see whose last report is getting old is asked again (InfoRequest), so the owner
    /// is tracked whether or not he is on our team. A heal decision only acts on a FRESH reading (TryFreshHp):
    /// a stale low number is a question, never a reason to stim.
    ///
    /// The bot's own vitals aren't tracked here: the server keeps the local player's stats live itself.
    /// </summary>
    public sealed class VitalsTracker
    {
        // A reading this recent is trusted for a heal decision. Poll faster than that so a visible
        // character never drops out of "fresh" just because nothing changed.
        public const double FreshSec = 4.0;
        private const double PollSec = 2.0;
        private const int MaxRequestsPerPoll = 2;   // spread a full team over a few frames, no bursts

        private sealed class Entry
        {
            public int Hp = -1, MaxHp = -1, Nano = -1, MaxNano = -1;
            public double HpAt = -999, NanoAt = -999;
            public string HpSource = "";
            public double AskedAt = -999;
            public double NotBefore = -999;   // readings at or before this are known to be out of date
        }

        private readonly BotContext _ctx;
        private readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>();   // by Identity.Instance
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private double Now => _clock.Elapsed.TotalSeconds;

        // Protocol check: InfoRequest replies are what makes solo tracking work. Say so once if they never come.
        private int _requestsSent, _infoReplies;
        private bool _warnedNoReplies, _loggedFirstReply;

        public VitalsTracker(BotContext ctx) { _ctx = ctx; }

        private Entry Get(Identity id)
        {
            if (!_entries.TryGetValue(id.Instance, out Entry e)) { e = new Entry(); _entries[id.Instance] = e; }
            return e;
        }

        // ---- Feed (Client.MessageReceived, on the update thread) ------------------

        public void OnMessage(Message m)
        {
            switch (m?.Body)
            {
                case TeamMemberInfoMessage t:
                    SetHp(t.Character, t.CurrentHealth, t.MaxHealth, "team");
                    SetNano(t.Character, t.CurrentNano, t.MaxNano);
                    break;
                case HealthDamageMessage hd:
                    if (hd.Stat == Stat.CurrentNano) SetNano(hd.Target, hd.TargetHp, -1);
                    else SetHp(hd.Target, hd.TargetHp, -1, "dmg");
                    break;
                case StatMessage s when s.Stats != null:
                    foreach (var st in s.Stats)
                    {
                        if (st.Value1 == Stat.Health) SetHp(s.Identity, (int)st.Value2, -1, "stat");
                        else if (st.Value1 == Stat.MaxHealth) Get(s.Identity).MaxHp = (int)st.Value2;
                        else if (st.Value1 == Stat.CurrentNano) SetNano(s.Identity, (int)st.Value2, -1);
                        else if (st.Value1 == Stat.MaxNanoEnergy) Get(s.Identity).MaxNano = (int)st.Value2;
                    }
                    break;
                case InfoPacketMessage ip when ip.Info is CharacterInfoPacket ci:
                    _infoReplies++;
                    SetHp(ip.Identity, ci.Health, ci.MaxHealth, "info");
                    if (!_loggedFirstReply)
                    {
                        _loggedFirstReply = true;
                        _ctx.Log($"VITALS: InfoRequest replies arrive — {ip.Identity.Instance} hp={ci.Health}/{ci.MaxHealth}.");
                    }
                    break;
            }
        }

        private void SetHp(Identity id, int hp, int max, string source)
        {
            if (hp < 0) return;
            Entry e = Get(id);
            e.Hp = hp; e.HpAt = Now; e.HpSource = source;
            if (max > 0) e.MaxHp = max;
        }

        private void SetNano(Identity id, int nano, int max)
        {
            if (nano < 0) return;
            Entry e = Get(id);
            e.Nano = nano; e.NanoAt = Now;
            if (max > 0) e.MaxNano = max;
        }

        // ---- Tracking ---------------------------------------------------------------

        // Ask the server again about every character we care about (owner + teammates) that is in view and
        // hasn't been reported on for a while. The team feed keeps teammates fresh on its own, so in a team
        // this mostly stays quiet; solo it is the only thing keeping the owner's HP current.
        public void Poll(LocalPlayer me, PlayerChar owner)
        {
            if (me == null) return;
            double now = Now;
            int sent = 0;
            foreach (SimpleChar c in Tracked(me, owner))
            {
                Entry e = Get(c.Identity);
                bool stale = now - e.HpAt >= PollSec || e.HpAt <= e.NotBefore;
                if (!stale || now - e.AskedAt < PollSec) continue;
                Client.InfoRequest(c.Identity);
                e.AskedAt = now;
                _requestsSent++;
                if (++sent >= MaxRequestsPerPoll) break;
            }

            if (!_warnedNoReplies && _infoReplies == 0 && _requestsSent >= 10)
            {
                _warnedNoReplies = true;
                _ctx.Log($"VITALS: {_requestsSent} InfoRequests, no InfoPacket reply — out of a team the owner's HP is only known when a hit or heal lands on him.");
            }
        }

        private static IEnumerable<SimpleChar> Tracked(LocalPlayer me, PlayerChar owner)
        {
            if (owner != null) yield return owner;
            foreach (TeamMember tm in Team.Members)
            {
                if (tm.Identity == me.Identity || (owner != null && tm.Identity == owner.Identity)) continue;
                SimpleChar c = DynelManager.Characters.FirstOrDefault(x => x.Identity == tm.Identity);
                if (c != null) yield return c;
            }
        }

        // We just healed him — whatever we knew is out of date until the next report.
        public void Invalidate(Identity id) => Get(id).NotBefore = Now;

        public void Clear() => _entries.Clear();

        // ---- Reads ------------------------------------------------------------------

        // Health% from a reading recent enough to act on. False = we don't know right now (never reported,
        // gone quiet, or healed since): the caller must not heal on a guess; Poll is already asking again.
        public bool TryFreshHp(SimpleChar c, out int pct, out double ageSec)
        {
            pct = 100; ageSec = double.MaxValue;
            if (c == null || !_entries.TryGetValue(c.Identity.Instance, out Entry e) || e.Hp < 0) return false;
            ageSec = Now - e.HpAt;
            if (ageSec > FreshSec || e.HpAt <= e.NotBefore) return false;
            int max = e.MaxHp > 0 ? e.MaxHp : (c.TryGetStat(Stat.MaxHealth, out int m) ? m : 0);
            if (max <= 0) return false;
            pct = Pct(e.Hp, max);
            return true;
        }

        public bool TryFreshNano(SimpleChar c, out int pct)
        {
            pct = 100;
            if (c == null || !_entries.TryGetValue(c.Identity.Instance, out Entry e) || e.Nano < 0) return false;
            if (Now - e.NanoAt > FreshSec || e.NanoAt <= e.NotBefore || e.MaxNano <= 0) return false;
            pct = Pct(e.Nano, e.MaxNano);
            return true;
        }

        // For logs: "49% info 1.2s" / "49% dmg 312s STALE" / "?".
        public string Describe(SimpleChar c)
        {
            if (c == null) return "n/a";
            if (!_entries.TryGetValue(c.Identity.Instance, out Entry e) || e.Hp < 0) return "?";
            double age = Now - e.HpAt;
            int max = e.MaxHp > 0 ? e.MaxHp : (c.TryGetStat(Stat.MaxHealth, out int m) ? m : 0);
            string pct = max > 0 ? Pct(e.Hp, max) + "%" : e.Hp + "hp";
            bool fresh = TryFreshHp(c, out _, out _);
            return $"{pct} {e.HpSource} {age:0.0}s{(fresh ? "" : " STALE")}";
        }

        private static int Pct(int cur, int max) => (int)Math.Max(0, Math.Min(100, 100L * cur / max));
    }
}