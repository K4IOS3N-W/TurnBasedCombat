using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using BattleSystem.Server;
using BattleSystem.Map;

namespace BattleSystem.Client
{
    public class UIManager : MonoBehaviour
    {
        [Header("Connection UI")]
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private TMP_InputField serverIPInput;
        [SerializeField] private TMP_InputField serverPortInput;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button hostButton;
        [SerializeField] private TextMeshProUGUI connectionStatusText;
        
        [Header("Room UI")]
        [SerializeField] private GameObject roomPanel;
        [SerializeField] private TMP_InputField roomCodeInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private TextMeshProUGUI roomCodeDisplay;
        [SerializeField] private TextMeshProUGUI playerCountText;
        
        [Header("Class Selection")]
        [SerializeField] private GameObject classSelectionPanel;
        [SerializeField] private Button warriorButton;
        [SerializeField] private Button mageButton;
        [SerializeField] private Button rogueButton;
        [SerializeField] private Button healerButton;
        [SerializeField] private TextMeshProUGUI selectedClassText;
        
        [Header("Team Management")]
        [SerializeField] private GameObject teamPanel;
        [SerializeField] private TMP_InputField teamNameInput;
        [SerializeField] private Button createTeamButton;
        [SerializeField] private Transform teamListParent;
        [SerializeField] private GameObject teamItemPrefab;
        [SerializeField] private Button readyButton;
        [SerializeField] private TextMeshProUGUI readyStatusText;
        
        [Header("Game UI")]
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private TextMeshProUGUI currentTurnText;
        [SerializeField] private TextMeshProUGUI turnTimerText;
        [SerializeField] private Transform waypointButtonsParent;
        [SerializeField] private GameObject waypointButtonPrefab;
        [SerializeField] private TextMeshProUGUI teamPositionText;
        
        [Header("Battle UI")]
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private Button attackButton;
        [SerializeField] private Button skillButton;
        [SerializeField] private Button defendButton;
        [SerializeField] private Transform enemyListParent;
        [SerializeField] private GameObject enemyItemPrefab;
        [SerializeField] private TextMeshProUGUI battleLogText;
        [SerializeField] private Slider playerHealthSlider;
        [SerializeField] private TextMeshProUGUI playerHealthText;
        
        [Header("Player Stats")]
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI experienceText;
        [SerializeField] private Slider experienceSlider;
        
        [Header("Chat")]
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private Button sendChatButton;
        [SerializeField] private ScrollRect chatScrollRect;
        [SerializeField] private Transform chatContent;
        [SerializeField] private GameObject chatMessagePrefab;
        
        [Header("Map Integration")]
        [SerializeField] private GameObject mapPanel;
        
        private GameClient gameClient;
        private MapManager mapManager;
        private string selectedClass = "";
        private string currentTeamId = "";
        private bool isTeamLeader = false;
        private Dictionary<string, Button> waypointButtons = new Dictionary<string, Button>();
        private List<GameObject> teamItems = new List<GameObject>();
        private List<GameObject> enemyItems = new List<GameObject>();
        
        private void Start()
        {
            gameClient = FindObjectOfType<GameClient>();
            mapManager = FindObjectOfType<MapManager>();
            
            if (gameClient == null)
            {
                Debug.LogError("GameClient not found!");
                return;
            }
            
            SetupEventListeners();
            ShowConnectionPanel();
        }
        
        private void SetupEventListeners()
        {
            // Connection events
            connectButton.onClick.AddListener(ConnectToServer);
            hostButton.onClick.AddListener(StartHost);
            
            // Room events
            createRoomButton.onClick.AddListener(CreateRoom);
            joinRoomButton.onClick.AddListener(JoinRoom);
            
            // Class selection events
            warriorButton.onClick.AddListener(() => SelectClass("Warrior"));
            mageButton.onClick.AddListener(() => SelectClass("Mage"));
            rogueButton.onClick.AddListener(() => SelectClass("Rogue"));
            healerButton.onClick.AddListener(() => SelectClass("Healer"));
            
            // Team events
            createTeamButton.onClick.AddListener(CreateTeam);
            readyButton.onClick.AddListener(ToggleReady);
            
            // Battle events
            attackButton.onClick.AddListener(() => SelectBattleAction("Attack"));
            skillButton.onClick.AddListener(() => SelectBattleAction("Skill"));
            defendButton.onClick.AddListener(() => SelectBattleAction("Defend"));
            
            // Chat events
            sendChatButton.onClick.AddListener(SendChatMessage);
            chatInput.onSubmit.AddListener((text) => SendChatMessage());
            
            // Game client events
            gameClient.OnConnected += OnConnected;
            gameClient.OnDisconnected += OnDisconnected;
            gameClient.OnRoomCreated += OnRoomCreated;
            gameClient.OnRoomJoined += OnRoomJoined;
            gameClient.OnGameStarted += OnGameStarted;
            gameClient.OnTurnChanged += OnTurnChanged;
            gameClient.OnBattleStarted += OnBattleStarted;
            gameClient.OnBattleEnded += OnBattleEnded;
            gameClient.OnPlayerStatsUpdated += OnPlayerStatsUpdated;
            
            // Add these new event subscriptions
            gameClient.OnTeamCreated += OnTeamCreated;
            gameClient.OnTeamJoined += OnTeamJoined;
            gameClient.OnErrorMessage += OnErrorMessage;
            gameClient.OnServerMessage += OnServerMessage;
            gameClient.OnChatMessage += OnChatMessageReceived;
        }
        
        private void ShowConnectionPanel()
        {
            connectionPanel.SetActive(true);
            roomPanel.SetActive(false);
            classSelectionPanel.SetActive(false);
            teamPanel.SetActive(false);
            gamePanel.SetActive(false);
            battlePanel.SetActive(false);
            statsPanel.SetActive(false);
            chatPanel.SetActive(false);
        }
        
        private async void ConnectToServer()
        {
            string ip = serverIPInput.text;
            int port = int.Parse(serverPortInput.text);
            
            connectionStatusText.text = "Connecting...";
            connectButton.interactable = false;
            
            await gameClient.ConnectToServer(ip, port);
        }
        
        private void StartHost()
        {
            connectionStatusText.text = "Starting server...";
            hostButton.interactable = false;
            
            gameClient.StartAsHost();
        }
        
        private void OnConnected()
        {
            connectionStatusText.text = "Connected!";
            ShowRoomPanel();
        }
        
        private void OnDisconnected()
        {
            connectionStatusText.text = "Disconnected";
            connectButton.interactable = true;
            hostButton.interactable = true;
            ShowConnectionPanel();
        }
        
        private void ShowRoomPanel()
        {
            connectionPanel.SetActive(false);
            roomPanel.SetActive(true);
            classSelectionPanel.SetActive(true);
            chatPanel.SetActive(true);
        }
        
        private void CreateRoom()
        {
            gameClient.CreateRoom();
        }
        
        private void JoinRoom()
        {
            string roomCode = roomCodeInput.text.ToUpper();
            if (string.IsNullOrEmpty(roomCode))
            {
                Debug.LogWarning("Enter a room code");
                return;
            }
            
            gameClient.JoinRoom(roomCode);
        }
        
        private void OnRoomCreated(string roomCode)
        {
            roomCodeDisplay.text = $"Room Code: {roomCode}";
            ShowTeamPanel();
        }
        
        private void OnRoomJoined(string roomCode, int playerCount)
        {
            roomCodeDisplay.text = $"Room Code: {roomCode}";
            playerCountText.text = $"Players: {playerCount}";
            ShowTeamPanel();
        }
        
        private void SelectClass(string className)
        {
            selectedClass = className;
            selectedClassText.text = $"Selected: {className}";
            gameClient.SelectClass(className);
            
            // Enable class-specific buttons
            UpdateClassButtons();
        }
        
        private void UpdateClassButtons()
        {
            warriorButton.interactable = selectedClass != "Warrior";
            mageButton.interactable = selectedClass != "Mage";
            rogueButton.interactable = selectedClass != "Rogue";
            healerButton.interactable = selectedClass != "Healer";
        }
        
        private void ShowTeamPanel()
        {
            teamPanel.SetActive(true);
            statsPanel.SetActive(true);
        }
        
        private void CreateTeam()
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
            
            gameClient.CreateTeam(teamName);
        }
        
        private void ToggleReady()
        {
            // Fix the ready toggle logic
            bool newReadyState = !isTeamLeader; // This should be toggled properly
            gameClient.SetTeamReady(newReadyState);
            readyStatusText.text = newReadyState ? "Ready!" : "Not Ready";
        }
        
        // Add this method to handle team creation success
        public void OnTeamCreated(string teamId, string teamName)
        {
            currentTeamId = teamId;
            isTeamLeader = true;
            readyStatusText.text = "Team Leader";
            createTeamButton.interactable = false;
        }
        
        // Add this method to handle joining a team
        public void OnTeamJoined(string teamId)
        {
            currentTeamId = teamId;
            isTeamLeader = false;
            readyStatusText.text = "Not Ready";
            createTeamButton.interactable = false;
        }
        
        // Add error handling
        public void OnErrorMessage(string error)
        {
            Debug.LogError($"Server Error: {error}");
            // You could show this in a popup or status text
            connectionStatusText.text = error;
        }
        
        // Add server message handling
        public void OnServerMessage(string message)
        {
            Debug.Log($"Server: {message}");
        }
        
        private void OnGameStarted()
        {
            ShowGamePanel();
        }
        
        private void ShowGamePanel()
        {
            roomPanel.SetActive(false);
            classSelectionPanel.SetActive(false);
            teamPanel.SetActive(false);
            gamePanel.SetActive(true);
            
            // Show map panel if available
            if (mapPanel != null)
                mapPanel.SetActive(true);
            
            // Only update waypoint buttons if map manager is not available
            if (mapManager == null)
                UpdateWaypointButtons();
        }
        
        private void UpdateWaypointButtons()
        {
            // If we have a map manager, let it handle waypoint interactions
            if (mapManager != null) return;
            
            // Clear existing buttons
            foreach (Transform child in waypointButtonsParent)
            {
                Destroy(child.gameObject);
            }
            waypointButtons.Clear();
            
            // Get available moves from game client
            var availableMoves = gameClient.GetAvailableMoves();
            
            foreach (string waypoint in availableMoves)
            {
                GameObject buttonObj = Instantiate(waypointButtonPrefab, waypointButtonsParent);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                
                buttonText.text = waypoint;
                button.onClick.AddListener(() => gameClient.MoveToWaypoint(waypoint));
                
                waypointButtons[waypoint] = button;
            }
        }
        
        private void OnTurnChanged(string currentTeam, bool isMyTurn)
        {
            currentTurnText.text = isMyTurn ? "Your Turn!" : $"Team {currentTeam}'s Turn";
            
            if (mapManager != null)
            {
                // Map manager will handle enabling/disabling waypoints
                return;
            }
            
            // Enable/disable waypoint buttons based on turn (fallback)
            foreach (var button in waypointButtons.Values)
            {
                button.interactable = isMyTurn;
            }
        }
        
        private void OnBattleStarted(EncounterData encounter)
        {
            ShowBattlePanel(encounter);
        }
        
        private void ShowBattlePanel(EncounterData encounter)
        {
            gamePanel.SetActive(false);
            battlePanel.SetActive(true);
            
            // Create enemy buttons
            CreateEnemyButtons(encounter.Enemies);
            
            battleLogText.text = "Battle started!";
        }
        
        private void CreateEnemyButtons(List<string> enemies)
        {
            // Clear existing enemy items
            foreach (var item in enemyItems)
            {
                Destroy(item);
            }
            enemyItems.Clear();
            
            foreach (string enemy in enemies)
            {
                GameObject enemyObj = Instantiate(enemyItemPrefab, enemyListParent);
                Button enemyButton = enemyObj.GetComponent<Button>();
                TextMeshProUGUI enemyText = enemyObj.GetComponentInChildren<TextMeshProUGUI>();
                
                enemyText.text = enemy;
                enemyButton.onClick.AddListener(() => SelectTarget(enemy));
                
                enemyItems.Add(enemyObj);
            }
        }
        
        private string selectedTarget = "";
        private string selectedAction = "";
        
        private void SelectTarget(string targetId)
        {
            selectedTarget = targetId;
            
            // Highlight selected target
            foreach (var item in enemyItems)
            {
                var button = item.GetComponent<Button>();
                var colors = button.colors;
                colors.normalColor = button.GetComponentInChildren<TextMeshProUGUI>().text == targetId ? 
                    Color.yellow : Color.white;
                button.colors = colors;
            }
            
            // Execute action if we have both action and target
            if (!string.IsNullOrEmpty(selectedAction))
            {
                ExecuteBattleAction();
            }
        }
        
        private void SelectBattleAction(string action)
        {
            selectedAction = action;
            
            if (!string.IsNullOrEmpty(selectedTarget))
            {
                ExecuteBattleAction();
            }
        }
        
        private void ExecuteBattleAction()
        {
            if (string.IsNullOrEmpty(selectedAction) || string.IsNullOrEmpty(selectedTarget))
                return;
            
            var actionData = new BattleActionData
            {
                Type = selectedAction,
                TargetId = selectedTarget
            };
            
            gameClient.PerformBattleAction(actionData);
            
            // Reset selections
            selectedAction = "";
            selectedTarget = "";
        }
        
        private void OnBattleEnded(bool victory)
        {
            battleLogText.text += victory ? "\nVictory!" : "\nDefeat!";
            
            // Return to game view after delay
            Invoke(nameof(ReturnToGame), 2f);
        }
        
        private void ReturnToGame()
        {
            battlePanel.SetActive(false);
            gamePanel.SetActive(true);
            UpdateWaypointButtons();
        }
        
        private void OnPlayerStatsUpdated(PlayerStats stats)
        {
            levelText.text = $"Level: {stats.Level}";
            experienceText.text = $"EXP: {stats.Experience}";
            
            // Update health
            playerHealthSlider.value = (float)stats.Health / stats.MaxHealth;
            playerHealthText.text = $"{stats.Health}/{stats.MaxHealth}";
            
            // Update experience bar
            int expNeeded = stats.Level * 100;
            experienceSlider.value = (float)stats.Experience / expNeeded;
        }
        
        private void SendChatMessage()
        {
            string message = chatInput.text.Trim();
            if (string.IsNullOrEmpty(message)) return;
            
            // For now, just display locally (server chat would need implementation)
            AddChatMessage("You", message);
            chatInput.text = "";
        }
        
        private void AddChatMessage(string sender, string message)
        {
            GameObject chatObj = Instantiate(chatMessagePrefab, chatContent);
            TextMeshProUGUI chatText = chatObj.GetComponentInChildren<TextMeshProUGUI>();
            chatText.text = $"<b>{sender}:</b> {message}";
            
            // Scroll to bottom
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
        
        private void Update()
        {
            // Update turn timer if in game
            if (gamePanel.activeSelf && gameClient.IsConnected)
            {
                float timeRemaining = gameClient.GetTurnTimeRemaining();
                turnTimerText.text = $"Time: {Mathf.Ceil(timeRemaining)}s";
            }
        }
        
        // Add chat message handling
        private void OnChatMessageReceived(string message, string sender)
        {
            AddChatMessage(sender, message);
        }
    }
}