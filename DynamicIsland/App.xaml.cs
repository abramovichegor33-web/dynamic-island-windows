using System.Windows;
using DynamicIsland.Services;

namespace DynamicIsland;

public partial class App : Application
{
    private Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        WatchCrashes();

        if (e.Args.Contains("--uninstall"))
        {
            // the setup's uninstaller: remove Claude Code hooks and autostart, nothing else on screen
            Uninstaller.RemoveRegistrations();
            Shutdown();
            return;
        }

        int fh = Array.IndexOf(e.Args, "--fps-helper");
        if (fh >= 0)
        {
            // the elevated FPS helper: no window, no single-instance lock, ends with Atoll
            int.TryParse(e.Args.ElementAtOrDefault(fh + 1), out int parent);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            new Thread(() => { FpsMeter.RunHelper(parent); Dispatcher.Invoke(Shutdown); }) { IsBackground = true }.Start();
            return;
        }

        _mutex = new Mutex(true, "DynamicIsland.SingleInstance", out bool created);
        if (!created && e.Args.Contains("--restart"))
        {
            // relaunched after a language change: wait for the old instance to let go
            try { created = _mutex.WaitOne(TimeSpan.FromSeconds(8)); }
            catch (AbandonedMutexException) { created = true; }
        }
        if (!created)
        {
            Shutdown();
            return;
        }

        // test and picture runs are killed at will: they neither leave nor read the start markers
        if (!TestRun)
            StartupGuard.Begin();
        else
            MagCapture.UseGuard = false;

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); // rasdial output
        var settings = Settings.Current;
        if (e.Args.Contains("--safe")) settings.EnterSafeMode();
        // "--lang de" previews another language without touching the saved setting
        int langArg = Array.IndexOf(e.Args, "--lang");
        Loc.Init(langArg >= 0 && langArg + 1 < e.Args.Length ? e.Args[langArg + 1] : settings.Language);
        Theme.Apply(settings.DarkTheme, settings.IslandColor, settings.GlassOpacity, settings.AccentColor);

        var window = new MainWindow(e.Args);
        MainWindow = window;
        window.Closed += (_, _) => Shutdown();
        window.Show();
    }

    /// <summary>A test, picture or demo run (--run, --cover, --snapshot…): it must not touch the user's state.</summary>
    public static bool TestRun => DynamicIsland.MainWindow.IsToolRun || Environment.GetCommandLineArgs().Contains("--run");

    private static readonly Dictionary<string, int> Crashes = [];

    /// <summary>
    /// An error nobody caught goes to the log with where it happened. One in a UI handler is survived: a single
    /// broken feature must not close the whole island (1.8.0 closed at once, leaving nothing to report). The
    /// same error from a timer would repeat every tick: it is logged the first 3 times, then every 100th.
    /// </summary>
    private static void WatchCrashes()
    {
        Current.DispatcherUnhandledException += (_, e) =>
        {
            Crash("ui", e.Exception);
            e.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Crash(e.IsTerminating ? "fatal" : "background", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Crash("task", e.Exception);
            e.SetObserved();
        };
    }

    /// <summary>Logs an error caught at a boundary (a timer, a frame handler) with the same limit.</summary>
    public static void Crash(string where, Exception? e)
    {
        if (e == null) return;
        var key = e.GetType().FullName + " " + e.TargetSite;
        int n;
        lock (Crashes) Crashes[key] = n = Crashes.GetValueOrDefault(key) + 1;
        if (n > 3 && n % 100 != 0) return;
        var text = e.ToString();
        if (text.Length > 3000) text = text[..3000] + "…";
        Log.Write($"unhandled ({where}, #{n}): {text}");
    }

    /// <summary>Give up the "only one island" lock early, so a relaunched copy can start while this one closes.</summary>
    public void ReleaseSingleInstance()
    {
        try { _mutex?.ReleaseMutex(); } catch { }
        _mutex?.Dispose();
        _mutex = null;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
