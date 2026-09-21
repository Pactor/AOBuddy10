using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Serilog;
using AOSharp.Clientless.Common;

namespace AOSharp.Clientless
{
    // .NET 10 has no AppDomains. AOBuddy10 runs one bot per process: the SDK core (this assembly)
    // and the plugin load into the default AssemblyLoadContext and talk to the static Client
    // directly — no child domain, no MarshalByRefObject remoting. The old multi-account isolation
    // that a child AppDomain provided is now achieved by launching a separate process per
    // character (its own config.json). Ordering is preserved from the old flow:
    //   CreateDomain (set identity + chat)  ->  LoadPlugin (construct + Init the plugin)  ->  Start (Client.Init: networking + update loop).
    public class ClientDomain
    {
        protected Logger _logger;
        private readonly List<Plugin> _plugins = new List<Plugin>();
        private static bool _resolverHooked;

        // appDomain is unused on .NET 10; the parameter is kept only so the reflective
        // Activator.CreateInstance call site (and any subclass) keeps the same ctor shape.
        protected ClientDomain(AppDomain appDomain, Logger logger)
        {
            _logger = logger;
        }

        internal static ClientDomain CreateDomain(string username, string password, string characterName, Dimension dimension, Logger logger, bool useChat = true)
        {
            return CreateDomain<ClientDomain>(username, password, characterName, dimension, logger, useChat);
        }

        internal static T CreateDomain<T>(string username, string password, string characterName, Dimension dimension, Logger logger, bool useChat = true) where T : ClientDomain
        {
            T clientDomain = (T)Activator.CreateInstance(
                typeof(T),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new object[] { (AppDomain)null, logger },
                System.Globalization.CultureInfo.InvariantCulture);

            // Set the bot identity directly in this process (formerly marshalled into the child domain).
            Client.Credentials = new Credentials(username, password);
            Client.CharacterName = characterName;
            Client.Dimension = dimension;
            Client.Logger = logger;

            if (useChat)
                Client.CreateChatClient();

            return clientDomain;
        }

        // Start networking + the built-in update loop. Called after all plugins are loaded so a
        // plugin's Init (event subscriptions) runs before the first tick, matching the old order.
        public void Start()
        {
            Client.Init();
        }

        public void Unload()
        {
            foreach (Plugin plugin in _plugins)
                plugin.Teardown();

            Client.Teardown();
        }

        public void LoadPlugin(string pluginPath)
        {
            try
            {
                string pluginDir = Path.GetDirectoryName(Path.GetFullPath(pluginPath));
                HookAssemblyResolver(pluginDir);

                Assembly assembly = Assembly.LoadFrom(pluginPath);
                IPCChannel.LoadMessages(assembly);

                // Find the first AOSharp.Clientless.IClientlessPluginEntry (same-assembly interface now,
                // but resolved reflectively to keep the plugin contract loose, as before).
                foreach (Type type in assembly.GetExportedTypes())
                {
                    if (type.GetInterface("AOSharp.Clientless.IClientlessPluginEntry") == null)
                        continue;

                    MethodInfo initMethod = type.GetMethod("Init", BindingFlags.Public | BindingFlags.Instance);
                    if (initMethod == null)
                        continue;

                    MethodInfo teardownMethod = type.GetMethod("Teardown", BindingFlags.Public | BindingFlags.Instance);
                    if (teardownMethod == null)
                        continue;

                    ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                    if (constructor == null)
                        continue;

                    object instance = constructor.Invoke(null);
                    if (instance == null)
                        continue;

                    var plugin = new Plugin(instance, initMethod, teardownMethod, pluginDir);
                    plugin.Initialize();
                    _plugins.Add(plugin);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error when loading plugin {pluginPath}:\n{e}");
            }
        }

        // When the plugin lives in a different directory than the host, its private dependencies
        // won't be on the host's probing path. Fall back to probing the plugin's own folder.
        // Shared assemblies (AOSharp.Clientless/Common, Serilog, Newtonsoft) are already loaded by
        // identity and are reused, so this only ever supplies a plugin-only dependency.
        private static void HookAssemblyResolver(string pluginDir)
        {
            if (_resolverHooked)
                return;

            _resolverHooked = true;
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                string candidate = Path.Combine(pluginDir, name.Name + ".dll");
                return File.Exists(candidate) ? context.LoadFromAssemblyPath(candidate) : null;
            };
        }
    }

    // Kept for API compatibility (Client.HostProxy / DomainProxySink still reference the type).
    // On .NET 10 this is an in-process logging shim — no remoting boundary to cross.
    internal class HostProxy
    {
        private readonly Logger _logger;

        public HostProxy(Logger logger)
        {
            _logger = logger;
        }

        public void Debug(string message) => _logger.Debug(message);

        public void Warning(string message) => _logger.Warning(message);

        public void Error(string message) => _logger.Error(message);

        public void Information(string message) => _logger.Information(message);
    }

    public class Plugin
    {
        public bool Initialized;

        private object _instance;
        private MethodInfo _initMethod;
        private MethodInfo _teardownMethod;
        private string _assemblyDir;

        public Plugin(object instance, MethodInfo initMethod, MethodInfo teardownMethod, string assemblyDir)
        {
            Initialized = false;
            _instance = instance;
            _initMethod = initMethod;
            _teardownMethod = teardownMethod;
            _assemblyDir = assemblyDir;
        }

        public void Initialize()
        {
            try
            {
                _initMethod.Invoke(_instance, new object[] { _assemblyDir });
            }
            catch { }

            Initialized = true;
        }

        public void Teardown()
        {
            try
            {
                _teardownMethod.Invoke(_instance, null);
            }
            catch { }
        }
    }
}
