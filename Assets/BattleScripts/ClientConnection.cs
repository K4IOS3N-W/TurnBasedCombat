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

        // Events
        public event Action<string, string> OnMessageReceived;
        public event ClientDisconnectedHandler OnDisconnected;

        public bool IsConnected => isConnected && tcpClient != null && tcpClient.Connected;

        public ClientConnection(TcpClient client)
        {
            tcpClient = client;
            Id = Guid.NewGuid().ToString();
            stream = tcpClient.GetStream();
        }

        public async Task StartAsync()
        {
            try
            {
                _ = Task.Run(ReceiveMessagesAsync);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting client connection: {ex.Message}");
                Disconnect();
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            var messageBuffer = new StringBuilder();

            while (isConnected && tcpClient.Connected)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
                    
                    if (bytesRead == 0)
                    {
                        // Client disconnected
                        break;
                    }

                    string receivedData = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
                    messageBuffer.Append(receivedData);

                    // Process complete messages (assuming messages are separated by newlines)
                    string bufferContent = messageBuffer.ToString();
                    string[] messages = bufferContent.Split('\n');

                    // Process all complete messages except the last one (which might be incomplete)
                    for (int i = 0; i < messages.Length - 1; i++)
                    {
                        string message = messages[i].Trim();
                        if (!string.IsNullOrEmpty(message))
                        {
                            OnMessageReceived?.Invoke(Id, message);
                        }
                    }

                    // Keep the last (potentially incomplete) message in the buffer
                    messageBuffer.Clear();
                    if (messages.Length > 0)
                    {
                        messageBuffer.Append(messages[messages.Length - 1]);
                    }
                }
                catch (IOException)
                {
                    // Connection was closed
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message from client {Id}: {ex.Message}");
                    break;
                }
            }

            Disconnect();
        }

        public async Task SendMessageAsync(string message)
        {
            if (!IsConnected) return;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message to client {Id}: {ex.Message}");
                Disconnect();
            }
        }

        public void SendMessage(string message)
        {
            _ = Task.Run(() => SendMessageAsync(message));
        }

        public void Disconnect()
        {
            if (!isConnected) return;

            isConnected = false;

            try
            {
                stream?.Close();
                tcpClient?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting client {Id}: {ex.Message}");
            }

            OnDisconnected?.Invoke(Id);
        }
    }
}