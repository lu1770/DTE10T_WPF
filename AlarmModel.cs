using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== 警报设定 ==========
    public class AlarmModel : INotifyPropertyChanged
    {
        private int _alarmDelay;
        private bool _alarmEnabled;
        private double _alarmHigh;
        private double _alarmLow;
        private string _alarmMode = "无警报";
        private string _channel = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));

        public int AlarmDelay
        {
            get => _alarmDelay;
            set
            {
                _alarmDelay = value;
                OnPropertyChanged();
            }
        }

        public bool AlarmEnabled
        {
            get => _alarmEnabled;
            set
            {
                _alarmEnabled = value;
                OnPropertyChanged();
            }
        }

        public double AlarmHigh
        {
            get => _alarmHigh;
            set
            {
                _alarmHigh = value;
                OnPropertyChanged();
            }
        }

        public double AlarmLow
        {
            get => _alarmLow;
            set
            {
                _alarmLow = value;
                OnPropertyChanged();
            }
        }

        public string AlarmMode
        {
            get => _alarmMode;
            set
            {
                _alarmMode = value;
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
    }
}
