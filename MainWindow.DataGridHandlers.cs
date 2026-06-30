using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DTE10T_WPF
{
    public partial class MainWindow
    {
        private async void DgAlarm_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                Logger.Warn("[Alarm] 请先连接设备");
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
                Logger.Error($"[Alarm] 写入失败: {ex.Message}", ex);
            }
        }

        private async void DgCT_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                Logger.Warn("[CT] 请先连接设备");
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
                return;
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
                Logger.Error($"[CT] 写入失败: {ex.Message}", ex);
            }
        }

        private async void DgEvent_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                Logger.Warn("[Event] 请先连接设备");
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
                Logger.Error($"[Event] 写入失败: {ex.Message}", ex);
            }
        }

        // ========== 功能选择参数 (三) 编辑处理 ==========
        private async void DgFunctionSelect_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                Logger.Warn("[FunctionSelect] 请先连接设备");
                return;
            }

            var model = e.Row.Item as FunctionSelectModel;
            if(model == null)
            {
                return;
            }

            int ch = GetChannelIndex(model.Channel);
            if(ch < 0 || ch >= 8)
            {
                return;
            }

            string columnName = (e.Column as DataGridTextColumn)?.Header?.ToString() ?? (e.Column as DataGridCheckBoxColumn)?.Header?.ToString() ?? (e.Column as DataGridComboBoxColumn)?.Header?.ToString() ?? string.Empty;

            try
            {
                switch(columnName)
                {
                    case "传感器类型":
                        int sensorIndex = Array.IndexOf(SensorTypeNames, model.SensorType);
                        if(sensorIndex >= 0)
                        {
                            await _modbus.WriteSensorTypeAsync(ch, sensorIndex);
                        }
                        break;
                    case "OUT1功能":
                        int out1Index = Array.IndexOf(OutFunctionNames, model.Out1Function);
                        if(out1Index >= 0)
                        {
                            await _modbus.SetOut1ControlAsync(ch, out1Index);
                        }
                        break;
                    case "SUB1功能":
                        int sub1Index = Array.IndexOf(OutFunctionNames, model.Sub1Function);
                        if(sub1Index >= 0)
                        {
                            await _modbus.SetOut2ControlAsync(ch, sub1Index);
                        }
                        break;
                    case "控制方式":
                        int modeIndex = Array.IndexOf(ControlModeNames, model.ControlMode);
                        if(modeIndex >= 0)
                        {
                            await _modbus.SetControlModeAsync(ch, modeIndex);
                        }
                        break;
                    case "警报一模式":
                        int alarm1Index = Array.IndexOf(AlarmModeNames, model.Alarm1Mode);
                        if(alarm1Index >= 0)
                        {
                            await _modbus.SetAlarm1ModeAsync(ch, alarm1Index);
                        }
                        break;
                    case "警报二模式":
                        int alarm2Index = Array.IndexOf(AlarmModeNames, model.Alarm2Mode);
                        if(alarm2Index >= 0)
                        {
                            await _modbus.SetAlarm2ModeAsync(ch, alarm2Index);
                        }
                        break;
                    case "控制周期(s)":
                        await _modbus.WriteControlCycleAsync(ch, model.ControlCycle);
                        break;
                    case "执行状态":
                        int execIndex = Array.IndexOf(ControlExecNames, model.ControlExecStatus);
                        if(execIndex >= 0)
                        {
                            await _modbus.SetControlExecAsync(ch, execIndex);
                        }
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
                    case "比例输出":
                        int propIndex = Array.IndexOf(ProportionSignNames, model.ProportionSign);
                        if(propIndex >= 0)
                        {
                            await _modbus.WriteProportionSignAsync(ch, propIndex);
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
                Logger.Error($"[FunctionSelect] 写入失败: {ex.Message}", ex);
            }
        }

        private async void DgHotRunner_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                Logger.Warn("[HotRunner] 请先连接设备");
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
                Logger.Error($"[HotRunner] 写入失败: {ex.Message}", ex);
            }
        }

        private async void DgInputAdj_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                Logger.Warn("[InputAdj] 请先连接设备");
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
                        if(ch == 0)
                        {
                            await _modbus.WriteFilterCountAsync(model.FilterCount);
                        }
                        break;
                    case "滤波范围":
                        if(ch == 0)
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
                Logger.Error($"[InputAdj] 写入失败: {ex.Message}", ex);
            }
        }

        private async void DgOutput_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                Logger.Warn("[Output] 请先连接设备");
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
                    case "控制周期 (s)":
                        await _modbus.WriteControlCycleAsync(ch, model.ControlCycle);
                        break;
                    default:
                        txtStatus.Text = $"未知列: {columnName}";
                        txtStatus.Foreground = Brushes.Red;
                        return;
                }
                txtStatus.Text = $"已写入 CH{ch + 1} {columnName}";
                txtStatus.Foreground = Brushes.Green;
            }
            catch(Exception ex)
            {
                txtStatus.Text = $"写入失败: {ex.Message}";
                txtStatus.Foreground = Brushes.Red;
                Logger.Error($"[Output] 写入失败: {ex.Message}", ex);
            }
        }

        private async void DgPID_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                Logger.Warn("[PID] 请先连接设备");
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

            string columnName = (e.Column as DataGridTextColumn)?.Header?.ToString() ?? (e.Column as DataGridCheckBoxColumn)?.Header?.ToString() ?? (e.Column as DataGridComboBoxColumn)?.Header?.ToString() ?? string.Empty;

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
                        if(!model.ATEnabled)
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
                Logger.Error($"[PID] 写入失败: {ex.Message}", ex);
            }
        }

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
                Logger.Error($"[Write] 写入失败: {ex.Message}", ex);
            }
        }

        private async void DgSlope_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if(!_isConnected || _modbus == null)
            {
                Logger.Warn("[Slope] 请先连接设备");
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
                Logger.Error($"[Slope] 写入失败: {ex.Message}", ex);
            }
        }
    }
}
