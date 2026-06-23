using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== CT 电流 ==========
    public class CTModel : INotifyPropertyChanged
    {
        private string _channel = string.Empty;
        private int _ctAdjust;
        private int _ctDynamic;
        private int _ctStatic;

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

        public int CTAdjust
        {
            get => _ctAdjust;
            set
            {
                _ctAdjust = value;
                OnPropertyChanged();
            }
        }

        public int CTDynamic
        {
            get => _ctDynamic;
            set
            {
                _ctDynamic = value;
                OnPropertyChanged();
            }
        }

        public int CTStatic
        {
            get => _ctStatic;
            set
            {
                _ctStatic = value;
                OnPropertyChanged();
            }
        }
    }
}
