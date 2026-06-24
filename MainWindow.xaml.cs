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
using System.Diagnostics;
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
        private const int MaxDataPoints = int.MaxValue;

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
        private bool _isRecording = true;

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

            ConnectAsync().ConfigureAwait(false);
            Task.Run(() => {
                Task.Delay(1000).Wait();
                Application.Current.Dispatcher.Invoke(() => {
                    StartRecord();
                });
            });
        }

        private async Task<bool> ApplyPollAllDataAsync()
        {
            if(_modbus == null)
            {
                return false;
            }
            var ws = Stopwatch.StartNew();
            try
            {
                // 获取当前选中的Tab索引
                int selectedTabIndex = mainTab.SelectedIndex;

                // 始终需要的基础数据（PV、SV、传感器类型、LED状态、状态指示）
                await PollPVValuesAsync();
                await PollSVValuesAsync();
                await PollSensorTypesAsync();
                await UpdateLEDStatusAsync();
                await UpdateStatusIndicatorsAsync();

                // 根据当前Tab选择加载对应模块的数据
                switch(selectedTabIndex)
                {
                    case 0: // 🌡 实时温度
                        // 基础数据已加载，无需额外加载
                        break;
                    case 1: // 📊 PV/SV 设定
                        // 基础数据已包含PV、SV、传感器类型
                        break;
                    case 2: // 🔧 PID 参数
                        await PollPIDParametersAsync();
                        break;
                    case 3: // 🔔 警报设定
                        await PollAlarmSettingsAsync();
                        break;
                    case 4: // ⚡ 输出配置
                        await PollOutputConfigAsync();
                        break;
                    case 5: // 📐 高级功能 (斜率控制、输入补偿、CT电流、EVENT、热流道)
                        await PollSlopeSettingsAsync();
                        await PollInputAdjustmentsAsync();
                        await PollCTCurrentAsync();
                        await PollEventFunctionsAsync();
                        await PollHotRunnerParamsAsync();
                        break;
                    case 6: // 🔌 通讯参数
                        await PollCommParamsAsync();
                        break;
                    case 7: // 📅 可程控
                        // 可程控数据未在轮询中加载
                        break;
                    default:
                        // 默认加载所有数据
                        await PollPIDParametersAsync();
                        await PollAlarmSettingsAsync();
                        await PollOutputConfigAsync();
                        await PollSlopeSettingsAsync();
                        await PollInputAdjustmentsAsync();
                        await PollCTCurrentAsync();
                        await PollEventFunctionsAsync();
                        await PollHotRunnerParamsAsync();
                        await PollCommParamsAsync();
                        break;
                }
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
            return true;
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
        private async void BtnConnect_Click(object sender, RoutedEventArgs e) { await ConnectAsync(); }

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

        private void BtnSaveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(_temperaturePlotModel == null)
                {
                    MessageBox.Show("没有可保存的图表数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PNG 图像 (*.png)|*.png|JPEG 图像 (*.jpg)|*.jpg|所有文件 (*.*)|*.*",
                    FilterIndex = 1,
                    FileName = $"温度曲线_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                    Title = "保存图像"
                };

                bool? result = saveFileDialog.ShowDialog();
                if(result == true)
                {
                    string filePath = saveFileDialog.FileName;
                    string extension = Path.GetExtension(filePath).ToLower();

                    using(var stream = File.Create(filePath))
                    {
                        var exporter = new PngExporter
                        {
                            Width = (int)pvTemperature.ActualWidth,
                            Height = (int)pvTemperature.ActualHeight
                        };

                        exporter.Export(_temperaturePlotModel, stream);
                    }

                    MessageBox.Show($"图像已保存到:\n{filePath}", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);

                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"保存图像失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[SaveImage] 保存失败: {ex.Message}");
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

        private void BtnStartRecord_Click(object sender, RoutedEventArgs e) { StartRecord(); }

        private void BtnStopRecord_Click(object sender, RoutedEventArgs e) { StopRecord(); }

        private void ChkChannel_CheckedChanged(object sender, RoutedEventArgs e) { UpdateChart(); }

        private void ChkShowChart_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if(chkShowChart?.IsChecked ?? false)
            {
                UpdateChart();
            }
        }

        private void ChkShowOut1_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool showOut1 = chkShowOut1?.IsChecked ?? false;
            if(_out1Series != null)
            {
                for(int i = 0; i < 8; i++)
                {
                    CheckBox? chkBox = FindName($"chkCH{i + 1}") as CheckBox;
                    bool isVisible = chkBox?.IsChecked ?? false;
                    _out1Series[i].IsVisible = showOut1 && isVisible;
                }
            }
        }

        private void ChkShowOut2_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool showOut2 = chkShowOut2?.IsChecked ?? false;
            if(_out2Series != null)
            {
                for(int i = 0; i < 8; i++)
                {
                    CheckBox? chkBox = FindName($"chkCH{i + 1}") as CheckBox;
                    bool isVisible = chkBox?.IsChecked ?? false;
                    _out2Series[i].IsVisible = showOut2 && isVisible;
                }
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
            if(_out1Series != null)
            {
                foreach(var series in _out1Series)
                {
                    series.Points.Clear();
                }
            }
            if(_out2Series != null)
            {
                foreach(var series in _out2Series)
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

        private async Task ConnectAsync()
        {
            btnConnect.IsEnabled = false;
            txtStatus.Text = "正在连接...";
            txtStatus.Foreground = Brushes.Orange;
            ledCOM.Fill = Brushes.Yellow;

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
                    ledCOM.Fill = Brushes.Green;

                    // 连接成功后读取一次全部数据
                    await PollAllDataAsync();

                    // 启动定时轮询 (每 1 秒)
                    var ms = 500;
                    _pollTimer = new System.Threading.Timer(PollCallback, null, ms, ms);
                }
                else
                {
                    txtStatus.Text = "连接失败: 无法建立连接，请检查串口设置";
                    txtStatus.Foreground = Brushes.Red;
                    btnConnect.IsEnabled = true;
                    ledCOM.Fill = Brushes.Red;
                    System.Diagnostics.Debug.WriteLine("[Connect] 无法连接到温控器，请检查: COM端口、串口占用、波特率/站号");
                }
            }
            catch(Exception ex)
            {
                string errorMessage = GetConnectionErrorMessage(ex);
                txtStatus.Text = $"连接失败: {errorMessage}";
                txtStatus.Foreground = Brushes.Red;
                btnConnect.IsEnabled = true;
                ledCOM.Fill = Brushes.Red;
                System.Diagnostics.Debug.WriteLine($"[Connect] 连接异常: {ex.Message}");
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

            string columnName = (e.Column as DataGridTextColumn)?.Header?.ToString() ?? (e.Column as DataGridCheckBoxColumn)?.Header?.ToString() ?? string.Empty;

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
                        await _modbus.StartATAsync(ch);
                        //if (model.ATEnabled)
                        //{
                        //    await _modbus.StartATAsync(ch);
                        //}
                        //else
                        //{
                        //    await _modbus.StopATAsync(ch);
                        //}

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
        /// 设置全部 SV 值</summary>
        private void BtnSetAllSV_Click(object sender, RoutedEventArgs e)
        {
            if(double.TryParse(txtSetAllSV.Text, out double value))
            {
                foreach(var item in PVSVList)
                {
                    item.SV = value;
                }
                txtStatus.Text = "已设置全部 SV 值";
                txtStatus.Foreground = Brushes.Green;
            }
            else
            {
                MessageBox.Show("请输入有效的数值", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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
        {
            if(double.TryParse(txtSetAllRangeLow.Text, out double value))
            {
                foreach(var item in PVSVList)
                {
                    item.RangeLow = value;
                }
                txtStatus.Text = "已设置全部量程下限";
                txtStatus.Foreground = Brushes.Green;
            }
            else
            {
                MessageBox.Show("请输入有效的数值", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void ExportToCsv()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                    FilterIndex = 1,
                    FileName = $"温度记录_{_recordStartTime:yyyyMMdd_HHmmss}.csv",
                    Title = "导出CSV文件"
                };

                bool? result = saveFileDialog.ShowDialog();
                if(result == true)
                {
                    string filePath = saveFileDialog.FileName;

                    using(var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                    {
                        // 写入表头
                        writer.Write("时间戳,相对时间(s)");
                        for(int i = 0; i < 8; i++)
                        {
                            writer.Write($",CH{i + 1}(℃)");
                        }
                        for(int i = 0; i < 8; i++)
                        {
                            writer.Write($",CH{i + 1}输出1(%)");
                        }
                        for(int i = 0; i < 8; i++)
                        {
                            writer.Write($",CH{i + 1}输出2(%)");
                        }
                        writer.WriteLine();

                        // 写入数据
                        foreach(var point in _recordedData)
                        {
                            writer.Write($"{point.Timestamp:yyyy-MM-dd HH:mm:ss},{point.ElapsedSeconds:F2}");
                            for(int i = 0; i < 8; i++)
                            {
                                writer.Write($",{point.CHValues[i]:F1}");
                            }
                            for(int i = 0; i < 8; i++)
                            {
                                writer.Write($",{point.Out1Values[i]:F1}");
                            }
                            for(int i = 0; i < 8; i++)
                            {
                                writer.Write($",{point.Out2Values[i]:F1}");
                            }
                            writer.WriteLine();
                        }
                    }

                    txtStatus.Text = $"✅ 已导出 {_recordedData.Count} 条数据";
                    txtStatus.Foreground = Brushes.Green;

                    MessageBox.Show($"数据已导出到:\n{filePath}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);

                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"[ExportCSV] 导出失败: {ex.Message}");
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

        ///<summary>
        /// 获取连接失败的详细错误信息</summary>
        private string GetConnectionErrorMessage(Exception ex)
        {
            if(ex is System.IO.IOException)
            {
                return "串口无法打开，请检查端口是否被占用或不存在";
            }
            else if(ex is UnauthorizedAccessException)
            {
                return "权限不足，无法访问串口";
            }
            else if(ex is ArgumentOutOfRangeException)
            {
                return "串口参数无效，请检查波特率等设置";
            }
            else if(ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                return "连接超时，请检查设备是否在线或通讯参数是否正确";
            }
            else if(ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return "串口不存在，请检查COM端口设置";
            }
            else
            {
                return ex.Message;
            }
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
                Minimum = -200,
                Maximum = 1300,
                AbsoluteMinimum = -200,   // 禁止缩放到比这更小
                AbsoluteMaximum = 1300,  // 禁止缩放到比这更大
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

            var outputAxis = new LinearAxis
            {
                Position = AxisPosition.Right,
                Title = "输出 (%)",
                Minimum = 0,
                Maximum = 100,
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColors.LightGray,
                MinorGridlineColor = OxyColors.LightGray,
                TitleFont = "微软雅黑",
                TitleFontSize = 12,
                Font = "微软雅黑",
                FontSize = 11,
                Key = "OutputAxis"
            };
            _temperaturePlotModel.Axes.Add(outputAxis);

            _channelSeries = new LineSeries[8];
            _out1Series = new LineSeries[8];
            _out2Series = new LineSeries[8];
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

                _out1Series[i] = new LineSeries
                {
                    Title = $"CH{i + 1} 输出1",
                    Color = ChannelColors[i],
                    StrokeThickness = 1,
                    MarkerType = MarkerType.None,
                    LineStyle = LineStyle.Dash,
                    IsVisible = false,
                    YAxisKey = "OutputAxis"
                };
                _temperaturePlotModel.Series.Add(_out1Series[i]);

                _out2Series[i] = new LineSeries
                {
                    Title = $"CH{i + 1} 输出2",
                    Color = ChannelColors[i],
                    StrokeThickness = 1,
                    MarkerType = MarkerType.None,
                    LineStyle = LineStyle.Dot,
                    IsVisible = false,
                    YAxisKey = "OutputAxis"
                };
                _temperaturePlotModel.Series.Add(_out2Series[i]);
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

        private async Task PollAlarmSettingsAsync()
        {
            var stepStart = Stopwatch.StartNew();
            for(int i = 0; i < 8; i++)
            {
                ushort ahHigh = await _modbus!.ReadHoldingRegisterAsync(0x1080 + i);
                ushort ahLow = await _modbus!.ReadHoldingRegisterAsync(0x1081 + i);
                AlarmList[i].AlarmHigh = Math.Round(ParseFloat(ahHigh, ahLow, 1), 1);

                ushort alHigh = await _modbus!.ReadHoldingRegisterAsync(0x1088 + i);
                ushort alLow = await _modbus!.ReadHoldingRegisterAsync(0x1089 + i);
                AlarmList[i].AlarmLow = Math.Round(ParseFloat(alHigh, alLow, 1), 1);

                ushort alarmMode = await _modbus!.ReadHoldingRegisterAsync(0x10C0 + i);
                AlarmList[i].AlarmMode = alarmMode < AlarmModeNames.Length
                    ? AlarmModeNames[alarmMode] : $"未知({alarmMode})";

                ushort delay = await _modbus!.ReadHoldingRegisterAsync(0x1990 + i);
                AlarmList[i].AlarmDelay = delay;
            }
            stepStart.Stop();
            // Debug.WriteLine($"[步骤5] 读取警报设定耗时: {stepStart.ElapsedMilliseconds}ms");
        }

        private async Task PollAllDataAsync()
        { await DoWorkAsync(nameof(ApplyPollAllDataAsync), async () => await ApplyPollAllDataAsync(), "轮询数据中..."); }

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

        private async Task PollCommParamsAsync()
        {
            var stepStart = Stopwatch.StartNew();
            var commParams = await _modbus!.ReadCommParamsAsync();
            UpdateCommParamsUI(commParams);
            stepStart.Stop();
            // Debug.WriteLine($"[步骤12] 读取通讯参数耗时: {stepStart.ElapsedMilliseconds}ms");
        }

        private async Task PollCTCurrentAsync()
        {
            var stepStart = Stopwatch.StartNew();
            for(int i = 0; i < 4; i++)
            {
                ushort ctSHigh = await _modbus!.ReadHoldingRegisterAsync(0x19A0 + i);
                ushort ctSLow = await _modbus!.ReadHoldingRegisterAsync(0x19A1 + i);
                CTList[i].CTStatic = (int)Math.Round(ParseFloat(ctSHigh, ctSLow, 1), 0);

                ushort ctDHigh = await _modbus!.ReadHoldingRegisterAsync(0x19A4 + i);
                ushort ctDLow = await _modbus!.ReadHoldingRegisterAsync(0x19A5 + i);
                CTList[i].CTDynamic = (int)Math.Round(ParseFloat(ctDHigh, ctDLow, 1), 0);

                ushort ctAdj = await _modbus!.ReadHoldingRegisterAsync(0x19A8 + i);
                CTList[i].CTAdjust = (short)ctAdj;
            }
            stepStart.Stop();
            // Debug.WriteLine($"[步骤9] 读取CT电流耗时: {stepStart.ElapsedMilliseconds}ms");
        }

        private async Task PollEventFunctionsAsync()
        {
            var stepStart = Stopwatch.StartNew();
            for(int i = 0; i < 8; i++)
            {
                ushort evtVal = await _modbus!.ReadHoldingRegisterAsync(0x1998 + i);
                EventList[i].EventFunction = evtVal < EventFunctionNames.Length
                    ? EventFunctionNames[evtVal] : $"未知({evtVal})";
            }
            stepStart.Stop();
            // Debug.WriteLine($"[步骤10] 读取EVENT功能耗时: {stepStart.ElapsedMilliseconds}ms");
        }

        private async Task PollHotRunnerParamsAsync()
        {
            var stepStart = Stopwatch.StartNew();
            for(int i = 0; i < 8; i++)
            {
                ushort limitTemp = await _modbus!.ReadHoldingRegisterAsync(0x1960 + i);
                HotRunnerList[i].LimitTemp = limitTemp;

                ushort fixedOut = await _modbus!.ReadHoldingRegisterAsync(0x1968 + i);
                HotRunnerList[i].FixedOutput = fixedOut;

                ushort soakTime = await _modbus!.ReadHoldingRegisterAsync(0x19B0 + i);
                HotRunnerList[i].SoakTime = soakTime;

                ushort svHigh = await _modbus!.ReadHoldingRegisterAsync(0x1008 + i);
                ushort svLow = await _modbus!.ReadHoldingRegisterAsync(0x1009 + i);
                HotRunnerList[i].SV = Math.Round(ParseFloat(svHigh, svLow, 0.1), 1);

                ushort slope = await _modbus!.ReadHoldingRegisterAsync(0x1970 + i);
                HotRunnerList[i].Slope = slope;
            }
            stepStart.Stop();
            // Debug.WriteLine($"[步骤11] 读取热流道参数耗时: {stepStart.ElapsedMilliseconds}ms");
        }

        private async Task PollInputAdjustmentsAsync()
        {
            var stepStart = Stopwatch.StartNew();
            for(int i = 0; i < 8; i++)
            {
                ushort offset = await _modbus!.ReadHoldingRegisterAsync(0x1020 + i);
                InputAdjList[i].Offset = (short)offset;

                ushort gain = await _modbus!.ReadHoldingRegisterAsync(0x19B8 + i);
                InputAdjList[i].Gain = (short)gain;
            }

            ushort filterCount = await _modbus!.ReadHoldingRegisterAsync(0x10F7);
            InputAdjList[0].FilterCount = filterCount;

            ushort frHigh = await _modbus!.ReadHoldingRegisterAsync(0x10F9);
            ushort frLow = await _modbus!.ReadHoldingRegisterAsync(0x10FA);
            InputAdjList[0].FilterRange = Math.Round(ParseFloat(frHigh, frLow, 0.1), 1);

            stepStart.Stop();
            // Debug.WriteLine($"[步骤8] 读取输入调整耗时: {stepStart.ElapsedMilliseconds}ms");
        }

        private async Task PollOutputConfigAsync()
        {
            var stepStart = Stopwatch.StartNew();
            for(int i = 0; i < 8; i++)
            {
                ushort out1Ctrl = await _modbus!.ReadHoldingRegisterAsync(0x10A8 + i);
                OutputList[i].Out1Function = out1Ctrl < OutFunctionNames.Length
                    ? OutFunctionNames[out1Ctrl] : $"未知({out1Ctrl})";

                ushort out2Ctrl = await _modbus!.ReadHoldingRegisterAsync(0x10B0 + i);
                OutputList[i].Out2Function = out2Ctrl < OutFunctionNames.Length
                    ? OutFunctionNames[out2Ctrl] : $"未知({out2Ctrl})";

                ushort outMax = await _modbus!.ReadHoldingRegisterAsync(0x1980 + i);
                OutputList[i].OutMax = outMax;

                ushort outMin = await _modbus!.ReadHoldingRegisterAsync(0x1988 + i);
                OutputList[i].OutMin = outMin;
            }
            stepStart.Stop();
            // Debug.WriteLine($"[步骤6] 读取输出配置耗时: {stepStart.ElapsedMilliseconds}ms");
        }

        private async Task PollPIDParametersAsync()
        {
            var stepStart = Stopwatch.StartNew();
            for(int i = 0; i < 8; i++)
            {
                ushort pbValue = await _modbus!.ReadHoldingRegisterAsync(0x1028 + i);
                PIDList[i].Pb = Math.Round(pbValue * 0.1, 1);

                ushort tiValue = await _modbus!.ReadHoldingRegisterAsync(0x1030 + i);
                PIDList[i].Ti = Math.Round(tiValue * 0.0, 0);

                ushort tdValue = await _modbus!.ReadHoldingRegisterAsync(0x1038 + i);
                PIDList[i].Td = Math.Round(tdValue * 0.0, 0);

                ushort intValue = await _modbus!.ReadHoldingRegisterAsync(0x1040 + i);
                PIDList[i].Integral = Math.Round(intValue * 0.1, 1);

                ushort o1Value = await _modbus!.ReadHoldingRegisterAsync(0x1070 + i);
                PIDList[i].Out1 = Math.Round(o1Value * 0.1, 1);

                ushort o2Value = await _modbus!.ReadHoldingRegisterAsync(0x1078 + i);
                PIDList[i].Out2 = Math.Round(o2Value * 0.1, 1);

                ushort ctrlMode = await _modbus!.ReadHoldingRegisterAsync(0x10B8 + i);
                PIDList[i].ControlMode = ctrlMode < ControlModeNames.Length
                    ? ControlModeNames[ctrlMode] : $"未知({ctrlMode})";

                ushort atStatus = await _modbus!.ReadHoldingRegisterAsync(0x10E0 + i);
                PIDList[i].ATEnabled = atStatus == 1;
            }
            stepStart.Stop();
            // Debug.WriteLine($"[步骤4] 读取PID参数耗时: {stepStart.ElapsedMilliseconds}ms");
        }

        // ===== 数据轮询方法 =====

        private async Task PollPVValuesAsync()
        {
            var stepStart = Stopwatch.StartNew();
            var pvs = await _modbus!.ReadAllPVAsync();
            for(int i = 0; i < 8 && i < TempCards.Count; i++)
            {
                TempCards[i].PV = Math.Round(pvs[i], 1);
                PVSVList[i].PV = Math.Round(pvs[i], 1);

                // 读取输出1量和输出2量
                ushort o1Value = await _modbus!.ReadHoldingRegisterAsync(0x1070 + i);
                PVSVList[i].Out1 = Math.Round(o1Value * 0.1, 1);

                ushort o2Value = await _modbus!.ReadHoldingRegisterAsync(0x1078 + i);
                PVSVList[i].Out2 = Math.Round(o2Value * 0.1, 1);
            }
            stepStart.Stop();
            // Debug.WriteLine($"[步骤1] 读取PV值耗时: {stepStart.ElapsedMilliseconds}ms");
        }

        private async Task PollSensorTypesAsync()
        {
            var stepStart = Stopwatch.StartNew();
            for(int i = 0; i < 8; i++)
            {
                ushort sensorVal = await _modbus!.ReadHoldingRegisterAsync(0x10A0 + i);
                string sensorName = sensorVal < SensorTypeNames.Length
                    ? SensorTypeNames[sensorVal]
                    : $"未知({sensorVal})";
                TempCards[i].InputType = sensorName;
                PVSVList[i].InputType = sensorName;
            }
            stepStart.Stop();
            // Debug.WriteLine($"[步骤3] 读取传感器类型耗时: {stepStart.ElapsedMilliseconds}ms");
        }

        private async Task PollSlopeSettingsAsync()
        {
            var stepStart = Stopwatch.StartNew();
            for(int i = 0; i < 8; i++)
            {
                ushort slopeVal = await _modbus!.ReadHoldingRegisterAsync(0x1970 + i);
                SlopeList[i].Slope = slopeVal;
            }
            stepStart.Stop();
            // Debug.WriteLine($"[步骤7] 读取斜率设定耗时: {stepStart.ElapsedMilliseconds}ms");
        }

        private async Task PollSVValuesAsync()
        {
            var stepStart = Stopwatch.StartNew();
            var svs = await _modbus!.ReadAllSVAsync();
            for(int i = 0; i < 8 && i < TempCards.Count; i++)
            {
                TempCards[i].SV = Math.Round(svs[i], 1);
                PVSVList[i].SV = Math.Round(svs[i], 1);
            }
            stepStart.Stop();
            // Debug.WriteLine($"[步骤2] 读取SV值耗时: {stepStart.ElapsedMilliseconds}ms");
        }

        private void RecordDataPoint()
        {
            if(!_isRecording)
            {
                return;
            }

            double elapsedSeconds = (DateTime.Now - _recordStartTime).TotalSeconds;
            double[] chValues = new double[8];
            double[] out1Values = new double[8];
            double[] out2Values = new double[8];
            for(int i = 0; i < 8; i++)
            {
                chValues[i] = TempCards[i].PV;
                out1Values[i] = PVSVList[i].Out1;
                out2Values[i] = PVSVList[i].Out2;
            }
            _recordedData.Add(new RecordedDataPoint(DateTime.Now, elapsedSeconds, chValues, out1Values, out2Values));
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

        private void StartRecord()
        {
            _isRecording = true;
            _recordStartTime = DateTime.Now;
            _recordedData.Clear();

            btnStartRecord.IsEnabled = false;
            btnStopRecord.IsEnabled = true;
            txtStatus.Text = "📝 正在记录数据...";
            txtStatus.Foreground = Brushes.Blue;
        }

        private void StopRecord()
        {
            _isRecording = false;
            btnStartRecord.IsEnabled = true;
            btnStopRecord.IsEnabled = false;

            if(_recordedData.Count > 0)
            {
                ExportToCsv();
            }
            else
            {
                txtStatus.Text = "没有记录的数据可导出";
                txtStatus.Foreground = Brushes.Gray;
            }
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

            _temperaturePlotModel.InvalidatePlot(true);
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

        private async Task UpdateLEDStatusAsync()
        {
            var stepStart = Stopwatch.StartNew();
            var leds = await _modbus!.ReadAllLEDStatusAsync();
            for(int i = 0; i < 8; i++)
            {
                ushort led = leds[i];
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
            stepStart.Stop();
            // Debug.WriteLine($"[步骤13] 更新LED状态耗时: {stepStart.ElapsedMilliseconds}ms");
        }

        private async Task UpdateStatusIndicatorsAsync()
        {
            var stepStart = Stopwatch.StartNew();

            var (hasError, errorCodes) = await _modbus!.CheckErrorsAsync();
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

            ledRUN.Fill = Brushes.LimeGreen;
            ledCOM.Fill = Brushes.Yellow;

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

            UpdateChart();
            stepStart.Stop();
            // Debug.WriteLine($"[步骤14] 更新状态指示耗时: {stepStart.ElapsedMilliseconds}ms");
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
                System.Diagnostics.Debug.WriteLine($"[Closing] 窗口关闭时断开连接异常: {ex.Message}");
            }
            finally
            {
                _modbus = null;
            }
            base.OnClosing(e);
        }

        public async Task DoWorkAsync(string key, Func<Task> action, string message)
        {
            var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            if(!await sem.WaitAsync(0))
            {
                return;
            }

            try
            {
                await action();
            }
            finally
            {
                sem.Release();
            }
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
