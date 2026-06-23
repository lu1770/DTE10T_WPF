using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== 实时温度卡片 ==========
    public class TempCardModel : INotifyPropertyChanged
    {
        private string _bgColor = "#FFFFFF";
        private string _channel = string.Empty;
        private string _inputType = string.Empty;
        private double _pv;
        private string _status = string.Empty;
        private double _sv;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));

        public string BgColor
        {
            get => _bgColor;
            set
            {
                _bgColor = value;
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

        public string InputType
        {
            get => _inputType;
            set
            {
                _inputType = value;
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

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
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
