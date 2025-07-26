using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using TrayX.Tray;
using TrayX.Utils;

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
            RegistryHelper.SetAutoStartIfEnabled(AppConfig.Load());
            
            Console.WriteLine($"Antivirus Installed: {AntivirusService.IsAntivirusInstalled()}");
            Console.WriteLine($"Antivirus Name: {AntivirusService.GetAntivirusName()}");
            Console.WriteLine($"Antivirus Updated: {AntivirusService.IsAntivirusUpdated()}");

            try
            {
                trayIcon = (TaskbarIcon)FindResource("TrayIcon");
                mainWindow = new MainWindow();
                trayIcon.TrayLeftMouseDown += (s, _) =>
                {
                    if (!mainWindow.IsVisible)
                    {
                        var screen = SystemParameters.WorkArea;
                        mainWindow.Left = screen.Left + (screen.Width - mainWindow.Width) / 2;
                        mainWindow.Top = screen.Top + (screen.Height - mainWindow.Height) / 2;
                        mainWindow.Show();
                    }
                    else
                    {
                        mainWindow.Activate();
                    }
                };
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex);
                MessageBox.Show("TrayX could not initialize properly.");
                Shutdown();
            }
            
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
                {
                    var screen = SystemParameters.WorkArea;
                    mainWindow.Left = screen.Left + (screen.Width - mainWindow.Width) / 2;
                    mainWindow.Top = screen.Top + (screen.Height - mainWindow.Height) / 2;
                    mainWindow.Show();
                }
                else
                {
                    mainWindow.Activate();
                }
            }
        }

        private async void Tray_CleanRam(object sender, RoutedEventArgs e)
        {
            var trayActions = new TrayActions();
            trayActions.CleanRam(trayIcon);
        }

        private void Tray_ClearTemp(object sender, RoutedEventArgs e)
        {
           var trayActions = new TrayActions();
           trayActions.Tray_ClearTemp(trayIcon);
        }

        private void Tray_FlushDns(object sender, RoutedEventArgs e)
        {
            var trayActions = new TrayActions();
            trayActions.FlushDns(trayIcon);
        }

        private void Tray_ListStartupPrograms(object sender, RoutedEventArgs e)
        {
           var startupPrograms = new StartupPrograms();
           startupPrograms.Tray_ListStartupPrograms();
        }

        private void Tray_EmptyRecycleBin(object sender, RoutedEventArgs e)
        {
          var recycleBinService = new RecycleBinService();
          recycleBinService.EmptyRecycleBin(trayIcon);
        }

        private void Tray_Exit(object sender, RoutedEventArgs e)
        {
            trayIcon.Dispose();
            Shutdown();
        }

    }
}

