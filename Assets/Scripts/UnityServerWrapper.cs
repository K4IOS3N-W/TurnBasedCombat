using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using BattleSystem.Server;

namespace BattleSystem
{
    public class UnityServerWrapper : MonoBehaviour
    {
        [Header("Server Settings")]
        [SerializeField] private int serverPort = 7777;
        [SerializeField] private bool startServerOnAwake = true;
        [SerializeField] private bool logToConsole = true;
        
        [Header("Status")]
        [SerializeField] private bool isServerRunning;
        [SerializeField] private string serverStatus = "Stopped";
        
        private BattleServer battleServer;
        private bool isInitialized = false;
        
        public event Action<string> OnServerLog;
        public event Action<bool> OnServerStatusChanged;
        
        private void Awake()
        {
            if (startServerOnAwake)
            {
                StartServer();
            }
        }
        
        public async void StartServer()
        {
            if (isServerRunning)
            {
                LogToConsole("Server is already running");
                return;
            }

            try
            {
                serverStatus = "Starting...";
                OnServerStatusChanged?.Invoke(false);

                battleServer = new BattleServer(serverPort);
                
                // Subscribe to server events if available
                // battleServer.OnLog += (message) => LogToConsole($"[Server] {message}");
                
                LogToConsole($"Starting server on port {serverPort}...");
                
                // Start server in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await battleServer.Start();
                    }
                    catch (Exception ex)
                    {
                        LogToConsole($"Server error: {ex.Message}");
                    }
                });

                // Give server time to initialize
                await Task.Delay(1000);

                isServerRunning = true;
                serverStatus = $"Running on port {serverPort}";
                OnServerStatusChanged?.Invoke(true);
                
                LogToConsole("Server started successfully");
            }
            catch (Exception ex)
            {
                serverStatus = $"Failed to start: {ex.Message}";
                LogToConsole($"Failed to start server: {ex.Message}");
                OnServerStatusChanged?.Invoke(false);
            }
        }

        public void StopServer()
        {
            if (!isServerRunning) return;

            try
            {
                battleServer?.Stop();
                isServerRunning = false;
                serverStatus = "Stopped";
                OnServerStatusChanged?.Invoke(false);
                LogToConsole("Server stopped");
            }
            catch (Exception ex)
            {
                LogToConsole($"Error stopping server: {ex.Message}");
            }
        }

        private void LogToConsole(string message)
        {
            if (logToConsole)
            {
                Debug.Log($"[UnityServerWrapper] {message}");
            }
            OnServerLog?.Invoke(message);
        }

        private void OnDestroy()
        {
            StopServer();
        }
        
        // Public properties for inspector display
        public bool IsServerRunning => isServerRunning;
        public string ServerStatus => serverStatus;
        public int Port => serverPort;
        
        // Unity Editor methods for testing
        [ContextMenu("Start Server")]
        private void StartServerContext()
        {
            StartServer();
        }
        
        [ContextMenu("Stop Server")]
        private void StopServerContext()
        {
            StopServer();
        }
        
        [ContextMenu("Restart Server")]
        private void RestartServerContext()
        {
            StopServer();
            // Wait a moment before restarting
            Invoke(nameof(StartServer), 1f);
        }
    }
}