using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using AOSharp.Clientless;
using System.IO;
using AOSharp.Clientless.Common;
using Newtonsoft.Json;

public class PluginLoader
{
    // The host reads config.json from its own folder, which the build puts at Build\ alongside
    // every assembly it needs:
    //
    //     Build\config.json                        accounts, and optionally which plugins to load
    //     Build\Plugins\<name>\<name>.dll           a plugin
    //     Build\Plugins\<name>\config.json         that plugin's own settings
    //
    // "Plugins" may be left out entirely, in which case every dll directly under a Plugins\<name>\
    // folder is loaded. Listing them is only for loading a subset or something kept elsewhere, and a
    // listed path may be relative to Build\ - an absolute path into some other repository's bin\Debug
    // was the single most annoying thing about setting this up.
    //
    // Example config:
    //{
    //  "Accounts": [
    //    { "Username": "TestUsername1", "Password": "Testpass1", "Character": "Testchar1" },
    //    { "Username": "TestUsername2", "Password": "Testpass2", "Character": "Testchar2", "Dimension": "RubiKa2019" }
    //  ],
    //  "Plugins": [ "Plugins\\AOBuddy\\AOBuddy.dll" ]
    //}

    private static List<ClientDomain> BotDomains = new List<ClientDomain>();

    static void Main(string[] args)
    {
        string configFile;
        string configPath = AppDomain.CurrentDomain.BaseDirectory + "config.json";

        try
        {
            configFile = File.ReadAllText(configPath);
        }
        catch
        {
            Console.WriteLine($"Config file not found at '{configPath}', read the instructions.");
            Console.ReadLine();
            return;
        }

        Config config = JsonConvert.DeserializeObject<Config>(configFile);

        if (config == null || config.Accounts == null || config.Accounts.Count == 0)
        {
            Console.WriteLine($"'{configPath}' has no Accounts. Copy config.example.json over it and fill it in.");
            Console.ReadLine();
            return;
        }

        List<string> plugins = ResolvePlugins(config.Plugins);
        if (plugins.Count == 0)
        {
            Console.WriteLine($"No plugins found. Put one in {Path.Combine(BaseDir, "Plugins")}\\<name>\\<name>.dll, "
                              + "or list it under \"Plugins\" in config.json.");
            Console.ReadLine();
            return;
        }

        foreach (string plugin in plugins)
            Console.WriteLine($"Plugin: {plugin}");

        foreach (AccountInfo acc in config.Accounts)
            CreateBot(acc, plugins);

        Console.ReadLine();

        foreach (var domain in BotDomains)
            domain.Unload();
    }

    private static string BaseDir => AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    /// Work out which plugin dlls to load. A configured list wins, with each entry taken relative to
    /// the host folder unless it is already absolute. With no list (or an empty one), every
    /// Plugins\&lt;name&gt;\&lt;anything&gt;.dll is loaded - which is the whole point of the layout: drop a
    /// plugin folder in and it runs, with no path to edit anywhere.
    /// </summary>
    private static List<string> ResolvePlugins(List<string> configured)
    {
        var found = new List<string>();

        if (configured != null && configured.Count > 0)
        {
            foreach (string entry in configured)
            {
                if (string.IsNullOrWhiteSpace(entry))
                    continue;

                string full = Path.IsPathRooted(entry) ? entry : Path.GetFullPath(Path.Combine(BaseDir, entry));
                if (File.Exists(full))
                    found.Add(full);
                else
                    Console.WriteLine($"Plugin not found, skipping: {full}");
            }

            return found;
        }

        string pluginsRoot = Path.Combine(BaseDir, "Plugins");
        if (!Directory.Exists(pluginsRoot))
            return found;

        foreach (string dir in Directory.GetDirectories(pluginsRoot))
        {
            // One plugin per folder. Prefer the dll named after the folder so a plugin that ships
            // its dependencies alongside it does not get every one of them loaded as a plugin.
            string preferred = Path.Combine(dir, Path.GetFileName(dir) + ".dll");
            if (File.Exists(preferred))
            {
                found.Add(preferred);
                continue;
            }

            string[] dlls = Directory.GetFiles(dir, "*.dll");
            if (dlls.Length == 1)
                found.Add(dlls[0]);
            else if (dlls.Length > 1)
                Console.WriteLine($"Skipping {dir}: several dlls and none named {Path.GetFileName(dir)}.dll - "
                                  + "name it after the folder, or list it in config.json.");
        }

        return found;
    }

    private static void CreateBot(AccountInfo accInfo, List<string> pluginPaths)
    {
        Logger logger = new LoggerConfiguration().WriteTo.Console().MinimumLevel.Debug().CreateLogger();

        Dimension dimension = ParseDimension(accInfo.Dimension);
        logger.Information($"Logging {accInfo.Character} into dimension {dimension}.");
        ClientDomain instance = Client.CreateInstance(accInfo.Username, accInfo.Password, accInfo.Character, dimension, logger);

        foreach (var path in pluginPaths)
            instance.LoadPlugin(path);

        instance.Start();
    }

    // Map the per-account "Dimension" config string to the enum. Accepts the enum name and
    // common aliases; blank/unknown defaults to RubiKa (main). Rubi-Ka 2019 is the fresh-start
    // progression server (its own char list and chat server).
    private static Dimension ParseDimension(string value)
    {
        string key = new string((value ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        switch (key)
        {
            case "rubika2019":
            case "rk2019":
            case "2019":
                return Dimension.RubiKa2019;
            case "":
            case "rubika":
            case "rk":
            case "rk1":
            case "rk5":
                return Dimension.RubiKa;
            default:
                if (Enum.TryParse(value, true, out Dimension parsed))
                    return parsed;
                Console.WriteLine($"Unknown dimension '{value}', defaulting to RubiKa. Use \"RubiKa\" or \"RubiKa2019\".");
                return Dimension.RubiKa;
        }
    }

    public class Config
    {
        public List<AccountInfo> Accounts;
        public List<string> Plugins;
    }

    public class AccountInfo
    {
        public string Username;
        public string Password;
        public string Character;
        public string Dimension;   // "RubiKa" (default) or "RubiKa2019"
    }
}