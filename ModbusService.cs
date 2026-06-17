using NModbus;
using NModbus.Device;
using NModbus.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DTE10T_WPF
{
    internal class SerialStreamResource : IStreamResource
    {
        private readonly SerialPort _serialPort;

        public SerialStreamResource(SerialPort serialPort)
        {
            _serialPort = serialPort;
        }

        public Stream Stream => _serialPort.BaseStream;

        public int InfiniteTimeout => Timeout.Infinite;

        public int ReadTimeout
        {
            get => _serialPort.ReadTimeout;
            set => _serialPort.ReadTimeout = value;
        }

        public int WriteTimeout
        {
            get => _serialPort.WriteTimeout;
            set => _serialPort.WriteTimeout = value;
        }

        public void DiscardInBuffer()
        {
            _serialPort.DiscardInBuffer();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return _serialPort.Read(buffer, offset, count);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _serialPort.Write(buffer, offset, count);
        }

        public void Dispose()
        {
            _serialPort.Dispose();
        }
    }
}

namespace DTE10T_WPF
{
    ///<summary>
    /// DTE10T Modbus RTU 通讯服务 - 基于 NModbus 真实实现
    /// NuGet: Install-Package NModbus
    ///</summary>
    public class ModbusService : IDisposable
    {
        // DTE10T 通讯地址映射表（完整版）
        private static readonly ModbusRegister[] _registerMap = new[]
        {
            // ===== 基本参数 (INA + INB 双地址) =====
            // PV 当前温度值 H1000~H1007 (FLOAT32, 0.1℃)
            new ModbusRegister
            {
                Name = "PV_CH1",
                Address = 0x1000,
                HexAddress = 0x1100,
                Description = "CH1 PV当前温度值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "PV_CH2",
                Address = 0x1001,
                HexAddress = 0x1200,
                Description = "CH2 PV当前温度值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "PV_CH3",
                Address = 0x1002,
                HexAddress = 0x1300,
                Description = "CH3 PV当前温度值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "PV_CH4",
                Address = 0x1003,
                HexAddress = 0x1400,
                Description = "CH4 PV当前温度值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "PV_CH5",
                Address = 0x1004,
                HexAddress = 0x1500,
                Description = "CH5 PV当前温度值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "PV_CH6",
                Address = 0x1005,
                HexAddress = 0x1600,
                Description = "CH6 PV当前温度值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "PV_CH7",
                Address = 0x1006,
                HexAddress = 0x1700,
                Description = "CH7 PV当前温度值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "PV_CH8",
                Address = 0x1007,
                HexAddress = 0x1800,
                Description = "CH8 PV当前温度值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },

            // SV 温度设定值 H1008~H100F
            new ModbusRegister
            {
                Name = "SV_CH1",
                Address = 0x1008,
                HexAddress = 0x1101,
                Description = "CH1 SV温度设定值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "SV_CH2",
                Address = 0x1009,
                HexAddress = 0x1201,
                Description = "CH2 SV温度设定值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "SV_CH3",
                Address = 0x100A,
                HexAddress = 0x1301,
                Description = "CH3 SV温度设定值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "SV_CH4",
                Address = 0x100B,
                HexAddress = 0x1401,
                Description = "CH4 SV温度设定值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "SV_CH5",
                Address = 0x100C,
                HexAddress = 0x1501,
                Description = "CH5 SV温度设定值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "SV_CH6",
                Address = 0x100D,
                HexAddress = 0x1601,
                Description = "CH6 SV温度设定值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "SV_CH7",
                Address = 0x100E,
                HexAddress = 0x1701,
                Description = "CH7 SV温度设定值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "SV_CH8",
                Address = 0x100F,
                HexAddress = 0x1801,
                Description = "CH8 SV温度设定值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "℃"
            },

            // 温度侦测上限 H1010~H1017
            new ModbusRegister
            {
                Name = "RangeHigh_CH1",
                Address = 0x1010,
                HexAddress = 0x1102,
                Description = "CH1 温度侦测上限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "RangeHigh_CH2",
                Address = 0x1011,
                HexAddress = 0x1202,
                Description = "CH2 温度侦测上限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "RangeHigh_CH3",
                Address = 0x1012,
                HexAddress = 0x1302,
                Description = "CH3 温度侦测上限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "RangeHigh_CH4",
                Address = 0x1013,
                HexAddress = 0x1402,
                Description = "CH4 温度侦测上限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "RangeHigh_CH5",
                Address = 0x1014,
                HexAddress = 0x1502,
                Description = "CH5 温度侦测上限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "RangeHigh_CH6",
                Address = 0x1015,
                HexAddress = 0x1602,
                Description = "CH6 温度侦测上限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "RangeHigh_CH7",
                Address = 0x1016,
                HexAddress = 0x1702,
                Description = "CH7 温度侦测上限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "RangeHigh_CH8",
                Address = 0x1017,
                HexAddress = 0x1802,
                Description = "CH8 温度侦测上限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },

            // 温度侦测下限 H1018~H101F
            new ModbusRegister
            {
                Name = "RangeLow_CH1",
                Address = 0x1018,
                HexAddress = 0x1103,
                Description = "CH1 温度侦测下限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "RangeLow_CH2",
                Address = 0x1019,
                HexAddress = 0x1203,
                Description = "CH2 温度侦测下限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "RangeLow_CH3",
                Address = 0x101A,
                HexAddress = 0x1303,
                Description = "CH3 温度侦测下限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "RangeLow_CH4",
                Address = 0x101B,
                HexAddress = 0x1403,
                Description = "CH4 温度侦测下限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "RangeLow_CH5",
                Address = 0x101C,
                HexAddress = 0x1503,
                Description = "CH5 温度侦测下限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "RangeLow_CH6",
                Address = 0x101D,
                HexAddress = 0x1603,
                Description = "CH6 温度侦测下限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "RangeLow_CH7",
                Address = 0x101E,
                HexAddress = 0x1703,
                Description = "CH7 温度侦测下限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "RangeLow_CH8",
                Address = 0x101F,
                HexAddress = 0x1803,
                Description = "CH8 温度侦测下限",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },

            // 输入补偿值 H1020~H1027 (INT16, -999~+999, 0.1℃)
            new ModbusRegister
            {
                Name = "Offset_CH1",
                Address = 0x1020,
                HexAddress = 0x1104,
                Description = "CH1 输入补偿值",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "Offset_CH2",
                Address = 0x1021,
                HexAddress = 0x1204,
                Description = "CH2 输入补偿值",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "Offset_CH3",
                Address = 0x1022,
                HexAddress = 0x1304,
                Description = "CH3 输入补偿值",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "Offset_CH4",
                Address = 0x1023,
                HexAddress = 0x1404,
                Description = "CH4 输入补偿值",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "Offset_CH5",
                Address = 0x1024,
                HexAddress = 0x1504,
                Description = "CH5 输入补偿值",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "Offset_CH6",
                Address = 0x1025,
                HexAddress = 0x1604,
                Description = "CH6 输入补偿值",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "Offset_CH7",
                Address = 0x1026,
                HexAddress = 0x1704,
                Description = "CH7 输入补偿值",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "Offset_CH8",
                Address = 0x1027,
                HexAddress = 0x1804,
                Description = "CH8 输入补偿值",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },

            // PID Pb 比例带 H1028~H102F
            new ModbusRegister
            {
                Name = "Pb_CH1",
                Address = 0x1028,
                HexAddress = 0x1105,
                Description = "CH1 Pb比例带",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Pb_CH2",
                Address = 0x1029,
                HexAddress = 0x1205,
                Description = "CH2 Pb比例带",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Pb_CH3",
                Address = 0x102A,
                HexAddress = 0x1305,
                Description = "CH3 Pb比例带",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Pb_CH4",
                Address = 0x102B,
                HexAddress = 0x1405,
                Description = "CH4 Pb比例带",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Pb_CH5",
                Address = 0x102C,
                HexAddress = 0x1505,
                Description = "CH5 Pb比例带",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Pb_CH6",
                Address = 0x102D,
                HexAddress = 0x1605,
                Description = "CH6 Pb比例带",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Pb_CH7",
                Address = 0x102E,
                HexAddress = 0x1705,
                Description = "CH7 Pb比例带",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Pb_CH8",
                Address = 0x102F,
                HexAddress = 0x1805,
                Description = "CH8 Pb比例带",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = string.Empty
            },

            // PID Ti 积分时间 H1030~H1037
            new ModbusRegister
            {
                Name = "Ti_CH1",
                Address = 0x1030,
                HexAddress = 0x1106,
                Description = "CH1 Ti积分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "Ti_CH2",
                Address = 0x1031,
                HexAddress = 0x1206,
                Description = "CH2 Ti积分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "Ti_CH3",
                Address = 0x1032,
                HexAddress = 0x1306,
                Description = "CH3 Ti积分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "Ti_CH4",
                Address = 0x1033,
                HexAddress = 0x1406,
                Description = "CH4 Ti积分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "Ti_CH5",
                Address = 0x1034,
                HexAddress = 0x1506,
                Description = "CH5 Ti积分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "Ti_CH6",
                Address = 0x1035,
                HexAddress = 0x1606,
                Description = "CH6 Ti积分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "Ti_CH7",
                Address = 0x1036,
                HexAddress = 0x1706,
                Description = "CH7 Ti积分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "Ti_CH8",
                Address = 0x1037,
                HexAddress = 0x1806,
                Description = "CH8 Ti积分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },

            // PID Td 微分时间 H1038~H103F
            new ModbusRegister
            {
                Name = "Td_CH1",
                Address = 0x1038,
                HexAddress = 0x1107,
                Description = "CH1 Td微分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "Td_CH2",
                Address = 0x1039,
                HexAddress = 0x1207,
                Description = "CH2 Td微分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "Td_CH3",
                Address = 0x103A,
                HexAddress = 0x1307,
                Description = "CH3 Td微分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "Td_CH4",
                Address = 0x103B,
                HexAddress = 0x1407,
                Description = "CH4 Td微分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "Td_CH5",
                Address = 0x103C,
                HexAddress = 0x1507,
                Description = "CH5 Td微分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "Td_CH6",
                Address = 0x103D,
                HexAddress = 0x1607,
                Description = "CH6 Td微分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "Td_CH7",
                Address = 0x103E,
                HexAddress = 0x1707,
                Description = "CH7 Td微分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "Td_CH8",
                Address = 0x103F,
                HexAddress = 0x1807,
                Description = "CH8 Td微分时间",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "s"
            },

            // 积分量默认值 H1040~H1047
            new ModbusRegister
            {
                Name = "Integral_CH1",
                Address = 0x1040,
                HexAddress = 0x1108,
                Description = "CH1 积分量默认值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Integral_CH2",
                Address = 0x1041,
                HexAddress = 0x1208,
                Description = "CH2 积分量默认值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Integral_CH3",
                Address = 0x1042,
                HexAddress = 0x1308,
                Description = "CH3 积分量默认值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Integral_CH4",
                Address = 0x1043,
                HexAddress = 0x1408,
                Description = "CH4 积分量默认值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Integral_CH5",
                Address = 0x1044,
                HexAddress = 0x1508,
                Description = "CH5 积分量默认值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Integral_CH6",
                Address = 0x1045,
                HexAddress = 0x1608,
                Description = "CH6 积分量默认值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Integral_CH7",
                Address = 0x1046,
                HexAddress = 0x1708,
                Description = "CH7 积分量默认值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Integral_CH8",
                Address = 0x1047,
                HexAddress = 0x1808,
                Description = "CH8 积分量默认值",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },

            // 比例误差补偿值 H1048~H104F
            new ModbusRegister
            {
                Name = "PropComp_CH1",
                Address = 0x1048,
                HexAddress = 0x1109,
                Description = "CH1 比例误差补偿",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "PropComp_CH2",
                Address = 0x1049,
                HexAddress = 0x1209,
                Description = "CH2 比例误差补偿",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "PropComp_CH3",
                Address = 0x104A,
                HexAddress = 0x1309,
                Description = "CH3 比例误差补偿",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "PropComp_CH4",
                Address = 0x104B,
                HexAddress = 0x1409,
                Description = "CH4 比例误差补偿",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "PropComp_CH5",
                Address = 0x104C,
                HexAddress = 0x1509,
                Description = "CH5 比例误差补偿",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "PropComp_CH6",
                Address = 0x104D,
                HexAddress = 0x1609,
                Description = "CH6 比例误差补偿",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "PropComp_CH7",
                Address = 0x104E,
                HexAddress = 0x1709,
                Description = "CH7 比例误差补偿",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "PropComp_CH8",
                Address = 0x104F,
                HexAddress = 0x1809,
                Description = "CH8 比例误差补偿",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },

            // 输出1量(读/写) H1070~H1077
            new ModbusRegister
            {
                Name = "Out1_CH1",
                Address = 0x1070,
                HexAddress = 0x110E,
                Description = "CH1 输出1量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Out1_CH2",
                Address = 0x1071,
                HexAddress = 0x120E,
                Description = "CH2 输出1量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Out1_CH3",
                Address = 0x1072,
                HexAddress = 0x130E,
                Description = "CH3 输出1量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Out1_CH4",
                Address = 0x1073,
                HexAddress = 0x140E,
                Description = "CH4 输出1量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Out1_CH5",
                Address = 0x1074,
                HexAddress = 0x150E,
                Description = "CH5 输出1量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Out1_CH6",
                Address = 0x1075,
                HexAddress = 0x160E,
                Description = "CH6 输出1量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Out1_CH7",
                Address = 0x1076,
                HexAddress = 0x170E,
                Description = "CH7 输出1量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Out1_CH8",
                Address = 0x1077,
                HexAddress = 0x180E,
                Description = "CH8 输出1量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },

            // 输出2量(读/写) H1078~H107F
            new ModbusRegister
            {
                Name = "Out2_CH1",
                Address = 0x1078,
                HexAddress = 0x110F,
                Description = "CH1 输出2量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Out2_CH2",
                Address = 0x1079,
                HexAddress = 0x120F,
                Description = "CH2 输出2量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Out2_CH3",
                Address = 0x107A,
                HexAddress = 0x130F,
                Description = "CH3 输出2量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Out2_CH4",
                Address = 0x107B,
                HexAddress = 0x140F,
                Description = "CH4 输出2量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Out2_CH5",
                Address = 0x107C,
                HexAddress = 0x150F,
                Description = "CH5 输出2量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Out2_CH6",
                Address = 0x107D,
                HexAddress = 0x160F,
                Description = "CH6 输出2量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Out2_CH7",
                Address = 0x107E,
                HexAddress = 0x170F,
                Description = "CH7 输出2量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "Out2_CH8",
                Address = 0x107F,
                HexAddress = 0x180F,
                Description = "CH8 输出2量",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = "%"
            },

            // 警报上限值 H1080~H1087
            new ModbusRegister
            {
                Name = "AlarmHigh_CH1",
                Address = 0x1080,
                HexAddress = 0x1110,
                Description = "CH1 警报上限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "AlarmHigh_CH2",
                Address = 0x1081,
                HexAddress = 0x1210,
                Description = "CH2 警报上限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "AlarmHigh_CH3",
                Address = 0x1082,
                HexAddress = 0x1310,
                Description = "CH3 警报上限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "AlarmHigh_CH4",
                Address = 0x1083,
                HexAddress = 0x1410,
                Description = "CH4 警报上限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "AlarmHigh_CH5",
                Address = 0x1084,
                HexAddress = 0x1510,
                Description = "CH5 警报上限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "AlarmHigh_CH6",
                Address = 0x1085,
                HexAddress = 0x1610,
                Description = "CH6 警报上限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "AlarmHigh_CH7",
                Address = 0x1086,
                HexAddress = 0x1710,
                Description = "CH7 警报上限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "AlarmHigh_CH8",
                Address = 0x1087,
                HexAddress = 0x1810,
                Description = "CH8 警报上限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },

            // 警报下限值 H1088~H108F
            new ModbusRegister
            {
                Name = "AlarmLow_CH1",
                Address = 0x1088,
                HexAddress = 0x1111,
                Description = "CH1 警报下限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "AlarmLow_CH2",
                Address = 0x1089,
                HexAddress = 0x1211,
                Description = "CH2 警报下限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "AlarmLow_CH3",
                Address = 0x108A,
                HexAddress = 0x1311,
                Description = "CH3 警报下限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "AlarmLow_CH4",
                Address = 0x108B,
                HexAddress = 0x1411,
                Description = "CH4 警报下限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "AlarmLow_CH5",
                Address = 0x108C,
                HexAddress = 0x1511,
                Description = "CH5 警报下限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "AlarmLow_CH6",
                Address = 0x108D,
                HexAddress = 0x1611,
                Description = "CH6 警报下限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "AlarmLow_CH7",
                Address = 0x108E,
                HexAddress = 0x1711,
                Description = "CH7 警报下限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "AlarmLow_CH8",
                Address = 0x108F,
                HexAddress = 0x1811,
                Description = "CH8 警报下限值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = "℃"
            },

            // 输入传感器类型 H10A0~H10A7 (INT16)
            new ModbusRegister
            {
                Name = "SensorType_CH1",
                Address = 0x10A0,
                HexAddress = 0x1114,
                Description = "CH1 传感器类型",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "SensorType_CH2",
                Address = 0x10A1,
                HexAddress = 0x1214,
                Description = "CH2 传感器类型",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "SensorType_CH3",
                Address = 0x10A2,
                HexAddress = 0x1314,
                Description = "CH3 传感器类型",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "SensorType_CH4",
                Address = 0x10A3,
                HexAddress = 0x1414,
                Description = "CH4 传感器类型",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "SensorType_CH5",
                Address = 0x10A4,
                HexAddress = 0x1514,
                Description = "CH5 传感器类型",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "SensorType_CH6",
                Address = 0x10A5,
                HexAddress = 0x1614,
                Description = "CH6 传感器类型",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "SensorType_CH7",
                Address = 0x10A6,
                HexAddress = 0x1714,
                Description = "CH7 传感器类型",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "SensorType_CH8",
                Address = 0x10A7,
                HexAddress = 0x1814,
                Description = "CH8 传感器类型",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },

            // 输出1控制选择 H10A8~H10AF (INT16)
            new ModbusRegister
            {
                Name = "Out1Ctrl_CH1",
                Address = 0x10A8,
                HexAddress = 0x1115,
                Description = "CH1 输出1控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Out1Ctrl_CH2",
                Address = 0x10A9,
                HexAddress = 0x1215,
                Description = "CH2 输出1控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Out1Ctrl_CH3",
                Address = 0x10AA,
                HexAddress = 0x1315,
                Description = "CH3 输出1控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Out1Ctrl_CH4",
                Address = 0x10AB,
                HexAddress = 0x1415,
                Description = "CH4 输出1控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Out1Ctrl_CH5",
                Address = 0x10AC,
                HexAddress = 0x1515,
                Description = "CH5 输出1控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Out1Ctrl_CH6",
                Address = 0x10AD,
                HexAddress = 0x1615,
                Description = "CH6 输出1控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Out1Ctrl_CH7",
                Address = 0x10AE,
                HexAddress = 0x1715,
                Description = "CH7 输出1控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Out1Ctrl_CH8",
                Address = 0x10AF,
                HexAddress = 0x1815,
                Description = "CH8 输出1控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },

            // 输出2控制选择 H10B0~H10B7 (INT16)
            new ModbusRegister
            {
                Name = "Out2Ctrl_CH1",
                Address = 0x10B0,
                HexAddress = 0x1116,
                Description = "CH1 输出2控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Out2Ctrl_CH2",
                Address = 0x10B1,
                HexAddress = 0x1216,
                Description = "CH2 输出2控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Out2Ctrl_CH3",
                Address = 0x10B2,
                HexAddress = 0x1316,
                Description = "CH3 输出2控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Out2Ctrl_CH4",
                Address = 0x10B3,
                HexAddress = 0x1416,
                Description = "CH4 输出2控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Out2Ctrl_CH5",
                Address = 0x10B4,
                HexAddress = 0x1516,
                Description = "CH5 输出2控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Out2Ctrl_CH6",
                Address = 0x10B5,
                HexAddress = 0x1616,
                Description = "CH6 输出2控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Out2Ctrl_CH7",
                Address = 0x10B6,
                HexAddress = 0x1716,
                Description = "CH7 输出2控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Out2Ctrl_CH8",
                Address = 0x10B7,
                HexAddress = 0x1816,
                Description = "CH8 输出2控制选择",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },

            // 控制方式 H10B8~H10BF (INT16)
            new ModbusRegister
            {
                Name = "CtrlMode_CH1",
                Address = 0x10B8,
                HexAddress = 0x1117,
                Description = "CH1 控制方式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CtrlMode_CH2",
                Address = 0x10B9,
                HexAddress = 0x1217,
                Description = "CH2 控制方式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CtrlMode_CH3",
                Address = 0x10BA,
                HexAddress = 0x1317,
                Description = "CH3 控制方式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CtrlMode_CH4",
                Address = 0x10BB,
                HexAddress = 0x1417,
                Description = "CH4 控制方式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CtrlMode_CH5",
                Address = 0x10BC,
                HexAddress = 0x1517,
                Description = "CH5 控制方式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CtrlMode_CH6",
                Address = 0x10BD,
                HexAddress = 0x1617,
                Description = "CH6 控制方式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CtrlMode_CH7",
                Address = 0x10BE,
                HexAddress = 0x1717,
                Description = "CH7 控制方式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CtrlMode_CH8",
                Address = 0x10BF,
                HexAddress = 0x1817,
                Description = "CH8 控制方式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },

            // 警报一输出模式 H10C0~H10C7 (INT16)
            new ModbusRegister
            {
                Name = "Alarm1Mode_CH1",
                Address = 0x10C0,
                HexAddress = 0x1118,
                Description = "CH1 警报一输出模式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Alarm1Mode_CH2",
                Address = 0x10C1,
                HexAddress = 0x1218,
                Description = "CH2 警报一输出模式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Alarm1Mode_CH3",
                Address = 0x10C2,
                HexAddress = 0x1318,
                Description = "CH3 警报一输出模式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Alarm1Mode_CH4",
                Address = 0x10C3,
                HexAddress = 0x1418,
                Description = "CH4 警报一输出模式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Alarm1Mode_CH5",
                Address = 0x10C4,
                HexAddress = 0x1518,
                Description = "CH5 警报一输出模式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Alarm1Mode_CH6",
                Address = 0x10C5,
                HexAddress = 0x1618,
                Description = "CH6 警报一输出模式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Alarm1Mode_CH7",
                Address = 0x10C6,
                HexAddress = 0x1718,
                Description = "CH7 警报一输出模式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Alarm1Mode_CH8",
                Address = 0x10C7,
                HexAddress = 0x1818,
                Description = "CH8 警报一输出模式",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },

            // 控制执行/停止 H10D8~H10DF (INT16)
            new ModbusRegister
            {
                Name = "CtrlExec_CH1",
                Address = 0x10D8,
                HexAddress = 0x111B,
                Description = "CH1 控制执行/停止",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CtrlExec_CH2",
                Address = 0x10D9,
                HexAddress = 0x121B,
                Description = "CH2 控制执行/停止",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CtrlExec_CH3",
                Address = 0x10DA,
                HexAddress = 0x131B,
                Description = "CH3 控制执行/停止",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CtrlExec_CH4",
                Address = 0x10DB,
                HexAddress = 0x141B,
                Description = "CH4 控制执行/停止",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CtrlExec_CH5",
                Address = 0x10DC,
                HexAddress = 0x151B,
                Description = "CH5 控制执行/停止",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CtrlExec_CH6",
                Address = 0x10DD,
                HexAddress = 0x161B,
                Description = "CH6 控制执行/停止",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CtrlExec_CH7",
                Address = 0x10DE,
                HexAddress = 0x171B,
                Description = "CH7 控制执行/停止",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CtrlExec_CH8",
                Address = 0x10DF,
                HexAddress = 0x181B,
                Description = "CH8 控制执行/停止",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },

            // AT 自整定状态 H10E0~H10E7 (INT16)
            new ModbusRegister
            {
                Name = "ATStatus_CH1",
                Address = 0x10E0,
                HexAddress = 0x111C,
                Description = "CH1 AT自整定状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "ATStatus_CH2",
                Address = 0x10E1,
                HexAddress = 0x121C,
                Description = "CH2 AT自整定状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "ATStatus_CH3",
                Address = 0x10E2,
                HexAddress = 0x131C,
                Description = "CH3 AT自整定状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "ATStatus_CH4",
                Address = 0x10E3,
                HexAddress = 0x141C,
                Description = "CH4 AT自整定状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "ATStatus_CH5",
                Address = 0x10E4,
                HexAddress = 0x151C,
                Description = "CH5 AT自整定状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "ATStatus_CH6",
                Address = 0x10E5,
                HexAddress = 0x161C,
                Description = "CH6 AT自整定状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "ATStatus_CH7",
                Address = 0x10E6,
                HexAddress = 0x171C,
                Description = "CH7 AT自整定状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "ATStatus_CH8",
                Address = 0x10E7,
                HexAddress = 0x181C,
                Description = "CH8 AT自整定状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },

            // ===== 系统参数 =====
            // H10F0 温度单位 (INT16)
            new ModbusRegister
            {
                Name = "TempUnit",
                Address = 0x10F0,
                Description = "温度单位 0=℃,1=°F",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            // H10F6 通道禁能 (INT16, Bit0~Bit7)
            new ModbusRegister
            {
                Name = "ChannelDisable",
                Address = 0x10F6,
                Description = "通道禁能 Bit0=CH1~Bit7=CH8",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            // H10F7 滤波次数 (INT16)
            new ModbusRegister
            {
                Name = "FilterCount",
                Address = 0x10F7,
                Description = "滤波次数 0~50",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            // H10F8 自动站号 (INT16)
            new ModbusRegister
            {
                Name = "AutoStation",
                Address = 0x10F8,
                Description = "自动站号设定 1=启用",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            // H10F9 滤波范围 (FLOAT32)
            new ModbusRegister
            {
                Name = "FilterRange",
                Address = 0x10F9,
                Description = "滤波范围 0.1~10.0",
                DataType = "FLOAT32",
                ScalingFactor = 0.1,
                Unit = string.Empty
            },
            // H10FA 波特率 (INT16)
            new ModbusRegister
            {
                Name = "BaudRate",
                Address = 0x10FA,
                Description = "波特率 0~6",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            // H10FB 协议格式 (INT16)
            new ModbusRegister
            {
                Name = "ProtocolFormat",
                Address = 0x10FB,
                Description = "协议 0=ASCII,1=RTU",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            // H10FC 数据位 (INT16)
            new ModbusRegister
            {
                Name = "DataBits",
                Address = 0x10FC,
                Description = "数据位 0=8bits,1=7bits",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            // H10FD 停止位 (INT16)
            new ModbusRegister
            {
                Name = "StopBits",
                Address = 0x10FD,
                Description = "停止位 0=2stop,1=1stop",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            // H10FE 校验位 (INT16)
            new ModbusRegister
            {
                Name = "Parity",
                Address = 0x10FE,
                Description = "校验 0=None,1=Even,2=Odd",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            // H10FF 站号 (INT16)
            new ModbusRegister
            {
                Name = "StationCode",
                Address = 0x10FF,
                Description = "站号 1~247",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },

            // ===== 输入增益值 H19B8~H19BF =====
            new ModbusRegister
            {
                Name = "Gain_CH1",
                Address = 0x19B8,
                Description = "CH1 输入增益值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Gain_CH2",
                Address = 0x19B9,
                Description = "CH2 输入增益值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Gain_CH3",
                Address = 0x19BA,
                Description = "CH3 输入增益值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Gain_CH4",
                Address = 0x19BB,
                Description = "CH4 输入增益值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Gain_CH5",
                Address = 0x19BC,
                Description = "CH5 输入增益值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Gain_CH6",
                Address = 0x19BD,
                Description = "CH6 输入增益值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Gain_CH7",
                Address = 0x19BE,
                Description = "CH7 输入增益值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "Gain_CH8",
                Address = 0x19BF,
                Description = "CH8 输入增益值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },

            // ===== 斜率设定 H1970~H1977 =====
            new ModbusRegister
            {
                Name = "Slope_CH1",
                Address = 0x1970,
                Description = "CH1 斜率设定",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃/min"
            },
            new ModbusRegister
            {
                Name = "Slope_CH2",
                Address = 0x1971,
                Description = "CH2 斜率设定",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃/min"
            },
            new ModbusRegister
            {
                Name = "Slope_CH3",
                Address = 0x1972,
                Description = "CH3 斜率设定",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃/min"
            },
            new ModbusRegister
            {
                Name = "Slope_CH4",
                Address = 0x1973,
                Description = "CH4 斜率设定",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃/min"
            },
            new ModbusRegister
            {
                Name = "Slope_CH5",
                Address = 0x1974,
                Description = "CH5 斜率设定",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃/min"
            },
            new ModbusRegister
            {
                Name = "Slope_CH6",
                Address = 0x1975,
                Description = "CH6 斜率设定",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃/min"
            },
            new ModbusRegister
            {
                Name = "Slope_CH7",
                Address = 0x1976,
                Description = "CH7 斜率设定",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃/min"
            },
            new ModbusRegister
            {
                Name = "Slope_CH8",
                Address = 0x1977,
                Description = "CH8 斜率设定",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃/min"
            },

            // ===== 输出最大值 H1980~H1987 (INT16, %) =====
            new ModbusRegister
            {
                Name = "OutMax_CH1",
                Address = 0x1980,
                Description = "CH1 输出最大值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "OutMax_CH2",
                Address = 0x1981,
                Description = "CH2 输出最大值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "OutMax_CH3",
                Address = 0x1982,
                Description = "CH3 输出最大值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "OutMax_CH4",
                Address = 0x1983,
                Description = "CH4 输出最大值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "OutMax_CH5",
                Address = 0x1984,
                Description = "CH5 输出最大值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "OutMax_CH6",
                Address = 0x1985,
                Description = "CH6 输出最大值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "OutMax_CH7",
                Address = 0x1986,
                Description = "CH7 输出最大值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "OutMax_CH8",
                Address = 0x1987,
                Description = "CH8 输出最大值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },

            // ===== 输出最小值 H1988~H198F (INT16, %) =====
            new ModbusRegister
            {
                Name = "OutMin_CH1",
                Address = 0x1988,
                Description = "CH1 输出最小值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "OutMin_CH2",
                Address = 0x1989,
                Description = "CH2 输出最小值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "OutMin_CH3",
                Address = 0x198A,
                Description = "CH3 输出最小值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "OutMin_CH4",
                Address = 0x198B,
                Description = "CH4 输出最小值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "OutMin_CH5",
                Address = 0x198C,
                Description = "CH5 输出最小值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "OutMin_CH6",
                Address = 0x198D,
                Description = "CH6 输出最小值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "OutMin_CH7",
                Address = 0x198E,
                Description = "CH7 输出最小值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "OutMin_CH8",
                Address = 0x198F,
                Description = "CH8 输出最小值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "%"
            },

            // ===== 警报延迟 H1990~H1997 (INT16, 秒) =====
            new ModbusRegister
            {
                Name = "AlarmDelay_CH1",
                Address = 0x1990,
                Description = "CH1 警报延迟",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "AlarmDelay_CH2",
                Address = 0x1991,
                Description = "CH2 警报延迟",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "AlarmDelay_CH3",
                Address = 0x1992,
                Description = "CH3 警报延迟",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "AlarmDelay_CH4",
                Address = 0x1993,
                Description = "CH4 警报延迟",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "AlarmDelay_CH5",
                Address = 0x1994,
                Description = "CH5 警报延迟",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "AlarmDelay_CH6",
                Address = 0x1995,
                Description = "CH6 警报延迟",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "AlarmDelay_CH7",
                Address = 0x1996,
                Description = "CH7 警报延迟",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "s"
            },
            new ModbusRegister
            {
                Name = "AlarmDelay_CH8",
                Address = 0x1997,
                Description = "CH8 警报延迟",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "s"
            },

            // ===== EVENT 功能 H1998~H199F (INT16) =====
            new ModbusRegister
            {
                Name = "EventFunc_CH1",
                Address = 0x1998,
                Description = "CH1 EVENT功能",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "EventFunc_CH2",
                Address = 0x1999,
                Description = "CH2 EVENT功能",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "EventFunc_CH3",
                Address = 0x199A,
                Description = "CH3 EVENT功能",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "EventFunc_CH4",
                Address = 0x199B,
                Description = "CH4 EVENT功能",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "EventFunc_CH5",
                Address = 0x199C,
                Description = "CH5 EVENT功能",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "EventFunc_CH6",
                Address = 0x199D,
                Description = "CH6 EVENT功能",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "EventFunc_CH7",
                Address = 0x199E,
                Description = "CH7 EVENT功能",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "EventFunc_CH8",
                Address = 0x199F,
                Description = "CH8 EVENT功能",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },

            // ===== CT 电流检知 =====
            // CT 保持值 H19A0~H19A3 (FLOAT32)
            new ModbusRegister
            {
                Name = "CTStatic_CH1",
                Address = 0x19A0,
                Description = "CH1 CT保持值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CTStatic_CH2",
                Address = 0x19A1,
                Description = "CH2 CT保持值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CTStatic_CH3",
                Address = 0x19A2,
                Description = "CH3 CT保持值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CTStatic_CH4",
                Address = 0x19A3,
                Description = "CH4 CT保持值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            // CT 动态值 H19A4~H19A7 (FLOAT32)
            new ModbusRegister
            {
                Name = "CTDynamic_CH1",
                Address = 0x19A4,
                Description = "CH1 CT动态值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CTDynamic_CH2",
                Address = 0x19A5,
                Description = "CH2 CT动态值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CTDynamic_CH3",
                Address = 0x19A6,
                Description = "CH3 CT动态值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CTDynamic_CH4",
                Address = 0x19A7,
                Description = "CH4 CT动态值",
                DataType = "FLOAT32",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            // CT 调整值 H19A8~H19AB (INT16)
            new ModbusRegister
            {
                Name = "CTAdjust_CH1",
                Address = 0x19A8,
                Description = "CH1 CT调整值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CTAdjust_CH2",
                Address = 0x19A9,
                Description = "CH2 CT调整值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CTAdjust_CH3",
                Address = 0x19AA,
                Description = "CH3 CT调整值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "CTAdjust_CH4",
                Address = 0x19AB,
                Description = "CH4 CT调整值",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },

            // ===== 热流道控制参数 =====
            // 界限温度 H1960~H1967 (INT16, 0.1℃)
            new ModbusRegister
            {
                Name = "HRLimitTemp_CH1",
                Address = 0x1960,
                Description = "CH1 热流道界限温度",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "HRLimitTemp_CH2",
                Address = 0x1961,
                Description = "CH2 热流道界限温度",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "HRLimitTemp_CH3",
                Address = 0x1962,
                Description = "CH3 热流道界限温度",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "HRLimitTemp_CH4",
                Address = 0x1963,
                Description = "CH4 热流道界限温度",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "HRLimitTemp_CH5",
                Address = 0x1964,
                Description = "CH5 热流道界限温度",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "HRLimitTemp_CH6",
                Address = 0x1965,
                Description = "CH6 热流道界限温度",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "HRLimitTemp_CH7",
                Address = 0x1966,
                Description = "CH7 热流道界限温度",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },
            new ModbusRegister
            {
                Name = "HRLimitTemp_CH8",
                Address = 0x1967,
                Description = "CH8 热流道界限温度",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "℃"
            },

            // 固定输出量 H1968~H196F (INT16, 0.1%)
            new ModbusRegister
            {
                Name = "HRFixedOut_CH1",
                Address = 0x1968,
                Description = "CH1 热流道固定输出量",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "HRFixedOut_CH2",
                Address = 0x1969,
                Description = "CH2 热流道固定输出量",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "HRFixedOut_CH3",
                Address = 0x196A,
                Description = "CH3 热流道固定输出量",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "HRFixedOut_CH4",
                Address = 0x196B,
                Description = "CH4 热流道固定输出量",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "HRFixedOut_CH5",
                Address = 0x196C,
                Description = "CH5 热流道固定输出量",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "HRFixedOut_CH6",
                Address = 0x196D,
                Description = "CH6 热流道固定输出量",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "HRFixedOut_CH7",
                Address = 0x196E,
                Description = "CH7 热流道固定输出量",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "%"
            },
            new ModbusRegister
            {
                Name = "HRFixedOut_CH8",
                Address = 0x196F,
                Description = "CH8 热流道固定输出量",
                DataType = "INT16",
                ScalingFactor = 0.1,
                Unit = "%"
            },

            // 定时时间 H19B0~H19B7 (INT16, 分)
            new ModbusRegister
            {
                Name = "HRSoakTime_CH1",
                Address = 0x19B0,
                Description = "CH1 热流道定时时间",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "min"
            },
            new ModbusRegister
            {
                Name = "HRSoakTime_CH2",
                Address = 0x19B1,
                Description = "CH2 热流道定时时间",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "min"
            },
            new ModbusRegister
            {
                Name = "HRSoakTime_CH3",
                Address = 0x19B2,
                Description = "CH3 热流道定时时间",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "min"
            },
            new ModbusRegister
            {
                Name = "HRSoakTime_CH4",
                Address = 0x19B3,
                Description = "CH4 热流道定时时间",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "min"
            },
            new ModbusRegister
            {
                Name = "HRSoakTime_CH5",
                Address = 0x19B4,
                Description = "CH5 热流道定时时间",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "min"
            },
            new ModbusRegister
            {
                Name = "HRSoakTime_CH6",
                Address = 0x19B5,
                Description = "CH6 热流道定时时间",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "min"
            },
            new ModbusRegister
            {
                Name = "HRSoakTime_CH7",
                Address = 0x19B6,
                Description = "CH7 热流道定时时间",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "min"
            },
            new ModbusRegister
            {
                Name = "HRSoakTime_CH8",
                Address = 0x19B7,
                Description = "CH8 热流道定时时间",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = "min"
            },

            // ===== 功能旗标 H4824 (INT16, Bit0~Bit7) =====
            new ModbusRegister
            {
                Name = "FuncFlags",
                Address = 0x4824,
                Description = "功能旗标 Bit1=EVENT Bit2=CT Bit3=断电储存 Bit5=斜率 Bit6=热流道",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },

            // ===== 特殊功能开锁 H10F1 (INT16) =====
            new ModbusRegister
            {
                Name = "UnlockCode",
                Address = 0x10F1,
                Description = "特殊功能开锁码 写入1234H",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },

            // ===== 恢复出厂值 H10F2 (INT16) =====
            new ModbusRegister
            {
                Name = "FactoryReset",
                Address = 0x10F2,
                Description = "恢复出厂值 写入1357H",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },

            // ===== LED 状态 H1124~H1824 =====
            new ModbusRegister
            {
                Name = "LEDStatus_CH1",
                Address = 0x1124,
                Description = "CH1 LED状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "LEDStatus_CH2",
                Address = 0x1224,
                Description = "CH2 LED状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "LEDStatus_CH3",
                Address = 0x1324,
                Description = "CH3 LED状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "LEDStatus_CH4",
                Address = 0x1424,
                Description = "CH4 LED状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "LEDStatus_CH5",
                Address = 0x1524,
                Description = "CH5 LED状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "LEDStatus_CH6",
                Address = 0x1624,
                Description = "CH6 LED状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "LEDStatus_CH7",
                Address = 0x1724,
                Description = "CH7 LED状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
            new ModbusRegister
            {
                Name = "LEDStatus_CH8",
                Address = 0x1824,
                Description = "CH8 LED状态",
                DataType = "INT16",
                ScalingFactor = 1,
                Unit = string.Empty
            },
        };
        private readonly int _baudRate;
        private readonly string _comPort;
        private readonly int _dataBits;
        private bool _isConnected;
        private IModbusSerialMaster? _master;
        private readonly Parity _parity;

        private SerialPort _serialPort;
        private readonly byte _slaveId;
        private readonly StopBits _stopBits;

        public ModbusService(int slaveId, string comPort, int baudRate,
            string parity = "Even", int dataBits = 8, string stopBits = "1")
        {
            _slaveId = (byte)slaveId;
            _comPort = comPort;
            _baudRate = baudRate;
            _parity = ParseParity(parity);
            _dataBits = dataBits;
            _stopBits = ParseStopBits(stopBits);
        }

        ///<summary>
        /// float → 2 个 ushort (大端 ABCD)
        ///</summary>
        private static ushort[] FloatToUshortArray(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return new ushort[]
            {
                (ushort)((bytes[0] << 8) | bytes[1]),
                (ushort)((bytes[2] << 8) | bytes[3])
            };
        }

        ///<summary>
        /// 将 ushort 数组解析为 double 数组 (每 2 个 ushort = 1 个 FLOAT32)
        ///</summary>
        private static double[] ParseFloats(ushort[] raw, int count)
        {
            var result = new double[count];
            for(int i = 0; i < count; i++)
            {
                result[i] = ParseFloatWithScaling(raw[i * 2], raw[i * 2 + 1], 1.0);
            }
            return result;
        }

        // ========== 工具方法 ==========

        ///<summary>
        /// 将 2 个 ushort (大端 ABCD) 转为 float, 再乘以缩放因子
        ///</summary>
        private static double ParseFloatWithScaling(ushort high, ushort low, double scaling)
        {
            byte[] bytes = new byte[4];
            bytes[0] = (byte)(high >> 8);
            bytes[1] = (byte)(high & 0xFF);
            bytes[2] = (byte)(low >> 8);
            bytes[3] = (byte)(low & 0xFF);
            float value = BitConverter.ToSingle(bytes, 0);
            return Math.Round(value * scaling, 2);
        }

        private static Parity ParseParity(string parity)
        {
            return parity?.ToLower() switch
            {
                "none" or "n" => Parity.None,
                "odd" or "o" => Parity.Odd,
                _ => Parity.Even
            };
        }

        private static StopBits ParseStopBits(string stopBits)
        {
            return stopBits switch
            {
                "2" => StopBits.Two,
                _ => StopBits.One
            };
        }

        private void ThrowIfNotConnected()
        {
            if(!_isConnected || _master == null)
            {
                throw new InvalidOperationException("Modbus 未连接，请先调用 ConnectAsync()");
            }
        }

        ///<summary>
        /// 读取错误码 (H1000~H1007 在错误时返回 H8001~H8003)
        ///</summary>
        public async Task<(bool hasError, ushort[] errorCodes)> CheckErrorsAsync()
        {
            var raw = await ReadHoldingRegistersAsync(0x1000, 8);
            bool hasError = false;
            for(int i = 0; i < raw.Length; i++)
            {
                if(raw[i] >= 0x8001 && raw[i] <= 0x8003)
                {
                    hasError = true;
                    break;
                }
            }
            return (hasError, raw);
        }

        ///<summary>
        /// 打开串口并初始化 Modbus RTU Master
        ///</summary>
        public async Task<bool> ConnectAsync()
        {
            return await Task.Run(() => {
                       try
                       {
                           _serialPort = new SerialPort(_comPort, _baudRate, _parity, _dataBits, _stopBits)
                           {
                               ReadTimeout = 5000,
                               WriteTimeout = 5000
                           };
                           _serialPort.Open();

                           _master = new ModbusFactory().CreateRtuMaster(new SerialStreamResource(_serialPort));
                           _master.Transport.ReadTimeout = 5000;
                           _master.Transport.WriteTimeout = 5000;

                           _isConnected = true;
                           return true;
                       }
                       catch(Exception ex)
                       {
                           System.Diagnostics.Debug.WriteLine($"[Modbus] 连接失败: {ex.Message}");
                           Dispose();
                           Application.Current?.Dispatcher?.Invoke(() =>
                           {
                               MessageBox.Show($"连接失败\n\n错误信息: {ex.Message}\n\n异常类型: {ex.GetType().Name}\n\n堆栈跟踪:\n{ex.StackTrace}", 
                                   "Modbus 连接错误", 
                                   MessageBoxButton.OK, 
                                   MessageBoxImage.Error);
                           });
                           return false;
                       }
            });
        }

        ///<summary>
        /// 断开连接并释放资源
        ///</summary>
        public void Disconnect()
        {
            try
            {
                _master?.Dispose();
                _serialPort?.Close();
                _serialPort?.Dispose();
            }
            catch
            {
            }
            finally
            {
                _master = null;
                _serialPort = null;
                _isConnected = false;
            }
        }

        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }

        ///<summary>
        /// 将缩放后的工程值转换为设备原始值写入
        ///</summary>
        public static ushort EncodeScaledInt(double engineeringValue, double scaling)
        { return (ushort)Math.Round(engineeringValue / scaling); }

        ///<summary>
        /// 恢复出厂值 (写入 0x1357 到 H10F2)
        ///</summary>
        public async Task<bool> FactoryResetAsync() { return await WriteSingleRegisterAsync(0x10F2, 0x1357); }

        ///<summary>
        /// 读取警报上下限值
        ///</summary>
        public async Task<(double[] high, double[] low)> ReadAllAlarmLimitsAsync()
        {
            var rawHigh = await ReadHoldingRegistersAsync(0x1080, 16);
            var rawLow = await ReadHoldingRegistersAsync(0x1088, 16);
            return (ParseFloats(rawHigh, 8), ParseFloats(rawLow, 8));
        }

        ///<summary>
        /// 读取 LED 状态 (H1124 + channel*0x100)
        ///</summary>
        public async Task<ushort[]> ReadAllLEDStatusAsync()
        {
            var results = new ushort[8];
            for(int i = 0; i < 8; i++)
            {
                int addr = 0x1124 + i * 0x100;
                results[i] = await ReadHoldingRegisterAsync(addr);
            }
            return results;
        }

        // ========== DTE10T 高级业务方法 ==========

        ///<summary>
        /// 读取全部 8 通道 PV 值 (H1000~H1007, 每通道 2 寄存器 = FLOAT32)
        ///</summary>
        public async Task<double[]> ReadAllPVAsync()
        {
            var raw = await ReadHoldingRegistersAsync(0x1000, 16); // 8ch × 2reg
            return ParseFloats(raw, 8);
        }

        ///<summary>
        /// 读取全部 8 通道 SV 值 (H1008~H100F)
        ///</summary>
        public async Task<double[]> ReadAllSVAsync()
        {
            var raw = await ReadHoldingRegistersAsync(0x1008, 16);
            return ParseFloats(raw, 8);
        }

        ///<summary>
        /// 读取通讯参数 (H10F8~H10FF)
        ///</summary>
        public async Task<Dictionary<string, ushort>> ReadCommParamsAsync()
        {
            var result = new Dictionary<string, ushort>();
            var addresses = new[] { 0x10F8, 0x10F9, 0x10FA, 0x10FB, 0x10FC, 0x10FD, 0x10FE, 0x10FF };
            var names = new[] { "自动站号", "滤波范围", "波特率", "协议格式", "数据位", "停止位", "校验位", "站号" };

            for(int i = 0; i < addresses.Length; i++)
            {
                result[names[i]] = await ReadHoldingRegisterAsync(addresses[i]);
            }
            return result;
        }

        // ========== 基础读写方法 ==========

        ///<summary>
        /// 读取单个保持寄存器 (功能码 0x03)
        ///</summary>
        public async Task<ushort> ReadHoldingRegisterAsync(int address)
        {
            ThrowIfNotConnected();
            return await Task.Run(() => {
                       try
                       {
                           var result = _master!.ReadHoldingRegisters(_slaveId, (ushort)address, 1);
                           return result[0];
                       }
                       catch(Exception ex)
                       {
                           System.Diagnostics.Debug.WriteLine($"[Modbus] 读取寄存器失败 @{address:X4}: {ex.Message}");
                           Application.Current?.Dispatcher?.Invoke(() =>
                           {
                               MessageBox.Show($"读取寄存器失败\n\n地址: 0x{address:X4}\n\n错误信息: {ex.Message}\n\n异常类型: {ex.GetType().Name}\n\n堆栈跟踪:\n{ex.StackTrace}", 
                                   "Modbus 读取错误", 
                                   MessageBoxButton.OK, 
                                   MessageBoxImage.Error);
                           });
                           throw;
                       }
            });
        }

        ///<summary>
        /// 读取多个连续保持寄存器 (功能码 0x03)
        ///</summary>
        public async Task<ushort[]> ReadHoldingRegistersAsync(int startAddress, int count)
        {
            ThrowIfNotConnected();
            return await Task.Run(() => {
                       try
                       {
                           return _master!.ReadHoldingRegisters(_slaveId, (ushort)startAddress, (ushort)count);
                       }
                       catch(Exception ex)
                       {
                           System.Diagnostics.Debug.WriteLine($"[Modbus] 批量读取寄存器失败 @{startAddress:X4}, count={count}: {ex.Message}");
                           Application.Current?.Dispatcher?.Invoke(() =>
                           {
                               MessageBox.Show($"批量读取寄存器失败\n\n起始地址: 0x{startAddress:X4}\n数量: {count}\n\n错误信息: {ex.Message}\n\n异常类型: {ex.GetType().Name}\n\n堆栈跟踪:\n{ex.StackTrace}", 
                                   "Modbus 读取错误", 
                                   MessageBoxButton.OK, 
                                   MessageBoxImage.Error);
                           });
                           throw;
                       }
            });
        }

        ///<summary>
        /// 设置警报一输出模式 (0~13)
        ///</summary>
        public async Task<bool> SetAlarm1ModeAsync(int channel, int mode)
        {
            int addr = 0x10C0 + channel;
            return await WriteSingleRegisterAsync(addr, (ushort)mode);
        }

        ///<summary>
        /// 设置通道禁能/使能 (H10F6, Bit0=CH1 ... Bit7=CH8)
        ///</summary>
        public async Task<bool> SetChannelDisableAsync(ushort bitmask)
        { return await WriteSingleRegisterAsync(0x10F6, bitmask); }

        ///<summary>
        /// 执行控制 (1) / 停止 (0) — H10D8 + channel*0x100
        ///</summary>
        public async Task<bool> SetControlExecAsync(int channel, int exec)
        {
            int addr = 0x10D8 + channel * 0x100;
            return await WriteSingleRegisterAsync(addr, (ushort)exec);
        }

        ///<summary>
        /// 设置控制方式 (0=PID, 1=ON-OFF, 2=Manual, 3=可程序PID)
        /// 地址: H10B8 + channel*0x100
        ///</summary>
        public async Task<bool> SetControlModeAsync(int channel, int mode)
        {
            int addr = 0x10B8 + channel * 0x100;
            return await WriteSingleRegisterAsync(addr, (ushort)mode);
        }

        ///<summary>
        /// 设置输出1控制选择 (0=加热, 1=冷却, 2=比例输出)
        ///</summary>
        public async Task<bool> SetOut1ControlAsync(int channel, int value)
        {
            int addr = 0x10A8 + channel;
            return await WriteSingleRegisterAsync(addr, (ushort)value);
        }

        ///<summary>
        /// 设置输出2控制选择 (0=加热, 1=冷却, 2=警报)
        ///</summary>
        public async Task<bool> SetOut2ControlAsync(int channel, int value)
        {
            int addr = 0x10B0 + channel;
            return await WriteSingleRegisterAsync(addr, (ushort)value);
        }

        ///<summary>
        /// 启动 AT 自整定 (写入 1 到对应通道的 H10E0~H10E7)
        ///</summary>
        public async Task<bool> StartATAsync(int channel)
        {
            int addr = 0x10E0 + channel;
            return await WriteSingleRegisterAsync(addr, 1);
        }

        ///<summary>
        /// 停止 AT 自整定
        ///</summary>
        public async Task<bool> StopATAsync(int channel)
        {
            int addr = 0x10E0 + channel;
            return await WriteSingleRegisterAsync(addr, 0);
        }

        ///<summary>
        /// 解锁特殊功能 (写入 0x1234 到 H10F1)
        ///</summary>
        public async Task<bool> UnlockSpecialFunctionsAsync() { return await WriteSingleRegisterAsync(0x10F1, 0x1234); }

        ///<summary>
        /// 写入警报上限值
        ///</summary>
        public async Task<bool> WriteAlarmHighAsync(int channel, double value)
        {
            int addr = 0x1080 + channel;
            ushort[] regs = FloatToUshortArray((float)value);
            return await WriteMultipleRegistersAsync(addr, regs);
        }

        ///<summary>
        /// 写入警报下限值
        ///</summary>
        public async Task<bool> WriteAlarmLowAsync(int channel, double value)
        {
            int addr = 0x1088 + channel;
            ushort[] regs = FloatToUshortArray((float)value);
            return await WriteMultipleRegistersAsync(addr, regs);
        }

        ///<summary>
        /// 写入通讯参数
        ///</summary>
        public async Task<bool> WriteCommParamAsync(string paramName, ushort value)
        {
            var paramMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "自动站号", 0x10F8 }, { "滤波范围", 0x10F9 }, { "波特率", 0x10FA },
                { "协议格式", 0x10FB }, { "数据位", 0x10FC }, { "停止位", 0x10FD },
                { "校验位", 0x10FE }, { "站号", 0x10FF }
            };
            if(!paramMap.TryGetValue(paramName, out int addr))
            {
                throw new ArgumentException($"未知通讯参数: {paramName}");
            }

            return await WriteSingleRegisterAsync(addr, value);
        }

        ///<summary>
        /// 写入多个保持寄存器 (功能码 0x10)
        ///</summary>
        public async Task<bool> WriteMultipleRegistersAsync(int startAddress, ushort[] values)
        {
            ThrowIfNotConnected();
            return await Task.Run(() => {
                       try
                       {
                           _master!.WriteMultipleRegisters(_slaveId, (ushort)startAddress, values);
                           return true;
                       }
                       catch(Exception ex)
                       {
                           System.Diagnostics.Debug.WriteLine($"[Modbus] 批量写入失败 @{startAddress:X4}: {ex.Message}");
                           Application.Current?.Dispatcher?.Invoke(() =>
                           {
                               MessageBox.Show($"批量写入寄存器失败\n\n起始地址: 0x{startAddress:X4}\n数量: {values.Length}\n\n错误信息: {ex.Message}\n\n异常类型: {ex.GetType().Name}\n\n堆栈跟踪:\n{ex.StackTrace}", 
                                   "Modbus 写入错误", 
                                   MessageBoxButton.OK, 
                                   MessageBoxImage.Error);
                           });
                           return false;
                       }
            });
        }

        ///<summary>
        /// 写入输出1量 (手动模式) — H1070 + channel
        ///</summary>
        public async Task<bool> WriteOutput1Async(int channel, double value)
        {
            int addr = 0x1070 + channel;
            ushort[] regs = FloatToUshortArray((float)value);
            return await WriteMultipleRegistersAsync(addr, regs);
        }

        ///<summary>
        /// 写入输出2量 (手动模式) — H1078 + channel
        ///</summary>
        public async Task<bool> WriteOutput2Async(int channel, double value)
        {
            int addr = 0x1078 + channel;
            ushort[] regs = FloatToUshortArray((float)value);
            return await WriteMultipleRegistersAsync(addr, regs);
        }

        ///<summary>
        /// 写入 PID 参数 (Pb, Ti, Td — 各 FLOAT32, 共 6 寄存器)
        /// 使用按功能顺序编码地址: H1028 + channel*0x100
        ///</summary>
        public async Task<bool> WritePIDAsync(int channel, double pb, double ti, double td)
        {
            int baseAddr = 0x1028 + channel * 0x100;
            var data = new List<ushort>();
            data.AddRange(FloatToUshortArray((float)pb));
            data.AddRange(FloatToUshortArray((float)ti));
            data.AddRange(FloatToUshortArray((float)td));
            return await WriteMultipleRegistersAsync(baseAddr, data.ToArray());
        }

        ///<summary>
        /// 写入单个保持寄存器 (功能码 0x06)
        ///</summary>
        public async Task<bool> WriteSingleRegisterAsync(int address, ushort value)
        {
            ThrowIfNotConnected();
            return await Task.Run(() => {
                       try
                       {
                           _master!.WriteSingleRegister(_slaveId, (ushort)address, value);
                           return true;
                       }
                       catch(Exception ex)
                       {
                           System.Diagnostics.Debug.WriteLine($"[Modbus] 写入失败 @{address:X4}: {ex.Message}");
                           Application.Current?.Dispatcher?.Invoke(() =>
                           {
                               MessageBox.Show($"写入寄存器失败\n\n地址: 0x{address:X4}\n值: {value}\n\n错误信息: {ex.Message}\n\n异常类型: {ex.GetType().Name}\n\n堆栈跟踪:\n{ex.StackTrace}", 
                                   "Modbus 写入错误", 
                                   MessageBoxButton.OK, 
                                   MessageBoxImage.Error);
                           });
                           return false;
                       }
            });
        }

        ///<summary>
        /// 写入 SV 设定值 (FLOAT32 = 2 寄存器)
        ///</summary>
        public async Task<bool> WriteSVAsync(int channel, double value)
        {
            int addr = 0x1008 + channel;
            ushort[] regs = FloatToUshortArray((float)value);
            return await WriteMultipleRegistersAsync(addr, regs);
        }

        public async Task<bool> WriteRangeHighAsync(int channel, double value)
        {
            int addr = 0x1010 + channel;
            ushort[] regs = FloatToUshortArray((float)value);
            return await WriteMultipleRegistersAsync(addr, regs);
        }

        public async Task<bool> WriteRangeLowAsync(int channel, double value)
        {
            int addr = 0x1018 + channel;
            ushort[] regs = FloatToUshortArray((float)value);
            return await WriteMultipleRegistersAsync(addr, regs);
        }

        public async Task<bool> WritePbAsync(int channel, double value)
        {
            int addr = 0x1028 + channel;
            ushort[] regs = FloatToUshortArray((float)value);
            return await WriteMultipleRegistersAsync(addr, regs);
        }

        public async Task<bool> WriteTiAsync(int channel, double value)
        {
            int addr = 0x1030 + channel;
            ushort[] regs = FloatToUshortArray((float)value);
            return await WriteMultipleRegistersAsync(addr, regs);
        }

        public async Task<bool> WriteTdAsync(int channel, double value)
        {
            int addr = 0x1038 + channel;
            ushort[] regs = FloatToUshortArray((float)value);
            return await WriteMultipleRegistersAsync(addr, regs);
        }

        public async Task<bool> WriteAlarmDelayAsync(int channel, double value)
        {
            int addr = 0x1990 + channel;
            return await WriteSingleRegisterAsync(addr, (ushort)value);
        }

        public async Task<bool> WriteOutMaxAsync(int channel, double value)
        {
            int addr = 0x1980 + channel;
            return await WriteSingleRegisterAsync(addr, (ushort)value);
        }

        public async Task<bool> WriteOutMinAsync(int channel, double value)
        {
            int addr = 0x1988 + channel;
            return await WriteSingleRegisterAsync(addr, (ushort)value);
        }

        public async Task<bool> WriteSlopeAsync(int channel, double value)
        {
            int addr = 0x1970 + channel;
            return await WriteSingleRegisterAsync(addr, (ushort)value);
        }

        public async Task<bool> WriteOffsetAsync(int channel, double value)
        {
            int addr = 0x1020 + channel;
            return await WriteSingleRegisterAsync(addr, (ushort)value);
        }

        public async Task<bool> WriteGainAsync(int channel, double value)
        {
            int addr = 0x19B8 + channel;
            return await WriteSingleRegisterAsync(addr, (ushort)value);
        }

        public async Task<bool> WriteFilterCountAsync(int value)
        {
            return await WriteSingleRegisterAsync(0x10F7, (ushort)value);
        }

        public async Task<bool> WriteFilterRangeAsync(double value)
        {
            ushort[] regs = FloatToUshortArray((float)value);
            return await WriteMultipleRegistersAsync(0x10F9, regs);
        }

        public async Task<bool> WriteCTAdjustAsync(int channel, double value)
        {
            int addr = 0x19A8 + channel;
            return await WriteSingleRegisterAsync(addr, (ushort)value);
        }

        public async Task<bool> WriteEventFunctionAsync(int channel, int value)
        {
            int addr = 0x1998 + channel;
            return await WriteSingleRegisterAsync(addr, (ushort)value);
        }

        public async Task<bool> WriteHRLimitTempAsync(int channel, double value)
        {
            int addr = 0x19E0 + channel;
            return await WriteSingleRegisterAsync(addr, (ushort)value);
        }

        public async Task<bool> WriteHRFixedOutputAsync(int channel, double value)
        {
            int addr = 0x19E8 + channel;
            return await WriteSingleRegisterAsync(addr, (ushort)value);
        }

        public async Task<bool> WriteHRSoakTimeAsync(int channel, double value)
        {
            int addr = 0x19F0 + channel;
            return await WriteSingleRegisterAsync(addr, (ushort)value);
        }

        public bool IsConnected => _isConnected;

        public IReadOnlyList<ModbusRegister> RegisterMap => _registerMap;
    }
}
