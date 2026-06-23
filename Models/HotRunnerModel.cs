using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== 热流道 ==========
    public class HotRunnerModel : INotifyPropertyChanged
    {
        private string _channel = string.Empty;
        private int _fixedOutput;
        private int _limitTemp;
        private int _slope;
        private int _soakTime;
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

        public int FixedOutput
        {
            get => _fixedOutput;
            set
            {
                _fixedOutput = value;
                OnPropertyChanged();
            }
        }

        public int LimitTemp
        {
            get => _limitTemp;
            set
            {
                _limitTemp = value;
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

        public int SoakTime
        {
            get => _soakTime;
            set
            {
                _soakTime = value;
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
