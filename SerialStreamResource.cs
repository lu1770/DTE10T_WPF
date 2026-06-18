using NModbus.IO;
using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace DTE10T_WPF
{
    internal class SerialStreamResource : IStreamResource
    {
        private readonly SerialPort _serialPort;

        public SerialStreamResource(SerialPort serialPort) { _serialPort = serialPort; }

        public void DiscardInBuffer() { _serialPort.DiscardInBuffer(); }

        public void Dispose() { _serialPort.Dispose(); }

        public int Read(byte[] buffer, int offset, int count) { return _serialPort.Read(buffer, offset, count); }

        public void Write(byte[] buffer, int offset, int count) { _serialPort.Write(buffer, offset, count); }

        public int InfiniteTimeout => Timeout.Infinite;

        public int ReadTimeout { get => _serialPort.ReadTimeout; set => _serialPort.ReadTimeout = value; }

        public Stream Stream => _serialPort.BaseStream;

        public int WriteTimeout { get => _serialPort.WriteTimeout; set => _serialPort.WriteTimeout = value; }
    }
}
