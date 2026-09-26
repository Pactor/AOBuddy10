using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace AOBuddyMonitor
{
    /// <summary>
    /// The map: the bot's nav data as a picture, top-down (world X → right, Z → down — the axes the
    /// heightfield uses). The background is MapRender's terrain (or a coordinate grid when the playfield
    /// has none, e.g. mission interiors); the overlays are exactly what GET /nav reports: the hike route
    /// and its exit line, the building walk, the door, his trail faded by age, and the server's snap-backs
    /// (red server→local segments — where the world said no). Pan by drag (dragging turns Follow off),
    /// zoom at the wheel, coordinates under the cursor.
    /// </summary>
    public sealed class MapView : Control
    {
        private readonly MapRender _render;
        private readonly Dictionary<(int Pf, int Floor), WriteableBitmap> _bitmaps = new Dictionary<(int, int), WriteableBitmap>();
        private BotClient.Nav _nav;
        private bool _follow = true, _steps = true;
        private int? _floorOverride;                    // mission: the floor being peeked at (null = follow the bot)
        private int _lastInstance;
        private Vector2 _center = new Vector2(1024, 1024);   // world x,z the view is centred on
        private double _scale = 0.35;                        // DIPs per metre
        private Vector2? _hover;                             // world x,z under the cursor
        private bool _dragging;
        private Vector2 _dragPoint;                          // screen point the drag started at
        private Vector2 _dragCenter;

        private readonly Pen _stepsPen = new Pen(new SolidColorBrush(Color.FromArgb(150, 220, 220, 220)), 1) { LineJoin = PenLineJoin.Round };
        private readonly Pen _trailPen = new Pen(Brushes.White, 2) { LineJoin = PenLineJoin.Round };
        private readonly Pen _routePen = new Pen(new SolidColorBrush(Color.FromArgb(230, 120, 220, 120)), 2.5) { LineJoin = PenLineJoin.Round };
        private readonly Pen _walkPen = new Pen(new SolidColorBrush(Color.FromArgb(230, 90, 200, 255)), 2) { LineJoin = PenLineJoin.Round };
        private readonly Pen _exitPen = new Pen(new SolidColorBrush(Color.FromArgb(230, 255, 170, 60)), 2.5) { DashStyle = new DashStyle(new[] { 5.0, 4.0 }, 0) };
        private readonly Pen _snapPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 255, 70, 70)), 1.5);
        private readonly Pen _gridPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1);
        private readonly Pen _exitRingPen = new Pen(new SolidColorBrush(Color.FromArgb(230, 120, 220, 120)), 2);
        private readonly IPen _botPen = new Pen(Brushes.Lime, 2);

        public MapView(MapRender render)
        {
            _render = render;
            ClipToBounds = true;
            Focusable = true;
        }

        public bool Follow { get => _follow; set { _follow = value; InvalidateVisual(); } }
        public bool ShowSteps { get => _steps; set { _steps = value; InvalidateVisual(); } }
        public double ZoomPct => _scale * 100;

        /// <summary>New /nav snapshot (1 Hz). Keep the last one even when a poll fails, so the map never
        /// blanks out while the bot zones.</summary>
        public void SetNav(BotClient.Nav nav)
        {
            int inst = nav?.Mission?.Layout?.Instance ?? 0;
            if (inst != _lastInstance) { _lastInstance = inst; _floorOverride = null; }
            if (_floorOverride != null && nav?.Mission?.Floor != null && _floorOverride.Value == nav.Mission.Floor.Value)
                _floorOverride = null;                  // caught up with the bot's floor — follow again
            _nav = nav;
            InvalidateVisual();
        }

        // world-order BGRA → a bitmap with row 0 at the TOP (heightfield row 0 is world z=0, and north is up)
        private static WriteableBitmap MakeBitmap(byte[] bgra, int w, int h)
        {
            var bmp = new WriteableBitmap(new PixelSize(w, h), new Vector(96, 96), Avalonia.Platform.PixelFormat.Bgra8888);
            using (var fb = bmp.Lock())
            {
                int stride = w * 4;
                for (int row = 0; row < h; row++)
                    Marshal.Copy(bgra, (h - 1 - row) * stride, IntPtr.Add(fb.Address, row * stride), stride);
            }
            return bmp;
        }

        // world ⇄ screen: the view is centred on _center at _scale DIPs per metre. Screen Y is INVERTED
        // (up = +Z): the in-game map reads north-up, and heightfield row 0 is the south edge — so world
        // Z grows towards the TOP of the screen and the terrain bitmap is copied row-reversed.
        private Point ToScreen(float x, float z) => new Point((x - _center.X) * _scale + Bounds.Width / 2, (_center.Y - z) * _scale + Bounds.Height / 2);
        private Vector2 ToWorld(Point p) => new Vector2((float)((p.X - Bounds.Width / 2) / _scale + _center.X), (float)(_center.Y - (p.Y - Bounds.Height / 2) / _scale));

        public override void Render(DrawingContext ctx)
        {
            var b = Bounds;
            ctx.FillRectangle(new SolidColorBrush(Color.FromRgb(24, 24, 24)), b);

            var nav = _nav;
            int pf = nav?.Pf ?? -1;
            if (_follow && nav?.Pos != null && nav.Pos.Length >= 3) _center = new Vector2(nav.Pos[0], nav.Pos[2]);

            // background: a mission building's floor plan when the bot is inside one (composed from the
            // zone-in placement by the same AOBuddyNav.ComposeMission the bot used), else the outdoor
            // terrain when this playfield has one, else a coordinate grid — the overlays read on all three.
            var mission = nav?.Mission?.Layout != null ? _render.GetMission(nav.Mission.Layout) : null;
            MapRender.Terrain terrain = null;
            int floor = 0;
            if (mission != null)
            {
                floor = _floorOverride ?? nav.Mission.Floor ?? (mission.Floors.Length > 0 ? mission.Floors[0] : 0);
                if (mission.FloorBgra.TryGetValue(floor, out var fb))
                {
                    if (!_bitmaps.TryGetValue((mission.Instance, floor), out var mbmp))
                        _bitmaps[(mission.Instance, floor)] = mbmp = MakeBitmap(fb, mission.W, mission.H);
                    var mtl = ToScreen(mission.MinX, mission.MinZ + mission.H * mission.Cell);
                    var mbr = ToScreen(mission.MinX + mission.W * mission.Cell, mission.MinZ);
                    ctx.DrawImage(mbmp, new Rect(0, 0, mission.W, mission.H), new Rect(mtl, mbr));
                }
            }
            else
            {
                terrain = pf >= 0 ? _render.Get(pf) : null;
                if (terrain != null)
                {
                    if (!_bitmaps.TryGetValue((pf, 0), out var bmp))
                        _bitmaps[(pf, 0)] = bmp = MakeBitmap(terrain.Bgra, terrain.W, terrain.H);
                    // the bitmap covers world [0..W*cell]×[0..H*cell]; z-max is the TOP edge here, and the
                    // bitmap was copied row-reversed so its row 0 already is z-max
                    var tl = ToScreen(0, terrain.H * terrain.Cell);
                    var br = ToScreen(terrain.W * terrain.Cell, 0);
                    ctx.DrawImage(bmp, new Rect(0, 0, terrain.W, terrain.H), new Rect(tl, br));
                }
                else
                {
                    double grid = 100 * _scale;                  // 100 m
                    if (grid > 8)
                        for (double gx = b.Width / 2 % grid; gx < b.Width; gx += grid) ctx.DrawLine(_gridPen, new Point(gx, 0), new Point(gx, b.Height));
                    if (grid > 8)
                        for (double gy = b.Height / 2 % grid; gy < b.Height; gy += grid) ctx.DrawLine(_gridPen, new Point(0, gy), new Point(b.Width, gy));
                }
            }

            // mission dressing: room names (zoomed in enough to read) and the way out, on the shown floor
            if (mission != null)
            {
                if (_scale >= 0.12)
                    foreach (var r in mission.Rooms)
                        if (r.Floor == floor)
                        {
                            var p = ToScreen(r.X, r.Z);
                            Label(ctx, p.X + 3, p.Y - 6, r.Name);
                        }
                if (mission.ExitXZ != null && mission.ExitFloor == floor)
                {
                    var ex = ToScreen(mission.ExitXZ[0], mission.ExitXZ[1]);
                    ctx.DrawEllipse(null, _exitRingPen, ex, 6, 6);
                    Label(ctx, ex.X + 9, ex.Y - 7, "exit");
                }
            }

            if (nav != null)
            {
                // his road network: the footsteps the bot records (toggle, default on — it explains the routes)
                if (_steps && pf >= 0)
                {
                    var segs = _render.Footsteps(pf);
                    if (segs != null)
                        foreach (var seg in segs)
                        {
                            if (seg.Count < 2) continue;
                            var pts = new StreamGeometry();
                            using (var g = pts.Open())
                            {
                                g.BeginFigure(ToScreen(seg[0][0], seg[0][2]), false);
                                for (int i = 1; i < seg.Count; i++) g.LineTo(ToScreen(seg[i][0], seg[i][2]));
                            }
                            ctx.DrawGeometry(null, _stepsPen, pts);
                        }
                }

                // the trail, newest bright: the bot keeps 15 min of points; fade by minute-buckets so it
                // stays one line but reads old-vs-new (a fresh detour glows, the route so far stays faint)
                double now = DateTime.Now.Ticks / (double)TimeSpan.TicksPerSecond;
                const int Bands = 15;
                var bandPts = new List<Point>[Bands];
                int lastBand = -1;
                foreach (var t in nav.Trail)
                {
                    if (t.P == null || t.Pf != pf) continue;
                    double age = Math.Max(0, now - t.T.Ticks / (double)TimeSpan.TicksPerSecond);
                    int band = Math.Min(Bands - 1, (int)(age / 60));
                    if (bandPts[band] == null) bandPts[band] = new List<Point>();
                    if (lastBand >= 0 && lastBand != band) bandPts[lastBand].Add(ToScreen(t.P[0], t.P[2])); // bridge the band seams
                    bandPts[band].Add(ToScreen(t.P[0], t.P[2]));
                    lastBand = band;
                }
                for (int i = 0; i < Bands; i++)
                {
                    var pts = bandPts[i];
                    if (pts == null || pts.Count < 2) continue;
                    _trailPen.Brush = new SolidColorBrush(Color.FromArgb((byte)(200 - i * 12), 255, 255, 255));
                    ctx.DrawGeometry(null, _trailPen, new PolylineGeometry(pts, false));
                }

                // the hike the overland walker is on: route + the exit line (a–b) with its label
                if (nav.Hike?.Route != null && nav.Hike.Route.Count >= 2)
                {
                    var pts = new List<Point>(nav.Hike.Route.Count);
                    foreach (var p in nav.Hike.Route) pts.Add(ToScreen(p[0], p[2]));
                    ctx.DrawGeometry(null, _routePen, new PolylineGeometry(pts, false));
                }
                if (nav.Hike?.Exit is BotClient.Exit e && e.A != null && e.B != null)
                {
                    var a = ToScreen(e.A[0], e.A[2]);
                    var bb = ToScreen(e.B[0], e.B[2]);
                    ctx.DrawLine(_exitPen, a, bb);
                    Label(ctx, (a.X + bb.X) / 2, (a.Y + bb.Y) / 2 - 10, e.Text ?? e.Kind ?? "exit");
                }

                // the walk inside the building, and the mission's door
                if (nav.MissionPath != null && nav.MissionPath.Count >= 2)
                {
                    var pts = new List<Point>(nav.MissionPath.Count);
                    foreach (var p in nav.MissionPath) pts.Add(ToScreen(p[0], p[2]));
                    ctx.DrawGeometry(null, _walkPen, new PolylineGeometry(pts, false));
                }
                if (nav.Mission?.Door != null)
                {
                    var d = ToScreen(nav.Mission.Door[0], nav.Mission.Door[2]);
                    Cross(ctx, d, 7, _walkPen);
                }

                // the server's snap-backs on this floor: where he tried to be (local) vs where he was put
                foreach (var s in nav.Snaps)
                {
                    if (s.Local == null || s.Server == null || s.Pf != pf) continue;
                    var sv = ToScreen(s.Server[0], s.Server[2]);
                    var lc = ToScreen(s.Local[0], s.Local[2]);
                    ctx.DrawLine(_snapPen, sv, lc);
                    ctx.DrawEllipse(null, _snapPen, lc, 2, 2);
                }

                // him: a green dot with his facing
                if (nav.Pos != null)
                {
                    var me = ToScreen(nav.Pos[0], nav.Pos[2]);
                    ctx.DrawEllipse(Brushes.Lime, _botPen, me, 5, 5);
                    if (nav.Hdg != null)
                    {
                        double len = 16;
                        ctx.DrawLine(_botPen, me, new Point(me.X + nav.Hdg[0] * len, me.Y - nav.Hdg[1] * len));   // -Z: screen Y is inverted
                    }
                }
            }

            // corner text: zone, floor when inside, zoom, hover coordinates
            string hoverTxt = _hover == null ? "" : $"  @ {_hover.Value.X:0}, {_hover.Value.Y:0}";
            string floorTxt = mission == null ? ""
                : $"  ·  floor {floor}" + (nav.Mission.Floor != null && floor != nav.Mission.Floor ? $" (bot on {nav.Mission.Floor})" : "")
                  + (mission.Floors.Length > 1 ? "  ·  PgUp/PgDn" : "");
            Label(ctx, 8, 6, $"{(pf < 0 ? "no bot" : nav?.Zone ?? "?")}{(terrain == null && mission == null ? " — no terrain (grid)" : "")}{floorTxt}  ·  {_scale * 100:0}%{hoverTxt}", alignTop: true);
        }

        private static void Cross(DrawingContext ctx, Point c, double r, IPen pen)
        {
            ctx.DrawLine(pen, new Point(c.X - r, c.Y - r), new Point(c.X + r, c.Y + r));
            ctx.DrawLine(pen, new Point(c.X - r, c.Y + r), new Point(c.X + r, c.Y - r));
        }

        private static void Label(DrawingContext ctx, double x, double y, string text, bool alignTop = false)
        {
            var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                Typeface.Default, 12, new SolidColorBrush(Color.FromArgb(220, 235, 235, 235)));
            ctx.DrawText(ft, new Point(x, y));
        }

        // ---- pan / zoom / hover ----------------------------------------------------------------------

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
            _dragging = true;
            _dragPoint = new Vector2((float)e.GetPosition(this).X, (float)e.GetPosition(this).Y);
            _dragCenter = _center;
            Follow = false;                                // taking the wheel yourself drops the leash
            e.Pointer.Capture(this);
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            var p = e.GetPosition(this);
            _hover = ToWorld(p);
            if (_dragging)
            {
                _center = new Vector2(
                    _dragCenter.X - (float)((p.X - _dragPoint.X) / _scale),
                    _dragCenter.Y + (float)((p.Y - _dragPoint.Y) / _scale));   // + : screen Y inverted
            }
            InvalidateVisual();
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            _dragging = false;
            e.Pointer.Capture(null);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            // zoom at the cursor: the world point under it stays under it
            var p = e.GetPosition(this);
            var w = ToWorld(p);
            _scale = Math.Clamp(_scale * Math.Pow(1.2, e.Delta.Y), 0.02, 10);
            if (!_follow)                                  // when leashed, the bot recentres anyway
                _center = new Vector2(
                    (float)(w.X - (p.X - Bounds.Width / 2) / _scale),
                    (float)(w.Y + (p.Y - Bounds.Height / 2) / _scale));   // + : screen Y inverted
            InvalidateVisual();
            e.Handled = true;
        }

        // PgUp/PgDn peek at other floors of the building the bot is in; picking the bot's own floor
        // drops the override and it follows him again. Needs keyboard focus — click the map first.
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            var lay = _nav?.Mission?.Layout;
            if (lay == null) return;
            var plan = _render.GetMission(lay);
            int[] floors = plan?.Floors ?? _nav.Mission.Floors;
            if (floors.Length < 2) return;
            int cur = _floorOverride ?? _nav.Mission.Floor ?? floors[0];
            int idx = Array.IndexOf(floors, cur);
            if (e.Key == Key.PageUp && idx >= 0 && idx < floors.Length - 1) _floorOverride = floors[idx + 1];
            else if (e.Key == Key.PageDown && idx > 0) _floorOverride = floors[idx - 1];
            else return;
            if (_nav.Mission.Floor != null && _floorOverride == _nav.Mission.Floor) _floorOverride = null;
            InvalidateVisual();
            e.Handled = true;
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            _hover = null;
            InvalidateVisual();
        }

        private struct Vector2
        {
            public float X, Y;
            public Vector2(float x, float y) { X = x; Y = y; }
        }
    }
}
