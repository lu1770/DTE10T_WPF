using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== 斜率控制 ==========
    public class SlopeModel : INotifyPropertyChanged
    {
        private string _channel = string.Empty;
        private int _slope;
        private bool _slopeEnabled;
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

        public int Slope
        {
            get => _slope;
            set
            {
                _slope = value;
                OnPropertyChanged();
            }
        }

        public bool SlopeEnabled
        {
            get => _slopeEnabled;
            set
            {
                _slopeEnabled = value;
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
