using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== PV/SV 设定 ==========
    public class PVSVModel : INotifyPropertyChanged
    {
        private string _channel = string.Empty;
        private string _inputType = string.Empty;
        private bool _isEnabled = true;
        private double _pv;
        private double _rangeHigh;
        private double _rangeLow;
        private double _sv;

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

        public string InputType
        {
            get => _inputType;
            set
            {
                _inputType = value;
                OnPropertyChanged();
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged();
            }
        }

        public double PV
        {
            get => _pv;
            set
            {
                _pv = value;
                OnPropertyChanged();
            }
        }

        public double RangeHigh
        {
            get => _rangeHigh;
            set
            {
                _rangeHigh = value;
                OnPropertyChanged();
            }
        }

        public double RangeLow
        {
            get => _rangeLow;
            set
            {
                _rangeLow = value;
                OnPropertyChanged();
            }
        }

        public double SV
        {
            get => _sv;
            set
            {
                _sv = value;
                OnPropertyChanged();
            }
        }
    }
}
