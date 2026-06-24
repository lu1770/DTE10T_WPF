using System;
using System.Linq;

namespace DTE10T_WPF
{
    public class RecordedDataPoint
    {
        public RecordedDataPoint(DateTime timestamp, double elapsedSeconds, double[] chValues, double[] out1Values, double[] out2Values)
        {
            Timestamp = timestamp;
            ElapsedSeconds = elapsedSeconds;
            CHValues = chValues;
            Out1Values = out1Values;
            Out2Values = out2Values;
        }

        public double[] CHValues { get; set; } = new double[8];

        public double ElapsedSeconds { get; set; }

        public double[] Out1Values { get; set; } = new double[8];

        public double[] Out2Values { get; set; } = new double[8];

        public DateTime Timestamp { get; set; }
    }
}
