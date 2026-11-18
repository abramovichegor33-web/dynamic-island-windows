using System.IO;
using System.Windows.Media.Imaging;
using Windows.Media.Control;

namespace DynamicIsland.Services;

public class MediaTrackInfo
{
    public string Title { get; set; } = "";
    public string Artist { get; set; } = "";
    public BitmapSource? AlbumArt { get; set; }
    public bool IsPlaying { get; set; }
    public TimeSpan Position { get; set; }
    public TimeSpan Duration { get; set; }
}

public class MediaService
{
    private static readonly Lazy<MediaService> _instance = new(() => new MediaService());
    public static MediaService Instance => _instance.Value;

    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;

    public MediaTrackInfo CurrentTrack { get; private set; } = new();

    public event Action<MediaTrackInfo>? TrackChanged;
    public event Action<bool>? PlaybackStateChanged;

    private MediaService()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
                UpdateCurrentSession(_sessionManager.GetCurrentSession());
            }
        }
        catch
        {
            // GSMTC may fail if Windows media service is stopped
        }
    }

    private void SessionManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        UpdateCurrentSession(sender.GetCurrentSession());
    }

    private void UpdateCurrentSession(GlobalSystemMediaTransportControlsSession? session)
    {
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= CurrentSession_MediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= CurrentSession_PlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged -= CurrentSession_TimelinePropertiesChanged;
        }

        _currentSession = session;

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += CurrentSession_MediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += CurrentSession_PlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged += CurrentSession_TimelinePropertiesChanged;
            _ = RefreshMediaPropertiesAsync();
        }
        else
        {
            CurrentTrack = new MediaTrackInfo { Title = "Нет воспроизведения", Artist = "", IsPlaying = false };
            TrackChanged?.Invoke(CurrentTrack);
        }
    }

    private void CurrentSession_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        _ = RefreshMediaPropertiesAsync();
    }

    private void CurrentSession_PlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        var info = sender.GetPlaybackInfo();
        if (info != null)
        {
            CurrentTrack.IsPlaying = info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            PlaybackStateChanged?.Invoke(CurrentTrack.IsPlaying);
        }
    }

    private void CurrentSession_TimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        var timeline = sender.GetTimelineProperties();
        if (timeline != null)
        {
            CurrentTrack.Position = timeline.Position;
            CurrentTrack.Duration = timeline.EndTime;
        }
    }

    private async Task RefreshMediaPropertiesAsync()
    {
        if (_currentSession == null) return;

        try
        {
            var props = await _currentSession.TryGetMediaPropertiesAsync();
            var playback = _currentSession.GetPlaybackInfo();
            var timeline = _currentSession.GetTimelineProperties();

            BitmapSource? albumArt = null;
            if (props?.Thumbnail != null)
            {
                try
                {
                    using var stream = await props.Thumbnail.OpenReadAsync();
                    using var netStream = stream.AsStreamForRead();
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = netStream;
                    bmp.EndInit();
                    bmp.Freeze();
                    albumArt = bmp;
                }
                catch { }
            }

            CurrentTrack = new MediaTrackInfo
            {
                Title = props?.Title ?? "",
                Artist = props?.Artist ?? "",
                AlbumArt = albumArt,
                IsPlaying = playback?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                Position = timeline?.Position ?? TimeSpan.Zero,
                Duration = timeline?.EndTime ?? TimeSpan.Zero
            };

            TrackChanged?.Invoke(CurrentTrack);
        }
        catch { }
    }

    public async Task PlayPauseAsync()
    {
        if (_currentSession != null)
            await _currentSession.TryTogglePlayPauseAsync();
    }

    public async Task NextTrackAsync()
    {
        if (_currentSession != null)
            await _currentSession.TrySkipNextAsync();
    }

    public async Task PreviousTrackAsync()
    {
        if (_currentSession != null)
            await _currentSession.TrySkipPreviousAsync();
    }
}
