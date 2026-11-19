namespace DynamicIsland.Services;

/// <summary>
/// Headset battery queries disabled to prevent continuous USB HID polling and local WebSocket scans.
/// </summary>
public static class HeadsetBattery
{
    public sealed record Reading(string Device, int Percent, bool Charging, string Method);

    public static bool HasDevice(ushort pid) => false;

    public static Reading? Read(string deviceName = "Logitech") => null;

    public static Task<List<Reading>> ReadFromGHubAsync() => Task.FromResult(new List<Reading>());
}
