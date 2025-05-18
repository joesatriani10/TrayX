using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace TrayX
{
    public partial class App : Application
    {
        private TaskbarIcon trayIcon;
        private MainWindow mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Try to load tray icon from XAML resource
            try
            {
                trayIcon = (TaskbarIcon)FindResource("TrayIcon");
            }
            catch (ResourceReferenceKeyNotFoundException ex)
            {
                MessageBox.Show("Failed to load tray icon: " + ex.Message);
                Shutdown(); // Exit if we can't show the icon
                return;
            }

            // Show a quick balloon tip to confirm it loaded
            // trayIcon.ShowBalloonTip("TraySys Monitor", "loaded.", BalloonIcon.Info);

            // Create the main window (but do not show yet)
            mainWindow = new MainWindow();

            // Attach click event: show/hide window on left click
            trayIcon.TrayLeftMouseDown += (s, args) =>
            {
                if (!mainWindow.IsVisible)
                    mainWindow.Show();
                else
                    mainWindow.Activate();
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            trayIcon?.Dispose();
            base.OnExit(e);
        }
        
        private void Tray_ShowWindow(object sender, RoutedEventArgs e)
        {
            if (mainWindow != null)
            {
                if (!mainWindow.IsVisible)
                    mainWindow.Show();
                else
                    mainWindow.Activate();
            }
        }

        private void Tray_Exit(object sender, RoutedEventArgs e)
        {
            trayIcon.Dispose();
            Shutdown();
        }

    }
}