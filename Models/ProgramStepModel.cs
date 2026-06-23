using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== 可程控 - 步骤 ==========
    public class ProgramStepModel : INotifyPropertyChanged
    {
        private string _pattern = string.Empty;
        private int _runTime;
        private string _step = string.Empty;
        private double _targetTemp;
        private string _timeUnit = "分";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));

        public string Pattern
        {
            get => _pattern;
            set
            {
                _pattern = value;
                OnPropertyChanged();
            }
        }

        public int RunTime
        {
            get => _runTime;
            set
            {
                _runTime = value;
                OnPropertyChanged();
            }
        }

        public string Step
        {
            get => _step;
            set
            {
                _step = value;
                OnPropertyChanged();
            }
        }

        public double TargetTemp
        {
            get => _targetTemp;
            set
            {
                _targetTemp = value;
                OnPropertyChanged();
            }
        }

        public string TimeUnit
        {
            get => _timeUnit;
            set
            {
                _timeUnit = value;
                OnPropertyChanged();
            }
        }
    }
}
