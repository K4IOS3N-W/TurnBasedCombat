using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using BattleSystem.Server;

namespace BattleSystem
{
    public class UnityServerWrapper : MonoBehaviour
    {
        [SerializeField] private bool startServerOnAwake = false;
        [SerializeField] private int serverPort = 7777;
        [SerializeField] private bool enableServerLogging = true;

        private BattleServer server;
        private CancellationTokenSource serverCancellationToken;
        private Task serverTask;

        private void Awake()
        {
            if (startServerOnAwake)
            {
                StartServer();
            }
        }

        public void StartServer()
        {
            if (server != null)
            {
                Debug.LogWarning("Servidor já está em execução");
                return;
            }

            server = new BattleServer(serverPort);
            serverCancellationToken = new CancellationTokenSource();

            if (enableServerLogging)
            {
                server.OnLog += (message) => Debug.Log($"[BattleServer] {message}");
                server.OnError += (exception) => Debug.LogError($"[BattleServer] {exception.Message}");
            }

            // Iniciar servidor em uma task separada
            serverTask = Task.Run(async () => {
                try
                {
                    await server.Start();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Erro ao iniciar servidor: {ex.Message}");
                }
            }, serverCancellationToken.Token);

            Debug.Log($"Servidor iniciado na porta {serverPort}");
        }

        public void StopServer()
        {
            if (server == null)
            {
                Debug.LogWarning("Servidor não está em execução");
                return;
            }

            server.Stop();
            serverCancellationToken.Cancel();
            server = null;
            Debug.Log("Servidor encerrado");
        }

        public bool IsServerRunning()
        {
            return server != null && serverTask != null && !serverTask.IsCompleted;
        }

        private void OnDestroy()
        {
            StopServer();
        }
    }
}