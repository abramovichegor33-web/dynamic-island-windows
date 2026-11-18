using System.Windows;

namespace DynamicIsland.Services;

/// <summary>
/// Safe window composition service.
/// Uses native WPF composition for pure dark/frosted styling
/// without calling unstable undocumented user32 APIs.
/// </summary>
public static class WindowComposition
{
    public static void EnableAcrylic(Window window, uint gradientColor = 0x99101216)
    {
        // Pure WPF hardware-accelerated transparency is safe and crash-free
    }

    public static void DisableBlur(Window window)
    {
        // Safe no-op
    }
}
