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

    private bool _isUpdatingUi;

    public VolumeWidget()
    {
        InitializeComponent();

        VolumeService.Instance.VolumeChanged += (vol, muted) =>
        {
            Dispatcher.Invoke(() => UpdateUi(vol, muted));
        };

        UpdateUi(VolumeService.Instance.CurrentVolume, VolumeService.Instance.IsMuted);
    }

    public void SetExpanded(bool isExpanded)
    {
        CompactContainer.Visibility = isExpanded ? Visibility.Collapsed : Visibility.Visible;
        ExpandedContainer.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateUi(float vol, bool muted)
    {
        _isUpdatingUi = true;
        try
        {
            int percent = (int)Math.Round(vol * 100);
            string icon = muted ? "🔇" : (percent == 0 ? "🔈" : (percent < 50 ? "🔉" : "🔊"));

            IconSpeaker.Text = icon;
            TextPercent.Text = muted ? "Mute" : $"{percent}%";
            VolBar.Value = muted ? 0 : percent;

            ExpIcon.Text = icon;
            TextMuteStatus.Text = muted ? "Звук отключен" : "Вывод звука активен";
            ExpPercent.Text = muted ? "Без звука" : $"{percent}%";
            ExpSlider.Value = percent;
        }
        finally
        {
            _isUpdatingUi = false;
        }
    }

    private void ExpSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isUpdatingUi && IsLoaded)
        {
            VolumeService.Instance.SetVolume((float)(e.NewValue / 100.0));
        }
    }

    private void BtnMute_Click(object sender, RoutedEventArgs e)
    {
        VolumeService.Instance.ToggleMute();
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out int val))
        {
            VolumeService.Instance.SetVolume(val / 100f);
        }
    }
}
