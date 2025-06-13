using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSystem.Server
{
    public enum GameState
    {
        Lobby,
        Playing,
        InBattle,
        Finished
    }
    
    public class GameRoom
    {
        public string RoomCode { get; private set; }
        public GameState State { get; private set; } = GameState.Lobby;
        public List<ClientHandler> Players { get; private set; } = new List<ClientHandler>();
        public Dictionary<string, GameTeam> Teams { get; private set; } = new Dictionary<string, GameTeam>();
        public string CurrentTurnTeam { get; private set; }
        public Dictionary<string, PlayerStats> PlayerStats { get; private set; } = new Dictionary<string, PlayerStats>();
        
        private int turnIndex = 0;
        private readonly string goalWaypoint = "goal";
        private DateTime lastActionTime = DateTime.Now;
        
        public GameRoom(string roomCode)
        {
            RoomCode = roomCode;
        }
        
        public bool AddPlayer(ClientHandler player)
        {
            if (Players.Count >= 8) return false;
            
            Players.Add(player);
            PlayerStats[player.Id] = new PlayerStats { PlayerId = player.Id };
            return true;
        }
        
        public void RemovePlayer(ClientHandler player)
        {
            Players.RemoveAll(p => p.Id == player.Id);
            PlayerStats.Remove(player.Id);
            
            // Remove from teams
            foreach (var team in Teams.Values.ToList())
            {
                team.Members.RemoveAll(m => m.Id == player.Id);
                if (team.Members.Count == 0)
                {
                    Teams.Remove(team.Id);
                }
            }
        }
        
        public string CreateTeam(string teamName, ClientHandler leader)
        {
            if (Teams.Count >= 4) return null;
            
            string teamId = Guid.NewGuid().ToString().Substring(0, 8);
            var team = new GameTeam
            {
                Id = teamId,
                Name = teamName,
                Members = new List<ClientHandler> { leader }
            };
            
            leader.TeamId = teamId;
            leader.IsTeamLeader = true;
            Teams[teamId] = team;
            
            return teamId;
        }
        
        public bool JoinTeam(string teamId, ClientHandler player)
        {
            if (!Teams.TryGetValue(teamId, out var team)) return false;
            if (team.Members.Count >= 4) return false;
            
            team.Members.Add(player);
            player.TeamId = teamId;
            player.IsTeamLeader = false;
            
            return true;
        }
        
        public void SetPlayerClass(string playerId, string playerClass)
        {
            if (PlayerStats.TryGetValue(playerId, out var stats))
            {
                stats.UpdateForClass(playerClass);
            }
        }
        
        public bool SetTeamReady(string teamId, bool isReady)
        {
            if (Teams.TryGetValue(teamId, out var team))
            {
                team.IsReady = isReady;
                return true;
            }
            return false;
        }
        
        public bool AllTeamsReady()
        {
            return Teams.Count >= 2 && Teams.Values.All(t => t.IsReady);
        }
        
        public bool CanStartGame()
        {
            return Teams.Count >= 2 && Teams.Values.All(t => t.IsReady);
        }
        
        public void StartGame()
        {
            if (!CanStartGame()) return;
            
            State = GameState.Playing;
            lastActionTime = DateTime.Now;
            
            // Position teams at start
            foreach (var team in Teams.Values)
            {
                team.Position = "start";
            }
            
            // Set first team's turn randomly
            var teamIds = Teams.Keys.ToList();
            if (teamIds.Count > 0)
            {
                var random = new System.Random();
                turnIndex = random.Next(teamIds.Count);
                CurrentTurnTeam = teamIds[turnIndex];
            }
        }
        
        public MoveResult MoveTeam(string teamId, string targetWaypoint)
        {
            if (State != GameState.Playing)
                return new MoveResult { Success = false, Message = "Game not in progress" };
                
            if (teamId != CurrentTurnTeam)
                return new MoveResult { Success = false, Message = "Not your turn" };
                
            if (!Teams.TryGetValue(teamId, out var team))
                return new MoveResult { Success = false, Message = "Team not found" };
            
            team.Position = targetWaypoint;
            lastActionTime = DateTime.Now;
            
            // Check for victory
            if (targetWaypoint == goalWaypoint)
            {
                State = GameState.Finished;
                return new MoveResult { Success = true, Message = "Team won!", IsVictory = true };
            }
            
            // Next team's turn
            NextTurn();
            
            return new MoveResult { Success = true, Message = "Move successful" };
        }
        
        public EncounterData CheckEncounter(string waypointId)
        {
            // Simple encounter check - could be expanded
            if (waypointId.Contains("enemy") || waypointId.Contains("danger"))
            {
                return new EncounterData
                {
                    Type = "Enemy",
                    Enemies = new List<string> { "Goblin", "Orc" },
                    Difficulty = 1
                };
            }
            return null;
        }
        
        public BattleActionResult ProcessBattleAction(string playerId, BattleActionData action)
        {
            // Simple battle action processing
            return new BattleActionResult
            {
                Success = true,
                Message = $"Player {playerId} performed {action.Type}",
                Damage = UnityEngine.Random.Range(10, 20)
            };
        }
        
        public void StartBattle(EncounterData encounter)
        {
            State = GameState.InBattle;
        }
        
        public object GetMapState()
        {
            return new
            {
                Teams = Teams.Values.Select(t => new { t.Id, t.Position }).ToList(),
                CurrentTurn = CurrentTurnTeam
            };
        }
        
        private void NextTurn()
        {
            var teamIds = Teams.Keys.ToList();
            if (teamIds.Count > 0)
            {
                turnIndex = (turnIndex + 1) % teamIds.Count;
                CurrentTurnTeam = teamIds[turnIndex];
                lastActionTime = DateTime.Now;
            }
        }
    }
}
