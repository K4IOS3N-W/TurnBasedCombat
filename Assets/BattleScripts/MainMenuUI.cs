using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject hostJoinPanel;
    public GameObject settingsPanel;
    
    [Header("Host Game Panel")]
    public TMP_InputField hostNameInput;
    public TMP_InputField portInput;
    public Button startServerButton;
    
    [Header("Join Game Panel")]
    public TMP_InputField joinNameInput;
    public TMP_InputField serverIPInput;
    public TMP_InputField joinPortInput;
    public Button connectButton;
    
    private void Start()
    {
        // Set default values
        portInput.text = "7777";
        joinPortInput.text = "7777";
        serverIPInput.text = "127.0.0.1";
        
        // Register with NetworkManager
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.mainMenuUI = this;
        }
        
        // Setup button listeners
        startServerButton.onClick.AddListener(OnStartServerClicked);
        connectButton.onClick.AddListener(OnConnectClicked);
        
        // Start with main panel
        ShowMainPanel();
    }
    
    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        hostJoinPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }
    
    public void ShowHostJoinPanel()
    {
        mainPanel.SetActive(false);
        hostJoinPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }
    
    public void ShowSettingsPanel()
    {
        mainPanel.SetActive(false);
        hostJoinPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }
    
    public void OnHostGameClicked()
    {
        hostNameInput.text = "Host";
        ShowHostJoinPanel();
    }
    
    public void OnJoinGameClicked()
    {
        joinNameInput.text = "Player";
        ShowHostJoinPanel();
    }
    
    private void OnStartServerClicked()
    {
        if (NetworkManager.Instance != null)
        {
            // Set player name
            NetworkManager.Instance.playerName = hostNameInput.text;
            
            // Set server port
            if (int.TryParse(portInput.text, out int port))
            {
                NetworkManager.Instance.serverPort = port;
            }
            
            // Start server
            NetworkManager.Instance.isServer = true;
            NetworkManager.Instance.StartServer();
            
            // Connect to own server
            NetworkManager.Instance.ConnectToServer();
        }
    }
    
    private void OnConnectClicked()
    {
        if (NetworkManager.Instance != null)
        {
            // Set player name
            NetworkManager.Instance.playerName = joinNameInput.text;
            
            // Set server IP and port
            NetworkManager.Instance.serverIP = serverIPInput.text;
            
            if (int.TryParse(joinPortInput.text, out int port))
            {
                NetworkManager.Instance.serverPort = port;
            }
            
            // Connect to server
            NetworkManager.Instance.ConnectToServer();
        }
    }
    
    public void OnQuitClicked()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
