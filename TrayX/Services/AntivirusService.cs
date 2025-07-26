using System;
using System.Linq;
using System.Management;

namespace TrayX;

public static class AntivirusService
{
    public static bool IsAntivirusInstalled()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\SecurityCenter2", "SELECT * FROM AntiVirusProduct");
            using var results = searcher.Get();
            return results.Cast<ManagementObject>().Any();
        }
        catch (Exception ex)
        {
            ErrorLogger.LogException(ex);
            return false;
        }
    }

    public static bool IsAntivirusUpdated()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\SecurityCenter2", "SELECT productState FROM AntiVirusProduct");
            foreach (ManagementObject av in searcher.Get())
            {
                int state = Convert.ToInt32(av["productState"]);
                return (state & 0x10) == 0; // 0 = up-to-date
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogException(ex);
        }

        return false;
    }

    public static string? GetAntivirusName()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\SecurityCenter2", "SELECT displayName FROM AntiVirusProduct");
            foreach (ManagementObject av in searcher.Get())
            {
                return av["displayName"]?.ToString();
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.LogException(ex);
        }

        return null;
    }
}
