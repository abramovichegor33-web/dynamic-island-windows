using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using DynamicIsland.Models;
using DynamicIsland.Services;
using DynamicIsland.Views;

namespace DynamicIsland;

public partial class MainWindow : Window
{
    private bool _isExpanded;
    private readonly DispatcherTimer _hoverTimer;
    private readonly DispatcherTimer _autoCollapseTimer;
    private readonly DispatcherTimer _progressTimer;
    private readonly DispatcherTimer _visualizerTimer;
    private readonly Random _rand = new();

    private TimeSpan _currentPosition = TimeSpan.Zero;
    private TimeSpan _currentDuration = TimeSpan.Zero;
    private bool _isPlaying;
    private bool _isScrubbing;

    public MainWindow()
    {
        InitializeComponent();

        _hoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _hoverTimer.Tick += (_, _) =>
        {
            _hoverTimer.Stop();
            if (ConfigService.Instance.Config.ExpandOnHover)
            {
                ExpandIsland();
            }
        };

        _autoCollapseTimer = new DispatcherTimer();
        _autoCollapseTimer.Tick += (_, _) =>
        {
            _autoCollapseTimer.Stop();
            if (!IsMouseOver && _isExpanded)
            {
                CollapseIsland();
            }
        };

        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _progressTimer.Tick += ProgressTimer_Tick;

        _visualizerTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
        _visualizerTimer.Tick += VisualizerTimer_Tick;

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ApplyInitialSizing();

            // Subscribe to Media events
            MediaService.Instance.TrackChanged += OnTrackChanged;
            MediaService.Instance.PlaybackStateChanged += OnPlaybackStateChanged;
            UpdateMediaUi(MediaService.Instance.CurrentTrack);

            // Subscribe to Config events
            ConfigService.Instance.ConfigChanged += OnConfigChanged;

            Recenter();
        }
        catch (Exception ex)
        {
            try { File.AppendAllText("crash.log", $"[{DateTime.Now}] Loaded: {ex}\n"); } catch { }
        }
    }

    private void ApplyInitialSizing()
    {
        Width = 480;
        Height = 250;

        IslandPill.Width = 216;
        IslandPill.Height = 38;
        IslandPill.CornerRadius = new CornerRadius(19);

        AuraGlow.Width = 216;
        AuraGlow.Height = 38;
        AuraGlow.CornerRadius = new CornerRadius(19);

        CompactContainer.Visibility = Visibility.Visible;
        ExpandedContainer.Visibility = Visibility.Collapsed;

        ApplyThemeVisuals();
    }

    public void Recenter()
    {
        var screenW = SystemParameters.PrimaryScreenWidth;
        Left = (screenW - Width) / 2.0;
        Top = ConfigService.Instance.Config.TopOffset;
    }

    private void OnConfigChanged(AppConfig config)
    {
        Dispatcher.Invoke(() =>
        {
            ApplyThemeVisuals();
            Recenter();
        });
    }

    private void ApplyThemeVisuals()
    {
        var config = ConfigService.Instance.Config;
        PillShadow.Opacity = config.ShowShadow ? 0.45 : 0.0;
        AuraGlow.Visibility = config.CustomTheme.EnableAuraGlow ? Visibility.Visible : Visibility.Collapsed;
    }

    // =========================================================================
    // EXPAND & COLLAPSE (CUBIC SMOOTH FLUID MORPHING)
    // =========================================================================
    private void ExpandIsland()
    {
        if (_isExpanded) return;
        _isExpanded = true;

        CompactContainer.Visibility = Visibility.Collapsed;
        ExpandedContainer.Visibility = Visibility.Visible;
        ExpandedContainer.Opacity = 0;

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
        {
            BeginTime = TimeSpan.FromMilliseconds(70),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        ExpandedContainer.BeginAnimation(OpacityProperty, fadeIn);

        AnimateMorph(targetPillW: 430, targetPillH: 186, targetRadius: 36);
    }

    private void CollapseIsland()
    {
        if (!_isExpanded) return;
        _isExpanded = false;

        ExpandedContainer.Visibility = Visibility.Collapsed;
        CompactContainer.Visibility = Visibility.Visible;
        CompactContainer.Opacity = 0;

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
        {
            BeginTime = TimeSpan.FromMilliseconds(40),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        CompactContainer.BeginAnimation(OpacityProperty, fadeIn);

        AnimateMorph(targetPillW: 216, targetPillH: 38, targetRadius: 19);
    }

    private void AnimateMorph(double targetPillW, double targetPillH, double targetRadius)
    {
        var duration = TimeSpan.FromMilliseconds(320);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var wAnim = new DoubleAnimation(targetPillW, duration) { EasingFunction = ease };
        var hAnim = new DoubleAnimation(targetPillH, duration) { EasingFunction = ease };

        IslandPill.BeginAnimation(WidthProperty, wAnim);
        IslandPill.BeginAnimation(HeightProperty, hAnim);

        AuraGlow.BeginAnimation(WidthProperty, wAnim);
        AuraGlow.BeginAnimation(HeightProperty, hAnim);

        IslandPill.CornerRadius = new CornerRadius(targetRadius);
        AuraGlow.CornerRadius = new CornerRadius(targetRadius);
    }

    // =========================================================================
    // MEDIA PLAYER MODULE
    // =========================================================================
    private void OnTrackChanged(MediaTrackInfo track)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateMediaUi(track);
            // Quick event preview if compact
            if (!_isExpanded && track.IsPlaying)
            {
                // Smooth aura blink if configured
            }
        });
    }

    private void UpdateMediaUi(MediaTrackInfo track)
    {
        bool hasTitle = !string.IsNullOrWhiteSpace(track.Title);

        CompactTitle.Text = hasTitle ? track.Title : "Музыка не играет";
        CompactArtist.Text = hasTitle ? (string.IsNullOrEmpty(track.Artist) ? "—" : track.Artist) : "Включите трек";

        ExpandedTitle.Text = hasTitle ? track.Title : "Ничего не воспроизводится";
        ExpandedArtist.Text = hasTitle ? (string.IsNullOrEmpty(track.Artist) ? "—" : track.Artist) : "Запустите Spotify, Яндекс или браузер";

        if (track.AlbumArt != null)
        {
            CompactArt.Source = track.AlbumArt;
            ExpandedArt.Source = track.AlbumArt;
            CompactArt.Visibility = Visibility.Visible;
            ExpandedArt.Visibility = Visibility.Visible;
        }
        else
        {
            CompactArt.Source = null;
            ExpandedArt.Source = null;
            CompactArt.Visibility = Visibility.Collapsed;
            ExpandedArt.Visibility = Visibility.Collapsed;
        }

        _currentPosition = track.Position;
        _currentDuration = track.Duration;

        UpdateProgressUi();
        OnPlaybackStateChanged(track.IsPlaying);
    }

    private void OnPlaybackStateChanged(bool isPlaying)
    {
        Dispatcher.Invoke(() =>
        {
            _isPlaying = isPlaying;
            var geom = (StreamGeometry)FindResource(isPlaying ? "PauseGeometry" : "PlayGeometry");
            IconPlayPause.Data = geom;
            // Visual optical centering offset: triangle play needs +2px right offset
            IconPlayPause.Margin = isPlaying ? new Thickness(0) : new Thickness(2, 0, 0, 0);

            if (isPlaying)
            {
                _progressTimer.Start();
                _visualizerTimer.Start();
                CompactVisualizer.Opacity = 1.0;
                ExpVisualizer.Opacity = 1.0;
            }
            else
            {
                _progressTimer.Stop();
                _visualizerTimer.Stop();
                CompactVisualizer.Opacity = 0.35;
                ExpVisualizer.Opacity = 0.35;

                // Resting visualizer bar heights
                Bar1.Height = 5;
                Bar2.Height = 8;
                Bar3.Height = 5;
                Bar4.Height = 7;

                ExpBar1.Height = 8;
                ExpBar2.Height = 12;
                ExpBar3.Height = 6;
                ExpBar4.Height = 10;
                ExpBar5.Height = 7;
            }
        });
    }

    private void VisualizerTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isPlaying) return;

        // Compact 4 bars
        Bar1.Height = _rand.Next(4, 12);
        Bar2.Height = _rand.Next(8, 20);
        Bar3.Height = _rand.Next(4, 15);
        Bar4.Height = _rand.Next(6, 17);

        // Expanded 5 bars
        ExpBar1.Height = _rand.Next(6, 18);
        ExpBar2.Height = _rand.Next(12, 26);
        ExpBar3.Height = _rand.Next(6, 20);
        ExpBar4.Height = _rand.Next(10, 24);
        ExpBar5.Height = _rand.Next(7, 19);
    }

    private void ProgressTimer_Tick(object? sender, EventArgs e)
    {
        if (_isPlaying && !_isScrubbing)
        {
            _currentPosition += TimeSpan.FromSeconds(1);
            if (_currentDuration > TimeSpan.Zero && _currentPosition > _currentDuration)
            {
                _currentPosition = _currentDuration;
            }
            UpdateProgressUi();
        }
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
        if (_isScrubbing)
        {
            _isScrubbing = false;
            if (_currentDuration > TimeSpan.Zero)
            {
                double sec = (_currentDuration.TotalSeconds * PlaybackProgress.Value) / 100.0;
                _currentPosition = TimeSpan.FromSeconds(sec);
                UpdateProgressUi();
                _ = MediaService.Instance.SetPositionAsync(_currentPosition);
            }
        }
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
        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
        return $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    private async void BtnPlayPause_Click(object sender, RoutedEventArgs e)
    {
        await MediaService.Instance.TogglePlayPauseAsync();
    }

    private async void BtnPrev_Click(object sender, RoutedEventArgs e)
    {
        await MediaService.Instance.PreviousAsync();
    }

    private async void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        await MediaService.Instance.NextAsync();
    }

    // =========================================================================
    // MOUSE HOVER, WHEEL & CONTEXT MENU
    // =========================================================================
    private void Pill_MouseEnter(object sender, MouseEventArgs e)
    {
        _autoCollapseTimer.Stop();
        if (!_isExpanded && ConfigService.Instance.Config.ExpandOnHover)
        {
            _hoverTimer.Start();
        }
    }

    private void Pill_MouseLeave(object sender, MouseEventArgs e)
    {
        _hoverTimer.Stop();
        if (_isExpanded)
        {
            _autoCollapseTimer.Stop();
            _autoCollapseTimer.Interval = TimeSpan.FromSeconds(ConfigService.Instance.Config.AutoCollapseSeconds > 0 ? ConfigService.Instance.Config.AutoCollapseSeconds : 2);
            _autoCollapseTimer.Start();
        }
    }

    private void Pill_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
            else
            {
                // Smooth collapse if user clicks directly on island background
                if (e.OriginalSource is DependencyObject dep && !IsInteractiveChild(dep))
                {
                    CollapseIsland();
                }
            }
        }
    }

    private static bool IsInteractiveChild(DependencyObject dep)
    {
        DependencyObject? cur = dep;
        while (cur != null)
        {
            if (cur is Button || cur is Slider || cur is RepeatButton || cur is Thumb)
                return true;
            cur = VisualTreeHelper.GetParent(cur);
        }
        return false;
    }

    private void Pill_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Smoothly adjust master volume with mouse wheel over island
        float step = e.Delta > 0 ? 0.02f : -0.02f;
        float newVol = Math.Clamp(VolumeService.Instance.CurrentVolume + step, 0f, 1f);
        VolumeService.Instance.SetVolume(newVol);
        e.Handled = true;
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
