using DTE10T_WPF.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DTE10T_WPF
{
    public partial class MainWindow
    {
        private async Task<bool> ApplyPollAllDataAsync()
        {
            if(_modbus == null)
            {
                return false;
            }
            var ws = Stopwatch.StartNew();
            try
            {
                int selectedTabIndex = mainTab.SelectedIndex;

                await PollPVValuesAsync();
                await PollSVValuesAsync();
                await PollSensorTypesAsync();
                await UpdateLEDStatusAsync();
                await UpdateStatusIndicatorsAsync();

                switch(selectedTabIndex)
                {
                    case 0:
                        break;
                    case 1:
                        break;
                    case 2:
                        await PollPIDParametersAsync();
                        break;
                    case 3:
                        await PollAlarmSettingsAsync();
                        break;
                    case 4:
                        await PollOutputConfigAsync();
                        break;
                    case 5:
                        await PollFunctionSelectAsync();
                        break;
                    case 6:
                        await PollSlopeSettingsAsync();
                        await PollInputAdjustmentsAsync();
                        await PollCTCurrentAsync();
                        await PollEventFunctionsAsync();
                        await PollHotRunnerParamsAsync();
                        break;
                    case 7:
                        await PollCommParamsAsync();
                        break;
                    case 8:
                        break;
                    default:
                        await PollPIDParametersAsync();
                        await PollAlarmSettingsAsync();
                        await PollOutputConfigAsync();
                        await PollFunctionSelectAsync();
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
                Logger.Error($"[Poll] 轮询异常: {ex.Message}", ex);
                ledCOM.Fill = Brushes.Red;
                txtStatus.Text = $"通讯异常: {ex.Message}";
                txtStatus.Foreground = Brushes.Red;
            }
            ws.Stop();
            return true;
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e) { await ConnectAsync(); }

        private async void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _isConnected = false;
            _pollTimer?.Dispose();
            _pollTimer = null;

            try
            {
                await Task.Run(() => _modbus?.Disconnect());
            }
            catch(Exception ex)
            {
                Logger.Error($"[Disconnect] 断开连接异常: {ex.Message}", ex);
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
                    Logger.Error($"[Refresh] 刷新数据异常: {ex.Message}", ex);
                }
                finally
                {
                    btnRefresh.IsEnabled = true;
                }
            }
        }

        private void CalculateStatistics(int channelIndex, double currentPV)
        {
            var history = _historyPVValues[channelIndex];
            if(history.Count < 2)
            {
                TempCards[channelIndex].Mean = currentPV;
                TempCards[channelIndex].StdDev = 0;
                TempCards[channelIndex].ZScore = 0;
                TempCards[channelIndex].Probability = 0.5;
                return;
            }

            double mean = history.Average();
            double variance = history.Sum(v => Math.Pow(v - mean, 2)) / (history.Count - 1);
            double stdDev = Math.Sqrt(variance);

            double zScore = stdDev > 0.001 ? (currentPV - mean) / stdDev : 0;
            double probability = NormalDistributionCDF(zScore);

            TempCards[channelIndex].Mean = Math.Round(mean, 1);
            TempCards[channelIndex].StdDev = Math.Round(stdDev, 2);
            TempCards[channelIndex].ZScore = Math.Round(zScore, 2);
            TempCards[channelIndex].Probability = Math.Round(probability, 4);
        }

        private async Task ConnectAsync()
        {
            btnConnect.IsEnabled = false;
            txtStatus.Text = "正在连接...";
            txtStatus.Foreground = Brushes.Orange;
            ledCOM.Fill = Brushes.Yellow;

            try
            {
                string port = (cmbComPort.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "COM1";
                int baudRate = int.Parse(((cmbBaudRate.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "9600"));
                int slaveId = int.Parse(txtStationCode.Text);

                string parity = "Even";
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
                    txtDeviceInfo.Text = $"| 温控器1 | SlaveID: {slaveId} | {port} | {protocol} | {baud}bps";

                    ledPWR.Fill = Brushes.LimeGreen;
                    ledCOM.Fill = Brushes.Green;

                    await PollAllDataAsync();

                    var ms = 1000;
                    _pollTimer = new System.Threading.Timer(PollCallback, null, ms, ms);
                }
                else
                {
                    txtStatus.Text = "连接失败: 无法建立连接，请检查串口设置";
                    txtStatus.Foreground = Brushes.Red;
                    btnConnect.IsEnabled = true;
                    ledCOM.Fill = Brushes.Red;
                    Logger.Error("[Connect] 无法连接到温控器，请检查: COM端口、串口占用、波特率/站号");
                }
            }
            catch(Exception ex)
            {
                string errorMessage = GetConnectionErrorMessage(ex);
                txtStatus.Text = $"连接失败: {errorMessage}";
                txtStatus.Foreground = Brushes.Red;
                btnConnect.IsEnabled = true;
                ledCOM.Fill = Brushes.Red;
                Logger.Error($"[Connect] 连接异常: {ex.Message}", ex);
            }
        }

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

        private double NormalDistributionCDF(double z)
        {
            double t = 1.0 / (1.0 + 0.2316419 * Math.Abs(z));
            double d = 0.3989423 * Math.Exp(-z * z / 2.0);
            double prob = d * t * (0.3193815 + t * (-0.3565638 + t * (1.781478 + t * (-1.821256 + t * 1.330274))));

            return z >= 0 ? 1.0 - prob : prob;
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
        }

        private async Task PollAllDataAsync()
        { await DoWorkAsync(nameof(ApplyPollAllDataAsync), async () => await ApplyPollAllDataAsync(), "轮询数据中..."); }

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
                Logger.Error($"[PollCallback] 定时轮询异常: {ex.Message}", ex);
            }
        }

        private async Task PollCommParamsAsync()
        {
            var stepStart = Stopwatch.StartNew();
            var commParams = await _modbus!.ReadCommParamsAsync();
            UpdateCommParamsUI(commParams);
            stepStart.Stop();
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
        }

        // ========== 功能选择参数 (三) 轮询 ==========
        private async Task PollFunctionSelectAsync()
        {
            var stepStart = Stopwatch.StartNew();
            for(int i = 0; i < 8; i++)
            {
                // 输入传感器类型 H10A0~H10A7
                ushort sensorVal = await _modbus!.ReadHoldingRegisterAsync(0x10A0 + i);
                FunctionSelectList[i].SensorType = sensorVal < SensorTypeNames.Length
                    ? SensorTypeNames[sensorVal] : $"未知({sensorVal})";

                // OUT1 输出功能 H10A8~H10AF
                ushort out1Ctrl = await _modbus!.ReadHoldingRegisterAsync(0x10A8 + i);
                FunctionSelectList[i].Out1Function = out1Ctrl < OutFunctionNames.Length
                    ? OutFunctionNames[out1Ctrl] : $"未知({out1Ctrl})";

                // SUB1 输出功能 H10B0~H10B7 (相当于 OUT2)
                ushort sub1Ctrl = await _modbus!.ReadHoldingRegisterAsync(0x10B0 + i);
                FunctionSelectList[i].Sub1Function = sub1Ctrl < OutFunctionNames.Length
                    ? OutFunctionNames[sub1Ctrl] : $"未知({sub1Ctrl})";

                // 控制方式 H10B8~H10BF
                ushort ctrlMode = await _modbus!.ReadHoldingRegisterAsync(0x10B8 + i);
                FunctionSelectList[i].ControlMode = ctrlMode < ControlModeNames.Length
                    ? ControlModeNames[ctrlMode] : $"未知({ctrlMode})";

                // 警报一输出模式 H10C0~H10C7
                ushort alarm1Mode = await _modbus!.ReadHoldingRegisterAsync(0x10C0 + i);
                FunctionSelectList[i].Alarm1Mode = alarm1Mode < AlarmModeNames.Length
                    ? AlarmModeNames[alarm1Mode] : $"未知({alarm1Mode})";

                // 警报二输出模式 H10C8~H10CF
                ushort alarm2Mode = await _modbus!.ReadHoldingRegisterAsync(0x10C8 + i);
                FunctionSelectList[i].Alarm2Mode = alarm2Mode < AlarmModeNames.Length
                    ? AlarmModeNames[alarm2Mode] : $"未知({alarm2Mode})";

                // 加热/冷却控制周期 H10D0~H10D7
                ushort ctrlCycle = await _modbus!.ReadHoldingRegisterAsync(0x10D0 + i);
                FunctionSelectList[i].ControlCycle = ctrlCycle;

                // 控制执行/停止设定 H10D8~H10DF
                ushort ctrlExec = await _modbus!.ReadHoldingRegisterAsync(0x10D8 + i);
                FunctionSelectList[i].ControlExecStatus = ctrlExec < ControlExecNames.Length
                    ? ControlExecNames[ctrlExec] : $"未知({ctrlExec})";

                // PID 自动调谐状态 H10E0~H10E7
                ushort atStatus = await _modbus!.ReadHoldingRegisterAsync(0x10E0 + i);
                FunctionSelectList[i].ATEnabled = atStatus == 1;

                // 设定正负比例输出 H10E8~H10EF
                ushort propSign = await _modbus!.ReadHoldingRegisterAsync(0x10E8 + i);
                FunctionSelectList[i].ProportionSign = propSign < ProportionSignNames.Length
                    ? ProportionSignNames[propSign] : $"未知({propSign})";
            }
            stepStart.Stop();
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

                ushort ctrlCycle = await _modbus!.ReadHoldingRegisterAsync(0x10D0 + i);
                int controlCycle = Convert.ToInt32(ctrlCycle);
                OutputList[i].ControlCycle = controlCycle;
            }
            stepStart.Stop();
        }

        private async Task PollPIDParametersAsync()
        {
            var stepStart = Stopwatch.StartNew();
            for(int i = 0; i < 8; i++)
            {
                ushort pbValue = await _modbus!.ReadHoldingRegisterAsync(0x1028 + i);
                double newPb = Math.Round(pbValue * 0.1, 1);
                if(PIDList[i].Pb != newPb)
                {
                    PIDList[i].Pb = newPb;
                }

                ushort tiValue = await _modbus!.ReadHoldingRegisterAsync(0x1030 + i);
                var newTi = _modbus!.ParseInt(tiValue, 1);
                if(PIDList[i].Ti != newTi)
                {
                    PIDList[i].Ti = newTi;
                }

                ushort tdValue = await _modbus!.ReadHoldingRegisterAsync(0x1038 + i);
                var newTd = _modbus!.ParseInt(tdValue, 1);
                if(PIDList[i].Td != newTd)
                {
                    PIDList[i].Td = newTd;
                }

                ushort intValue = await _modbus!.ReadHoldingRegisterAsync(0x1040 + i);
                double newIntegral = Math.Round(intValue * 0.1, 1);
                if(PIDList[i].Integral != newIntegral)
                {
                    PIDList[i].Integral = newIntegral;
                }

                ushort o1Value = await _modbus!.ReadHoldingRegisterAsync(0x1070 + i);
                double newOut1 = Math.Round(o1Value * 0.1, 1);
                if(PIDList[i].Out1 != newOut1)
                {
                    PIDList[i].Out1 = newOut1;
                }

                ushort o2Value = await _modbus!.ReadHoldingRegisterAsync(0x1078 + i);
                double newOut2 = Math.Round(o2Value * 0.1, 1);
                if(PIDList[i].Out2 != newOut2)
                {
                    PIDList[i].Out2 = newOut2;
                }

                ushort ctrlMode = await _modbus!.ReadHoldingRegisterAsync(0x10B8 + i);
                string newControlMode = ctrlMode < ControlModeNames.Length
                    ? ControlModeNames[ctrlMode] : $"未知({ctrlMode})";
                if(PIDList[i].ControlMode != newControlMode)
                {
                    PIDList[i].ControlMode = newControlMode;
                }

                ushort atStatus = await _modbus!.ReadHoldingRegisterAsync(0x10E0 + i);
                bool newATEnabled = atStatus == 1;
                if(PIDList[i].ATEnabled != newATEnabled)
                {
                    PIDList[i].ATEnabled = newATEnabled;
                }
            }
            stepStart.Stop();
        }


        private async Task PollPVValuesAsync()
        {
            var stepStart = Stopwatch.StartNew();
            var pvs = await _modbus!.ReadAllPVAsync();
            DateTime now = DateTime.Now;
            for(int i = 0; i < 8 && i < TempCards.Count; i++)
            {
                double rawPV = pvs[i];
                double currentPV = Math.Round(rawPV, 1);
                TempCards[i].PV = currentPV;
                PVSVList[i].PV = currentPV;

                _historyPVValues[i].Add(rawPV);
                if(_historyPVValues[i].Count > _maxHistoryPoints)
                {
                    _historyPVValues[i].RemoveAt(0);
                }

                CalculateStatistics(i, rawPV);

                if(_lastPVTime[i] != DateTime.MinValue)
                {
                    TimeSpan timeDiff = now - _lastPVTime[i];

                    if(timeDiff.TotalSeconds >= RateCalculationSeconds)
                    {
                        double tempDiff = rawPV - _lastPVValues[i];
                        double ratePerMinute = (tempDiff / timeDiff.TotalMilliseconds) * 60 * 1000;
                        //Logger.Debug($"[步骤2] 温度变化速率: {ratePerMinute}℃/min ({tempDiff}℃ / {timeDiff.TotalMilliseconds}ms)");
                        TempCards[i].RateOfChange = Math.Round(ratePerMinute, 1);

                        _lastPVValues[i] = rawPV;
                        _lastPVTime[i] = now;
                    }
                }
                else
                {
                    _lastPVValues[i] = rawPV;
                    _lastPVTime[i] = now;
                    TempCards[i].RateOfChange = 0;
                }

                ushort o1Value = await _modbus!.ReadHoldingRegisterAsync(0x1070 + i);
                PVSVList[i].Out1 = Math.Round(o1Value * 0.1, 1);

                ushort o2Value = await _modbus!.ReadHoldingRegisterAsync(0x1078 + i);
                PVSVList[i].Out2 = Math.Round(o2Value * 0.1, 1);
            }
            stepStart.Stop();
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
        }

        private async Task ReconnectAsync()
        {
            if(!_isConnected)
            {
                return;
            }

            txtStatus.Text = "配置变更，正在重连...";
            txtStatus.Foreground = Brushes.Orange;
            ledCOM.Fill = Brushes.Yellow;

            bool wasConnected = _isConnected;

            try
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
                    Logger.Error($"[Reconnect] 断开连接异常: {ex.Message}", ex);
                }
                finally
                {
                    _modbus = null;
                }

                string port = (cmbComPort.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "COM1";
                int baudRate = int.Parse(((cmbBaudRate.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "9600"));
                int slaveId = int.Parse(txtStationCode.Text);
                string parity = "Even";
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
                    txtDeviceInfo.Text = $"| 温控器1 | SlaveID: {slaveId} | {port} | {protocol} | {baud}bps";

                    ledPWR.Fill = Brushes.LimeGreen;
                    ledCOM.Fill = Brushes.Green;

                    await PollAllDataAsync();

                    var ms = 1000;
                    _pollTimer = new System.Threading.Timer(PollCallback, null, ms, ms);
                }
                else
                {
                    txtStatus.Text = "重连失败: 无法建立连接，请检查串口设置";
                    txtStatus.Foreground = Brushes.Red;
                    btnConnect.IsEnabled = true;
                    btnDisconnect.IsEnabled = false;
                    btnRefresh.IsEnabled = false;
                    ledCOM.Fill = Brushes.Red;
                    Logger.Error("[Reconnect] 无法重新连接到温控器");
                }
            }
            catch(Exception ex)
            {
                string errorMessage = GetConnectionErrorMessage(ex);
                txtStatus.Text = $"重连失败: {errorMessage}";
                txtStatus.Foreground = Brushes.Red;
                btnConnect.IsEnabled = true;
                btnDisconnect.IsEnabled = false;
                btnRefresh.IsEnabled = false;
                ledCOM.Fill = Brushes.Red;
                Logger.Error($"[Reconnect] 重连异常: {ex.Message}", ex);
            }
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
    }
}
