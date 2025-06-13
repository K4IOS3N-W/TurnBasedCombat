using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using BattleSystem.Server;

public class GameClient : MonoBehaviour
{
    public static GameClient Instance { get; private set; }
    
    [Header("Connection")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 7777;
    
    [Header("Player Data")]
    public string playerClass;
    public string teamId;
    public bool isTeamLeader;
    public string roomCode;
    public string currentPosition = "start";
    
    public bool IsConnected => tcpClient != null && tcpClient.Connected;
    
    // Events
    public event System.Action OnConnected;
    public event System.Action OnDisconnected;
    public event System.Action<ServerResponse> OnServerMessage;
    
    private TcpClient tcpClient;
    private NetworkStream stream;
    private bool isListening;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public async Task<bool> ConnectToServer()
    {
        try
        {
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(serverIP, serverPort);
            stream = tcpClient.GetStream();
            
            OnConnected?.Invoke();
            StartListening();
            
            Debug.Log($"Connected to server at {serverIP}:{serverPort}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to connect: {ex.Message}");
            return false;
        }
    }
    
    public void Disconnect()
    {
        isListening = false;
        stream?.Close();
        tcpClient?.Close();
        OnDisconnected?.Invoke();
        Debug.Log("Disconnected from server");
        
        // Reset client state
        ResetClientState();
    }
    
    private void ResetClientState()
    {
        playerClass = "";
        teamId = "";
        isTeamLeader = false;
        roomCode = "";
        currentPosition = "start";
    }
    
    private async void StartListening()
    {
        isListening = true;
        var buffer = new byte[4096];
        
        try
        {
            while (isListening && tcpClient.Connected)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;
                
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var response = JsonConvert.DeserializeObject<ServerResponse>(message);
                
                // Handle on main thread
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    OnServerMessage?.Invoke(response);
                    HandleServerMessage(response);
                });
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Listening error: {ex.Message}");
        }
        finally
        {
            if (isListening)
            {
                Disconnect();
            }
        }
    }
    
    private void HandleServerMessage(ServerResponse response)
    {
        // Log important server messages
        switch (response.Type?.ToLower())
        {
            case "teammoved":
                if (response.TeamId == teamId)
                {
                    currentPosition = response.NewPosition;
                    Debug.Log($"Moved to: {currentPosition}");
                }
                break;
                
            case "gamestarted":
                Debug.Log("Game started! Race to the goal!");
                break;
                
            case "battlestarted":
                Debug.Log("Entering battle...");
                break;
                
            case "error":
                Debug.LogWarning($"Server error: {response.Message}");
                break;
        }
    }
    
    public async void SendRequest(ServerRequest request)
    {
        if (!IsConnected) 
        {
            Debug.LogWarning("Cannot send request - not connected to server");
            return;
        }
        
        try
        {
            string json = JsonConvert.SerializeObject(request);
            byte[] data = Encoding.UTF8.GetBytes(json);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
            
            Debug.Log($"Sent request: {request.Type}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Send error: {ex.Message}");
            Disconnect();
        }
    }
    
    // Convenience methods with validation
    public void CreateRoom()
    {
        if (!IsConnected) return;
        SendRequest(new ServerRequest { Type = "CreateRoom" });
    }
    
    public void JoinRoom(string code)
    {
        if (!IsConnected || string.IsNullOrEmpty(code)) return;
        SendRequest(new ServerRequest { Type = "JoinRoom", RoomCode = code });
    }
    
    public void SelectClass(string className)
    {
        if (!IsConnected || string.IsNullOrEmpty(className)) return;
        playerClass = className;
        SendRequest(new ServerRequest { Type = "SelectClass", PlayerClass = className });
    }
    
    public void CreateTeam(string teamName)
    {
        if (!IsConnected || string.IsNullOrEmpty(teamName) || string.IsNullOrEmpty(playerClass)) return;
        SendRequest(new ServerRequest { Type = "CreateTeam", TeamName = teamName });
    }
    
    public void JoinTeam(string targetTeamId)
    {
        if (!IsConnected || string.IsNullOrEmpty(targetTeamId) || string.IsNullOrEmpty(playerClass)) return;
        SendRequest(new ServerRequest { Type = "JoinTeam", TeamId = targetTeamId });
    }
    
    public void SetTeamReady(bool ready)
    {
        if (!IsConnected || string.IsNullOrEmpty(teamId)) return;
        SendRequest(new ServerRequest { Type = "TeamReady", IsReady = ready });
    }
    
    public void MoveTeam(string waypoint)
    {
        if (!IsConnected || string.IsNullOrEmpty(teamId) || string.IsNullOrEmpty(waypoint)) return;
        SendRequest(new ServerRequest { Type = "MoveTeam", TargetWaypoint = waypoint });
    }
    
    public void BattleAction(string actionType, string targetId = null, string skillId = null)
    {
        if (!IsConnected || string.IsNullOrEmpty(actionType)) return;
        
        SendRequest(new ServerRequest 
        { 
            Type = "BattleAction", 
            BattleAction = new BattleActionData 
            { 
                Type = actionType, 
                TargetId = targetId, 
                SkillId = skillId 
            }
        });
    }
    
    private void OnDestroy()
    {
        Disconnect();
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Optionally disconnect on pause
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            // Handle focus loss
        }
    }
}
