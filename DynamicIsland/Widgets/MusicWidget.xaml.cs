using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DynamicIsland.Services;

namespace DynamicIsland.Widgets;

public partial class MusicWidget : UserControl, IIslandWidget
{
    public string WidgetId => "music";
    public string DisplayName => "Медиаплеер";
    public string Icon => "🎵";
    public bool IsEnabled => ConfigService.Instance.Config.EnableMusicWidget;

    private readonly DispatcherTimer _progressTimer;
    private readonly DispatcherTimer _visualizerTimer;
    private readonly Random _rand = new();

    private TimeSpan _currentPosition = TimeSpan.Zero;
    private TimeSpan _currentDuration = TimeSpan.Zero;
    private bool _isPlaying;

    public MusicWidget()
    {
        InitializeComponent();

        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _progressTimer.Tick += ProgressTimer_Tick;

        _visualizerTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _visualizerTimer.Tick += VisualizerTimer_Tick;

        MediaService.Instance.TrackChanged += OnTrackChanged;
        MediaService.Instance.PlaybackStateChanged += OnPlaybackStateChanged;

        UpdateUi(MediaService.Instance.CurrentTrack);
    }

    public void SetExpanded(bool isExpanded)
    {
        CompactContainer.Visibility = isExpanded ? Visibility.Collapsed : Visibility.Visible;
        ExpandedContainer.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnTrackChanged(MediaTrackInfo track)
    {
        Dispatcher.Invoke(() => UpdateUi(track));
    }

    private void OnPlaybackStateChanged(bool isPlaying)
    {
        Dispatcher.Invoke(() =>
        {
            _isPlaying = isPlaying;
            IconPlayPause.Text = isPlaying ? "⏸" : "▶";
            if (isPlaying)
            {
                if (!_progressTimer.IsEnabled) _progressTimer.Start();
                if (!_visualizerTimer.IsEnabled) _visualizerTimer.Start();
            }
            else
            {
                _visualizerTimer.Stop();
                ResetVisualizer();
            }
        });
    }

    private void UpdateUi(MediaTrackInfo track)
    {
        string title = string.IsNullOrWhiteSpace(track.Title) ? "Нет трека" : track.Title;
        string artist = string.IsNullOrWhiteSpace(track.Artist) ? "Неизвестный исполнитель" : track.Artist;

        CompactTitle.Text = title;
        CompactArtist.Text = artist;
        ExpandedTitle.Text = title;
        ExpandedArtist.Text = artist;

        CompactArt.Source = track.AlbumArt;
        ExpandedArt.Source = track.AlbumArt;

        _isPlaying = track.IsPlaying;
        IconPlayPause.Text = _isPlaying ? "⏸" : "▶";

        _currentPosition = track.Position;
        _currentDuration = track.Duration;

        UpdateProgressUi();

        if (_isPlaying)
        {
            if (!_progressTimer.IsEnabled) _progressTimer.Start();
            if (!_visualizerTimer.IsEnabled) _visualizerTimer.Start();
        }
        else
        {
            _visualizerTimer.Stop();
            ResetVisualizer();
        }
    }

    private void ProgressTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isPlaying) return;

        _currentPosition = _currentPosition.Add(TimeSpan.FromSeconds(1));
        if (_currentDuration > TimeSpan.Zero && _currentPosition > _currentDuration)
        {
            _currentPosition = _currentDuration;
        }

        UpdateProgressUi();
    }

    private void UpdateProgressUi()
    {
        TextElapsed.Text = FormatTime(_currentPosition);
        TextTotal.Text = FormatTime(_currentDuration);

        if (_currentDuration.TotalSeconds > 0)
        {
            double pct = (_currentPosition.TotalSeconds / _currentDuration.TotalSeconds) * 100.0;
            PlaybackProgress.Value = Math.Clamp(pct, 0.0, 100.0);
        }
        else
        {
            PlaybackProgress.Value = 0;
        }
    }

    private static string FormatTime(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return ts.ToString(@"h\:mm\:ss");
        return ts.ToString(@"m\:ss");
    }

    private void VisualizerTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isPlaying) return;

        // Compact visualizer bars
        Bar1.Height = _rand.Next(4, 14);
        Bar2.Height = _rand.Next(6, 16);
        Bar3.Height = _rand.Next(3, 12);
        Bar4.Height = _rand.Next(5, 15);

        // Expanded visualizer bars
        ExpBar1.Height = _rand.Next(6, 20);
        ExpBar2.Height = _rand.Next(10, 26);
        ExpBar3.Height = _rand.Next(4, 16);
        ExpBar4.Height = _rand.Next(8, 24);
        ExpBar5.Height = _rand.Next(5, 18);
    }

    private void ResetVisualizer()
    {
        Bar1.Height = 4;
        Bar2.Height = 6;
        Bar3.Height = 3;
        Bar4.Height = 5;

        ExpBar1.Height = 6;
        ExpBar2.Height = 8;
        ExpBar3.Height = 5;
        ExpBar4.Height = 7;
        ExpBar5.Height = 6;
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
