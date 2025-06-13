using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace BattleSystem.Server
{
    public class GameServer : MonoBehaviour // Add MonoBehaviour inheritance
    {
        private TcpListener listener;
        private bool isRunning;
        private readonly Dictionary<string, ClientHandler> clients = new Dictionary<string, ClientHandler>();
        private readonly Dictionary<string, GameRoom> gameRooms = new Dictionary<string, GameRoom>();
        
        public bool IsRunning => isRunning;
        public int Port { get; private set; }
        
        public event Action<string> OnServerLog;
        
        public GameServer(int port = 7777)
        {
            Port = port;
        }
        
        public async Task StartAsync()
        {
            if (isRunning) return;
            
            try
            {
                listener = new TcpListener(IPAddress.Any, Port);
                listener.Start();
                isRunning = true;
                
                Log($"Game Server started on port {Port}");
                
                _ = Task.Run(AcceptClientsAsync);
                
                await Task.CompletedTask; // Add await to fix warning
            }
            catch (Exception ex)
            {
                Log($"Failed to start server: {ex.Message}");
                throw;
            }
        }
        
        public void Stop()
        {
            if (!isRunning) return;
            
            isRunning = false;
            listener?.Stop();
            
            foreach (var client in clients.Values.ToList())
            {
                client.Disconnect();
            }
            clients.Clear();
            gameRooms.Clear();
            
            Log("Game Server stopped");
        }
        
        private async Task AcceptClientsAsync()
        {
            while (isRunning)
            {
                try
                {
                    var tcpClient = await listener.AcceptTcpClientAsync();
                    var clientHandler = new ClientHandler(tcpClient);
                    
                    clients[clientHandler.Id] = clientHandler;
                    clientHandler.OnMessageReceived += HandleClientMessage;
                    clientHandler.OnDisconnected += HandleClientDisconnected;
                    
                    Log($"Client connected: {clientHandler.Id}");
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log($"Accept client error: {ex.Message}");
                }
            }
        }
        
        private void HandleClientDisconnected(string clientId)
        {
            if (clients.TryGetValue(clientId, out var client))
            {
                // Remove from room if in one
                if (!string.IsNullOrEmpty(client.RoomCode) && gameRooms.TryGetValue(client.RoomCode, out var room))
                {
                    room.RemovePlayer(client);
                    
                    // Notify other players
                    BroadcastToRoom(client.RoomCode, new ServerResponse
                    {
                        Type = "PlayerLeft",
                        PlayerCount = room.Players.Count
                    });
                }
                
                clients.Remove(clientId);
                Log($"Client disconnected: {clientId}");
            }
        }
        
        public void HandleClientMessage(string clientId, string message)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<ServerRequest>(message);
                var response = ProcessRequest(clientId, request);
                
                if (clients.TryGetValue(clientId, out var client))
                {
                    _ = client.SendResponse(response);
                }
            }
            catch (Exception ex)
            {
                Log($"Handle message error for {clientId}: {ex.Message}");
            }
        }
        
        private ServerResponse ProcessRequest(string clientId, ServerRequest request)
        {
            try
            {
                switch (request.Type.ToLower())
                {
                    case "createroom":
                        return HandleCreateRoom(clientId);
                        
                    case "joinroom":
                        return HandleJoinRoom(clientId, request.RoomCode);
                        
                    case "selectclass":
                        return HandleSelectClass(clientId, request.PlayerClass);
                        
                    case "createteam":
                        return HandleCreateTeam(clientId, request.TeamName);
                        
                    case "jointeam":
                        return HandleJoinTeam(clientId, request.TeamId);
                        
                    case "teamready":
                        return HandleTeamReady(clientId, request.IsReady);
                        
                    case "moveteam":
                        return HandleMoveTeam(clientId, request.TargetWaypoint);
                        
                    case "battleaction":
                        return HandleBattleAction(clientId, request.BattleAction);
                        
                    default:
                        return new ServerResponse { Success = false, Message = "Unknown request type" };
                }
            }
            catch (Exception ex)
            {
                Log($"Process request error: {ex.Message}");
                return new ServerResponse { Success = false, Message = "Server error occurred" };
            }
        }
        
        private ServerResponse HandleCreateRoom(string clientId)
        {
            var roomCode = GenerateRoomCode();
            var gameRoom = new GameRoom(roomCode);
            gameRooms[roomCode] = gameRoom;
            
            var client = clients[clientId];
            client.RoomCode = roomCode;
            gameRoom.AddPlayer(client);
            
            return new ServerResponse 
            { 
                Type = "RoomCreated",
                Success = true, 
                Message = "Room created",
                RoomCode = roomCode,
                GameState = "Lobby"
            };
        }
        
        private ServerResponse HandleJoinRoom(string clientId, string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode) || !gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                return new ServerResponse { Success = false, Message = "Room not found" };
            }
            
            if (gameRoom.Players.Count >= 16)
            {
                return new ServerResponse { Success = false, Message = "Room is full" };
            }
            
            var client = clients[clientId];
            client.RoomCode = roomCode;
            gameRoom.AddPlayer(client);
            
            // Notify other players
            BroadcastToRoom(roomCode, new ServerResponse 
            { 
                Type = "PlayerJoined",
                PlayerCount = gameRoom.Players.Count
            });
            
            return new ServerResponse 
            { 
                Type = "JoinedRoom",
                Success = true, 
                Message = "Joined room",
                RoomCode = roomCode,
                PlayerCount = gameRoom.Players.Count
            };
        }
        
        private ServerResponse HandleSelectClass(string clientId, string playerClass)
        {
            var client = clients[clientId];
            client.PlayerClass = playerClass;
            
            return new ServerResponse { Success = true, Message = "Class selected" };
        }
        
        private ServerResponse HandleCreateTeam(string clientId, string teamName)
        {
            var client = clients[clientId];
            if (client.RoomCode == null || !gameRooms.TryGetValue(client.RoomCode, out var gameRoom))
            {
                return new ServerResponse { Success = false, Message = "Not in a room" };
            }
            
            if (string.IsNullOrEmpty(client.PlayerClass))
            {
                return new ServerResponse { Success = false, Message = "Select a class first" };
            }
            
            var teamId = gameRoom.CreateTeam(teamName, client);
            if (teamId == null)
            {
                return new ServerResponse { Success = false, Message = "Cannot create team" };
            }
            
            BroadcastToRoom(client.RoomCode, new ServerResponse 
            { 
                Type = "TeamCreated",
                TeamId = teamId,
                TeamName = teamName,
                LeaderId = clientId
            });
            
            return new ServerResponse 
            { 
                Type = "TeamCreated",
                Success = true, 
                Message = "Team created",
                TeamId = teamId,
                IsLeader = true
            };
        }
        
        private ServerResponse HandleJoinTeam(string clientId, string teamId)
        {
            var client = clients[clientId];
            if (client.RoomCode == null || !gameRooms.TryGetValue(client.RoomCode, out var gameRoom))
            {
                return new ServerResponse { Success = false, Message = "Not in a room" };
            }
            
            if (string.IsNullOrEmpty(client.PlayerClass))
            {
                return new ServerResponse { Success = false, Message = "Select a class first" };
            }
            
            if (!gameRoom.JoinTeam(teamId, client))
            {
                return new ServerResponse { Success = false, Message = "Cannot join team" };
            }
            
            BroadcastToRoom(client.RoomCode, new ServerResponse 
            { 
                Type = "PlayerJoinedTeam",
                TeamId = teamId
            });
            
            return new ServerResponse { Success = true, Message = "Joined team" };
        }
        
        private ServerResponse HandleTeamReady(string clientId, bool isReady)
        {
            var client = clients[clientId];
            if (client.RoomCode == null || !gameRooms.TryGetValue(client.RoomCode, out var gameRoom))
            {
                return new ServerResponse { Success = false, Message = "Not in a room" };
            }
            
            if (!gameRoom.SetTeamReady(client.TeamId, isReady))
            {
                return new ServerResponse { Success = false, Message = "Cannot set ready status" };
            }
            
            BroadcastToRoom(client.RoomCode, new ServerResponse 
            { 
                Type = "TeamReadyChanged",
                TeamId = client.TeamId,
                IsReady = isReady
            });
            
            // Check if all teams are ready
            if (gameRoom.AllTeamsReady() && gameRoom.Teams.Count >= 2)
            {
                StartGame(gameRoom);
            }
            
            return new ServerResponse { Success = true, Message = "Ready status updated" };
        }
        
        private ServerResponse HandleMoveTeam(string clientId, string targetWaypoint)
        {
            var client = clients[clientId];
            if (client.RoomCode == null || !gameRooms.TryGetValue(client.RoomCode, out var gameRoom))
            {
                return new ServerResponse { Success = false, Message = "Not in a room" };
            }
            
            var result = gameRoom.MoveTeam(client.TeamId, targetWaypoint);
            if (!result.Success)
            {
                return new ServerResponse { Success = false, Message = result.Message };
            }
            
            BroadcastToRoom(client.RoomCode, new ServerResponse 
            { 
                Type = "TeamMoved",
                TeamId = client.TeamId,
                NewPosition = targetWaypoint,
                CurrentTurn = gameRoom.CurrentTurnTeam
            });
            
            // Check for encounters
            var encounter = gameRoom.CheckEncounter(targetWaypoint);
            if (encounter != null)
            {
                StartBattle(gameRoom, encounter);
            }
            
            // Check for victory
            if (result.IsVictory)
            {
                BroadcastToRoom(client.RoomCode, new ServerResponse
                {
                    Type = "GameWon",
                    Message = "Team reached the goal!",
                    TeamId = client.TeamId
                });
            }
            
            return new ServerResponse { Success = true, Message = "Team moved" };
        }
        
        private ServerResponse HandleBattleAction(string clientId, BattleActionData action)
        {
            var client = clients[clientId];
            if (client.RoomCode == null || !gameRooms.TryGetValue(client.RoomCode, out var gameRoom))
            {
                return new ServerResponse { Success = false, Message = "Not in a room" };
            }
            
            var result = gameRoom.ProcessBattleAction(clientId, action);
            if (!result.Success)
            {
                return new ServerResponse { Success = false, Message = result.Message };
            }
            
            BroadcastToRoom(client.RoomCode, new ServerResponse 
            { 
                Type = "BattleAction",
                BattleResult = result
            });
            
            return new ServerResponse { Success = true, Message = "Action processed" };
        }
        
        private void StartGame(GameRoom gameRoom)
        {
            gameRoom.StartGame();
            
            BroadcastToRoom(gameRoom.RoomCode, new ServerResponse 
            { 
                Type = "GameStarted",
                GameState = "Playing",
                CurrentTurn = gameRoom.CurrentTurnTeam,
                MapState = gameRoom.GetMapState()
            });
        }
        
        private void StartBattle(GameRoom gameRoom, EncounterData encounter)
        {
            gameRoom.StartBattle(encounter);
            
            BroadcastToRoom(gameRoom.RoomCode, new ServerResponse 
            { 
                Type = "BattleStarted",
                Encounter = encounter
            });
        }
        
        private void BroadcastToRoom(string roomCode, ServerResponse response)
        {
            if (!gameRooms.TryGetValue(roomCode, out var room)) return;
            
            foreach (var player in room.Players)
            {
                _ = player.SendResponse(response);
            }
        }
        
        private string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new System.Random(); // Explicitly use System.Random
            
            string code;
            do
            {
                code = new string(Enumerable.Repeat(chars, 6)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (gameRooms.ContainsKey(code));
            
            return code;
        }
        
        private void Log(string message)
        {
            OnServerLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
            UnityEngine.Debug.Log(message);
        }
        
        private BattleActionResult ProcessAIBattleAction(string participantId)
        {
            // Simple AI logic - use UnityEngine.Random explicitly
            var actions = new[] { "Attack", "Defend", "Skill" };
            string selectedAction = actions[UnityEngine.Random.Range(0, actions.Length)];
            
            return new BattleActionResult
            {
                Success = true,
                Message = $"{participantId} used {selectedAction}",
                Damage = UnityEngine.Random.Range(10, 30)
            };
        }
    }
}
