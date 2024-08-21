using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SerialM.Business.Network.Interfaces;

namespace SerialM.Business.Network
{
    public class NetworkClient : INetworkDevice
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

        /// <summary>
        /// Occurs when the client gets disconnected from the server.
        /// </summary>
        public event Action<string>? OnClientDisconnected;

        /// <summary>
        /// Event triggered when a client disconnects from the server.
        /// </summary>
        public event Action<string>? OnStartError;


        public bool Connected => _client.Connected;

        public async void StartAsync(string ipAddress, int port)
        {
            _client = new TcpClient();
            try
            {
                await _client.ConnectAsync(IPAddress.Parse(ipAddress), port);
            }
            catch (Exception ex)
            {
                OnStartError?.Invoke(ex.Message);
                return;
            }
            OnClientConnected?.Invoke($"Connected to {ipAddress}:{port}");
            ReceiveMessageAsync();
        }

        public async void SendMessageAsync(string message)
        {
            if (_client == null || !_client.Connected)
                OnClientDisconnected?.Invoke("Not connected to a server.");

            var stream = _client.GetStream();
            var data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
        }

        public async void ReceiveMessageAsync()
        {
            if (_client == null)
                OnClientDisconnected?.Invoke("Not connected to a server.");

            var stream = _client.GetStream();
            var buffer = new byte[2048];

            await Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (byteCount == 0)
                        {
                            OnClientDisconnected?.Invoke("Server Disconnected ...");
                            break; // Server disconnected
                        }

                        var message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                        OnMessageReceived?.Invoke(message);
                    }
                    catch (Exception ex)
                    {
                        OnClientDisconnected?.Invoke($"Disconnected : {ex.Message}");
                        break;
                    }
                }
            });
        }

        public void Dispose()
        {
            _client.Close();
            _client.Dispose();
        }
    }
}
