using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DynamicIsland.Services;

namespace DynamicIsland.Widgets;

public class ShelfItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
}

public partial class FileShelfWidget : UserControl, IIslandWidget
{
    public string WidgetId => "shelf";
    public string DisplayName => "Полка файлов";
    public string Icon => "📁";
    public bool IsEnabled => ConfigService.Instance.Config.EnableShelfWidget;

    public FrameworkElement CompactView => CompactContainer;
    public FrameworkElement ExpandedView => ExpandedContainer;

    private readonly ObservableCollection<ShelfItem> _items = [];

    public FileShelfWidget()
    {
        InitializeComponent();
        ListFiles.ItemsSource = _items;

        var saved = ConfigService.Instance.Config.PinnedFiles;
        foreach (var p in saved.Where(File.Exists))
        {
            _items.Add(new ShelfItem { Name = Path.GetFileName(p), Path = p });
        }
        UpdateState();
    }

    public void AddFiles(string[] paths)
    {
        foreach (var p in paths)
        {
            if (!_items.Any(i => i.Path.Equals(p, StringComparison.OrdinalIgnoreCase)))
            {
                _items.Add(new ShelfItem { Name = Path.GetFileName(p), Path = p });
            }
        }
        SyncConfig();
        UpdateState();
    }

    private void UpdateState()
    {
        int count = _items.Count;
        TextCompactCount.Text = count == 0 ? "Полка пуста" : $"{count} файл{(count == 1 ? "" : "а")}";
        TextPlaceholder.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ListFiles.Visibility = count == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SyncConfig()
    {
        var cfg = ConfigService.Instance.Config;
        cfg.PinnedFiles = _items.Select(i => i.Path).ToList();
        ConfigService.Instance.Save();
    }

    private void BtnClearAll_Click(object sender, RoutedEventArgs e)
    {
        _items.Clear();
        SyncConfig();
        UpdateState();
    }

    private void BtnCopyFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path && File.Exists(path))
        {
            var collection = new System.Collections.Specialized.StringCollection { path };
            Clipboard.SetFileDropList(collection);
        }
    }

    private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path && File.Exists(path))
        {
            Process.Start("explorer.exe", $"/select,\"{path}\"");
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
}
