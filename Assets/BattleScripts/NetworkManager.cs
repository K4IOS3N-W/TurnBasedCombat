using System.Collections.Generic;
using UnityEngine;
using BattleSystem;
using BattleSystem.Server;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;


public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    
    [Header("Connection Settings")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 7777;
    public bool isServer = false;
    
    [Header("Player Info")]
    public string playerName = "";
    public string playerClass = "";
    public string teamId = "";
    public bool isTeamLeader = false;
    public bool isReady = false;
    
    [Header("Debug")]
    public bool autoConnect = false;
    
    private BattleServer server;
    private BattleTestClient client;
    private GameManager gameManager;
    private GameNetworkBridge networkBridge;

    private Dictionary<string, Player> connectedPlayers = new Dictionary<string, Player>();
    private Dictionary<string, Team> teams = new Dictionary<string, Team>();
    
    // UI References (will be set by UI scripts)
    [HideInInspector] public MainMenuUI mainMenuUI;
    [HideInInspector] public LobbyUI lobbyUI;
    
    public bool IsConnected => client != null && client.IsConnected;
    public bool IsServerRunning => server != null && server.IsRunning;
    public string PlayerId { get; private set; }
    
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
    
    private void Start()
    {

        if (autoConnect)
        {
            StartCoroutine(AutoConnectRoutine());
        }

        // Find or create the network bridge
        networkBridge = FindObjectOfType<GameNetworkBridge>();
        if (networkBridge == null && Application.isPlaying)
        {
            var bridgeObj = new GameObject("GameNetworkBridge");
            networkBridge = bridgeObj.AddComponent<GameNetworkBridge>();
        }
    }
    
    private IEnumerator AutoConnectRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        if (isServer)
        {
            StartServer();
            yield return new WaitForSeconds(0.5f);
        }
        ConnectToServer();
    }

    public void StartServer()
    {
        if (server != null && server.IsRunning)
            return;

        server = new BattleServer(serverPort);
        _ = server.StartAsync(); // Use the async method
        Debug.Log($"Server started on port {serverPort}");

        // Create game manager when starting server
        gameManager = new GameManager();
    }

    public void ConnectToServer()
    {
        if (client != null && client.IsConnected)
            return;

        client = gameObject.AddComponent<BattleTestClient>();

        // Set the server IP and port before connecting
        var clientFields = typeof(BattleTestClient).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var serverIpField = clientFields.FirstOrDefault(f => f.Name == "serverIp");
        var serverPortField = clientFields.FirstOrDefault(f => f.Name == "serverPort");

        if (serverIpField != null) serverIpField.SetValue(client, serverIP);
        if (serverPortField != null) serverPortField.SetValue(client, serverPort);

        client.Connect();
    }

    private void OnConnected()
    {
        Debug.Log("Connected to server");
        
        // Move to the lobby scene if not already there
        if (SceneManager.GetActiveScene().name != "Lobby")
        {
            SceneManager.LoadScene("Lobby");
        }
    }
    
    private void OnDisconnected()
    {
        Debug.Log("Disconnected from server");
        
        // Return to main menu
        SceneManager.LoadScene("MainMenu");
    }
    
    private void OnMessageReceived(string message)
    {
        // Process messages from server
        Debug.Log($"Received: {message}");

        // Forward message to network bridge
        if (networkBridge != null)
        {
            networkBridge.ProcessMessage(message);
        }

        // Parse message based on JSON format and update UI accordingly
        // This would handle player joins, team updates, ready statu
    }

    public void SetPlayerInfo(string name, string characterClass)
    {
        playerName = name;
        playerClass = characterClass;
        
        // Send player info to server
        if (IsConnected)
        {
            var request = new CreatePlayerRequest
            {
                Name = playerName,
                Class = playerClass
            };
            
            string json = JsonUtility.ToJson(request);
            client.SendMessage(json);
        }
    }
    
    public void CreateOrJoinTeam(string targetTeamId = "")
    {
        if (IsConnected)
        {
            var request = new JoinTeamRequest
            {
                TeamId = targetTeamId // Empty for new team
            };
            
            string json = JsonUtility.ToJson(request);
            client.SendMessage(json);
        }
    }
    
    public void SetReadyStatus(bool ready)
    {
        isReady = ready;
        
        if (IsConnected)
        {
            var request = new ReadyRequest
            {
                IsReady = ready
            };
            
            string json = JsonUtility.ToJson(request);
            client.SendMessage(json);
        }
    }

    public void StartGame()
    {
        if (isServer && gameManager != null)
        {
            // Only team leaders or server admin can start the game
            gameManager.StartGame();

            // Broadcast game start to all clients
            if (server != null)
            {
                var response = new GameStateResponse
                {
                    State = "Playing"
                };

                // We need to broadcast this to all clients
                // This would normally go through server.BroadcastMessage but we need to implement that
            }
        }
    }

    void OnDestroy()
    {
        if (client != null)
        {
            client.Disconnect();
        }

        if (server != null)
        {
            server.Stop();
        }
    }
}

// Request/Response classes
[System.Serializable]
public class CreatePlayerRequest
{
    public string Name;
    public string Class;
}

[System.Serializable]
public class JoinTeamRequest
{
    public string TeamId;
}

[System.Serializable]
public class ReadyRequest
{
    public bool IsReady;
}

[System.Serializable]
public class GameStateResponse
{
    public string State;
}
