namespace DynamicIsland.Services;

/// <summary>
/// Disabled FPS meter stub to eliminate system-wide ETW kernel tracing and game stuttering.
/// </summary>
public sealed class FpsMeter : IDisposable
{
    public FpsMeter(string sessionName = "AtollFps") { }

    public bool Running => false;
    public bool AccessDenied => false;

    public bool Start() => false;
    public void Stop() { }

    public int? FpsOf(int pid) => null;
    public Dictionary<int, int> All() => [];

    public static string HelperFile => "";

    public static void RunHelper(int parentPid) { }

    public static int? FromHelper(int pid) => null;

    public static bool HelperAlive() => false;

    public static bool StartHelper() => false;

    public static bool GrantAccess() => false;

    public void Dispose() { }
}
