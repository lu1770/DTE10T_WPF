
namespace DTE10T_WPF.Config
{
    public class SerialPortSettings
    {
        public int BaudRate { get; set; } = 115200;

        public int DataBits { get; set; } = 8;

        public string Parity { get; set; } = "Even";

        public string PortName { get; set; } = "COM1";

        public string Protocol { get; set; } = "RTU";

        public int SlaveId { get; set; } = 1;

        public string StopBits { get; set; } = "1";
    }
}
