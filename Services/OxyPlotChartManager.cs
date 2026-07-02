using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace DTE10T_WPF.Services
{
    public class OxyPlotChartManager
    {
        private const int MaxDataPoints = int.MaxValue;

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

        private LineSeries[]? _channelSeries;
        private LineSeries[]? _out1Series;
        private LineSeries[]? _out2Series;
        private PlotModel? _temperaturePlotModel;
        private LineSeries? _tempUpperLine;
        private LineSeries? _tempLowerLine;
        private DateTime _chartStartTime;
        private double _chartTimeOffset = 0;
        private bool _isChartPaused = false;
        private readonly List<double>[] _historyPVValues = new List<double>[8];

        private readonly Func<bool> _getShowOut1;
        private readonly Func<bool> _getShowOut2;
        private readonly Func<int, bool> _getChannelVisible;
        private readonly Func<int, double> _getPVValue;
        private readonly Func<int, double> _getOut1Value;
        private readonly Func<int, double> _getOut2Value;

        public PlotModel? TemperaturePlotModel => _temperaturePlotModel;

        public bool IsChartPaused => _isChartPaused;

        public OxyPlotChartManager(
            Func<bool> getShowOut1,
            Func<bool> getShowOut2,
            Func<int, bool> getChannelVisible,
            Func<int, double> getPVValue,
            Func<int, double> getOut1Value,
            Func<int, double> getOut2Value)
        {
            _getShowOut1 = getShowOut1;
            _getShowOut2 = getShowOut2;
            _getChannelVisible = getChannelVisible;
            _getPVValue = getPVValue;
            _getOut1Value = getOut1Value;
            _getOut2Value = getOut2Value;

            for(int i = 0; i < 8; i++)
            {
                _historyPVValues[i] = new List<double>();
            }
        }

        public void InitializeChart()
        {
            _temperaturePlotModel = new PlotModel
            {
                Title = "实时温度曲线",
                Background = OxyColors.White,
                PlotAreaBorderColor = OxyColors.LightGray,
                TitleFont = "微软雅黑",
                TitleFontSize = 14,
                PlotType = PlotType.XY,
                Legends = { new Legend
                            {
                                LegendPosition = LegendPosition.TopRight,
                                LegendFont = "微软雅黑",
                                LegendFontSize = 11
                            } }
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
                Maximum = 400,
                AbsoluteMinimum = -50,
                AbsoluteMaximum = 400,
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

            _tempUpperLine = new LineSeries
            {
                Title = "温度上界",
                Color = OxyColors.Red,
                StrokeThickness = 2,
                MarkerType = MarkerType.None,
                LineStyle = LineStyle.Dash,
                IsVisible = false
            };
            _temperaturePlotModel.Series.Add(_tempUpperLine);

            _tempLowerLine = new LineSeries
            {
                Title = "温度下界",
                Color = OxyColors.Blue,
                StrokeThickness = 2,
                MarkerType = MarkerType.None,
                LineStyle = LineStyle.Dash,
                IsVisible = false
            };
            _temperaturePlotModel.Series.Add(_tempLowerLine);

            _chartStartTime = DateTime.Now;
        }

        public void UpdateChart()
        {
            if(_isChartPaused || _channelSeries == null || _temperaturePlotModel == null)
            {
                return;
            }

            double currentTime = (DateTime.Now - _chartStartTime).TotalSeconds + _chartTimeOffset;
            bool showOut1 = _getShowOut1();
            bool showOut2 = _getShowOut2();

            for(int i = 0; i < 8; i++)
            {
                bool isVisible = _getChannelVisible(i);
                _channelSeries[i].IsVisible = isVisible;

                if(!isVisible)
                {
                    continue;
                }

                double pvValue = _getPVValue(i);
                _channelSeries[i].Points.Add(new DataPoint(currentTime, pvValue));

                if(_channelSeries[i].Points.Count > MaxDataPoints)
                {
                    _channelSeries[i].Points.RemoveAt(0);
                }

                if(_out1Series != null)
                {
                    _out1Series[i].IsVisible = showOut1 && isVisible;
                    double out1Value = _getOut1Value(i);
                    _out1Series[i].Points.Add(new DataPoint(currentTime, out1Value));

                    if(_out1Series[i].Points.Count > MaxDataPoints)
                    {
                        _out1Series[i].Points.RemoveAt(0);
                    }
                }

                if(_out2Series != null)
                {
                    _out2Series[i].IsVisible = showOut2 && isVisible;
                    double out2Value = _getOut2Value(i);
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

        public void UpdateTempRangeLines(double currentTime)
        {
            if(_tempUpperLine == null || _tempLowerLine == null)
            {
                return;
            }

            double minTime = currentTime - 60;
            if(minTime < 0)
            {
                minTime = 0;
            }
            double maxTime = currentTime + 5;

            if(_tempLowerLine.IsVisible && _tempLowerLine.Points.Count > 0)
            {
                double yValue = _tempLowerLine.Points[0].Y;
                _tempLowerLine.Points.Clear();
                _tempLowerLine.Points.Add(new DataPoint(minTime, yValue));
                _tempLowerLine.Points.Add(new DataPoint(maxTime, yValue));
            }

            if(_tempUpperLine.IsVisible && _tempUpperLine.Points.Count > 0)
            {
                double yValue = _tempUpperLine.Points[0].Y;
                _tempUpperLine.Points.Clear();
                _tempUpperLine.Points.Add(new DataPoint(minTime, yValue));
                _tempUpperLine.Points.Add(new DataPoint(maxTime, yValue));
            }
        }

        public void ClearChart()
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

            for(int i = 0; i < 8; i++)
            {
                _historyPVValues[i].Clear();
            }

            _chartStartTime = DateTime.Now;
            _chartTimeOffset = 0;

            if(_temperaturePlotModel != null)
            {
                _temperaturePlotModel.InvalidatePlot(true);
            }
        }

        public void ToggleChartPause()
        {
            if(_isChartPaused || _channelSeries == null || _temperaturePlotModel == null)
            {
                return;
            }

            double currentTime = (DateTime.Now - _chartStartTime).TotalSeconds + _chartTimeOffset;
            bool showOut1 = _getShowOut1();
            bool showOut2 = _getShowOut2();

            for(int i = 0; i < 8; i++)
            {
                bool isVisible = _getChannelVisible(i);
                _channelSeries[i].IsVisible = isVisible;

                if(!isVisible)
                {
                    continue;
                }

                double pvValue = _getPVValue(i);
                _channelSeries[i].Points.Add(new DataPoint(currentTime, pvValue));

                if(_channelSeries[i].Points.Count > MaxDataPoints)
                {
                    _channelSeries[i].Points.RemoveAt(0);
                }

                if(_out1Series != null)
                {
                    _out1Series[i].IsVisible = showOut1 && isVisible;
                    double out1Value = _getOut1Value(i);
                    _out1Series[i].Points.Add(new DataPoint(currentTime, out1Value));

                    if(_out1Series[i].Points.Count > MaxDataPoints)
                    {
                        _out1Series[i].Points.RemoveAt(0);
                    }
                }

                if(_out2Series != null)
                {
                    _out2Series[i].IsVisible = showOut2 && isVisible;
                    double out2Value = _getOut2Value(i);
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

        public void ApplyTempRange(double? lowerValue, double? upperValue)
        {
            if(_tempUpperLine == null || _tempLowerLine == null || _temperaturePlotModel == null)
            {
                return;
            }

            _tempLowerLine.Points.Clear();
            _tempUpperLine.Points.Clear();

            if(lowerValue.HasValue)
            {
                _tempLowerLine.Points.Add(new DataPoint(0, lowerValue.Value));
                _tempLowerLine.Points.Add(new DataPoint(60, lowerValue.Value));
                _tempLowerLine.IsVisible = true;
            }
            else
            {
                _tempLowerLine.IsVisible = false;
            }

            if(upperValue.HasValue)
            {
                _tempUpperLine.Points.Add(new DataPoint(0, upperValue.Value));
                _tempUpperLine.Points.Add(new DataPoint(60, upperValue.Value));
                _tempUpperLine.IsVisible = true;
            }
            else
            {
                _tempUpperLine.IsVisible = false;
            }

            _temperaturePlotModel.InvalidatePlot(true);
        }

        public void UpdateOut1Visibility()
        {
            if(_out1Series == null) return;

            bool showOut1 = _getShowOut1();
            for(int i = 0; i < 8; i++)
            {
                bool isVisible = _getChannelVisible(i);
                _out1Series[i].IsVisible = showOut1 && isVisible;
            }
        }

        public void UpdateOut2Visibility()
        {
            if(_out2Series == null) return;

            bool showOut2 = _getShowOut2();
            for(int i = 0; i < 8; i++)
            {
                bool isVisible = _getChannelVisible(i);
                _out2Series[i].IsVisible = showOut2 && isVisible;
            }
        }

        public async Task SaveImageAsync(FrameworkElement plotView)
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

                    await Task.Run(() => {
                        using(var stream = File.Create(filePath))
                        {
                            var exporter = new PngExporter
                            {
                                Width = (int)plotView.ActualWidth,
                                Height = (int)plotView.ActualHeight
                            };

                            exporter.Export(_temperaturePlotModel, stream);
                        }
                    });

                    MessageBox.Show($"图像已保存到:\n{filePath}", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);

                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"保存图像失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error($"[SaveImage] 保存失败: {ex.Message}", ex);
            }
        }
    }
}