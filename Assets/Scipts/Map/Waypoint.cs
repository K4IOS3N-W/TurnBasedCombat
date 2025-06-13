using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using BattleSystem.Server; // Add this using statement

namespace BattleSystem.Map
{
    [System.Serializable]
    public class EnemyEncounter
    {
        public string encounterName;
        public List<string> enemyTypes = new List<string>();
        public int difficulty = 1;
        public bool isActive = true;
        public int cooldownTurns = 0;
    }
    
    public class Waypoint : MonoBehaviour
    {
        [Header("Waypoint Settings")]
        [SerializeField] private string waypointId;
        [SerializeField] private string waypointName;
        [SerializeField] private List<Waypoint> connectedWaypoints = new List<Waypoint>();
        [SerializeField] private bool isGoal = false;
        [SerializeField] private bool isStartPoint = false;
        
        [Header("Enemy Encounters")]
        [SerializeField] private EnemyEncounter enemyEncounter;
        [SerializeField] private bool hasEnemyEncounter = false;
        
        [Header("Team vs Team Combat")]
        [SerializeField] private int teamBattleCooldown = 3; // 3 turns cooldown
        private Dictionary<string, int> teamBattleCooldowns = new Dictionary<string, int>();
        
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer waypointSprite;
        [SerializeField] private GameObject nameLabel;
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private LineRenderer[] pathLines;
        [SerializeField] private GameObject teamIndicator;
        [SerializeField] private GameObject enemyIndicator;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color availableColor = Color.green;
        [SerializeField] private Color occupiedColor = Color.red;
        [SerializeField] private Color goalColor = Color.yellow; // Changed from Color.gold
        [SerializeField] private Color encounterColor = new Color(0.5f, 0f, 0f); // Dark red
        [SerializeField] private Color multiTeamColor = new Color(1f, 0.5f, 0f); // Orange
        
        [Header("Interaction")]
        [SerializeField] private Button waypointButton;
        [SerializeField] private Collider2D waypointCollider;
        
        private bool isAvailable = false;
        private List<string> occupyingTeams = new List<string>();
        private int currentTurn = 0;
        
        public string WaypointId => waypointId;
        public string WaypointName => waypointName;
        public List<Waypoint> ConnectedWaypoints => connectedWaypoints;
        public bool HasEnemyEncounter => hasEnemyEncounter && enemyEncounter?.isActive == true;
        public EnemyEncounter EnemyEncounter => enemyEncounter;
        public bool IsGoal => isGoal;
        public bool IsStartPoint => isStartPoint;
        public bool IsAvailable => isAvailable;
        public bool IsOccupied => occupyingTeams.Count > 0;
        public bool HasMultipleTeams => occupyingTeams.Count > 1;
        public List<string> OccupyingTeams => new List<string>(occupyingTeams);
        
        public event System.Action<Waypoint> OnWaypointClicked;
        public event System.Action<Waypoint, string> OnTeamEntered;
        public event System.Action<Waypoint, List<string>> OnTeamConflict;
        public event System.Action<Waypoint, EnemyEncounter> OnEnemyEncounter;
        
        private void Start()
        {
            InitializeWaypoint();
        }
        
        private void InitializeWaypoint()
        {
            if (string.IsNullOrEmpty(waypointId))
                waypointId = gameObject.name;
                
            if (string.IsNullOrEmpty(waypointName))
                waypointName = waypointId;
            
            // Setup visual components
            if (nameText != null)
                nameText.text = waypointName;
            
            // Setup button interaction
            if (waypointButton != null)
            {
                waypointButton.onClick.AddListener(() => OnWaypointClicked?.Invoke(this));
            }
            else if (waypointCollider != null)
            {
                waypointCollider.enabled = true;
            }
            
            // Draw path lines to connected waypoints
            DrawPathLines();
            
            // Set initial color
            UpdateVisuals();
        }
        
        private void DrawPathLines()
        {
            if (pathLines == null || pathLines.Length == 0) return;
            
            for (int i = 0; i < pathLines.Length && i < connectedWaypoints.Count; i++)
            {
                if (pathLines[i] != null && connectedWaypoints[i] != null)
                {
                    pathLines[i].positionCount = 2;
                    pathLines[i].SetPosition(0, transform.position);
                    pathLines[i].SetPosition(1, connectedWaypoints[i].transform.position);
                    pathLines[i].startWidth = 0.1f;
                    pathLines[i].endWidth = 0.1f;
                    pathLines[i].material.color = Color.gray; // Changed from pathLines[i].color
                }
            }
        }
        
        public void SetAvailable(bool available)
        {
            isAvailable = available;
            UpdateVisuals();
            
            if (waypointButton != null)
                waypointButton.interactable = available;
        }
        
        public bool AddTeam(string teamId)
        {
            if (occupyingTeams.Contains(teamId)) return false;
            
            occupyingTeams.Add(teamId);
            UpdateVisuals();
            
            OnTeamEntered?.Invoke(this, teamId);
            
            // Check for encounters
            CheckForEncounters(teamId);
            
            return true;
        }
        
        public bool RemoveTeam(string teamId)
        {
            bool removed = occupyingTeams.Remove(teamId);
            if (removed)
            {
                UpdateVisuals();
            }
            return removed;
        }
        
        private void CheckForEncounters(string enteringTeamId)
        {
            // Check for enemy encounter
            if (HasEnemyEncounter && enemyEncounter.cooldownTurns <= 0)
            {
                OnEnemyEncounter?.Invoke(this, enemyEncounter);
                StartEnemyEncounterCooldown();
                return;
            }
            
            // Check for team vs team conflict
            if (occupyingTeams.Count > 1)
            {
                CheckForTeamConflict(enteringTeamId);
            }
        }
        
        private void CheckForTeamConflict(string enteringTeamId)
        {
            var conflictingTeams = new List<string>();
            
            foreach (string teamId in occupyingTeams)
            {
                if (teamId != enteringTeamId && CanTeamsBattle(enteringTeamId, teamId))
                {
                    conflictingTeams.Add(teamId);
                }
            }
            
            if (conflictingTeams.Count > 0)
            {
                conflictingTeams.Add(enteringTeamId);
                OnTeamConflict?.Invoke(this, conflictingTeams);
                StartTeamBattleCooldown(conflictingTeams);
            }
        }
        
        private bool CanTeamsBattle(string team1, string team2)
        {
            string cooldownKey = GetTeamPairKey(team1, team2);
            return !teamBattleCooldowns.ContainsKey(cooldownKey) || teamBattleCooldowns[cooldownKey] <= 0;
        }
        
        private void StartTeamBattleCooldown(List<string> teams)
        {
            for (int i = 0; i < teams.Count; i++)
            {
                for (int j = i + 1; j < teams.Count; j++)
                {
                    string cooldownKey = GetTeamPairKey(teams[i], teams[j]);
                    teamBattleCooldowns[cooldownKey] = teamBattleCooldown;
                }
            }
        }
        
        private void StartEnemyEncounterCooldown()
        {
            if (enemyEncounter != null)
            {
                enemyEncounter.cooldownTurns = 2; // 2 turn cooldown for enemy encounters
            }
        }
        
        private string GetTeamPairKey(string team1, string team2)
        {
            // Create a consistent key regardless of order
            var teams = new[] { team1, team2 };
            System.Array.Sort(teams);
            return $"{teams[0]}_{teams[1]}";
        }
        
        public void AdvanceTurn()
        {
            currentTurn++;
            
            // Reduce enemy encounter cooldown
            if (enemyEncounter != null && enemyEncounter.cooldownTurns > 0)
            {
                enemyEncounter.cooldownTurns--;
            }
            
            // Reduce team battle cooldowns
            var keysToRemove = new List<string>();
            var keysToUpdate = new List<string>(teamBattleCooldowns.Keys);
            
            foreach (string key in keysToUpdate)
            {
                teamBattleCooldowns[key]--;
                if (teamBattleCooldowns[key] <= 0)
                {
                    keysToRemove.Add(key);
                }
            }
            
            foreach (string key in keysToRemove)
            {
                teamBattleCooldowns.Remove(key);
            }
            
            UpdateVisuals();
        }
        
        public bool CanMoveTo(Waypoint targetWaypoint)
        {
            return connectedWaypoints.Contains(targetWaypoint);
        }
        
        public List<string> GetConnectedWaypointIds()
        {
            var ids = new List<string>();
            foreach (var waypoint in connectedWaypoints)
            {
                if (waypoint != null)
                    ids.Add(waypoint.WaypointId);
            }
            return ids;
        }
        
        private void UpdateVisuals()
        {
            if (waypointSprite == null) return;
            
            Color targetColor = normalColor;
            
            if (isGoal)
                targetColor = goalColor;
            else if (occupyingTeams.Count > 1)
                targetColor = multiTeamColor;
            else if (IsOccupied)
                targetColor = occupiedColor;
            else if (HasEnemyEncounter)
                targetColor = encounterColor;
            else if (isAvailable)
                targetColor = availableColor;
            
            waypointSprite.color = targetColor;
            
            // Show/hide indicators
            if (teamIndicator != null)
                teamIndicator.SetActive(IsOccupied);
                
            if (enemyIndicator != null)
                enemyIndicator.SetActive(HasEnemyEncounter);
        }
        
        private void OnMouseDown()
        {
            if (waypointButton == null && isAvailable)
            {
                OnWaypointClicked?.Invoke(this);
            }
        }
        
        public int GetTeamBattleCooldown(string team1, string team2)
        {
            string key = GetTeamPairKey(team1, team2);
            return teamBattleCooldowns.ContainsKey(key) ? teamBattleCooldowns[key] : 0;
        }
        
        // Editor helper methods
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(waypointId))
                waypointId = gameObject.name;
                
            if (nameText != null && !string.IsNullOrEmpty(waypointName))
                nameText.text = waypointName;
        }
        
        public void AddConnection(Waypoint waypoint)
        {
            if (waypoint != null && !connectedWaypoints.Contains(waypoint))
            {
                connectedWaypoints.Add(waypoint);
                if (!waypoint.connectedWaypoints.Contains(this))
                    waypoint.connectedWaypoints.Add(this);
            }
        }
        
        public void RemoveConnection(Waypoint waypoint)
        {
            if (waypoint != null)
            {
                connectedWaypoints.Remove(waypoint);
                waypoint.connectedWaypoints.Remove(this);
            }
        }
    }
}