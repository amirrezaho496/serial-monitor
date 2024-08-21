using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialM.Business.Network.Interfaces
{
    public interface INetworkDevice : IDisposable
    {
        public bool Connected { get;}
        public void StartAsync(string ipAddress, int port);
        public void SendMessageAsync(string message);

    }
}
