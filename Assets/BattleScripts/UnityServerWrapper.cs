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
        [SerializeField] private bool startOnAwake = true;
        [SerializeField] private bool autoRestart = false;

        [Header("Debug Settings")]
        [SerializeField] private bool logServerMessages = true;
        [SerializeField] private bool logClientConnections = true;

        private BattleServer battleServer;
        private CancellationTokenSource cancellationTokenSource;

        public bool IsServerRunning => battleServer?.IsRunning ?? false;
        public int ConnectedClients => battleServer?.ConnectedClients ?? 0;
        public int ActiveGames => battleServer?.ActiveGamesCount ?? 0;

        public event Action<string> OnServerStatusChanged;
        public event Action<string> OnServerMessage;

        void Awake()
        {
            if (startOnAwake)
            {
                StartServer();
            }
        }

        void OnDestroy()
        {
            StopServer();
        }

        void OnApplicationQuit()
        {
            StopServer();
        }

        public async void StartServer()
        {
            if (IsServerRunning)
            {
                Debug.LogWarning("Server is already running");
                return;
            }

            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                battleServer = new BattleServer(serverPort);

                // Subscribe to server events
                battleServer.OnServerMessage += HandleServerMessage;
                battleServer.OnClientConnected += HandleClientConnected;
                battleServer.OnClientDisconnected += HandleClientDisconnected;

                await battleServer.StartAsync();
                
                OnServerStatusChanged?.Invoke("Server started successfully");
                
                if (logServerMessages)
                {
                    Debug.Log($"Battle Server started on port {serverPort}");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to start server: {ex.Message}";
                OnServerStatusChanged?.Invoke(errorMessage);
                
                if (logServerMessages)
                {
                    Debug.LogError(errorMessage);
                }

                if (autoRestart)
                {
                    Debug.Log("Auto-restart enabled, attempting to restart server in 5 seconds...");
                    await Task.Delay(5000);
                    StartServer();
                }
            }
        }

        public void StopServer()
        {
            if (!IsServerRunning)
            {
                return;
            }

            try
            {
                cancellationTokenSource?.Cancel();
                battleServer?.Stop();
                
                // Unsubscribe from events
                if (battleServer != null)
                {
                    battleServer.OnServerMessage -= HandleServerMessage;
                    battleServer.OnClientConnected -= HandleClientConnected;
                    battleServer.OnClientDisconnected -= HandleClientDisconnected;
                }

                battleServer = null;
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;

                OnServerStatusChanged?.Invoke("Server stopped");
                
                if (logServerMessages)
                {
                    Debug.Log("Battle Server stopped");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error stopping server: {ex.Message}";
                OnServerStatusChanged?.Invoke(errorMessage);
                
                if (logServerMessages)
                {
                    Debug.LogError(errorMessage);
                }
            }
        }

        public void RestartServer()
        {
            StopServer();
            StartServer();
        }

        private void HandleServerMessage(string message)
        {
            OnServerMessage?.Invoke(message);
            
            if (logServerMessages)
            {
                Debug.Log($"[Server] {message}");
            }
        }

        private void HandleClientConnected(string clientId)
        {
            if (logClientConnections)
            {
                Debug.Log($"[Server] Client connected: {clientId}");
            }
        }

        private void HandleClientDisconnected(string clientId)
        {
            if (logClientConnections)
            {
                Debug.Log($"[Server] Client disconnected: {clientId}");
            }
        }

        [ContextMenu("Start Server")]
        public void StartServerFromInspector()
        {
            StartServer();
        }

        [ContextMenu("Stop Server")]
        public void StopServerFromInspector()
        {
            StopServer();
        }

        [ContextMenu("Restart Server")]
        public void RestartServerFromInspector()
        {
            RestartServer();
        }

        void OnGUI()
        {
            if (!Application.isEditor) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Battle Server Status", EditorStyles.boldLabel);
            GUILayout.Label($"Running: {IsServerRunning}");
            GUILayout.Label($"Port: {serverPort}");
            GUILayout.Label($"Connected Clients: {ConnectedClients}");
            GUILayout.Label($"Active Games: {ActiveGames}");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button(IsServerRunning ? "Stop Server" : "Start Server"))
            {
                if (IsServerRunning)
                    StopServer();
                else
                    StartServer();
            }
            
            if (IsServerRunning && GUILayout.Button("Restart Server"))
            {
                RestartServer();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }

    public static class EditorStyles
    {
        public static GUIStyle boldLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
    }
}