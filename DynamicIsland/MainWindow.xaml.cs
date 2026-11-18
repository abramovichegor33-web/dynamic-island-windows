using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DynamicIsland.Models;
using DynamicIsland.Services;
using DynamicIsland.Views;
using DynamicIsland.Widgets;

namespace DynamicIsland;

public partial class MainWindow : Window
{
    private readonly List<IIslandWidget> _widgets = [];
    private IIslandWidget? _activeWidget;
    private bool _isExpanded;

    private readonly DispatcherTimer _hoverTimer;
    private readonly DispatcherTimer _autoCollapseTimer;

    private MusicWidget _musicWidget = null!;
    private VolumeWidget _volumeWidget = null!;
    private ClockWidget _clockWidget = null!;
    private WeatherWidget _weatherWidget = null!;
    private SystemStatsWidget _statsWidget = null!;
    private FileShelfWidget _shelfWidget = null!;
    private NotificationWidget _notificationWidget = null!;

    public MainWindow()
    {
        InitializeComponent();

        _hoverTimer = new DispatcherTimer();
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

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        InitWidgets();
        ApplyConfig();
        Recenter();

        ConfigService.Instance.ConfigChanged += _ => Dispatcher.Invoke(ApplyConfig);
        ThemeService.Instance.ThemeChanged += OnThemeChanged;

        // Reactive events
        VolumeService.Instance.VolumeChanged += (vol, muted) =>
        {
            Dispatcher.Invoke(() => TriggerEventExpansion(_volumeWidget, 2.5));
        };

        MediaService.Instance.TrackChanged += track =>
        {
            if (track.IsPlaying && !string.IsNullOrWhiteSpace(track.Title))
            {
                Dispatcher.Invoke(() => TriggerEventExpansion(_musicWidget, 3.0));
            }
        };

        NotificationService.Instance.NotificationReceived += _ =>
        {
            Dispatcher.Invoke(() => TriggerEventExpansion(_notificationWidget, 4.0));
        };
    }

    private void InitWidgets()
    {
        _musicWidget = new MusicWidget();
        _volumeWidget = new VolumeWidget();
        _clockWidget = new ClockWidget();
        _weatherWidget = new WeatherWidget();
        _statsWidget = new SystemStatsWidget();
        _shelfWidget = new FileShelfWidget();
        _notificationWidget = new NotificationWidget();

        _widgets.Clear();
        _widgets.Add(_musicWidget);
        _widgets.Add(_volumeWidget);
        _widgets.Add(_clockWidget);
        _widgets.Add(_weatherWidget);
        _widgets.Add(_statsWidget);
        _widgets.Add(_shelfWidget);
        _widgets.Add(_notificationWidget);

        RebuildTabs();
        SelectWidget(_musicWidget);
    }

    private void RebuildTabs()
    {
        TabsPanel.Children.Clear();
        foreach (var widget in _widgets.Where(w => w.IsEnabled))
        {
            var btn = new Button
            {
                Content = widget.Icon,
                ToolTip = widget.DisplayName,
                Width = 26,
                Height = 24,
                FontSize = 12,
                Margin = new Thickness(2, 0, 2, 0),
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush"),
                Cursor = Cursors.Hand,
                Tag = widget
            };
            btn.Click += (s, _) =>
            {
                if (s is Button b && b.Tag is IIslandWidget w) SelectWidget(w);
            };
            TabsPanel.Children.Add(btn);
        }
    }

    private void SelectWidget(IIslandWidget widget)
    {
        if (_activeWidget != null) _activeWidget.OnDeactivated();
        _activeWidget = widget;
        WidgetHost.Content = _isExpanded ? widget.ExpandedView : widget.CompactView;
        if (_isExpanded) _activeWidget.OnActivated();
    }

    private void ApplyConfig()
    {
        var cfg = ConfigService.Instance.Config;
        _hoverTimer.Interval = TimeSpan.FromMilliseconds(cfg.HoverDelayMs);
        PillShadow.Opacity = cfg.ShowShadow ? 0.45 : 0;

        RebuildTabs();
        ApplyBlurStyle(ThemeService.Instance.CurrentTheme);
        Recenter();
    }

    private void OnThemeChanged(IslandTheme theme)
    {
        ApplyBlurStyle(theme);
    }

    private void ApplyBlurStyle(IslandTheme theme)
    {
        if (theme.EnableAcrylicBlur)
        {
            WindowComposition.EnableAcrylic(this, 0x88121418);
        }
        else
        {
            WindowComposition.DisableBlur(this);
        }
        AuraGlow.Visibility = theme.EnableAuraGlow ? Visibility.Visible : Visibility.Collapsed;
    }

    public void Recenter()
    {
        var cfg = ConfigService.Instance.Config;
        double screenW = SystemParameters.PrimaryScreenWidth;
        Top = cfg.TopOffset;
        Left = (screenW - Width) / 2;
    }

    private void TriggerEventExpansion(IIslandWidget widget, double durationSeconds)
    {
        SelectWidget(widget);
        ExpandIsland();
        _autoCollapseTimer.Stop();
        _autoCollapseTimer.Interval = TimeSpan.FromSeconds(durationSeconds);
        _autoCollapseTimer.Start();
    }

    private void ExpandIsland()
    {
        if (_isExpanded) return;
        _isExpanded = true;

        var cfg = ConfigService.Instance.Config;
        double targetW = cfg.ExpandedWidth * (cfg.ScalePercent / 100.0);
        double targetH = cfg.ExpandedHeight * (cfg.ScalePercent / 100.0);

        HeaderTabs.Visibility = Visibility.Visible;
        if (_activeWidget != null)
        {
            WidgetHost.Content = _activeWidget.ExpandedView;
            _activeWidget.OnActivated();
        }

        AnimateMorph(targetW, targetH);
    }

    private void CollapseIsland()
    {
        if (!_isExpanded) return;
        _isExpanded = false;

        var cfg = ConfigService.Instance.Config;
        double targetW = cfg.CompactWidth * (cfg.ScalePercent / 100.0);
        double targetH = cfg.CompactHeight * (cfg.ScalePercent / 100.0);

        HeaderTabs.Visibility = Visibility.Collapsed;
        if (_activeWidget != null)
        {
            _activeWidget.OnDeactivated();
            WidgetHost.Content = _activeWidget.CompactView;
        }

        AnimateMorph(targetW, targetH);
    }

    private void AnimateMorph(double targetWidth, double targetHeight)
    {
        double screenW = SystemParameters.PrimaryScreenWidth;
        double targetLeft = (screenW - targetWidth) / 2;

        var duration = TimeSpan.FromMilliseconds(220);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var wAnim = new DoubleAnimation(targetWidth, duration) { EasingFunction = ease };
        var hAnim = new DoubleAnimation(targetHeight, duration) { EasingFunction = ease };
        var lAnim = new DoubleAnimation(targetLeft, duration) { EasingFunction = ease };

        BeginAnimation(WidthProperty, wAnim);
        BeginAnimation(HeightProperty, hAnim);
        BeginAnimation(LeftProperty, lAnim);
    }

    // ---------------- Mouse Events ----------------

    private void Window_MouseEnter(object sender, MouseEventArgs e)
    {
        var cfg = ConfigService.Instance.Config;
        _autoCollapseTimer.Stop();
        if (cfg.ExpandOnHover && !_isExpanded)
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
            _autoCollapseTimer.Interval = TimeSpan.FromMilliseconds(400);
            _autoCollapseTimer.Start();
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            if (!_isExpanded) ExpandIsland();
        }
    }

    // ---------------- Drag & Drop Files to Shelf ----------------

    private void Window_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            ExpandIsland();
            SelectWidget(_shelfWidget);
        }
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                _shelfWidget.AddFiles(files);
                SelectWidget(_shelfWidget);
            }
        }
    }

    // ---------------- Context Menu ----------------

    private void MenuSettings_Click(object sender, RoutedEventArgs e)
    {
        var win = new SettingsWindow { Owner = this };
        win.ShowDialog();
    }

    private void MenuNextTheme_Click(object sender, RoutedEventArgs e)
    {
        var presets = IslandTheme.GetPresets();
        var cfg = ConfigService.Instance.Config;
        int idx = presets.FindIndex(p => p.Id == cfg.SelectedThemeId);
        int nextIdx = (idx + 1) % presets.Count;
        cfg.SelectedThemeId = presets[nextIdx].Id;
        ConfigService.Instance.Save(cfg);
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
