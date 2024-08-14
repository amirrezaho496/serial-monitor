using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SerialM.Business.Network
{
    public class NetworkServer
    {
        private TcpListener _server;

        /// <summary>
        /// Event triggered when the server starts successfully.
        /// </summary>
        public event Action<string>? OnServerStarted;

        /// <summary>
        /// Event triggered when a client connects to the server.
        /// </summary>
        public event Action<string>? OnClientConnected;

        /// <summary>
        /// Event triggered when a message is received from a client or server.
        /// </summary>
        public event Action<string>? OnMessageReceived;

        /// <summary>
        /// Event triggered when a client disconnects from the server.
        /// </summary>
        public event Action<string>? OnClientDisconnected;

        public async Task StartServer(string ipAddress, int port)
        {
            _server = new TcpListener(IPAddress.Parse(ipAddress), port);
            _server.Start();
            OnServerStarted?.Invoke("Server started...");

            while (true)
            {
                var client = await _server.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            OnClientConnected?.Invoke("Client connected...");

            var stream = client.GetStream();
            var buffer = new byte[1024];

            while (true)
            {
                var byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (byteCount == 0)
                    break; // Client disconnected

                var message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                OnMessageReceived?.Invoke(message);

                // Echo the message back to the client (or handle the message differently)
                await stream.WriteAsync(buffer, 0, byteCount);
            }

            client.Close();
            OnClientDisconnected?.Invoke("Client disconnected...");
        }
    }

}