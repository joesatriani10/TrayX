using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows;
using WUApiLib; // Requires COM reference to Microsoft Update Session (wuapi.dll)

public static class WindowsUpdateService
{
    // Checks if the Windows Update service (wuauserv) is running
    public static bool IsUpdateServiceRunning()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service WHERE Name = 'wuauserv'");
            var service = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
            return service != null && service["State"]?.ToString() == "Running";
        }
        catch (Exception ex)
        {
            ErrorLogger.LogException(ex);
            return false;
        }
    }

    // Gets the most recent installed Windows Update patch using QuickFixEngineering
    public static string? GetLastInstalledUpdate()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_QuickFixEngineering");
            var updates = searcher.Get().Cast<ManagementObject>()
                                  .OrderByDescending(mo => Convert.ToDateTime(mo["InstalledOn"]))
                                  .ToList();

            if (updates.Count == 0)
                return null;

            var last = updates.First();
            return $"{last["HotFixID"]} installed on {last["InstalledOn"]}";
        }
        catch (Exception ex)
        {
            ErrorLogger.LogException(ex);
            return null;
        }
    }

    // Returns the number of pending software updates
    public static int GetPendingUpdatesCount()
    {
        try
        {
            var session = new UpdateSession();
            var searcher = session.CreateUpdateSearcher();
            var result = searcher.Search("IsInstalled=0 AND Type='Software'");
            return result.Updates.Count;
        }
        catch (COMException ex)
        {
            ErrorLogger.LogException(ex);
            return -1;
        }
    }

    // Downloads and installs all pending updates
    public static void InstallPendingUpdates()
    {
        try
        {
            var session = new UpdateSession();
            var searcher = session.CreateUpdateSearcher();
            var searchResult = searcher.Search("IsInstalled=0 AND IsHidden=0");

            if (searchResult.Updates.Count == 0)
            {
                MessageBox.Show("No pending updates found.", "Windows Update", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var updatesToDownload = new UpdateCollection();
            foreach (IUpdate update in searchResult.Updates)
            {
                if (!update.IsDownloaded)
                    updatesToDownload.Add(update);
            }

            if (updatesToDownload.Count > 0)
            {
                var downloader = session.CreateUpdateDownloader();
                downloader.Updates = updatesToDownload;
                downloader.Download();
            }

            var updatesToInstall = new UpdateCollection();
            foreach (IUpdate update in searchResult.Updates)
            {
                if (update.IsDownloaded)
                    updatesToInstall.Add(update);
            }

            if (updatesToInstall.Count == 0)
            {
                MessageBox.Show("All updates already downloaded. Nothing to install.", "Windows Update", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var installer = session.CreateUpdateInstaller();
            installer.Updates = updatesToInstall;
            var result = installer.Install();

            MessageBox.Show(
                $"Installation completed.\nReboot required: {result.RebootRequired}\nResult code: {result.ResultCode}",
                "Windows Update", MessageBoxButton.OK, MessageBoxImage.Information
            );
        }
        catch (Exception ex)
        {
            ErrorLogger.LogException(ex);
            MessageBox.Show("An error occurred during update installation. Check the logs for details.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Shows a MessageBox with current update status (optional UI for quick testing)
    public static void ShowUpdateStatus()
    {
        try
        {
            bool isRunning = IsUpdateServiceRunning();
            int pending = GetPendingUpdatesCount();
            string? lastInstalled = GetLastInstalledUpdate();

            var msg = $"Windows Update Service: {(isRunning ? "Running" : "Stopped")}\n" +
                      $"Pending Updates: {(pending >= 0 ? pending.ToString() : "Unknown")}\n" +
                      $"Last Installed Update: {lastInstalled ?? "None"}";

            MessageBox.Show(msg, "Windows Update Status", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogger.LogException(ex);
        }
    }
}
