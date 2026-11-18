using System.Windows;
using System.Windows.Media;
using DynamicIsland.Models;

namespace DynamicIsland.Services;

public class ThemeService
{
    private static readonly Lazy<ThemeService> _instance = new(() => new ThemeService());
    public static ThemeService Instance => _instance.Value;

    public IslandTheme CurrentTheme { get; private set; }

    public event Action<IslandTheme>? ThemeChanged;

    private ThemeService()
    {
        var config = ConfigService.Instance.Config;
        CurrentTheme = ResolveTheme(config);
        ApplyTheme(CurrentTheme);

        ConfigService.Instance.ConfigChanged += cfg =>
        {
            var newTheme = ResolveTheme(cfg);
            ApplyTheme(newTheme);
        };
    }

    public IslandTheme ResolveTheme(AppConfig config)
    {
        if (config.SelectedThemeId == "custom")
            return config.CustomTheme;

        var preset = IslandTheme.GetPresets().FirstOrDefault(p => p.Id == config.SelectedThemeId);
        return preset ?? IslandTheme.GetPresets()[0];
    }

    public void ApplyTheme(IslandTheme theme)
    {
        CurrentTheme = theme;

        Application.Current.Dispatcher.Invoke(() =>
        {
            var res = Application.Current.Resources;

            var bgCol = ParseColor(theme.BackgroundColor);
            bgCol.A = (byte)(theme.BackgroundOpacity * 255);
            res["IslandBgBrush"] = new SolidColorBrush(bgCol);

            var borderCol = ParseColor(theme.BorderColor);
            res["IslandBorderBrush"] = new SolidColorBrush(borderCol);
            res["IslandBorderThickness"] = new Thickness(theme.BorderThickness);
            res["IslandCornerRadius"] = new CornerRadius(theme.CornerRadius);

            var textPriCol = ParseColor(theme.TextPrimaryColor);
            res["TextPrimaryBrush"] = new SolidColorBrush(textPriCol);

            var textSecCol = ParseColor(theme.TextSecondaryColor);
            res["TextSecondaryBrush"] = new SolidColorBrush(textSecCol);

            var accentCol = ParseColor(theme.AccentColor);
            res["AccentBrush"] = new SolidColorBrush(accentCol);

            var auraCol = theme.EnableAuraGlow ? ParseColor(theme.AuraGlowColor) : Colors.Transparent;
            res["AuraGlowBrush"] = new SolidColorBrush(auraCol);
            res["AuraGlowRadius"] = theme.EnableAuraGlow ? theme.AuraGlowRadius : 0.0;

            ThemeChanged?.Invoke(theme);
        });
    }

    private static Color ParseColor(string hex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            return Colors.White;
        }
    }
}
