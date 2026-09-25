using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AOSharp.Common.GameData;
using Newtonsoft.Json;

namespace AOBuddy
{
    /// <summary>
    /// PATH STORE — persistence for the owner's recorded walking routes (R3.4, moved verbatim from
    /// Main): one JSON file of [x,y,z] triples per named path under paths/. Save goes through
    /// JsonStore (atomic, failures logged and returned); Load deliberately THROWS on a bad file —
    /// the 'path' command catches it and replies "Load failed: ...", which is its error surface.
    /// The record/replay orchestration stays with the command (FOLLOW owns the walking); this is
    /// only the file side.
    /// </summary>
    public class PathStore
    {
        private readonly string _dir;
        private readonly Action<string> _log;

        public PathStore(string pathsDir, Action<string> log)
        {
            _dir = pathsDir;
            _log = log;
        }

        private string PathFile(string name) => Path.Combine(_dir, name + ".json");

        public bool Save(string name, List<Vector3> pts)
        {
            var data = pts.Select(p => new[] { p.X, p.Y, p.Z }).ToList();
            return JsonStore.Save(PathFile(name), JsonConvert.SerializeObject(data), _log);
        }

        public List<Vector3> Load(string name)
        {
            var data = JsonConvert.DeserializeObject<List<float[]>>(File.ReadAllText(PathFile(name)));
            return data.Select(a => new Vector3(a[0], a[1], a[2])).ToList();
        }

        public string List()
        {
            try
            {
                string[] files = Directory.GetFiles(_dir, "*.json");
                return files.Length == 0 ? "(none)" : string.Join(", ", files.Select(Path.GetFileNameWithoutExtension));
            }
            catch { return "(none)"; }
        }
    }
}