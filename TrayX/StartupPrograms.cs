using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace TrayX;

public class StartupPrograms
{
    public void Tray_ListStartupPrograms()
    {
        try
        {
            var sb = new StringBuilder();

            void AppendPrograms(RegistryKey? key)
            {
                if (key == null) return;
                foreach (var name in key.GetValueNames())
                {
                    var value = key.GetValue(name)?.ToString() ?? string.Empty;
                    // sb.AppendLine($"{name} - {value}");
                     sb.AppendLine($"{name}");
                }
            }

            AppendPrograms(Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"));
            AppendPrograms(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"));

            var userStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            var commonStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);

            foreach (var file in Directory.EnumerateFiles(userStartup, "*.lnk"))
                sb.AppendLine(Path.GetFileNameWithoutExtension(file));
            foreach (var file in Directory.EnumerateFiles(commonStartup, "*.lnk"))
                sb.AppendLine(Path.GetFileNameWithoutExtension(file));

            var result = sb.Length > 0 ? sb.ToString() : "No startup programs found.";
            MessageBox.Show(result, "Startup Programs", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorLogger.LogException(ex);
            MessageBox.Show("An error occurred while listing startup programs. Please check the log for details.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}