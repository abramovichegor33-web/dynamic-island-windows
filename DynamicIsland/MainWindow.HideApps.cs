using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DynamicIsland.Services;

namespace DynamicIsland;

/// <summary>
/// Programs in front of which the island hides (Settings > Behavior), like over a full-screen game. An entry is a
/// process name ("Zoom"), the full path of a program, or a folder: everything started from inside it (a game
/// that is in no list, a portable program).
/// </summary>
public partial class MainWindow
{
    private uint _hiddenPid;
    private string _hiddenPidName = "", _hiddenPidPath = "";

    private static bool IsPathEntry(string entry) => entry.Contains('\\');

    private bool IsHiddenApp(IntPtr fg)
    {
        if (_settings.HideInApps.Count == 0 || fg == IntPtr.Zero || fg == _hwnd) return false;
        Native.GetWindowThreadProcessId(fg, out uint pid);
        if (pid == 0 || pid == (uint)Environment.ProcessId) return false;
        if (pid != _hiddenPid)
        {
            _hiddenPid = pid;
            try { using var p = Process.GetProcessById((int)pid); _hiddenPidName = p.ProcessName; }
            catch { _hiddenPidName = ""; }
            _hiddenPidPath = _settings.HideInApps.Any(IsPathEntry) ? Native.ProcessPath(pid) : "";
        }
        return _settings.HideInApps.Any(a => !IsPathEntry(a)
            ? a.Equals(_hiddenPidName, StringComparison.OrdinalIgnoreCase)
            : _hiddenPidPath.Length > 0 && (_hiddenPidPath.Equals(a, StringComparison.OrdinalIgnoreCase) ||
                                            _hiddenPidPath.StartsWith(a.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>Running programs with a window, for the picker.</summary>
    private void FillHideAppPicker()
    {
        HideAppPicker.Items.Clear();
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (p.Id == Environment.ProcessId || p.MainWindowHandle == IntPtr.Zero || string.IsNullOrWhiteSpace(p.MainWindowTitle)) continue;
                if (p.ProcessName is "explorer" or "ApplicationFrameHost" or "TextInputHost") continue;
                names.Add(p.ProcessName);
            }
            catch { }
            finally { p.Dispose(); }
        }
        foreach (var n in names) HideAppPicker.Items.Add(n);
        BuildHideAppChips();
    }

    private void BuildHideAppChips()
    {
        HideAppChips.Children.Clear();
        foreach (var name in _settings.HideInApps)
        {
            bool path = IsPathEntry(name), folder = path && !name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
            var b = new Button { Style = (Style)FindResource("GhostPill"), Padding = new Thickness(12, 5, 10, 5), Margin = new Thickness(0, 0, 6, 6),
                                 ToolTip = path ? name + "\n" + L.T("Убрать") : L.T("Убрать") };
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            if (path)
                row.Children.Add(new TextBlock { Text = folder ? "\uE8B7" : "\uE756", FontFamily = (System.Windows.Media.FontFamily)FindResource("IconFont"),
                                                 FontSize = 12, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(new TextBlock { Text = path ? Path.GetFileName(name.TrimEnd('\\')) : name, MaxWidth = 220,
                                             TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(new TextBlock { Text = "  \u2715", Opacity = 0.6, VerticalAlignment = VerticalAlignment.Center });
            b.Content = row;
            var n = name;
            b.Click += (_, _) => { _settings.HideInApps.Remove(n); _settings.Save(); BuildHideAppChips(); Remeasure(); };
            HideAppChips.Children.Add(b);
        }
        HideAppsEmpty.Visibility = _settings.HideInApps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void HideAppAdd_Click(object sender, RoutedEventArgs e)
    {
        var name = (HideAppPicker.SelectedItem as string ?? HideAppPicker.Text ?? "").Trim().Trim('"');
        if (!IsPathEntry(name) && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name = name[..^4];
        AddHideApp(name);
        HideAppPicker.Text = "";
    }

    /// <summary>Any program on disk: a game that is not running now, one that is in no list.</summary>
    private void HideAppProgram_Click(object sender, RoutedEventArgs e)
    {
        if (PickPath(folder: false, L.T("Перед какой программой прятать остров"), L.T("Программы") + "|*.exe|" + L.T("Все файлы") + "|*.*") is { } path)
            AddHideApp(path);
    }

    /// <summary>A whole folder: every program started from inside it.</summary>
    private void HideAppFolder_Click(object sender, RoutedEventArgs e)
    {
        if (PickPath(folder: true, L.T("Перед программами из какой папки прятать остров")) is { } path)
            AddHideApp(path);
    }

    /// <summary>
    /// A file or folder from the Windows picker (a shortcut gives the file it points to). The island stays open
    /// while the picker is up: it would fold away the moment the cursor left it.
    /// </summary>
    private string? PickPath(bool folder, string title, string filter = "")
    {
        bool was = _pinnedOpen;
        _pinnedOpen = true;
        try
        {
            if (folder)
            {
                var d = new Microsoft.Win32.OpenFolderDialog { Title = title };
                return d.ShowDialog(this) == true ? d.FolderName : null;
            }
            var f = new Microsoft.Win32.OpenFileDialog { Title = title, Filter = filter, DereferenceLinks = true };
            return f.ShowDialog(this) == true ? f.FileName : null;
        }
        catch (Exception ex)
        {
            Log.Write("pick a path: " + ex.Message);
            return null;
        }
        finally { _pinnedOpen = was; }
    }

    private void AddHideApp(string name)
    {
        if (name.Length == 0 || _settings.HideInApps.Contains(name, StringComparer.OrdinalIgnoreCase)) return;
        _settings.HideInApps.Add(name);
        _settings.Save();
        BuildHideAppChips();
        Remeasure();
    }
}
