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
    //This plugin loader assumes you have a file 'config.json' in your debug folder, change account info and plugin paths to your liking

    // Example config:
    //{
    //  "Accounts": [
    //    {
    //      "Username": "TestUsername1",
    //      "Password": "Testpass1",
    //      "Character": "Testchar1"
    //    },
    //    {
    //    "Username": "TestUsername2",
    //      "Password": "Testpass2",
    //      "Character": "Testchar2"
    //    }
    //  ],
    //  "Plugins": [
    //    "C:\\lolis\\repos\\aosharp.clientless\\Test\\bin\\Debug\\TestClientlessPlugin.dll",
    //    "C:\\lolis\\repos\\someotherrepo\\bin\\Debug\\someotherplugin.dll",
    //  ]
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

        foreach (AccountInfo acc in config.Accounts)
            CreateBot(acc, config.Plugins);

        Console.ReadLine();

        foreach (var domain in BotDomains)
            domain.Unload();
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