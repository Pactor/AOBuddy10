using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace AOBuddyMonitor
{
    /// <summary>
    /// The dashboard's wiring — deliberately no MVVM framework (the repo's culture is dependency-light;
    /// a monitor is a poller and a paint method, not a data-binding problem). One background thread polls
    /// the bot's API once a second (/status, /nav, /log; /inventory every 10 s or the moment /status's
    /// freeSlots moves) and hands plain objects to these handlers on the UI thread. The tell box at the
    /// bottom is the one control that writes: POST /command, replies into the log.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MonitorConfig _cfg;
        private readonly BotClient _bot;
        private readonly MapRender _render;
        private readonly MapView _map;
        private readonly Thread _poll;
        private volatile bool _running = true;

        // log state: the bot's ring seq we have consumed, and the command history
        private int _logSeq = -1;
        private readonly List<string> _cmdHistory = new List<string>();
        private int _cmdHistoryAt = -1;

        // xp/s is measured here: the bot reports the level and XP into it, the monitor diffs them
        private int _xpLevel, _xpInto;
        private DateTime _xpAt = DateTime.MinValue;
        private double _xpPerSec = double.NaN;

        private DateTime _nextInventory = DateTime.MinValue;
        private int _lastFreeSlots = int.MinValue;

        public MainWindow()
        {
            InitializeComponent();
            _cfg = MonitorConfig.Load();
            _bot = new BotClient(_cfg);
            _render = new MapRender(_cfg.PluginDir);
            _map = new MapView(_render);
            MapHost.Children.Add(_map);
            _render.Rendered += pf => Dispatcher.UIThread.Post(() => _map.InvalidateVisual());
            LogLine("monitor up — " + (_cfg.PluginDir.Length > 0 ? "nav data: " + _cfg.PluginDir : "no plugin dir found; maps will be grids"));
            LogLine("waiting for the bot on " + _cfg.Base);
            _poll = new Thread(PollLoop) { IsBackground = true, Name = "AOBuddyMonitor poll" };
            _poll.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _running = false;
            base.OnClosed(e);
        }

        // ---- the poller ---------------------------------------------------------------------------------

        private void PollLoop()
        {
            while (_running)
            {
                try
                {
                    var st = _bot.GetStatus();
                    if (st == null)
                    {
                        Dispatcher.UIThread.Post(() => Conn(false));
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() => { Conn(true); ApplyStatus(st); });
                        var nav = _bot.GetNav();
                        if (nav != null) Dispatcher.UIThread.Post(() => _map.SetNav(nav));
                        PollLog();
                        bool due = DateTime.UtcNow >= _nextInventory || st.FreeSlots != _lastFreeSlots;
                        if (due)
                        {
                            _nextInventory = DateTime.UtcNow.AddSeconds(10);
                            _lastFreeSlots = st.FreeSlots;
                            var inv = _bot.GetInventory();
                            if (inv != null) Dispatcher.UIThread.Post(() => ApplyInventory(inv));
                        }
                    }
                }
                catch { /* one bad tick must never take the poller down */ }
                Thread.Sleep(1000);
            }
        }

        private void PollLog()
        {
            var (seq, lines) = _bot.GetLog(_logSeq);
            if (seq < 0) return;
            if (seq < _logSeq)
            {
                // the seq counter went backwards: the bot restarted with an empty ring — start over
                _logSeq = -1;
                Dispatcher.UIThread.Post(() => LogLine("[bot restarted — log ring reset]", Local()));
                var (seq2, lines2) = _bot.GetLog(-1);
                if (seq2 >= 0) { _logSeq = seq2; Post(lines2); }
                return;
            }
            _logSeq = seq;
            Post(lines);
            void Post(List<(int Seq, string T, string Line)> ls)
            {
                if (ls.Count > 0) Dispatcher.UIThread.Post(() =>
                {
                    foreach (var l in ls) LogLine(l.T + "  " + l.Line, Server());
                });
            }
        }

        // ---- applying what the poll brought back --------------------------------------------------------

        private void Conn(bool up)
        {
            ConnTxt.Text = up ? "● connected" : "○ no bot";
            ConnTxt.Foreground = up ? new SolidColorBrush(Color.FromRgb(0x5f, 0xbf, 0x6f)) : new SolidColorBrush(Color.FromRgb(0xd6, 0x5b, 0x5b));
            if (up) ConnTxt.Text += " " + DateTime.Now.ToString("HH:mm:ss");
            else ConnTxt.Text += " — " + _cfg.Base;
        }

        private static readonly Dictionary<string, Color> KindColor = new Dictionary<string, Color>
        {
            ["dead"] = Color.FromRgb(0xb0, 0x40, 0x40),
            ["missionrun"] = Color.FromRgb(0x3f, 0x8f, 0x4f),
            ["chewy"] = Color.FromRgb(0x8a, 0x5f, 0xb0),
            ["overland"] = Color.FromRgb(0x3f, 0x8f, 0x8f),
            ["hunt"] = Color.FromRgb(0xb0, 0x7a, 0x3f),
            ["resupply"] = Color.FromRgb(0xb0, 0x9a, 0x3f),
            ["travel"] = Color.FromRgb(0x4f, 0x6f, 0xb0),
            ["walk"] = Color.FromRgb(0x7a, 0x7a, 0x7a),
        };

        private void ApplyStatus(BotClient.Status st)
        {
            // task chip + text
            TaskKind.Text = st.Task.Phase ?? st.Task.Kind;
            TaskChip.Background = new SolidColorBrush(KindColor.TryGetValue(st.Task.Kind, out var c) ? c : Color.FromRgb(0x55, 0x55, 0x55));
            TaskText.Text = st.Task.Text.Length > 0 ? st.Task.Text : st.Behavior;
            TaskDetail.Text = st.Task.Detail ?? st.Behavior;
            ToolTip.SetTip(this, st.Task.Detail ?? "");

            // vitals: the bar takes the percentage, the label keeps the raw numbers
            HpBar.Value = Math.Clamp(st.HpPct < 0 ? 0 : st.HpPct, 0, 100);
            HpTxt.Text = st.HpPct < 0 ? "HP ?" : $"HP {st.HpCur}/{st.HpMax} · {st.HpPct}%";
            NanoBar.Value = Math.Clamp(st.NanoPct < 0 ? 0 : st.NanoPct, 0, 100);
            NanoTxt.Text = st.NanoPct < 0 ? "Nano ?" : $"Nano {st.NanoCur}/{st.NanoMax} · {st.NanoPct}%";

            // xp: into-level progress plus a measured rate (reset on ding)
            if (st.Level > 0)
            {
                var now = DateTime.Now;
                if (_xpAt != DateTime.MinValue && st.Level == _xpLevel && st.XpInto > _xpInto && now > _xpAt)
                {
                    var rate = (st.XpInto - _xpInto) / (now - _xpAt).TotalSeconds;
                    _xpPerSec = double.IsNaN(_xpPerSec) ? rate : _xpPerSec * 0.7 + rate * 0.3;   // smooth it
                }
                if (st.Level != _xpLevel || _xpAt == DateTime.MinValue) { _xpLevel = st.Level; _xpPerSec = double.NaN; }
                _xpInto = st.XpInto; _xpAt = now;
            }
            XpBar.Value = Math.Clamp(st.XpPctNext < 0 ? 0 : st.XpPctNext, 0, 100);
            XpTxt.Text = st.Level > 0
                ? $"Lvl {st.Level} · {st.XpPctNext}% to next" + (double.IsNaN(_xpPerSec) ? "" : $" · +{_xpPerSec:0}/s")
                : "Lvl ?";

            // the fight he is in: the target's vitals as the bot reads them (its own hpPct when the server
            // has told us; nano only when the thing has any)
            if (st.Target != null && st.Target.HpPct >= 0)
            {
                TgtTxt.Foreground = new SolidColorBrush(Color.FromRgb(0xe8, 0xb0, 0xb0));
                TgtTxt.Text = $"→ {st.Target.Name} {st.Target.HpPct}%"
                    + (st.Target.NanoPct >= 0 ? $" · n{st.Target.NanoPct}%" : "")
                    + (st.Target.Dist > 0 ? $" · {st.Target.Dist:0} m" : "");
                TgtBar.Value = Math.Clamp(st.Target.HpPct, 0, 100);
            }
            else
            {
                TgtTxt.Foreground = new SolidColorBrush(Color.FromRgb(0x9a, 0x9a, 0x9a));
                TgtTxt.Text = "no target";
                TgtBar.Value = 0;
            }

            ZoneTxt.Text = st.Pf >= 0 ? $"{st.Zone} ({st.Pf})" : "—";
            if (st.Pos != null) PosTxt.Text = $"{st.Pos[0]:0}, {st.Pos[1]:0.0}, {st.Pos[2]:0}";
            MoneyTxt.Text = (st.Credits >= 0 ? Credits(st.Credits) : "") + (st.FreeSlots >= 0 ? $"  ·  {st.FreeSlots} free" : "");
            var flags = new List<string>();
            if (st.Dead) flags.Add("dead");
            if (st.InCombat) flags.Add("combat");
            if (st.Resting) flags.Add("rest");
            if (st.Casting) flags.Add("casting");
            if (st.InMission) flags.Add("in mission");
            FlagsTxt.Text = string.Join(" · ", flags);

            // pets
            PetsPanel.Children.Clear();
            if (st.Pets.Count == 0) PetsPanel.Children.Add(new TextBlock { Text = "none", Foreground = new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77)) });
            foreach (var p in st.Pets)
            {
                // one line per pet, sized to the 270 px sidebar: name(80) + role(32) + bar(40) + "92% · 4 m"
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Avalonia.Thickness(0, 0, 8, 0) };
                row.Children.Add(new TextBlock { Text = p.Name, FontWeight = FontWeight.Bold, Width = 80, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis });
                row.Children.Add(new TextBlock { Text = RoleShort(p.Role), Foreground = new SolidColorBrush(Color.FromRgb(0x9a, 0x9a, 0x9a)), Width = 32, FontSize = 12 });
                var bar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 8, Value = Math.Clamp(p.HpPct < 0 ? 0 : p.HpPct, 0, 100), Margin=new Thickness(5), HorizontalAlignment = HorizontalAlignment.Stretch};
                if (p.HpPct >= 0 && p.HpPct < 35) bar.Foreground = new SolidColorBrush(Color.FromRgb(0xc9, 0x50, 0x50));
                row.Children.Add(bar);
                row.Children.Add(new TextBlock
                {
                    Text = (p.HpPct < 0 ? "?" : p.HpPct + "%") + (p.Dist > 0 ? " · " + p.Dist.ToString("0") + " m" : ""),
                    FontSize = 11,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Width = 88,
                });
                PetsPanel.Children.Add(row);
            }
        }

        private static string RoleShort(string role)
        {
            switch (role)
            {
                case "Attack": return "⚔";
                case "Heal": return "✚";
                case "Support": return "◈";
                default: return role ?? "?";
            }
        }

        private static string Credits(long cr) => cr >= 1000000 ? cr / 1000000.0 + "M cr" : cr >= 1000 ? cr / 1000.0 + "k cr" : cr + " cr";

        private void ApplyInventory(BotClient.Inventory inv)
        {
            InvFreeTxt.Text = inv.FreeSlots >= 0 ? $"{inv.FreeSlots}/30 slots free" : "";
            InvPanel.Children.Clear();
            foreach (var it in inv.Items) InvPanel.Children.Add(ItemLine(it, 0));
            foreach (var bag in inv.Bags)
            {
                // an unopened bag claims nothing: the bot only learns a bag's contents by opening it,
                // so "21 free" there would be a guess dressed as a number
                InvPanel.Children.Add(new TextBlock
                {
                    Text = bag.Known ? $"{bag.Name}  ({bag.Free} free)" : $"{bag.Name}  (not opened — contents unknown)",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x9a, 0x9a, 0x9a)),
                    Margin = new Avalonia.Thickness(0, 6, 0, 0),
                });
                foreach (var it in bag.Items) InvPanel.Children.Add(ItemLine(it, 12));
            }
        }

        private static TextBlock ItemLine(BotClient.InvItem it, double indent) => new TextBlock
        {
            Text = $"{it.Name}  QL {it.Ql}" + (it.Count > 1 ? $"  ×{it.Count}" : ""),
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Avalonia.Thickness(indent, 0, 0, 0),
        };

        // ---- the log pane -------------------------------------------------------------------------------

        private static IBrush Server() => new SolidColorBrush(Color.FromRgb(0xc8, 0xc8, 0xc8));
        private static IBrush Local() => new SolidColorBrush(Color.FromRgb(0xd8, 0xb8, 0x4a));
        private static IBrush Reply() => new SolidColorBrush(Color.FromRgb(0x6f, 0xc0, 0xd0));

        private void LogLine(string text, IBrush brush = null)
        {
            // autoscroll only while the reader is at the bottom; scrolling up to read pauses it
            bool atBottom = LogScroll.Offset.Y + LogScroll.Viewport.Height >= LogScroll.Extent.Height - 16;
            LogPanel.Children.Add(new TextBlock { Text = text, FontSize = 11, Foreground = brush ?? Server(), TextWrapping = TextWrapping.Wrap });
            while (LogPanel.Children.Count > 1200) LogPanel.Children.RemoveAt(0);
            if (atBottom) LogScroll.ScrollToEnd();
        }

        // ---- the owner-tell box -------------------------------------------------------------------------

        private void CmdKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { Send(); e.Handled = true; }
            else if (e.Key == Key.Up && _cmdHistory.Count > 0)
            {
                _cmdHistoryAt = Math.Max(0, _cmdHistoryAt < 0 ? _cmdHistory.Count - 1 : _cmdHistoryAt - 1);
                CmdBox.Text = _cmdHistory[_cmdHistoryAt];
                CmdBox.CaretIndex = CmdBox.Text.Length;
                e.Handled = true;
            }
            else if (e.Key == Key.Down && _cmdHistoryAt >= 0)
            {
                _cmdHistoryAt++;
                if (_cmdHistoryAt >= _cmdHistory.Count) { _cmdHistoryAt = -1; CmdBox.Text = ""; }
                else CmdBox.Text = _cmdHistory[_cmdHistoryAt];
                e.Handled = true;
            }
        }

        private void CmdSendClick(object sender, RoutedEventArgs e) => Send();

        private void Send()
        {
            string text = (CmdBox.Text ?? "").Trim();
            if (text.Length == 0 || !CmdBox.IsEnabled) return;
            CmdBox.Text = "";
            CmdBox.IsEnabled = false;
            CmdState.Text = "sending — collecting replies…";
            if (!_cmdHistory.Contains(text)) _cmdHistory.Add(text);
            _cmdHistoryAt = -1;
            LogLine("› " + text, Local());
            System.Threading.Tasks.Task.Run(() =>
            {
                var replies = _bot.Command(text);
                Dispatcher.UIThread.Post(() =>
                {
                    foreach (var r in replies) LogLine("‹ " + r, Reply());
                    if (replies.Count == 0) LogLine("‹ (no reply)", Reply());
                    CmdState.Text = "";
                    CmdBox.IsEnabled = true;
                    CmdBox.Focus();
                });
            });
        }

        // ---- map toolbar --------------------------------------------------------------------------------

        private void FollowChanged(object sender, RoutedEventArgs e) => _map.Follow = CbFollow.IsChecked == true;
        private void StepsChanged(object sender, RoutedEventArgs e) => _map.ShowSteps = CbSteps.IsChecked == true;
    }
}
