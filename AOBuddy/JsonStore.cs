using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AOBuddy
{
    /// <summary>
    /// THE persistence helper (R2.3): every JSON state file the bot writes goes through here, so a
    /// failing disk/permission can no longer vanish inside a per-site `catch { }` (the resupply
    /// memory, keep lists, danger marks and nav zones all used to fail silently). Two calls:
    ///   Load<T>  — null when the file is absent OR corrupt, with ONE logged line per path so a
    ///              hand-corrupted file is visible and the caller starts clean instead of silently;
    ///   Save     — serialize at the call site (formatting stays each site's own), write a .tmp
    ///              first and File.Replace it over the target, so a crash mid-write can never
    ///              leave a truncated state file. Logs once per path on failure too — an autosave
    ///              hitting a locked file every few seconds must not spam the log.
    /// Stays raw on purpose: the .bin wire captures and aobuddy.log (append) are not state files;
    /// read-only GameData inputs (Zoning.json, ItemData, the SQL dump) keep their own loaders.
    /// </summary>
    public static class JsonStore
    {
        private static readonly HashSet<string> _complained = new HashSet<string>();

        public static T Load<T>(string path, Action<string> log = null) where T : class
        {
            try
            {
                if (!File.Exists(path)) return null;
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            }
            catch (Exception ex) { Complain(path, "read", ex.Message, log); return null; }
        }

        /// <summary>Returns false (and logs once) when the write failed; never throws.</summary>
        public static bool Save(string path, string text, Action<string> log = null)
        {
            try
            {
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, text);
                if (File.Exists(path)) File.Replace(tmp, path, null);
                else File.Move(tmp, path);
                return true;
            }
            catch (Exception ex)
            {
                Complain(path, "write", ex.Message, log);
                try { File.Delete(path + ".tmp"); } catch { }
                return false;
            }
        }

        private static void Complain(string path, string what, string why, Action<string> log)
        {
            // Once per path+direction: the failure will repeat on every save until fixed.
            if (!_complained.Add(path + ":" + what)) return;
            log?.Invoke($"JSONSTORE: couldn't {what} {path}: {why}");
        }
    }
}