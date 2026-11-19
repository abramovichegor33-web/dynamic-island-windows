using System.IO;

namespace DynamicIsland.Services;

/// <summary>
/// "Atoll doesn't open": a marker lives from the start until the island has been up for a few seconds (or was
/// closed normally). Still there at the next start means the last one died or hung on the way up — in a way no
/// exception handler sees (a crash inside Windows' own code). The island then starts with light graphics, which
/// is what helped people with 1.8.0 by hand.
/// </summary>
public static class StartupGuard
{
    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DynamicIsland", "startup.pending");

    private static bool _on;

    /// <summary>The previous start never got as far as "the island is up".</summary>
    public static bool PreviousFailed { get; private set; }

    public static void Begin()
    {
        _on = true;
        try
        {
            PreviousFailed = File.Exists(FilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, DateTime.UtcNow.ToString("o"));
        }
        catch (Exception e) { Log.Write("startup guard: " + e.Message); }
    }

    /// <summary>The island is up, or was closed on purpose: this start counts as good.</summary>
    public static void Done()
    {
        if (!_on) return;
        try { File.Delete(FilePath); }
        catch (Exception e) { Log.Write("startup guard: " + e.Message); }
    }
}
