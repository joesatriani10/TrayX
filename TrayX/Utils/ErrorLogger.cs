using System;
using System.IO;

namespace TrayX
{
    internal static class ErrorLogger
    {
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");

        public static void LogException(Exception ex)
        {
            try
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:u} {ex}\n\n");
            }
            catch
            {
                // ignore logging failures
            }
        }
    }
}

