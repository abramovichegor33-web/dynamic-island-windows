using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DynamicIsland.Services;

namespace DynamicIsland.Widgets;

public partial class ClockWidget : UserControl, IIslandWidget
{
    public string WidgetId => "clock";
    public string DisplayName => "Часы и таймер";
    public string Icon => "⏱️";
    public bool IsEnabled => ConfigService.Instance.Config.EnableClockWidget;

    public FrameworkElement CompactView => CompactContainer;
    public FrameworkElement ExpandedView => ExpandedContainer;

    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _swTimer;
    private readonly Stopwatch _stopwatch = new();

    public ClockWidget()
    {
        InitializeComponent();

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();

        _swTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _swTimer.Tick += (_, _) =>
        {
            var ts = _stopwatch.Elapsed;
            TextStopwatch.Text = $"{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 100}";
        };

        UpdateClock();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        var ru = new CultureInfo("ru-RU");

        TextCompactTime.Text = now.ToString("HH:mm");
        TextCompactDate.Text = now.ToString("ddd, d MMM", ru);

        TextBigTime.Text = now.ToString("HH:mm:ss");
        TextBigDate.Text = now.ToString("dddd, d MMMM yyyy", ru);
    }

    private void BtnSwStartPause_Click(object sender, RoutedEventArgs e)
    {
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Stop();
            _swTimer.Stop();
            BtnSwStartPause.Content = "Старт";
        }
        else
        {
            _stopwatch.Start();
            _swTimer.Start();
            BtnSwStartPause.Content = "Пауза";
        }
    }

    private void BtnSwReset_Click(object sender, RoutedEventArgs e)
    {
        _stopwatch.Reset();
        _swTimer.Stop();
        TextStopwatch.Text = "00:00.0";
        BtnSwStartPause.Content = "Старт";
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
