using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleSystem
{
    public enum GameState
    {
        Lobby,
        Preparing,
        Playing,
        Finished
    }

    public class GameManager : MonoBehaviour
    {
        public string Id { get; private set; }
        public string RoomCode { get; private set; }
        public GameState State { get; private set; } = GameState.Lobby;
        public List<Team> Teams { get; private set; } = new List<Team>();
        public List<Player> Players { get; private set; } = new List<Player>();
        public MapManager MapManager { get; private set; }
        public Dictionary<string, Battle> ActiveBattles { get; private set; } = new Dictionary<string, Battle>();
        public Dictionary<string, Enemy> EnemyTemplates { get; private set; } = new Dictionary<string, Enemy>();

        public int CurrentTeamTurn { get; private set; } = 0;
        public DateTime GameStartTime { get; private set; }
        public DateTime? GameEndTime { get; private set; }
        
        public event Action<string> OnGameStateChanged;
        public event Action<string, string> OnTeamMoved;
        public event Action<string> OnBattleStarted;
        public event Action<string> OnBattleEnded;

        public GameManager()
        {
            Id = Guid.NewGuid().ToString();
            RoomCode = GenerateRoomCode();
            MapManager = new MapManager();
            EnemyTemplates = Enemy.CreateEnemyTemplates();
        }

        public GameManager(string roomCode) : this()
        {
            RoomCode = roomCode;
        }

        private string GenerateRoomCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new System.Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public GameResponse AddPlayer(string playerName, string playerClass, string teamId = null)
        {
            if (State != GameState.Lobby)
            {
                return new GameResponse { Success = false, Message = "Game has already started" };
            }

            if (Players.Count >= 16) // Max 4 teams * 4 players
            {
                return new GameResponse { Success = false, Message = "Game is full" };
            }

            var playerId = Guid.NewGuid().ToString();
            var player = Player.CreatePlayer(playerId, playerName, playerClass);
            Players.Add(player);

            Team targetTeam = null;
            bool isTeamLeader = false;

            if (!string.IsNullOrEmpty(teamId))
            {
                targetTeam = Teams.FirstOrDefault(t => t.Id == teamId);
            }

            if (targetTeam == null)
            {
                // Create new team
                targetTeam = new Team
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Team {Teams.Count + 1}"
                };
                Teams.Add(targetTeam);
                isTeamLeader = true;
            }

            try
            {
                targetTeam.AddPlayer(player);
            }
            catch (InvalidOperationException ex)
            {
                Players.Remove(player);
                return new GameResponse { Success = false, Message = ex.Message };
            }

            return new GameResponse
            {
                Success = true,
                Message = "Player added successfully",
                Data = new
                {
                    PlayerId = playerId,
                    TeamId = targetTeam.Id,
                    TeamName = targetTeam.Name,
                    IsTeamLeader = isTeamLeader
                }
            };
        }

        public GameResponse SetTeamReady(string teamId, bool ready)
        {
            var team = Teams.FirstOrDefault(t => t.Id == teamId);
            if (team == null)
            {
                return new GameResponse { Success = false, Message = "Team not found" };
            }

            team.IsReady = ready;

            if (Teams.All(t => t.IsReady) && Teams.Count >= 2)
            {
                StartGame();
            }

            return new GameResponse { Success = true, Message = "Team ready status updated" };
        }

        public void StartGame()
        {
            if (State != GameState.Lobby)
                return;

            State = GameState.Preparing;
            GameStartTime = DateTime.Now;

            // Assign teams to spawn positions
            var spawnPositions = new[] { "spawn_1", "spawn_2", "spawn_3", "spawn_4" };
            for (int i = 0; i < Teams.Count && i < spawnPositions.Length; i++)
            {
                var spawnNodeId = MapManager.Nodes.FirstOrDefault(n => n.Name == spawnPositions[i])?.Id;
                if (spawnNodeId != null)
                {
                    MapManager.AssignTeamToNode(Teams[i].Id, spawnNodeId);
                }
            }

            // Generate enemies on the map
            MapManager.GenerateEnemies(EnemyTemplates);

            State = GameState.Playing;
            CurrentTeamTurn = 0;

            OnGameStateChanged?.Invoke("Game started");
        }

        public GameResponse ProcessTeamMovement(string teamId, string targetNodeId)
        {
            if (State != GameState.Playing)
            {
                return new GameResponse { Success = false, Message = "Game is not in progress" };
            }

            var currentTeam = GetCurrentTeam();
            if (currentTeam?.Id != teamId)
            {
                return new GameResponse { Success = false, Message = "Not your team's turn" };
            }

            if (!MapManager.MoveTeam(teamId, targetNodeId))
            {
                return new GameResponse { Success = false, Message = "Invalid movement" };
            }

            OnTeamMoved?.Invoke(teamId, targetNodeId);

            // Check for battle initiation
            if (MapManager.CheckForBattleStart(targetNodeId, out bool isPvP, out var battleTeams, out var enemyTemplateIds))
            {
                var battle = CreateBattle(targetNodeId, isPvP, battleTeams, enemyTemplateIds);
                if (battle != null)
                {
                    ActiveBattles[battle.Id] = battle;
                    MapManager.StartBattleAtNode(targetNodeId, battle.Id);
                    OnBattleStarted?.Invoke(battle.Id);
                }
            }

            NextTeamTurn();

            return new GameResponse { Success = true, Message = "Team moved successfully" };
        }

        private Battle CreateBattle(string nodeId, bool isPvP, List<string> teamIds, List<string> enemyTemplateIds)
        {
            var battle = new Battle
            {
                NodeId = nodeId,
                IsPvP = isPvP
            };

            // Add teams to battle
            foreach (var teamId in teamIds)
            {
                var team = Teams.FirstOrDefault(t => t.Id == teamId);
                if (team != null)
                {
                    battle.AddTeam(team);
                }
            }

            // Add enemies if PvE battle
            if (!isPvP && enemyTemplateIds.Count > 0)
            {
                var enemies = new List<Enemy>();
                foreach (var templateId in enemyTemplateIds)
                {
                    if (EnemyTemplates.TryGetValue(templateId, out var template))
                    {
                        enemies.Add(template.Clone());
                    }
                }
                battle.AddEnemies(enemies);
            }

            battle.Start();
            return battle;
        }

        public GameResponse ProcessBattleAction(string battleId, string playerId, Action action)
        {
            if (!ActiveBattles.TryGetValue(battleId, out var battle))
            {
                return new GameResponse { Success = false, Message = "Battle not found" };
            }

            var result = battle.ProcessAction(playerId, action);

            if (battle.State == BattleState.Finished)
            {
                EndBattle(battleId);
            }

            return new GameResponse
            {
                Success = result.Success,
                Message = result.Message,
                Data = result
            };
        }

        public GameResponse ProcessTeamInvasion(string teamId, string battleId)
        {
            if (!ActiveBattles.TryGetValue(battleId, out var battle))
            {
                return new GameResponse { Success = false, Message = "Battle not found" };
            }

            if (!battle.CanInvade())
            {
                return new GameResponse { Success = false, Message = "Battle cannot be invaded" };
            }

            var invadingTeam = Teams.FirstOrDefault(t => t.Id == teamId);
            if (invadingTeam == null)
            {
                return new GameResponse { Success = false, Message = "Team not found" };
            }

            if (!MapManager.CanTeamInvadeBattle(teamId, battle.NodeId))
            {
                return new GameResponse { Success = false, Message = "Team not adjacent to battle" };
            }

            if (battle.AddInvadingTeam(invadingTeam))
            {
                return new GameResponse { Success = true, Message = "Team successfully invaded battle" };
            }

            return new GameResponse { Success = false, Message = "Failed to invade battle" };
        }

        private void EndBattle(string battleId)
        {
            if (ActiveBattles.TryGetValue(battleId, out var battle))
            {
                if (!string.IsNullOrEmpty(battle.WinnerTeamId))
                {
                    var winnerTeam = Teams.FirstOrDefault(t => t.Id == battle.WinnerTeamId);
                    if (winnerTeam != null)
                    {
                        AwardExperience(winnerTeam, battle);
                    }
                }

                MapManager.EndBattleAtNode(battle.NodeId);
                ActiveBattles.Remove(battleId);
                OnBattleEnded?.Invoke(battleId);

                CheckGameEnd();
            }
        }

        private void AwardExperience(Team team, Battle battle)
        {
            int baseExp = battle.IsPvP ? 100 : 50;
            foreach (var player in team.Players)
            {
                player.GainExperience(baseExp);
            }
        }

        private void NextTeamTurn()
        {
            do
            {
                CurrentTeamTurn = (CurrentTeamTurn + 1) % Teams.Count;
            }
            while (IsTeamInBattle(Teams[CurrentTeamTurn].Id) && Teams.Any(t => !IsTeamInBattle(t.Id)));
        }

        private bool IsTeamInBattle(string teamId)
        {
            return ActiveBattles.Values.Any(b => b.Teams.Any(t => t.Id == teamId));
        }

        private Team GetCurrentTeam()
        {
            return CurrentTeamTurn < Teams.Count ? Teams[CurrentTeamTurn] : null;
        }

        private void CheckGameEnd()
        {
            if (State != GameState.Playing)
                return;

            var aliveTeams = Teams.Where(t => t.IsAlive).ToList();
            if (aliveTeams.Count <= 1)
            {
                State = GameState.Finished;
                GameEndTime = DateTime.Now;
                OnGameStateChanged?.Invoke("Game ended");
            }
        }

        public GameInfo GetGameInfo()
        {
            return new GameInfo
            {
                Id = Id,
                RoomCode = RoomCode,
                State = State,
                TeamsCount = Teams.Count,
                PlayersCount = Players.Count,
                ActiveBattlesCount = ActiveBattles.Count,
                CurrentTeamTurn = CurrentTeamTurn,
                GameDuration = GameEndTime.HasValue ? GameEndTime.Value - GameStartTime : DateTime.Now - GameStartTime
            };
        }
    }

    [Serializable]
    public class GameResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }

    [Serializable]
    public class GameInfo
    {
        public string Id { get; set; }
        public string RoomCode { get; set; }
        public GameState State { get; set; }
        public int TeamsCount { get; set; }
        public int PlayersCount { get; set; }
        public int ActiveBattlesCount { get; set; }
        public int CurrentTeamTurn { get; set; }
        public TimeSpan GameDuration { get; set; }
    }
}