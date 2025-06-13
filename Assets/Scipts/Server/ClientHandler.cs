using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BattleSystem.Server
{
    public class ClientHandler
    {
        public string Id { get; private set; }
        public string PlayerClass { get; set; }
        public string TeamId { get; set; }
        public bool IsTeamLeader { get; set; }
      public string RoomCode { get; set; }
        
        private TcpClient tcpClient;
        private NetworkStream stream;
        private bool isConnected;
        
        public event Action<string, string> OnMessageReceived;
        public event Action<string> OnDisconnected;
        
        public ClientHandler(TcpClient client)
        {
            Id = Guid.NewGuid().ToString();
            tcpClient = client;
            stream = client.GetStream();
            isConnected = true;
            
            _ = Task.Run(ListenForMessages);
        }
        
        private async Task ListenForMessages()
        {
            byte[] buffer = new byte[4096];
            
            try
            {
                while (isConnected && tcpClient.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    OnMessageReceived?.Invoke(Id, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client {Id} error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }
        
        public async Task SendMessage(string message)
        {
            if (!isConnected || !tcpClient.Connected) return;
            
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error to {Id}: {ex.Message}");
                Disconnect();
            }
        }
        
        public async Task SendResponse(ServerResponse response)
        {
            string json = JsonConvert.SerializeObject(response);
            await SendMessage(json);
        }
        
        public void Disconnect()
        {
            if (!isConnected) return;
            
            isConnected = false;
            stream?.Close();
            tcpClient?.Close();
            
            OnDisconnected?.Invoke(Id);
        }
    }
}
