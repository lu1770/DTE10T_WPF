using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== 功能选择参数 (三) ==========
    // 包含: 输入传感器类型、OUT1/OUT2输出功能、SUB1/SUB2输出功能、控制方式
    //       警报一/二输出模式、加热/冷却控制周期、控制执行/停止、AT自整定状态、正负比例输出设定
    public class FunctionSelectModel : INotifyPropertyChanged
    {
        private string _channel = string.Empty;
        private string _sensorType = "K型热电偶";
        private string _out1Function = "加热(逆向)";
        private string _sub1Function = "加热(逆向)";
        private string _controlMode = "PID";
        private string _alarm1Mode = "无警报";
        private string _alarm2Mode = "无警报";
        private int _controlCycle = 1;
        private string _controlExecStatus = "停止";
        private bool _atEnabled = false;
        private string _proportionSign = "正";

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

        public string SensorType
        {
            get => _sensorType;
            set
            {
                _sensorType = value;
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

        public string Sub1Function
        {
            get => _sub1Function;
            set
            {
                _sub1Function = value;
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

        public string Alarm1Mode
        {
            get => _alarm1Mode;
            set
            {
                _alarm1Mode = value;
                OnPropertyChanged();
            }
        }

        public string Alarm2Mode
        {
            get => _alarm2Mode;
            set
            {
                _alarm2Mode = value;
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

        public string ControlExecStatus
        {
            get => _controlExecStatus;
            set
            {
                _controlExecStatus = value;
                OnPropertyChanged();
            }
        }

        public bool ATEnabled
        {
            get => _atEnabled;
            set
            {
                _atEnabled = value;
                OnPropertyChanged();
            }
        }

        public string ProportionSign
        {
            get => _proportionSign;
            set
            {
                _proportionSign = value;
                OnPropertyChanged();
            }
        }
    }
}