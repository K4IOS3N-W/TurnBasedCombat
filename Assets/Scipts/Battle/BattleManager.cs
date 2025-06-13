using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BattleSystem.Client;
using BattleSystem.Server;

namespace BattleSystem.Battle
{
    public enum BattleType
    {
        PlayerVsEnemy,
        TeamVsTeam,
        PlayerVsAI
    }
    
    public class BattleManager : MonoBehaviour
    {
        [Header("Battle Settings")]
        [SerializeField] private BattleType currentBattleType;
        [SerializeField] private float turnTimeLimit = 30f;
        [SerializeField] private bool enableAI = true;
        
        [Header("AI Settings")]
        [SerializeField] private float aiThinkTime = 2f;
        [SerializeField] private int aiDifficulty = 1;
        
        private EncounterData currentEncounter;
        private List<string> turnOrder = new List<string>();
        private int currentTurnIndex = 0;
        private bool battleActive = false;
        private Dictionary<string, int> participantHealth = new Dictionary<string, int>();
        private Dictionary<string, bool> isAI = new Dictionary<string, bool>();
        
        private BattleSystem.Client.GameClient gameClient;
        
        public event System.Action<BattleType, EncounterData> OnBattleStarted;
        public event System.Action<string, bool> OnBattleEnded;
        public event System.Action<string, bool> OnTurnChanged;
        public event System.Action<BattleActionResult> OnActionPerformed;
        
        private void Start()
        {
            gameClient = FindObjectOfType<BattleSystem.Client.GameClient>();
            var mapManager = FindObjectOfType<BattleSystem.Map.MapManager>();
            
            if (gameClient != null)
            {
                gameClient.OnBattleStarted += OnBattleStartedFromClient;
                gameClient.OnBattleActionResult += OnBattleActionFromClient;
            }
            
            // Listen to map manager events
            if (mapManager != null)
            {
                mapManager.OnEnemyEncounterTriggered += (encounter, waypointId) => {
                    var encounterData = new EncounterData
                    {
                        Type = "Enemy",
                        Enemies = new List<string> { encounter.encounterName },
                        Difficulty = encounter.difficulty
                    };
                    StartBattle(encounterData);
                };
                
                mapManager.OnTeamConflictTriggered += (conflictingTeams, waypointId) => {
                    var encounterData = new EncounterData
                    {
                        Type = "TeamVsTeam",
                        Enemies = conflictingTeams.Where(t => t != gameClient.TeamId).ToList(),
                        Difficulty = conflictingTeams.Count
                    };
                    StartBattle(encounterData);
                };
            }
        }

        private void OnBattleStartedFromClient(EncounterData encounter)
        {
            StartBattle(encounter);
        }

        private void OnBattleActionFromClient(BattleActionResult result)
        {
            OnActionPerformed?.Invoke(result);
        }
        
        public void StartBattle(EncounterData encounter)
        {
            currentEncounter = encounter;
            battleActive = true;
            
            // Determine battle type
            if (encounter.Type == "TeamVsTeam")
            {
                currentBattleType = BattleType.TeamVsTeam;
                SetupTeamVsTeamBattle(encounter);
            }
            else if (encounter.Type == "Enemy")
            {
                currentBattleType = BattleType.PlayerVsEnemy;
                SetupPlayerVsEnemyBattle(encounter);
            }
            
            OnBattleStarted?.Invoke(currentBattleType, encounter);
            StartFirstTurn();
        }
        
        private void SetupPlayerVsEnemyBattle(EncounterData encounter)
        {
            currentEncounter = encounter;
            turnOrder.Clear();
            participantHealth.Clear();
            isAI.Clear();
            
            // Add player team
            if (gameClient != null)
            {
                string playerTeamId = gameClient.TeamId;
                turnOrder.Add(playerTeamId);
                participantHealth[playerTeamId] = gameClient.PlayerStats.Health;
                isAI[playerTeamId] = false;
            }
            
            // Add enemies from encounter
            for (int i = 0; i < encounter.Enemies.Count; i++)
            {
                string enemyId = encounter.Enemies[i];
                turnOrder.Add(enemyId);
                participantHealth[enemyId] = 100; // Default enemy health
                isAI[enemyId] = true;
            }
            
            ShuffleTurnOrder();
            StartFirstTurn();
        }
        
        private void SetupTeamVsTeamBattle(EncounterData encounter)
        {
            currentEncounter = encounter;
            turnOrder.Clear();
            participantHealth.Clear();
            isAI.Clear();
            
            // Add player team
            if (gameClient != null)
            {
                string playerTeamId = gameClient.TeamId;
                turnOrder.Add(playerTeamId);
                participantHealth[playerTeamId] = gameClient.PlayerStats.Health;
                isAI[playerTeamId] = false;
            }
            
            // Add enemy teams
            foreach (string enemyTeam in encounter.Enemies)
            {
                turnOrder.Add(enemyTeam);
                participantHealth[enemyTeam] = 100; // Default team health
                isAI[enemyTeam] = DetermineIfTeamIsAI(enemyTeam);
            }
            
            ShuffleTurnOrder();
            StartFirstTurn();
        }
        
        private bool DetermineIfTeamIsAI(string teamId)
        {
            // For now, assume other teams are AI if AI is enabled
            // In a real implementation, this would check if the team has human players
            return enableAI;
        }
        
        private void ShuffleTurnOrder()
        {
            // Simple speed-based ordering with randomness
            turnOrder = turnOrder.OrderBy(id => 
            {
                int baseSpeed = UnityEngine.Random.Range(1, 10);
                if (isAI.ContainsKey(id) && isAI[id])
                {
                    baseSpeed += aiDifficulty; // AI gets speed bonus based on difficulty
                }
                return baseSpeed;
            }).ToList();
        }
        
        private void StartFirstTurn()
        {
            currentTurnIndex = 0;
            ProcessCurrentTurn();
        }
        
        private void ProcessCurrentTurn()
        {
            if (currentTurnIndex >= turnOrder.Count) return;
            
            string currentParticipant = turnOrder[currentTurnIndex];
            bool isCurrentTurnAI = isAI.GetValueOrDefault(currentParticipant, false);
            
            OnTurnChanged?.Invoke(currentParticipant, !isCurrentTurnAI);
            
            if (isCurrentTurnAI && enableAI)
            {
                ProcessAITurn();
            }
            else if (gameClient != null && currentParticipant == gameClient.TeamId)
            {
                // Player's turn - wait for input
                Debug.Log("Your turn! Choose an action.");
            }
        }
        
        private void ProcessAITurn()
        {
            if (!battleActive) return;
            
            string currentAI = turnOrder[currentTurnIndex];
            BattleActionData aiAction = GenerateAIAction(currentAI);
            
            if (aiAction != null)
            {
                ProcessBattleAction(currentAI, aiAction);
            }
            else
            {
                // AI skips turn
                NextTurn();
            }
        }
        
        private BattleActionData GenerateAIAction(string aiId)
        {
            // Simple AI logic
            var possibleTargets = GetPossibleTargets(aiId);
            if (possibleTargets.Count == 0) return null;
            
            string targetId = possibleTargets[UnityEngine.Random.Range(0, possibleTargets.Count)];
            string actionType = UnityEngine.Random.Range(0f, 1f) < 0.7f ? "Attack" : "Skill";
            
            return new BattleActionData
            {
                Type = actionType,
                TargetId = targetId
            };
        }
        
        private List<string> GetPossibleTargets(string attackerId)
        {
            var targets = new List<string>();
            
            foreach (string participantId in participantHealth.Keys)
            {
                if (participantId != attackerId && participantHealth[participantId] > 0)
                {
                    // Check if this is a valid target based on battle type
                    if (currentBattleType == BattleType.PlayerVsEnemy)
                    {
                        // Enemies target players, players target enemies
                        bool attackerIsAI = isAI.ContainsKey(attackerId) && isAI[attackerId];
                        bool targetIsAI = isAI.ContainsKey(participantId) && isAI[participantId];
                        
                        if (attackerIsAI != targetIsAI)
                        {
                            targets.Add(participantId);
                        }
                    }
                    else if (currentBattleType == BattleType.TeamVsTeam)
                    {
                        // Teams target other teams
                        if (participantId != attackerId)
                        {
                            targets.Add(participantId);
                        }
                    }
                }
            }
            
            return targets;
        }
        
        public void ProcessBattleAction(string actorId, BattleActionData action)
        {
            if (!battleActive) return;
            
            // Cancel any pending turn timer
            CancelInvoke(nameof(ForceNextTurn));
            
            var result = CalculateBattleResult(actorId, action);
            
            // Apply damage/effects
            if (result.Damage > 0 && !string.IsNullOrEmpty(action.TargetId))
            {
                if (participantHealth.ContainsKey(action.TargetId))
                {
                    participantHealth[action.TargetId] = Mathf.Max(0, 
                        participantHealth[action.TargetId] - result.Damage);
                }
            }
            
            OnActionPerformed?.Invoke(result);
            
            // Check for battle end conditions
            if (CheckBattleEndConditions())
            {
                EndBattle();
            }
            else
            {
                NextTurn();
            }
        }
        
        private BattleActionResult CalculateBattleResult(string actorId, BattleActionData action)
        {
            var result = new BattleActionResult { Success = true };
            
            switch (action.Type.ToLower())
            {
                case "attack":
                    result.Damage = UnityEngine.Random.Range(10, 20);
                    result.Message = $"{actorId} attacks for {result.Damage} damage!";
                    break;
                    
                case "skill":
                    result.Damage = UnityEngine.Random.Range(15, 25);
                    result.Message = $"{actorId} uses a skill for {result.Damage} damage!";
                    break;
                    
                case "defend":
                    result.Message = $"{actorId} defends!";
                    // Could add defense buff logic here
                    break;
                    
                default:
                    result.Success = false;
                    result.Message = "Unknown action";
                    break;
            }
            
            return result;
        }
        
        private bool CheckBattleEndConditions()
        {
            if (currentBattleType == BattleType.PlayerVsEnemy)
            {
                // Check if all enemies are defeated
                bool allEnemiesDefeated = true;
                bool allPlayersDefeated = true;
                
                foreach (var kvp in participantHealth)
                {
                    if (kvp.Value > 0)
                    {
                        if (isAI.ContainsKey(kvp.Key) && isAI[kvp.Key])
                        {
                            allEnemiesDefeated = false;
                        }
                        else
                        {
                            allPlayersDefeated = false;
                        }
                    }
                }
                
                return allEnemiesDefeated || allPlayersDefeated;
            }
            else if (currentBattleType == BattleType.TeamVsTeam)
            {
                // Check if only one team remains
                int teamsAlive = participantHealth.Values.Count(health => health > 0);
                return teamsAlive <= 1;
            }
            
            return false;
        }
        
        private void EndBattle()
        {
            battleActive = false;
            CancelInvoke();
            
            bool victory = DetermineVictory();
            OnBattleEnded?.Invoke(turnOrder[currentTurnIndex], victory);
        }
        
        private bool DetermineVictory()
        {
            if (gameClient == null) return false;
            
            string playerId = gameClient.TeamId;
            return participantHealth.ContainsKey(playerId) && participantHealth[playerId] > 0;
        }
        
        private void NextTurn()
        {
            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
            
            // Skip defeated participants
            int safety = 0;
            while (safety < turnOrder.Count && 
                   participantHealth[turnOrder[currentTurnIndex]] <= 0)
            {
                currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
                safety++;
            }
            
            ProcessCurrentTurn();
        }
        
        private void ForceNextTurn()
        {
            // Player took too long, skip their turn
            NextTurn();
        }
        
        private int GetEnemyHealth(string enemyType)
        {
            return enemyType.ToLower() switch
            {
                "goblin" => 30,
                "orc" => 50,
                "skeleton" => 40,
                "dragon" => 200,
                _ => 25
            };
        }
        
        // Public methods for external access
        public bool IsBattleActive() => battleActive;
        public BattleType GetCurrentBattleType() => currentBattleType;
        public Dictionary<string, int> GetParticipantHealth() => new Dictionary<string, int>(participantHealth);
        public string GetCurrentTurnParticipant() => 
            battleActive && turnOrder.Count > 0 ? turnOrder[currentTurnIndex] : "";
    }
}