using System;
using System.IO;
using Newtonsoft.Json;

namespace AOBuddyMonitor
{
    /// <summary>
    /// The monitor's own little config, monitor.json beside the exe (created with defaults on first run).
    ///   host / port   where the bot's BotApi listens (same PC: 127.0.0.1, Config.BotApiPort — 5591)
    ///   pluginDir     the bot plugin folder, for GameData/Nav/&lt;pf&gt;/ (terrain) and nav/&lt;pf&gt;.json (footsteps);
    ///                 the default walks up from the exe until it finds Build/Plugins/AOBuddy, so it works
    ///                 from tools/AOBuddyMonitor/bin/Debug and from a published folder dropped in the repo.
    /// </summary>
    public sealed class MonitorConfig
    {
        public string Host = "127.0.0.1";
        public int Port = 5591;
        public string PluginDir = "";

        public string Base => $"http://{Host}:{Port}";

        public static MonitorConfig Load()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "monitor.json");
            MonitorConfig c;
            try { c = File.Exists(path) ? JsonConvert.DeserializeObject<MonitorConfig>(File.ReadAllText(path)) ?? new MonitorConfig() : new MonitorConfig(); }
            catch { c = new MonitorConfig(); }
            if (string.IsNullOrEmpty(c.PluginDir)) c.PluginDir = FindPluginDir();
            try { File.WriteAllText(path, JsonConvert.SerializeObject(c, Formatting.Indented)); } catch { }
            return c;
        }

        // tools/AOBuddyMonitor/bin/Debug/net... → up four is the repo root; keep walking a while anyway
        // so a published folder anywhere inside the repo also finds it.
        private static string FindPluginDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "Build", "Plugins", "AOBuddy");
                if (Directory.Exists(candidate)) return candidate;
            }
            return "";
        }
    }
}