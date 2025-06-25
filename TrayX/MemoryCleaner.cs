using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TrayX
{
    internal static class MemoryCleaner
    {
        [DllImport("psapi.dll")]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        public static Task CleanAsync()
        {
            return Task.Run(() =>
            {
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        EmptyWorkingSet(process.Handle);
                    }
                    catch
                    {
                        // ignore failures on system processes
                    }
                }
            });
        }
    }
}
