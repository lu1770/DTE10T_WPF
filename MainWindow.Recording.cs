using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace DTE10T_WPF
{
    public partial class MainWindow
    {
        private void BtnStartRecord_Click(object sender, RoutedEventArgs e) { StartRecord(); }

        private void BtnStopRecord_Click(object sender, RoutedEventArgs e) { StopRecord(); }

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

            if(_recordedData.Count % AutoSaveInterval == 0)
            {
                SaveTempCsvFile();
            }
        }

        private void SaveTempCsvFile()
        {
            try
            {
                _tempFileCounter++;
                string tempFilePath = Path.Combine(_tempFolder, $"temp_{_tempFileCounter:D4}.csv");

                using(var writer = new StreamWriter(tempFilePath, false, System.Text.Encoding.UTF8))
                {
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

                    int startIndex = Math.Max(0, _recordedData.Count - AutoSaveInterval);
                    for(int idx = startIndex; idx < _recordedData.Count; idx++)
                    {
                        var point = _recordedData[idx];
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

                System.Diagnostics.Debug.WriteLine($"[TempCSV] 已保存临时文件: {tempFilePath}, 数据条数: {AutoSaveInterval}");
                txtStatus.Text = $"📝 已记录 {_recordedData.Count} 条数据 (临时文件 #{_tempFileCounter})";
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TempCSV] 保存临时文件失败: {ex.Message}");
            }
        }

        private void StartRecord()
        {
            _isRecording = true;
            _recordStartTime = DateTime.Now;
            _recordedData.Clear();
            _tempFileCounter = 0;

            _tempFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DTE10T_WPF",
                "TempRecords",
                _recordStartTime.ToString("yyyyMMdd_HHmmss")
            );
            if(!Directory.Exists(_tempFolder))
            {
                Directory.CreateDirectory(_tempFolder);
            }

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
    }
}
