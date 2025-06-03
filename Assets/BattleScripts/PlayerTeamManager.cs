using UnityEngine;
using BattleSystem;
using System.Collections.Generic;

public class PlayerTeamManager : MonoBehaviour
{
    [Header("Team Management")]
    public string currentTeamId;
    public Transform currentTeamMarker; // Visual indicator of current team

    [Header("Movement")]
    public bool isPlayerTurn = true;
    
    private List<Team> teams = new List<Team>();
    private int currentTeamIndex = 0;

    public string GetCurrentTeamId()
    {
        return currentTeamId;
    }

    public void SetCurrentTeam(string teamId)
    {
        currentTeamId = teamId;
        
        // Update visual marker position
        if (currentTeamMarker != null)
        {
            var waypoint = FindCurrentTeamWaypoint();
            if (waypoint != null)
            {
                currentTeamMarker.position = waypoint.transform.position + Vector3.up * 2;
            }
        }
    }

    public void NextTeamTurn()
    {
        currentTeamIndex = (currentTeamIndex + 1) % teams.Count;
        if (currentTeamIndex < teams.Count)
        {
            SetCurrentTeam(teams[currentTeamIndex].Id);
        }
    }

    private WaypointBehaviour FindCurrentTeamWaypoint()
    {
        var waypoints = FindObjectsOfType<WaypointBehaviour>();
        return System.Array.Find(waypoints, w => w.teamsInWaypoint.Contains(currentTeamId));
    }

    public void RegisterTeam(Team team)
    {
        if (!teams.Exists(t => t.Id == team.Id))
        {
            teams.Add(team);
        }
    }
}