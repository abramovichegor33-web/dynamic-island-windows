using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using DynamicIsland.Services;

namespace DynamicIsland;

public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;

    [DllImport("kernel32.dll")]
    private static extern uint SetErrorMode(uint uMode);

    private const uint SEM_FAILCRITICALERRORS = 0x0001;
    private const uint SEM_NOGPFAULTERRORBOX = 0x0002;
    private const uint SEM_NOOPENFILEERRORBOX = 0x8000;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Suppress Windows legacy hard error dialogs (like 0xc0000005 popup)
        try
        {
            SetErrorMode(SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX | SEM_NOOPENFILEERRORBOX);
        }
        catch { }

        // Global exception handling to prevent any crashes
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

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            try
            {
                File.AppendAllText("crash.log", $"[{DateTime.Now}] Task Exception: {args.Exception}\n");
            }
            catch { }
            args.SetObserved();
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
