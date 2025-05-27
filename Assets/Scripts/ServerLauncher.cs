using System;
using System.Threading.Tasks;
using BattleSystem.Server;
using UnityEngine;
using System.Collections.Generic;

namespace BattleSystem
{
    public class ServerLauncher : MonoBehaviour
    {
        [Header("Server Configuration")]
        [SerializeField] private int defaultPort = 7777;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool showDebugInfo = true;
        
        [Header("Server Status")]
        [SerializeField] private bool isRunning = false;
        [SerializeField] private string currentStatus = "Stopped";
        [SerializeField] private int connectedClients = 0;
        [SerializeField] private int activeBattles = 0;
        
        private BattleServer server;
        private List<string> serverLogs = new List<string>();
        private const int maxLogEntries = 100;
        
        public bool IsRunning => isRunning;
        public string Status => currentStatus;
        public int ConnectedClients => connectedClients;
        public int ActiveBattles => activeBattles;
        public List<string> RecentLogs => serverLogs;
        
        public event Action<bool> OnServerStatusChanged;
        public event Action<string> OnServerLogAdded;
        
        private void Awake()
        {
            // Ensure this is a singleton
            var existingLaunchers = FindObjectsOfType<ServerLauncher>();
            if (existingLaunchers.Length > 1)
            {
                Destroy(gameObject);
                return;
            }
            
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            if (autoStart)
            {
                StartServer();
            }
        }
        
        public async void StartServer()
        {
            if (isRunning)
            {
                AddLog("Server is already running");
                return;
            }
            
            try
            {
                currentStatus = "Starting...";
                OnServerStatusChanged?.Invoke(false);
                
                server = new BattleServer(defaultPort);
                
                // Subscribe to server events
                server.OnLog += HandleServerLog;
                server.OnError += HandleServerError;
                
                AddLog($"Starting server on port {defaultPort}...");
                
                // Start server in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await server.Start();
                    }
                    catch (Exception ex)
                    {
                        HandleServerError(ex);
                    }
                });
                
                // Give server time to initialize
                await Task.Delay(1000);
                
                isRunning = true;
                currentStatus = $"Running on port {defaultPort}";
                OnServerStatusChanged?.Invoke(true);
                
                AddLog("Server started successfully");
                
                // Start periodic status updates
                InvokeRepeating(nameof(UpdateServerStats), 5f, 5f);
            }
            catch (Exception ex)
            {
                HandleServerError(ex);
                currentStatus = $"Failed to start: {ex.Message}";
                OnServerStatusChanged?.Invoke(false);
            }
        }
        
        public void StopServer()
        {
            if (!isRunning)
            {
                AddLog("Server is not running");
                return;
            }
            
            try
            {
                CancelInvoke(nameof(UpdateServerStats));
                
                server?.Stop();
                
                isRunning = false;
                currentStatus = "Stopped";
                connectedClients = 0;
                activeBattles = 0;
                
                OnServerStatusChanged?.Invoke(false);
                AddLog("Server stopped successfully");
            }
            catch (Exception ex)
            {
                HandleServerError(ex);
            }
        }
        
        public void RestartServer()
        {
            AddLog("Restarting server...");
            StopServer();
            
            // Wait a moment before restarting
            Invoke(nameof(StartServer), 2f);
        }
        
        private void HandleServerLog(string message)
        {
            AddLog($"[Server] {message}");
        }
        
        private void HandleServerError(Exception ex)
        {
            string errorMsg = $"[ERROR] {ex.Message}";
            AddLog(errorMsg);
            
            if (showDebugInfo)
            {
                Debug.LogError($"[ServerLauncher] {errorMsg}");
                Debug.LogException(ex);
            }
            
            // Auto-restart on critical errors (optional)
            if (isRunning)
            {
                AddLog("Critical error detected, attempting restart...");
                RestartServer();
            }
        }
        
        private void AddLog(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {message}";
            
            serverLogs.Add(logEntry);
            
            // Limit log size
            while (serverLogs.Count > maxLogEntries)
            {
                serverLogs.RemoveAt(0);
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[ServerLauncher] {logEntry}");
            }
            
            OnServerLogAdded?.Invoke(logEntry);
        }
        
        private void UpdateServerStats()
        {
            if (!isRunning || server == null) return;
            
            // These would need to be implemented in BattleServer
            // For now, we'll use placeholder values
            // connectedClients = server.GetConnectedClientCount();
            // activeBattles = server.GetActiveBattleCount();
        }
        
        private void OnApplicationQuit()
        {
            StopServer();
        }
        
        private void OnDestroy()
        {
            StopServer();
        }
        
        // Editor context menu methods
        [ContextMenu("Start Server")]
        private void StartServerContext() => StartServer();
        
        [ContextMenu("Stop Server")]
        private void StopServerContext() => StopServer();
        
        [ContextMenu("Restart Server")]
        private void RestartServerContext() => RestartServer();
        
        [ContextMenu("Clear Logs")]
        private void ClearLogs()
        {
            serverLogs.Clear();
            AddLog("Logs cleared");
        }
        
        // Public methods for external control
        public void ChangePort(int newPort)
        {
            if (isRunning)
            {
                AddLog($"Cannot change port while server is running. Current port: {defaultPort}");
                return;
            }
            
            defaultPort = newPort;
            AddLog($"Port changed to {newPort}");
        }
        
        public void SetAutoStart(bool enabled)
        {
            autoStart = enabled;
            AddLog($"Auto-start {(enabled ? "enabled" : "disabled")}");
        }
        
        public void SetDebugMode(bool enabled)
        {
            showDebugInfo = enabled;
            AddLog($"Debug mode {(enabled ? "enabled" : "disabled")}");
        }
        
        public ServerInfo GetServerInfo()
        {
            return new ServerInfo
            {
                IsRunning = isRunning,
                Port = defaultPort,
                Status = currentStatus,
                ConnectedClients = connectedClients,
                ActiveBattles = activeBattles,
                Uptime = isRunning ? Time.time : 0f,
                AutoStart = autoStart,
                DebugMode = showDebugInfo
            };
        }
    }
    
    [Serializable]
    public class ServerInfo
    {
        public bool IsRunning { get; set; }
        public int Port { get; set; }
        public string Status { get; set; }
        public int ConnectedClients { get; set; }
        public int ActiveBattles { get; set; }
        public float Uptime { get; set; }
        public bool AutoStart { get; set; }
        public bool DebugMode { get; set; }
    }
}