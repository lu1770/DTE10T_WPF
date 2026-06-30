using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace DTE10T_WPF
{
    public partial class MainWindow
    {
        private async void BtnStartRecord_Click(object sender, RoutedEventArgs e) { await StartRecordAsync(); }

        private async void BtnStopRecord_Click(object sender, RoutedEventArgs e) { await StopRecordAsync(); }

        private async Task ExportToCsvAsync()
        {
            try
            {
                string exportFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "DTE10T_WPF",
                    "Exports"
                );

                if(!Directory.Exists(exportFolder))
                {
                    Directory.CreateDirectory(exportFolder);
                }

                string filePath = Path.Combine(exportFolder, $"温度记录_{_recordStartTime:yyyyMMdd_HHmmss}.csv");

                await Task.Run(() => {
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
                });
                Application.Current.Dispatcher.Invoke(() => {
                    txtStatus.Text = $"✅ 已导出 {_recordedData.Count} 条数据";
                    txtStatus.Foreground = Brushes.Green;
                    MessageBox.Show($"数据已导出到:\n{filePath}", "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                });
            }
            catch(Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error($"[ExportCSV] 导出失败: {ex.Message}", ex);
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

                Application.Current.Dispatcher.Invoke(() => {
                    Logger.Info($"[TempCSV] 已保存临时文件: {tempFilePath}, 数据条数: {AutoSaveInterval}");
                    txtStatus.Text = $"📝 已记录 {_recordedData.Count} 条数据 (临时文件 #{_tempFileCounter})";
                });
            }
            catch(Exception ex)
            {
                Logger.Error($"[TempCSV] 保存临时文件失败: {ex.Message}", ex);
            }
        }

        private async Task StartRecordAsync()
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

            await Task.Run(() => {
                if(!Directory.Exists(_tempFolder))
                {
                    Directory.CreateDirectory(_tempFolder);
                }
            });

            btnStartRecord.IsEnabled = false;
            Application.Current.Dispatcher.Invoke(() => {
                btnStopRecord.IsEnabled = true;
                txtStatus.Text = "📝 正在记录数据...";
                txtStatus.Foreground = Brushes.Blue;
            });
        }

        private async Task StopRecordAsync()
        {
            _isRecording = false;
            btnStartRecord.IsEnabled = true;
            btnStopRecord.IsEnabled = false;

            if(_recordedData.Count > 0)
            {
                await ExportToCsvAsync();
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() => {
                    txtStatus.Text = "没有记录的数据可导出";
                    txtStatus.Foreground = Brushes.Gray;
                }); 
            }
        }

        private async Task MergeTempCsvFilesAsync()
        {
            try
            {
                if(!Directory.Exists(_tempFolder))
                {
                    return;
                }

                string[] tempFiles = Directory.GetFiles(_tempFolder, "temp_*.csv")
                    .OrderBy(f => f).ToArray();

                if(tempFiles.Length == 0)
                {
                    return;
                }

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string mergedFilePath = Path.Combine(desktopPath, $"温度记录_{_recordStartTime:yyyyMMdd_HHmmss}.csv");

                await Task.Run(() => {
                    bool headerWritten = false;
                    using(var writer = new StreamWriter(mergedFilePath, false, System.Text.Encoding.UTF8))
                    {
                        foreach(string tempFile in tempFiles)
                        {
                            using(var reader = new StreamReader(tempFile, System.Text.Encoding.UTF8))
                            {
                                string line;
                                bool isFirstLine = true;
                                while((line = reader.ReadLine()) != null)
                                {
                                    if(isFirstLine)
                                    {
                                        isFirstLine = false;
                                        if(!headerWritten)
                                        {
                                            writer.WriteLine(line);
                                            headerWritten = true;
                                        }
                                        continue;
                                    }
                                    writer.WriteLine(line);
                                }
                            }
                        }
                    }
                });
                Application.Current.Dispatcher.Invoke(() => {
                    Logger.Info($"[MergeCSV] 已合并 {tempFiles.Length} 个临时文件到桌面: {mergedFilePath}");
                });
            }
            catch(Exception ex)
            {
                Logger.Error($"[MergeCSV] 合并临时文件失败: {ex.Message}", ex);
            }
        }
    }
}
