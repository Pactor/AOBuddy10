using System.Reflection;

namespace AOSharp.Clientless.Logging
{
    public static class Logger
    {
        public static void Debug(string message) => Client.Logger.Debug($"[{Assembly.GetCallingAssembly().GetName().Name}] {message}"); 
        public static void Information(string message) => Client.Logger.Information($"[{Assembly.GetCallingAssembly().GetName().Name}] {message}");
        public static void Warning(string message) => Client.Logger.Warning($"[{Assembly.GetCallingAssembly().GetName().Name}] {message}");
        public static void Error(string message) => Client.Logger.Error($"[{Assembly.GetCallingAssembly().GetName().Name}] {message}");
    }
}
