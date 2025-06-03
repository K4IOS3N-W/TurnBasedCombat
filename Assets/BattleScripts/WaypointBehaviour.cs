using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using BattleSystem;

public class WaypointBehaviour : MonoBehaviour
{
    [Header("Assign these in the Inspector:")]
    [Tooltip("The waypoint you consider 'before' this one")]
    public WaypointBehaviour previousWaypoint;

    [Tooltip("The waypoint you consider 'after' this one")]
    public WaypointBehaviour nextWaypoint;

    [Tooltip("An extra branch/connection (if this waypoint has 3 neighbors)")]
    public WaypointBehaviour additionalWaypoint;

    [Header("Visual Settings")]
    public Material activeMaterial;
    public Material inactiveMaterial;
    public Material enemyMaterial;
    private Renderer waypointRenderer;

    [Header("Battle Settings")]
    public bool hasEnemies = false;
    public List<string> enemyTemplateIds = new List<string>();
    public bool inBattle = false;
    public string battleId;

    [Header("Team Tracking")]
    public List<string> teamsInWaypoint = new List<string>();

    private BattleManager battleManager;
    private static System.Random random = new System.Random();

    void Start()
    {
        waypointRenderer = GetComponent<Renderer>();
        battleManager = FindObjectOfType<BattleManager>();
        
        if (waypointRenderer != null && inactiveMaterial != null)
        {
            waypointRenderer.material = inactiveMaterial;
        }

        // Randomly spawn enemies (60% chance)
        if (!name.StartsWith("spawn_") && random.NextDouble() < 0.6f)
        {
            SpawnRandomEnemies();
        }

        UpdateVisualState();
    }

    private void SpawnRandomEnemies()
    {
        hasEnemies = true;
        enemyTemplateIds.Clear();
        
        // Add 1-3 random enemies
        string[] enemyTypes = { "goblin", "orc", "dragon" };
        int enemyCount = random.Next(1, 4);
        
        for (int i = 0; i < enemyCount; i++)
        {
            string randomEnemy = enemyTypes[random.Next(enemyTypes.Length)];
            enemyTemplateIds.Add(randomEnemy);
        }
        
        Debug.Log($"Spawned enemies at {name}: {string.Join(", ", enemyTemplateIds)}");
    }

    /// <summary>
    /// Call this when the player "selects" (clicks) this waypoint.
    /// It disables itself and enables all assigned neighbors (up to 3).
    /// </summary>
    public void OnSelected()
    {
        // Don't allow selection if in battle
        if (inBattle)
        {
            Debug.Log("Cannot move to waypoint - battle in progress!");
            return;
        }

        // Get the team that's moving
        string teamId = GetMovingTeamId();
        if (string.IsNullOrEmpty(teamId))
        {
            Debug.LogError("No team found for movement!");
            return;
        }

        // Remove team from previous waypoint
        RemoveTeamFromOtherWaypoints(teamId);

        // Add team to this waypoint
        if (!teamsInWaypoint.Contains(teamId))
        {
            teamsInWaypoint.Add(teamId);
            Debug.Log($"Team {teamId} moved to waypoint {name}");
        }

        // Check for battle conditions
        CheckForBattle();

        // Disable this waypoint and enable neighbors (if not in battle)
        if (!inBattle)
        {
            gameObject.SetActive(false);
            EnableNeighbors();
        }
        
        UpdateVisualState();
    }

    private void RemoveTeamFromOtherWaypoints(string teamId)
    {
        var allWaypoints = FindObjectsOfType<WaypointBehaviour>();
        foreach (var waypoint in allWaypoints)
        {
            if (waypoint != this)
            {
                waypoint.teamsInWaypoint.Remove(teamId);
            }
        }
    }

    private void CheckForBattle()
    {
        // PvP Battle: Multiple teams in same waypoint
        if (teamsInWaypoint.Count > 1)
        {
            StartPvPBattle();
        }
        // PvE Battle: Team encounters enemies
        else if (teamsInWaypoint.Count == 1 && hasEnemies)
        {
            StartPvEBattle();
        }
    }

    private void StartPvPBattle()
    {
        if (battleManager == null) return;

        Debug.Log($"Starting PvP battle at waypoint {name}");
        battleId = battleManager.CreatePvPBattle(transform.position, teamsInWaypoint);
        inBattle = true;
        UpdateVisualState();
    }

    private void StartPvEBattle()
    {
        if (battleManager == null) return;

        Debug.Log($"Starting PvE battle at waypoint {name}");
        battleId = battleManager.CreatePvEBattle(transform.position, teamsInWaypoint[0], enemyTemplateIds);
        inBattle = true;
        UpdateVisualState();
    }

    public void OnBattleEnded(string winnerTeamId)
    {
        inBattle = false;
        battleId = null;
        
        // Remove defeated teams or clear enemies
        if (!string.IsNullOrEmpty(winnerTeamId))
        {
            teamsInWaypoint.Clear();
            teamsInWaypoint.Add(winnerTeamId);
            
            // Clear enemies if PvE battle
            if (hasEnemies)
            {
                hasEnemies = false;
                enemyTemplateIds.Clear();
            }
        }

        UpdateVisualState();
        EnableNeighbors();
    }

    public bool CanInvadeBattle()
    {
        return inBattle && !string.IsNullOrEmpty(battleId) && teamsInWaypoint.Count < 3;
    }

    public void InvadeBattle(string invadingTeamId)
    {
        if (!CanInvadeBattle()) return;

        if (!teamsInWaypoint.Contains(invadingTeamId))
        {
            teamsInWaypoint.Add(invadingTeamId);
        }

        // Get battle and add invading team
        var battle = battleManager.GetBattle(battleId);
        if (battle != null)
        {
            var gameManager = FindObjectOfType<SimpleGameManager>();
            var invadingTeam = gameManager?.Teams.FirstOrDefault(t => t.Id == invadingTeamId);
            
            if (invadingTeam != null)
            {
                battle.AddInvadingTeam(invadingTeam);
                Debug.Log($"Team {invadingTeamId} invaded battle at {name}");
            }
        }
    }

    private void EnableNeighbors()
    {
        if (previousWaypoint != null)
        {
            previousWaypoint.EnableWaypoint();
        }

        if (nextWaypoint != null)
        {
            nextWaypoint.EnableWaypoint();
        }

        if (additionalWaypoint != null)
        {
            additionalWaypoint.EnableWaypoint();
        }
    }

    /// <summary>
    /// Enable this waypoint and make it visible/clickable
    /// </summary>
    public void EnableWaypoint()
    {
        gameObject.SetActive(true);
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (waypointRenderer == null) return;

        if (inBattle)
        {
            // Battle in progress - use special material
            waypointRenderer.material = enemyMaterial ?? activeMaterial;
        }
        else if (hasEnemies)
        {
            // Enemies present - use enemy material
            waypointRenderer.material = enemyMaterial ?? inactiveMaterial;
        }
        else
        {
            // Normal waypoint
            waypointRenderer.material = inactiveMaterial;
        }
    }

    private string GetMovingTeamId()
    {
        // Get the default team from SimpleGameManager
        SimpleGameManager gameManager = FindObjectOfType<SimpleGameManager>();
        var defaultTeam = gameManager?.GetDefaultTeam();
        return defaultTeam?.Id;
    }

    void OnMouseEnter()
    {
        if (!inBattle && waypointRenderer != null && activeMaterial != null)
        {
            waypointRenderer.material = activeMaterial;
        }
    }

    void OnMouseExit()
    {
        if (!inBattle)
        {
            UpdateVisualState();
        }
    }

    void OnDrawGizmos()
    {
        // Draw connections in Scene view for debugging
        Gizmos.color = Color.blue;
        
        if (previousWaypoint != null)
        {
            Gizmos.DrawLine(transform.position, previousWaypoint.transform.position);
        }
        
        if (nextWaypoint != null)
        {
            Gizmos.DrawLine(transform.position, nextWaypoint.transform.position);
        }
        
        if (additionalWaypoint != null)
        {
            Gizmos.DrawLine(transform.position, additionalWaypoint.transform.position);
        }

        // Draw enemy indicator
        if (hasEnemies)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2, 0.5f);
        }

        // Draw battle indicator
        if (inBattle)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 3, Vector3.one);
        }

        // Draw invasion possibility
        if (CanInvadeBattle())
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 4, Vector3.one * 0.5f);
        }
    }
}
