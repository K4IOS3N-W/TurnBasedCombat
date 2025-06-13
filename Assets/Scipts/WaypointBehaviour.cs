using UnityEngine;
using BattleSystem.Server; // Add this line
using System.Collections.Generic;

public class WaypointBehaviour : MonoBehaviour
{
    [Header("Waypoint Settings")]
    public string waypointId;
    public bool isGoal = false;
    public bool isStartPosition = false;
    
    [Header("Visual Indicators")]
    public GameObject enemyIndicator;
    public GameObject goalIndicator;
    public GameObject startIndicator;
    public GameObject[] teamMarkers = new GameObject[4]; // Support up to 4 teams
    
    [Header("Encounter Data")]
    public bool hasEnemies = false;
    public List<string> enemyTypes = new List<string> { "Goblin", "Orc" };
    
    [Header("Visual Effects")]
    public ParticleSystem moveEffect;
    public AudioSource moveSound;
    
    private GameClient client;
    private bool isCurrentlyOccupied = false;
    
    void Start()
    {
        client = GameClient.Instance;
        
        // Auto-generate waypoint ID if not set
        if (string.IsNullOrEmpty(waypointId))
        {
            if (isGoal)
                waypointId = "goal";
            else if (isStartPosition)
                waypointId = "start";
            else
                waypointId = $"waypoint{Random.Range(1, 100)}";
        }
        
        SetupVisuals();
        
        // Subscribe to client events
        if (client != null)
        {
            client.OnServerMessage += OnServerMessage;
        }
    }
    
    private void SetupVisuals()
    {
        // Set up visual indicators
        if (enemyIndicator != null)
            enemyIndicator.SetActive(hasEnemies);
            
        if (goalIndicator != null)
            goalIndicator.SetActive(isGoal);
            
        if (startIndicator != null)
            startIndicator.SetActive(isStartPosition);
            
        // Hide all team markers initially
        foreach (var marker in teamMarkers)
        {
            if (marker != null)
                marker.SetActive(false);
        }
        
        // Set waypoint color based on type
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            if (isGoal)
                renderer.material.color = Color.yellow; // Changed from Color.gold
            else if (isStartPosition)
                renderer.material.color = Color.green;
            else if (hasEnemies)
                renderer.material.color = Color.red;
            else
                renderer.material.color = Color.white;
        }
    }
    
    private void OnServerMessage(ServerResponse response)
    {
        switch (response.Type?.ToLower())
        {
            case "teammoved":
                UpdateTeamPosition(response.TeamId, response.NewPosition);
                break;
                
            case "gamestarted":
                // Reset all team positions
                ResetTeamMarkers();
                break;
        }
    }
    
    private void UpdateTeamPosition(string teamId, string newPosition)
    {
        // Clear this waypoint if team moved away
        if (newPosition != waypointId)
        {
            ClearTeamMarker(teamId);
        }
        // Show team marker if they moved here
        else if (newPosition == waypointId)
        {
            ShowTeamMarker(teamId);
            PlayMoveEffects();
        }
    }
    
    private void ShowTeamMarker(string teamId)
    {
        // Simple team assignment to markers (could be improved with team indexing)
        int teamIndex = teamId.GetHashCode() % teamMarkers.Length;
        teamIndex = Mathf.Abs(teamIndex);
        
        if (teamIndex < teamMarkers.Length && teamMarkers[teamIndex] != null)
        {
            teamMarkers[teamIndex].SetActive(true);
            isCurrentlyOccupied = true;
        }
    }
    
    private void ClearTeamMarker(string teamId)
    {
        // Clear the team marker (simplified approach)
        int teamIndex = teamId.GetHashCode() % teamMarkers.Length;
        teamIndex = Mathf.Abs(teamIndex);
        
        if (teamIndex < teamMarkers.Length && teamMarkers[teamIndex] != null)
        {
            teamMarkers[teamIndex].SetActive(false);
        }
        
        // Check if any team is still here
        isCurrentlyOccupied = false;
        foreach (var marker in teamMarkers)
        {
            if (marker != null && marker.activeInHierarchy)
            {
                isCurrentlyOccupied = true;
                break;
            }
        }
    }
    
    private void ResetTeamMarkers()
    {
        foreach (var marker in teamMarkers)
        {
            if (marker != null)
                marker.SetActive(false);
        }
        isCurrentlyOccupied = false;
        
        // Show teams at start position
        if (isStartPosition)
        {
            // This would be handled by server messages
        }
    }
    
    private void PlayMoveEffects()
    {
        if (moveEffect != null)
        {
            moveEffect.Play();
        }
        
        if (moveSound != null)
        {
            moveSound.Play();
        }
    }
    
    void OnMouseDown()
    {
        // Handle click to move (if it's player's turn)
        if (client != null && client.IsConnected)
        {
            // The UI handles movement now, but this could be alternative input
            Debug.Log($"Clicked waypoint: {waypointId}");
        }
    }
    
    private void OnDestroy()
    {
        if (client != null)
        {
            client.OnServerMessage -= OnServerMessage;
        }
    }
    
    // Helper method to get waypoint info
    public WaypointInfo GetWaypointInfo()
    {
        return new WaypointInfo
        {
            Id = waypointId,
            IsGoal = isGoal,
            IsStart = isStartPosition,
            HasEnemies = hasEnemies,
            EnemyTypes = new List<string>(enemyTypes),
            IsOccupied = isCurrentlyOccupied
        };
    }
}

[System.Serializable]
public class WaypointInfo
{
    public string Id;
    public bool IsGoal;
    public bool IsStart;
    public bool HasEnemies;
    public List<string> EnemyTypes;
    public bool IsOccupied;
}