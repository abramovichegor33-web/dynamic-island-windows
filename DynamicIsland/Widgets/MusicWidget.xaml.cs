using System.Windows;
using System.Windows.Controls;
using DynamicIsland.Services;

namespace DynamicIsland.Widgets;

public partial class MusicWidget : UserControl, IIslandWidget
{
    public string WidgetId => "music";
    public string DisplayName => "Медиаплеер";
    public string Icon => "🎵";
    public bool IsEnabled => ConfigService.Instance.Config.EnableMusicWidget;

    public FrameworkElement CompactView => CompactContainer;
    public FrameworkElement ExpandedView => ExpandedContainer;

    public MusicWidget()
    {
        InitializeComponent();

        MediaService.Instance.TrackChanged += OnTrackChanged;
        MediaService.Instance.PlaybackStateChanged += OnPlaybackStateChanged;

        UpdateUi(MediaService.Instance.CurrentTrack);
    }

    private void OnTrackChanged(MediaTrackInfo track)
    {
        Dispatcher.Invoke(() => UpdateUi(track));
    }

    private void OnPlaybackStateChanged(bool isPlaying)
    {
        Dispatcher.Invoke(() =>
        {
            CompactPlayingIndicator.Text = isPlaying ? "▶" : "⏸";
        });
    }

    private void UpdateUi(MediaTrackInfo track)
    {
        string title = string.IsNullOrWhiteSpace(track.Title) ? "Нет трека" : track.Title;
        string artist = track.Artist;

        CompactTitle.Text = title;
        CompactArtist.Text = artist;
        ExpandedTitle.Text = title;
        ExpandedArtist.Text = string.IsNullOrWhiteSpace(artist) ? "Неизвестный исполнитель" : artist;

        CompactArt.Source = track.AlbumArt;
        ExpandedArt.Source = track.AlbumArt;

        CompactPlayingIndicator.Text = track.IsPlaying ? "▶" : "⏸";

        if (track.Duration > TimeSpan.Zero)
        {
            PlaybackProgress.Value = (track.Position.TotalSeconds / track.Duration.TotalSeconds) * 100;
        }
        else
        {
            PlaybackProgress.Value = 0;
        }
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

    private async void BtnPlayPause_Click(object sender, RoutedEventArgs e)
    {
        await MediaService.Instance.PlayPauseAsync();
    }

    private async void BtnPrev_Click(object sender, RoutedEventArgs e)
    {
        await MediaService.Instance.PreviousTrackAsync();
    }

    private async void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        await MediaService.Instance.NextTrackAsync();
    }
}
