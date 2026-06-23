using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DTE10T_WPF
{
    public partial class MainWindow : Window
    {
        private const int MaxDataPoints = 100;

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

        private static readonly string[] ControlModeNames = new[]
        {
            "PID", "ON-OFF", "手动", "可程序PID"
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
        private bool _isChartPaused = false;
        // ========== 配置管理 ==========
        private bool _isConfigSaving = false;
        private bool _isConnected = false;

        // ========== Modbus 服务 ==========
        private ModbusService? _modbus;
        private System.Threading.Timer? _pollTimer;
        private int _readCount = 0;
        // ========== OxyPlot 图表相关 ==========
        private PlotModel? _temperaturePlotModel;

        public MainWindow()
        {
            InitializeComponent();
            InitAllGrids();
            icTempCards.ItemsSource = TempCards;
            LoadAvailablePorts();
            InitializeChart();
            DataContext = this;

            LoadConfigIfExists();
            SetupConfigChangeListeners();
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
        }

        private void BtnClearChart_Click(object sender, RoutedEventArgs e) { ClearChart(); }

        // ========== 连接 / 断开 ==========
        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            btnConnect.IsEnabled = false;
            txtStatus.Text = "正在连接...";
            txtStatus.Foreground = Brushes.Orange;

            try
            {
                // 从 UI 获取连接参数
                string port = (cmbComPort.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "COM1";
                int baudRate = int.Parse(((cmbBaudRate.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "9600"));
                int slaveId = int.Parse(txtStationCode.Text);

                // 获取校验位、停止位和协议类型
                string parity = "Even";  // 默认
                string stopBits = "1";
                string protocol = (cmbProtocol.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "RTU";

                _modbus = new ModbusService(
                    slaveId: slaveId,
                    comPort: port,
                    baudRate: baudRate,
                    parity: parity,
                    dataBits: 8,
                    stopBits: stopBits,
                    protocol: protocol
                );

                bool success = await _modbus.ConnectAsync();

                if(success)
                {
                    _isConnected = true;
                    txtStatus.Text = "已连接";
                    txtStatus.Foreground = Brushes.Green;
                    btnConnect.IsEnabled = false;
                    btnDisconnect.IsEnabled = true;
                    btnRefresh.IsEnabled = true;
                    txtConnStatus.Text = "● 已连接";
                    txtConnStatus.Foreground = Brushes.Green;

                    string baud = cmbBaudRate.Text;
                    txtDeviceInfo.Text = $"| 基恩士20站.温控器1 | SlaveID: {slaveId} | {port} | {protocol} | {baud}bps";

                    ledPWR.Fill = Brushes.LimeGreen;

                    // 连接成功后读取一次全部数据
                    await PollAllDataAsync();

                    // 启动定时轮询 (每 1 秒)
                    var ms = 1000;
                    _pollTimer = new System.Threading.Timer(PollCallback, null, ms, ms);
                }
                else
                {
                    txtStatus.Text = "连接失败";
                    txtStatus.Foreground = Brushes.Red;
                    btnConnect.IsEnabled = true;
                    System.Diagnostics.Debug.WriteLine("[Connect] 无法连接到温控器，请检查: COM端口、串口占用、波特率/站号");
                }
            }
            catch(Exception ex)
            {
                txtStatus.Text = "连接异常";
                txtStatus.Foreground = Brushes.Red;
                btnConnect.IsEnabled = true;
                System.Diagnostics.Debug.WriteLine($"[Connect] 连接异常: {ex.Message}");
            }
        }

        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _isConnected = false;
            _pollTimer?.Dispose();
            _pollTimer = null;

            try
            {
                _modbus?.Disconnect();
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Disconnect] 断开连接异常: {ex.Message}");
            }
            finally
            {
                _modbus = null;
            }

            txtStatus.Text = "未连接";
            txtStatus.Foreground = Brushes.Gray;
            btnConnect.IsEnabled = true;
            btnDisconnect.IsEnabled = false;
            btnRefresh.IsEnabled = false;
            txtConnStatus.Text = "● 未连接";
            txtConnStatus.Foreground = Brushes.Gray;
            ledPWR.Fill = Brushes.Gray;
            ledRUN.Fill = Brushes.Gray;
            ledCOM.Fill = Brushes.Gray;
            ledERR.Fill = Brushes.Gray;
        }

        private void BtnPauseChart_Click(object sender, RoutedEventArgs e) { ToggleChartPause(); }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if(_isConnected && _modbus != null)
            {
                btnRefresh.IsEnabled = false;
                try
                {
                    await PollAllDataAsync();
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Refresh] 刷新数据异常: {ex.Message}");
                }
                finally
                {
                    btnRefresh.IsEnabled = true;
                }
            }
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

        private void ChkChannel_CheckedChanged(object sender, RoutedEventArgs e) { UpdateChart(); }

        private void ChkShowChart_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if(chkShowChart?.IsChecked ?? false)
            {
                UpdateChart();
            }
        }

        private void ClearChart()
        {
            if(_channelSeries != null)
            {
                foreach(var series in _channelSeries)
                {
                    series.Points.Clear();
                }
            }
            _chartStartTime = DateTime.Now;
            _chartTimeOffset = 0;

            if(_temperaturePlotModel != null)
            {
                _temperaturePlotModel.InvalidatePlot(true);
            }
        }

        ///<summary>
        /// 警报设定表格编辑事件</summary>
        private async void DgAlarm_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                System.Diagnostics.Debug.WriteLine("[Alarm] 请先连接设备");
                return;
            }

            var model = e.Row.Item as AlarmModel;
            if(model == null)
            {
                return;
            }

            int ch = GetChannelIndex(model.Channel);
            if(ch < 0 || ch >= 8)
            {
                return;
            }

            string columnName = (e.Column as DataGridTextColumn)?.Header?.ToString() ?? string.Empty;

            try
            {
                switch(columnName)
                {
                    case "警报模式":
                        int modeIndex = Array.IndexOf(AlarmModeNames, model.AlarmMode);
                        if(modeIndex >= 0)
                        {
                            await _modbus.SetAlarm1ModeAsync(ch, modeIndex);
                        }

                        break;
                    case "上限值":
                        await _modbus.WriteAlarmHighAsync(ch, model.AlarmHigh);
                        break;
                    case "下限值":
                        await _modbus.WriteAlarmLowAsync(ch, model.AlarmLow);
                        break;
                    case "延迟 (s)":
                        await _modbus.WriteAlarmDelayAsync(ch, model.AlarmDelay);
                        break;
                }
                txtStatus.Text = $"已写入 CH{ch + 1} {columnName}";
                txtStatus.Foreground = Brushes.Green;
            }
            catch(Exception ex)
            {
                txtStatus.Text = $"写入失败: {ex.Message}";
                txtStatus.Foreground = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"[Alarm] 写入失败: {ex.Message}");
            }
        }

        ///<summary>
        /// CT电流表格编辑事件</summary>
        private async void DgCT_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                System.Diagnostics.Debug.WriteLine("[CT] 请先连接设备");
                return;
            }

            var model = e.Row.Item as CTModel;
            if(model == null)
            {
                return;
            }

            int ch = GetChannelIndex(model.Channel);
            if(ch < 0 || ch >= 4)
            {
                return; // CT只有CH1-CH4
            }

            string columnName = (e.Column as DataGridTextColumn)?.Header?.ToString() ?? string.Empty;

            try
            {
                if(columnName == "CT 调整值")
                {
                    await _modbus.WriteCTAdjustAsync(ch, model.CTAdjust);
                    txtStatus.Text = $"已写入 CH{ch + 1} {columnName}";
                    txtStatus.Foreground = Brushes.Green;
                }
            }
            catch(Exception ex)
            {
                txtStatus.Text = $"写入失败: {ex.Message}";
                txtStatus.Foreground = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"[CT] 写入失败: {ex.Message}");
            }
        }

        ///<summary>
        /// EVENT事件表格编辑事件</summary>
        private async void DgEvent_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                System.Diagnostics.Debug.WriteLine("[Event] 请先连接设备");
                return;
            }

            var model = e.Row.Item as EventModel;
            if(model == null)
            {
                return;
            }

            int ch = GetChannelIndex(model.Channel);
            if(ch < 0 || ch >= 8)
            {
                return;
            }

            string columnName = (e.Column as DataGridTextColumn)?.Header?.ToString() ?? string.Empty;

            try
            {
                if(columnName == "EVENT 功能")
                {
                    int funcIndex = Array.IndexOf(EventFunctionNames, model.EventFunction);
                    if(funcIndex >= 0)
                    {
                        await _modbus.WriteEventFunctionAsync(ch, funcIndex);
                        txtStatus.Text = $"已写入 CH{ch + 1} {columnName}";
                        txtStatus.Foreground = Brushes.Green;
                    }
                }
            }
            catch(Exception ex)
            {
                txtStatus.Text = $"写入失败: {ex.Message}";
                txtStatus.Foreground = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"[Event] 写入失败: {ex.Message}");
            }
        }

        ///<summary>
        /// 热流道控制表格编辑事件</summary>
        private async void DgHotRunner_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                System.Diagnostics.Debug.WriteLine("[HotRunner] 请先连接设备");
                return;
            }

            var model = e.Row.Item as HotRunnerModel;
            if(model == null)
            {
                return;
            }

            int ch = GetChannelIndex(model.Channel);
            if(ch < 0 || ch >= 8)
            {
                return;
            }

            string columnName = (e.Column as DataGridTextColumn)?.Header?.ToString() ?? string.Empty;

            try
            {
                switch(columnName)
                {
                    case "界限温度 (0.1℃)":
                        await _modbus.WriteHRLimitTempAsync(ch, model.LimitTemp);
                        break;
                    case "固定输出量 (0.1%)":
                        await _modbus.WriteHRFixedOutputAsync(ch, model.FixedOutput);
                        break;
                    case "定时 (min)":
                        await _modbus.WriteHRSoakTimeAsync(ch, model.SoakTime);
                        break;
                    case "SV 目标 (℃)":
                        await _modbus.WriteSVAsync(ch, model.SV);
                        break;
                    case "斜率 (0.1℃/min)":
                        await _modbus.WriteSlopeAsync(ch, model.Slope);
                        break;
                }
                txtStatus.Text = $"已写入 CH{ch + 1} {columnName}";
                txtStatus.Foreground = Brushes.Green;
            }
            catch(Exception ex)
            {
                txtStatus.Text = $"写入失败: {ex.Message}";
                txtStatus.Foreground = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"[HotRunner] 写入失败: {ex.Message}");
            }
        }

        ///<summary>
        /// 输入调整表格编辑事件</summary>
        private async void DgInputAdj_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                System.Diagnostics.Debug.WriteLine("[InputAdj] 请先连接设备");
                return;
            }

            var model = e.Row.Item as InputAdjModel;
            if(model == null)
            {
                return;
            }

            int ch = GetChannelIndex(model.Channel);
            if(ch < 0 || ch >= 8)
            {
                return;
            }

            string columnName = (e.Column as DataGridTextColumn)?.Header?.ToString() ?? string.Empty;

            try
            {
                switch(columnName)
                {
                    case "补偿值 (0.1℃)":
                        await _modbus.WriteOffsetAsync(ch, model.Offset);
                        break;
                    case "增益 (‰)":
                        await _modbus.WriteGainAsync(ch, model.Gain);
                        break;
                    case "滤波次数":
                        if(ch == 0) // 滤波次数是全局参数
                        {
                            await _modbus.WriteFilterCountAsync(model.FilterCount);
                        }

                        break;
                    case "滤波范围":
                        if(ch == 0) // 滤波范围是全局参数
                        {
                            await _modbus.WriteFilterRangeAsync(model.FilterRange);
                        }

                        break;
                }
                txtStatus.Text = $"已写入 CH{ch + 1} {columnName}";
                txtStatus.Foreground = Brushes.Green;
            }
            catch(Exception ex)
            {
                txtStatus.Text = $"写入失败: {ex.Message}";
                txtStatus.Foreground = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"[InputAdj] 写入失败: {ex.Message}");
            }
        }

        ///<summary>
        /// 输出配置表格编辑事件</summary>
        private async void DgOutput_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                System.Diagnostics.Debug.WriteLine("[Output] 请先连接设备");
                return;
            }

            var model = e.Row.Item as OutputModel;
            if(model == null)
            {
                return;
            }

            int ch = GetChannelIndex(model.Channel);
            if(ch < 0 || ch >= 8)
            {
                return;
            }

            string columnName = (e.Column as DataGridTextColumn)?.Header?.ToString() ?? string.Empty;

            try
            {
                switch(columnName)
                {
                    case "OUT1 功能":
                        int out1Index = Array.IndexOf(OutFunctionNames, model.Out1Function);
                        if(out1Index >= 0)
                        {
                            await _modbus.SetOut1ControlAsync(ch, out1Index);
                        }

                        break;
                    case "OUT2 功能":
                        int out2Index = Array.IndexOf(OutFunctionNames, model.Out2Function);
                        if(out2Index >= 0)
                        {
                            await _modbus.SetOut2ControlAsync(ch, out2Index);
                        }

                        break;
                    case "输出上限 (%)":
                        await _modbus.WriteOutMaxAsync(ch, model.OutMax);
                        break;
                    case "输出下限 (%)":
                        await _modbus.WriteOutMinAsync(ch, model.OutMin);
                        break;
                }
                txtStatus.Text = $"已写入 CH{ch + 1} {columnName}";
                txtStatus.Foreground = Brushes.Green;
            }
            catch(Exception ex)
            {
                txtStatus.Text = $"写入失败: {ex.Message}";
                txtStatus.Foreground = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"[Output] 写入失败: {ex.Message}");
            }
        }

        ///<summary>
        /// PID 参数表格编辑事件</summary>
        private async void DgPID_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                System.Diagnostics.Debug.WriteLine("[PID] 请先连接设备");
                return;
            }

            var model = e.Row.Item as PIDModel;
            if(model == null)
            {
                return;
            }

            int ch = GetChannelIndex(model.Channel);
            if(ch < 0 || ch >= 8)
            {
                return;
            }

            string columnName = (e.Column as DataGridTextColumn)?.Header?.ToString() ?? string.Empty;

            try
            {
                switch(columnName)
                {
                    case "控制方式":
                        int modeIndex = Array.IndexOf(ControlModeNames, model.ControlMode);
                        if(modeIndex >= 0)
                        {
                            await _modbus.SetControlModeAsync(ch, modeIndex);
                        }

                        break;
                    case "Pb 比例带":
                        await _modbus.WritePbAsync(ch, model.Pb);
                        break;
                    case "Ti 积分 (s)":
                        await _modbus.WriteTiAsync(ch, model.Ti);
                        break;
                    case "Td 微分 (s)":
                        await _modbus.WriteTdAsync(ch, model.Td);
                        break;
                    case "AT自整定":
                        if(model.ATEnabled)
                        {
                            await _modbus.StartATAsync(ch);
                        }
                        else
                        {
                            await _modbus.StopATAsync(ch);
                        }

                        break;
                }
                txtStatus.Text = $"已写入 CH{ch + 1} {columnName}";
                txtStatus.Foreground = Brushes.Green;
            }
            catch(Exception ex)
            {
                txtStatus.Text = $"写入失败: {ex.Message}";
                txtStatus.Foreground = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"[PID] 写入失败: {ex.Message}");
            }
        }

        ///<summary>
        /// PV/SV 设定表格编辑事件</summary>
        private async void DgPVSV_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                MessageBox.Show("请先连接设备", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var model = e.Row.Item as PVSVModel;
            if(model == null)
            {
                return;
            }

            int ch = GetChannelIndex(model.Channel);
            if(ch < 0 || ch >= 8)
            {
                return;
            }

            string columnName = (e.Column as DataGridTextColumn)?.Header?.ToString() ?? string.Empty;

            try
            {
                switch(columnName)
                {
                    case "SV 设定值 (℃)":
                        await _modbus.WriteSVAsync(ch, model.SV);
                        break;
                    case "量程上限 (℃)":
                        await _modbus.WriteRangeHighAsync(ch, model.RangeHigh);
                        break;
                    case "量程下限 (℃)":
                        await _modbus.WriteRangeLowAsync(ch, model.RangeLow);
                        break;
                }
                txtStatus.Text = $"已写入 CH{ch + 1} {columnName}";
                txtStatus.Foreground = Brushes.Green;
            }
            catch(Exception ex)
            {
                txtStatus.Text = $"写入失败: {ex.Message}";
                txtStatus.Foreground = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"[Write] 写入失败: {ex.Message}");
            }
        }

        ///<summary>
        /// 斜率控制表格编辑事件</summary>
        private async void DgSlope_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                System.Diagnostics.Debug.WriteLine("[Slope] 请先连接设备");
                return;
            }

            var model = e.Row.Item as SlopeModel;
            if(model == null)
            {
                return;
            }

            int ch = GetChannelIndex(model.Channel);
            if(ch < 0 || ch >= 8)
            {
                return;
            }

            string columnName = (e.Column as DataGridTextColumn)?.Header?.ToString() ?? string.Empty;

            try
            {
                switch(columnName)
                {
                    case "SV 目标 (℃)":
                        await _modbus.WriteSVAsync(ch, model.SV);
                        break;
                    case "斜率 (0.1℃/min)":
                        await _modbus.WriteSlopeAsync(ch, model.Slope);
                        break;
                }
                txtStatus.Text = $"已写入 CH{ch + 1} {columnName}";
                txtStatus.Foreground = Brushes.Green;
            }
            catch(Exception ex)
            {
                txtStatus.Text = $"写入失败: {ex.Message}";
                txtStatus.Foreground = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"[Slope] 写入失败: {ex.Message}");
            }
        }

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
        }

        // ========== OxyPlot 图表初始化 ==========
        private void InitializeChart()
        {
            _temperaturePlotModel = new PlotModel
            {
                Title = "实时温度曲线",
                Background = OxyColors.White,
                PlotAreaBorderColor = OxyColors.LightGray,
                TitleFont = "微软雅黑",
                TitleFontSize = 14
            };

            var timeAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "时间 (s)",
                Minimum = 0,
                Maximum = 60,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColors.LightGray,
                MinorGridlineColor = OxyColors.LightGray,
                TitleFont = "微软雅黑",
                TitleFontSize = 12,
                Font = "微软雅黑",
                FontSize = 11
            };
            _temperaturePlotModel.Axes.Add(timeAxis);

            var tempAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "温度 (℃)",
                Minimum = -50,
                Maximum = 500,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColors.LightGray,
                MinorGridlineColor = OxyColors.LightGray,
                TitleFont = "微软雅黑",
                TitleFontSize = 12,
                Font = "微软雅黑",
                FontSize = 11
            };
            _temperaturePlotModel.Axes.Add(tempAxis);

            _channelSeries = new LineSeries[8];
            for(int i = 0; i < 8; i++)
            {
                _channelSeries[i] = new LineSeries
                {
                    Title = $"CH{i + 1}",
                    Color = ChannelColors[i],
                    StrokeThickness = 2,
                    MarkerType = MarkerType.None,
                    IsVisible = i < 4
                };
                _temperaturePlotModel.Series.Add(_channelSeries[i]);
            }

            _chartStartTime = DateTime.Now;
        }

        ///<summary>
        /// 加载可用的串口列表</summary>
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

        // ========== 配置管理方法 ==========
        private void LoadConfigIfExists()
        {
            bool configLoaded = false;

            if(ConfigManager.ConfigExists())
            {
                try
                {
                    AppConfig config = ConfigManager.LoadConfig();

                    // 加载串口设置
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

            // 如果没有配置文件，或者保存的串口不在可用列表中，则默认选择第一个串口
            if(!configLoaded || cmbComPort.SelectedItem == null)
            {
                if(cmbComPort.Items.Count > 0)
                {
                    cmbComPort.SelectedIndex = 0;
                }
            }

            // 默认选择第一个波特率和协议
            if(cmbBaudRate.SelectedItem == null && cmbBaudRate.Items.Count > 0)
            {
                cmbBaudRate.SelectedIndex = 2; // 默认9600
            }

            if(cmbProtocol.SelectedItem == null && cmbProtocol.Items.Count > 0)
            {
                cmbProtocol.SelectedIndex = 0;
            }
        }

        private void ModelPropertyChanged(object? sender, PropertyChangedEventArgs e) { SaveConfig(); }

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

        private async Task PollAllDataAsync()
        {
            if(_modbus == null)
            {
                return;
            }
            var ws = Stopwatch.StartNew();
            try
            {
                // ===== 1. 读取 PV 值 (H1000~H1007) =====
                var step1Start = Stopwatch.StartNew();
                var pvs = await _modbus.ReadAllPVAsync();
                Debug.WriteLine($"All PVS {string.Join(",", pvs)}");
                for(int i = 0; i < 8 && i < TempCards.Count; i++)
                {
                    TempCards[i].PV = Math.Round(pvs[i], 1);
                    PVSVList[i].PV = Math.Round(pvs[i], 1);
                }
                step1Start.Stop();
                Debug.WriteLine($"[步骤1] 读取PV值耗时: {step1Start.ElapsedMilliseconds}ms");

                // ===== 2. 读取 SV 值 (H1008~H100F) =====
                var step2Start = Stopwatch.StartNew();
                var svs = await _modbus.ReadAllSVAsync();
                Debug.WriteLine($"All SV {string.Join(",", svs)}");
                for(int i = 0; i < 8 && i < TempCards.Count; i++)
                {
                    TempCards[i].SV = Math.Round(svs[i], 1);
                    PVSVList[i].SV = Math.Round(svs[i], 1);
                }
                step2Start.Stop();
                Debug.WriteLine($"[步骤2] 读取SV值耗时: {step2Start.ElapsedMilliseconds}ms");

                // ===== 3. 读取传感器类型 (H10A0~H10A7) =====
                var step3Start = Stopwatch.StartNew();
                for(int i = 0; i < 8; i++)
                {
                    ushort sensorVal = await _modbus.ReadHoldingRegisterAsync(0x10A0 + i);
                    string sensorName = sensorVal < SensorTypeNames.Length
                        ? SensorTypeNames[sensorVal]
                        : $"未知({sensorVal})";
                    TempCards[i].InputType = sensorName;
                    PVSVList[i].InputType = sensorName;
                    Debug.WriteLine($"All cards {sensorName}");
                }
                step3Start.Stop();
                Debug.WriteLine($"[步骤3] 读取传感器类型耗时: {step3Start.ElapsedMilliseconds}ms");

                // ===== 4. 读取 PID 参数 =====
                var step4Start = Stopwatch.StartNew();
                for(int i = 0; i < 8; i++)
                {
                    // Pb (H1028~H102F)
                    ushort pbHigh = await _modbus.ReadHoldingRegisterAsync(0x1028 + i);
                    ushort pbLow = await _modbus.ReadHoldingRegisterAsync(0x1029 + i);
                    PIDList[i].Pb = Math.Round(ParseFloat(pbHigh, pbLow, 0.1), 1);

                    // Ti (H1030~H1037)
                    ushort tiHigh = await _modbus.ReadHoldingRegisterAsync(0x1030 + i);
                    ushort tiLow = await _modbus.ReadHoldingRegisterAsync(0x1031 + i);
                    PIDList[i].Ti = Math.Round(ParseFloat(tiHigh, tiLow, 1), 0);

                    // Td (H1038~H103F)
                    ushort tdHigh = await _modbus.ReadHoldingRegisterAsync(0x1038 + i);
                    ushort tdLow = await _modbus.ReadHoldingRegisterAsync(0x1039 + i);
                    PIDList[i].Td = Math.Round(ParseFloat(tdHigh, tdLow, 1), 0);

                    // 积分量 (H1040~H1047)
                    ushort intHigh = await _modbus.ReadHoldingRegisterAsync(0x1040 + i);
                    ushort intLow = await _modbus.ReadHoldingRegisterAsync(0x1041 + i);
                    PIDList[i].Integral = Math.Round(ParseFloat(intHigh, intLow, 0.1), 1);

                    // 输出1量 (H1070~H1077)
                    ushort o1High = await _modbus.ReadHoldingRegisterAsync(0x1070 + i);
                    ushort o1Low = await _modbus.ReadHoldingRegisterAsync(0x1071 + i);
                    PIDList[i].Out1 = Math.Round(ParseFloat(o1High, o1Low, 0.1), 1);

                    // 输出2量 (H1078~H107F)
                    ushort o2High = await _modbus.ReadHoldingRegisterAsync(0x1078 + i);
                    ushort o2Low = await _modbus.ReadHoldingRegisterAsync(0x1079 + i);
                    PIDList[i].Out2 = Math.Round(ParseFloat(o2High, o2Low, 0.1), 1);

                    // 控制方式 (H10B8~H10BF)
                    ushort ctrlMode = await _modbus.ReadHoldingRegisterAsync(0x10B8 + i);
                    PIDList[i].ControlMode = ctrlMode < ControlModeNames.Length
                        ? ControlModeNames[ctrlMode] : $"未知({ctrlMode})";

                    // AT 状态 (H10E0~H10E7)
                    ushort atStatus = await _modbus.ReadHoldingRegisterAsync(0x10E0 + i);
                    PIDList[i].ATEnabled = atStatus == 1;
                }
                Debug.WriteLine($"All PIDList {PIDList}");
                step4Start.Stop();
                Debug.WriteLine($"[步骤4] 读取PID参数耗时: {step4Start.ElapsedMilliseconds}ms");

                // ===== 5. 读取警报设定 =====
                var step5Start = Stopwatch.StartNew();
                for(int i = 0; i < 8; i++)
                {
                    // 警报上限 (H1080~H1087)
                    ushort ahHigh = await _modbus.ReadHoldingRegisterAsync(0x1080 + i);
                    ushort ahLow = await _modbus.ReadHoldingRegisterAsync(0x1081 + i);
                    AlarmList[i].AlarmHigh = Math.Round(ParseFloat(ahHigh, ahLow, 1), 1);

                    // 警报下限 (H1088~H108F)
                    ushort alHigh = await _modbus.ReadHoldingRegisterAsync(0x1088 + i);
                    ushort alLow = await _modbus.ReadHoldingRegisterAsync(0x1089 + i);
                    AlarmList[i].AlarmLow = Math.Round(ParseFloat(alHigh, alLow, 1), 1);

                    // 警报一模式 (H10C0~H10C7)
                    ushort alarmMode = await _modbus.ReadHoldingRegisterAsync(0x10C0 + i);
                    AlarmList[i].AlarmMode = alarmMode < AlarmModeNames.Length
                        ? AlarmModeNames[alarmMode] : $"未知({alarmMode})";

                    // 警报延迟 (H1990~H1997)
                    ushort delay = await _modbus.ReadHoldingRegisterAsync(0x1990 + i);
                    AlarmList[i].AlarmDelay = delay;
                }
                Debug.WriteLine($"All AlarmList {AlarmList}");
                step5Start.Stop();
                Debug.WriteLine($"[步骤5] 读取警报设定耗时: {step5Start.ElapsedMilliseconds}ms");

                // ===== 6. 读取输出配置 =====
                var step6Start = Stopwatch.StartNew();
                for(int i = 0; i < 8; i++)
                {
                    // OUT1 功能 (H10A8~H10AF)
                    ushort out1Ctrl = await _modbus.ReadHoldingRegisterAsync(0x10A8 + i);
                    OutputList[i].Out1Function = out1Ctrl < OutFunctionNames.Length
                        ? OutFunctionNames[out1Ctrl] : $"未知({out1Ctrl})";

                    // OUT2 功能 (H10B0~H10B7)
                    ushort out2Ctrl = await _modbus.ReadHoldingRegisterAsync(0x10B0 + i);
                    OutputList[i].Out2Function = out2Ctrl < OutFunctionNames.Length
                        ? OutFunctionNames[out2Ctrl] : $"未知({out2Ctrl})";

                    // 输出最大值 (H1980~H1987)
                    ushort outMax = await _modbus.ReadHoldingRegisterAsync(0x1980 + i);
                    OutputList[i].OutMax = outMax;

                    // 输出最小值 (H1988~H198F)
                    ushort outMin = await _modbus.ReadHoldingRegisterAsync(0x1988 + i);
                    OutputList[i].OutMin = outMin;
                }
                Debug.WriteLine($"All OutputList {OutputList}");
                step6Start.Stop();
                Debug.WriteLine($"[步骤6] 读取输出配置耗时: {step6Start.ElapsedMilliseconds}ms");

                // ===== 7. 读取斜率设定 (H1970~H1977) =====
                var step7Start = Stopwatch.StartNew();
                for(int i = 0; i < 8; i++)
                {
                    ushort slopeVal = await _modbus.ReadHoldingRegisterAsync(0x1970 + i);
                    SlopeList[i].Slope = slopeVal;
                }
                Debug.WriteLine($"All SlopeList {SlopeList}");
                step7Start.Stop();
                Debug.WriteLine($"[步骤7] 读取斜率设定耗时: {step7Start.ElapsedMilliseconds}ms");

                // ===== 8. 读取输入调整 =====
                var step8Start = Stopwatch.StartNew();
                for(int i = 0; i < 8; i++)
                {
                    // 补偿值 (H1020~H1027)
                    ushort offset = await _modbus.ReadHoldingRegisterAsync(0x1020 + i);
                    InputAdjList[i].Offset = (short)offset;

                    // 增益值 (H19B8~H19BF)
                    ushort gain = await _modbus.ReadHoldingRegisterAsync(0x19B8 + i);
                    InputAdjList[i].Gain = (short)gain;
                }

                // 滤波次数 (H10F7)
                ushort filterCount = await _modbus.ReadHoldingRegisterAsync(0x10F7);
                InputAdjList[0].FilterCount = filterCount;

                // 滤波范围 (H10F9)
                ushort frHigh = await _modbus.ReadHoldingRegisterAsync(0x10F9);
                ushort frLow = await _modbus.ReadHoldingRegisterAsync(0x10FA);
                InputAdjList[0].FilterRange = Math.Round(ParseFloat(frHigh, frLow, 0.1), 1);

                Debug.WriteLine($"All InputAdjList {InputAdjList}");
                step8Start.Stop();
                Debug.WriteLine($"[步骤8] 读取输入调整耗时: {step8Start.ElapsedMilliseconds}ms");

                // ===== 9. 读取 CT 电流 =====
                var step9Start = Stopwatch.StartNew();
                for(int i = 0; i < 4; i++)
                {
                    // CT 保持值 (H19A0~H19A3)
                    ushort ctSHigh = await _modbus.ReadHoldingRegisterAsync(0x19A0 + i);
                    ushort ctSLow = await _modbus.ReadHoldingRegisterAsync(0x19A1 + i);
                    CTList[i].CTStatic = (int)Math.Round(ParseFloat(ctSHigh, ctSLow, 1), 0);

                    // CT 动态值 (H19A4~H19A7)
                    ushort ctDHigh = await _modbus.ReadHoldingRegisterAsync(0x19A4 + i);
                    ushort ctDLow = await _modbus.ReadHoldingRegisterAsync(0x19A5 + i);
                    CTList[i].CTDynamic = (int)Math.Round(ParseFloat(ctDHigh, ctDLow, 1), 0);

                    // CT 调整值 (H19A8~H19AB)
                    ushort ctAdj = await _modbus.ReadHoldingRegisterAsync(0x19A8 + i);
                    CTList[i].CTAdjust = (short)ctAdj;
                }
                Debug.WriteLine($"All CTList {CTList}");
                step9Start.Stop();
                Debug.WriteLine($"[步骤9] 读取CT电流耗时: {step9Start.ElapsedMilliseconds}ms");

                // ===== 10. 读取 EVENT 功能 (H1998~H199F) =====
                var step10Start = Stopwatch.StartNew();
                for(int i = 0; i < 8; i++)
                {
                    ushort evtVal = await _modbus.ReadHoldingRegisterAsync(0x1998 + i);
                    EventList[i].EventFunction = evtVal < EventFunctionNames.Length
                        ? EventFunctionNames[evtVal] : $"未知({evtVal})";
                }
                Debug.WriteLine($"All EventList {EventList}");
                step10Start.Stop();
                Debug.WriteLine($"[步骤10] 读取EVENT功能耗时: {step10Start.ElapsedMilliseconds}ms");

                // ===== 11. 读取热流道参数 =====
                var step11Start = Stopwatch.StartNew();
                for(int i = 0; i < 8; i++)
                {
                    // 界限温度 (H1960~H1967)
                    ushort limitTemp = await _modbus.ReadHoldingRegisterAsync(0x1960 + i);
                    HotRunnerList[i].LimitTemp = limitTemp;

                    // 固定输出量 (H1968~H196F)
                    ushort fixedOut = await _modbus.ReadHoldingRegisterAsync(0x1968 + i);
                    HotRunnerList[i].FixedOutput = fixedOut;

                    // 定时时间 (H19B0~H19B7)
                    ushort soakTime = await _modbus.ReadHoldingRegisterAsync(0x19B0 + i);
                    HotRunnerList[i].SoakTime = soakTime;

                    // SV (H1008~H100F)
                    ushort svHigh = await _modbus.ReadHoldingRegisterAsync(0x1008 + i);
                    ushort svLow = await _modbus.ReadHoldingRegisterAsync(0x1009 + i);
                    HotRunnerList[i].SV = Math.Round(ParseFloat(svHigh, svLow, 0.1), 1);

                    // 斜率 (H1970~H1977)
                    ushort slope = await _modbus.ReadHoldingRegisterAsync(0x1970 + i);
                    HotRunnerList[i].Slope = slope;
                }
                Debug.WriteLine($"All HotRunnerList {HotRunnerList}");
                step11Start.Stop();
                Debug.WriteLine($"[步骤11] 读取热流道参数耗时: {step11Start.ElapsedMilliseconds}ms");

                // ===== 12. 读取通讯参数 =====
                var step12Start = Stopwatch.StartNew();
                var commParams = await _modbus.ReadCommParamsAsync();
                UpdateCommParamsUI(commParams);
                step12Start.Stop();
                Debug.WriteLine($"[步骤12] 读取通讯参数耗时: {step12Start.ElapsedMilliseconds}ms");

                // ===== 13. 更新 LED 状态 =====
                var step13Start = Stopwatch.StartNew();
                var leds = await _modbus.ReadAllLEDStatusAsync();
                for(int i = 0; i < 8; i++)
                {
                    ushort led = leds[i];
                    // LED 状态位: b0=无报警, b1=Alarm, b2=℃, b3=°F, b4=Alarm1, b5=OUT2, b6=OUT1, b7=AT
                    bool hasAlarm = (led & 0x02) != 0;
                    bool atActive = (led & 0x80) != 0;

                    if(hasAlarm)
                    {
                        ledERR.Fill = Brushes.Red;
                    }
                    else
                    {
                        ledERR.Fill = Brushes.Gray;
                    }

                    if(atActive)
                    {
                        PIDList[i].ATEnabled = true;
                    }
                }
                Debug.WriteLine($"All PIDList {PIDList}");
                step13Start.Stop();
                Debug.WriteLine($"[步骤13] 更新LED状态耗时: {step13Start.ElapsedMilliseconds}ms");

                // ===== 14. 更新状态指示 =====
                var step14Start = Stopwatch.StartNew();

                // 检查错误码
                var (hasError, errorCodes) = await _modbus.CheckErrorsAsync();
                if(hasError)
                {
                    ledERR.Fill = Brushes.Red;
                    for(int i = 0; i < errorCodes.Length; i++)
                    {
                        if(errorCodes[i] >= 0x8001 && errorCodes[i] <= 0x8003)
                        {
                            string errMsg = errorCodes[i] switch
                            {
                                0x8001 => "EEPROM写入失败",
                                0x8002 => $"CH{i + 1} 传感器未连接",
                                0x8003 => "INB群组未连接",
                                _ => $"未知错误(0x{errorCodes[i]:X4})"
                            };
                            TempCards[i].Status = $"❌ {errMsg}";
                        }
                    }
                }
                else
                {
                    ledERR.Fill = Brushes.Gray;
                }

                // ===== 14. 更新状态指示 =====
                ledRUN.Fill = Brushes.LimeGreen;
                ledCOM.Fill = Brushes.Yellow; // 闪烁效果

                // 更新卡片状态
                for(int i = 0; i < 8; i++)
                {
                    double pv = TempCards[i].PV;
                    double sv = TempCards[i].SV;

                    if(Math.Abs(pv - sv) < 2)
                    {
                        TempCards[i].Status = "✔ 稳定";
                    }
                    else if(pv < sv)
                    {
                        TempCards[i].Status = "↑ 加热中";
                    }
                    else
                    {
                        TempCards[i].Status = "↓ 冷却中";
                    }
                }

                _readCount++;
                txtReadCount.Text = $"读取次数: {_readCount}";
                txtLastUpdate.Text = $"最后更新: {DateTime.Now:HH:mm:ss}";

                // 更新实时曲线图表
                UpdateChart();
                step14Start.Stop();
                Debug.WriteLine($"[步骤14] 更新状态指示耗时: {step14Start.ElapsedMilliseconds}ms");
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Poll] 轮询异常: {ex.Message}");
                ledCOM.Fill = Brushes.Red;
                txtStatus.Text = $"通讯异常: {ex.Message}";
                txtStatus.Foreground = Brushes.Red;
            }
            ws.Stop();
            Debug.WriteLine($"总耗时:{ws.ElapsedMilliseconds}ms");
        }

        // ========== 定时轮询 ==========
        private async void PollCallback(object? state)
        {
            if(!_isConnected || _modbus == null)
            {
                return;
            }

            try
            {
                await Dispatcher.InvokeAsync(async () => await PollAllDataAsync());
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PollCallback] 定时轮询异常: {ex.Message}");
            }
        }

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
                System.Diagnostics.Debug.WriteLine("[Config] 配置保存成功");
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

        ///<summary>
        /// 将传感器数值转换为工程值（考虑缩放因子）</summary>
        private static double ScaleToEngineering(ushort rawValue, double scaling)
        { return Math.Round(rawValue * scaling, 2); }

        private void SetupConfigChangeListeners()
        {
            // 监听串口设置变化
            txtStationCode.LostFocus += (sender, e) => SaveConfig();
            cmbBaudRate.SelectionChanged += (sender, e) => SaveConfig();
            cmbComPort.SelectionChanged += (sender, e) => SaveConfig();
            cmbProtocol.SelectionChanged += (sender, e) => SaveConfig();

            // 监听数据集合变化
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

            // 为每个数据模型的 PropertyChanged 事件添加监听
            AttachPropertyChangedListeners();
        }

        private void ToggleChartPause()
        {
            _isChartPaused = !_isChartPaused;
            btnPauseChart.Content = _isChartPaused ? "继续" : "暂停";

            if(!_isChartPaused)
            {
                _chartStartTime = DateTime.Now;
                _chartTimeOffset = 0;
            }
        }

        private void UpdateChart()
        {
            if(_isChartPaused || _channelSeries == null || _temperaturePlotModel == null)
            {
                return;
            }

            double currentTime = (DateTime.Now - _chartStartTime).TotalSeconds + _chartTimeOffset;

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

            _temperaturePlotModel.InvalidatePlot(true);
            Debug.WriteLine($"更新图表");
        }

        private void UpdateCommParamsUI(Dictionary<string, ushort> commParams)
        {
            foreach(var kvp in commParams)
            {
                var item = CommList.FirstOrDefault(c => c.ParamName == kvp.Key);
                if(item == null)
                {
                    continue;
                }

                switch(kvp.Key)
                {
                    case "波特率":
                        item.ParamValue = kvp.Value < BaudRateNames.Length
                            ? BaudRateNames[kvp.Value] : kvp.Value.ToString();
                        break;
                    case "校验位":
                        item.ParamValue = kvp.Value < ParityNames.Length
                            ? ParityNames[kvp.Value] : kvp.Value.ToString();
                        break;
                    default:
                        item.ParamValue = kvp.Value.ToString();
                        break;
                }
            }
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
                System.Diagnostics.Debug.WriteLine($"[Closing] 窗口关闭时断开连接异常: {ex.Message}");
            }
            finally
            {
                _modbus = null;
            }
            base.OnClosing(e);
        }

        public ObservableCollection<AlarmModel> AlarmList { get; } = new();

        public ObservableCollection<CommParamModel> CommList { get; } = new();

        public ObservableCollection<CTModel> CTList { get; } = new();

        public ObservableCollection<EventModel> EventList { get; } = new();

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
