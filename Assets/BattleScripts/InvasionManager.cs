using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using BattleSystem;

public class InvasionManager : MonoBehaviour
{
    [Header("Invasion UI")]
    public GameObject invasionPanel;
    public TMPro.TextMeshProUGUI invasionInfoText;
    public UnityEngine.UI.Button invadeButton;
    public UnityEngine.UI.Button cancelButton;
    
    private TurnManager turnManager;
    private List<WaypointBehaviour> invasionTargets = new List<WaypointBehaviour>();
    private WaypointBehaviour selectedTarget;

    void Start()
    {
        turnManager = FindObjectOfType<TurnManager>();
        
        if (invadeButton != null)
            invadeButton.onClick.AddListener(ExecuteInvasion);
            
        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelInvasion);
            
        invasionPanel?.SetActive(false);
    }

    void Update()
    {
        // Check for invasion opportunities each turn
        if (turnManager != null && turnManager.isPlayerTurn)
        {
            CheckInvasionOpportunities();
        }

        // Handle number key input for target selection
        if (invasionPanel != null && invasionPanel.activeInHierarchy && invasionTargets.Count > 0)
        {
            for (int i = 1; i <= invasionTargets.Count && i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    selectedTarget = invasionTargets[i - 1];
                    UpdateInvasionUI();
                    break;
                }
            }
        }
    }

    private void CheckInvasionOpportunities()
    {
        var currentTeam = turnManager.GetCurrentTeam();
        if (currentTeam == null) return;

        invasionTargets.Clear();
        var allWaypoints = FindObjectsOfType<WaypointBehaviour>();
        var currentWaypoint = allWaypoints.FirstOrDefault(w => w.teamsInWaypoint.Contains(currentTeam.Id));
        
        if (currentWaypoint == null) return;

        // Find adjacent waypoints with ongoing battles that can be invaded
        var adjacentWaypoints = GetAdjacentWaypoints(currentWaypoint);
        
        foreach (var waypoint in adjacentWaypoints)
        {
            if (waypoint.CanInvadeBattle())
            {
                invasionTargets.Add(waypoint);
            }
        }

        // Show invasion options if available
        if (invasionTargets.Count > 0 && (invasionPanel == null || !invasionPanel.activeInHierarchy))
        {
            ShowInvasionOptions();
        }
        else if (invasionTargets.Count == 0 && invasionPanel != null && invasionPanel.activeInHierarchy)
        {
            invasionPanel.SetActive(false);
        }
    }

    private List<WaypointBehaviour> GetAdjacentWaypoints(WaypointBehaviour currentWaypoint)
    {
        var adjacent = new List<WaypointBehaviour>();
        
        if (currentWaypoint.previousWaypoint != null)
            adjacent.Add(currentWaypoint.previousWaypoint);
            
        if (currentWaypoint.nextWaypoint != null)
            adjacent.Add(currentWaypoint.nextWaypoint);
            
        if (currentWaypoint.additionalWaypoint != null)
            adjacent.Add(currentWaypoint.additionalWaypoint);
            
        return adjacent;
    }

    private void ShowInvasionOptions()
    {
        if (invasionPanel == null) return;
        
        invasionPanel.SetActive(true);
        
        if (invasionInfoText != null)
        {
            string info = "Invasion Opportunities:\n\n";
            for (int i = 0; i < invasionTargets.Count; i++)
            {
                var target = invasionTargets[i];
                info += $"{i + 1}. {target.name} - {target.teamsInWaypoint.Count} teams in battle\n";
            }
            info += "\nPress number key (1-" + invasionTargets.Count + ") to select target, then click Invade";
            invasionInfoText.text = info;
        }
        
        // Select first target by default
        if (invasionTargets.Count > 0)
        {
            selectedTarget = invasionTargets[0];
        }
    }

    

    private void UpdateInvasionUI()
    {
        if (invasionInfoText != null && selectedTarget != null)
        {
            string info = "Invasion Opportunities:\n\n";
            for (int i = 0; i < invasionTargets.Count; i++)
            {
                var target = invasionTargets[i];
                string prefix = target == selectedTarget ? "► " : "  ";
                info += $"{prefix}{i + 1}. {target.name} - {target.teamsInWaypoint.Count} teams in battle\n";
            }
            info += "\nSelected: " + selectedTarget.name;
            invasionInfoText.text = info;
        }
    }

    private void ExecuteInvasion()
    {
        if (selectedTarget == null || turnManager == null) return;
        
        var currentTeam = turnManager.GetCurrentTeam();
        if (currentTeam == null) return;

        // Execute the invasion
        selectedTarget.InvadeBattle(currentTeam.Id);
        
        // Hide invasion panel
        invasionPanel?.SetActive(false);
        
        // End turn since team is now in battle
        turnManager.EndTurn();
        
        Debug.Log($"Team {currentTeam.Name} invaded battle at {selectedTarget.name}");
    }

    private void CancelInvasion()
    {
        invasionPanel?.SetActive(false);
    }
}