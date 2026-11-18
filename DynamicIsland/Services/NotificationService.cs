using System.Linq;
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
        _ = Task.Run(InitializeAsync);
    }

    private async Task InitializeAsync()
    {
        try
        {
            var listener = UserNotificationListener.Current;
            if (listener != null)
            {
                var access = await listener.RequestAccessAsync();
                if (access == UserNotificationListenerAccessStatus.Allowed)
                {
                    listener.NotificationChanged += Listener_NotificationChanged;
                    _listener = listener;
                }
            }
        }
        catch
        {
            // Ignore in unpackaged desktop mode
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
