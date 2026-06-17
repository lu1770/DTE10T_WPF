using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== 实时温度卡片 ==========
    public class TempCardModel : INotifyPropertyChanged
    {
        private string _channel = "";
        private double _pv;
        private double _sv;
        private string _status = "";
        private string _inputType = "";
        private string _bgColor = "#FFFFFF";

        public string Channel { get => _channel; set { _channel = value; OnPropertyChanged(); } }
        public double PV { get => _pv; set { _pv = value; OnPropertyChanged(); } }
        public double SV { get => _sv; set { _sv = value; OnPropertyChanged(); } }
        public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
        public string InputType { get => _inputType; set { _inputType = value; OnPropertyChanged(); } }
        public string BgColor { get => _bgColor; set { _bgColor = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
    }

    // ========== PV/SV 设定 ==========
    public class PVSVModel : INotifyPropertyChanged
    {
        private string _channel = "";
        private double _pv;
        private double _sv;
        private string _inputType = "";
        private double _rangeHigh;
        private double _rangeLow;
        private bool _isEnabled = true;

        public string Channel { get => _channel; set { _channel = value; OnPropertyChanged(); } }
        public double PV { get => _pv; set { _pv = value; OnPropertyChanged(); } }
        public double SV { get => _sv; set { _sv = value; OnPropertyChanged(); } }
        public string InputType { get => _inputType; set { _inputType = value; OnPropertyChanged(); } }
        public double RangeHigh { get => _rangeHigh; set { _rangeHigh = value; OnPropertyChanged(); } }
        public double RangeLow { get => _rangeLow; set { _rangeLow = value; OnPropertyChanged(); } }
        public bool IsEnabled { get => _isEnabled; set { _isEnabled = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
    }

    // ========== PID 参数 ==========
    public class PIDModel : INotifyPropertyChanged
    {
        private string _channel = "";
        private string _controlMode = "PID";
        private double _pb;
        private double _ti;
        private double _td;
        private double _integral;
        private double _out1;
        private double _out2;
        private bool _atEnabled;

        public string Channel { get => _channel; set { _channel = value; OnPropertyChanged(); } }
        public string ControlMode { get => _controlMode; set { _controlMode = value; OnPropertyChanged(); } }
        public double Pb { get => _pb; set { _pb = value; OnPropertyChanged(); } }
        public double Ti { get => _ti; set { _ti = value; OnPropertyChanged(); } }
        public double Td { get => _td; set { _td = value; OnPropertyChanged(); } }
        public double Integral { get => _integral; set { _integral = value; OnPropertyChanged(); } }
        public double Out1 { get => _out1; set { _out1 = value; OnPropertyChanged(); } }
        public double Out2 { get => _out2; set { _out2 = value; OnPropertyChanged(); } }
        public bool ATEnabled { get => _atEnabled; set { _atEnabled = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
    }

    // ========== 警报设定 ==========
    public class AlarmModel : INotifyPropertyChanged
    {
        private string _channel = "";
        private string _alarmMode = "无警报";
        private double _alarmHigh;
        private double _alarmLow;
        private int _alarmDelay;
        private bool _alarmEnabled;

        public string Channel { get => _channel; set { _channel = value; OnPropertyChanged(); } }
        public string AlarmMode { get => _alarmMode; set { _alarmMode = value; OnPropertyChanged(); } }
        public double AlarmHigh { get => _alarmHigh; set { _alarmHigh = value; OnPropertyChanged(); } }
        public double AlarmLow { get => _alarmLow; set { _alarmLow = value; OnPropertyChanged(); } }
        public int AlarmDelay { get => _alarmDelay; set { _alarmDelay = value; OnPropertyChanged(); } }
        public bool AlarmEnabled { get => _alarmEnabled; set { _alarmEnabled = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
    }

    // ========== 输出配置 ==========
    public class OutputModel : INotifyPropertyChanged
    {
        private string _channel = "";
        private string _out1Function = "加热";
        private string _out2Function = "警报";
        private int _outMax = 100;
        private int _outMin = 0;
        private int _controlCycle = 1;
        private bool _outputReverse;

        public string Channel { get => _channel; set { _channel = value; OnPropertyChanged(); } }
        public string Out1Function { get => _out1Function; set { _out1Function = value; OnPropertyChanged(); } }
        public string Out2Function { get => _out2Function; set { _out2Function = value; OnPropertyChanged(); } }
        public int OutMax { get => _outMax; set { _outMax = value; OnPropertyChanged(); } }
        public int OutMin { get => _outMin; set { _outMin = value; OnPropertyChanged(); } }
        public int ControlCycle { get => _controlCycle; set { _controlCycle = value; OnPropertyChanged(); } }
        public bool OutputReverse { get => _outputReverse; set { _outputReverse = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
    }

    // ========== 斜率控制 ==========
    public class SlopeModel : INotifyPropertyChanged
    {
        private string _channel = "";
        private double _sv;
        private int _slope;
        private bool _slopeEnabled;

        public string Channel { get => _channel; set { _channel = value; OnPropertyChanged(); } }
        public double SV { get => _sv; set { _sv = value; OnPropertyChanged(); } }
        public int Slope { get => _slope; set { _slope = value; OnPropertyChanged(); } }
        public bool SlopeEnabled { get => _slopeEnabled; set { _slopeEnabled = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
    }

    // ========== 输入调整 ==========
    public class InputAdjModel : INotifyPropertyChanged
    {
        private string _channel = "";
        private int _offset;
        private int _gain;
        private int _filterCount = 8;
        private double _filterRange = 1.0;

        public string Channel { get => _channel; set { _channel = value; OnPropertyChanged(); } }
        public int Offset { get => _offset; set { _offset = value; OnPropertyChanged(); } }
        public int Gain { get => _gain; set { _gain = value; OnPropertyChanged(); } }
        public int FilterCount { get => _filterCount; set { _filterCount = value; OnPropertyChanged(); } }
        public double FilterRange { get => _filterRange; set { _filterRange = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
    }

    // ========== CT 电流 ==========
    public class CTModel : INotifyPropertyChanged
    {
        private string _channel = "";
        private int _ctStatic;
        private int _ctDynamic;
        private int _ctAdjust;

        public string Channel { get => _channel; set { _channel = value; OnPropertyChanged(); } }
        public int CTStatic { get => _ctStatic; set { _ctStatic = value; OnPropertyChanged(); } }
        public int CTDynamic { get => _ctDynamic; set { _ctDynamic = value; OnPropertyChanged(); } }
        public int CTAdjust { get => _ctAdjust; set { _ctAdjust = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
    }

    // ========== EVENT ==========
    public class EventModel : INotifyPropertyChanged
    {
        private string _channel = "";
        private string _eventFunction = "无功能";

        public string Channel { get => _channel; set { _channel = value; OnPropertyChanged(); } }
        public string EventFunction { get => _eventFunction; set { _eventFunction = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
    }

    // ========== 热流道 ==========
    public class HotRunnerModel : INotifyPropertyChanged
    {
        private string _channel = "";
        private int _limitTemp;
        private int _fixedOutput;
        private int _soakTime;
        private double _sv;
        private int _slope;

        public string Channel { get => _channel; set { _channel = value; OnPropertyChanged(); } }
        public int LimitTemp { get => _limitTemp; set { _limitTemp = value; OnPropertyChanged(); } }
        public int FixedOutput { get => _fixedOutput; set { _fixedOutput = value; OnPropertyChanged(); } }
        public int SoakTime { get => _soakTime; set { _soakTime = value; OnPropertyChanged(); } }
        public double SV { get => _sv; set { _sv = value; OnPropertyChanged(); } }
        public int Slope { get => _slope; set { _slope = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
    }

    // ========== 通讯参数 ==========
    public class CommParamModel : INotifyPropertyChanged
    {
        private string _paramName = "";
        private string _paramValue = "";
        private string _paramDesc = "";

        public string ParamName { get => _paramName; set { _paramName = value; OnPropertyChanged(); } }
        public string ParamValue { get => _paramValue; set { _paramValue = value; OnPropertyChanged(); } }
        public string ParamDesc { get => _paramDesc; set { _paramDesc = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
    }

    // ========== 可程控 - 样式 ==========
    public class ProgramPatternModel : INotifyPropertyChanged
    {
        private string _pattern = "";
        private int _maxSteps;
        private int _loopCount;
        private int _nextPattern;

        public string Pattern { get => _pattern; set { _pattern = value; OnPropertyChanged(); } }
        public int MaxSteps { get => _maxSteps; set { _maxSteps = value; OnPropertyChanged(); } }
        public int LoopCount { get => _loopCount; set { _loopCount = value; OnPropertyChanged(); } }
        public int NextPattern { get => _nextPattern; set { _nextPattern = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
    }

    // ========== 可程控 - 步骤 ==========
    public class ProgramStepModel : INotifyPropertyChanged
    {
        private string _pattern = "";
        private string _step = "";
        private double _targetTemp;
        private int _runTime;
        private string _timeUnit = "分";

        public string Pattern { get => _pattern; set { _pattern = value; OnPropertyChanged(); } }
        public string Step { get => _step; set { _step = value; OnPropertyChanged(); } }
        public double TargetTemp { get => _targetTemp; set { _targetTemp = value; OnPropertyChanged(); } }
        public int RunTime { get => _runTime; set { _runTime = value; OnPropertyChanged(); } }
        public string TimeUnit { get => _timeUnit; set { _timeUnit = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? ""));
    }

    // ========== Modbus 寄存器映射 ==========
    public class ModbusRegister
    {
        public string Name { get; set; } = "";
        public int Address { get; set; }
        public int HexAddress { get; set; }
        public string Description { get; set; } = "";
        public string DataType { get; set; } = "FLOAT32";
        public double ScalingFactor { get; set; } = 1.0;
        public string Unit { get; set; } = "";
        public int Channel { get; set; }
    }
}
