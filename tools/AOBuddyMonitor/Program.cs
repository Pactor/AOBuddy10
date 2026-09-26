using System;
using System.IO;
using Avalonia;

namespace AOBuddyMonitor
{
    // AOBuddyMonitor — see the csproj comment. One window, dark, no MVVM framework: a poller thread
    // reads the bot's API and hands plain objects to the window's code-behind.
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // a GUI app dies silently otherwise: park any unhandled exception beside the exe
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "crash.txt"), e.ExceptionObject.ToString()); } catch { }
            };
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}