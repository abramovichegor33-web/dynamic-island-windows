using System.Threading;
using System.Windows;
using DynamicIsland.Services;

namespace DynamicIsland;

public partial class App : Application
{
    private static Mutex? _singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string appGuid = "DynamicIsland_Win10_Singleton_Guid";
        _singleInstanceMutex = new Mutex(true, appGuid, out bool isNewInstance);

        if (!isNewInstance)
        {
            MessageBox.Show("Приложение Dynamic Island уже запущено!", "Dynamic Island", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Initialize Themes and Services
        _ = ThemeService.Instance;

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceMutex?.ReleaseMutex();
        base.OnExit(e);
    }
}
