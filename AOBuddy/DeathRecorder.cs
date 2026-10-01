using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AOSharp.Clientless;
using AOSharp.Common.GameData;
using SmokeLounge.AOtomation.Messaging.Messages;

namespace AOBuddy
{
    /// <summary>
    /// DEATH RECORDER (owner, 2026-10-01). After a death the bot waited 35 minutes at the terminal for rez sickness
    /// that had ended in game: his TemporarySkillReduction (247) stayed at the reclaim's 5000 and his max HP at the
    /// sick 342 until a relog read 0 and 498. The server sent the end; the SDK lost it - and no recording covered that
    /// moment (the mission recorder closes at the reclaim, 5 s after the death). This records it: every packet, both
    /// ways, from the death until <see cref="RecordMinutes"/> later, and logs as they come every Stat on him carrying
    /// 247 or MaxHealth (1), every Buff (a buff ending) on him, and at the end how many of each message type were
    /// addressed to him.
    ///
    /// Files, in Plugins/AOBuddy/missions/records/: death-&lt;yyyyMMdd-HHmmss&gt;.pkt, the mission recorder's format:
    /// [1 byte dir: 0 server, 1 client][8 bytes ms since the death][4 bytes length][the packet], big-endian.
    /// </summary>
    public sealed class DeathRecorder
    {
        public const int RecordMinutes = 25;

        private readonly BotContext _ctx;
        private readonly string _dir;
        private readonly object _lock = new object();
        private FileStream _fs;
        private string _name;
        private DateTime _startedAt;
        private int _packets;
        private readonly Dictionary<uint, int> _toMe = new Dictionary<uint, int>();
        private int _lastTsr = int.MinValue, _lastMaxHp = int.MinValue;

        public DeathRecorder(BotContext ctx, string pluginDir)
        {
            _ctx = ctx;
            _dir = Path.Combine(pluginDir, "missions", "records");
        }

        public bool Recording { get { lock (_lock) return _fs != null; } }

        private static int BE32(byte[] b, int p) => (b[p] << 24) | (b[p + 1] << 16) | (b[p + 2] << 8) | b[p + 3];

        /// <summary>Update thread: he died. A death while still recording the last one starts a new file.</summary>
        public void Start()
        {
            lock (_lock)
            {
                if (_fs != null) CloseLocked("died again");
                try
                {
                    Directory.CreateDirectory(_dir);
                    _name = $"death-{DateTime.Now:yyyyMMdd-HHmmss}.pkt";
                    _fs = new FileStream(Path.Combine(_dir, _name), FileMode.Create, FileAccess.Write, FileShare.Read);
                    _startedAt = DateTime.UtcNow; _packets = 0; _toMe.Clear();
                    _lastTsr = int.MinValue; _lastMaxHp = int.MinValue;
                    _ctx.Log($"DEATHREC: recording every packet for {RecordMinutes} minutes to {_name}.");
                }
                catch (Exception ex) { _ctx.Log("DEATHREC: could not start: " + ex.Message); _fs = null; }
            }
        }

        /// <summary>Network thread: every packet, both ways.</summary>
        public void OnPacket(byte[] p, bool server)
        {
            if (p == null) return;
            lock (_lock)
            {
                if (_fs == null) return;
                long ms = (long)(DateTime.UtcNow - _startedAt).TotalMilliseconds;
                try
                {
                    var h = new byte[13];
                    h[0] = (byte)(server ? 0 : 1);
                    for (int i = 0; i < 8; i++) h[1 + i] = (byte)(ms >> (56 - 8 * i));
                    for (int i = 0; i < 4; i++) h[9 + i] = (byte)(p.Length >> (24 - 8 * i));
                    _fs.Write(h, 0, h.Length); _fs.Write(p, 0, p.Length); _fs.Flush();
                    _packets++;
                }
                catch (Exception ex) { _ctx.Log("DEATHREC: write failed: " + ex.Message); CloseLocked("write failed"); return; }

                // Server N3 messages about him: header PacketType 0x0A at 2-3, N3 type at 16, identity instance at 24.
                if (!server || p.Length < 29 || p[2] != 0 || p[3] != 0x0A || BE32(p, 24) != Client.LocalDynelId) return;
                uint type = (uint)BE32(p, 16);
                _toMe[type] = _toMe.TryGetValue(type, out int n) ? n + 1 : 1;
                if (type == (uint)N3MessageType.Stat && p.Length >= 33)
                {
                    int count = BE32(p, 29);
                    var hits = new List<string>();
                    for (int k = 0; k < count && 33 + 8 * k + 8 <= p.Length; k++)
                    {
                        int stat = BE32(p, 33 + 8 * k), value = BE32(p, 37 + 8 * k);
                        if (stat == (int)Stat.TemporarySkillReduction || stat == (int)Stat.MaxHealth) hits.Add($"{(Stat)stat}={value}");
                    }
                    if (hits.Count > 0) _ctx.Log($"DEATHREC: +{ms / 1000.0:0.0} s Stat on me: {string.Join(", ", hits)}.");
                }
                else if (type == (uint)N3MessageType.Buff)
                    _ctx.Log($"DEATHREC: +{ms / 1000.0:0.0} s Buff (a buff ended) on me: {BitConverter.ToString(p, 29).Replace("-", "")}.");
            }
        }

        /// <summary>Update thread, every frame: what the bot holds for 247 and max HP, logged when it changes.</summary>
        public void Tick(LocalPlayer me)
        {
            if (!Recording) return;
            if ((DateTime.UtcNow - _startedAt).TotalMinutes >= RecordMinutes) { lock (_lock) CloseLocked($"{RecordMinutes} minutes up"); return; }
            if (me == null) return;
            int tsr = me.TryGetStat(Stat.TemporarySkillReduction, out int t) ? t : -1;
            int max = me.TryGetStat(Stat.MaxHealth, out int m) ? m : -1;
            if (tsr != _lastTsr || max != _lastMaxHp)
            {
                double s = (DateTime.UtcNow - _startedAt).TotalSeconds;
                _ctx.Log($"DEATHREC: +{s:0.0} s the bot holds TemporarySkillReduction(247)={(tsr < 0 ? "not sent" : tsr.ToString())}, MaxHealth={(max < 0 ? "not sent" : max.ToString())}.");
                _lastTsr = tsr; _lastMaxHp = max;
            }
        }

        private void CloseLocked(string why)
        {
            try { _fs?.Dispose(); } catch { }
            _fs = null;
            string types = string.Join(", ", _toMe.OrderByDescending(kv => kv.Value)
                .Select(kv => $"{(Enum.IsDefined(typeof(N3MessageType), (int)kv.Key) ? ((N3MessageType)(int)kv.Key).ToString() : "0x" + kv.Key.ToString("X8"))} {kv.Value}"));
            _ctx.Log($"DEATHREC: closed {_name} ({why}): {_packets} packets. Server messages about me by type: {types}.");
        }
    }
}
