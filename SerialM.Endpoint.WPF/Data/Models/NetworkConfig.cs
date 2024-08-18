using SerialM.Business.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialM.Endpoint.WPF.Data.Models
{
    public class NetworkConfig
    {
        public string IP { get; set; } = string.Empty;
        public int Port { get; set; }
        public NetworkMode NetworkMode { get; set; }
    }
}
