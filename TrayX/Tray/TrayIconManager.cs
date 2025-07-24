using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;


namespace TrayX.Tray;

public class TrayIconManager
{private readonly TaskbarIcon trayIcon;
    private readonly MainWindow mainWindow;

    public TrayIconManager()
    {
        trayIcon = (TaskbarIcon)Application.Current.FindResource("TrayIcon");
        mainWindow = new MainWindow();
        AttachEvents();
    }

    private void AttachEvents()
    {
        trayIcon.TrayLeftMouseDown += (s, e) =>
        {
            if (!mainWindow.IsVisible)
                mainWindow.Show();
            else
                mainWindow.Activate();
        };
    }

    public void Dispose() => trayIcon.Dispose();
}