using OxyPlot;
using OxyPlot.SkiaSharp;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DTE10T_WPF
{
    public partial class MainWindow
    {
        private async void BtnApplyTempRange_Click(object sender, RoutedEventArgs e)
        {
            await Task.Yield();

            double? lowerValue = null;
            double? upperValue = null;

            if(!string.IsNullOrEmpty(txtTempLower.Text) && double.TryParse(txtTempLower.Text, out double lower))
            {
                lowerValue = lower;
            }

            if(!string.IsNullOrEmpty(txtTempUpper.Text) && double.TryParse(txtTempUpper.Text, out double upper))
            {
                upperValue = upper;
            }

            _chartManager?.ApplyTempRange(lowerValue, upperValue);
        }

        private void BtnClearChart_Click(object sender, RoutedEventArgs e) { ClearChart(); }

        private void BtnPauseChart_Click(object sender, RoutedEventArgs e) { ToggleChartPause(); }

        private async void BtnSaveImage_Click(object sender, RoutedEventArgs e)
        {
            await _chartManager?.SaveImageAsync(pvTemperature)!;
        }

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
            _chartManager?.UpdateOut1Visibility();
        }

        private void ChkShowOut2_CheckedChanged(object sender, RoutedEventArgs e)
        {
            _chartManager?.UpdateOut2Visibility();
        }

        private void ClearChart()
        {
            _chartManager?.ClearChart();
        }

        private void UpdateChart()
        {
            _chartManager?.UpdateChart();
        }
    }
}
