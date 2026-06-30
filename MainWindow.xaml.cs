using DTE10T_WPF.Config;
using DTE10T_WPF.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DTE10T_WPF
{
    public partial class MainWindow : Window
    {
        private const int AutoSaveInterval = 100;
        private const int MaxDataPoints = int.MaxValue;
        private const int RateCalculationSeconds = 10;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        private static readonly string[] AlarmModeNames = new[]
        {
            "无警报", "上下限警报", "上限警报", "下限警报",
            "上下限逆动作", "绝对值上下限", "绝对值上限", "绝对值下限",
            "待机上下限", "待机上限", "待机下限", "迟滞上限",
            "迟滞下限", "CT警报"
        };

        private static readonly string[] BaudRateNames = new[]
        {
            "2400", "4800", "9600", "19200", "38400", "57600", "115200"
        };

        // 各通道曲线颜色
        private static readonly OxyColor[] ChannelColors = new[]
        {
            OxyColors.Red,
            OxyColors.Blue,
            OxyColors.Green,
            OxyColors.Orange,
            OxyColors.Purple,
            OxyColors.Cyan,
            OxyColors.Magenta,
            OxyColors.Gold
        };

        // 控制执行/停止状态名称
        private static readonly string[] ControlExecNames = new[]
        {
            "停止", "执行中", "程序结束", "程序暂停"
        };

        private static readonly string[] ControlModeNames = new[]
        {
            "PID", "ON-OFF", "Manual", "可程序PID"
        };

        private static readonly string[] EventFunctionNames = new[]
        {
            "无功能", "执行/停止", "SV1/SV2切换", "自动/手动", "执行/暂停"
        };

        private static readonly string[] OutFunctionNames = new[]
        {
            "加热(逆向)", "冷却(正向)", "比例输出", "输出禁能"
        };

        private static readonly string[] ParityNames = new[]
        {
            "无(None)", "偶(Even)", "奇(Odd)"
        };

        // 正负比例输出设定
        private static readonly string[] ProportionSignNames = new[]
        {
            "正", "负(斜率)"
        };

        // ========== 常量映射 ==========
        private static readonly string[] SensorTypeNames = new[]
        {
            "K型热电偶", "J型热电偶", "T型热电偶", "E型热电偶",
            "N型热电偶", "R型热电偶", "S型热电偶", "B型热电偶",
            "L型热电偶", "U型热电偶", "TXK型", "白金JPt100",
            "白金Pt100", "Ni120", "Cu50"
        };
        private LineSeries[]? _channelSeries;
        private DateTime _chartStartTime;
        private double _chartTimeOffset = 0;
        private readonly List<double>[] _historyPVValues = new List<double>[8];
        private bool _isChartPaused = false;
        // ========== 配置管理 ==========
        private bool _isConfigSaving = false;
        private bool _isConnected = false;
        private bool _isRecording = true;
        private DateTime[] _lastPVTime = new DateTime[8];
        // ========== 温度变化速率计算 ==========
        private double[] _lastPVValues = new double[8];
        private readonly int _maxHistoryPoints = 100;

        // ========== Modbus 服务 ==========
        private ModbusService? _modbus;
        private LineSeries[]? _out1Series;
        private LineSeries[]? _out2Series;
        private System.Threading.Timer? _pollTimer;
        private int _readCount = 0;
        private List<RecordedDataPoint> _recordedData = new();
        private DateTime _recordStartTime;
        // ========== OxyPlot 图表相关 ==========
        private PlotModel? _temperaturePlotModel;
        // ========== 临时CSV文件保存 ==========
        private int _tempFileCounter = 0;
        private string _tempFolder = string.Empty;
        private LineSeries? _tempLowerLine;
        private LineSeries? _tempUpperLine;

        public MainWindow()
        {
            InitializeComponent();
            InitAllGrids();
            icTempCards.ItemsSource = TempCards;
            LoadAvailablePorts();
            InitializeChart();
            DataContext = this;

            for(int i = 0; i < 8; i++)
            {
                _historyPVValues[i] = new List<double>();
            }

            LoadConfigIfExists();
            SetupConfigChangeListeners();

            ConnectAsync().ConfigureAwait(false);
            Task.Run(() => {
                Task.Delay(1000).Wait();
                Application.Current.Dispatcher.Invoke(() => {
                    StartRecord();
                });
            });
        }

        private void AttachListenersToCollection<T>(ObservableCollection<T> collection) where T : INotifyPropertyChanged
        {
            foreach(var item in collection)
            {
                item.PropertyChanged += ModelPropertyChanged;
            }
        }

        private void AttachPropertyChangedListeners()
        {
            AttachListenersToCollection<PVSVModel>(PVSVList);
            AttachListenersToCollection<PIDModel>(PIDList);
            AttachListenersToCollection<AlarmModel>(AlarmList);
            AttachListenersToCollection<OutputModel>(OutputList);
            AttachListenersToCollection<SlopeModel>(SlopeList);
            AttachListenersToCollection<InputAdjModel>(InputAdjList);
            AttachListenersToCollection<CTModel>(CTList);
            AttachListenersToCollection<EventModel>(EventList);
            AttachListenersToCollection<HotRunnerModel>(HotRunnerList);
            AttachListenersToCollection<CommParamModel>(CommList);
            AttachListenersToCollection<ProgramPatternModel>(PatternList);
            AttachListenersToCollection<ProgramStepModel>(StepList);
            AttachListenersToCollection<FunctionSelectModel>(FunctionSelectList);

            // 监听新增项
            PVSVList.CollectionChanged += (sender, e) => {
                if(e.NewItems != null)
                {
                    foreach(INotifyPropertyChanged item in e.NewItems)
                    {
                        item.PropertyChanged += ModelPropertyChanged;
                    }
                }
            };
            PIDList.CollectionChanged += (sender, e) => {
                if(e.NewItems != null)
                {
                    foreach(INotifyPropertyChanged item in e.NewItems)
                    {
                        item.PropertyChanged += ModelPropertyChanged;
                    }
                }
            };
            AlarmList.CollectionChanged += (sender, e) => {
                if(e.NewItems != null)
                {
                    foreach(INotifyPropertyChanged item in e.NewItems)
                    {
                        item.PropertyChanged += ModelPropertyChanged;
                    }
                }
            };
            OutputList.CollectionChanged += (sender, e) => {
                if(e.NewItems != null)
                {
                    foreach(INotifyPropertyChanged item in e.NewItems)
                    {
                        item.PropertyChanged += ModelPropertyChanged;
                    }
                }
            };
            SlopeList.CollectionChanged += (sender, e) => {
                if(e.NewItems != null)
                {
                    foreach(INotifyPropertyChanged item in e.NewItems)
                    {
                        item.PropertyChanged += ModelPropertyChanged;
                    }
                }
            };
            InputAdjList.CollectionChanged += (sender, e) => {
                if(e.NewItems != null)
                {
                    foreach(INotifyPropertyChanged item in e.NewItems)
                    {
                        item.PropertyChanged += ModelPropertyChanged;
                    }
                }
            };
            CTList.CollectionChanged += (sender, e) => {
                if(e.NewItems != null)
                {
                    foreach(INotifyPropertyChanged item in e.NewItems)
                    {
                        item.PropertyChanged += ModelPropertyChanged;
                    }
                }
            };
            EventList.CollectionChanged += (sender, e) => {
                if(e.NewItems != null)
                {
                    foreach(INotifyPropertyChanged item in e.NewItems)
                    {
                        item.PropertyChanged += ModelPropertyChanged;
                    }
                }
            };
            HotRunnerList.CollectionChanged += (sender, e) => {
                if(e.NewItems != null)
                {
                    foreach(INotifyPropertyChanged item in e.NewItems)
                    {
                        item.PropertyChanged += ModelPropertyChanged;
                    }
                }
            };
            CommList.CollectionChanged += (sender, e) => {
                if(e.NewItems != null)
                {
                    foreach(INotifyPropertyChanged item in e.NewItems)
                    {
                        item.PropertyChanged += ModelPropertyChanged;
                    }
                }
            };
            PatternList.CollectionChanged += (sender, e) => {
                if(e.NewItems != null)
                {
                    foreach(INotifyPropertyChanged item in e.NewItems)
                    {
                        item.PropertyChanged += ModelPropertyChanged;
                    }
                }
            };
            StepList.CollectionChanged += (sender, e) => {
                if(e.NewItems != null)
                {
                    foreach(INotifyPropertyChanged item in e.NewItems)
                    {
                        item.PropertyChanged += ModelPropertyChanged;
                    }
                }
            };
            FunctionSelectList.CollectionChanged += (sender, e) => {
                if(e.NewItems != null)
                {
                    foreach(INotifyPropertyChanged item in e.NewItems)
                    {
                        item.PropertyChanged += ModelPropertyChanged;
                    }
                }
            };
        }

        // ========== 通道选择 ==========
        private void BtnSelAll_Click(object sender, RoutedEventArgs e)
        {
            chkCH1.IsChecked = chkCH2.IsChecked = chkCH3.IsChecked = chkCH4.IsChecked =
            chkCH5.IsChecked = chkCH6.IsChecked = chkCH7.IsChecked = chkCH8.IsChecked = true;
        }

        private void BtnSelNone_Click(object sender, RoutedEventArgs e)
        {
            chkCH1.IsChecked = chkCH2.IsChecked = chkCH3.IsChecked = chkCH4.IsChecked =
            chkCH5.IsChecked = chkCH6.IsChecked = chkCH7.IsChecked = chkCH8.IsChecked = false;
        }

        ///<summary>
        /// 设置全部警报模式</summary>
        private async void BtnSetAllAlarmMode_Click(object sender, RoutedEventArgs e)
        {
            string? alarmMode = (cmbSetAllAlarmMode.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if(!string.IsNullOrEmpty(alarmMode))
            {
                int modeIndex = Array.IndexOf(AlarmModeNames, alarmMode);
                if(modeIndex >= 0)
                {
                    foreach(var item in FunctionSelectList)
                    {
                        item.Alarm1Mode = alarmMode;
                        item.Alarm2Mode = alarmMode;
                    }
                    txtStatus.Text = "已设置全部警报模式";
                    txtStatus.Foreground = Brushes.Green;

                    if(_modbus != null && _isConnected)
                    {
                        for(int i = 0; i < 8; i++)
                        {
                            await _modbus.SetAlarm1ModeAsync(i, modeIndex);
                            await _modbus.SetAlarm2ModeAsync(i, modeIndex);
                        }
                        txtStatus.Text = "已写入全部警报模式";
                    }
                }
            }
            else
            {
                MessageBox.Show("请选择有效的警报模式", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        ///<summary>
        /// 设置全部 AT 自整定</summary>
        private async void BtnSetAllAT_Click(object sender, RoutedEventArgs e)
        {
            bool atEnabled = chkSetAllAT.IsChecked ?? false;
            foreach(var item in FunctionSelectList)
            {
                item.ATEnabled = atEnabled;
            }
            txtStatus.Text = atEnabled ? "已启动全部 AT 自整定" : "已停止全部 AT 自整定";
            txtStatus.Foreground = Brushes.Green;

            if(_modbus != null && _isConnected)
            {
                for(int i = 0; i < 8; i++)
                {
                    if(atEnabled)
                    {
                        await _modbus.StartATAsync(i);
                    }
                    else
                    {
                        await _modbus.StopATAsync(i);
                    }
                }
                txtStatus.Text = atEnabled ? "已写入全部启动 AT" : "已写入全部停止 AT";
            }
        }

        ///<summary>
        /// 设置全部控制周期</summary>
        private void BtnSetAllControlCycle_Click(object sender, RoutedEventArgs e)
        { SetAllControlCycle().ConfigureAwait(false); }

        ///<summary>
        /// 设置全部控制执行状态</summary>
        private async void BtnSetAllControlExec_Click(object sender, RoutedEventArgs e)
        {
            string? execStatus = (cmbSetAllControlExec.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if(!string.IsNullOrEmpty(execStatus))
            {
                int statusIndex = Array.IndexOf(ControlExecNames, execStatus);
                if(statusIndex >= 0)
                {
                    foreach(var item in FunctionSelectList)
                    {
                        item.ControlExecStatus = execStatus;
                    }
                    txtStatus.Text = "已设置全部执行状态";
                    txtStatus.Foreground = Brushes.Green;

                    if(_modbus != null && _isConnected)
                    {
                        for(int i = 0; i < 8; i++)
                        {
                            await _modbus.SetControlExecAsync(i, statusIndex);
                        }
                        txtStatus.Text = "已写入全部执行状态";
                    }
                }
            }
            else
            {
                MessageBox.Show("请选择有效的执行状态", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        ///<summary>
        /// 设置全部控制方式</summary>
        private async void BtnSetAllControlMode_Click(object sender, RoutedEventArgs e)
        {
            string? controlMode = (cmbSetAllControlMode.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if(!string.IsNullOrEmpty(controlMode))
            {
                int modeIndex = Array.IndexOf(ControlModeNames, controlMode);
                if(modeIndex >= 0)
                {
                    foreach(var item in FunctionSelectList)
                    {
                        item.ControlMode = controlMode;
                    }
                    txtStatus.Text = "已设置全部控制方式";
                    txtStatus.Foreground = Brushes.Green;

                    if(_modbus != null && _isConnected)
                    {
                        for(int i = 0; i < 8; i++)
                        {
                            await _modbus.SetControlModeAsync(i, modeIndex);
                        }
                        txtStatus.Text = "已写入全部控制方式";
                    }
                }
            }
            else
            {
                MessageBox.Show("请选择有效的控制方式", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        ///<summary>
        /// 设置全部通道使能</summary>
        private void BtnSetAllEnabled_Click(object sender, RoutedEventArgs e)
        {
            bool enabled = chkSetAllEnabled.IsChecked ?? false;
            foreach(var item in PVSVList)
            {
                item.IsEnabled = enabled;
            }
            txtStatus.Text = enabled ? "已启用全部通道" : "已禁用全部通道";
            txtStatus.Foreground = Brushes.Green;
        }

        ///<summary>
        /// 设置全部 OUT1 功能</summary>
        private void BtnSetAllOut1Function_Click(object sender, RoutedEventArgs e)
        {
            string? function = (cmbSetAllOut1Function.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if(!string.IsNullOrEmpty(function))
            {
                foreach(var item in OutputList)
                {
                    item.Out1Function = function;
                }
                txtStatus.Text = "已设置全部 OUT1 功能";
                txtStatus.Foreground = Brushes.Green;
            }
            else
            {
                MessageBox.Show("请选择有效的功能", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        ///<summary>
        /// 设置全部 OUT2 功能</summary>
        private void BtnSetAllOut2Function_Click(object sender, RoutedEventArgs e)
        {
            string? function = (cmbSetAllOut2Function.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if(!string.IsNullOrEmpty(function))
            {
                foreach(var item in OutputList)
                {
                    item.Out2Function = function;
                }
                txtStatus.Text = "已设置全部 OUT2 功能";
                txtStatus.Foreground = Brushes.Green;
            }
            else
            {
                MessageBox.Show("请选择有效的功能", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        ///<summary>
        /// 设置全部输出上限</summary>
        private void BtnSetAllOutMax_Click(object sender, RoutedEventArgs e)
        {
            if(int.TryParse(txtSetAllOutMax.Text, out int value))
            {
                foreach(var item in OutputList)
                {
                    item.OutMax = value;
                }
                txtStatus.Text = "已设置全部输出上限";
                txtStatus.Foreground = Brushes.Green;
            }
            else
            {
                MessageBox.Show("请输入有效的数值", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        ///<summary>
        /// 设置全部输出下限</summary>
        private void BtnSetAllOutMin_Click(object sender, RoutedEventArgs e)
        {
            if(int.TryParse(txtSetAllOutMin.Text, out int value))
            {
                foreach(var item in OutputList)
                {
                    item.OutMin = value;
                }
                txtStatus.Text = "已设置全部输出下限";
                txtStatus.Foreground = Brushes.Green;
            }
            else
            {
                MessageBox.Show("请输入有效的数值", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        ///<summary>
        /// 设置全部输出反向</summary>
        private void BtnSetAllOutputReverse_Click(object sender, RoutedEventArgs e)
        {
            bool reverse = chkSetAllOutputReverse.IsChecked ?? false;
            foreach(var item in OutputList)
            {
                item.OutputReverse = reverse;
            }
            txtStatus.Text = reverse ? "已启用全部输出反向" : "已禁用全部输出反向";
            txtStatus.Foreground = Brushes.Green;
        }

        ///<summary>
        /// 设置全部量程上限</summary>
        private void BtnSetAllRangeHigh_Click(object sender, RoutedEventArgs e)
        {
            if(double.TryParse(txtSetAllRangeHigh.Text, out double value))
            {
                foreach(var item in PVSVList)
                {
                    item.RangeHigh = value;
                }
                txtStatus.Text = "已设置全部量程上限";
                txtStatus.Foreground = Brushes.Green;
            }
            else
            {
                MessageBox.Show("请输入有效的数值", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        ///<summary>
        /// 设置全部量程下限</summary>
        private void BtnSetAllRangeLow_Click(object sender, RoutedEventArgs e)
        { SetAllRangeLow().ConfigureAwait(false); }


        // ========== 功能选择参数 (三) 批量设置按钮 ==========
        ///<summary>
        /// 设置全部传感器类型</summary>
        private async void BtnSetAllSensorType_Click(object sender, RoutedEventArgs e)
        {
            string? sensorType = (cmbSetAllSensorType.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if(!string.IsNullOrEmpty(sensorType))
            {
                int sensorIndex = Array.IndexOf(SensorTypeNames, sensorType);
                if(sensorIndex >= 0)
                {
                    foreach(var item in FunctionSelectList)
                    {
                        item.SensorType = sensorType;
                    }
                    txtStatus.Text = "已设置全部传感器类型";
                    txtStatus.Foreground = Brushes.Green;

                    if(_modbus != null && _isConnected)
                    {
                        for(int i = 0; i < 8; i++)
                        {
                            await _modbus.WriteSensorTypeAsync(i, sensorIndex);
                        }
                        txtStatus.Text = "已写入全部传感器类型";
                    }
                }
            }
            else
            {
                MessageBox.Show("请选择有效的传感器类型", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        ///<summary>
        /// 设置全部 SV 值</summary>
        private void BtnSetAllSV_Click(object sender, RoutedEventArgs e) { SetAllSV().ConfigureAwait(false); }

        // ========== DataGrid 编辑事件处理 ==========

        ///<summary>
        /// 获取通道索引 (CH1-CH8 -> 0-7)</summary>
        private int GetChannelIndex(string channelName)
        {
            if(string.IsNullOrEmpty(channelName) || !channelName.StartsWith("CH"))
            {
                return -1;
            }

            if(int.TryParse(channelName.Substring(2), out int ch))
            {
                return ch - 1;
            }

            return -1;
        }

        ///<summary>
        /// 获取连接失败的详细错误信息</summary>
        // ========== 初始化所有 DataGrid 数据 ==========
        private void InitAllGrids()
        {
            for(int ch = 1; ch <= 8; ch++)
            {
                string chName = $"CH{ch}";
                string bg = ch <= 4 ? "#E3F2FD" : "#FFF3E0";

                TempCards.Add(new() { Channel = chName, BgColor = bg, InputType = ch <= 4 ? "K型热电偶" : "--" });
                PVSVList.Add(new() { Channel = chName, RangeHigh = 1300, RangeLow = -200 });
                PIDList.Add(new()
                {
                    Channel = chName,
                    ControlMode = "PID",
                    Pb = 50,
                    Ti = 120,
                    Td = 30,
                    Integral = 0,
                    Out1 = 0,
                    Out2 = 0
                });
                AlarmList.Add(new()
                {
                    Channel = chName,
                    AlarmMode = "无警报",
                    AlarmHigh = 500,
                    AlarmLow = 0,
                    AlarmDelay = 0
                });
                OutputList.Add(new()
                {
                    Channel = chName,
                    Out1Function = "加热(逆向)",
                    Out2Function = "警报",
                    OutMax = 100,
                    OutMin = 0,
                    ControlCycle = 1
                });
                SlopeList.Add(new() { Channel = chName, SV = 200, Slope = 50 });
                InputAdjList.Add(new() { Channel = chName, Offset = 0, Gain = 0, FilterCount = 8, FilterRange = 1.0 });
                CTList.Add(new() { Channel = chName });
                EventList.Add(new() { Channel = chName, EventFunction = "无功能" });
                HotRunnerList.Add(new()
                {
                    Channel = chName,
                    LimitTemp = 1000,
                    FixedOutput = 350,
                    SoakTime = 15,
                    SV = 200,
                    Slope = 200
                });

                // 功能选择参数 (三)
                FunctionSelectList.Add(new()
                {
                    Channel = chName,
                    SensorType = "K型热电偶",
                    Out1Function = "加热(逆向)",
                    Sub1Function = "加热(逆向)",
                    ControlMode = "PID",
                    Alarm1Mode = "无警报",
                    Alarm2Mode = "无警报",
                    ControlCycle = 1,
                    ControlExecStatus = "停止",
                    ATEnabled = false,
                    ProportionSign = "正"
                });
            }

            // 通讯参数
            CommList.Add(new()
            {
                ParamName = "波特率",
                ParamValue = "9600",
                ParamDesc = "0=2400 1=4800 2=9600 3=19200 4=38400 5=57600 6=115200 (H10FA)"
            });
            CommList.Add(new() { ParamName = "数据位", ParamValue = "8", ParamDesc = "0=8bits 1=7bits (H10FC)" });
            CommList.Add(new() { ParamName = "校验位", ParamValue = "Even", ParamDesc = "0=None 1=Even 2=Odd (H10FE)" });
            CommList.Add(new() { ParamName = "停止位", ParamValue = "1", ParamDesc = "0=2stop 1=1stop (H10FD)" });
            CommList.Add(new() { ParamName = "协议格式", ParamValue = "RTU", ParamDesc = "0=ASCII 1=RTU (H10FB)" });
            CommList.Add(new() { ParamName = "站号", ParamValue = "1", ParamDesc = "范围 1~247 (H10FF)" });
            CommList.Add(new() { ParamName = "自动站号", ParamValue = "0", ParamDesc = "1=启用自动站号分配 (H10F8)" });
            CommList.Add(new()
            {
                ParamName = "通道禁能",
                ParamValue = "0",
                ParamDesc = "Bit0=CH1 ... Bit7=CH8, 1=禁能 (H10F6)"
            });
            CommList.Add(new() { ParamName = "温度单位", ParamValue = "℃", ParamDesc = "0=℃ 1=°F (H10F0)" });

            // 可程控样式
            for(int p = 0; p < 8; p++)
            {
                PatternList.Add(new() { Pattern = $"样式{p}", MaxSteps = 4, LoopCount = 1, NextPattern = 8 });
            }

            // 可程控步骤 (样式0 示例)
            string[] stepNames = { "步骤0", "步骤1", "步骤2", "步骤3", "步骤4", "步骤5", "步骤6", "步骤7" };
            for(int s = 0; s < 8; s++)
            {
                StepList.Add(new() { Pattern = "样式0", Step = stepNames[s], TargetTemp = 100 + s * 50, RunTime = 30 });
            }

            // 绑定数据源
            dgPVSV.ItemsSource = PVSVList;
            dgPID.ItemsSource = PIDList;
            dgAlarm.ItemsSource = AlarmList;
            dgOutput.ItemsSource = OutputList;
            dgSlope.ItemsSource = SlopeList;
            dgInputAdj.ItemsSource = InputAdjList;
            dgCT.ItemsSource = CTList;
            dgEvent.ItemsSource = EventList;
            dgHotRunner.ItemsSource = HotRunnerList;
            dgComm.ItemsSource = CommList;
            dgProgramPattern.ItemsSource = PatternList;
            dgProgramSteps.ItemsSource = StepList;
            dgFunctionSelect.ItemsSource = FunctionSelectList;
        }

        // ========== 工具方法 ==========
        private static float ParseFloat(ushort high, ushort low, double scaling)
        {
            byte[] bytes = new byte[4];
            bytes[0] = (byte)(high >> 8);
            bytes[1] = (byte)(high & 0xFF);
            bytes[2] = (byte)(low >> 8);
            bytes[3] = (byte)(low & 0xFF);
            float value = BitConverter.ToSingle(bytes, 0);
            return (float)(value * scaling);
        }

        private async Task SetAllControlCycle()
        {
            if(int.TryParse(txtSetAllControlCycle.Text, out int value))
            {
                foreach(var item in OutputList)
                {
                    item.ControlCycle = value;
                }

                txtStatus.Text = "已设置全部控制周期";
                txtStatus.Foreground = Brushes.Green;

                if(_modbus != null && _isConnected)
                {
                    for(int i = 0; i < 8; i++)
                    {
                        await _modbus.WriteControlCycleAsync(i, value);
                    }
                    txtStatus.Text = "已写入全部控制周期";
                }
            }
            else
            {
                MessageBox.Show("请输入有效的数值", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task SetAllRangeLow()
        {
            if(double.TryParse(txtSetAllRangeLow.Text, out double value))
            {
                foreach(var item in PVSVList)
                {
                    item.RangeLow = value;
                    await _modbus.WriteRangeLowAsync(GetChannelIndex(item.Channel), value);
                }
                txtStatus.Text = "已设置全部量程下限";
                txtStatus.Foreground = Brushes.Green;
            }
            else
            {
                MessageBox.Show("请输入有效的数值", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task SetAllSV()
        {
            if(double.TryParse(txtSetAllSV.Text, out double value))
            {
                foreach(var item in PVSVList)
                {
                    item.SV = value;
                    await _modbus.WriteSVAsync(GetChannelIndex(item.Channel), item.SV);
                }
                txtStatus.Text = "已设置全部 SV 值";
                txtStatus.Foreground = Brushes.Green;
            }
            else
            {
                MessageBox.Show("请输入有效的数值", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ToggleChartPause()
        {
            if(_isChartPaused || _channelSeries == null || _temperaturePlotModel == null)
            {
                return;
            }

            RecordDataPoint();

            double currentTime = (DateTime.Now - _chartStartTime).TotalSeconds + _chartTimeOffset;
            bool showOut1 = chkShowOut1?.IsChecked ?? false;
            bool showOut2 = chkShowOut2?.IsChecked ?? false;

            for(int i = 0; i < 8; i++)
            {
                CheckBox? chkBox = FindName($"chkCH{i + 1}") as CheckBox;
                bool isVisible = chkBox?.IsChecked ?? false;
                _channelSeries[i].IsVisible = isVisible;

                if(!isVisible)
                {
                    continue;
                }

                double pvValue = TempCards[i].PV;
                _channelSeries[i].Points.Add(new DataPoint(currentTime, pvValue));

                if(_channelSeries[i].Points.Count > MaxDataPoints)
                {
                    _channelSeries[i].Points.RemoveAt(0);
                }

                // 更新输出1量曲线
                if(_out1Series != null)
                {
                    _out1Series[i].IsVisible = showOut1 && isVisible;
                    double out1Value = PVSVList[i].Out1;
                    _out1Series[i].Points.Add(new DataPoint(currentTime, out1Value));

                    if(_out1Series[i].Points.Count > MaxDataPoints)
                    {
                        _out1Series[i].Points.RemoveAt(0);
                    }
                }

                // 更新输出2量曲线
                if(_out2Series != null)
                {
                    _out2Series[i].IsVisible = showOut2 && isVisible;
                    double out2Value = PVSVList[i].Out2;
                    _out2Series[i].Points.Add(new DataPoint(currentTime, out2Value));

                    if(_out2Series[i].Points.Count > MaxDataPoints)
                    {
                        _out2Series[i].Points.RemoveAt(0);
                    }
                }
            }

            if(_temperaturePlotModel.Axes.Count > 0)
            {
                var timeAxis = _temperaturePlotModel.Axes[0] as LinearAxis;
                if(timeAxis != null)
                {
                    timeAxis.Minimum = currentTime - 60;
                    timeAxis.Maximum = currentTime + 5;
                    if(timeAxis.Minimum < 0)
                    {
                        timeAxis.Minimum = 0;
                    }
                }
            }

            UpdateTempRangeLines(currentTime);

            _temperaturePlotModel.InvalidatePlot(true);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveConfig();
            ExportToCsv();
        }

        // ========== 窗口关闭 ==========
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _pollTimer?.Dispose();
            SaveConfig();
            try
            {
                _modbus?.Disconnect();
            }
            catch(Exception ex)
            {
                Logger.Error($"[Closing] 窗口关闭时断开连接异常: {ex.Message}", ex);
            }
            finally
            {
                _modbus = null;
            }
            base.OnClosing(e);
        }

        public ObservableCollection<AlarmModel> AlarmList { get; } = new();

        public ObservableCollection<CommParamModel> CommList { get; } = new();

        public List<string> ControlModeList { get; } = ControlModeNames.ToList();

        public ObservableCollection<CTModel> CTList { get; } = new();

        public ObservableCollection<EventModel> EventList { get; } = new();

        // ========== 功能选择参数 (三) ==========
        public ObservableCollection<FunctionSelectModel> FunctionSelectList { get; } = new();

        public ObservableCollection<HotRunnerModel> HotRunnerList { get; } = new();

        public ObservableCollection<InputAdjModel> InputAdjList { get; } = new();

        public ObservableCollection<OutputModel> OutputList { get; } = new();

        public ObservableCollection<ProgramPatternModel> PatternList { get; } = new();

        public ObservableCollection<PIDModel> PIDList { get; } = new();

        public ObservableCollection<PVSVModel> PVSVList { get; } = new();

        public Visibility ShowChartVisibility => chkShowChart?.IsChecked ?? false ? Visibility.Visible : Visibility.Collapsed;

        public ObservableCollection<SlopeModel> SlopeList { get; } = new();

        public ObservableCollection<ProgramStepModel> StepList { get; } = new();

        // ========== 数据集合 ==========
        public ObservableCollection<TempCardModel> TempCards { get; } = new();

        public PlotModel? TemperaturePlotModel => _temperaturePlotModel;
    }
}
