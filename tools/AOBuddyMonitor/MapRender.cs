using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AOBuddy;
using Newtonsoft.Json;

namespace AOBuddyMonitor
{
    /// <summary>
    /// The map background: one pixel per heightfield cell, coloured by the bot's own answers — the same
    /// ground.bin, the same SwimY verdict tools/navmap renders. Linked against AOBuddy.csproj on purpose
    /// so this can never drift from what the bot walks on (the RDB-sweep higher-resolution terrain, when
    /// it comes, lands in the same files and shows up here unchanged). The water sweep is not free, so
    /// renders happen on a background task and land in the cache when ready.
    /// </summary>
    public sealed class MapRender
    {
        public sealed class Terrain
        {
            public int Pf;
            public string Name = "";
            public int W, H;                     // cells (pixels)
            public float Cell;                  // metres per cell
            public byte[] Bgra;                 // W*H*4, for a WriteableBitmap
        }

        private readonly object _lock = new object();
        private readonly Dictionary<int, Terrain> _done = new Dictionary<int, Terrain>();
        private readonly HashSet<int> _loading = new HashSet<int>();
        private readonly string _pluginDir;
        private DateTime _loadedAt;
        private Dictionary<int, List<List<float[]>>> _steps = new Dictionary<int, List<List<float[]>>>();

        public MapRender(string pluginDir) { _pluginDir = pluginDir; }

        /// <summary>The rendered terrain for a playfield, or null while it loads / when there is none
        /// (mission interiors and unextracted zones draw on the plain coordinate grid instead).</summary>
        public Terrain Get(int pf)
        {
            lock (_lock)
            {
                if (_done.TryGetValue(pf, out var t)) return t;
                if (!_loading.Contains(pf) && _pluginDir.Length > 0)
                {
                    _loading.Add(pf);
                    Task.Run(() =>
                    {
                        var rendered = Render(pf);
                        lock (_lock)
                        {
                            _loading.Remove(pf);
                            if (rendered != null) _done[pf] = rendered;
                        }
                        Rendered?.Invoke(pf);
                    });
                }
                return null;
            }
        }

        /// <summary>Fired on the background thread when a terrain finishes; the view invalidates itself.</summary>
        public event Action<int> Rendered;

        /// <summary>The owner's recorded footsteps (the bot's road network) for a playfield, as walked
        /// polylines. nav/&lt;pf&gt;.json is the bot's own file and may be mid-write — a failed read just
        /// yields no layer this round (re-read on the next playfield change).</summary>
        public List<List<float[]>> Footsteps(int pf)
        {
            if (_pluginDir.Length == 0) return null;
            string file = Path.Combine(_pluginDir, "nav", pf + ".json");
            try
            {
                var at = File.GetLastWriteTimeUtc(file);
                lock (_lock)
                    if (at == _loadedAt && _steps.TryGetValue(pf, out var cached))
                        return cached;
                var parsed = JsonConvert.DeserializeObject<WalkedFile>(File.ReadAllText(file));
                var segs = parsed?.Segments?.Where(s => s != null).ToList() ?? new List<List<float[]>>();
                lock (_lock) { _loadedAt = at; _steps[pf] = segs; }
                return segs;
            }
            catch { return null; }
        }

        private sealed class WalkedFile { public List<List<float[]>> Segments; }

        // ---- the render (navmap's colour logic: terrain grey, water blue, 256 m grid) ---------------------

        private Terrain Render(int pf)
        {
            try
            {
                var nav = AOBuddyNav.Load(_pluginDir, pf);
                var g = nav?.Ground;
                if (g == null) return null;
                int w = g.SamplesX - 1, h = g.SamplesZ - 1;
                if (w <= 0 || h <= 0) return null;
                float cell = g.Cell;
                float Low(int iz, int ix) => g.Heights[iz * g.SamplesX + ix] * g.HeightScale;
                float CornerMin(int iz, int ix) => Math.Min(Math.Min(Low(iz, ix), Low(iz, ix + 1)), Math.Min(Low(iz + 1, ix), Low(iz + 1, ix + 1)));

                // ask SwimY once so its lazy water build happens on this thread, then everywhere
                g.SwimY(cell / 2, cell / 2, 0.3);

                float hmin = float.MaxValue, hmax = float.MinValue;
                foreach (ushort v in g.Heights) { float hv = v * g.HeightScale; if (hv < hmin) hmin = hv; if (hv > hmax) hmax = hv; }

                var img = new byte[w * h * 4];         // BGRA
                for (int iz = 0; iz < h; iz++)
                    for (int ix = 0; ix < w; ix++)
                    {
                        int i = iz * w + ix;
                        float t = (CornerMin(iz, ix) - hmin) / Math.Max(0.1f, hmax - hmin);
                        int v = 38 + (int)(150 * t);   // a shade darker than navmap: it is a background here
                        int r = v, gr = v, b = v;
                        double sw = g.SwimY((ix + 0.5f) * cell, (iz + 0.5f) * cell, 0.3);
                        if (!double.IsNaN(sw))
                        {
                            float depth = (float)(sw - g.HeightAt((ix + 0.5f) * cell, (iz + 0.5f) * cell));
                            int a = Math.Min(200, 80 + (int)(depth * 22));
                            r += (50 - r) * a / 255; gr += (100 - gr) * a / 255; b += (230 - b) * a / 255;
                        }
                        img[i * 4] = (byte)b; img[i * 4 + 1] = (byte)gr; img[i * 4 + 2] = (byte)r; img[i * 4 + 3] = 255;
                    }

                // a grid line every 256 m, lightened like navmap's, keeps the sense of scale when zoomed in
                int step = Math.Max(1, (int)Math.Round(256 / cell));
                for (int x = 0; x < w; x += step) for (int z = 0; z < h; z++) Lighten(z * w + x);
                for (int z = 0; z < h; z += step) for (int x = 0; x < w; x++) Lighten(z * w + x);
                void Lighten(int i)
                {
                    img[i * 4] = (byte)Math.Min(255, img[i * 4] + 20);
                    img[i * 4 + 1] = (byte)Math.Min(255, img[i * 4 + 1] + 20);
                    img[i * 4 + 2] = (byte)Math.Min(255, img[i * 4 + 2] + 20);
                }

                return new Terrain { Pf = pf, Name = nav.Name, W = w, H = h, Cell = cell, Bgra = img };
            }
            catch { return null; }
        }
    }
}
