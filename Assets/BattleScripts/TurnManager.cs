using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using BattleSystem;

public class TurnManager : MonoBehaviour
{
    [Header("Turn Management")]
    public int currentTeamIndex = 0;
    public bool isPlayerTurn = true;
    
    [Header("UI")]
    public TMPro.TextMeshProUGUI turnIndicatorText;
    public UnityEngine.UI.Button endTurnButton;
    
    private List<Team> teams = new List<Team>();
    private SimpleGameManager gameManager;

    void Start()
    {
        gameManager = FindObjectOfType<SimpleGameManager>();
        if (gameManager != null)
        {
            teams = gameManager.Teams;
        }
        
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(EndTurn);
        }
        
        UpdateTurnUI();
    }

    public void EndTurn()
    {
        if (!isPlayerTurn) return;
        
        // Move to next team
        currentTeamIndex = (currentTeamIndex + 1) % teams.Count;
        
        // Skip teams that are in battle
        int attempts = 0;
        while (IsTeamInBattle(GetCurrentTeam()?.Id) && attempts < teams.Count)
        {
            currentTeamIndex = (currentTeamIndex + 1) % teams.Count;
            attempts++;
        }
        
        UpdateTurnUI();
        Debug.Log($"Turn ended. Current team: {GetCurrentTeam()?.Name}");
    }

    private bool IsTeamInBattle(string teamId)
    {
        if (string.IsNullOrEmpty(teamId)) return false;
        
        var waypoints = FindObjectsOfType<WaypointBehaviour>();
        return waypoints.Any(w => w.inBattle && w.teamsInWaypoint.Contains(teamId));
    }

    public Team GetCurrentTeam()
    {
        if (teams.Count == 0 || currentTeamIndex >= teams.Count) return null;
        return teams[currentTeamIndex];
    }

    private void UpdateTurnUI()
    {
        var currentTeam = GetCurrentTeam();
        if (turnIndicatorText != null && currentTeam != null)
        {
            turnIndicatorText.text = $"Current Turn: {currentTeam.Name}";
        }
        
        if (endTurnButton != null)
        {
            endTurnButton.interactable = isPlayerTurn && !IsTeamInBattle(currentTeam?.Id);
        }
    }

    public bool IsCurrentTeam(string teamId)
    {
        var currentTeam = GetCurrentTeam();
        return currentTeam != null && currentTeam.Id == teamId;
    }
}