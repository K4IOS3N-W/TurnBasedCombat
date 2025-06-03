using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace BattleSystem.Server
{
    public delegate void ClientDisconnectedHandler(string clientId);

    public class BattleServer
    {
        private TcpListener tcpListener;
        private bool isRunning = false;
        private readonly Dictionary<string, ClientConnection> clients = new Dictionary<string, ClientConnection>();
        private readonly Dictionary<string, GameManager> activeGames = new Dictionary<string, GameManager>();
        private readonly Dictionary<string, string> clientGameMapping = new Dictionary<string, string>();

        public event Action<string> OnServerMessage;
        public event Action<string> OnClientConnected;
        public event Action<string> OnClientDisconnected;

        public bool IsRunning => isRunning;
        public int Port { get; private set; }
        public int ConnectedClients => clients.Count;
        public int ActiveGamesCount => activeGames.Count;

        public BattleServer(int port = 7777)
        {
            Port = port;
        }

        public async Task StartAsync()
        {
            if (isRunning) return;

            try
            {
                tcpListener = new TcpListener(IPAddress.Any, Port);
                tcpListener.Start();
                isRunning = true;

                OnServerMessage?.Invoke($"Battle Server started on port {Port}");

                // Start accepting clients
                _ = Task.Run(AcceptClientsAsync);
            }
            catch (Exception ex)
            {
                OnServerMessage?.Invoke($"Failed to start server: {ex.Message}");
                throw;
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            tcpListener?.Stop();

            // Disconnect all clients
            foreach (var client in clients.Values.ToList())
            {
                client.Disconnect();
            }
            clients.Clear();

            OnServerMessage?.Invoke("Battle Server stopped");
        }

        private async Task AcceptClientsAsync()
        {
            while (isRunning)
            {
                try
                {
                    var tcpClient = await tcpListener.AcceptTcpClientAsync();
                    var clientConnection = new ClientConnection(tcpClient);
                    
                    clients[clientConnection.Id] = clientConnection;
                    clientConnection.OnMessageReceived += HandleClientMessage;
                    clientConnection.OnDisconnected += HandleClientDisconnected;
                    
                    await clientConnection.StartAsync();
                    OnClientConnected?.Invoke(clientConnection.Id);
                }
                catch (ObjectDisposedException)
                {
                    // Server was stopped
                    break;
                }
                catch (Exception ex)
                {
                    if (isRunning)
                    {
                        OnServerMessage?.Invoke($"Error accepting client: {ex.Message}");
                    }
                }
            }
        }

        private void HandleClientMessage(string clientId, string message)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<BaseRequest>(message);
                var response = ProcessRequest(clientId, request, message);
                
                if (response != null)
                {
                    SendToClient(clientId, response);
                }
            }
            catch (Exception ex)
            {
                OnServerMessage?.Invoke($"Error processing message from {clientId}: {ex.Message}");
            }
        }

        private object ProcessRequest(string clientId, BaseRequest request, string originalMessage)
        {
            switch (request.RequestType?.ToLower())
            {
                case "creategame":
                    return HandleCreateGame(clientId, JsonConvert.DeserializeObject<CreateGameRequest>(originalMessage));
                
                case "joingame":
                    return HandleJoinGame(clientId, JsonConvert.DeserializeObject<JoinGameRequest>(originalMessage));
                
                case "setteamready":
                    return HandleSetTeamReady(clientId, JsonConvert.DeserializeObject<SetGameTeamReadyRequest>(originalMessage));
                
                case "moveteam":
                    return HandleMoveTeam(clientId, JsonConvert.DeserializeObject<MoveTeamRequest>(originalMessage));
                
                case "battleaction":
                    return HandleBattleAction(clientId, JsonConvert.DeserializeObject<ExecuteActionRequest>(originalMessage));
                
                case "invadebattle":
                    return HandleInvadeBattle(clientId, JsonConvert.DeserializeObject<InvadeBattleRequest>(originalMessage));
                
                case "getgamestate":
                    return HandleGetGameState(clientId);
                
                default:
                    return new BaseResponse
                    {
                        Success = false,
                        Message = $"Unknown request type: {request.RequestType}"
                    };
            }
        }

        private CreateGameResponse HandleCreateGame(string clientId, CreateGameRequest request)
        {
            try
            {
                var game = new GameManager();
                activeGames[game.Id] = game;
                clientGameMapping[clientId] = game.Id;

                return new CreateGameResponse
                {
                    Success = true,
                    Message = "Game created successfully",
                    GameId = game.Id,
                    RoomCode = game.RoomCode
                };
            }
            catch (Exception ex)
            {
                return new CreateGameResponse
                {
                    Success = false,
                    Message = $"Failed to create game: {ex.Message}"
                };
            }
        }

        private class JoinGameData
        {
            public string PlayerId { get; set; }
            public string TeamId { get; set; }
            public string TeamName { get; set; }
            public bool IsTeamLeader { get; set; }
        }

        private JoinGameResponse HandleJoinGame(string clientId, JoinGameRequest request)
        {
            try
            {
                GameManager game = null;
                
                if (!string.IsNullOrEmpty(request.GameId))
                {
                    activeGames.TryGetValue(request.GameId, out game);
                }
                else if (!string.IsNullOrEmpty(request.RoomCode))
                {
                    game = activeGames.Values.FirstOrDefault(g => g.RoomCode == request.RoomCode);
                }

                if (game == null)
                {
                    return new JoinGameResponse
                    {
                        Success = false,
                        Message = "Game not found"
                    };
                }

                var result = game.AddPlayer(request.PlayerName, request.Class, request.TeamId);
                if (result.Success)
                {
                    clientGameMapping[clientId] = game.Id;
                    
                    var data = (JoinGameData)result.Data;
                    return new JoinGameResponse
                    {
                        Success = true,
                        Message = result.Message,
                        GameId = game.Id,
                        PlayerId = data.PlayerId,
                        TeamId = data.TeamId,
                        TeamName = data.TeamName,
                        IsTeamLeader = data.IsTeamLeader
                    };
                }

                return new JoinGameResponse
                {
                    Success = false,
                    Message = result.Message
                };
            }
            catch (Exception ex)
            {
                return new JoinGameResponse
                {
                    Success = false,
                    Message = $"Failed to join game: {ex.Message}"
                };
            }
        }

        private BaseResponse HandleSetTeamReady(string clientId, SetGameTeamReadyRequest request)
        {
            var game = GetClientGame(clientId);
            if (game == null)
            {
                return new BaseResponse { Success = false, Message = "Not in a game" };
            }

            var result = game.SetTeamReady(request.TeamId, request.IsReady);
            return new BaseResponse
            {
                Success = result.Success,
                Message = result.Message
            };
        }

        private BaseResponse HandleMoveTeam(string clientId, MoveTeamRequest request)
        {
            var game = GetClientGame(clientId);
            if (game == null)
            {
                return new BaseResponse { Success = false, Message = "Not in a game" };
            }

            var result = game.ProcessTeamMovement(request.TeamId, request.TargetNodeId);
            return new BaseResponse
            {
                Success = result.Success,
                Message = result.Message
            };
        }

        private GenericResponse<object> HandleBattleAction(string clientId, ExecuteActionRequest request)
        {
            var game = GetClientGame(clientId);
            if (game == null)
            {
                return new GenericResponse<object> { Success = false, Message = "Not in a game" };
            }

            var result = game.ProcessBattleAction(request.BattleId, request.PlayerId, request.Action);
            
            return new GenericResponse<object>
            {
                Success = result.Success,
                Message = result.Message,
                Data = result.Data
            };
        }

        private BaseResponse HandleInvadeBattle(string clientId, InvadeBattleRequest request)
        {
            var game = GetClientGame(clientId);
            if (game == null)
            {
                return new BaseResponse { Success = false, Message = "Not in a game" };
            }

            var result = game.ProcessTeamInvasion(request.TeamId, request.BattleId);
            return new BaseResponse
            {
                Success = result.Success,
                Message = result.Message
            };
        }

        private GetGameStateResponse HandleGetGameState(string clientId)
        {
            var game = GetClientGame(clientId);
            if (game == null)
            {
                return new GetGameStateResponse { Success = false, Message = "Not in a game" };
            }

            return new GetGameStateResponse
            {
                Success = true,
                Message = "Game state retrieved",
                GameId = game.Id,
                State = game.State,
                Teams = game.Teams,
                Map = game.MapManager.Nodes,
                TeamPositions = game.MapManager.TeamNodePositions,
                ActiveBattles = game.ActiveBattles
            };
        }

        private GameManager GetClientGame(string clientId)
        {
            if (clientGameMapping.TryGetValue(clientId, out string gameId))
            {
                activeGames.TryGetValue(gameId, out GameManager game);
                return game;
            }
            return null;
        }

        private void BroadcastToGame(string gameId, object notification)
        {
            var gameClients = clientGameMapping.Where(kvp => kvp.Value == gameId).Select(kvp => kvp.Key);
            foreach (var clientId in gameClients)
            {
                SendToClient(clientId, notification);
            }
        }

        private void SendToClient(string clientId, object data)
        {
            if (clients.TryGetValue(clientId, out ClientConnection client))
            {
                var json = JsonConvert.SerializeObject(data);
                client.SendMessage(json);
            }
        }

        private void HandleClientDisconnected(string clientId)
        {
            clients.Remove(clientId);
            OnClientDisconnected?.Invoke(clientId);
            
            if (clientGameMapping.TryGetValue(clientId, out string gameId))
            {
                clientGameMapping.Remove(clientId);
                var remainingClients = clientGameMapping.Count(kvp => kvp.Value == gameId);
                if (remainingClients == 0)
                {
                    activeGames.Remove(gameId);
                }
            }
        }
    }

    [Serializable]
    public class SetGameTeamReadyRequest : BaseRequest
    {
        public string TeamId { get; set; }
        public bool IsReady { get; set; }
    }
}