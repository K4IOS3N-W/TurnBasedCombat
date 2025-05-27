using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BattleSystem.Server
{
    public class ClientConnection
    {
        private readonly TcpClient tcpClient;
        private NetworkStream stream;
        private readonly byte[] receiveBuffer = new byte[4096];
        private bool isConnected = true;

        public string Id { get; }

        // Eventos
        public event Action<string> OnMessageReceived;
        public event ClientDisconnectedHandler OnDisconnected;

        public ClientConnection(TcpClient client, string id)
        {
            tcpClient = client;
            Id = id;
            stream = client.GetStream();
        }

        public async Task StartListeningAsync()
        {
            try
            {
                while (isConnected && tcpClient.Connected)
                {
                    // Configurar um timeout para a leitura
                    using (var cts = new System.Threading.CancellationTokenSource(30000))
                    {
                        int bytesRead = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length, cts.Token);
                        
                        if (bytesRead <= 0)
                        {
                            Disconnect(DisconnectReason.ConnectionLost);
                            break;
                        }

                        string message = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
                        OnMessageReceived?.Invoke(message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout na operação de leitura
                Disconnect(DisconnectReason.Timeout);
            }
            catch (IOException)
            {
                // Provavelmente a conexão foi fechada
                Disconnect(DisconnectReason.ConnectionLost);
            }
            catch (ObjectDisposedException)
            {
                // Stream já foi fechado
                Disconnect(DisconnectReason.ConnectionLost);
            }
            catch (Exception)
            {
                // Outros erros não esperados
                Disconnect(DisconnectReason.Error);
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (!isConnected || stream == null)
                return;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                
                // Usar timeout no envio
                using (var cts = new System.Threading.CancellationTokenSource(10000))
                {
                    await stream.WriteAsync(data, 0, data.Length, cts.Token);
                    await stream.FlushAsync(cts.Token);
                }
            }
            catch (Exception)
            {
                Disconnect(DisconnectReason.Error);
            }
        }

        public void Disconnect(DisconnectReason reason)
        {
            if (!isConnected)
                return;

            isConnected = false;

            try
            {
                stream?.Close();
                tcpClient?.Close();
            }
            catch (Exception) { }

            OnDisconnected?.Invoke(Id, reason);
        }

        public bool IsConnected => isConnected && tcpClient != null && tcpClient.Connected;
    }
}