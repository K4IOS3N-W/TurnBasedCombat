using UnityEngine;
using BattleSystem.Server;
using System.Threading.Tasks;

public class ServerStarter : MonoBehaviour
{
    [Header("Server Settings")]
    public bool startServerOnAwake = true;
    public int serverPort = 7777;
    
    [Header("UI")]
    public TMPro.TextMeshProUGUI serverStatusText;
    public UnityEngine.UI.Button startServerButton;
    public UnityEngine.UI.Button stopServerButton;
    
    private GameServer server;
    
    private void Awake()
    {
        if (startServerOnAwake)
        {
            StartServer();
        }
        
        SetupUI();
    }
    
    private void SetupUI()
    {
        if (startServerButton != null)
            startServerButton.onClick.AddListener(StartServer);
            
        if (stopServerButton != null)
            stopServerButton.onClick.AddListener(StopServer);
            
        UpdateUI();
    }
    
    public async void StartServer()
    {
        if (server != null && server.IsRunning) return;
        
        server = new GameServer(serverPort);
        server.OnServerLog += OnServerLog;
        
        await server.StartAsync();
        UpdateUI();
    }
    
    public void StopServer()
    {
        if (server == null || !server.IsRunning) return;
        
        server.Stop();
        server = null;
        UpdateUI();
    }
    
    private void OnServerLog(string message)
    {
        Debug.Log($"[Server] {message}");
        
        if (serverStatusText != null)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                serverStatusText.text = message;
            });
        }
    }
    
    private void UpdateUI()
    {
        bool serverRunning = server != null && server.IsRunning;
        
        if (startServerButton != null)
            startServerButton.interactable = !serverRunning;
            
        if (stopServerButton != null)
            stopServerButton.interactable = serverRunning;
            
        if (serverStatusText != null)
            serverStatusText.text = serverRunning ? $"Server running on port {serverPort}" : "Server stopped";
    }
    
    private void OnDestroy()
    {
        StopServer();
    }
}
