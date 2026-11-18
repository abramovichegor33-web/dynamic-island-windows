using System.Windows;
using System.Windows.Controls;
using DynamicIsland.Services;

namespace DynamicIsland.Widgets;

public partial class SystemStatsWidget : UserControl, IIslandWidget
{
    public string WidgetId => "stats";
    public string DisplayName => "Мониторинг ПК";
    public string Icon => "⚡";
    public bool IsEnabled => ConfigService.Instance.Config.EnableStatsWidget;

    public FrameworkElement CompactView => CompactContainer;
    public FrameworkElement ExpandedView => ExpandedContainer;

    public SystemStatsWidget()
    {
        InitializeComponent();

        SystemStatsService.Instance.StatsUpdated += stats =>
        {
            Dispatcher.Invoke(() => UpdateUi(stats));
        };

        UpdateUi(SystemStatsService.Instance.CurrentStats);
    }

    private void UpdateUi(SystemStats stats)
    {
        TextCompactCpu.Text = $"CPU {stats.CpuPercent:0}%";
        TextCompactRam.Text = $"RAM {stats.RamPercent:0}%";

        TextExpCpu.Text = $"{stats.CpuPercent:0}%";
        BarCpu.Value = stats.CpuPercent;

        TextExpRam.Text = $"{stats.RamPercent:0}%";
        BarRam.Value = stats.RamPercent;

        if (stats.HasBattery)
        {
            string chargeIcon = stats.IsCharging ? "⚡" : "🔋";
            LabelBattery.Text = $"{chargeIcon} Батарея";
            TextExpBattery.Text = $"{stats.BatteryPercent}%";
            BarBattery.Value = stats.BatteryPercent;
        }
        else
        {
            LabelBattery.Text = "🔌 Питание";
            TextExpBattery.Text = "Сеть 220V";
            BarBattery.Value = 100;
        }
    }

    public void OnActivated()
    {
        CompactContainer.Visibility = Visibility.Collapsed;
        ExpandedContainer.Visibility = Visibility.Visible;
        SystemStatsService.Instance.SetActiveSampling(true);
    }

    public void OnDeactivated()
    {
        ExpandedContainer.Visibility = Visibility.Collapsed;
        CompactContainer.Visibility = Visibility.Visible;
        SystemStatsService.Instance.SetActiveSampling(false);
    }
}
