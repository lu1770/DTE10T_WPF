using System;
using System.ComponentModel;

namespace DTE10T_WPF
{
    // ========== 可程控 - 样式 ==========
    public class ProgramPatternModel : INotifyPropertyChanged
    {
        private int _loopCount;
        private int _maxSteps;
        private int _nextPattern;
        private string _pattern = string.Empty;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name ?? string.Empty));

        public int LoopCount
        {
            get => _loopCount;
            set
            {
                _loopCount = value;
                OnPropertyChanged();
            }
        }

        public int MaxSteps
        {
            get => _maxSteps;
            set
            {
                _maxSteps = value;
                OnPropertyChanged();
            }
        }

        public int NextPattern
        {
            get => _nextPattern;
            set
            {
                _nextPattern = value;
                OnPropertyChanged();
            }
        }

        public string Pattern
        {
            get => _pattern;
            set
            {
                _pattern = value;
                OnPropertyChanged();
            }
        }
    }
}
