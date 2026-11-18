namespace DynamicIsland.Models;

public class AppConfig
{
    // Appearance & Themes
    public string SelectedThemeId { get; set; } = "deep-black";
    public IslandTheme CustomTheme { get; set; } = new IslandTheme
    {
        Id = "custom",
        Name = "Пользовательская тема",
        BackgroundColor = "#0A0A0A",
        BackgroundOpacity = 0.95,
        EnableAcrylicBlur = false,
        BorderColor = "#333333",
        BorderThickness = 1.0,
        CornerRadius = 18,
        TextPrimaryColor = "#FFFFFF",
        TextSecondaryColor = "#8E8E93",
        AccentColor = "#30D158",
        EnableAuraGlow = false,
        AuraGlowColor = "#30D158",
        AuraGlowRadius = 12
    };

    // Dimensions
    public double CompactWidth { get; set; } = 210;
    public double CompactHeight { get; set; } = 38;
    public double ExpandedWidth { get; set; } = 440;
    public double ExpandedHeight { get; set; } = 185;
    public double TopOffset { get; set; } = 10;
    public double ScalePercent { get; set; } = 100; // 80 - 150 %

    // Behavior
    public bool ExpandOnHover { get; set; } = true;
    public int HoverDelayMs { get; set; } = 100;
    public int AutoCollapseSeconds { get; set; } = 3;
    public bool PlaySounds { get; set; } = false;
    public bool ShowShadow { get; set; } = true;

    // Active Widgets
    public bool EnableMusicWidget { get; set; } = true;
    public bool EnableVolumeWidget { get; set; } = true;
    public bool EnableClockWidget { get; set; } = true;
    public bool EnableWeatherWidget { get; set; } = true;
    public bool EnableStatsWidget { get; set; } = true;
    public bool EnableShelfWidget { get; set; } = true;
    public bool EnableNotificationWidget { get; set; } = true;

    // Widget specific
    public bool Clock24Hours { get; set; } = true;
    public bool ClockShowSeconds { get; set; } = false;
    public string WeatherCity { get; set; } = "Moscow";
    public double WeatherLatitude { get; set; } = 55.7558;
    public double WeatherLongitude { get; set; } = 37.6173;

    // Pinned shelf items (file paths)
    public List<string> PinnedFiles { get; set; } = [];
}
