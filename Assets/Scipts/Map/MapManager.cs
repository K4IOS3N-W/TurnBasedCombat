using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using BattleSystem.Client; // Add this using directive
using BattleSystem.Server; // Add this if needed for EncounterData

namespace BattleSystem.Map
{
    public class MapManager : MonoBehaviour
    {
        [Header("Map Settings")]
        [SerializeField] private List<Waypoint> allWaypoints = new List<Waypoint>();
        [SerializeField] private Waypoint startWaypoint;
        [SerializeField] private Waypoint goalWaypoint;
        
        [Header("Team Visualization")]
        [SerializeField] private GameObject teamMarkerPrefab;
        [SerializeField] private Color[] teamColors = { Color.blue, Color.red, Color.green, Color.yellow };
        
        [Header("Battle Management")]
        [SerializeField] private bool enableTeamVsTeamBattles = true;
        [SerializeField] private bool enableEnemyEncounters = true;
        
        private Dictionary<string, Waypoint> waypointLookup = new Dictionary<string, Waypoint>();
        private Dictionary<string, GameObject> teamMarkers = new Dictionary<string, GameObject>();
        private BattleSystem.Client.GameClient gameClient; // Use fully qualified type
        private string currentTeamId = "";
        private int currentTurn = 0;
        
        public event System.Action<string> OnWaypointClicked;
        public event System.Action<EnemyEncounter, string> OnEnemyEncounterTriggered;
        public event System.Action<List<string>, string> OnTeamConflictTriggered;
        
        private void Start()
        {
            InitializeMap();
            gameClient = FindObjectOfType<BattleSystem.Client.GameClient>();
            
            if (gameClient != null)
            {
                gameClient.OnTurnChanged += OnTurnChanged;
                gameClient.OnGameStarted += OnGameStarted;
            }
        }
        
        private void InitializeMap()
        {
            // Find all waypoints if not assigned
            if (allWaypoints.Count == 0)
            {
                allWaypoints = FindObjectsOfType<Waypoint>().ToList();
            }
            
            // Build lookup dictionary and setup event handlers
            waypointLookup.Clear();
            foreach (var waypoint in allWaypoints)
            {
                waypointLookup[waypoint.WaypointId] = waypoint;
                waypoint.OnWaypointClicked += OnWaypointClickedInternal;
                waypoint.OnEnemyEncounter += OnEnemyEncounterInternal;
                waypoint.OnTeamConflict += OnTeamConflictInternal;
            }
            
            // Find start and goal if not assigned
            if (startWaypoint == null)
                startWaypoint = allWaypoints.FirstOrDefault(w => w.IsStartPoint);
                
            if (goalWaypoint == null)
                goalWaypoint = allWaypoints.FirstOrDefault(w => w.IsGoal);
            
            // Initially disable all waypoints
            SetAllWaypointsAvailable(false);
        }
        
        private void OnWaypointClickedInternal(Waypoint clickedWaypoint)
        {
            if (gameClient == null) return;
            
            // Check if it's the player's turn and they can move to this waypoint
            string currentPosition = GetTeamPosition(gameClient.TeamId);
            if (CanMoveBetween(currentPosition, clickedWaypoint.WaypointId))
            {
                gameClient.MoveToWaypoint(clickedWaypoint.WaypointId);
            }
        }
        
        private void OnTurnChanged(string currentTeam, bool isMyTurn)
        {
            if (isMyTurn && gameClient != null)
            {
                // Advance turn counter when it becomes our turn
                AdvanceTurn();
                
                // Get current team position and enable available moves
                string currentPosition = GetTeamPosition(gameClient.TeamId);
                var availableMoves = GetConnectedWaypoints(currentPosition);
                SetAvailableMovesForTeam(gameClient.TeamId, currentPosition, availableMoves);
            }
            else
            {
                // Disable all moves when it's not our turn
                SetAllWaypointsAvailable(false);
            }
        }
        
        private void OnGameStarted()
        {
            if (gameClient != null)
            {
                // Initialize team position at start
                UpdateTeamPosition(gameClient.TeamId, "start");
            }
        }
        
        private void OnEnemyEncounterInternal(Waypoint waypoint, EnemyEncounter encounter)
        {
            if (enableEnemyEncounters && waypoint.OccupyingTeams.Contains(gameClient.TeamId))
            {
                Debug.Log($"Enemy encounter at {waypoint.WaypointName}: {encounter.encounterName}");
                OnEnemyEncounterTriggered?.Invoke(encounter, waypoint.WaypointId);
                
                // Notify game client about enemy encounter
                if (gameClient != null)
                {
                    var encounterData = new EncounterData
                    {
                        Type = "Enemy",
                        Enemies = new List<string> { encounter.encounterName },
                        Difficulty = encounter.difficulty
                    };
                    
                    // Trigger battle through a method call instead of direct event invocation
                    TriggerBattle(encounterData);
                }
            }
        }
        
        private void OnTeamConflictInternal(Waypoint waypoint, List<string> conflictingTeams)
        {
            if (enableTeamVsTeamBattles && conflictingTeams.Contains(gameClient.TeamId))
            {
                Debug.Log($"Team conflict at {waypoint.WaypointName}: {string.Join(", ", conflictingTeams)}");
                
                var otherTeams = conflictingTeams.Where(t => t != gameClient.TeamId).ToList();
                OnTeamConflictTriggered?.Invoke(conflictingTeams, waypoint.WaypointId);
                
                // Notify game client about team battle
                if (gameClient != null)
                {
                    var encounterData = new EncounterData
                    {
                        Type = "TeamVsTeam",
                        Enemies = otherTeams,
                        Difficulty = conflictingTeams.Count
                    };
                    
                    // Trigger battle through a method call instead of direct event invocation
                    TriggerBattle(encounterData);
                }
            }
        }
        
        // Add this helper method
        private void TriggerBattle(EncounterData encounterData)
        {
            // Find and notify the battle manager instead of directly invoking the event
            var battleManager = FindObjectOfType<BattleSystem.Battle.BattleManager>();
            if (battleManager != null)
            {
                battleManager.StartBattle(encounterData);
            }
        }
        
        public void UpdateTeamPosition(string teamId, string waypointId)
        {
            // Remove team from all waypoints first
            foreach (var waypoint in allWaypoints)
            {
                waypoint.RemoveTeam(teamId);
            }
            
            // Add team to new waypoint
            if (waypointLookup.TryGetValue(waypointId, out var newWaypoint))
            {
                newWaypoint.AddTeam(teamId);
                
                // Update visual marker
                UpdateTeamMarker(teamId, newWaypoint.transform.position);
            }
        }
        
        private void AdvanceTurn()
        {
            currentTurn++;
            
            // Advance turn for all waypoints (handles cooldowns)
            foreach (var waypoint in allWaypoints)
            {
                waypoint.AdvanceTurn();
            }
        }
        
        public List<string> GetConnectedWaypoints(string waypointId)
        {
            if (waypointLookup.TryGetValue(waypointId, out var waypoint))
            {
                return waypoint.GetConnectedWaypointIds();
            }
            return new List<string>();
        }
        
        public bool CanMoveBetween(string fromWaypointId, string toWaypointId)
        {
            if (waypointLookup.TryGetValue(fromWaypointId, out var fromWaypoint) &&
                waypointLookup.TryGetValue(toWaypointId, out var toWaypoint))
            {
                return fromWaypoint.CanMoveTo(toWaypoint);
            }
            return false;
        }
        
        public int GetTeamBattleCooldown(string waypointId, string team1, string team2)
        {
            if (waypointLookup.TryGetValue(waypointId, out var waypoint))
            {
                return waypoint.GetTeamBattleCooldown(team1, team2);
            }
            return 0;
        }
        
        public List<string> GetTeamsAtWaypoint(string waypointId)
        {
            if (waypointLookup.TryGetValue(waypointId, out var waypoint))
            {
                return waypoint.OccupyingTeams;
            }
            return new List<string>();
        }
        
        public bool HasEnemyEncounter(string waypointId)
        {
            if (waypointLookup.TryGetValue(waypointId, out var waypoint))
            {
                return waypoint.HasEnemyEncounter;
            }
            return false;
        }
        
        private void UpdateTeamMarker(string teamId, Vector3 position)
        {
            if (!teamMarkers.TryGetValue(teamId, out var marker))
            {
                // Create new marker
                if (teamMarkerPrefab != null)
                {
                    marker = Instantiate(teamMarkerPrefab, position, Quaternion.identity);
                    teamMarkers[teamId] = marker;
                    
                    // Set team color
                    var renderer = marker.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        int colorIndex = teamMarkers.Count % teamColors.Length;
                        renderer.color = teamColors[colorIndex];
                    }
                    
                    // Add team name label
                    var textComponent = marker.GetComponentInChildren<TextMeshPro>();
                    if (textComponent != null)
                    {
                        textComponent.text = teamId.Substring(0, System.Math.Min(4, teamId.Length));
                    }
                }
            }
            else
            {
                // Move existing marker
                marker.transform.position = position;
            }
        }
        
        private string GetTeamPosition(string teamId)
        {
            // Find which waypoint the team is currently at
            foreach (var waypoint in allWaypoints)
            {
                if (waypoint.OccupyingTeams.Contains(teamId))
                {
                    return waypoint.WaypointId;
                }
            }
            return startWaypoint?.WaypointId ?? "start";
        }
        
        public Waypoint GetWaypoint(string waypointId)
        {
            waypointLookup.TryGetValue(waypointId, out var waypoint);
            return waypoint;
        }
        
        public Vector3 GetWaypointPosition(string waypointId)
        {
            var waypoint = GetWaypoint(waypointId);
            return waypoint != null ? waypoint.transform.position : Vector3.zero;
        }
        
        // Debug and utility methods
        public void LogMapState()
        {
            Debug.Log($"=== Map State (Turn {currentTurn}) ===");
            foreach (var waypoint in allWaypoints)
            {
                if (waypoint.IsOccupied)
                {
                    Debug.Log($"{waypoint.WaypointName}: Teams [{string.Join(", ", waypoint.OccupyingTeams)}]");
                }
                if (waypoint.HasEnemyEncounter)
                {
                    Debug.Log($"{waypoint.WaypointName}: Enemy Encounter - {waypoint.EnemyEncounter.encounterName}");
                }
            }
        }
        
        [ContextMenu("Auto-Setup Waypoints")]
        public void AutoSetupWaypoints()
        {
            allWaypoints = FindObjectsOfType<Waypoint>().ToList();
            
            foreach (var waypoint in allWaypoints)
            {
                if (waypoint.WaypointId.ToLower().Contains("start"))
                {
                    startWaypoint = waypoint;
                }
                else if (waypoint.WaypointId.ToLower().Contains("goal"))
                {
                    goalWaypoint = waypoint;
                }
            }
        }

        // Add the missing methods
        public void SetAvailableMovesForTeam(string teamId, string currentPosition, List<string> availableMoves)
        {
            // Set all waypoints as unavailable first
            SetAllWaypointsAvailable(false);
            
            // Enable only the available moves
            foreach (string waypointId in availableMoves)
            {
                var waypoint = GetWaypoint(waypointId);
                if (waypoint != null)
                {
                    waypoint.SetAvailable(true);
                }
            }
        }

        public void SetAllWaypointsAvailable(bool available)
        {
            foreach (var waypoint in allWaypoints)
            {
                waypoint.SetAvailable(available);
            }
        }
    }
}