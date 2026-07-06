using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    public class PointRWModel : INotifyPropertyChanged
    {
        private string _channel = string.Empty;
        private double _integral;
        private double _propComp;
        private double _outRatio;
        private double _overlapTemp;
        private double _sensitivity1;
        private double _sensitivity2;
        private double _anOutUpper;
        private double _anOutLower;
        private int _timeUnit;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));

        public string Channel
        {
            get => _channel;
            set
            {
                _channel = value;
                OnPropertyChanged();
            }
        }

        public double Integral
        {
            get => _integral;
            set
            {
                _integral = value;
                OnPropertyChanged();
            }
        }

        public double PropComp
        {
            get => _propComp;
            set
            {
                _propComp = value;
                OnPropertyChanged();
            }
        }

        public double OutRatio
        {
            get => _outRatio;
            set
            {
                _outRatio = value;
                OnPropertyChanged();
            }
        }

        public double OverlapTemp
        {
            get => _overlapTemp;
            set
            {
                _overlapTemp = value;
                OnPropertyChanged();
            }
        }

        public double Sensitivity1
        {
            get => _sensitivity1;
            set
            {
                _sensitivity1 = value;
                OnPropertyChanged();
            }
        }

        public double Sensitivity2
        {
            get => _sensitivity2;
            set
            {
                _sensitivity2 = value;
                OnPropertyChanged();
            }
        }

        public double AnOutUpper
        {
            get => _anOutUpper;
            set
            {
                _anOutUpper = value;
                OnPropertyChanged();
            }
        }

        public double AnOutLower
        {
            get => _anOutLower;
            set
            {
                _anOutLower = value;
                OnPropertyChanged();
            }
        }

        public int TimeUnit
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