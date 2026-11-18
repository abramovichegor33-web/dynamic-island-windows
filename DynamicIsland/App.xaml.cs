using System.IO;
using System.Threading;
using System.Windows;
using DynamicIsland.Services;

namespace DynamicIsland;

public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Global exception handling to prevent any hard crashes
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            try
            {
                var err = args.ExceptionObject?.ToString() ?? "Unknown error";
                File.AppendAllText("crash.log", $"[{DateTime.Now}] AppDomain Exception: {err}\n");
            }
            catch { }
        };

        DispatcherUnhandledException += (s, args) =>
        {
            try
            {
                File.AppendAllText("crash.log", $"[{DateTime.Now}] Dispatcher Exception: {args.Exception}\n");
            }
            catch { }
            args.Handled = true; // Prevent app termination
        };

        const string appGuid = "DynamicIsland_Win10_Singleton_Guid";
        _singleInstanceMutex = new Mutex(true, appGuid, out bool isNewInstance);

        if (!isNewInstance)
        {
            // Another instance is already running, exit cleanly
            Shutdown();
            return;
        }

        try
        {
            _ = ThemeService.Instance;
        }
        catch { }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceMutex?.ReleaseMutex();
        base.OnExit(e);
    }
}
