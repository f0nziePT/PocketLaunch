using System.Threading;
using System.Windows;
using H.NotifyIcon;
using Application = System.Windows.Application;

namespace PocketLaunch;

public partial class App : Application
{
    private const string MutexName = "PocketLaunch-SingleInstance-Mutex";
    private Mutex? _mutex;
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            // Another instance is already running.
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.TrayLeftMouseUp += (_, _) => ToggleMainWindow();
        _trayIcon.ForceCreate();

        _mainWindow = new MainWindow();
        _mainWindow.Hide();
    }

    private void ToggleMainWindow()
    {
        if (_mainWindow is null) return;

        if (_mainWindow.IsVisible)
        {
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.ShowNearTray();
        }
    }

    private void TrayOpen_Click(object sender, RoutedEventArgs e)
    {
        _mainWindow?.ShowNearTray();
    }

    private void TrayClose_Click(object sender, RoutedEventArgs e)
    {
        _trayIcon?.Dispose();
        _mutex?.ReleaseMutex();
        Shutdown();
    }
}
