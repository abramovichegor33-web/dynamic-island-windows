using System.Windows;

namespace DynamicIsland.Widgets;

public interface IIslandWidget
{
    string WidgetId { get; }
    string DisplayName { get; }
    string Icon { get; }
    bool IsEnabled { get; }

    FrameworkElement CompactView { get; }
    FrameworkElement ExpandedView { get; }

    void OnActivated();
    void OnDeactivated();
}
