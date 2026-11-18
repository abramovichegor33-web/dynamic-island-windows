using System.Windows;
using System.Windows.Controls;
using DynamicIsland.Services;

namespace DynamicIsland.Widgets;

public partial class NotificationWidget : UserControl, IIslandWidget
{
    public string WidgetId => "notification";
    public string DisplayName => "Уведомления";
    public string Icon => "🔔";
    public bool IsEnabled => ConfigService.Instance.Config.EnableNotificationWidget;

    public NotificationWidget()
    {
        InitializeComponent();

        NotificationService.Instance.NotificationReceived += notif =>
        {
            Dispatcher.Invoke(() => ShowNotification(notif));
        };
    }

    public void SetExpanded(bool isExpanded)
    {
        CompactContainer.Visibility = isExpanded ? Visibility.Collapsed : Visibility.Visible;
        ExpandedContainer.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
    }

    public void ShowNotification(IslandNotification notif)
    {
        CompactApp.Text = notif.AppName;
        CompactMessage.Text = string.IsNullOrWhiteSpace(notif.Title) ? notif.Body : notif.Title;
        CompactTime.Text = notif.Timestamp.ToString("HH:mm");

        ExpApp.Text = notif.AppName;
        ExpTime.Text = notif.Timestamp.ToString("HH:mm");
        ExpTitle.Text = notif.Title;
        ExpBody.Text = notif.Body;
    }
}
