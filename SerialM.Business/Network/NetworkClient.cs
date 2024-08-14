using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SerialM.Business.Network
{
    public class NetworkClient
    {
        private TcpClient _client;

        /// <summary>
        /// Occurs when the client successfully connects to a server.
        /// </summary>
        public event Action<string>? OnClientConnected;

        /// <summary>
        /// Occurs when a message is received from the server.
        /// </summary>
        public event Action<string>? OnMessageReceived;

        public async Task ConnectToServer(string ipAddress, int port)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(IPAddress.Parse(ipAddress), port);
            OnClientConnected?.Invoke("Connected to server...");
        }

        public async Task SendMessageAsync(string message)
        {
            if (_client == null)
                throw new InvalidOperationException("Not connected to a server.");

            var stream = _client.GetStream();
            var data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
        }

        public async Task ReceiveMessageAsync()
        {
            if (_client == null)
                throw new InvalidOperationException("Not connected to a server.");

            var stream = _client.GetStream();
            var buffer = new byte[1024];

            while (true)
            {
                var byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (byteCount == 0)
                    break; // Server disconnected

                var message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                OnMessageReceived?.Invoke(message);
            }
        }
    }
}
