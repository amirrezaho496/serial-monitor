using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialM.Endpoint.WPF
{
    public class SerialConfig
    {
        public string PortName { get; set; } = string.Empty;
        public int BaudRate { get; set; }
        public Parity Parity { get; set; }
        public StopBits StopBits { get; set; }
    }
}
