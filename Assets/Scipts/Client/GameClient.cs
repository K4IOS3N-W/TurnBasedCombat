using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using BattleSystem.Server;
using BattleSystem.Map;

namespace BattleSystem.Client
{
    public class GameClient : MonoBehaviour
    {
        [Header("Connection Settings")]
        [SerializeField] private string defaultServerIP = "127.0.0.1";
        [SerializeField] private int defaultServerPort = 7777;
        [SerializeField] private bool autoConnectOnStart = false;
        
        // Connection state
        private TcpClient tcpClient;
        private NetworkStream stream;
        private bool isConnected = false;
        private bool isHost = false;
        
        // Game state
        private string roomCode = "";
        private string teamId = "";
        private string selectedClass = "";
        private PlayerStats playerStats;
        private List<string> availableMoves = new List<string>();
        private float turnTimeRemaining = 30f;
        private DateTime lastTurnUpdate = DateTime.Now;
        
        // Server components (for hosting)
        private GameServer gameServer;
        
        // Map integration
        private MapManager mapManager;
        private Dictionary<string, string> teamPositions = new Dictionary<string, string>();
        
        // Events for UI
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnRoomCreated;
        public event Action<string, int> OnRoomJoined;
        public event Action<string, string> OnTeamCreated;
        public event Action<string> OnTeamJoined;
        public event Action OnGameStarted;
        public event Action<string, bool> OnTurnChanged;
        public event Action<EncounterData> OnBattleStarted;
        public event Action<bool> OnBattleEnded;
        public event Action<PlayerStats> OnPlayerStatsUpdated;
        public event Action<BattleActionResult> OnBattleActionResult;
        public event Action<string> OnErrorMessage;
        public event Action<string, string> OnChatMessage;
        public event Action<string> OnServerMessage;
        
        // Properties for external access
        public bool IsConnected => isConnected;
        public bool IsHost => isHost;
        public string RoomCode => roomCode;
        public string TeamId => teamId;
        public PlayerStats PlayerStats => playerStats;
        public bool IsTeamLeader { get; private set; }
        
        private void Start()
        {
            playerStats = new PlayerStats();
            mapManager = FindObjectOfType<MapManager>();
            
            if (mapManager != null)
            {
                mapManager.OnWaypointClicked += OnWaypointClickedFromMap;
            }
            
            if (autoConnectOnStart)
            {
                _ = ConnectToServer(defaultServerIP, defaultServerPort);
            }
        }
        
        private void Update()
        {
            // Update turn timer
            if (isConnected)
            {
                var timePassed = (float)(DateTime.Now - lastTurnUpdate).TotalSeconds;
                turnTimeRemaining = Mathf.Max(0, 30f - timePassed);
            }
        }
        
        public async void StartAsHost()
        {
            try
            {
                isHost = true;
                
                // Create server instance
                var serverGO = new GameObject("GameServer");
                gameServer = serverGO.AddComponent<GameServer>();
                
                // Start server
                await gameServer.StartAsync();
                
                // Connect to own server
                await ConnectToServer(defaultServerIP, defaultServerPort);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to start as host: {ex.Message}");
                OnErrorMessage?.Invoke($"Failed to start server: {ex.Message}");
            }
        }
        
        public async Task ConnectToServer(string ip, int port)
        {
            try
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(ip, port);
                stream = tcpClient.GetStream();
                isConnected = true;
                
                OnConnected?.Invoke();
                
                _ = Task.Run(ListenForMessages);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection failed: {ex.Message}");
                OnErrorMessage?.Invoke($"Connection failed: {ex.Message}");
            }
        }
        
        private async Task ListenForMessages()
        {
            byte[] buffer = new byte[4096];
            
            try
            {
                while (isConnected && tcpClient.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;
                    
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    
                    // Use Unity's main thread dispatcher for UI updates
                    UnityMainThreadDispatcher.Instance().Enqueue(() => {
                        HandleServerMessage(message);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }
        
        private void HandleServerMessage(string message)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<ServerResponse>(message);
                ProcessServerResponse(response);
                OnServerMessage?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse server message: {ex.Message}");
            }
        }
        
        private void ProcessServerResponse(ServerResponse response)
        {
            switch (response.Type?.ToLower())
            {
                case "roomcreated":
                    roomCode = response.RoomCode;
                    OnRoomCreated?.Invoke(response.RoomCode);
                    break;
                    
                case "joinedroom":
                    roomCode = response.RoomCode;
                    OnRoomJoined?.Invoke(response.RoomCode, response.PlayerCount);
                    break;
                    
                case "teamcreated":
                    teamId = response.TeamId;
                    IsTeamLeader = response.IsLeader;
                    OnTeamCreated?.Invoke(response.TeamId, response.TeamName);
                    break;
                    
                case "playerjoinedteam":
                    OnTeamJoined?.Invoke(response.TeamId);
                    break;
                    
                case "gamestarted":
                    InitializeMapPositions();
                    UpdateAvailableMoves(response.MapState);
                    OnGameStarted?.Invoke();
                    break;
                    
                case "teammoved":
                    UpdateTeamPosition(response.TeamId, response.NewPosition);
                    UpdateAvailableMoves(response.MapState);
                    UpdateTurnInfo(response.CurrentTurn);
                    break;
                    
                case "battlestarted":
                    OnBattleStarted?.Invoke(response.Encounter);
                    break;
                    
                case "battleaction":
                    OnBattleActionResult?.Invoke(response.BattleResult);
                    break;
                    
                case "battleended":
                    bool victory = response.BattleResult?.Success ?? false;
                    OnBattleEnded?.Invoke(victory);
                    break;
                    
                case "playerstats":
                    UpdatePlayerStats(response);
                    break;
                    
                case "turnchanged":
                    UpdateTurnInfo(response.CurrentTurn);
                    break;
                    
                case "chatmessage":
                    OnChatMessage?.Invoke(response.Message, response.TeamId);
                    break;
                    
                case "error":
                    OnErrorMessage?.Invoke(response.Message);
                    break;
                    
                default:
                    Debug.Log($"Unhandled server response: {response.Type}");
                    break;
            }
        }
        
        private void UpdateTeamPosition(string teamIdParam, string newPosition)
        {
            teamPositions[teamIdParam] = newPosition;
            
            if (mapManager != null)
            {
                mapManager.UpdateTeamPosition(teamIdParam, newPosition);
            }
        }
        
        private void InitializeMapPositions()
        {
            teamPositions[teamId] = "start";
            
            if (mapManager != null)
            {
                mapManager.UpdateTeamPosition(teamId, "start");
            }
        }
        
        private void UpdateAvailableMoves(object mapState)
        {
            availableMoves.Clear();
            
            if (mapManager != null)
            {
                string currentPosition = GetCurrentTeamPosition();
                availableMoves = mapManager.GetConnectedWaypoints(currentPosition);
            }
            else
            {
                availableMoves.AddRange(new[] { "waypoint1", "waypoint2", "waypoint3", "goal" });
            }
        }
        
        private void UpdateTurnInfo(string currentTurn)
        {
            bool isMyTurn = currentTurn == teamId;
            lastTurnUpdate = DateTime.Now;
            turnTimeRemaining = 30f;
            
            OnTurnChanged?.Invoke(currentTurn, isMyTurn);
        }
        
        private void UpdatePlayerStats(ServerResponse response)
        {
            OnPlayerStatsUpdated?.Invoke(playerStats);
        }
        
        // Public methods for UI
        public async void CreateRoom()
        {
            var request = new ServerRequest { Type = "CreateRoom" };
            await SendRequest(request);
        }
        
        public async void JoinRoom(string code)
        {
            var request = new ServerRequest { Type = "JoinRoom", RoomCode = code };
            await SendRequest(request);
        }
        
        public async void SelectClass(string className)
        {
            selectedClass = className;
            playerStats.UpdateForClass(className);
            
            var request = new ServerRequest { Type = "SelectClass", PlayerClass = className };
            await SendRequest(request);
            OnPlayerStatsUpdated?.Invoke(playerStats);
        }
        
        public async void CreateTeam(string teamName)
        {
            var request = new ServerRequest { Type = "CreateTeam", TeamName = teamName };
            await SendRequest(request);
        }
        
        public async void JoinTeam(string teamIdParam)
        {
            var request = new ServerRequest { Type = "JoinTeam", TeamId = teamIdParam };
            await SendRequest(request);
        }
        
        public async void SetTeamReady(bool ready)
        {
            var request = new ServerRequest { Type = "TeamReady", IsReady = ready };
            await SendRequest(request);
        }
        
        public async void MoveToWaypoint(string waypoint)
        {
            var request = new ServerRequest { Type = "MoveTeam", TargetWaypoint = waypoint };
            await SendRequest(request);
        }
        
        public async void PerformBattleAction(BattleActionData actionData)
        {
            var request = new ServerRequest { Type = "BattleAction", BattleAction = actionData };
            await SendRequest(request);
        }
        
        public async void SendChatMessage(string message)
        {
            var request = new ServerRequest { Type = "ChatMessage", TeamName = message };
            await SendRequest(request);
        }
        
        private async Task SendRequest(ServerRequest request)
        {
            if (!isConnected) return;
            
            try
            {
                string json = JsonConvert.SerializeObject(request);
                byte[] data = Encoding.UTF8.GetBytes(json);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send request: {ex.Message}");
                OnErrorMessage?.Invoke($"Failed to send request: {ex.Message}");
            }
        }
        
        public void Disconnect()
        {
            if (!isConnected) return;
            
            isConnected = false;
            stream?.Close();
            tcpClient?.Close();
            
            if (isHost && gameServer != null)
            {
                gameServer.Stop();
                if (gameServer.gameObject != null)
                    Destroy(gameServer.gameObject);
            }
            
            OnDisconnected?.Invoke();
        }
        
        // Utility methods
        public List<string> GetAvailableMoves()
        {
            return new List<string>(availableMoves);
        }
        
        public float GetTurnTimeRemaining()
        {
            return turnTimeRemaining;
        }
        
        private void OnWaypointClickedFromMap(string waypointId)
        {
            if (IsMyTurn() && CanMoveToWaypoint(waypointId))
            {
                MoveToWaypoint(waypointId);
            }
        }
        
        private bool IsMyTurn()
        {
            return isConnected;
        }
        
        private bool CanMoveToWaypoint(string waypointId)
        {
            if (mapManager == null) return false;
            
            string currentPosition = GetCurrentTeamPosition();
            return mapManager.CanMoveBetween(currentPosition, waypointId);
        }
        
        private string GetCurrentTeamPosition()
        {
            if (teamPositions.TryGetValue(teamId, out string position))
                return position;
            return "start";
        }
        
        private void OnDestroy()
        {
            Disconnect();
        }
    }
}