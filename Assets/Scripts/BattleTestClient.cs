using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BattleSystem
{
    public class BattleTestClient : MonoBehaviour
    {
        #region Campos Serializados

        [Header("Configurações de Conexão")]
        [SerializeField] private string serverIp = "127.0.0.1";
        [SerializeField] private int serverPort = 7777;
        [SerializeField] private bool autoConnectOnStart = true;

        [Header("UI - Conexão")]
        [SerializeField] private Button connectButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private TextMeshProUGUI connectionStatusText;

        [Header("UI - Criação de Batalha")]
        [SerializeField] private Button createBattleButton;
        [SerializeField] private TMP_InputField battleIdInputField;

        [Header("UI - Entrar na Batalha")]
        [SerializeField] private Button joinBattleButton;
        [SerializeField] private TMP_InputField playerNameInputField;
        [SerializeField] private TMP_Dropdown classDropdown;
        [SerializeField] private TextMeshProUGUI playerIdText;

        [Header("UI - Controle da Batalha")]
        [SerializeField] private Button startBattleButton;
        [SerializeField] private TMP_Dropdown enemySelectionDropdown;
        [SerializeField] private Button addEnemyButton;
        [SerializeField] private TextMeshProUGUI selectedEnemiesText;

        [Header("UI - Preparação")]
        [SerializeField] private Button toggleReadyButton;
        [SerializeField] private TextMeshProUGUI readyStatusText;
        [SerializeField] private TextMeshProUGUI roomCodeText;

        [Header("UI - Ações de Batalha")]
        [SerializeField] private Button attackButton;
        [SerializeField] private Button skillButton;
        [SerializeField] private Button passButton;
        [SerializeField] private TMP_Dropdown targetSelectionDropdown;
        [SerializeField] private TMP_Dropdown skillSelectionDropdown;

        [Header("UI - Estado da Batalha")]
        [SerializeField] private Button refreshStateButton;
        [SerializeField] private TextMeshProUGUI battleStateText;
        [SerializeField] private TextMeshProUGUI currentTurnText;
        [SerializeField] private TextMeshProUGUI logText;

        [Header("UI - Painéis")]
        [SerializeField] private GameObject connectPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject preparationPanel;
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private GameObject endBattlePanel;

        [Header("UI - Modo de Batalha")]
        [SerializeField] private TMP_Dropdown battleModeDropdown;

        [Header("UI - Seleção de Equipe")]
        [SerializeField] private TMP_Dropdown teamSelectionDropdown;
        [SerializeField] private TMP_InputField createTeamInputField;
        [SerializeField] private Button createTeamButton;
        [SerializeField] private Button refreshTeamsButton;
        [SerializeField] private Button joinTeamButton;

        [Header("UI - Progressão do Jogador")]
        [SerializeField] private TextMeshProUGUI playerProgressText;
        [SerializeField] private Button viewProgressButton;

        [Header("UI - Retorno ao Lobby")]
        [SerializeField] private Button returnToLobbyButton;

        [Header("UI - Resultado da Batalha")]
        [SerializeField] private TextMeshProUGUI battleResultText;

        #endregion

        #region Campos Privados

        private enum ClientState
        {
            Disconnected,
            Connected,
            InLobby,
            PreparingBattle,
            InBattle,
            BattleEnded
        }

        private enum BattleMode
        {
            PvE,  // Jogadores contra inimigos
            PvP   // Jogadores contra jogadores
        }

        private ClientState currentState = ClientState.Disconnected;
        private BattleMode currentBattleMode = BattleMode.PvE;
        private bool isTeamReady = false;

        // Informações da batalha
        private string battleId;
        private string playerId;
        private string teamId;
        private List<string> selectedEnemyIds = new List<string>();
        private Battle currentBattle;

        // Conexão com o servidor
        private TcpClient tcpClient;
        private NetworkStream stream;
        private bool isConnected = false;
        private byte[] receiveBuffer = new byte[4096];

        // Dicionários para mapear IDs e nomes
        private Dictionary<string, string> teamIdNameMap = new Dictionary<string, string>();
        private Dictionary<string, string> targetIdMap = new Dictionary<string, string>();
        private Dictionary<string, string> skillIdMap = new Dictionary<string, string>();
        private Dictionary<string, ClassMasteryInfo> localClassProgress = new Dictionary<string, ClassMasteryInfo>();

        #endregion

        #region Ciclo de Vida Unity

        private void Start()
        {
            InitializeUI();
            ChangeState(ClientState.Disconnected);

            // Inicializar os dropdowns
            InitializeDropdowns();

            if (autoConnectOnStart)
            {
                Connect();
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        #endregion

        #region Inicialização

        private void InitializeUI()
        {
            // Configurar listeners para os botões
            if (connectButton != null) connectButton.onClick.AddListener(Connect);
            if (disconnectButton != null) disconnectButton.onClick.AddListener(Disconnect);
            if (createBattleButton != null) createBattleButton.onClick.AddListener(CreateBattle);
            if (joinBattleButton != null) joinBattleButton.onClick.AddListener(JoinBattle);
            if (startBattleButton != null) startBattleButton.onClick.AddListener(StartBattle);
            if (addEnemyButton != null) addEnemyButton.onClick.AddListener(AddEnemyToList);
            if (attackButton != null) attackButton.onClick.AddListener(ExecuteAttack);
            if (skillButton != null) skillButton.onClick.AddListener(ExecuteSkill);
            if (passButton != null) passButton.onClick.AddListener(ExecutePass);
            if (refreshStateButton != null) refreshStateButton.onClick.AddListener(GetBattleState);
            if (createTeamButton != null) createTeamButton.onClick.AddListener(CreateTeam);
            if (refreshTeamsButton != null) refreshTeamsButton.onClick.AddListener(RefreshTeams);
            if (returnToLobbyButton != null) returnToLobbyButton.onClick.AddListener(ReturnToLobby);
            if (joinTeamButton != null) joinTeamButton.onClick.AddListener(JoinSelectedTeam);
            if (toggleReadyButton != null) toggleReadyButton.onClick.AddListener(ToggleTeamReady);
            if (viewProgressButton != null) viewProgressButton.onClick.AddListener(ShowPlayerProgress);

            // Configurar o listener do dropdown de modo de batalha
            if (battleModeDropdown != null)
                battleModeDropdown.onValueChanged.AddListener(OnBattleModeChanged);

            // Desabilitar botões que requerem conexão
            UpdateButtonsBasedOnState(ClientState.Disconnected);

            // Estado inicial da UI
            UpdateConnectionStatus("Desconectado");
            LogMessage("Cliente de teste iniciado.");
        }

        private void InitializeDropdowns()
        {
            // Dropdown de classes
            if (classDropdown != null)
            {
                classDropdown.ClearOptions();
                classDropdown.AddOptions(new List<string> { "warrior", "mage", "healer" });
            }

            // Dropdown de inimigos
            if (enemySelectionDropdown != null)
            {
                enemySelectionDropdown.ClearOptions();
                enemySelectionDropdown.AddOptions(new List<string> {
                    "enemy_goblin", "enemy_orc", "enemy_necromancer", "enemy_dragon"
                });
            }

            // Dropdown de modos de batalha
            if (battleModeDropdown != null)
            {
                battleModeDropdown.ClearOptions();
                battleModeDropdown.AddOptions(new List<string> {
                    "PvE (Jogadores vs Inimigos)",
                    "PvP (Jogadores vs Jogadores)"
                });
            }
        }

        #endregion

        #region Gerenciamento de Estado

        private void ChangeState(ClientState newState)
        {
            LogMessage($"Mudando estado: {currentState} -> {newState}");
            currentState = newState;

            // Atualizar UI com base no novo estado
            UpdateUI();
            UpdateButtonsBasedOnState(newState);
        }

        private void UpdateUI()
        {
            // Esconder todos os painéis
            if (connectPanel != null) connectPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            if (preparationPanel != null) preparationPanel.SetActive(false);
            if (battlePanel != null) battlePanel.SetActive(false);
            if (endBattlePanel != null) endBattlePanel.SetActive(false);

            // Mostrar apenas o painel relevante para o estado atual
            switch (currentState)
            {
                case ClientState.Disconnected:
                    if (connectPanel != null) connectPanel.SetActive(true);
                    break;

                case ClientState.Connected:
                case ClientState.InLobby:
                    if (lobbyPanel != null) lobbyPanel.SetActive(true);
                    break;

                case ClientState.PreparingBattle:
                    if (preparationPanel != null) preparationPanel.SetActive(true);
                    break;

                case ClientState.InBattle:
                    if (battlePanel != null) battlePanel.SetActive(true);
                    break;

                case ClientState.BattleEnded:
                    if (endBattlePanel != null) endBattlePanel.SetActive(true);
                    break;
            }
        }

        private void UpdateButtonsBasedOnState(ClientState state)
        {
            // Desabilitar todos os botões primeiro
            if (connectButton != null) connectButton.interactable = false;
            if (disconnectButton != null) disconnectButton.interactable = false;
            if (createBattleButton != null) createBattleButton.interactable = false;
            if (joinBattleButton != null) joinBattleButton.interactable = false;
            if (startBattleButton != null) startBattleButton.interactable = false;
            if (addEnemyButton != null) addEnemyButton.interactable = false;
            if (attackButton != null) attackButton.interactable = false;
            if (skillButton != null) skillButton.interactable = false;
            if (passButton != null) passButton.interactable = false;
            if (refreshStateButton != null) refreshStateButton.interactable = false;
            if (createTeamButton != null) createTeamButton.interactable = false;
            if (refreshTeamsButton != null) refreshTeamsButton.interactable = false;
            if (joinTeamButton != null) joinTeamButton.interactable = false;
            if (returnToLobbyButton != null) returnToLobbyButton.interactable = false;
            if (toggleReadyButton != null) toggleReadyButton.interactable = false;

            // Ativar botões relevantes para o estado atual
            switch (state)
            {
                case ClientState.Disconnected:
                    if (connectButton != null) connectButton.interactable = true;
                    break;

                case ClientState.Connected:
                case ClientState.InLobby:
                    if (disconnectButton != null) disconnectButton.interactable = true;
                    if (createBattleButton != null) createBattleButton.interactable = true;
                    if (joinBattleButton != null) joinBattleButton.interactable = true;
                    break;

                case ClientState.PreparingBattle:
                    if (disconnectButton != null) disconnectButton.interactable = true;
                    if (startBattleButton != null) startBattleButton.interactable = true;
                    if (createTeamButton != null) createTeamButton.interactable = true;
                    if (refreshTeamsButton != null) refreshTeamsButton.interactable = true;
                    if (joinTeamButton != null) joinTeamButton.interactable = true;
                    if (toggleReadyButton != null) toggleReadyButton.interactable = true;
                    if (currentBattleMode == BattleMode.PvE && addEnemyButton != null)
                        addEnemyButton.interactable = true;
                    break;

                case ClientState.InBattle:
                    if (disconnectButton != null) disconnectButton.interactable = true;
                    if (refreshStateButton != null) refreshStateButton.interactable = true;

                    // Botões de ação só estão ativos no turno do jogador (verificado em UpdateBattleStateUI)
                    bool isPlayerTurn = currentBattle != null && currentBattle.CurrentParticipant == playerId;
                    if (attackButton != null) attackButton.interactable = isPlayerTurn;
                    if (skillButton != null) skillButton.interactable = isPlayerTurn;
                    if (passButton != null) passButton.interactable = isPlayerTurn;
                    break;

                case ClientState.BattleEnded:
                    if (disconnectButton != null) disconnectButton.interactable = true;
                    if (returnToLobbyButton != null) returnToLobbyButton.interactable = true;
                    break;
            }
        }

        #endregion

        #region Eventos de UI

        private void OnBattleModeChanged(int index)
        {
            currentBattleMode = (BattleMode)index;
            LogMessage($"Modo de batalha alterado para: {currentBattleMode}");

            // Atualizar visibilidade de controles baseado no modo
            bool isPvE = currentBattleMode == BattleMode.PvE;
            if (enemySelectionDropdown != null) enemySelectionDropdown.gameObject.SetActive(isPvE);
            if (addEnemyButton != null) addEnemyButton.gameObject.SetActive(isPvE);

            // Atualizar interatividade dos botões para o estado atual
            UpdateButtonsBasedOnState(currentState);
        }

        #endregion

        #region Conexão com o Servidor

        public async void Connect()
        {
            if (isConnected)
            {
                LogMessage("Já está conectado ao servidor");
                return;
            }

            try
            {
                LogMessage($"Tentando conectar ao servidor: {serverIp}:{serverPort}...");
                UpdateConnectionStatus("Conectando...");

                // Criar nova instância do TcpClient
                tcpClient = new TcpClient();

                // Definir timeout para a conexão
                var connectTask = tcpClient.ConnectAsync(serverIp, serverPort);
                var timeoutTask = Task.Delay(5000); // Timeout de 5 segundos

                // Aguardar a conexão ou o timeout
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Timeout ocorreu
                    throw new TimeoutException("Tempo de conexão esgotado");
                }

                // Garantir que a tarefa de conexão completou sem exceções
                await connectTask;

                // Verificar se a conexão foi estabelecida
                if (!tcpClient.Connected)
                {
                    throw new Exception("Falha ao estabelecer conexão");
                }

                // Obter o stream para comunicação
                stream = tcpClient.GetStream();
                isConnected = true;

                UpdateConnectionStatus("Conectado");
                LogMessage("Conectado com sucesso ao servidor.");

                // Mudar para o estado Connected
                ChangeState(ClientState.Connected);

                // Inicia a leitura contínua de mensagens em uma task separada
                _ = ListenForMessages();
            }
            catch (Exception ex)
            {
                // Fechar recursos se necessário
                if (tcpClient != null && tcpClient.Connected)
                {
                    tcpClient.Close();
                }

                tcpClient = null;
                stream = null;
                isConnected = false;

                UpdateConnectionStatus($"Erro: {ex.Message}");
                LogMessage($"Falha ao conectar: {ex.Message}");
                ChangeState(ClientState.Disconnected);
            }
        }

        public void Disconnect()
        {
            if (!isConnected) return;

            LogMessage("Desconectando do servidor...");

            try
            {
                // Marcar como desconectado primeiro para parar o loop de leitura
                isConnected = false;

                // Fechar recursos de rede
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }

                if (tcpClient != null)
                {
                    tcpClient.Close();
                    tcpClient = null;
                }

                UpdateConnectionStatus("Desconectado");
                LogMessage("Desconectado do servidor.");

                // Limpar dados da batalha
                battleId = null;
                playerId = null;
                teamId = null;
                selectedEnemyIds.Clear();
                currentBattle = null;

                // Resetar campos da UI
                if (battleIdInputField != null) battleIdInputField.text = "";
                if (playerIdText != null) playerIdText.text = "Player ID: N/A";
                if (battleStateText != null) battleStateText.text = "N/A";
                if (currentTurnText != null) currentTurnText.text = "N/A";
                if (selectedEnemiesText != null) selectedEnemiesText.text = "";

                // Mudar para o estado desconectado
                ChangeState(ClientState.Disconnected);
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao desconectar: {ex.Message}");
            }
        }

        private async Task ListenForMessages()
        {
            LogMessage("Iniciando ciclo de escuta de mensagens");

            while (isConnected && tcpClient != null && tcpClient.Connected)
            {
                try
                {
                    // Verifica se o stream está disponível
                    if (stream == null || !tcpClient.Connected)
                    {
                        LogMessage("Stream não disponível. Desconectando...");
                        Disconnect();
                        break;
                    }

                    // Configurar um CancellationTokenSource com timeout para evitar bloqueios indefinidos
                    using (var cts = new System.Threading.CancellationTokenSource(30000)) // 30 segundos de timeout
                    {
                        // Ler dados do stream com timeout
                        int bytesRead = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length, cts.Token);

                        // Se não recebeu dados, o servidor provavelmente desconectou
                        if (bytesRead <= 0)
                        {
                            LogMessage("Conexão fechada pelo servidor (0 bytes recebidos)");
                            Disconnect();
                            break;
                        }

                        // Processar a mensagem recebida
                        string message = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
                        ProcessMessage(message);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Timeout na leitura
                    LogMessage("Timeout na leitura de dados. Verificando conexão...");

                    // Verificar se o cliente ainda está conectado
                    if (tcpClient != null && tcpClient.Connected)
                    {
                        // A conexão ainda está ativa, continuar o loop
                        continue;
                    }
                    else
                    {
                        LogMessage("Conexão perdida durante timeout");
                        Disconnect();
                        break;
                    }
                }
                catch (IOException ex)
                {
                    // Erro de IO geralmente significa que a conexão foi fechada
                    LogMessage($"Erro de IO: {ex.Message}");
                    Disconnect();
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // O stream ou cliente foi fechado durante a operação
                    LogMessage("Objeto de conexão foi fechado");
                    isConnected = false;
                    break;
                }
                catch (Exception ex)
                {
                    // Outros erros
                    LogMessage($"Erro ao receber mensagem: {ex.Message}");

                    // Verificar se ainda estamos conectados após o erro
                    if (isConnected && tcpClient != null && tcpClient.Connected)
                    {
                        // Erro recuperável, continuar o loop
                        continue;
                    }
                    else
                    {
                        // Erro não recuperável
                        Disconnect();
                        break;
                    }
                }
            }

            LogMessage("Ciclo de escuta de mensagens finalizado");

            // Garantir que o estado de desconexão seja aplicado
            if (isConnected)
            {
                Disconnect();
            }
        }

        private async Task SendMessage(object request)
        {
            if (!isConnected || stream == null)
            {
                LogMessage("Não é possível enviar mensagem: não conectado");
                return;
            }

            try
            {
                // Serializar a requisição para JSON
                string json = JsonConvert.SerializeObject(request);
                LogMessage($"Enviando: {json}");

                // Converter para bytes e enviar
                byte[] data = Encoding.UTF8.GetBytes(json);

                // Usar um CancellationTokenSource para definir timeout no envio
                using (var cts = new System.Threading.CancellationTokenSource(10000)) // 10 segundos de timeout
                {
                    await stream.WriteAsync(data, 0, data.Length, cts.Token);
                    await stream.FlushAsync(cts.Token); // Garantir que os dados sejam enviados imediatamente
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage("Timeout ao enviar mensagem");
                Disconnect();
            }
            catch (IOException ex)
            {
                LogMessage($"Erro de IO ao enviar mensagem: {ex.Message}");
                Disconnect();
            }
            catch (ObjectDisposedException)
            {
                LogMessage("Tentativa de envio em conexão fechada");
                isConnected = false;
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao enviar mensagem: {ex.Message}");
                Disconnect();
            }
        }

        #endregion
        #region Processamento de Mensagens

        private void ProcessMessage(string message)
        {
            try
            {
                LogMessage($"Recebido: {message}");

                var baseResponse = JsonConvert.DeserializeObject<BaseResponse>(message);

                if (!baseResponse.Success)
                {
                    LogMessage($"ERRO do servidor: {baseResponse.Message}");
                    return;
                }

                // Tratamento específico por tipo de mensagem
                if (message.Contains("\"level\"") && message.Contains("\"experienceToNextLevel\""))
                {
                    var expResponse = JsonConvert.DeserializeObject<ExperienceUpdateResponse>(message);
                    HandleExperienceUpdate(expResponse);
                }
                else if (message.Contains("\"battleId\"") && !message.Contains("\"battle\""))
                {
                    var response = JsonConvert.DeserializeObject<CreateBattleResponse>(message);
                    HandleCreateBattleResponse(response);
                }
                else if (message.Contains("\"teamId\"") && message.Contains("\"teamName\"") && !message.Contains("\"playerId\""))
                {
                    var response = JsonConvert.DeserializeObject<CreateTeamResponse>(message);
                    HandleCreateTeamResponse(response);
                }
                else if (message.Contains("\"playerId\""))
                {
                    var response = JsonConvert.DeserializeObject<JoinBattleResponse>(message);
                    HandleJoinBattleResponse(response);
                }
                else if (message.Contains("\"turnOrder\""))
                {
                    var response = JsonConvert.DeserializeObject<StartBattleResponse>(message);
                    HandleStartBattleResponse(response);
                }
                else if (message.Contains("\"results\""))
                {
                    var response = JsonConvert.DeserializeObject<ExecuteActionResponse>(message);
                    HandleExecuteActionResponse(response);
                }
                else if (message.Contains("\"battle\""))
                {
                    var response = JsonConvert.DeserializeObject<GetBattleStateResponse>(message);
                    HandleBattleStateResponse(response);
                }
                else
                {
                    LogMessage($"Mensagem processada: {baseResponse.Message}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao processar mensagem: {ex.Message}");
            }
        }

        #endregion

        #region Handlers de Resposta

        private void HandleCreateTeamResponse(CreateTeamResponse response)
        {
            LogMessage($"Equipe criada com sucesso: {response.TeamName} (ID: {response.TeamId})");
            // Atualizar estado da batalha para ver a nova equipe
            GetBattleState();
        }

        private void HandleCreateBattleResponse(CreateBattleResponse response)
        {
            battleId = response.BattleId;
            if (battleIdInputField != null) battleIdInputField.text = response.RoomCode;
            if (roomCodeText != null) roomCodeText.text = $"Código da Sala: {response.RoomCode}";

            LogMessage($"Batalha criada com código: {response.RoomCode}");

            // Após criar uma batalha, entramos no modo de preparação
            ChangeState(ClientState.PreparingBattle);
        }

        private void HandleJoinBattleResponse(JoinBattleResponse response)
        {
            playerId = response.PlayerId;
            teamId = response.TeamId;

            if (playerIdText != null) playerIdText.text = $"Player ID: {playerId} | Equipe: {response.TeamName}";
            LogMessage($"Entrou na batalha (ID: {playerId})");
            LogMessage($"Equipe: {response.TeamName}");

            // Inicializar progresso da classe
            InitializeClassProgress();

            // Transicionar para o estado de preparação de batalha
            ChangeState(ClientState.PreparingBattle);

            // Solicitar o estado atual da batalha para ver outros jogadores
            GetBattleState();
        }

        private void InitializeClassProgress()
        {
            if (classDropdown == null) return;

            string currentClass = classDropdown.options[classDropdown.value].text;
            if (!localClassProgress.ContainsKey(currentClass))
            {
                localClassProgress[currentClass] = new ClassMasteryInfo
                {
                    Class = currentClass,
                    Level = 1,
                    Experience = 0,
                    ExperienceToNextLevel = 100,
                    SessionDuration = TimeSpan.Zero,
                    BattlesWon = 0,
                    BattlesLost = 0,
                    WinRate = 0,
                    TotalDamageDealt = 0,
                    TotalHealingDone = 0
                };
            }

            UpdatePlayerProgressUI();
        }

        private void HandleStartBattleResponse(StartBattleResponse response)
        {
            LogMessage("Batalha iniciada. Ordem de turnos:");
            for (int i = 0; i < response.TurnOrder.Count; i++)
            {
                LogMessage($"{i + 1}: {response.TurnOrder[i]}");
            }

            // Quando a batalha começa, mudamos para o estado InBattle
            ChangeState(ClientState.InBattle);

            // Solicitar estado da batalha para atualizar UI
            GetBattleState();
        }

        private void HandleBattleEndedEvent(Battle battle)
        {
            string result = "A batalha terminou! ";

            if (battle.State == BattleState.Finished)
            {
                // Verificar resultado da batalha
                bool playersWon = false;

                if (battle.IsPvP)
                {
                    // Em PvP, verificar se o time do jogador venceu
                    Team playerTeam = battle.Teams.FirstOrDefault(t => t.Players.Any(p => p.Id == playerId));
                    playersWon = playerTeam != null && playerTeam.Players.Any(p => p.IsAlive);
                }
                else
                {
                    // Em PvE, verificar se algum jogador sobreviveu e todos os inimigos foram derrotados
                    playersWon = battle.Teams.Any(t => t.Players.Any(p => p.IsAlive)) &&
                                 battle.Enemies.All(e => !e.IsAlive);
                }

                result += playersWon ? "Sua equipe venceu!" : "Sua equipe perdeu!";
            }

            LogMessage(result);
            if (battleResultText != null) battleResultText.text = result;

            // Mudar para o estado de fim de batalha
            ChangeState(ClientState.BattleEnded);
        }

        private void HandleExecuteActionResponse(ExecuteActionResponse response)
        {
            LogMessage($"Ação executada. Próximo jogador: {response.NextPlayer}");

            if (response.Results != null && response.Results.Count > 0)
            {
                LogMessage("Resultados da ação:");
                foreach (var result in response.Results)
                {
                    string resultMsg = $"Alvo {result.TargetId}: ";

                    // Mostrar informações de dano
                    if (result.DamageReceived > 0)
                        resultMsg += $"Dano: {result.DamageReceived} ";

                    // Mostrar informações de cura
                    if (result.HealingReceived > 0)
                        resultMsg += $"Cura: {result.HealingReceived} ";

                    // Verificar se o alvo foi derrotado
                    if (result.IsDead)
                        resultMsg += "(Derrotado)";

                    LogMessage(resultMsg);
                }
            }

            // Atualizar estado da batalha após a ação para ver as mudanças
            GetBattleState();
        }

        private void HandleBattleStateResponse(GetBattleStateResponse response)
        {
            // Atualizar o battleId se estiver vindo junto com a resposta
            if (!string.IsNullOrEmpty(response.BattleId))
            {
                battleId = response.BattleId;
                LogMessage($"ID da batalha atualizado: {battleId}");

                // Se estamos entrando em uma batalha pela primeira vez, mude para o estado PreparingBattle
                if (currentState == ClientState.Connected || currentState == ClientState.InLobby)
                {
                    ChangeState(ClientState.PreparingBattle);
                    LogMessage("Entrando na sala de preparação da batalha");
                }
            }

            Battle previousBattle = currentBattle;
            currentBattle = response.Battle;

            // Verificar se a batalha acabou de terminar
            if (previousBattle != null &&
                previousBattle.State == BattleState.InProgress &&
                currentBattle.State == BattleState.Finished)
            {
                HandleBattleEndedEvent(currentBattle);
                return;
            }

            // Atualizar UI
            UpdateBattleStateUI();
            UpdateTeamDropdown();
            UpdateTargetsDropdown();
            UpdateSkillsDropdown();

            // Se estamos em batalha, ajustar interatividade dos botões de ação
            if (currentState == ClientState.InBattle && currentBattle != null)
            {
                bool isPlayerTurn = currentBattle.CurrentParticipant == playerId;
                if (attackButton != null) attackButton.interactable = isPlayerTurn;
                if (skillButton != null) skillButton.interactable = isPlayerTurn;
                if (passButton != null) passButton.interactable = isPlayerTurn;
            }
        }

        private void HandleExperienceUpdate(ExperienceUpdateResponse response)
        {
            LogMessage($"Experiência atualizada: +{response.XpGained} XP");

            // Atualizar dados de progresso local
            string currentClass = response.Class;
            if (!localClassProgress.ContainsKey(currentClass))
            {
                localClassProgress[currentClass] = new ClassMasteryInfo
                {
                    Class = currentClass,
                    Level = response.Level,
                    Experience = response.Experience,
                    ExperienceToNextLevel = response.ExperienceToNextLevel
                };
            }
            else
            {
                localClassProgress[currentClass].Level = response.Level;
                localClassProgress[currentClass].Experience = response.Experience;
                localClassProgress[currentClass].ExperienceToNextLevel = response.ExperienceToNextLevel;
            }

            // Mostrar mensagem de level up
            if (response.LeveledUp)
            {
                string newSkills = response.NewSkillsLearned.Count > 0
                    ? $"\nNovas habilidades: {string.Join(", ", response.NewSkillsLearned)}"
                    : "";

                string statBonuses = "";
                foreach (var bonus in response.StatBonuses)
                {
                    statBonuses += $"\n+{bonus.Value} {bonus.Key}";
                }

                LogMessage($"LEVEL UP! Agora você é um {currentClass} nível {response.Level}!{newSkills}{statBonuses}");
            }

            // Atualizar UI de progresso
            UpdatePlayerProgressUI();
        }

        #endregion

        #region Ações do Cliente

        public async void CreateBattle()
        {
            var request = new CreateBattleRequest
            {
                RequestType = "CreateBattle"
            };

            await SendMessage(request);
        }

        public void ReturnToLobby()
        {
            // Limpar dados da batalha atual
            battleId = null;
            playerId = null;
            teamId = null;
            selectedEnemyIds.Clear();
            currentBattle = null;

            // Voltar para o estado de lobby
            ChangeState(ClientState.InLobby);
        }

        public async void CreateTeam()
        {
            if (string.IsNullOrEmpty(battleId))
            {
                LogMessage("ID da batalha não especificado");
                return;
            }

            string teamName = !string.IsNullOrEmpty(createTeamInputField.text)
                ? createTeamInputField.text
                : "Time " + UnityEngine.Random.Range(1, 100);

            var request = new CreateTeamRequest
            {
                RequestType = "CreateTeam",
                BattleId = battleId,
                TeamName = teamName
            };

            await SendMessage(request);
        }

        public void RefreshTeams()
        {
            if (string.IsNullOrEmpty(battleId))
            {
                LogMessage("ID da batalha não especificado");
                return;
            }

            GetBattleState();
        }

        public async void JoinBattle()
        {
            if (string.IsNullOrEmpty(battleIdInputField.text))
            {
                LogMessage("Código da sala não especificado");
                return;
            }

            // Verificar se o que foi digitado é um código de sala ou ID
            string inputCode = battleIdInputField.text.Trim();
            bool isRoomCode = inputCode.Length == 5 && inputCode.All(char.IsDigit);

            var stateRequest = new GetBattleStateRequest
            {
                RequestType = "GetBattleState"
            };

            if (isRoomCode)
            {
                LogMessage($"Usando como código de sala: {inputCode}");
                stateRequest.RoomCode = inputCode;
            }
            else
            {
                LogMessage($"Usando como ID de batalha: {inputCode}");
                battleId = inputCode;
                stateRequest.BattleId = battleId;
            }

            await SendMessage(stateRequest);
            LogMessage("Verificando informações da sala...");
        }

        public async void JoinSelectedTeam()
        {
            if (string.IsNullOrEmpty(battleId) || teamSelectionDropdown == null || teamSelectionDropdown.options.Count == 0)
            {
                LogMessage("Não há equipes disponíveis ou ID da batalha não especificado");
                return;
            }

            string playerName = !string.IsNullOrEmpty(playerNameInputField.text)
                ? playerNameInputField.text
                : $"Player {UnityEngine.Random.Range(1000, 9999)}";

            // Obter a opção selecionada no dropdown
            string selectedTeam = teamSelectionDropdown.options[teamSelectionDropdown.value].text;

            // Verificar se é para criar uma nova equipe
            if (selectedTeam == "+ Criar Nova Equipe")
            {
                await CreateTeamAndJoin(playerName);
                return;
            }

            // Encontrar o ID da equipe correspondente à seleção
            string selectedTeamId = FindTeamId(selectedTeam);
            if (string.IsNullOrEmpty(selectedTeamId))
            {
                LogMessage($"Não foi possível identificar o ID da equipe para: {selectedTeam}");
                return;
            }

            // Verificar se o jogador já está nesta equipe
            if (teamId == selectedTeamId && !string.IsNullOrEmpty(playerId))
            {
                LogMessage($"Você já está na equipe com ID: {selectedTeamId}");
                return;
            }

            // Enviar requisição para entrar na equipe
            await JoinTeam(playerName, selectedTeamId);
        }

        private string FindTeamId(string selectedTeam)
        {
            // Procurar a entrada exata primeiro
            if (teamIdNameMap.ContainsKey(selectedTeam))
                return teamIdNameMap[selectedTeam];

            // Tentar encontrar o ID da equipe nas entradas do dicionário
            foreach (var entry in teamIdNameMap)
            {
                // Se a entrada do dicionário contém o início do texto selecionado
                if (selectedTeam.StartsWith(entry.Key.Split('(')[0].Trim()))
                    return entry.Value;
            }

            return null;
        }

        private async Task CreateTeamAndJoin(string playerName)
        {
            // Criar uma equipe
            string teamName = !string.IsNullOrEmpty(createTeamInputField.text)
                ? createTeamInputField.text
                : "Time " + UnityEngine.Random.Range(1, 100);

            await CreateTeamWithName(teamName);

            // Aguardar um pouco para garantir que a equipe foi criada
            await Task.Delay(500);
            GetBattleState();
            await Task.Delay(500);

            // Entrar na equipe recém-criada (o servidor escolherá a última equipe criada)
            await JoinTeam(playerName, null);
        }

        private async Task CreateTeamWithName(string teamName)
        {
            if (string.IsNullOrEmpty(battleId))
            {
                LogMessage("ID da batalha não especificado");
                return;
            }

            var request = new CreateTeamRequest
            {
                RequestType = "CreateTeam",
                BattleId = battleId,
                TeamName = teamName
            };

            await SendMessage(request);
        }

        private async Task JoinTeam(string playerName, string teamId)
        {
            if (string.IsNullOrEmpty(battleId))
            {
                LogMessage("ID da batalha não especificado");
                return;
            }

            if (classDropdown == null)
            {
                LogMessage("Dropdown de classes não encontrado");
                return;
            }

            var request = new JoinBattleRequest
            {
                RequestType = "JoinBattle",
                BattleId = battleId,
                PlayerName = playerName,
                Class = classDropdown.options[classDropdown.value].text,
                TeamId = teamId
            };

            await SendMessage(request);
        }

        public async void ToggleTeamReady()
        {
            if (string.IsNullOrEmpty(battleId) || string.IsNullOrEmpty(teamId))
            {
                LogMessage("Você precisa entrar em uma equipe primeiro");
                return;
            }

            isTeamReady = !isTeamReady;
            UpdateReadyStatusUI();

            var request = new SetTeamReadyRequest
            {
                RequestType = "SetTeamReady",
                BattleId = battleId,
                TeamId = teamId,
                IsReady = isTeamReady
            };

            await SendMessage(request);
        }

        public void AddEnemyToList()
        {
            if (enemySelectionDropdown == null)
                return;

            string enemyId = enemySelectionDropdown.options[enemySelectionDropdown.value].text;

            if (!selectedEnemyIds.Contains(enemyId))
            {
                selectedEnemyIds.Add(enemyId);
                UpdateSelectedEnemiesUI();
            }
        }

        public async void StartBattle()
        {
            if (string.IsNullOrEmpty(battleId))
            {
                LogMessage("ID da batalha não especificado");
                return;
            }

            var request = new StartBattleRequest
            {
                RequestType = "StartBattle",
                BattleId = battleId,
                EnemyIds = currentBattleMode == BattleMode.PvE ? selectedEnemyIds : new List<string>(),
                IsPvP = currentBattleMode == BattleMode.PvP
            };

            await SendMessage(request);
        }

        public async void GetBattleState()
        {
            if (string.IsNullOrEmpty(battleId))
            {
                LogMessage("ID da batalha não especificado");
                return;
            }

            var request = new GetBattleStateRequest
            {
                RequestType = "GetBattleState",
                BattleId = battleId
            };

            await SendMessage(request);
        }

        public void ExecuteAttack()
        {
            ExecuteAction(ActionType.Attack).ConfigureAwait(false);
        }

        public void ExecuteSkill()
        {
            ExecuteAction(ActionType.Skill).ConfigureAwait(false);
        }

        public void ExecutePass()
        {
            ExecuteAction(ActionType.Pass).ConfigureAwait(false);
        }

        private async Task ExecuteAction(ActionType actionType)
        {
            if (string.IsNullOrEmpty(battleId) || string.IsNullOrEmpty(playerId) || currentBattle == null)
            {
                LogMessage("Dados de batalha incompletos");
                return;
            }

            // Verificar se é o turno do jogador
            if (currentBattle.CurrentParticipant != playerId)
            {
                LogMessage("Não é o seu turno! Turno atual: " + currentBattle.CurrentParticipant);
                return;
            }

            // Obter alvo e habilidade selecionados
            string targetId = GetSelectedTargetId(actionType);
            string skillId = GetSelectedSkillId(actionType);

            // Criar a ação
            var action = new Action
            {
                Type = actionType,
                TargetId = targetId,
                SkillId = skillId
            };

            LogMessage($"Executando ação: {actionType}, Alvo: {targetId}, Habilidade: {skillId}");

            // Enviar a requisição
            var request = new ExecuteActionRequest
            {
                RequestType = "ExecuteAction",
                BattleId = battleId,
                PlayerId = playerId,
                Action = action
            };

            await SendMessage(request);
        }

        private string GetSelectedTargetId(ActionType actionType)
        {
            if (actionType == ActionType.Pass || targetSelectionDropdown == null || targetSelectionDropdown.options.Count == 0)
                return null;

            string selectedDisplayName = targetSelectionDropdown.options[targetSelectionDropdown.value].text;
            return targetIdMap.TryGetValue(selectedDisplayName, out string targetId) ? targetId : null;
        }

        private string GetSelectedSkillId(ActionType actionType)
        {
            if (actionType != ActionType.Skill || skillSelectionDropdown == null || skillSelectionDropdown.options.Count == 0)
                return null;

            string selectedSkillDisplay = skillSelectionDropdown.options[skillSelectionDropdown.value].text;

            if (skillIdMap.TryGetValue(selectedSkillDisplay, out string skillId))
            {
                return skillId;
            }
            else
            {
                LogMessage($"Erro: ID da habilidade não encontrado para: {selectedSkillDisplay}");
                return null;
            }
        }

        public void ShowPlayerProgress()
        {
            if (playerProgressText != null)
            {
                LogMessage("Mostrando progresso do jogador");
                UpdatePlayerProgressUI();
            }
        }

        #endregion

        #region Atualização da UI

        private void UpdateConnectionStatus(string status)
        {
            if (connectionStatusText != null)
                connectionStatusText.text = $"Status: {status}";
        }

        private void UpdateSelectedEnemiesUI()
        {
            if (selectedEnemiesText == null)
                return;

            selectedEnemiesText.text = "Inimigos selecionados:\n";
            foreach (var enemyId in selectedEnemyIds)
            {
                selectedEnemiesText.text += $"- {enemyId}\n";
            }
        }

        private void UpdateBattleStateUI()
        {
            if (currentBattle == null)
            {
                if (battleStateText != null) battleStateText.text = "N/A";
                if (currentTurnText != null) currentTurnText.text = "N/A";
                return;
            }

            if (!string.IsNullOrEmpty(currentBattle.RoomCode) && roomCodeText != null)
            {
                roomCodeText.text = $"Código da Sala: {currentBattle.RoomCode}";
            }

            Team playerTeam = FindPlayerTeam();

            if (battleStateText != null)
                battleStateText.text = $"Estado: {currentBattle.State}";

            if (currentTurnText != null)
                currentTurnText.text = $"Turno: {currentBattle.CurrentTurn} | Jogador: {currentBattle.CurrentParticipant}";

            if (battleStateText != null)
            {
                string teamsText = FormatTeamsText(playerTeam);
                string enemiesText = FormatEnemiesText();
                battleStateText.text = $"{battleStateText.text}\n\n{teamsText}{enemiesText}";
            }
        }

        private string FormatTeamsText(Team playerTeam)
        {
            string result = "";

            if (currentBattle.IsPvP && playerTeam != null)
            {
                // Modo PvP - mostrar seu time e times inimigos
                result += "<color=green><b>SEU TIME</b></color>\n";
                result += FormatTeamPlayers(playerTeam);
                result += "\n<color=red><b>TIMES INIMIGOS</b></color>\n";

                foreach (var team in currentBattle.Teams)
                {
                    if (team.Id == playerTeam.Id) continue;
                    result += FormatTeamPlayers(team);
                }
            }
            else
            {
                // Modo PvE ou sem time - mostrar todos os times
                foreach (var team in currentBattle.Teams)
                {
                    bool isPlayerTeam = team.Id == playerTeam?.Id;
                    string teamHeader = isPlayerTeam ?
                        $"<color=green>{team.Name} (SEU TIME)</color>" : team.Name;
                    result += $"Equipe {teamHeader}:\n";

                    foreach (var player in team.Players)
                    {
                        string status = player.IsAlive ? "Vivo" : "Morto";
                        result += $"  - {player.Name} ({player.Class}) - Nível {player.Level} - {player.Health}/{player.MaxHealth} HP - {status}\n";
                    }
                    result += "\n";
                }
            }

            return result;
        }

        private string FormatTeamPlayers(Team team)
        {
            string result = $"Equipe {team.Name}:\n";
            foreach (var player in team.Players)
            {
                string status = player.IsAlive ? "Vivo" : "Morto";
                result += $"  - {player.Name} ({player.Class}) - Nível {player.Level} - {player.Health}/{player.MaxHealth} HP - {status}\n";
            }
            result += "\n";
            return result;
        }

        private string FormatEnemiesText()
        {
            if (currentBattle.IsPvP || currentBattle.Enemies.Count == 0)
                return "";

            string result = "\n<color=red>Inimigos:</color>\n";
            foreach (var enemy in currentBattle.Enemies)
            {
                string status = enemy.IsAlive ? "Vivo" : "Morto";
                result += $"  - {enemy.Name} - {enemy.Health}/{enemy.MaxHealth} HP - {status}\n";
            }
            return result;
        }

        private void UpdateTeamDropdown()
        {
            if (currentBattle == null || teamSelectionDropdown == null)
                return;

            teamSelectionDropdown.ClearOptions();
            teamIdNameMap.Clear();

            List<string> teamOptions = new List<string>();
            Team currentPlayerTeam = FindPlayerTeam();

            // Adicionar equipes existentes
            foreach (var team in currentBattle.Teams)
            {
                string readyStatus = currentBattle.TeamReadyStatus.TryGetValue(team.Id, out bool isReady)
                    ? (isReady ? "✓" : "✗")
                    : "✗";

                string displayName = $"{team.Name} ({team.Players.Count}/4) {readyStatus}";
                teamOptions.Add(displayName);
                teamIdNameMap[displayName] = team.Id;
            }

            // Adicionar opção de criar nova equipe se houver espaço
            if (currentBattle.Teams.Count < 4)
            {
                teamOptions.Add("+ Criar Nova Equipe");
            }

            teamSelectionDropdown.ClearOptions();
            teamSelectionDropdown.AddOptions(teamOptions);

            // Atualizar estado de prontidão
            if (currentPlayerTeam != null && !string.IsNullOrEmpty(teamId))
            {
                if (currentBattle.TeamReadyStatus.TryGetValue(currentPlayerTeam.Id, out bool teamReady))
                {
                    isTeamReady = teamReady;
                    UpdateReadyStatusUI();
                }
            }
        }

        private void UpdateTargetsDropdown()
        {
            if (currentBattle == null || targetSelectionDropdown == null)
                return;

            targetSelectionDropdown.ClearOptions();
            targetIdMap.Clear();

            List<string> targetDisplayNames = new List<string>();
            Team playerTeam = FindPlayerTeam();

            if (currentBattle.IsPvP)
            {
                AddPvPTargets(playerTeam, targetDisplayNames);
            }
            else
            {
                AddPvETargets(targetDisplayNames);
            }

            targetSelectionDropdown.AddOptions(targetDisplayNames);
        }

        private void AddPvPTargets(Team playerTeam, List<string> targetDisplayNames)
        {
            if (playerTeam == null) return;

            foreach (var team in currentBattle.Teams)
            {
                if (team.Id == playerTeam.Id) continue;

                foreach (var player in team.Players)
                {
                    if (player.IsAlive)
                    {
                        string displayName = $"{player.Name} ({player.Class}) - {player.Health}/{player.MaxHealth} HP [Inimigo]";
                        targetDisplayNames.Add(displayName);
                        targetIdMap[displayName] = player.Id;
                    }
                }
            }
        }

        private void AddPvETargets(List<string> targetDisplayNames)
        {
            // Adicionar jogadores (aliados e outros)
            foreach (var team in currentBattle.Teams)
            {
                foreach (var player in team.Players)
                {
                    if (player.IsAlive)
                    {
                        bool isAlly = IsAlly(player.Id);
                        string allyStatus = isAlly ? "[Aliado]" : "[Neutro]";
                        string displayName = $"{player.Name} ({player.Class}) - {player.Health}/{player.MaxHealth} HP {allyStatus}";
                        targetDisplayNames.Add(displayName);
                        targetIdMap[displayName] = player.Id;
                    }
                }
            }

            // Adicionar inimigos
            foreach (var enemy in currentBattle.Enemies)
            {
                if (enemy.IsAlive)
                {
                    string displayName = $"{enemy.Name} - {enemy.Health}/{enemy.MaxHealth} HP [Inimigo]";
                    targetDisplayNames.Add(displayName);
                    targetIdMap[displayName] = enemy.Id;
                }
            }
        }

        private void UpdateSkillsDropdown()
        {
            if (currentBattle == null || skillSelectionDropdown == null)
                return;

            skillSelectionDropdown.ClearOptions();
            skillIdMap.Clear();
            List<string> skillDisplayNames = new List<string>();

            Player currentPlayer = GetCurrentPlayer();
            if (currentPlayer != null && currentPlayer.Skills != null)
            {
                foreach (var skill in currentPlayer.Skills)
                {
                    string cooldownInfo = skill.CurrentCooldown > 0 ? $" (CD: {skill.CurrentCooldown})" : "";
                    string manaInfo = skill.ManaCost > 0 ? $" - {skill.ManaCost} MP" : "";
                    bool canUse = skill.CurrentCooldown == 0 && currentPlayer.Mana >= skill.ManaCost;
                    string usabilityInfo = canUse ? "" : " [Indisponível]";

                    string displayName = $"{skill.Name}{manaInfo}{cooldownInfo}{usabilityInfo}";
                    skillDisplayNames.Add(displayName);
                    skillIdMap[displayName] = skill.Id;
                }
            }

            skillSelectionDropdown.AddOptions(skillDisplayNames);
        }

        private void UpdatePlayerProgressUI()
        {
            if (playerProgressText == null || classDropdown == null)
                return;

            string currentClass = classDropdown.options[classDropdown.value].text;

            if (localClassProgress.TryGetValue(currentClass, out var progress))
            {
                playerProgressText.text = $"Classe: {progress.Class}\n" +
                                         $"Nível: {progress.Level}\n" +
                                         $"Experiência: {progress.Experience}/{progress.ExperienceToNextLevel}\n" +
                                         $"Vitórias: {progress.BattlesWon}\n" +
                                         $"Derrotas: {progress.BattlesLost}";
            }
            else
            {
                playerProgressText.text = "Sem progresso para esta classe";
            }
        }

        private void UpdateReadyStatusUI()
        {
            if (readyStatusText != null)
            {
                readyStatusText.text = isTeamReady ? "Status: Pronto ✓" : "Status: Não Pronto ✗";
                readyStatusText.color = isTeamReady ? Color.green : Color.red;
            }

            if (toggleReadyButton != null)
            {
                var buttonText = toggleReadyButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = isTeamReady ? "Cancelar Pronto" : "Marcar como Pronto";
                }
            }
        }

        #endregion

        #region Métodos Auxiliares

        private Team FindPlayerTeam()
        {
            if (string.IsNullOrEmpty(playerId) || currentBattle == null)
                return null;

            foreach (var team in currentBattle.Teams)
            {
                if (team.Players.Any(p => p.Id == playerId))
                {
                    return team;
                }
            }

            return null;
        }

        private Player GetCurrentPlayer()
        {
            if (string.IsNullOrEmpty(playerId) || currentBattle == null)
                return null;

            foreach (var team in currentBattle.Teams)
            {
                var player = team.Players.FirstOrDefault(p => p.Id == playerId);
                if (player != null)
                    return player;
            }

            return null;
        }

        private bool IsAlly(string characterId)
        {
            if (currentBattle == null || string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(characterId))
                return false;

            // Se é o próprio jogador
            if (characterId == playerId)
                return true;

            // Encontrar o time do jogador
            Team playerTeam = FindPlayerTeam();
            if (playerTeam == null)
                return false;

            // Se estamos em modo PvP
            if (currentBattle.IsPvP)
            {
                // Verificar se o personagem está no mesmo time
                return playerTeam.Players.Any(p => p.Id == characterId);
            }
            else
            {
                // Em PvE, todos os jogadores são aliados
                foreach (var team in currentBattle.Teams)
                {
                    if (team.Players.Any(p => p.Id == characterId))
                        return true;
                }

                // Inimigos nunca são aliados
                return false;
            }
        }

        private void LogMessage(string message)
        {
            Debug.Log($"[BattleTestClient] {message}");

            if (logText != null)
            {
                // Adicionar à UI com timestamp
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                logText.text = $"[{timestamp}] {message}\n" + logText.text;

                // Limitar o tamanho do log para evitar problemas de performance
                if (logText.text.Length > 5000)
                {
                    logText.text = logText.text.Substring(0, 5000);
                }
            }
        }

        #endregion

        #region Métodos Públicos

        public bool IsConnected => isConnected;

        public void SetConnectionParameters(string ip, int port)
        {
            serverIp = ip;
            serverPort = port;
        }

        #endregion
    }
}