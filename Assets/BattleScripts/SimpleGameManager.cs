using UnityEngine;
using System.Collections.Generic;
using BattleSystem;

public class SimpleGameManager : MonoBehaviour
{
    [Header("Default Team Setup")]
    public string defaultTeamName = "Player Team";
    
    private List<Team> teams = new List<Team>();
    
    public List<Team> Teams => teams;

    void Start()
    {
        CreateDefaultTeam();
    }

    private void CreateDefaultTeam()
    {
        var team = new Team
        {
            Id = System.Guid.NewGuid().ToString(),
            Name = defaultTeamName
        };

        // Create sample players
        var warrior = Player.CreatePlayer("player1", "Hero Warrior", "Warrior");
        var mage = Player.CreatePlayer("player2", "Hero Mage", "Mage");
        var healer = Player.CreatePlayer("player3", "Hero Healer", "Healer");

        team.AddPlayer(warrior);
        team.AddPlayer(mage);
        team.AddPlayer(healer);

        teams.Add(team);
        
        Debug.Log($"Created default team '{defaultTeamName}' with {team.Players.Count} players");
    }

    public Team GetDefaultTeam()
    {
        return teams.Count > 0 ? teams[0] : null;
    }
}