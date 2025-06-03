using System.Collections.Generic;
using UnityEngine;
using BattleSystem;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

public class BattleManagerExtension : MonoBehaviour
{
    public static BattleManagerExtension Instance { get; private set; }

    [Header("Player Prefabs")]
    public GameObject warriorPrefab;
    public GameObject magePrefab;
    public GameObject healerPrefab;

    [Header("Spawn Points")]
    public Transform[] teamSpawnPoints;

    // Events for game flow - use fully qualified System.Action to avoid ambiguity
    public event System.Action<string> OnBattleStarted;
    public event System.Action<Player> OnPlayerJoined;
    public event System.Action<Team> OnTeamCreated;
    public event System.Action OnAllPlayersReady;

    private BattleManager battleManager;
    private GameManager gameManager;
    private Dictionary<string, GameObject> playerObjects = new Dictionary<string, GameObject>();
    private List<Team> teams = new List<Team>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        battleManager = GetComponent<BattleManager>();
        gameManager = FindObjectOfType<GameManager>();
    }

    private void Start()
    {
        // Initialize event handlers
        // You'll need to add methods to track when players join, teams are created, etc.

        // Set up event listeners for NetworkManager
        if (NetworkManager.Instance != null)
        {
            // You'll need to add message handlers to process network events
        }
    }

    // New method to simulate battle start for testing
    public void TriggerBattleStarted(string battleId)
    {
        OnBattleStarted?.Invoke(battleId);
    }

    // New method to simulate player joined for testing
    public void TriggerPlayerJoined(Player player)
    {
        OnPlayerJoined?.Invoke(player);
    }

    // New method to simulate team created for testing
    public void TriggerTeamCreated(Team team)
    {
        OnTeamCreated?.Invoke(team);
    }

    // New method to simulate all players ready for testing
    public void TriggerAllPlayersReady()
    {
        OnAllPlayersReady?.Invoke();
    }

    // Method to register a player
    public void RegisterPlayer(Player player)
    {
        // Add the player to your tracking
        OnPlayerJoined?.Invoke(player);
    }

    // Method to register a team
    public void RegisterTeam(Team team)
    {
        teams.Add(team);
        OnTeamCreated?.Invoke(team);
    }

    // Method to check if all players are ready
    public void CheckAllPlayersReady()
    {
        if (teams.Count > 0 && teams.All(t => t.IsReady))
        {
            OnAllPlayersReady?.Invoke();
        }
    }

    // Method to get teams
    public List<Team> GetTeams()
    {
        // If gameManager is available, use its teams
        if (gameManager != null && gameManager.Teams != null)
        {
            return gameManager.Teams;
        }

        // Otherwise return our local teams list
        return teams;
    }

    // Rename event handler methods to avoid conflicts with the event definitions
    private void HandleBattleStarted(string battleId)
    {
        Debug.Log($"Battle started: {battleId}");

        // Load the battle scene
        if (battleManager.useSceneTransition)
        {
            SceneManager.LoadScene("BattleScene");
        }

        // Spawn player characters
        StartCoroutine(SpawnPlayerCharacters());
    }

    private void HandlePlayerJoined(Player player)
    {
        Debug.Log($"Player joined: {player.Name} as {player.Class}");

        // Update UI if needed
        if (NetworkManager.Instance != null && NetworkManager.Instance.lobbyUI != null)
        {
            // Update player list in the UI
        }
    }

    private void HandleTeamCreated(Team team)
    {
        Debug.Log($"Team created: {team.Name}");

        // Update UI if needed
        if (NetworkManager.Instance != null && NetworkManager.Instance.lobbyUI != null)
        {
            // Update team list in the UI
        }
    }

    private void HandleAllPlayersReady()
    {
        Debug.Log("All players are ready!");

        // Start the game if we're the server
        if (NetworkManager.Instance != null && NetworkManager.Instance.isServer)
        {
            NetworkManager.Instance.StartGame();
        }
    }

    private System.Collections.IEnumerator SpawnPlayerCharacters()
    {
        // Wait for scene to load
        yield return new WaitForSeconds(0.5f);

        // Get teams
        var teams = GetTeams();

        // Spawn each team at their designated spawn point
        for (int i = 0; i < teams.Count && i < teamSpawnPoints.Length; i++)
        {
            SpawnTeam(teams[i], teamSpawnPoints[i]);
        }
    }

    private void SpawnTeam(Team team, Transform spawnPoint)
    {
        // Calculate positions for each team member
        Vector3 centerPos = spawnPoint.position;
        float radius = 2f;
        int playerCount = team.Players.Count;

        for (int i = 0; i < playerCount; i++)
        {
            Player player = team.Players[i];

            // Calculate position in a circle around the spawn point
            float angle = i * (360f / playerCount);
            Vector3 pos = centerPos + new Vector3(
                radius * Mathf.Cos(angle * Mathf.Deg2Rad),
                0,
                radius * Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            // Spawn the appropriate prefab based on class
            GameObject playerPrefab = null;
            switch (player.Class.ToLower())
            {
                case "warrior":
                    playerPrefab = warriorPrefab;
                    break;
                case "mage":
                    playerPrefab = magePrefab;
                    break;
                case "healer":
                    playerPrefab = healerPrefab;
                    break;
            }

            if (playerPrefab != null)
            {
                GameObject playerObj = Instantiate(playerPrefab, pos, Quaternion.identity);

                // Set up player controller
                PlayerController controller = playerObj.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.SetTeamId(team.Id);

                    // If this is our player, attach camera
                    if (NetworkManager.Instance != null &&
                        NetworkManager.Instance.PlayerId == player.Id)
                    {
                        // Find and activate camera
                        var cameraController = FindObjectOfType<CameraController>();
                        if (cameraController != null)
                        {
                            cameraController.SetTarget(playerObj.transform);
                        }
                    }
                }

                // Store reference
                playerObjects[player.Id] = playerObj;
            }
        }
    }

    private void OnDestroy()
    {
        // No need to unsubscribe from events since we're defining them ourselves
    }
}
