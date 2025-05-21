using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSystem
{

    public class BattleTestClient : MonoBehaviour
    {
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

        private bool isTeamReady = false;

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
        [SerializeField] private GameObject connectPanel;      // Painel de conexão
        [SerializeField] private GameObject lobbyPanel;        // Painel de lobby (criar/entrar em batalha)
        [SerializeField] private GameObject preparationPanel;  // Painel de preparação (equipes/inimigos)
        [SerializeField] private GameObject battlePanel;       // Painel de batalha (ações/alvos)
        [SerializeField] private GameObject endBattlePanel;    // Painel de fim de batalha (resultados)

        // Adicione essas variáveis na classe
        [Header("UI - Modo de Batalha")]
        [SerializeField] private TMP_Dropdown battleModeDropdown;
        private BattleMode currentBattleMode = BattleMode.PvE;

        [Header("UI - Seleção de Equipe")]
        [SerializeField] private TMP_Dropdown teamSelectionDropdown;
        [SerializeField] private TMP_InputField createTeamInputField;
        [SerializeField] private Button createTeamButton;
        [SerializeField] private Button refreshTeamsButton;
        [SerializeField] private Button joinTeamButton;
        private Dictionary<string, string> teamIdNameMap = new Dictionary<string, string>();

        private enum ClientState
        {
            Disconnected,
            Connected,
            InLobby,
            PreparingBattle,
            InBattle,
            BattleEnded
        }

        // Adicione esta variável à classe
        private ClientState currentState = ClientState.Disconnected;



        private enum BattleMode
        {
            PvE,  // Jogadores contra inimigos
            PvP   // Jogadores contra jogadores
        }



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

        private void Start()
        {
            InitializeUI();

            // Inicializar estado inicial
            ChangeState(ClientState.Disconnected);

            if (autoConnectOnStart)
            {
                Connect();
            }

            // Populando o dropdown de classes de personagens
            classDropdown.ClearOptions();
            classDropdown.AddOptions(new List<string> { "warrior", "mage", "healer" });

            // Populando o dropdown de inimigos
            enemySelectionDropdown.ClearOptions();
            enemySelectionDropdown.AddOptions(new List<string> {
        "enemy_goblin", "enemy_orc", "enemy_necromancer", "enemy_dragon"
    });

            // Populando o dropdown de modos de batalha
            battleModeDropdown.ClearOptions();
            battleModeDropdown.AddOptions(new List<string> { "PvE (Jogadores vs Inimigos)", "PvP (Jogadores vs Jogadores)" });
            battleModeDropdown.onValueChanged.AddListener(OnBattleModeChanged);
        }


        private void ChangeState(ClientState newState)
        {
            LogMessage($"Mudando estado: {currentState} -> {newState}");
            currentState = newState;

            // Atualizar UI com base no novo estado
            UpdateUI();

            // Atualizar interatividade dos botões conforme o estado
            UpdateButtonsBasedOnState(newState);
        }


        private void UpdateButtonsBasedOnState(ClientState state)
        {
            // Primeiro desabilita tudo
            connectButton.interactable = false;
            disconnectButton.interactable = false;
            createBattleButton.interactable = false;
            joinBattleButton.interactable = false;
            startBattleButton.interactable = false;
            addEnemyButton.interactable = false;
            attackButton.interactable = false;
            skillButton.interactable = false;
            passButton.interactable = false;
            refreshStateButton.interactable = false;
            createTeamButton.interactable = false;
            refreshTeamsButton.interactable = false;
            returnToLobbyButton.interactable = false;

            // Habilita botões específicos com base no estado
            switch (state)
            {
                case ClientState.Disconnected:
                    connectButton.interactable = true;
                    break;

                case ClientState.Connected:
                case ClientState.InLobby:
                    disconnectButton.interactable = true;
                    createBattleButton.interactable = true;
                    joinBattleButton.interactable = true;
                    break;

                case ClientState.PreparingBattle:
                    disconnectButton.interactable = true;
                    startBattleButton.interactable = true;
                    createTeamButton.interactable = true;
                    refreshTeamsButton.interactable = true;
                    joinTeamButton.interactable = true;

                    // No modo PvE, os botões de adicionar inimigos devem estar habilitados
                    if (currentBattleMode == BattleMode.PvE)
                    {
                        addEnemyButton.interactable = true;
                    }
                    break;

                case ClientState.InBattle:
                    disconnectButton.interactable = true;
                    attackButton.interactable = true;
                    skillButton.interactable = true;
                    passButton.interactable = true;
                    refreshStateButton.interactable = true;
                    break;

                case ClientState.BattleEnded:
                    disconnectButton.interactable = true;
                    returnToLobbyButton.interactable = true;
                    break;
            }
        }


        private void UpdateUI()
        {
            // Esconder todos os painéis
            connectPanel.SetActive(false);
            lobbyPanel.SetActive(false);
            preparationPanel.SetActive(false);
            battlePanel.SetActive(false);
            endBattlePanel.SetActive(false);

            // Mostrar apenas o painel relevante para o estado atual
            switch (currentState)
            {
                case ClientState.Disconnected:
                    connectPanel.SetActive(true);
                    break;

                case ClientState.Connected:
                case ClientState.InLobby:
                    lobbyPanel.SetActive(true);
                    break;

                case ClientState.PreparingBattle:
                    preparationPanel.SetActive(true);
                    break;

                case ClientState.InBattle:
                    battlePanel.SetActive(true);
                    break;

                case ClientState.BattleEnded:
                    endBattlePanel.SetActive(true);
                    break;
            }
        }



        private void OnBattleModeChanged(int index)
        {
            currentBattleMode = (BattleMode)index;
            LogMessage($"Modo de batalha alterado para: {currentBattleMode}");

            // Atualizar visibilidade de controles baseado no modo
            bool isPvE = currentBattleMode == BattleMode.PvE;
            enemySelectionDropdown.gameObject.SetActive(isPvE);
            addEnemyButton.gameObject.SetActive(isPvE);

            // Atualizar interatividade dos botões para o estado atual
            UpdateButtonsBasedOnState(currentState);
        }


        [Header("UI - Fim de Batalha")]
        [SerializeField] private TextMeshProUGUI battleResultText;
        [SerializeField] private Button returnToLobbyButton;
        private void InitializeUI()
        {
            connectButton.onClick.AddListener(Connect);
            disconnectButton.onClick.AddListener(Disconnect);
            createBattleButton.onClick.AddListener(CreateBattle);
            joinBattleButton.onClick.AddListener(JoinBattle);
            startBattleButton.onClick.AddListener(StartBattle);
            addEnemyButton.onClick.AddListener(AddEnemyToList);
            attackButton.onClick.AddListener(ExecuteAttack);
            skillButton.onClick.AddListener(ExecuteSkill);
            passButton.onClick.AddListener(ExecutePass);
            refreshStateButton.onClick.AddListener(GetBattleState);
            createTeamButton.onClick.AddListener(CreateTeam);
            refreshTeamsButton.onClick.AddListener(RefreshTeams);
            returnToLobbyButton.onClick.AddListener(ReturnToLobby);
            joinTeamButton.onClick.AddListener(JoinSelectedTeam);
            toggleReadyButton.onClick.AddListener(ToggleTeamReady);

            // Desabilitar botões que requerem conexão
            UpdateButtonsBasedOnState(ClientState.Disconnected);

            // Estado inicial da UI
            UpdateConnectionStatus("Desconectado");
            LogMessage("Cliente de teste iniciado.");


        }

        #region Conexão com o Servidor

        public async void Connect()
        {
            try
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(serverIp, serverPort);
                stream = tcpClient.GetStream();
                isConnected = true;

                UpdateConnectionStatus("Conectado");
                LogMessage("Conectado com sucesso ao servidor.");

                // Mudar para o estado Connected - ADICIONAR ESTA LINHA
                ChangeState(ClientState.Connected);

                // Inicia a leitura contínua de mensagens
                _ = ListenForMessages();
            }
            catch (Exception ex)
            {
                UpdateConnectionStatus($"Erro: {ex.Message}");
                LogMessage($"Falha ao conectar: {ex.Message}");
                ChangeState(ClientState.Disconnected);
            }
        }

        public void Disconnect()
        {
            if (!isConnected) return;

            isConnected = false;
            stream?.Close();
            tcpClient?.Close();

            UpdateConnectionStatus("Desconectado");
            LogMessage("Desconectado do servidor.");

            // Limpar dados da batalha
            battleId = null;
            playerId = null;
            teamId = null;
            selectedEnemyIds.Clear();
            currentBattle = null;

            // Atualizar UI
            battleIdInputField.text = "";
            playerIdText.text = "Player ID: N/A";
            battleStateText.text = "N/A";
            currentTurnText.text = "N/A";
            selectedEnemiesText.text = "";

            // Mudar para o estado desconectado - ADICIONAR ESTA LINHA
            ChangeState(ClientState.Disconnected);
        }
        private async Task ListenForMessages()
        {
            while (isConnected)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
                    if (bytesRead <= 0)
                    {
                        Disconnect();
                        break;
                    }

                    string message = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
                    ProcessMessage(message);
                }
                catch (Exception ex)
                {
                    if (isConnected)
                    {
                        LogMessage($"Erro ao receber mensagem: {ex.Message}");
                        Disconnect();
                    }
                    break;
                }
            }
        }

        private async Task SendMessage(object request)
        {
            if (!isConnected) return;

            try
            {
                string json = JsonConvert.SerializeObject(request);
                LogMessage($"Enviando: {json}");

                byte[] data = Encoding.UTF8.GetBytes(json);
                await stream.WriteAsync(data, 0, data.Length);
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

                // Tentar desserializar como resposta base
                var baseResponse = JsonConvert.DeserializeObject<BaseResponse>(message);

                if (!baseResponse.Success)
                {
                    LogMessage($"ERRO do servidor: {baseResponse.Message}");
                    return;
                }

                // Verificar tipo específico de resposta
                if (message.Contains("\"battleId\"") && !message.Contains("\"battle\""))
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

        private void HandleCreateTeamResponse(CreateTeamResponse response)
        {
            LogMessage($"Equipa criada com sucesso: {response.TeamName} (ID: {response.TeamId})");
            // Atualizar estado da batalha para ver a nova equipe
            GetBattleState();
        }





        private void HandleCreateBattleResponse(CreateBattleResponse response)
        {
            battleId = response.BattleId;
            battleIdInputField.text = response.RoomCode; // Mostrar o código de sala em vez do ID completo
            roomCodeText.text = $"Código da Sala: {response.RoomCode}";

            LogMessage($"Batalha criada com código: {response.RoomCode}");

            // Após criar uma batalha, entramos no modo de preparação
            ChangeState(ClientState.PreparingBattle);
        }

        private void HandleJoinBattleResponse(JoinBattleResponse response)
        {
            playerId = response.PlayerId;
            teamId = response.TeamId;

            playerIdText.text = $"Player ID: {playerId} | Equipa: {response.TeamName}";
            LogMessage($"entrou na batalha (ID: {playerId})");
            LogMessage($"Equipa: {response.TeamName}");

            // Atualizar UI para mostrar informações do jogador
            UpdatePlayerUI();

            // Transicionar para o estado de preparação de batalha
            ChangeState(ClientState.PreparingBattle);

            // Solicitar o estado atual da batalha para ver outros jogadores
            GetBattleState();
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

                result += playersWon ? "Sua equipa venceu!" : "Sua equipa perdeu!";
            }

            LogMessage(result);
            battleResultText.text = result;

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
            else
            {
                LogMessage("A ação não produziu resultados (possivelmente uma ação de Passar Turno)");
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

            UpdateBattleStateUI();

            // Atualizar dropdown de equipes
            UpdateTeamDropdown();

            // Atualizar dropdown de alvos
            UpdateTargetsDropdown();

            // Atualizar dropdown de habilidades se temos um jogador
            if (!string.IsNullOrEmpty(playerId))
            {
                UpdateSkillsDropdown();
            }

            // Se estamos em batalha, ajustar interatividade dos botões de ação
            if (currentState == ClientState.InBattle && currentBattle != null)
            {
                bool isPlayerTurn = currentBattle.CurrentParticipant == playerId;
                attackButton.interactable = isPlayerTurn;
                skillButton.interactable = isPlayerTurn;
                passButton.interactable = isPlayerTurn;
            }
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
                foreach (var player in playerTeam.Players)
                {
                    if (player.Id == characterId)
                        return true;
                }

                // Em PvP, jogadores de outros times não são aliados
                return false;
            }
            else
            {
                // Em PvE, todos os jogadores são aliados
                foreach (var team in currentBattle.Teams)
                {
                    foreach (var player in team.Players)
                    {
                        if (player.Id == characterId)
                            return true;
                    }
                }

                // Verificar se é um inimigo (em PvE, inimigos nunca são aliados)
                foreach (var enemy in currentBattle.Enemies)
                {
                    if (enemy.Id == characterId)
                        return false;
                }
            }

            return false;
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

        public async void RefreshTeams()
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

            // Primeiro, verificamos se o que foi digitado é um código de sala
            string inputCode = battleIdInputField.text.Trim();
            LogMessage($"Tentando entrar com código: {inputCode}");

            // Se o código tiver o tamanho de um código de sala (5 dígitos)
            bool isRoomCode = inputCode.Length == 5 && inputCode.All(char.IsDigit);

            if (isRoomCode)
            {
                LogMessage($"Usando como código de sala: {inputCode}");
                // Solicita informações da batalha usando o código da sala
                var stateRequest = new GetBattleStateRequest
                {
                    RequestType = "GetBattleState",
                    RoomCode = inputCode
                };

                await SendMessage(stateRequest);
            }
            else
            {
                LogMessage($"Usando como ID de batalha: {inputCode}");
                // Usa o valor como ID da batalha diretamente
                battleId = inputCode;
                var stateRequest = new GetBattleStateRequest
                {
                    RequestType = "GetBattleState",
                    BattleId = battleId
                };

                await SendMessage(stateRequest);
            }

            // Não mudamos o estado aqui - ele será atualizado quando recebermos a resposta
            LogMessage($"Verificando informações da sala...");
        }

        private async Task CreateTeamAndJoin(string playerName)
        {
            // Primeiro cria uma equipe
            string teamName = !string.IsNullOrEmpty(createTeamInputField.text)
                ? createTeamInputField.text
                : "Time " + UnityEngine.Random.Range(1, 100);

            await CreateTeamWithName(teamName);

            // Aguardar um pouco para garantir que a equipe foi criada
            await Task.Delay(500);

            // Obter o estado atualizado da batalha
            GetBattleState();

            // Aguardar mais um pouco para garantir que recebemos o estado
            await Task.Delay(500);

            // Agora tentar entrar em qualquer equipe (o servidor escolherá a nova equipe)
            var request = new JoinBattleRequest
            {
                RequestType = "JoinBattle",
                BattleId = battleIdInputField.text,
                PlayerName = playerName,
                Class = classDropdown.options[classDropdown.value].text,
                // Deixar TeamId nulo para que o servidor escolha a última equipe criada
                TeamId = null
            };

            await SendMessage(request);
        }


        public async void JoinSelectedTeam()
        {
            if (string.IsNullOrEmpty(battleId) || teamSelectionDropdown.options.Count == 0)
            {
                LogMessage("Não há equipes disponíveis ou ID da batalha não especificado");
                return;
            }

            string playerName = !string.IsNullOrEmpty(playerNameInputField.text)
                ? playerNameInputField.text
                : $"Player {UnityEngine.Random.Range(1000, 9999)}";

            // Obtém a opção selecionada no dropdown
            string selectedTeam = teamSelectionDropdown.options[teamSelectionDropdown.value].text;
            LogMessage($"Opção selecionada: '{selectedTeam}'");

            // Verifica se é para criar uma nova equipe
            if (selectedTeam == "+ Criar Nova Equipe")
            {
                LogMessage("Criando nova equipe e entrando...");
                await CreateTeamAndJoin(playerName);
                return;
            }

            // Encontra o ID da equipe correspondente à seleção
            string selectedTeamId = null;

            // Procurar a entrada exata primeiro
            if (teamIdNameMap.ContainsKey(selectedTeam))
            {
                selectedTeamId = teamIdNameMap[selectedTeam];
            }
            else
            {
                // Tentar encontrar o ID da equipe nas entradas do dicionário
                foreach (var entry in teamIdNameMap)
                {
                    LogMessage($"Analisando equipe: '{entry.Key}' com ID: {entry.Value}");

                    // Se a entrada do dicionário contém o início do texto selecionado
                    // (ignorando a parte com contagem de jogadores e status)
                    if (selectedTeam.StartsWith(entry.Key.Split('(')[0].Trim()))
                    {
                        selectedTeamId = entry.Value;
                        LogMessage($"Correspondência encontrada para equipe: {entry.Key} -> {entry.Value}");
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(selectedTeamId))
            {
                LogMessage($"Não foi possível identificar o ID da equipe para: {selectedTeam}");
                LogMessage("IDs de equipe disponíveis:");
                foreach (var entry in teamIdNameMap)
                {
                    LogMessage($"- '{entry.Key}': {entry.Value}");
                }
                return;
            }

            // Verifica se o jogador já está nesta equipe
            if (teamId == selectedTeamId && !string.IsNullOrEmpty(playerId))
            {
                LogMessage($"Você já está na equipe com ID: {selectedTeamId}");
                return;
            }

            LogMessage($"Entrando na equipe com ID: {selectedTeamId}");

            // Prepara a requisição para entrar na equipe
            var request = new JoinBattleRequest
            {
                RequestType = "JoinBattle",
                BattleId = battleId,
                PlayerName = playerName,
                Class = classDropdown.options[classDropdown.value].text,
                TeamId = selectedTeamId
            };

            // Envia a requisição
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


        public void AddEnemyToList()
        {
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

        public async void ExecuteAttack()
        {
            await ExecuteAction(ActionType.Attack);
        }

        public async void ExecuteSkill()
        {
            await ExecuteAction(ActionType.Skill);
        }

        public async void ExecutePass()
        {
            await ExecuteAction(ActionType.Pass);
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

            string targetId = null;
            if (actionType != ActionType.Pass && targetSelectionDropdown.options.Count > 0)
            {
                // Obter o nome de exibição selecionado
                string selectedDisplayName = targetSelectionDropdown.options[targetSelectionDropdown.value].text;

                // Obter o ID real do alvo usando o mapeamento
                if (targetIdMap.ContainsKey(selectedDisplayName))
                {
                    targetId = targetIdMap[selectedDisplayName];
                }
            }

            string skillId = null;
            if (actionType == ActionType.Skill && skillSelectionDropdown.options.Count > 0)
            {
                // Obter o nome de exibição selecionado da habilidade
                string selectedSkillDisplay = skillSelectionDropdown.options[skillSelectionDropdown.value].text;

                // Obter o ID real da habilidade usando o mapeamento
                if (skillIdMap.ContainsKey(selectedSkillDisplay))
                {
                    skillId = skillIdMap[selectedSkillDisplay];
                    LogMessage($"Usando habilidade: {selectedSkillDisplay} -> ID: {skillId}");
                }
                else
                {
                    LogMessage($"Erro: ID da habilidade não encontrado para: {selectedSkillDisplay}");
                    LogMessage("Habilidades disponíveis:");
                    foreach (var entry in skillIdMap)
                    {
                        LogMessage($"- '{entry.Key}': {entry.Value}");
                    }
                    return;
                }
            }

            var action = new Action
            {
                Type = actionType,
                TargetId = targetId,
                SkillId = skillId
            };

            LogMessage($"Executando ação: {actionType}, Alvo: {targetId}, Habilidade: {skillId}");

            var request = new ExecuteActionRequest
            {
                RequestType = "ExecuteAction",
                BattleId = battleId,
                PlayerId = playerId,
                Action = action
            };

            await SendMessage(request);
        }

        #endregion

        #region Atualização da UI

        private void UpdateConnectionStatus(string status)
        {
            connectionStatusText.text = $"Status: {status}";
        }

        private void SetButtonsInteractable(bool interactable)
        {
            connectButton.interactable = !interactable;
            disconnectButton.interactable = interactable;
            createBattleButton.interactable = interactable;
            joinBattleButton.interactable = interactable;
            startBattleButton.interactable = interactable;
            addEnemyButton.interactable = interactable;
            attackButton.interactable = interactable;
            skillButton.interactable = interactable;
            passButton.interactable = interactable;
            refreshStateButton.interactable = interactable;
        }

        private void UpdatePlayerUI()
        {
            playerIdText.text = $"Player ID: {playerId}";
        }

        private void UpdateSelectedEnemiesUI()
        {
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
                battleStateText.text = "N/A";
                currentTurnText.text = "N/A";
                return;
            }

            // Exibir o código da sala se disponível
            if (!string.IsNullOrEmpty(currentBattle.RoomCode))
            {
                roomCodeText.text = $"Código da Sala: {currentBattle.RoomCode}";
            }

            // Identificar o time do jogador
            Team playerTeam = FindPlayerTeam();
            string playerTeamId = playerTeam?.Id;

            battleStateText.text = $"Estado: {currentBattle.State}";
            currentTurnText.text = $"Turno: {currentBattle.CurrentTurn} | Jogador: {currentBattle.CurrentParticipant}";

            string teams = "";

            // Em PvP, separar claramente seu time dos times adversários
            if (currentBattle.IsPvP && playerTeam != null)
            {
                // Primeiro mostrar seu time
                teams += "<color=green><b>SEU TIME</b></color>\n";
                teams += $"Equipe {playerTeam.Name}:\n";

                foreach (var player in playerTeam.Players)
                {
                    string youMarker = (player.Id == playerId) ? " [VOCÊ]" : "";
                    string turnMarker = (player.Id == currentBattle.CurrentParticipant) ? " <<< TURNO ATUAL" : "";
                    teams += $"- {player.Name} ({player.Class}): HP {player.Health}/{player.MaxHealth}, MP {player.Mana}/{player.MaxMana}{turnMarker}{youMarker}\n";
                }

                teams += "\n<color=red><b>TIMES INIMIGOS</b></color>\n";

                // Depois mostrar os times adversários
                foreach (var team in currentBattle.Teams)
                {
                    if (team.Id == playerTeam.Id) continue; // Pular seu próprio time

                    teams += $"Equipe {team.Name}:\n";
                    foreach (var player in team.Players)
                    {
                        string turnMarker = (player.Id == currentBattle.CurrentParticipant) ? " <<< TURNO ATUAL" : "";
                        teams += $"- {player.Name} ({player.Class}): HP {player.Health}/{player.MaxHealth}, MP {player.Mana}/{player.MaxMana}{turnMarker}\n";
                    }
                }
            }
            else
            {
                // No modo PvE ou se não conseguir identificar o time, mostrar tudo normalmente
                foreach (var team in currentBattle.Teams)
                {
                    bool isPlayerTeam = (team.Id == playerTeamId);
                    string teamHeader = isPlayerTeam ? "<color=green>Equipe " : "Equipe ";
                    teamHeader += team.Name;
                    teamHeader += isPlayerTeam ? "</color>:" : ":";
                    teams += $"{teamHeader}\n";

                    foreach (var player in team.Players)
                    {
                        string youMarker = (player.Id == playerId) ? " [VOCÊ]" : "";
                        string turnMarker = (player.Id == currentBattle.CurrentParticipant) ? " <<< TURNO ATUAL" : "";
                        teams += $"- {player.Name} ({player.Class}): HP {player.Health}/{player.MaxHealth}, MP {player.Mana}/{player.MaxMana}{turnMarker}{youMarker}\n";
                    }
                }
            }

            // Mostrar inimigos (apenas em modo PvE)
            string enemies = "";
            if (!currentBattle.IsPvP && currentBattle.Enemies.Count > 0)
            {
                enemies = "\n<color=red>Inimigos:</color>\n";
                foreach (var enemy in currentBattle.Enemies)
                {
                    string turnMarker = enemy.Id == currentBattle.CurrentParticipant ? " <<< TURNO ATUAL" : "";
                    enemies += $"- {enemy.Name}: HP {enemy.Health}/{enemy.MaxHealth}{turnMarker}\n";
                }
            }

            battleStateText.text = $"{battleStateText.text}\n\n{teams}{enemies}";
        }


        private void UpdateTeamDropdown()
        {
            if (currentBattle == null) return;

            teamSelectionDropdown.ClearOptions();
            teamIdNameMap.Clear();

            List<string> teamOptions = new List<string>();

            // Verificar se o jogador já está em alguma equipe
            bool playerHasTeam = !string.IsNullOrEmpty(teamId);
            Team currentPlayerTeam = null;

            // Procurar a equipe atual do jogador
            if (playerHasTeam)
            {
                foreach (var team in currentBattle.Teams)
                {
                    if (team.Players.Any(p => p.Id == playerId))
                    {
                        currentPlayerTeam = team;
                        break;
                    }
                }
            }

            // Adicionar todas as equipes ao dropdown
            foreach (var team in currentBattle.Teams)
            {
                // Verificar status de prontidão
                string readyStatus = currentBattle.TeamReadyStatus.TryGetValue(team.Id, out bool isReady) && isReady
                    ? " [PRONTO]"
                    : " [NÃO PRONTO]";

                // Marcar equipe do jogador
                string currentTeamMarker = (team == currentPlayerTeam) ? " ✓" : "";

                // Gerar texto de exibição com informações da equipe
                string displayText = $"{team.Name} ({team.Players.Count}/4 jogadores){currentTeamMarker}{readyStatus}";

                // Adicionar à lista de opções
                teamOptions.Add(displayText);

                // Importante: mapear diretamente o texto de exibição para o ID da equipe
                teamIdNameMap[displayText] = team.Id;

                // Log para debug
                LogMessage($"Mapeando equipe: '{displayText}' -> ID: {team.Id}");
            }

            // Adicionar opção "Criar Nova Equipe"
            if (currentBattle.Teams.Count < 4) // Limite de 4 equipes
            {
                teamOptions.Add("+ Criar Nova Equipe");
            }

            // Adicionar as opções ao dropdown depois de construir a lista completa
            teamSelectionDropdown.ClearOptions();
            teamSelectionDropdown.AddOptions(teamOptions);

            // Atualizar UI de status de prontidão para a equipe atual
            if (playerHasTeam && currentPlayerTeam != null)
            {
                // Atualizar o status de prontidão local baseado no status da equipe
                if (currentBattle.TeamReadyStatus.TryGetValue(currentPlayerTeam.Id, out bool teamReady))
                {
                    isTeamReady = teamReady;
                    UpdateReadyStatusUI();
                }
            }
        }

        private void UpdateTargetsDropdown()
        {
            if (currentBattle == null) return;

            targetSelectionDropdown.ClearOptions();

            // Usaremos um dicionário para mapear os nomes exibidos aos IDs reais
            Dictionary<string, string> targetMap = new Dictionary<string, string>();
            List<string> targetDisplayNames = new List<string>();

            // Encontrar o time do jogador atual
            Team playerTeam = FindPlayerTeam();

            // Verificar se estamos em uma batalha PvP ou PvE
            if (currentBattle.IsPvP)
            {
                // Log para depuração
                LogMessage($"Modo PvP: Montando alvos. Seu ID: {playerId}, Time: {playerTeam?.Name ?? "Desconhecido"}");

                if (playerTeam != null)
                {
                    // Adicionar jogadores de times opositores como possíveis alvos de ataque
                    foreach (var team in currentBattle.Teams)
                    {
                        bool isEnemyTeam = team.Id != playerTeam.Id;
                        string teamRelation = isEnemyTeam ? "INIMIGO" : "ALIADO";

                        LogMessage($"Analisando time: {team.Name} (ID: {team.Id}) - {teamRelation}");

                        foreach (var player in team.Players)
                        {
                            if (!player.IsAlive) continue;

                            if (isEnemyTeam)
                            {
                                // Jogador de time adversário - alvo válido para ataque
                                string displayName = $"{player.Name} [{team.Name}] (Inimigo)";
                                targetMap[displayName] = player.Id;
                                targetDisplayNames.Add(displayName);
                                LogMessage($"Adicionando inimigo: {player.Name} (ID: {player.Id})");
                            }
                            else if (player.Id != playerId) // Mesmo time, mas não é o próprio jogador
                            {
                                // Aliado - alvo válido para skills de cura/suporte
                                string displayName = $"{player.Name} (Aliado)";
                                targetMap[displayName] = player.Id;
                                targetDisplayNames.Add(displayName);
                                LogMessage($"Adicionando aliado: {player.Name} (ID: {player.Id})");
                            }
                        }
                    }
                }
            }
            else // Modo PvE
            {
                // Adicionar outros jogadores da equipe como possíveis alvos (para cura/buffs)
                foreach (var team in currentBattle.Teams)
                {
                    foreach (var player in team.Players)
                    {
                        if (player.IsAlive && player.Id != playerId)
                        {
                            string displayName = $"{player.Name} (Aliado)";
                            targetMap[displayName] = player.Id;
                            targetDisplayNames.Add(displayName);
                        }
                    }
                }

                // Adicionar inimigos como alvos
                foreach (var enemy in currentBattle.Enemies)
                {
                    if (enemy.IsAlive)
                    {
                        string displayName = $"{enemy.Name} (Inimigo)";
                        targetMap[displayName] = enemy.Id;
                        targetDisplayNames.Add(displayName);
                    }
                }
            }

            // Adicionar os nomes formatados ao dropdown
            targetSelectionDropdown.AddOptions(targetDisplayNames);

            // Armazenar o mapeamento
            this.targetIdMap = targetMap;
        }

        // Adicione este método auxiliar para encontrar o time do jogador atual
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

        // Adicione esta variável à classe
        private Dictionary<string, string> targetIdMap = new Dictionary<string, string>();


        private void UpdateReadyStatusUI()
        {
            if (isTeamReady)
            {
                readyStatusText.text = "Status: Pronto ✓";
                readyStatusText.color = Color.green;
                toggleReadyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Cancelar Prontidão";
            }
            else
            {
                readyStatusText.text = "Status: Não Pronto ✗";
                readyStatusText.color = Color.red;
                toggleReadyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Marcar como Pronto";
            }
        }
        private void UpdateSkillsDropdown()
        {
            if (currentBattle == null) return;

            skillSelectionDropdown.ClearOptions();
            List<string> skillDisplayNames = new List<string>();
            Dictionary<string, string> skillMap = new Dictionary<string, string>();

            // Buscar jogador atual
            Player currentPlayer = null;
            foreach (var team in currentBattle.Teams)
            {
                foreach (var player in team.Players)
                {
                    if (player.Id == playerId)
                    {
                        currentPlayer = player;
                        break;
                    }
                }
                if (currentPlayer != null) break;
            }

            if (currentPlayer != null && currentPlayer.Skills != null)
            {
                LogMessage($"Atualizando habilidades para jogador {currentPlayer.Name} (ID: {currentPlayer.Id}), Mana: {currentPlayer.Mana}/{currentPlayer.MaxMana}");

                foreach (var skill in currentPlayer.Skills)
                {
                    string displayName = $"{skill.Name} (Mana: {skill.ManaCost})";
                    skillMap[displayName] = skill.Id;
                    skillDisplayNames.Add(displayName);

                    LogMessage($"Habilidade registrada: {skill.Name} (ID: {skill.Id}, Dano: {skill.Damage}, Cura: {skill.Healing}, Mana: {skill.ManaCost})");
                }
            }
            else
            {
                LogMessage("Nenhuma habilidade encontrada para o jogador atual");
            }

            skillSelectionDropdown.AddOptions(skillDisplayNames);
            this.skillIdMap = skillMap;
        }



        // Adicione esta variável à classe
        private Dictionary<string, string> skillIdMap = new Dictionary<string, string>();

        private void LogMessage(string message)
        {
            Debug.Log($"[BattleTestClient] {message}");

            // Adicionar à UI com timestamp
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            logText.text = $"[{timestamp}] {message}\n" + logText.text;
            
            // Limitar o tamanho do log para evitar problemas de performance
            if (logText.text.Length > 5000)
            {
                logText.text = logText.text.Substring(0, 5000);
            }
        }




        #endregion

        private void OnDestroy()
        {
            Disconnect();
        }
    }
}