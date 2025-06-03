using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject characterSelectionPanel;
    public GameObject teamCreationPanel;
    public GameObject waitingForPlayersPanel;
    public GameObject readyPanel;
    
    [Header("Character Selection")]
    public Button warriorButton;
    public Button mageButton;
    public Button healerButton;
    public TMP_InputField playerNameInput;
    public Button confirmCharacterButton;
    public TextMeshProUGUI characterDescriptionText;
    
    [Header("Team Creation")]
    public Transform teamListContainer;
    public Button createTeamButton;
    public Button confirmTeamButton;
    public GameObject teamPrefab;
    
    [Header("Ready Panel")]
    public Button readyButton;
    public Button notReadyButton;
    public TextMeshProUGUI playersReadyText;
    public TextMeshProUGUI waitingForText;
    
    private string selectedClass = "";
    private readonly Dictionary<string, GameObject> teamUIElements = new Dictionary<string, GameObject>();
    
    private void Start()
    {
        // Register with NetworkManager
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.lobbyUI = this;
        }
        
        // Setup button listeners
        warriorButton.onClick.AddListener(() => SelectClass("Warrior"));
        mageButton.onClick.AddListener(() => SelectClass("Mage"));
        healerButton.onClick.AddListener(() => SelectClass("Healer"));
        confirmCharacterButton.onClick.AddListener(OnConfirmCharacter);
        
        createTeamButton.onClick.AddListener(OnCreateTeam);
        confirmTeamButton.onClick.AddListener(OnConfirmTeam);
        
        readyButton.onClick.AddListener(() => SetReadyStatus(true));
        notReadyButton.onClick.AddListener(() => SetReadyStatus(false));
        
        // Start with character selection
        ShowCharacterSelection();
    }
    
    public void ShowCharacterSelection()
    {
        characterSelectionPanel.SetActive(true);
        teamCreationPanel.SetActive(false);
        waitingForPlayersPanel.SetActive(false);
        readyPanel.SetActive(false);
        
        // Set default player name if not already set
        if (string.IsNullOrEmpty(playerNameInput.text) && NetworkManager.Instance != null)
        {
            playerNameInput.text = NetworkManager.Instance.playerName;
        }
    }
    
    public void ShowTeamCreation()
    {
        characterSelectionPanel.SetActive(false);
        teamCreationPanel.SetActive(true);
        waitingForPlayersPanel.SetActive(false);
        readyPanel.SetActive(false);
        
        // Refresh team list
        RefreshTeamList();
    }
    
    public void ShowWaitingForPlayers()
    {
        characterSelectionPanel.SetActive(false);
        teamCreationPanel.SetActive(false);
        waitingForPlayersPanel.SetActive(true);
        readyPanel.SetActive(false);
    }
    
    public void ShowReadyScreen()
    {
        characterSelectionPanel.SetActive(false);
        teamCreationPanel.SetActive(false);
        waitingForPlayersPanel.SetActive(false);
        readyPanel.SetActive(true);
        
        // Update ready status UI
        UpdateReadyStatus();
    }
    
    private void SelectClass(string className)
    {
        selectedClass = className;
        
        // Update UI to show selected class
        warriorButton.GetComponent<Image>().color = (className == "Warrior") ? Color.green : Color.white;
        mageButton.GetComponent<Image>().color = (className == "Mage") ? Color.green : Color.white;
        healerButton.GetComponent<Image>().color = (className == "Healer") ? Color.green : Color.white;
        
        // Update description text
        switch (className)
        {
            case "Warrior":
                characterDescriptionText.text = "Warriors are strong frontline fighters with high health and defense.";
                break;
            case "Mage":
                characterDescriptionText.text = "Mages are powerful spellcasters that deal high damage but have low health.";
                break;
            case "Healer":
                characterDescriptionText.text = "Healers can restore health to allies and provide useful buffs.";
                break;
        }
    }
    
    private void OnConfirmCharacter()
    {
        if (string.IsNullOrEmpty(selectedClass) || string.IsNullOrEmpty(playerNameInput.text))
        {
            Debug.LogWarning("Please select a class and enter a name");
            return;
        }
        
        if (NetworkManager.Instance != null)
        {
            // Save player info
            NetworkManager.Instance.SetPlayerInfo(playerNameInput.text, selectedClass);
            
            // Move to team creation
            ShowTeamCreation();
        }
    }
    
    private void RefreshTeamList()
    {
        // Clear existing team UI elements
        foreach (Transform child in teamListContainer)
        {
            Destroy(child.gameObject);
        }
        teamUIElements.Clear();
        
        // Example - add team data from NetworkManager
        // In a real implementation, this would be populated with actual team data
        AddTeamToUI("Team 1", 2, true);
        AddTeamToUI("Team 2", 1, false);
    }
    
    private void AddTeamToUI(string teamName, int playerCount, bool joined)
    {
        GameObject teamUI = Instantiate(teamPrefab, teamListContainer);
        teamUI.transform.Find("TeamNameText").GetComponent<TextMeshProUGUI>().text = teamName;
        teamUI.transform.Find("PlayerCountText").GetComponent<TextMeshProUGUI>().text = $"Players: {playerCount}/4";
        
        Button joinButton = teamUI.transform.Find("JoinButton").GetComponent<Button>();
        
        if (joined)
        {
            joinButton.GetComponentInChildren<TextMeshProUGUI>().text = "Joined";
            joinButton.interactable = false;
        }
        else
        {
            joinButton.onClick.AddListener(() => OnJoinTeam(teamName));
        }
        
        teamUIElements[teamName] = teamUI;
    }
    
    private void OnCreateTeam()
    {
        if (NetworkManager.Instance != null)
        {
            // Create a new team (empty teamId)
            NetworkManager.Instance.CreateOrJoinTeam("");
            
            // Move to ready screen
            ShowReadyScreen();
        }
    }
    
    private void OnJoinTeam(string teamName)
    {
        if (NetworkManager.Instance != null)
        {
            // Join an existing team
            // We would need to map teamName to teamId in a real implementation
            string teamId = teamName; // Placeholder
            NetworkManager.Instance.CreateOrJoinTeam(teamId);
            
            // Move to ready screen
            ShowReadyScreen();
        }
    }
    
    private void OnConfirmTeam()
    {
        // This would be called after selecting a team
        ShowReadyScreen();
    }
    
    private void SetReadyStatus(bool ready)
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SetReadyStatus(ready);
            
            // Update UI
            readyButton.gameObject.SetActive(!ready);
            notReadyButton.gameObject.SetActive(ready);
        }
    }
    
    private void UpdateReadyStatus()
    {
        // Update UI based on how many players are ready
        // This would be populated with actual data in a real implementation
        int readyCount = 3;
        int totalCount = 5;
        playersReadyText.text = $"Players Ready: {readyCount}/{totalCount}";
        
        if (readyCount < totalCount)
        {
            waitingForText.gameObject.SetActive(true);
            waitingForText.text = "Waiting for all players to be ready...";
        }
        else
        {
            waitingForText.gameObject.SetActive(true);
            waitingForText.text = "All players ready! Starting game...";
            
            // Start the game
            if (NetworkManager.Instance != null && NetworkManager.Instance.isTeamLeader)
            {
                Invoke(nameof(StartGame), 2f);
            }
        }
    }
    
    private void StartGame()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.StartGame();
        }
    }
}
