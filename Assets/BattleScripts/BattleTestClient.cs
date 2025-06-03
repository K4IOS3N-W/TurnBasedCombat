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
            PvE,
            PvP
        }

        private ClientState currentState = ClientState.Disconnected;
        private BattleMode currentBattleMode = BattleMode.PvE;
        private bool isTeamReady = false;

        private string battleId;
        private string playerId;
        private string teamId;
        private List<string> selectedEnemyIds = new List<string>();
        private Battle currentBattle;

        private TcpClient tcpClient;
        private NetworkStream stream;
        private bool isConnected = false;
        private byte[] receiveBuffer = new byte[4096];

        private Dictionary<string, string> teamIdNameMap = new Dictionary<string, string>();
        private Dictionary<string, string> targetIdMap = new Dictionary<string, string>();
        private Dictionary<string, string> skillIdMap = new Dictionary<string, string>();
        private Dictionary<string, ClassMasteryInfo> localClassProgress = new Dictionary<string, ClassMasteryInfo>();

        private void Start()
        {
            InitializeUI();
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

        private void InitializeUI()
        {
            if (connectButton != null)
                connectButton.onClick.AddListener(Connect);
            if (disconnectButton != null)
                disconnectButton.onClick.AddListener(Disconnect);
            if (createBattleButton != null)
                createBattleButton.onClick.AddListener(CreateBattle);
            if (joinBattleButton != null)
                joinBattleButton.onClick.AddListener(JoinBattle);
            if (startBattleButton != null)
                startBattleButton.onClick.AddListener(StartBattle);
            if (addEnemyButton != null)
                addEnemyButton.onClick.AddListener(AddSelectedEnemy);
            if (attackButton != null)
                attackButton.onClick.AddListener(Attack);
            if (skillButton != null)
                skillButton.onClick.AddListener(UseSkill);
            if (passButton != null)
                passButton.onClick.AddListener(Pass);
            if (refreshStateButton != null)
                refreshStateButton.onClick.AddListener(RefreshBattleState);
            if (createTeamButton != null)
                createTeamButton.onClick.AddListener(CreateTeam);
            if (refreshTeamsButton != null)
                refreshTeamsButton.onClick.AddListener(RefreshTeams);
            if (returnToLobbyButton != null)
                returnToLobbyButton.onClick.AddListener(ReturnToLobby);
            if (joinTeamButton != null)
                joinTeamButton.onClick.AddListener(JoinTeam);
            if (toggleReadyButton != null)
                toggleReadyButton.onClick.AddListener(ToggleReady);
            if (viewProgressButton != null)
                viewProgressButton.onClick.AddListener(ViewProgress);
            if (battleModeDropdown != null)
                battleModeDropdown.onValueChanged.AddListener(OnBattleModeChanged);

            ChangeState(ClientState.Disconnected);
        }

        private void InitializeDropdowns()
        {
            if (classDropdown != null)
            {
                classDropdown.options.Clear();
                classDropdown.options.Add(new TMP_Dropdown.OptionData("Warrior"));
                classDropdown.options.Add(new TMP_Dropdown.OptionData("Mage"));
                classDropdown.options.Add(new TMP_Dropdown.OptionData("Rogue"));
                classDropdown.options.Add(new TMP_Dropdown.OptionData("Cleric"));
                classDropdown.RefreshShownValue();
            }
            
            if (enemySelectionDropdown != null)
            {
                enemySelectionDropdown.options.Clear();
                enemySelectionDropdown.options.Add(new TMP_Dropdown.OptionData("Goblin"));
                enemySelectionDropdown.options.Add(new TMP_Dropdown.OptionData("Orc"));
                enemySelectionDropdown.options.Add(new TMP_Dropdown.OptionData("Skeleton"));
                enemySelectionDropdown.RefreshShownValue();
            }
            
            if (battleModeDropdown != null)
            {
                battleModeDropdown.options.Clear();
                battleModeDropdown.options.Add(new TMP_Dropdown.OptionData("PvE"));
                battleModeDropdown.options.Add(new TMP_Dropdown.OptionData("PvP"));
                battleModeDropdown.RefreshShownValue();
            }
        }

        private void ChangeState(ClientState newState)
        {
            currentState = newState;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (connectPanel != null)
                connectPanel.SetActive(currentState == ClientState.Disconnected);
            if (lobbyPanel != null)
                lobbyPanel.SetActive(currentState == ClientState.Connected || currentState == ClientState.InLobby);
            if (preparationPanel != null)
                preparationPanel.SetActive(currentState == ClientState.PreparingBattle);
            if (battlePanel != null)
                battlePanel.SetActive(currentState == ClientState.InBattle);
            if (endBattlePanel != null)
                endBattlePanel.SetActive(currentState == ClientState.BattleEnded);

            UpdateButtonsBasedOnState(currentState);
        }

        private void UpdateButtonsBasedOnState(ClientState state)
        {
            switch (state)
            {
                case ClientState.Disconnected:
                    if (connectButton != null)
                        connectButton.interactable = true;
                    if (disconnectButton != null)
                        disconnectButton.interactable = false;
                    break;

                case ClientState.Connected:
                case ClientState.InLobby:
                    if (disconnectButton != null)
                        disconnectButton.interactable = true;
                    if (createBattleButton != null)
                        createBattleButton.interactable = true;
                    if (joinBattleButton != null)
                        joinBattleButton.interactable = true;
                    break;

                case ClientState.PreparingBattle:
                    if (disconnectButton != null)
                        disconnectButton.interactable = true;
                    if (startBattleButton != null)
                        startBattleButton.interactable = true;
                    if (createTeamButton != null)
                        createTeamButton.interactable = true;
                    if (refreshTeamsButton != null)
                        refreshTeamsButton.interactable = true;
                    if (joinTeamButton != null)
                        joinTeamButton.interactable = true;
                    if (toggleReadyButton != null)
                        toggleReadyButton.interactable = true;
                    if (currentBattleMode == BattleMode.PvE && addEnemyButton != null)
                        addEnemyButton.interactable = true;
                    break;

                case ClientState.InBattle:
                    if (disconnectButton != null)
                        disconnectButton.interactable = true;
                    if (refreshStateButton != null)
                        refreshStateButton.interactable = true;
                    if (attackButton != null)
                        attackButton.interactable = true;
                    if (skillButton != null)
                        skillButton.interactable = true;
                    if (passButton != null)
                        passButton.interactable = true;
                    break;

                case ClientState.BattleEnded:
                    if (disconnectButton != null)
                        disconnectButton.interactable = true;
                    if (returnToLobbyButton != null)
                        returnToLobbyButton.interactable = true;
                    break;
            }
        }

        private void OnBattleModeChanged(int index)
        {
            currentBattleMode = (BattleMode)index;
            
            if (enemySelectionDropdown != null)
                enemySelectionDropdown.gameObject.SetActive(currentBattleMode == BattleMode.PvE);
            if (addEnemyButton != null)
                addEnemyButton.gameObject.SetActive(currentBattleMode == BattleMode.PvE);
        }

        public async void Connect()
        {
            if (isConnected)
            {
                AddLog("Já conectado ao servidor");
                return;
            }

            try
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(serverIp, serverPort);
                stream = tcpClient.GetStream();
                isConnected = true;

                if (connectionStatusText != null)
                    connectionStatusText.text = "Status: Conectado";

                ChangeState(ClientState.Connected);
                AddLog("Conectado ao servidor com sucesso");

                _ = ListenForMessages();
            }
            catch (Exception ex)
            {
                AddLog($"Erro ao conectar: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            if (!isConnected)
                return;

            try
            {
                isConnected = false;
                stream?.Close();
                tcpClient?.Close();

                if (connectionStatusText != null)
                    connectionStatusText.text = "Status: Desconectado";

                ChangeState(ClientState.Disconnected);
                AddLog("Desconectado do servidor");
            }
            catch (Exception ex)
            {
                AddLog($"Erro ao desconectar: {ex.Message}");
            }
        }

        private async Task ListenForMessages()
        {
            try
            {
                while (isConnected && tcpClient != null && tcpClient.Connected)
                {
                    int bytesRead = await stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
                    if (bytesRead <= 0)
                        break;

                    string message = Encoding.UTF8.GetString(receiveBuffer, 0, bytesRead);
                    ProcessMessage(message);
                }
            }
            catch (Exception ex)
            {
                AddLog($"Erro na escuta: {ex.Message}");
            }

            if (isConnected)
            {
                Disconnect();
            }
        }

        private async Task SendMessage(object request)
        {
            if (!isConnected || stream == null)
            {
                AddLog("Não conectado ao servidor");
                return;
            }

            try
            {
                string json = JsonConvert.SerializeObject(request);
                byte[] data = Encoding.UTF8.GetBytes(json);
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();
            }
            catch (Exception ex)
            {
                AddLog($"Erro ao enviar mensagem: {ex.Message}");
            }
        }

        private void ProcessMessage(string message)
        {
            try
            {
                // Processar resposta do servidor
                AddLog($"Recebido: {message}");
            }
            catch (Exception ex)
            {
                AddLog($"Erro ao processar mensagem: {ex.Message}");
            }
        }

        private void HandleCreateTeamResponse(CreateTeamResponse response)
        {
            if (response.Success)
            {
                teamId = response.TeamId;
                AddLog($"Time criado: {response.TeamName}");
            }
            else
            {
                AddLog($"Erro ao criar time: {response.Message}");
            }
        }

        private void CreateBattle()
        {
            var request = new CreateBattleRequest
            {
                RequestType = "CreateBattle"
            };
            _ = SendMessage(request);
        }

        private void JoinBattle()
        {
            if (battleIdInputField == null || playerNameInputField == null || classDropdown == null)
                return;

            var request = new JoinBattleRequest
            {
                RequestType = "JoinBattle",
                BattleId = battleIdInputField.text,
                PlayerName = playerNameInputField.text,
                Class = classDropdown.options[classDropdown.value].text,
                TeamId = teamId
            };
            _ = SendMessage(request);
        }

        private void StartBattle()
        {
            var request = new StartBattleRequest
            {
                RequestType = "StartBattle",
                BattleId = battleId,
                EnemyIds = selectedEnemyIds,
                IsPvP = currentBattleMode == BattleMode.PvP
            };
            _ = SendMessage(request);
        }

        private void AddSelectedEnemy()
        {
            if (enemySelectionDropdown == null)
                return;

            string enemyName = enemySelectionDropdown.options[enemySelectionDropdown.value].text;
            selectedEnemyIds.Add(enemyName);
            UpdateSelectedEnemiesDisplay();
        }

        private void Attack()
        {
            if (targetSelectionDropdown == null)
                return;

            string targetId = GetSelectedTargetId();
            var action = new Action
            {
                Type = ActionType.Attack,
                TargetId = targetId
            };

            var request = new ExecuteActionRequest
            {
                RequestType = "ExecuteAction",
                BattleId = battleId,
                PlayerId = playerId,
                Action = action
            };
            _ = SendMessage(request);
        }

        private void UseSkill()
        {
            if (targetSelectionDropdown == null || skillSelectionDropdown == null)
                return;

            string targetId = GetSelectedTargetId();
            string skillId = GetSelectedSkillId();
            
            var action = new Action
            {
                Type = ActionType.Skill,
                TargetId = targetId,
                SkillId = skillId
            };

            var request = new ExecuteActionRequest
            {
                RequestType = "ExecuteAction",
                BattleId = battleId,
                PlayerId = playerId,
                Action = action
            };
            _ = SendMessage(request);
        }

        private void Pass()
        {
            var action = new Action
            {
                Type = ActionType.Pass
            };

            var request = new ExecuteActionRequest
            {
                RequestType = "ExecuteAction",
                BattleId = battleId,
                PlayerId = playerId,
                Action = action
            };
            _ = SendMessage(request);
        }

        private void RefreshBattleState()
        {
            var request = new GetBattleStateRequest
            {
                RequestType = "GetBattleState",
                BattleId = battleId
            };
            _ = SendMessage(request);
        }

        private void CreateTeam()
        {
            if (createTeamInputField == null)
                return;

            var request = new CreateTeamRequest
            {
                RequestType = "CreateTeam",
                BattleId = battleId,
                TeamName = createTeamInputField.text
            };
            _ = SendMessage(request);
        }

        private void RefreshTeams()
        {
            // Implementar refresh de times
            AddLog("Atualizando lista de times...");
        }

        private void JoinTeam()
        {
            // Implementar join team
            AddLog("Entrando no time...");
        }

        private void ToggleReady()
        {
            isTeamReady = !isTeamReady;
            
            var request = new SetTeamReadyRequest
            {
                RequestType = "SetTeamReady",
                BattleId = battleId,
                TeamId = teamId,
                IsReady = isTeamReady
            };
            _ = SendMessage(request);

            UpdateReadyStatusDisplay();
        }

        private void ViewProgress()
        {
            AddLog("Visualizando progresso do jogador...");
        }

        private void ReturnToLobby()
        {
            ChangeState(ClientState.Connected);
            AddLog("Retornando ao lobby...");
        }

        private void UpdateSelectedEnemiesDisplay()
        {
            if (selectedEnemiesText != null)
            {
                selectedEnemiesText.text = "Inimigos: " + string.Join(", ", selectedEnemyIds);
            }
        }

        private void UpdateReadyStatusDisplay()
        {
            if (readyStatusText != null)
            {
                readyStatusText.text = isTeamReady ? "Pronto" : "Não Pronto";
            }
        }

        private string GetSelectedTargetId()
        {
            if (targetSelectionDropdown == null || targetSelectionDropdown.value >= targetSelectionDropdown.options.Count)
                return "";

            string targetName = targetSelectionDropdown.options[targetSelectionDropdown.value].text;
            return targetIdMap.ContainsKey(targetName) ? targetIdMap[targetName] : targetName;
        }

        private string GetSelectedSkillId()
        {
            if (skillSelectionDropdown == null || skillSelectionDropdown.value >= skillSelectionDropdown.options.Count)
                return "";

            string skillName = skillSelectionDropdown.options[skillSelectionDropdown.value].text;
            return skillIdMap.ContainsKey(skillName) ? skillIdMap[skillName] : skillName;
        }

        private void AddLog(string message)
        {
            Debug.Log($"[BattleTestClient] {message}");
            
            if (logText != null)
            {
                logText.text += $"\n{DateTime.Now:HH:mm:ss} - {message}";
            }
        }

        public bool IsConnected => isConnected;
    }

    [Serializable]
    public class ClassMasteryInfo
    {
        public int Level { get; set; }
        public int Experience { get; set; }
        public int ExperienceToNextLevel { get; set; }
    }
}