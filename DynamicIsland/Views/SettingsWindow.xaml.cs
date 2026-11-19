using System.Windows;
using DynamicIsland.Models;
using DynamicIsland.Services;

namespace DynamicIsland.Views;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _config;

    public SettingsWindow()
    {
        InitializeComponent();

        _config = ConfigService.Instance.Config;
        LoadSettingsIntoUi();
    }

    private void LoadSettingsIntoUi()
    {
        // Presets
        var presets = IslandTheme.GetPresets();
        ComboThemePreset.ItemsSource = presets;
        ComboThemePreset.DisplayMemberPath = "Name";

        var selected = presets.FirstOrDefault(p => p.Id == _config.SelectedThemeId);
        if (selected != null)
        {
            ComboThemePreset.SelectedItem = selected;
        }

        // Custom
        var custom = _config.CustomTheme;
        TxtBgColor.Text = custom.BackgroundColor;
        TxtBorderColor.Text = custom.BorderColor;
        TxtAccentColor.Text = custom.AccentColor;
        SliderOpacity.Value = custom.BackgroundOpacity * 100;
        SliderRadius.Value = custom.CornerRadius;
        ChkAcrylicBlur.IsChecked = custom.EnableAcrylicBlur;
        ChkAuraGlow.IsChecked = custom.EnableAuraGlow;

        // Size & Position
        SliderScale.Value = _config.ScalePercent;
        SliderTopOffset.Value = _config.TopOffset;

        // Behavior
        ChkExpandOnHover.IsChecked = _config.ExpandOnHover;
        ChkShowShadow.IsChecked = _config.ShowShadow;

        // Widgets
        ChkWidgetMusic.IsChecked = _config.EnableMusicWidget;
        ChkWidgetVolume.IsChecked = _config.EnableVolumeWidget;
        ChkWidgetClock.IsChecked = _config.EnableClockWidget;
        ChkWidgetWeather.IsChecked = _config.EnableWeatherWidget;
        TxtWeatherCity.Text = _config.WeatherCity;
        ChkWidgetStats.IsChecked = _config.EnableStatsWidget;
        ChkWidgetShelf.IsChecked = _config.EnableShelfWidget;
        ChkWidgetNotification.IsChecked = _config.EnableNotificationWidget;
    }

    private void ComboThemePreset_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ComboThemePreset.SelectedItem is IslandTheme theme)
        {
            TxtBgColor.Text = theme.BackgroundColor;
            TxtBorderColor.Text = theme.BorderColor;
            TxtAccentColor.Text = theme.AccentColor;
            SliderOpacity.Value = theme.BackgroundOpacity * 100;
            SliderRadius.Value = theme.CornerRadius;
            ChkAcrylicBlur.IsChecked = theme.EnableAcrylicBlur;
            ChkAuraGlow.IsChecked = theme.EnableAuraGlow;
        }
    }

    private void SliderOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblOpacity != null)
            LblOpacity.Text = $"Непрозрачность: {(int)e.NewValue}%";
    }

    private void SliderRadius_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblRadius != null)
            LblRadius.Text = $"Скругление углов: {(int)e.NewValue} px";
    }

    private void SliderScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblScale != null)
            LblScale.Text = $"Общий масштаб: {(int)e.NewValue}%";
    }

    private void SliderTopOffset_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LblTopOffset != null)
            LblTopOffset.Text = $"Отступ от верхнего края экрана: {(int)e.NewValue} px";
    }

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (ComboThemePreset.SelectedItem is IslandTheme selPreset)
        {
            _config.SelectedThemeId = selPreset.Id;
        }

        // Save Custom theme
        _config.CustomTheme.BackgroundColor = TxtBgColor.Text.Trim();
        _config.CustomTheme.BorderColor = TxtBorderColor.Text.Trim();
        _config.CustomTheme.AccentColor = TxtAccentColor.Text.Trim();
        _config.CustomTheme.BackgroundOpacity = SliderOpacity.Value / 100.0;
        _config.CustomTheme.CornerRadius = SliderRadius.Value;
        _config.CustomTheme.EnableAcrylicBlur = ChkAcrylicBlur.IsChecked == true;
        _config.CustomTheme.EnableAuraGlow = ChkAuraGlow.IsChecked == true;

        // Size & Position
        _config.ScalePercent = SliderScale.Value;
        _config.TopOffset = SliderTopOffset.Value;

        // Behavior
        _config.ExpandOnHover = ChkExpandOnHover.IsChecked == true;
        _config.ShowShadow = ChkShowShadow.IsChecked == true;

        // Widgets
        _config.EnableMusicWidget = ChkWidgetMusic.IsChecked == true;
        _config.EnableVolumeWidget = ChkWidgetVolume.IsChecked == true;
        _config.EnableClockWidget = ChkWidgetClock.IsChecked == true;
        _config.EnableWeatherWidget = ChkWidgetWeather.IsChecked == true;
        _config.WeatherCity = TxtWeatherCity.Text.Trim();
        _config.EnableStatsWidget = ChkWidgetStats.IsChecked == true;
        _config.EnableShelfWidget = ChkWidgetShelf.IsChecked == true;
        _config.EnableNotificationWidget = ChkWidgetNotification.IsChecked == true;

        ConfigService.Instance.Save(_config);
        Close();
    }
}
