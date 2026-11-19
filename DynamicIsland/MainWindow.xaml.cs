using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using DynamicIsland.Models;
using DynamicIsland.Services;
using DynamicIsland.Views;

namespace DynamicIsland;

public class DroppedFileItem
{
    public string Path { get; set; } = "";
    public string Name => System.IO.Path.GetFileName(Path);
}

public partial class MainWindow : Window
{
    private bool _isExpanded;
    private readonly DispatcherTimer _hoverTimer;
    private readonly DispatcherTimer _autoCollapseTimer;
    private readonly DispatcherTimer _progressTimer;
    private readonly DispatcherTimer _visualizerTimer;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _stopwatchTimer;
    private readonly Stopwatch _stopwatch = new();
    private readonly Random _rand = new();

    private TimeSpan _currentPosition = TimeSpan.Zero;
    private TimeSpan _currentDuration = TimeSpan.Zero;
    private bool _isPlaying;
    private bool _isUpdatingVolumeUi;
    private bool _isScrubbing;

    public ObservableCollection<DroppedFileItem> ShelfFiles { get; } = [];

    public MainWindow()
    {
        InitializeComponent();

        ListFiles.ItemsSource = ShelfFiles;

        _hoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _hoverTimer.Tick += (_, _) =>
        {
            _hoverTimer.Stop();
            ExpandIsland();
        };

        _autoCollapseTimer = new DispatcherTimer();
        _autoCollapseTimer.Tick += (_, _) =>
        {
            _autoCollapseTimer.Stop();
            if (!IsMouseOver) CollapseIsland();
        };

        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _progressTimer.Tick += ProgressTimer_Tick;

        _visualizerTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _visualizerTimer.Tick += VisualizerTimer_Tick;

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();

        _stopwatchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _stopwatchTimer.Tick += (_, _) =>
        {
            var ts = _stopwatch.Elapsed;
            TextStopwatch.Text = $"{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
        };

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyInitialSizing();
            UpdateClock();

            // Subscribe to Media events
            MediaService.Instance.TrackChanged += OnTrackChanged;
            MediaService.Instance.PlaybackStateChanged += OnPlaybackStateChanged;
            UpdateMediaUi(MediaService.Instance.CurrentTrack);

            // Subscribe to Volume events
            VolumeService.Instance.VolumeChanged += (vol, muted) =>
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateVolumeUi(vol, muted);
                    if (!_isExpanded)
                    {
                        SwitchToTab(TabSound);
                        TriggerEventExpansion(2.5);
                    }
                });
            };
            UpdateVolumeUi(VolumeService.Instance.CurrentVolume, VolumeService.Instance.IsMuted);

            // Subscribe to Weather events
            WeatherService.Instance.WeatherUpdated += data =>
            {
                Dispatcher.Invoke(() => UpdateWeatherUi(data));
            };
            if (WeatherService.Instance.CurrentWeather != null)
            {
                UpdateWeatherUi(WeatherService.Instance.CurrentWeather);
            }

            // Subscribe to Stats events
            SystemStatsService.Instance.StatsUpdated += stats =>
            {
                Dispatcher.Invoke(() => UpdateStatsUi(stats));
            };
            UpdateStatsUi(SystemStatsService.Instance.CurrentStats);

            Recenter();
        }
        catch (Exception ex)
        {
            try { File.AppendAllText("crash.log", $"[{DateTime.Now}] Loaded: {ex}\n"); } catch { }
        }
    }

    private void ApplyInitialSizing()
    {
        Width = 236;
        Height = 58;
        IslandPill.Width = 216;
        IslandPill.Height = 38;
        IslandPill.CornerRadius = new CornerRadius(19);
        CompactContainer.Visibility = Visibility.Visible;
        ExpandedContainer.Visibility = Visibility.Collapsed;
    }

    public void Recenter()
    {
        double screenW = SystemParameters.PrimaryScreenWidth;
        Top = 10;
        Left = (screenW - Width) / 2;
    }

    // =========================================================================
    // NAVIGATION & TAB SWITCHING
    // =========================================================================
    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (ModuleMedia == null) return;

        ModuleMedia.Visibility = TabMedia.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        ModuleSound.Visibility = TabSound.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        ModuleClock.Visibility = TabClock.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        ModuleWeather.Visibility = TabWeather.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        ModulePC.Visibility = TabPC.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        ModuleShelf.Visibility = TabShelf.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SwitchToTab(RadioButton tab)
    {
        tab.IsChecked = true;
    }

    // =========================================================================
    // EXPAND & COLLAPSE (CUBIC SMOOTH FLUID MORPHING)
    // =========================================================================
    private void TriggerEventExpansion(double durationSeconds)
    {
        ExpandIsland();
        _autoCollapseTimer.Stop();
        _autoCollapseTimer.Interval = TimeSpan.FromSeconds(durationSeconds);
        _autoCollapseTimer.Start();
    }

    private void ExpandIsland()
    {
        if (_isExpanded) return;
        _isExpanded = true;

        CompactContainer.Visibility = Visibility.Collapsed;
        ExpandedContainer.Visibility = Visibility.Visible;

        AnimateMorph(targetWindowW: 464, targetWindowH: 212, targetPillW: 444, targetPillH: 192, targetRadius: 34);
    }

    private void CollapseIsland()
    {
        if (!_isExpanded) return;
        _isExpanded = false;

        ExpandedContainer.Visibility = Visibility.Collapsed;
        CompactContainer.Visibility = Visibility.Visible;

        AnimateMorph(targetWindowW: 236, targetWindowH: 58, targetPillW: 216, targetPillH: 38, targetRadius: 19);
    }

    private void AnimateMorph(double targetWindowW, double targetWindowH, double targetPillW, double targetPillH, double targetRadius)
    {
        double screenW = SystemParameters.PrimaryScreenWidth;
        double targetLeft = (screenW - targetWindowW) / 2;

        var duration = TimeSpan.FromMilliseconds(320);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        // Animate Window bounds
        var winWAnim = new DoubleAnimation(targetWindowW, duration) { EasingFunction = ease };
        var winHAnim = new DoubleAnimation(targetWindowH, duration) { EasingFunction = ease };
        var winLAnim = new DoubleAnimation(targetLeft, duration) { EasingFunction = ease };

        BeginAnimation(WidthProperty, winWAnim);
        BeginAnimation(HeightProperty, winHAnim);
        BeginAnimation(LeftProperty, winLAnim);

        // Animate Island Pill Dimensions
        var pillWAnim = new DoubleAnimation(targetPillW, duration) { EasingFunction = ease };
        var pillHAnim = new DoubleAnimation(targetPillH, duration) { EasingFunction = ease };

        IslandPill.BeginAnimation(WidthProperty, pillWAnim);
        IslandPill.BeginAnimation(HeightProperty, pillHAnim);
        IslandPill.CornerRadius = new CornerRadius(targetRadius);
    }

    // =========================================================================
    // MEDIA PLAYER MODULE
    // =========================================================================
    private void OnTrackChanged(MediaTrackInfo track)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateMediaUi(track);
            if (track.IsPlaying && !string.IsNullOrWhiteSpace(track.Title) && !_isExpanded)
            {
                SwitchToTab(TabMedia);
                TriggerEventExpansion(3.0);
            }
        });
    }

    private void OnPlaybackStateChanged(bool isPlaying)
    {
        Dispatcher.Invoke(() =>
        {
            _isPlaying = isPlaying;
            UpdatePlayPauseIcon(isPlaying);
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

    private void UpdateMediaUi(MediaTrackInfo track)
    {
        string title = string.IsNullOrWhiteSpace(track.Title) ? "Музыка не играет" : track.Title;
        string artist = string.IsNullOrWhiteSpace(track.Artist) ? "Включите трек" : track.Artist;

        CompactTitle.Text = title;
        CompactArtist.Text = artist;
        ExpandedTitle.Text = title;
        ExpandedArtist.Text = artist;

        CompactArt.Source = track.AlbumArt;
        ExpandedArt.Source = track.AlbumArt;

        _isPlaying = track.IsPlaying;
        UpdatePlayPauseIcon(_isPlaying);

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

    private void UpdatePlayPauseIcon(bool isPlaying)
    {
        var geomKey = isPlaying ? "PauseGeometry" : "PlayGeometry";
        if (TryFindResource(geomKey) is Geometry geom)
        {
            IconPlayPause.Data = geom;
            IconPlayPause.Margin = isPlaying ? new Thickness(0) : new Thickness(2, 0, 0, 0);
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

        if (_currentDuration > TimeSpan.Zero)
        {
            var remaining = _currentDuration - _currentPosition;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            TextTotal.Text = $"-{FormatTime(remaining)}";

            if (!_isScrubbing)
            {
                double pct = (_currentPosition.TotalSeconds / _currentDuration.TotalSeconds) * 100.0;
                PlaybackProgress.Value = Math.Clamp(pct, 0.0, 100.0);
            }
        }
        else
        {
            TextTotal.Text = "0:00";
            if (!_isScrubbing)
            {
                PlaybackProgress.Value = 0;
            }
        }
    }

    private void PlaybackProgress_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isScrubbing = true;
    }

    private void PlaybackProgress_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isScrubbing = false;
    }

    private void PlaybackProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isScrubbing && _currentDuration > TimeSpan.Zero)
        {
            double sec = (_currentDuration.TotalSeconds * e.NewValue) / 100.0;
            _currentPosition = TimeSpan.FromSeconds(sec);
            TextElapsed.Text = FormatTime(_currentPosition);
            var remaining = _currentDuration - _currentPosition;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            TextTotal.Text = $"-{FormatTime(remaining)}";
        }
    }

    private static string FormatTime(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return ts.ToString(@"h\:mm\:ss");
        return ts.ToString(@"m\:ss");
    }

    private void VisualizerTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isPlaying) return;

        Bar1.Height = _rand.Next(4, 12);
        Bar2.Height = _rand.Next(6, 16);
        Bar3.Height = _rand.Next(3, 10);
        Bar4.Height = _rand.Next(5, 14);

        ExpBar1.Height = _rand.Next(6, 18);
        ExpBar2.Height = _rand.Next(10, 24);
        ExpBar3.Height = _rand.Next(4, 14);
        ExpBar4.Height = _rand.Next(8, 22);
        ExpBar5.Height = _rand.Next(5, 16);
    }

    private void ResetVisualizer()
    {
        Bar1.Height = 4; Bar2.Height = 6; Bar3.Height = 3; Bar4.Height = 5;
        ExpBar1.Height = 6; ExpBar2.Height = 8; ExpBar3.Height = 5; ExpBar4.Height = 7; ExpBar5.Height = 6;
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

    // =========================================================================
    // VOLUME MODULE
    // =========================================================================
    private void UpdateVolumeUi(float vol, bool muted)
    {
        _isUpdatingVolumeUi = true;
        try
        {
            int percent = (int)Math.Round(vol * 100);
            ExpPercent.Text = muted ? "Mute" : $"{percent}%";
            TextMuteStatus.Text = muted ? "Звук выключен" : "Вывод звука активен";
            ExpSlider.Value = percent;

            var geomKey = muted ? "VolumeMuteGeometry" : (percent > 40 ? "VolumeWaveHighGeometry" : "VolumeSpeakerGeometry");
            if (TryFindResource(geomKey) is Geometry geom)
            {
                IconVolumeSpeaker.Data = geom;
            }
        }
        finally
        {
            _isUpdatingVolumeUi = false;
        }
    }

    private void ExpSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isUpdatingVolumeUi && IsLoaded)
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

    // =========================================================================
    // CLOCK & STOPWATCH MODULE
    // =========================================================================
    private void UpdateClock()
    {
        var now = DateTime.Now;
        var ru = new CultureInfo("ru-RU");

        TextBigTime.Text = now.ToString("HH:mm:ss");
        TextBigDate.Text = now.ToString("dddd, d MMMM yyyy", ru);
    }

    private void BtnSwStartPause_Click(object sender, RoutedEventArgs e)
    {
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Stop();
            _stopwatchTimer.Stop();
            BtnSwStartPause.Content = "Старт";
            BtnSwStartPause.Background = (Brush)FindResource("AccentGreenBrush");
        }
        else
        {
            _stopwatch.Start();
            _stopwatchTimer.Start();
            BtnSwStartPause.Content = "Пауза";
            BtnSwStartPause.Background = (Brush)FindResource("AccentOrangeBrush");
        }
    }

    private void BtnSwReset_Click(object sender, RoutedEventArgs e)
    {
        _stopwatch.Reset();
        _stopwatchTimer.Stop();
        TextStopwatch.Text = "00:00.0";
        BtnSwStartPause.Content = "Старт";
        BtnSwStartPause.Background = (Brush)FindResource("AccentGreenBrush");
    }

    // =========================================================================
    // WEATHER MODULE
    // =========================================================================
    private void UpdateWeatherUi(WeatherData data)
    {
        string sign = data.Temperature > 0 ? "+" : "";
        TextExpTemp.Text = $"{sign}{data.Temperature:0}°";
        TextDesc.Text = data.Description;
        TextCity.Text = $"{ConfigService.Instance.Config.WeatherCity} • {data.Description}";
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        BtnRefresh.IsEnabled = false;
        await WeatherService.Instance.RefreshWeatherAsync(force: true);
        BtnRefresh.IsEnabled = true;
    }

    // =========================================================================
    // PC TELEMETRY MODULE
    // =========================================================================
    private void UpdateStatsUi(SystemStats stats)
    {
        TextExpCpu.Text = $"{stats.CpuPercent:0}%";
        BarCpu.Value = stats.CpuPercent;

        TextExpRam.Text = $"{stats.RamPercent:0}%";
        BarRam.Value = stats.RamPercent;

        if (stats.HasBattery)
        {
            LabelBattery.Text = stats.IsCharging ? "CHG" : "BAT";
            TextExpBattery.Text = $"{stats.BatteryPercent}%";
            BarBattery.Value = stats.BatteryPercent;
        }
        else
        {
            LabelBattery.Text = "PWR";
            TextExpBattery.Text = "220V";
            BarBattery.Value = 100;
        }
    }

    // =========================================================================
    // FILE SHELF MODULE (DRAG & DROP)
    // =========================================================================
    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            ExpandIsland();
            SwitchToTab(TabShelf);
        }
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                foreach (var f in files)
                {
                    if (File.Exists(f) || Directory.Exists(f))
                    {
                        if (!ShelfFiles.Any(x => x.Path.Equals(f, StringComparison.OrdinalIgnoreCase)))
                        {
                            ShelfFiles.Add(new DroppedFileItem { Path = f });
                        }
                    }
                }

                DropPrompt.Visibility = ShelfFiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                ListFiles.Visibility = ShelfFiles.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    private void BtnClearAll_Click(object sender, RoutedEventArgs e)
    {
        ShelfFiles.Clear();
        DropPrompt.Visibility = Visibility.Visible;
        ListFiles.Visibility = Visibility.Collapsed;
    }

    private void BtnCopyFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path && File.Exists(path))
        {
            var coll = new System.Collections.Specialized.StringCollection { path };
            Clipboard.SetFileDropList(coll);
        }
    }

    private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }

    // =========================================================================
    // MOUSE HOVER & CONTEXT MENU
    // =========================================================================
    private void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        _autoCollapseTimer.Stop();
        if (!_isExpanded)
        {
            _hoverTimer.Start();
        }
    }

    private void Window_MouseLeave(object sender, MouseEventArgs e)
    {
        _hoverTimer.Stop();
        if (_isExpanded)
        {
            _autoCollapseTimer.Stop();
            _autoCollapseTimer.Interval = TimeSpan.FromMilliseconds(500);
            _autoCollapseTimer.Start();
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftAlt))
            {
                DragMove();
                return;
            }

            if (!_isExpanded)
            {
                ExpandIsland();
            }
        }
    }

    private void MenuSettings_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow { Owner = this };
        win.ShowDialog();
    }

    private void MenuRecenter_Click(object sender, RoutedEventArgs e)
    {
        Recenter();
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
