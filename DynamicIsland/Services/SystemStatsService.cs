using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DynamicIsland.Services;

public class SystemStats
{
    public float CpuPercent { get; set; }
    public float RamPercent { get; set; }
    public int BatteryPercent { get; set; }
    public bool IsCharging { get; set; }
    public bool HasBattery { get; set; }
}

public class SystemStatsService
{
    private static readonly Lazy<SystemStatsService> _instance = new(() => new SystemStatsService());
    public static SystemStatsService Instance => _instance.Value;

    private PerformanceCounter? _cpuCounter;
    private bool _activeSampling = false;
    private System.Threading.Timer? _sampleTimer;

    public SystemStats CurrentStats { get; private set; } = new();
    public event Action<SystemStats>? StatsUpdated;

    private SystemStatsService()
    {
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            _cpuCounter.NextValue(); // initial warmup
        }
        catch { }

        _sampleTimer = new System.Threading.Timer(_ =>
        {
            if (_activeSampling) Sample();
        }, null, 1000, 2000);
    }

    public void SetActiveSampling(bool active)
    {
        _activeSampling = active;
        if (active) Sample();
    }

    public void Sample()
    {
        try
        {
            float cpu = _cpuCounter?.NextValue() ?? 0f;

            // Memory status
            var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            GlobalMemoryStatusEx(ref mem);
            float ram = mem.dwMemoryLoad;

            // Battery status
            var power = SystemInformation.PowerStatus;
            bool hasBattery = power.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery;
            int battery = (int)(power.BatteryLifePercent * 100);
            bool isCharging = (power.BatteryChargeStatus & BatteryChargeStatus.Charging) != 0;

            CurrentStats = new SystemStats
            {
                CpuPercent = (float)Math.Round(cpu),
                RamPercent = ram,
                BatteryPercent = Math.Clamp(battery, 0, 100),
                IsCharging = isCharging,
                HasBattery = hasBattery
            };

            StatsUpdated?.Invoke(CurrentStats);
        }
        catch { }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
}
