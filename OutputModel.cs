using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== 输出配置 ==========
    public class OutputModel : INotifyPropertyChanged
    {
        private string _channel = string.Empty;
        private int _controlCycle = 1;
        private string _out1Function = "加热";
        private string _out2Function = "警报";
        private int _outMax = 100;
        private int _outMin = 0;
        private bool _outputReverse;

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

        public int ControlCycle
        {
            get => _controlCycle;
            set
            {
                _controlCycle = value;
                OnPropertyChanged();
            }
        }

        public string Out1Function
        {
            get => _out1Function;
            set
            {
                _out1Function = value;
                OnPropertyChanged();
            }
        }

        public string Out2Function
        {
            get => _out2Function;
            set
            {
                _out2Function = value;
                OnPropertyChanged();
            }
        }

        public int OutMax
        {
            get => _outMax;
            set
            {
                _outMax = value;
                OnPropertyChanged();
            }
        }

        public int OutMin
        {
            get => _outMin;
            set
            {
                _outMin = value;
                OnPropertyChanged();
            }
        }

        public bool OutputReverse
        {
            get => _outputReverse;
            set
            {
                _outputReverse = value;
                OnPropertyChanged();
            }
        }
    }
}
