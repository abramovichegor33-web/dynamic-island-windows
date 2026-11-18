using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace DynamicIsland.Services;

public class IslandNotification
{
    public string AppName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class NotificationService
{
    private static readonly Lazy<NotificationService> _instance = new(() => new NotificationService());
    public static NotificationService Instance => _instance.Value;

    private UserNotificationListener? _listener;

    public event Action<IslandNotification>? NotificationReceived;

    private NotificationService()
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            if (UserNotificationListener.Current != null)
            {
                _listener = UserNotificationListener.Current;
                var access = await _listener.RequestAccessAsync();
                if (access == UserNotificationListenerAccessStatus.Allowed)
                {
                    _listener.NotificationChanged += Listener_NotificationChanged;
                }
            }
        }
        catch
        {
            // Windows Notification listener might be disabled in group policy
        }
    }

    private void Listener_NotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
    {
        if (args.ChangeKind != UserNotificationChangedKind.Added) return;

        try
        {
            var notif = sender.GetNotification(args.UserNotificationId);
            if (notif?.Notification?.Visual == null) return;

            string appName = notif.AppInfo?.DisplayInfo?.DisplayName ?? "Приложение";
            string title = "";
            string body = "";

            var binding = notif.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
            if (binding != null)
            {
                var textElements = binding.GetTextElements().ToList();
                if (textElements.Count > 0) title = textElements[0].Text;
                if (textElements.Count > 1) body = textElements[1].Text;
            }

            if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(body))
            {
                NotificationReceived?.Invoke(new IslandNotification
                {
                    AppName = appName,
                    Title = title,
                    Body = body,
                    Timestamp = DateTime.Now
                });
            }
        }
        catch { }
    }
}
