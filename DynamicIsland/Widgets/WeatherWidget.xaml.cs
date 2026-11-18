using System.Windows;
using System.Windows.Controls;
using DynamicIsland.Services;

namespace DynamicIsland.Widgets;

public partial class WeatherWidget : UserControl, IIslandWidget
{
    public string WidgetId => "weather";
    public string DisplayName => "Погода";
    public string Icon => "🌤️";
    public bool IsEnabled => ConfigService.Instance.Config.EnableWeatherWidget;

    public FrameworkElement CompactView => CompactContainer;
    public FrameworkElement ExpandedView => ExpandedContainer;

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

    private void UpdateUi(WeatherData data)
    {
        string sign = data.Temperature > 0 ? "+" : "";
        string tempStr = $"{sign}{data.Temperature:0}°";

        IconCompact.Text = data.Icon;
        TextCompactTemp.Text = $"{tempStr}C";

        IconExpanded.Text = data.Icon;
        TextExpTemp.Text = tempStr;
        TextCity.Text = ConfigService.Instance.Config.WeatherCity;
        TextDesc.Text = data.Description;
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        BtnRefresh.IsEnabled = false;
        await WeatherService.Instance.RefreshWeatherAsync(force: true);
        BtnRefresh.IsEnabled = true;
    }

    public void OnActivated()
    {
        CompactContainer.Visibility = Visibility.Collapsed;
        ExpandedContainer.Visibility = Visibility.Visible;
    }

    public void OnDeactivated()
    {
        ExpandedContainer.Visibility = Visibility.Collapsed;
        CompactContainer.Visibility = Visibility.Visible;
    }
}
