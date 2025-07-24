using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
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

        private void Tray_ClearTemp(object sender, RoutedEventArgs e)
        {
            try
            {
                var tempPath = Environment.GetEnvironmentVariable("TEMP");
                if (string.IsNullOrEmpty(tempPath)) return;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C del /q/f/s \"{tempPath}\\*\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });

                trayIcon.ShowBalloonTip("TrayX", "Temporary files deleted (or scheduled)", BalloonIcon.Info);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex);
                MessageBox.Show("An error occurred while clearing temporary files. Please check the log for details.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Tray_FlushDns(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });

                trayIcon.ShowBalloonTip("TrayX", "DNS cache flushed", BalloonIcon.Info);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex);
                MessageBox.Show("An error occurred while flushing the DNS cache. Please check the log for details.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Tray_ListStartupPrograms(object sender, RoutedEventArgs e)
        {
           var startupPrograms = new StartupPrograms();
           startupPrograms.Tray_ListStartupPrograms();
        }

        private void Tray_EmptyRecycleBin(object sender, RoutedEventArgs e)
        {
            try
            {
                // First query the Recycle Bin to avoid triggering an error when it is already empty
                var info = new NativeMethods.SHQUERYRBINFO { cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.SHQUERYRBINFO)) };
                var queryResult = NativeMethods.SHQueryRecycleBin(null, ref info);

                if (queryResult == 0 && info.i64NumItems == 0)
                {
                    trayIcon.ShowBalloonTip("TrayX", "Recycle Bin already empty", BalloonIcon.Info);
                    return;
                }

                var flags = NativeMethods.SherbNoconfirmation | NativeMethods.SherbNoprogressui | NativeMethods.SherbNosound;
                var result = NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, flags);

                if (result == 0)
                {
                    trayIcon.ShowBalloonTip("TrayX", "Recycle Bin emptied", BalloonIcon.Info);
                }
                else if (result == NativeMethods.HresultSFalse)
                {
                    trayIcon.ShowBalloonTip("TrayX", "Recycle Bin already empty", BalloonIcon.Info);
                }
                else
                {
                    MessageBox.Show($"Failed to empty Recycle Bin. Error code: {result}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex);
                MessageBox.Show("An error occurred while emptying the Recycle Bin. Please check the log for details.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Tray_Exit(object sender, RoutedEventArgs e)
        {
            trayIcon.Dispose();
            Shutdown();
        }

    }

    internal static class NativeMethods
    {
        [DllImport("shell32.dll", SetLastError = true)]
        internal static extern uint SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        internal struct SHQUERYRBINFO
        {
            public uint cbSize;
            public ulong i64Size;
            public ulong i64NumItems;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        internal static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        internal const uint SherbNoconfirmation = 0x00000001;
        internal const uint SherbNoprogressui   = 0x00000002;
        internal const uint SherbNosound        = 0x00000004;
        internal const uint HresultSFalse       = 0x00000001;
    }
}

