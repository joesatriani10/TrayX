using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace TrayX.Tray;

public class TrayActions
{
    public async void CleanRam(TaskbarIcon trayIcon)
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
    
    public void FlushDns(TaskbarIcon trayIcon)
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
    
    public void Tray_ClearTemp(TaskbarIcon trayIcon)
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
    
}