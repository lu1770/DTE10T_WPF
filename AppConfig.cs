using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DTE10T_WPF
{
    public class AppConfig
    {
        public List<PVSVModel> PVSVList { get; set; } = new();
        public List<PIDModel> PIDList { get; set; } = new();
        public List<AlarmModel> AlarmList { get; set; } = new();
        public List<OutputModel> OutputList { get; set; } = new();
        public List<SlopeModel> SlopeList { get; set; } = new();
        public List<InputAdjModel> InputAdjList { get; set; } = new();
        public List<CTModel> CTList { get; set; } = new();
        public List<EventModel> EventList { get; set; } = new();
        public List<HotRunnerModel> HotRunnerList { get; set; } = new();
        public List<CommParamModel> CommList { get; set; } = new();
        public List<ProgramPatternModel> PatternList { get; set; } = new();
        public List<ProgramStepModel> StepList { get; set; } = new();

        public SerialPortSettings SerialPortSettings { get; set; } = new();
    }

    public class SerialPortSettings
    {
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public string Parity { get; set; } = "Even";
        public int DataBits { get; set; } = 8;
        public string StopBits { get; set; } = "1";
        public string Protocol { get; set; } = "RTU";
        public int SlaveId { get; set; } = 1;
    }

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
