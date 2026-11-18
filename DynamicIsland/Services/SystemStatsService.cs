using System.Runtime.InteropServices;

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

    private bool _activeSampling = false;
    private System.Threading.Timer? _sampleTimer;

    private ulong _prevIdleTime;
    private ulong _prevKernelTime;
    private ulong _prevUserTime;

    public SystemStats CurrentStats { get; private set; } = new();
    public event Action<SystemStats>? StatsUpdated;

    private SystemStatsService()
    {
        // Initial CPU baseline
        SampleCpuBaseline();

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
            float cpu = CalculateCpuPercent();

            // RAM
            var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            GlobalMemoryStatusEx(ref mem);
            float ram = mem.dwMemoryLoad;

            // Battery (native Win32 GetSystemPowerStatus)
            int battery = 100;
            bool isCharging = false;
            bool hasBattery = false;

            if (GetSystemPowerStatus(out var powerStatus))
            {
                hasBattery = (powerStatus.BatteryFlag & 128) == 0 && powerStatus.BatteryFlag != 255;
                isCharging = (powerStatus.ACLineStatus == 1);
                if (powerStatus.BatteryLifePercent <= 100)
                {
                    battery = powerStatus.BatteryLifePercent;
                }
            }

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

    private void SampleCpuBaseline()
    {
        if (GetSystemTimes(out var idle, out var kernel, out var user))
        {
            _prevIdleTime = ToUInt64(idle);
            _prevKernelTime = ToUInt64(kernel);
            _prevUserTime = ToUInt64(user);
        }
    }

    private float CalculateCpuPercent()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
            return 0f;

        ulong curIdle = ToUInt64(idle);
        ulong curKernel = ToUInt64(kernel);
        ulong curUser = ToUInt64(user);

        ulong usrDiff = curUser - _prevUserTime;
        ulong kerDiff = curKernel - _prevKernelTime;
        ulong idlDiff = curIdle - _prevIdleTime;

        _prevIdleTime = curIdle;
        _prevKernelTime = curKernel;
        _prevUserTime = curUser;

        ulong sysDiff = usrDiff + kerDiff;
        if (sysDiff == 0) return 0f;

        double cpu = (1.0 - ((double)idlDiff / sysDiff)) * 100.0;
        return (float)Math.Clamp(cpu, 0.0, 100.0);
    }

    private static ulong ToUInt64(System.Runtime.InteropServices.ComTypes.FILETIME ft)
    {
        return ((ulong)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(
        out System.Runtime.InteropServices.ComTypes.FILETIME lpIdleTime,
        out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime,
        out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);

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

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS sps);
}
