namespace DTE10T_WPF
{
    public class SerialPortSettings
    {
        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public string Parity { get; set; } = "Even";
        public int DataBits { get; set; } = 8;
        public string StopBits { get; set; } = "1";
        public string Protocol { get; set; } = "RTU";
        public int SlaveId { get; set; } = 1;
    }
}
