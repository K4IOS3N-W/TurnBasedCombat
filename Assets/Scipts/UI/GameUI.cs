using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSystem.Client;
using BattleSystem.Server;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BattleSystem.UI
{
    public class GameUI : MonoBehaviour
    {
        [Header("Connection Panel")]
        public GameObject connectionPanel;
        public Button connectButton;
        public TMP_InputField serverIPInput;
        public TextMeshProUGUI connectionStatus;
        
        [Header("Lobby Panel")]
        public GameObject lobbyPanel;
        public Button createRoomButton;
        public TMP_InputField roomCodeInput;
        public Button joinRoomButton;
        public TextMeshProUGUI roomCodeDisplay;
        public TextMeshProUGUI playerCountText;
        
        [Header("Class Selection")]
        public GameObject classPanel;
        public Button warriorButton;
        public Button mageButton;
        public Button healerButton;
        public Button rogueButton;
        public TextMeshProUGUI selectedClassText;
        
        [Header("Team Panel")]
        public GameObject teamPanel;
        public TMP_InputField teamNameInput;
        public Button createTeamButton;
        public Transform teamListParent;
        public GameObject teamItemPrefab;
        public Button readyButton;
        public TextMeshProUGUI readyStatusText;
        public TextMeshProUGUI teamMembersText;
        public Button joinTeamButton;
        public TMP_Dropdown teamSelectionDropdown;
        
        [Header("Game Panel")]
        public GameObject gamePanel;
        public TextMeshProUGUI currentTurnText;
        public TextMeshProUGUI gameStatusText;
        public TextMeshProUGUI teamPositionText;
        
        [Header("Battle Panel")]
        public GameObject battlePanel;
        public Button attackButton;
        public Button skillButton;
        public Button defendButton;
        public TextMeshProUGUI battleLogText;
        public TextMeshProUGUI battleTurnText;
        public Button backToMapButton;
        
        private BattleSystem.Client.GameClient client;
        private TeamCameraController cameraController;
        
        private bool isReady = false;
        private Dictionary<string, string> availableTeams = new Dictionary<string, string>();
        private string selectedClass = "";
        
        private void Start()
        {
            client = FindObjectOfType<BattleSystem.Client.GameClient>();
            cameraController = FindObjectOfType<TeamCameraController>();
            SetupUI();
            SetupEventListeners();
            ShowConnectionPanel();
        }
        
        private void SetupUI()
        {
            // Hide all panels initially
            connectionPanel.SetActive(false);
            lobbyPanel.SetActive(false);
            classPanel.SetActive(false);
            teamPanel.SetActive(false);
            gamePanel.SetActive(false);
            battlePanel.SetActive(false);
        }
        
        private void SetupEventListeners()
        {
            // Connection
            connectButton.onClick.AddListener(OnConnectClicked);
            
            // Lobby
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
            
            // Class selection
            warriorButton.onClick.AddListener(() => OnClassSelected("Warrior"));
            mageButton.onClick.AddListener(() => OnClassSelected("Mage"));
            healerButton.onClick.AddListener(() => OnClassSelected("Healer"));
            rogueButton.onClick.AddListener(() => OnClassSelected("Rogue"));
            
            // Team
            createTeamButton.onClick.AddListener(OnCreateTeamClicked);
            joinTeamButton.onClick.AddListener(OnJoinTeamClicked);
            readyButton.onClick.AddListener(OnReadyClicked);
            
            // Game
            
            // Battle
            attackButton.onClick.AddListener(() => OnBattleAction("Attack"));
            skillButton.onClick.AddListener(() => OnBattleAction("Skill"));
            defendButton.onClick.AddListener(() => OnBattleAction("Defend"));
            backToMapButton.onClick.AddListener(OnBackToMap);
            
            // Client events - Remove the problematic OnServerMessage line
            if (client != null)
            {
                client.OnConnected += OnConnected;
                client.OnDisconnected += OnDisconnected;
                // Remove this line: client.OnServerMessage += OnServerMessage;
            }
        }
        
        private async void OnConnectClicked()
        {
            connectionStatus.text = "Connecting...";
            connectButton.interactable = false;
            
            string serverIP = !string.IsNullOrEmpty(serverIPInput.text) ? serverIPInput.text : "127.0.0.1";
            int port = 7777; // Add the required port parameter
            
            await client.ConnectToServer(serverIP, port);
        }
        
        private void OnConnected()
        {
            connectionStatus.text = "Connected!";
            ShowLobbyPanel();
        }
        
        private void OnDisconnected()
        {
            connectionStatus.text = "Disconnected";
            connectButton.interactable = true;
            ShowConnectionPanel();
        }
        
        private void OnCreateRoomClicked()
        {
            client.CreateRoom();
        }
        
        private void OnJoinRoomClicked()
        {
            if (!string.IsNullOrEmpty(roomCodeInput.text))
            {
                client.JoinRoom(roomCodeInput.text.ToUpper());
            }
        }
        
        private void OnClassSelected(string className)
        {
            selectedClass = className;
            client.SelectClass(className);
            
            // Update UI to show selected class
            UpdateClassSelection(className);
            
            // Auto-proceed to team panel after short delay
            Invoke(nameof(ShowTeamPanel), 0.5f);
        }
        
        private void UpdateClassSelection(string className)
        {
            // Reset all button colors
            warriorButton.GetComponent<Image>().color = Color.white;
            mageButton.GetComponent<Image>().color = Color.white;
            healerButton.GetComponent<Image>().color = Color.white;
            rogueButton.GetComponent<Image>().color = Color.white;
            
            // Highlight selected class
            switch (className)
            {
                case "Warrior":
                    warriorButton.GetComponent<Image>().color = Color.green;
                    break;
                case "Mage":
                    mageButton.GetComponent<Image>().color = Color.blue;
                    break;
                case "Healer":
                    healerButton.GetComponent<Image>().color = Color.yellow;
                    break;
                case "Rogue":
                    rogueButton.GetComponent<Image>().color = Color.red;
                    break;
            }
            
            if (selectedClassText != null)
            {
                selectedClassText.text = $"Selected: {className}";
            }
        }
        
        private void OnCreateTeamClicked()
        {
            string teamName = teamNameInput.text;
            if (string.IsNullOrEmpty(teamName))
            {
                Debug.LogWarning("Enter a team name");
                return;
            }
            
            if (string.IsNullOrEmpty(selectedClass))
            {
                Debug.LogWarning("Select a class first");
                return;
            }
            
            client.CreateTeam(teamName);
        }
        
        private void OnJoinTeamClicked()
        {
            if (teamSelectionDropdown.options.Count > 0)
            {
                string teamName = teamSelectionDropdown.options[teamSelectionDropdown.value].text;
                if (availableTeams.ContainsKey(teamName))
                {
                    client.JoinTeam(availableTeams[teamName]);
                }
            }
        }
        
        private void OnBackToMap()
        {
            ShowGamePanel();
        }
        
        private void OnReadyClicked()
        {
            isReady = !isReady;
            client.SetTeamReady(isReady);
            UpdateReadyButton();
        }
        
        private void OnBattleAction(string actionType)
        {
            var actionData = new BattleActionData
            {
                Type = actionType,
                TargetId = "", // This would need to be selected from UI
                SkillId = ""
            };
            
            // Use the public PerformBattleAction method instead of SendRequest
            client.PerformBattleAction(actionData);
        }
        
        private void UpdateTeamDisplay()
        {
            if (teamMembersText != null)
            {
                // Use the correct property names from BattleSystem.Client.GameClient
                string teamInfo = client.TeamId ?? "No Team";
                string roleInfo = client.IsTeamLeader ? "Leader" : "Member";
                teamMembersText.text = $"Team: {teamInfo}\nRole: {roleInfo}";
            }
            
            if (readyButton != null)
            {
                readyButton.interactable = true;
            }
        }
        
        private void UpdateTeamPosition(string teamId, string position)
        {
            if (teamId == client.TeamId && teamPositionText != null)
            {
                teamPositionText.text = $"Your Position: {position}";
            }
        }
        
        private void UpdatePlayerCount(int count)
        {
            if (playerCountText != null)
            {
                playerCountText.text = $"Players: {count}/16";
            }
        }
        
        private void UpdateReadyButton()
        {
            readyButton.GetComponentInChildren<TextMeshProUGUI>().text = isReady ? "Not Ready" : "Ready";
            readyStatusText.text = isReady ? "Ready" : "Not Ready";
        }
        
        private void UpdateTeamDropdown()
        {
            if (teamSelectionDropdown != null)
            {
                teamSelectionDropdown.options.Clear();
                foreach (var team in availableTeams.Keys)
                {
                    teamSelectionDropdown.options.Add(new TMP_Dropdown.OptionData(team));
                }
                teamSelectionDropdown.RefreshShownValue();
            }
        }
        
        private void UpdateGameDisplay(ServerResponse response)
        {
            if (response.CurrentTurn != null)
            {
                bool isMyTurn = response.CurrentTurn == client.TeamId;
                currentTurnText.text = $"Current Turn: {(isMyTurn ? "YOUR TURN" : "Waiting...")}";
                
                if (isMyTurn)
                {
                    UpdateGameStatus("Your turn! Click a waypoint to move.");
                }
                else
                {
                    UpdateGameStatus("Waiting for other team's turn...");
                }
            }
        }
        
        private void UpdateGameStatus(string status)
        {
            if (gameStatusText != null)
            {
                gameStatusText.text = status;
            }
        }
        
        private void UpdateBattleLog(string message)
        {
            if (battleLogText != null)
            {
                battleLogText.text += $"\n{message}";
            }
        }
        
        private void ShowError(string message)
        {
            Debug.LogError($"Server Error: {message}");
            // Could show a popup or error panel here
        }
        
        // Panel management
        private void ShowConnectionPanel()
        {
            connectionPanel.SetActive(true);
            lobbyPanel.SetActive(false);
            classPanel.SetActive(false);
            teamPanel.SetActive(false);
            gamePanel.SetActive(false);
            battlePanel.SetActive(false);
        }
        
        private void ShowLobbyPanel()
        {
            connectionPanel.SetActive(false);
            lobbyPanel.SetActive(true);
        }
        
        private void ShowClassPanel()
        {
            lobbyPanel.SetActive(false);
            classPanel.SetActive(true);
        }
        
        private void ShowTeamPanel()
        {
            classPanel.SetActive(false);
            teamPanel.SetActive(true);
        }
        
        private void ShowGamePanel()
        {
            teamPanel.SetActive(false);
            gamePanel.SetActive(true);
            battlePanel.SetActive(false);
        }
        
        private void ShowBattlePanel()
        {
            gamePanel.SetActive(false);
            battlePanel.SetActive(true);
        }
        
        public void OnFocusTeamClicked()
        {
            cameraController?.FocusOnTeamPosition();
        }

        public void OnZoomInClicked()
        {
            cameraController?.ZoomIn();
        }

        public void OnZoomOutClicked()
        {
            cameraController?.ZoomOut();
        }
    }
}