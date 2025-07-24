using System.Runtime.InteropServices;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace TrayX;

public class RecycleBinService
{
     public void EmptyRecycleBin(TaskbarIcon trayIcon)
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