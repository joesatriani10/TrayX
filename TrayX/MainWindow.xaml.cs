using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.Devices;

namespace TrayX
{
public partial class MainWindow : Window
{
    private PerformanceCounter cpuCounter;
    private PerformanceCounter ramCounter;
    private DispatcherTimer timer;
    private PerformanceCounter netSentCounter;
    private PerformanceCounter netReceivedCounter;
    private DateTime lastDiskUpdate;
    private readonly float totalMemoryMb;


    public MainWindow()
    {
        InitializeComponent();

        cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        totalMemoryMb = new ComputerInfo().TotalPhysicalMemory / (1024f * 1024f);
        
        CpuText.Text = "Initializing...";
        RamText.Text = "Initializing...";
        DiskText.Text = "Loading disks...";
        NetworkText.Text = "Detecting interface...";


        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        timer.Tick += Timer_Tick;
        lastDiskUpdate = DateTime.MinValue;
        timer.Start();
        
        // Get name of the first active network interface
        var networkInterfaces = new PerformanceCounterCategory("Network Interface").GetInstanceNames();
        var activeInterface = networkInterfaces.FirstOrDefault(name =>
            !name.ToLower().Contains("loopback") &&
            !name.ToLower().Contains("pseudo") &&
            !name.ToLower().Contains("isatap"));

        if (activeInterface != null)
        {
            netSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", activeInterface);
            netReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", activeInterface);
        }

        Loaded += (s, e) =>
        {
            var screen = SystemParameters.WorkArea;
            Left = screen.Right - Width - 10;
            Top = screen.Bottom - Height - 10;
        };
        
        this.Opacity = 0;
        this.Loaded += (s, e) =>
        {
            var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3))
            {
                AccelerationRatio = 0.2,
                DecelerationRatio = 0.8
            };
            this.BeginAnimation(Window.OpacityProperty, fadeIn);
        };


    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        // CPU
        var cpu = cpuCounter.NextValue();
        CpuText.Text = $"{cpu:0.0}%";

        if (cpu < 30)
        {
            CpuText.Foreground = Brushes.LightGreen;
            CpuText.ToolTip = "Low CPU usage";
        }
        else if (cpu < 70)
        {
            CpuText.Foreground = Brushes.Goldenrod;
            CpuText.ToolTip = "Moderate CPU usage";
        }
        else
        {
            CpuText.Foreground = Brushes.OrangeRed;
            CpuText.ToolTip = "High CPU usage";
        }



      
        var ramAvailable = ramCounter.NextValue();
        var ramTotalMb = totalMemoryMb;
        var ramUsedMb = ramTotalMb - ramAvailable;

        // Convert to GB
        var ramTotalGb = ramTotalMb / 1024.0;
        var ramUsedGb = ramUsedMb / 1024.0;
        var ramPercent = (ramUsedGb / ramTotalGb) * 100;
        
        if (ramPercent < 50)
        {
            RamText.Foreground = Brushes.LightGreen;
            RamText.ToolTip = "Sufficient memory available";
        }
        else if (ramPercent < 80)
        {
            RamText.Foreground = Brushes.Goldenrod;
            RamText.ToolTip = "Memory usage is increasing";
        }
        else
        {
            RamText.Foreground = Brushes.OrangeRed;
            RamText.ToolTip = "High memory usage";
        }


        RamText.Text = $"{ramUsedGb:0.0} GB / {ramTotalGb:0.0} GB ({ramPercent:0.0}%)";

        
        

        if (DateTime.UtcNow - lastDiskUpdate > TimeSpan.FromSeconds(10))
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed);

            var sb = new StringBuilder();

            foreach (var d in drives)
            {
                double total = d.TotalSize / (1024.0 * 1024 * 1024);           // GB
                double free = d.AvailableFreeSpace / (1024.0 * 1024 * 1024);   // GB
                double used = total - free;
                double percentUsed = (used / total) * 100;

                sb.AppendLine($"{d.Name} → {used:0.0} GB used / {total:0.0} GB ({percentUsed:0.0}%)");
            }

            DiskText.Text = sb.ToString();
            lastDiskUpdate = DateTime.UtcNow;
        }
        
        if (netSentCounter != null && netReceivedCounter != null)
        {
            float sent = netSentCounter.NextValue();       // bytes per second
            float received = netReceivedCounter.NextValue();

            float sentMbps = sent * 8 / 1_000_000;          // bits to megabits
            float receivedMbps = received * 8 / 1_000_000;

            NetworkText.Text = $"↓ {formatValue(receivedMbps)}   ↑ {formatValue(sentMbps)}";
        }
        else
        {
            NetworkText.Text = "No active interface detected";
        }

    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Hide(); // or this.Close(); if you want to exit
    }


    protected override void OnClosed(EventArgs e)
    {
        timer.Stop();
        cpuCounter.Dispose();
        ramCounter.Dispose();
        netSentCounter?.Dispose();
        netReceivedCounter?.Dispose();
        base.OnClosed(e);
    }
    
    string formatValue(float val)
    {
        return val >= 1 ? $"{val:0.00} Mbps" : $"{val * 1000:0} Kbps";
    }
    
    private void ClearTemp_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var tempPath = Environment.GetEnvironmentVariable("TEMP");
            if (!string.IsNullOrEmpty(tempPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C del /q/f/s \"{tempPath}\\*\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });

                MessageBox.Show("Temporary files deleted (or scheduled for deletion).", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error clearing temp: {ex.Message}");
        }
    }

    private void FlushDns_Click(object sender, RoutedEventArgs e)
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

            MessageBox.Show("DNS cache flushed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error flushing DNS: {ex.Message}");
        }
    }
    
    private void EmptyRecycleBin_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            uint flags = NativeMethods.SHERB_NOCONFIRMATION | NativeMethods.SHERB_NOPROGRESSUI | NativeMethods.SHERB_NOSOUND;
            uint result = NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, flags);

            if (result == 0)
            {
                MessageBox.Show("Recycle Bin emptied successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Failed to empty Recycle Bin. Error code: {result}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error: " + ex.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


}
}
internal static class NativeMethods
{
    [DllImport("shell32.dll", SetLastError = true)]
    internal static extern uint SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

    internal const uint SHERB_NOCONFIRMATION = 0x00000001;
    internal const uint SHERB_NOPROGRESSUI   = 0x00000002;
    internal const uint SHERB_NOSOUND        = 0x00000004;
}
