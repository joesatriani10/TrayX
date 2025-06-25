using System.Diagnostics;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;

namespace TrayX
{
    public partial class App : Application
    {
        public static AppConfig Config { get; private set; } = AppConfig.Load();
        private TaskbarIcon trayIcon;
        private MainWindow mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (Config.AutoStart)
            {
                try
                {
                    using var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    var exe = Process.GetCurrentProcess().MainModule?.FileName;
                    if (key != null && exe != null)
                        key.SetValue("TrayX", exe);
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException(ex);
                }
            }

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

        private async void Tray_CleanRam(object sender, RoutedEventArgs e)
        {
            try
            {
                await MemoryCleaner.CleanAsync();
                trayIcon.ShowBalloonTip("TrayX", "RAM cleanup completed.", BalloonIcon.Info);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex);
                MessageBox.Show("An error occurred while cleaning RAM. Please check the log for details.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Tray_Exit(object sender, RoutedEventArgs e)
        {
            trayIcon.Dispose();
            Shutdown();
        }

    }
}