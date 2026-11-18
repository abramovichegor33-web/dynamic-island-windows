using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        try
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
        catch (Exception ex)
        {
            try { File.AppendAllText("crash.log", $"[{DateTime.Now}] Loaded Exception: {ex}\n"); } catch { }
        }
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
                Content = new TextBlock
                {
                    Text = widget.Icon,
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                ToolTip = widget.DisplayName,
                Height = 26,
                Padding = new Thickness(8, 0, 8, 0),
                Margin = new Thickness(1, 0, 1, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0x8E, 0x93)),
                Cursor = Cursors.Hand,
                Tag = widget
            };

            btn.Resources.Add(typeof(Border), new Style(typeof(Border))
            {
                Setters = { new Setter(Border.CornerRadiusProperty, new CornerRadius(13)) }
            });

            btn.Click += (s, _) =>
            {
                if (s is Button b && b.Tag is IIslandWidget w) SelectWidget(w);
            };

            TabsPanel.Children.Add(btn);
        }

        UpdateTabSelection();
    }

    private void UpdateTabSelection()
    {
        var textPrimaryBrush = Brushes.White;
        var textSecondaryBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0x8E, 0x93));
        var activePillBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3C));

        foreach (var child in TabsPanel.Children)
        {
            if (child is Button btn && btn.Tag is IIslandWidget widget)
            {
                bool isSelected = widget == _activeWidget;
                btn.Background = isSelected ? activePillBrush : Brushes.Transparent;
                btn.Foreground = isSelected ? textPrimaryBrush : textSecondaryBrush;
            }
        }
    }

    private void SelectWidget(IIslandWidget widget)
    {
        if (_activeWidget != null)
        {
            _activeWidget.SetExpanded(false);
        }

        _activeWidget = widget;
        WidgetHost.Content = (FrameworkElement)widget;
        _activeWidget.SetExpanded(_isExpanded);

        UpdateTabSelection();
    }

    private void ApplyConfig()
    {
        var cfg = ConfigService.Instance.Config;
        _hoverTimer.Interval = TimeSpan.FromMilliseconds(cfg.HoverDelayMs);
        PillShadow.Opacity = cfg.ShowShadow ? 0.5 : 0;

        if (!_isExpanded)
        {
            Width = cfg.CompactWidth * (cfg.ScalePercent / 100.0);
            Height = cfg.CompactHeight * (cfg.ScalePercent / 100.0);
            IslandPill.CornerRadius = new CornerRadius(19);
            AuraGlow.CornerRadius = new CornerRadius(19);
        }

        RebuildTabs();
        ApplyBlurStyle(ThemeService.Instance.CurrentTheme);
        Recenter();
    }

    private void OnThemeChanged(IslandTheme theme)
    {
        ApplyBlurStyle(theme);
        UpdateTabSelection();
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
        IslandPill.CornerRadius = new CornerRadius(32);
        AuraGlow.CornerRadius = new CornerRadius(32);

        _activeWidget?.SetExpanded(true);
        UpdateTabSelection();

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
        IslandPill.CornerRadius = new CornerRadius(19);
        AuraGlow.CornerRadius = new CornerRadius(19);

        _activeWidget?.SetExpanded(false);

        AnimateMorph(targetW, targetH);
    }

    private void AnimateMorph(double targetWidth, double targetHeight)
    {
        double screenW = SystemParameters.PrimaryScreenWidth;
        double targetLeft = (screenW - targetWidth) / 2;

        var duration = TimeSpan.FromMilliseconds(260);
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
            _autoCollapseTimer.Interval = TimeSpan.FromMilliseconds(500);
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
