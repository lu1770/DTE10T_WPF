using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== 通讯参数 ==========
    public class CommParamModel : INotifyPropertyChanged
    {
        private string _paramDesc = string.Empty;
        private string _paramName = string.Empty;
        private string _paramValue = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));

        public string ParamDesc
        {
            get => _paramDesc;
            set
            {
                _paramDesc = value;
                OnPropertyChanged();
            }
        }

        public string ParamName
        {
            get => _paramName;
            set
            {
                _paramName = value;
                OnPropertyChanged();
            }
        }

        public string ParamValue
        {
            get => _paramValue;
            set
            {
                _paramValue = value;
                OnPropertyChanged();
            }
        }
    }
}
