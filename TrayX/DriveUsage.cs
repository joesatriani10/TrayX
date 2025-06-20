using System;

namespace TrayX
{
    public class DriveUsage
    {
        public string Name { get; set; } = string.Empty;
        public double UsedGb { get; set; }
        public double TotalGb { get; set; }
        public double PercentUsed { get; set; }
    }
}
