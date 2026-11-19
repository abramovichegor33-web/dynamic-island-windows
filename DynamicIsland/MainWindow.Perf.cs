using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using DynamicIsland.Services;

namespace DynamicIsland;

/// <summary>
/// Smoothness on other people's PCs: Windows must not slow the island down as a "background app", and a
/// report with the real numbers (frame times while the island moves, screen, graphics card) can be copied
/// from Settings and sent to the developer.
/// </summary>
public partial class MainWindow
{
    // ------------------------------------------------------------------ "the island froze": what was it doing?

    private long _uiBeat = Environment.TickCount64;
    private volatile string _uiStep = "start";
    private System.Threading.Timer? _stallTimer;
    private bool _stallLogged;

    /// <summary>Called on the UI thread at the start of anything that could take long: a heartbeat with a name.</summary>
    private void UiStep(string what)
    {
        _uiStep = what;
        Interlocked.Exchange(ref _uiBeat, Environment.TickCount64);
    }

    /// <summary>
    /// A background timer watches the heartbeat. If the UI thread stops answering for a few seconds, the log
    /// gets one line saying so and what it was doing — so a "the island hung" report can be acted on.
    /// </summary>
    private void InitStallWatch()
    {
        Karaoke.Spent = SpentElsewhere;
        _stallTimer = new System.Threading.Timer(_ =>
        {
            long late = Environment.TickCount64 - Interlocked.Read(ref _uiBeat);
            if (late is > 4000 and < 120000)   // longer than that is the computer waking from sleep, not a hang
            {
                if (_stallLogged) return;
                _stallLogged = true;
                Log.Write($"ui thread not answering for {late / 1000} s, last step: {_uiStep}");
            }
            else _stallLogged = false;
        }, null, 3000, 1000);
    }

    /// <summary>Sets up one feature; if it fails, the log says which and the rest of the island still starts.</summary>
    private void Safely(string what, Action init)
    {
        try { init(); }
        catch (Exception e) { Log.Write($"start: {what} failed: {e}"); }
    }

    private async Task SafelyAsync(string what, Func<Task> init)
    {
        try { await init(); }
        catch (Exception e) { Log.Write($"start: {what} failed: {e}"); }
    }

    // ------------------------------------------------------------------ what the UI thread spends its time on

    private sealed class Cost { public long Calls, Ticks, Max; }

    private Dictionary<string, Cost> _costs = [], _costsDone = [];
    private long _costsFrom = Stopwatch.GetTimestamp(), _costsDoneTicks;
    private TimeSpan _cpuFrom = CpuTime();
    private double _cpuDone = -1;
    private long _childTicks;
    private static readonly long CostWindow = Stopwatch.Frequency * 600;

    /// <summary>
    /// Runs work and books its time under name (UI thread only). Time spent in a Measure nested inside is
    /// booked to the inner name alone, so the numbers add up. For async work only the part before the
    /// first await counts — that is the part that holds the UI thread. An error is logged and stops here:
    /// a DispatcherTimer whose Tick throws never fires again (while still saying it is on), so one bad
    /// moment in a timer would quietly stop that part of the island for good.
    /// </summary>
    private void Measure(string name, Action work) => Measure(name, () => { work(); return 0; });

    private T Measure<T>(string name, Func<T> work)
    {
        long outer = _childTicks;
        _childTicks = 0;
        long t0 = Stopwatch.GetTimestamp();
        try { return work(); }
        catch (Exception e)
        {
            App.Crash(name, e);
            return default!;
        }
        finally
        {
            long total = Stopwatch.GetTimestamp() - t0;
            Spent(name, total - _childTicks);
            _childTicks = outer + total;
        }
    }

    /// <summary>Time measured by a view (its frame handler, its drawing) — not counted again in a Measure around it.</summary>
    private void SpentElsewhere(string name, long ticks)
    {
        Spent(name, ticks);
        _childTicks += ticks;
    }

    private void Spent(string name, long ticks)
    {
        if (!_costs.TryGetValue(name, out var c)) _costs[name] = c = new Cost();
        c.Calls++;
        c.Ticks += ticks;
        if (ticks > c.Max) c.Max = ticks;
        long now = Stopwatch.GetTimestamp();
        if (now - _costsFrom < CostWindow) return;
        // a finished window is kept for the report, and its top lines go to the log
        var cpu = CpuTime();
        _cpuDone = CpuPercent(cpu - _cpuFrom, now - _costsFrom);
        _costsDone = _costs;
        _costsDoneTicks = now - _costsFrom;
        _costs = [];
        _costsFrom = now;
        _cpuFrom = cpu;
        Log.Write("perf: " + TopCosts(_costsDone, _costsDoneTicks, _cpuDone).Replace(Environment.NewLine, " | "));
    }

    private static TimeSpan CpuTime()
    {
        try { using var p = Process.GetCurrentProcess(); return p.TotalProcessorTime; }
        catch { return TimeSpan.Zero; }
    }

    /// <summary>Share of the whole CPU (all cores) the process used.</summary>
    private static double CpuPercent(TimeSpan cpu, long ticks) =>
        ticks <= 0 ? 0 : cpu.TotalSeconds / ((double)ticks / Stopwatch.Frequency) / Environment.ProcessorCount * 100;

    private static string TopCosts(Dictionary<string, Cost> costs, long windowTicks, double cpu)
    {
        double secs = Math.Max(1, (double)windowTicks / Stopwatch.Frequency);
        static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
        var sb = new StringBuilder();
        sb.Append($"UI work, last {secs / 60:0.#} min, CPU {cpu:0.#}% of all {Environment.ProcessorCount} threads:");
        foreach (var (name, c) in costs.OrderByDescending(kv => kv.Value.Ticks).Take(5))
            sb.Append($"{Environment.NewLine}  {name}: {Ms(c.Ticks) / secs:0.0} ms/s, avg {Ms(c.Ticks) / c.Calls:0.00} ms, max {Ms(c.Max):0} ms, {c.Calls / secs:0.#}/s");
        if (costs.Count == 0) sb.Append(" nothing measured yet");
        return sb.ToString();
    }

    /// <summary>Tests: start counting from now.</summary>
    private void ResetCosts()
    {
        _costs = [];
        _costsDone = [];
        _costsFrom = Stopwatch.GetTimestamp();
        _cpuFrom = CpuTime();
    }

    /// <summary>The top 5 for a report: the window running now once it is a couple of minutes long, else the last full one.</summary>
    private string TopCostsReport()
    {
        long now = Stopwatch.GetTimestamp();
        bool current = _costsDone.Count == 0 || now - _costsFrom > Stopwatch.Frequency * 120;
        return current
            ? TopCosts(_costs, now - _costsFrom, CpuPercent(CpuTime() - _cpuFrom, now - _costsFrom))
            : TopCosts(_costsDone, _costsDoneTicks, _cpuDone);
    }

    // ------------------------------------------------------------------ no power throttling

    [StructLayout(LayoutKind.Sequential)]
    private struct PowerThrottlingState { public uint Version, ControlMask, StateMask; }
    [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll")] private static extern bool SetProcessInformation(IntPtr process, int infoClass, ref PowerThrottlingState info, int size);

    /// <summary>
    /// Windows 11 runs programs that are never the active window in "efficiency mode" (EcoQoS: slower CPU,
    /// coarse timers) — on laptops especially. The island is never the active window, yet it animates:
    /// opt out, so its frames come on time.
    /// </summary>
    private static void DisablePowerThrottling()
    {
        try
        {
            var s = new PowerThrottlingState { Version = 1, ControlMask = 0x1 | 0x4, StateMask = 0 };   // EXECUTION_SPEED | IGNORE_TIMER_RESOLUTION: both off
            if (!SetProcessInformation(GetCurrentProcess(), 4 /* ProcessPowerThrottling */, ref s, Marshal.SizeOf<PowerThrottlingState>()))
                Log.Write("power throttling: could not opt out (" + Marshal.GetLastWin32Error() + ")");
        }
        catch (Exception e) { Log.Write("power throttling: " + e.Message); }
    }

    // ------------------------------------------------------------------ frame statistics while the island moves

    private long _perfFrames, _perfSlow, _perfVerySlow;
    private double _perfSum, _perfWorst;
    private DateTime _perfLogged = DateTime.UtcNow;

    private void RecordPerf(double dt)
    {
        if (dt <= 0 || dt > 0.5) return;   // a pause, not a frame
        _perfFrames++;
        _perfSum += dt;
        if (dt > 0.020) _perfSlow++;
        if (dt > 0.050) _perfVerySlow++;
        _perfWorst = Math.Max(_perfWorst, dt);
        // a line in the log now and then, so a log sent by a user already says how smooth it was
        if (_perfFrames % 1200 == 0 && DateTime.UtcNow - _perfLogged > TimeSpan.FromMinutes(10))
        {
            _perfLogged = DateTime.UtcNow;
            Log.Write("perf: " + PerfSummary());
        }
    }

    private string PerfSummary() => _perfFrames == 0
        ? "no animation frames yet"
        : $"{_perfFrames} frames while moving, {1 / (_perfSum / _perfFrames):0} fps avg, late (>20 ms) {_perfSlow * 100.0 / _perfFrames:0.#}%, " +
          $"very late (>50 ms) {_perfVerySlow * 100.0 / _perfFrames:0.#}%, worst {_perfWorst * 1000:0} ms";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public short dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
        public int dmFields, dmPositionX, dmPositionY, dmDisplayOrientation, dmDisplayFixedOutput;
        public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
        public int dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool EnumDisplaySettings(string device, int mode, ref DevMode dm);

    /// <summary>Everything that decides how smooth the island is on this PC, in a few lines.</summary>
    private string PerfReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Atoll {Updater.Current} · Windows {Environment.OSVersion.Version}");
        sb.AppendLine("Smoothness: " + PerfSummary());
        sb.AppendLine($"Graphics: {string.Join(" + ", Native.GraphicsCards())} · WPF tier {RenderCapability.Tier >> 16}");
        sb.AppendLine($"CPU threads: {Environment.ProcessorCount}");
        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
        {
            var dm = new DevMode { dmSize = (short)Marshal.SizeOf<DevMode>() };
            int hz = EnumDisplaySettings(screen.DeviceName, -1, ref dm) ? dm.dmDisplayFrequency : 0;
            sb.AppendLine($"Screen: {screen.Bounds.Width}x{screen.Bounds.Height} {hz} Hz{(screen.Primary ? " (main)" : "")}");
        }
        sb.AppendLine($"Island: scale {Dpi:0.00} · size {_settings.IslandScale}% · light graphics {(Lite ? "on" : "off")}{(_slowPcSeen ? " (auto)" : "")} · glass {(_settings.Glass ? "on" : "off")} · waves {(_settings.LiquidWaves ? "on" : "off")}");
        var power = System.Windows.Forms.SystemInformation.PowerStatus;
        sb.AppendLine($"Power: {(power.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Offline ? "battery" : "plugged in")}");
        sb.Append(TopCostsReport());
        return sb.ToString();
    }

    private void CopyPerfReport_Click(object sender, RoutedEventArgs e)
    {
        var text = PerfReport();
        try { Clipboard.SetText(text); } catch { }
        Log.Write("perf report:\n" + text);
        ShowToast("", L.T("Отчёт скопирован"), L.T("Вставьте его в комментарии или в Telegram-канал"));
    }
}
