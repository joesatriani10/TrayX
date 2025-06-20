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
public partial class MainWindow
{
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _ramCounter;
    private readonly DispatcherTimer _timer;
    private readonly PerformanceCounter _netSentCounter;
    private readonly PerformanceCounter _netReceivedCounter;
    private DateTime _lastDiskUpdate;
    private readonly float _totalMemoryMb;


    public MainWindow()
    {
        InitializeComponent();

        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        _totalMemoryMb = new ComputerInfo().TotalPhysicalMemory / (1024f * 1024f);
        
        CpuText.Text = "Initializing...";
        RamText.Text = "Initializing...";
        DiskText.Text = "Loading disks...";
        NetworkText.Text = "Detecting interface...";


        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        _lastDiskUpdate = DateTime.MinValue;
        _timer.Start();
        
        // Get the name of the first active network interface
        var networkInterfaces = new PerformanceCounterCategory("Network Interface").GetInstanceNames();
        var activeInterface = networkInterfaces.FirstOrDefault(name =>
            !name.ToLower().Contains("loopback") &&
            !name.ToLower().Contains("pseudo") &&
            !name.ToLower().Contains("isatap"));

        if (activeInterface != null)
        {
            _netSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", activeInterface);
            _netReceivedCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", activeInterface);
        }

        Loaded += (_, _) =>
        {
            var screen = SystemParameters.WorkArea;
            Left = screen.Right - Width - 10;
            Top = screen.Bottom - Height - 10;
        };
        
        this.Opacity = 0;
        this.Loaded += (_, _) =>
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
        var cpu = _cpuCounter.NextValue();
        CpuText.Text = $"{cpu:0.0}%";

        switch (cpu)
        {
            case < 30:
                CpuText.Foreground = Brushes.LightGreen;
                CpuText.ToolTip = "Low CPU usage";
                break;
            case < 70:
                CpuText.Foreground = Brushes.Goldenrod;
                CpuText.ToolTip = "Moderate CPU usage";
                break;
            default:
                CpuText.Foreground = Brushes.OrangeRed;
                CpuText.ToolTip = "High CPU usage";
                break;
        }



      
        var ramAvailable = _ramCounter.NextValue();
        var ramTotalMb = _totalMemoryMb;
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

        
        

        if (DateTime.UtcNow - _lastDiskUpdate > TimeSpan.FromSeconds(10))
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
            _lastDiskUpdate = DateTime.UtcNow;
        }

        {
            var sent = _netSentCounter.NextValue();       // bytes per second
            var received = _netReceivedCounter.NextValue();

            var sentMbps = sent * 8 / 1_000_000;          // bits to megabits
            var receivedMbps = received * 8 / 1_000_000;

            NetworkText.Text = $"↓ {FormatValue(receivedMbps)}   ↑ {FormatValue(sentMbps)}";
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
        _timer.Stop();
        _cpuCounter.Dispose();
        _ramCounter.Dispose();
        _netSentCounter.Dispose();
        _netReceivedCounter.Dispose();
        base.OnClosed(e);
    }

    private static string FormatValue(float val)
    {
        return val >= 1 ? $"{val:0.00} Mbps" : $"{val * 1000:0} Kbps";
    }
    
    private void ClearTemp_Click(object sender, RoutedEventArgs e)
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

            MessageBox.Show("Temporary files deleted (or scheduled for deletion).", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
            var flags = NativeMethods.SherbNoconfirmation | NativeMethods.SherbNoprogressui | NativeMethods.SherbNosound;
            var result = NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, flags);

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
    internal static extern uint SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    internal const uint SherbNoconfirmation = 0x00000001;
    internal const uint SherbNoprogressui   = 0x00000002;
    internal const uint SherbNosound        = 0x00000004;
}
