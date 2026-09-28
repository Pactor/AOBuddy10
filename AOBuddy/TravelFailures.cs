using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AOSharp.Common.GameData;
using Newtonsoft.Json;

namespace AOBuddy
{
    /// <summary>
    /// FAILED TRAVEL, REMEMBERED (owner, 2026-09-27: "does not repeat the same attempt again and again"). 20:11:48,
    /// 20:13:01 and 20:14:31 the same travelto from the Longest Road town to the door at (3596,1500) planned the same
    /// six legs out through the booths toward Old Athen, was stopped, and was planned again. Each failed attempt is
    /// kept here (pluginDir/travelfails.json, across restarts, KeepHours): where it started, the goal (pf + spot),
    /// the first exit the plan used from its start zone ("walk" for an on-foot plan), the reason and the time.
    /// A plan toward the same goal leaves out the exits that failed as a first step; MissionRun gives up on a door
    /// after GiveUpAfter failed attempts and goes back to its terminal.
    /// </summary>
    public sealed class TravelFailures
    {
        public sealed class Entry
        {
            public int FromPf, GoalPf;
            public bool HasGoal;
            public float GX, GZ;
            public string First, FirstText, Reason;
            public DateTime When;
        }

        public const double KeepHours = 6;
        public const float SameGoalMeters = 30f;   // doors are points; a goal this close counts as the same one
        public const int GiveUpAfter = 2;
        public const string Walk = "walk";

        private readonly string _path;
        private readonly Action<string> _log;
        private List<Entry> _list;

        public TravelFailures(string pluginDir, Action<string> log)
        {
            _path = Path.Combine(pluginDir, "travelfails.json");
            _log = log;
        }

        /// <summary>A stable name for an exit: zone, kind, object and index (zone lines have no object).</summary>
        public static string Key(ZoneExit e) =>
            e == null ? Walk
            : e.Kind == ExitKind.Scotty ? "scotty:" + e.Tell
            : $"{e.FromPf}:{e.Kind}:{e.ObjInstance}:{e.Idx}";

        private List<Entry> List
        {
            get
            {
                if (_list == null) _list = JsonStore.Load<List<Entry>>(_path, _log) ?? new List<Entry>();
                _list.RemoveAll(x => (DateTime.UtcNow - x.When).TotalHours > KeepHours);
                return _list;
            }
        }

        private static bool Same(Entry x, int goalPf, Vector3? goal) =>
            x.GoalPf == goalPf && x.HasGoal == goal.HasValue
            && (!goal.HasValue || Math.Sqrt((x.GX - goal.Value.X) * (x.GX - goal.Value.X) + (x.GZ - goal.Value.Z) * (x.GZ - goal.Value.Z)) < SameGoalMeters);

        public void Record(int fromPf, int goalPf, Vector3? goal, string first, string firstText, string reason)
        {
            var l = List;
            l.Add(new Entry
            {
                FromPf = fromPf, GoalPf = goalPf, HasGoal = goal.HasValue,
                GX = goal.HasValue ? (float)Math.Round(goal.Value.X) : 0f, GZ = goal.HasValue ? (float)Math.Round(goal.Value.Z) : 0f,
                First = first ?? Walk, FirstText = firstText, Reason = reason, When = DateTime.UtcNow,
            });
            JsonStore.Save(_path, JsonConvert.SerializeObject(l, Formatting.Indented), _log);
            string g = goal.HasValue ? $"({goal.Value.X:0},{goal.Value.Z:0}) in {Zoning.Name(goalPf)}" : Zoning.Name(goalPf);
            _log($"TRAVELFAILS: recorded a failed trip from {Zoning.Name(fromPf)} to {g}, first step {firstText ?? first} ({reason}); {Count(goalPf, goal)} failed attempt(s) at that goal in the last {KeepHours:0} h.");
        }

        /// <summary>The first steps (exit keys, or Walk) that failed recently toward this goal.</summary>
        public HashSet<string> FailedFirsts(int goalPf, Vector3? goal) =>
            new HashSet<string>(List.Where(x => Same(x, goalPf, goal)).Select(x => x.First));

        /// <summary>Failed attempts at this goal within KeepHours.</summary>
        public int Count(int goalPf, Vector3? goal) => List.Count(x => Same(x, goalPf, goal));
    }
}
