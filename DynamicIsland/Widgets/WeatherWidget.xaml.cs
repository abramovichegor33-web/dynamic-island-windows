using System.Windows;
using System.Windows.Controls;
using DynamicIsland.Services;

namespace DynamicIsland.Widgets;

public partial class WeatherWidget : UserControl, IIslandWidget
{
    public string WidgetId => "weather";
    public string DisplayName => "Погода";
    public string Icon => "⛅";
    public bool IsEnabled => ConfigService.Instance.Config.EnableWeatherWidget;

    public WeatherWidget()
    {
        InitializeComponent();

        WeatherService.Instance.WeatherUpdated += data =>
        {
            Dispatcher.Invoke(() => UpdateUi(data));
        };

        if (WeatherService.Instance.CurrentWeather != null)
        {
            UpdateUi(WeatherService.Instance.CurrentWeather);
        }
    }

    public void SetExpanded(bool isExpanded)
    {
        CompactContainer.Visibility = isExpanded ? Visibility.Collapsed : Visibility.Visible;
        ExpandedContainer.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateUi(WeatherData data)
    {
        string sign = data.Temperature > 0 ? "+" : "";
        string tempStr = $"{sign}{data.Temperature:0}°C";

        IconCompact.Text = data.Icon;
        TextCompactTemp.Text = tempStr;

        IconExpanded.Text = data.Icon;
        TextExpTemp.Text = $"{sign}{data.Temperature:0}°";
        TextDesc.Text = data.Description;
        TextCity.Text = ConfigService.Instance.Config.WeatherCity;
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        BtnRefresh.IsEnabled = false;
        await WeatherService.Instance.RefreshWeatherAsync(force: true);
        BtnRefresh.IsEnabled = true;
    }
}
