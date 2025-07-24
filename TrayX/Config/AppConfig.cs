using System;
using System.IO;
using System.Text.Json;

namespace TrayX
{
    public class AppConfig
    {
        public int UpdateIntervalSeconds { get; set; } = 1;
        public string? NetworkInterface { get; set; }
        public bool AutoStart { get; set; } = false;

        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                    if (cfg != null) return cfg;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex);
            }
            return new AppConfig();
        }

        public void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogException(ex);
            }
        }
    }
}
