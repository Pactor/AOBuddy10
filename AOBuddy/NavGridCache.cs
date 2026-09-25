using System;
using System.Threading.Tasks;

namespace AOBuddy
{
    /// <summary>
    /// A playfield's nav data and walkable grid, built OFF the update thread — Lush Fields' grid took
    /// 6.3 s and froze the whole bot while it built (log 2026-09-24 01:36). One cache per consumer
    /// (OverlandController for travel, MissionRun for hikes); THE one loader (R1.9: two verbatim
    /// copies used to live in both).
    ///
    /// Poll it every tick: Request starts the build for a playfield (or keeps waiting on one already
    /// running) and returns false while it loads; the first true carries Nav/Grid for that playfield
    /// (null on failure — the caller falls back to straight lines, exactly as before).
    /// </summary>
    public sealed class NavGridCache
    {
        private Task<(AOBuddyNav Nav, IWalkGrid Grid)> _task;
        private int _taskPf = -1;

        public int LoadedPf { get; private set; } = -1;
        public AOBuddyNav Nav { get; private set; }
        public IWalkGrid Grid { get; private set; }

        /// <summary>tag prefixes the failure log line so it reads as the consumer that asked.</summary>
        public bool Request(int pf, string pluginDir, Action<string> log, string tag)
        {
            if (pf == LoadedPf) return true;
            if (_task == null || _taskPf != pf)
            {
                string dir = pluginDir;
                var logger = log;
                _taskPf = pf;
                _task = Task.Run(() =>
                {
                    var nav = AOBuddyNav.Load(dir, pf);
                    // The finished grid first from the disk cache (GridCache, 2026-09-25); only a miss
                    // pays the 0.6-3.8 s stamping, and a fresh build is saved back for next time.
                    IWalkGrid grid = GridCache.TryLoad(dir, pf, nav, logger);
                    if (grid == null)
                    {
                        grid = (IWalkGrid)OverlandGrid.Build(dir, pf, nav, logger) ?? FloorGrid.Build(dir, pf, nav, logger);
                        if (grid != null) GridCache.Save(dir, pf, grid, logger);
                    }
                    return (nav, grid);
                });
                return false;
            }
            if (!_task.IsCompleted) return false;
            var done = _task;
            _task = null;
            LoadedPf = pf;
            Nav = null; Grid = null;
            if (done.IsFaulted) log($"{tag}: no nav data for pf {pf}: {done.Exception?.GetBaseException().Message}");
            else (Nav, Grid) = done.Result;
            return true;
        }
    }
}