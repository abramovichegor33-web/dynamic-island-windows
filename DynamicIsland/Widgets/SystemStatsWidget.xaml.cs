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

    public SystemStatsWidget()
    {
        InitializeComponent();

        SystemStatsService.Instance.StatsUpdated += stats =>
        {
            Dispatcher.Invoke(() => UpdateUi(stats));
        };

        UpdateUi(SystemStatsService.Instance.CurrentStats);
    }

    public void SetExpanded(bool isExpanded)
    {
        CompactContainer.Visibility = isExpanded ? Visibility.Collapsed : Visibility.Visible;
        ExpandedContainer.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
        SystemStatsService.Instance.SetActiveSampling(isExpanded);
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
}
