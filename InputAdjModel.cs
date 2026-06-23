using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== 输入调整 ==========
    public class InputAdjModel : INotifyPropertyChanged
    {
        private string _channel = string.Empty;
        private int _filterCount = 8;
        private double _filterRange = 1.0;
        private int _gain;
        private int _offset;

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

        public int FilterCount
        {
            get => _filterCount;
            set
            {
                _filterCount = value;
                OnPropertyChanged();
            }
        }

        public double FilterRange
        {
            get => _filterRange;
            set
            {
                _filterRange = value;
                OnPropertyChanged();
            }
        }

        public int Gain
        {
            get => _gain;
            set
            {
                _gain = value;
                OnPropertyChanged();
            }
        }

        public int Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                OnPropertyChanged();
            }
        }
    }
}
