using DTE10T_WPF.Config;
using System;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DTE10T_WPF
{
    public partial class MainWindow
    {
        private void LoadAvailablePorts()
        {
            try
            {
                cmbComPort.Items.Clear();
                string[] ports = SerialPort.GetPortNames();

                if(ports.Length == 0)
                {
                    cmbComPort.Items.Add(new ComboBoxItem { Content = "无可用串口" });
                    cmbComPort.IsEnabled = false;
                }
                else
                {
                    foreach(string port in ports.OrderBy(p => p))
                    {
                        cmbComPort.Items.Add(new ComboBoxItem { Content = port });
                    }
                    cmbComPort.IsEnabled = true;
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadPorts] 加载串口列表异常: {ex.Message}");
            }
        }

        private void LoadConfigIfExists()
        {
            bool configLoaded = false;

            if(ConfigManager.ConfigExists())
            {
                try
                {
                    AppConfig config = ConfigManager.LoadConfig();

                    if(config.SerialPortSettings != null)
                    {
                        txtStationCode.Text = config.SerialPortSettings.SlaveId.ToString();

                        foreach(ComboBoxItem item in cmbBaudRate.Items)
                        {
                            if(item.Content.ToString() == config.SerialPortSettings.BaudRate.ToString())
                            {
                                cmbBaudRate.SelectedItem = item;
                                break;
                            }
                        }

                        foreach(ComboBoxItem item in cmbComPort.Items)
                        {
                            if(item.Content.ToString() == config.SerialPortSettings.PortName)
                            {
                                cmbComPort.SelectedItem = item;
                                break;
                            }
                        }

                        foreach(ComboBoxItem item in cmbProtocol.Items)
                        {
                            if(item.Tag?.ToString() == config.SerialPortSettings.Protocol)
                            {
                                cmbProtocol.SelectedItem = item;
                                break;
                            }
                        }

                        configLoaded = true;
                    }

                    System.Diagnostics.Debug.WriteLine("[Config] 配置加载成功");
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Config] 加载配置失败: {ex.Message}");
                }
            }

            if(!configLoaded || cmbComPort.SelectedItem == null)
            {
                if(cmbComPort.Items.Count > 0)
                {
                    cmbComPort.SelectedIndex = 0;
                }
            }

            if(cmbBaudRate.SelectedItem == null && cmbBaudRate.Items.Count > 0)
            {
                cmbBaudRate.SelectedIndex = 2;
            }

            if(cmbProtocol.SelectedItem == null && cmbProtocol.Items.Count > 0)
            {
                cmbProtocol.SelectedIndex = 0;
            }
        }

        private void ModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) { SaveConfig(); }

        private void SaveConfig()
        {
            if(_isConfigSaving)
            {
                return;
            }

            try
            {
                _isConfigSaving = true;

                AppConfig config = new AppConfig
                {
                    PVSVList = PVSVList.ToList(),
                    PIDList = PIDList.ToList(),
                    AlarmList = AlarmList.ToList(),
                    OutputList = OutputList.ToList(),
                    SlopeList = SlopeList.ToList(),
                    InputAdjList = InputAdjList.ToList(),
                    CTList = CTList.ToList(),
                    EventList = EventList.ToList(),
                    HotRunnerList = HotRunnerList.ToList(),
                    CommList = CommList.ToList(),
                    PatternList = PatternList.ToList(),
                    StepList = StepList.ToList(),
                    SerialPortSettings = new SerialPortSettings
                    {
                        PortName = (cmbComPort.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "COM1",
                        BaudRate = int.TryParse(((cmbBaudRate.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "9600"), out int baud) ? baud : 9600,
                        Parity = "Even",
                        DataBits = 8,
                        StopBits = "1",
                        Protocol = (cmbProtocol.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "RTU",
                        SlaveId = int.TryParse(txtStationCode.Text, out int id) ? id : 1
                    }
                };

                ConfigManager.SaveConfig(config);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Config] 保存配置失败: {ex.Message}");
            }
            finally
            {
                _isConfigSaving = false;
            }
        }

        private void SetupConfigChangeListeners()
        {
            txtStationCode.LostFocus += (sender, e) => SaveConfig();
            cmbBaudRate.SelectionChanged += (sender, e) => SaveConfig();
            cmbComPort.SelectionChanged += (sender, e) => SaveConfig();
            cmbProtocol.SelectionChanged += (sender, e) => SaveConfig();

            PVSVList.CollectionChanged += (sender, e) => SaveConfig();
            PIDList.CollectionChanged += (sender, e) => SaveConfig();
            AlarmList.CollectionChanged += (sender, e) => SaveConfig();
            OutputList.CollectionChanged += (sender, e) => SaveConfig();
            SlopeList.CollectionChanged += (sender, e) => SaveConfig();
            InputAdjList.CollectionChanged += (sender, e) => SaveConfig();
            CTList.CollectionChanged += (sender, e) => SaveConfig();
            EventList.CollectionChanged += (sender, e) => SaveConfig();
            HotRunnerList.CollectionChanged += (sender, e) => SaveConfig();
            CommList.CollectionChanged += (sender, e) => SaveConfig();
            PatternList.CollectionChanged += (sender, e) => SaveConfig();
            StepList.CollectionChanged += (sender, e) => SaveConfig();

            AttachPropertyChangedListeners();
        }
    }
}
