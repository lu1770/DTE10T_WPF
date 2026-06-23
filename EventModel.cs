using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== EVENT ==========
    public class EventModel : INotifyPropertyChanged
    {
        private string _channel = string.Empty;
        private string _eventFunction = "无功能";

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

        public string EventFunction
        {
            get => _eventFunction;
            set
            {
                _eventFunction = value;
                OnPropertyChanged();
            }
        }
    }
}
