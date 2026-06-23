using System.IO;
using System.Text.Json;

namespace DTE10T_WPF
{
    public static class ConfigManager
    {
        private const string ConfigFileName = "app_config.json";

        public static string ConfigFilePath => Path.Combine(
            System.AppDomain.CurrentDomain.BaseDirectory,
            ConfigFileName
        );

        public static void SaveConfig(AppConfig config)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                string json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] 保存配置失败: {ex.Message}");
            }
        }

        public static AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] 加载配置失败: {ex.Message}");
            }
            return new AppConfig();
        }

        public static bool ConfigExists()
        {
            return File.Exists(ConfigFilePath);
        }
    }
}
