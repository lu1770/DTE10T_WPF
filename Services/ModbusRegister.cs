using System;

namespace DTE10T_WPF
{
    // ========== Modbus 寄存器映射 ==========
    public class ModbusRegister
    {
        public int Address { get; set; }

        public int Channel { get; set; }

        public string DataType { get; set; } = "FLOAT32";

        public string Description { get; set; } = string.Empty;

        public int HexAddress { get; set; }

        public string Name { get; set; } = string.Empty;

        public double ScalingFactor { get; set; } = 1.0;

        public string Unit { get; set; } = string.Empty;
    }
}
