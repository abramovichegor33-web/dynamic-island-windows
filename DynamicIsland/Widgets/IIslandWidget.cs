using System.Windows;

namespace DynamicIsland.Widgets;

public interface IIslandWidget
{
    string WidgetId { get; }
    string DisplayName { get; }
    string Icon { get; }
    bool IsEnabled { get; }

    void SetExpanded(bool isExpanded);
}
