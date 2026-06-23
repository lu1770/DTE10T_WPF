using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== PID 参数 ==========
    public class PIDModel : INotifyPropertyChanged
    {
        private bool _atEnabled;
        private string _channel = string.Empty;
        private string _controlMode = "PID";
        private double _integral;
        private double _out1;
        private double _out2;
        private double _pb;
        private double _td;
        private double _ti;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));

        public bool ATEnabled
        {
            get => _atEnabled;
            set
            {
                _atEnabled = value;
                OnPropertyChanged();
            }
        }

        public string Channel
        {
            get => _channel;
            set
            {
                _channel = value;
                OnPropertyChanged();
            }
        }

        public string ControlMode
        {
            get => _controlMode;
            set
            {
                _controlMode = value;
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

        public double Out1
        {
            get => _out1;
            set
            {
                _out1 = value;
                OnPropertyChanged();
            }
        }

        public double Out2
        {
            get => _out2;
            set
            {
                _out2 = value;
                OnPropertyChanged();
            }
        }

        public double Pb
        {
            get => _pb;
            set
            {
                _pb = value;
                OnPropertyChanged();
            }
        }

        public double Td
        {
            get => _td;
            set
            {
                _td = value;
                OnPropertyChanged();
            }
        }

        public double Ti
        {
            get => _ti;
            set
            {
                _ti = value;
                OnPropertyChanged();
            }
        }
    }
}
