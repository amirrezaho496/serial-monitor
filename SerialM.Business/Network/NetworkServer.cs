using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SerialM.Business.Network.Interfaces;
using System.Threading;

namespace SerialM.Business.Network
{
    public class NetworkServer : INetworkDevice
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
        
        /// <summary>
        /// Event triggered when a client disconnects from the server.
        /// </summary>
        public event Action<string>? OnStartError;


        private List<TcpClient> _clients;
        private Task? _acceptTcpClients;
        private CancellationTokenSource _cancellationTokenSource;


        public bool Connected => _clients.Count > 0;

        public NetworkServer()
        {
            _clients = new();
        }

        public async void StartAsync(string ipAddress, int port)
        {
            _server = new TcpListener(IPAddress.Parse(ipAddress), port);

            try
            {
                _server.Start();
                OnServerStarted?.Invoke("Server started...");
            }
            catch (Exception ex)
            {
                OnStartError?.Invoke(ex.Message);
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;

            _acceptTcpClients = Task.Run(async () =>
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var client = await _server.AcceptTcpClientAsync();
                        _clients.Add(client);
                        HandleClientAsync(client);
                    }
                }
                catch (Exception ex)
                {
                    OnStartError?.Invoke($"Error in accepting clients: {ex.Message}");
                }
            }, cancellationToken);

            await _acceptTcpClients;
        }

        public void Stop()
        {
            if (_server != null && _cancellationTokenSource != null)
            {
                _server.Stop();
                _cancellationTokenSource.Cancel();
            }
        }

        private async void HandleClientAsync(TcpClient client)
        {
            await Task.Run(async () =>
            {
                OnClientConnected?.Invoke($"Client connected ...");

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
                    //await stream.WriteAsync(buffer, 0, byteCount);
                }

                _clients.Remove(client);
                client.Close();
                OnClientDisconnected?.Invoke("Client disconnected...");
            });

        }

        public async void SendMessageAsync(string message)
        {
            var data = Encoding.UTF8.GetBytes(message);
            var disconnectedClients = new List<TcpClient>();

            foreach (var client in _clients)
            {
                try
                {
                    var stream = client.GetStream();
                    await stream.WriteAsync(data, 0, data.Length);
                }
                catch (Exception)
                {
                    disconnectedClients.Add(client);
                }
            }

            // Remove any clients that were found to be disconnected during the send
            foreach (var client in disconnectedClients)
            {
                _clients.Remove(client);
                client.Close();
                OnClientDisconnected?.Invoke("Client disconnected during send...");
            }
        }

        public void Dispose()
        {
            Stop();
            foreach (var client in _clients)
            {
                client.Close();
                client.GetStream().Dispose();
                client.Dispose();
            }
            _clients.Clear();
            _server.Dispose();
        }
    }

}