using System.Windows;
using System.Windows.Controls;
using DynamicIsland.Services;

namespace DynamicIsland.Widgets;

public partial class VolumeWidget : UserControl, IIslandWidget
{
    public string WidgetId => "volume";
    public string DisplayName => "Громкость";
    public string Icon => "🔊";
    public bool IsEnabled => ConfigService.Instance.Config.EnableVolumeWidget;

    public FrameworkElement CompactView => CompactContainer;
    public FrameworkElement ExpandedView => ExpandedContainer;

    public VolumeWidget()
    {
        InitializeComponent();

        VolumeService.Instance.VolumeChanged += (vol, muted) =>
        {
            Dispatcher.Invoke(() => UpdateUi(vol, muted));
        };

        UpdateUi(VolumeService.Instance.CurrentVolume, VolumeService.Instance.IsMuted);
    }

    private void UpdateUi(float vol, bool muted)
    {
        int percent = (int)Math.Round(vol * 100);
        string icon = muted ? "🔇" : (percent == 0 ? "🔈" : (percent < 50 ? "🔉" : "🔊"));

        IconSpeaker.Text = icon;
        TextPercent.Text = muted ? "Mute" : $"{percent}%";
        VolBar.Value = muted ? 0 : percent;

        ExpIcon.Text = $"{icon} Громкость системы";
        ExpPercent.Text = muted ? "Без звука" : $"{percent}%";
        ExpSlider.Value = percent;
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

    private void ExpSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // slider can be extended to set volume if desired
    }
}
