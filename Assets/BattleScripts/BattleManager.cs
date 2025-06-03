using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using BattleSystem;

public class BattleManager : MonoBehaviour
{
    [Header("Battle Settings")]
    public bool useSceneTransition = false; // Set to false for single scene

    [Header("Enemy Templates")]
    public List<EnemyTemplate> enemyTemplates = new List<EnemyTemplate>();

    [Header("UI References")]
    public BattleUI battleUI;

    private Dictionary<string, Battle> activeBattles = new Dictionary<string, Battle>();
    private Dictionary<string, Enemy> enemyTemplateDict = new Dictionary<string, Enemy>();
    private GameManager gameManager;

    [System.Serializable]
    public class EnemyTemplate
    {
        public string id;
        public string displayName;
        public GameObject prefab; // For visual representation
    }

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        InitializeEnemyTemplates();
        
        // Find BattleUI if not assigned
        if (battleUI == null)
            battleUI = FindObjectOfType<BattleUI>();
    }

    private void InitializeEnemyTemplates()
    {
        enemyTemplateDict = Enemy.CreateEnemyTemplates();
    }

    public string CreatePvEBattle(Vector3 worldPosition, string teamId, List<string> enemyIds)
    {
        var battle = new Battle();
        
        // Get team from game manager
        var team = gameManager?.Teams.FirstOrDefault(t => t.Id == teamId);
        if (team == null)
        {
            Debug.LogError($"Team {teamId} not found!");
            return null;
        }

        battle.AddTeam(team);

        // Create enemies from templates
        var enemies = new List<Enemy>();
        foreach (string enemyId in enemyIds)
        {
            if (enemyTemplateDict.TryGetValue(enemyId, out Enemy template))
            {
                enemies.Add(template.Clone());
            }
        }
        battle.AddEnemies(enemies);

        // Start the battle
        battle.Start();
        activeBattles[battle.Id] = battle;

        // Start battle UI directly
        StartBattleUI(battle.Id);

        return battle.Id;
    }

    public string CreatePvPBattle(Vector3 worldPosition, List<string> teamIds)
    {
        var battle = new Battle();
        battle.IsPvP = true;

        // Add all teams to battle
        foreach (string teamId in teamIds)
        {
            var team = gameManager?.Teams.FirstOrDefault(t => t.Id == teamId);
            if (team != null)
            {
                battle.AddTeam(team);
            }
        }

        battle.Start();
        activeBattles[battle.Id] = battle;

        // Start battle UI directly
        StartBattleUI(battle.Id);

        return battle.Id;
    }

    private void StartBattleUI(string battleId)
    {
        if (battleUI != null)
        {
            battleUI.StartBattle(battleId);
        }
        else
        {
            Debug.LogError("BattleUI not found! Make sure to assign it in the inspector or have it in the scene.");
        }
    }

    public void EndBattle(string battleId, string winnerTeamId)
    {
        if (!activeBattles.TryGetValue(battleId, out Battle battle))
            return;

        // Find the waypoint where this battle took place
        var waypoints = FindObjectsOfType<WaypointBehaviour>();
        var battleWaypoint = waypoints.FirstOrDefault(w => w.battleId == battleId);
        
        if (battleWaypoint != null)
        {
            battleWaypoint.OnBattleEnded(winnerTeamId);
        }

        // Clean up
        activeBattles.Remove(battleId);

        Debug.Log($"Battle {battleId} ended. Winner: {winnerTeamId ?? "None"}");
    }

    public Battle GetBattle(string battleId)
    {
        activeBattles.TryGetValue(battleId, out Battle battle);
        return battle;
    }
}