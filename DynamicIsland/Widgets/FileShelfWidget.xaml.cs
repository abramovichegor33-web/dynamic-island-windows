using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DynamicIsland.Services;

namespace DynamicIsland.Widgets;

public class ShelfFileItem
{
    public string Path { get; set; } = "";
    public string Name => System.IO.Path.GetFileName(Path);
}

public partial class FileShelfWidget : UserControl, IIslandWidget
{
    public string WidgetId => "shelf";
    public string DisplayName => "Полка файлов";
    public string Icon => "📁";
    public bool IsEnabled => ConfigService.Instance.Config.EnableShelfWidget;

    public ObservableCollection<ShelfFileItem> Files { get; } = [];

    public FileShelfWidget()
    {
        InitializeComponent();
        ListFiles.ItemsSource = Files;
        UpdateEmptyState();
    }

    public void SetExpanded(bool isExpanded)
    {
        CompactContainer.Visibility = isExpanded ? Visibility.Collapsed : Visibility.Visible;
        ExpandedContainer.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
    }

    public void AddFiles(IEnumerable<string> filePaths)
    {
        foreach (var p in filePaths)
        {
            if (File.Exists(p) || Directory.Exists(p))
            {
                if (!Files.Any(f => f.Path.Equals(p, StringComparison.OrdinalIgnoreCase)))
                {
                    Files.Add(new ShelfFileItem { Path = p });
                }
            }
        }
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        int count = Files.Count;
        TextCompactCount.Text = count == 0 ? "Полка пуста" : $"{count} {PluralFiles(count)}";
        TextPlaceholder.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ListFiles.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string PluralFiles(int n)
    {
        if (n % 10 == 1 && n % 100 != 11) return "файл";
        if (n % 10 >= 2 && n % 10 <= 4 && (n % 100 < 10 || n % 100 >= 20)) return "файла";
        return "файлов";
    }

    private void BtnClearAll_Click(object sender, RoutedEventArgs e)
    {
        Files.Clear();
        UpdateEmptyState();
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
}
