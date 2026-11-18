using System.Text.Json.Serialization;

namespace DynamicIsland.Models;

public class IslandTheme
{
    public string Id { get; set; } = "deep-black";
    public string Name { get; set; } = "Apple OLED (Deep Black)";

    public string BackgroundColor { get; set; } = "#000000";
    public double BackgroundOpacity { get; set; } = 0.96;
    public bool EnableAcrylicBlur { get; set; } = false;

    public string BorderColor { get; set; } = "#262626";
    public double BorderThickness { get; set; } = 1.0;
    public double CornerRadius { get; set; } = 18.0;

    public string TextPrimaryColor { get; set; } = "#FFFFFF";
    public string TextSecondaryColor { get; set; } = "#8E8E93";
    public string AccentColor { get; set; } = "#30D158";

    public bool EnableAuraGlow { get; set; } = false;
    public string AuraGlowColor { get; set; } = "#30D158";
    public double AuraGlowRadius { get; set; } = 15.0;

    public static List<IslandTheme> GetPresets()
    {
        return
        [
            new IslandTheme
            {
                Id = "deep-black",
                Name = "Apple OLED (Глубокий черный)",
                BackgroundColor = "#000000",
                BackgroundOpacity = 0.98,
                EnableAcrylicBlur = false,
                BorderColor = "#282828",
                BorderThickness = 1.0,
                CornerRadius = 18,
                TextPrimaryColor = "#FFFFFF",
                TextSecondaryColor = "#8E8E93",
                AccentColor = "#30D158",
                EnableAuraGlow = false
            },
            new IslandTheme
            {
                Id = "frosted-glass",
                Name = "Frosted Glass (Матовое стекло)",
                BackgroundColor = "#15181E",
                BackgroundOpacity = 0.70,
                EnableAcrylicBlur = true,
                BorderColor = "#40FFFFFF",
                BorderThickness = 1.0,
                CornerRadius = 18,
                TextPrimaryColor = "#FFFFFF",
                TextSecondaryColor = "#B0B3B8",
                AccentColor = "#0A84FF",
                EnableAuraGlow = false
            },
            new IslandTheme
            {
                Id = "cyberpunk-neon",
                Name = "Cyberpunk Neon (Киберпанк)",
                BackgroundColor = "#080B10",
                BackgroundOpacity = 0.95,
                EnableAcrylicBlur = false,
                BorderColor = "#00F0FF",
                BorderThickness = 1.2,
                CornerRadius = 12,
                TextPrimaryColor = "#00F0FF",
                TextSecondaryColor = "#FF007F",
                AccentColor = "#FFE600",
                EnableAuraGlow = true,
                AuraGlowColor = "#00F0FF",
                AuraGlowRadius = 16
            },
            new IslandTheme
            {
                Id = "nord-frost",
                Name = "Nordic Frost (Северный холодный)",
                BackgroundColor = "#242933",
                BackgroundOpacity = 0.92,
                EnableAcrylicBlur = false,
                BorderColor = "#434C5E",
                BorderThickness = 1.0,
                CornerRadius = 18,
                TextPrimaryColor = "#ECEFF4",
                TextSecondaryColor = "#D8DEE9",
                AccentColor = "#88C0D0",
                EnableAuraGlow = true,
                AuraGlowColor = "#88C0D0",
                AuraGlowRadius = 12
            },
            new IslandTheme
            {
                Id = "sunset-amber",
                Name = "Sunset Glow (Закатный янтарь)",
                BackgroundColor = "#120D14",
                BackgroundOpacity = 0.95,
                EnableAcrylicBlur = false,
                BorderColor = "#FF6B6B",
                BorderThickness = 1.0,
                CornerRadius = 18,
                TextPrimaryColor = "#FFFFFF",
                TextSecondaryColor = "#FFA07A",
                AccentColor = "#FF9F43",
                EnableAuraGlow = true,
                AuraGlowColor = "#FF6B6B",
                AuraGlowRadius = 14
            }
        ];
    }
}
