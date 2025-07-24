using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace TrayX.Utils
{
    public static class RegistryHelper
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "TrayX";

        /// <summary>
        /// Sets the app to run at startup if enabled in the config file.
        /// </summary>
        public static void SetAutoStartIfEnabled(AppConfig config)
        {
            if (!config.AutoStart) return;

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;

                if (key != null && exePath != null)
                    key.SetValue(AppName, exePath);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex);
            }
        }

        /// <summary>
        /// Removes the autostart entry from the registry.
        /// </summary>
        public static void RemoveAutoStart()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
                key?.DeleteValue(AppName, throwOnMissingValue: false);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex);
            }
        }

        /// <summary>
        /// Checks if the app is currently set to run at startup.
        /// </summary>
        public static bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
                return key?.GetValue(AppName) != null;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex);
                return false;
            }
        }
    }
}