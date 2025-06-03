using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleSystem
{
    [Serializable]
    public class Team
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Player> Players { get; set; } = new List<Player>();
        public TeamStrategy Strategy { get; set; } = TeamStrategy.Balanced;
        public Dictionary<string, object> TeamBuffs { get; set; } = new Dictionary<string, object>();
        public bool IsReady { get; set; } = false;

        // Team statistics
        public int TotalLevel => Players.Sum(p => p.Level);
        public int AverageLevel => Players.Count > 0 ? TotalLevel / Players.Count : 0;
        public int TotalHealth => Players.Sum(p => p.Health);
        public int MaxTotalHealth => Players.Sum(p => p.MaxHealth);
        public int TotalMana => Players.Sum(p => p.Mana);
        public int MaxTotalMana => Players.Sum(p => p.MaxMana);

        public bool HasTank => Players.Any(p => p.Class.ToLower() == "warrior");
        public bool HasHealer => Players.Any(p => p.Class.ToLower() == "healer");
        public bool HasDamageDealer => Players.Any(p => p.Class.ToLower() == "mage");
        public bool IsBalanced => HasTank && HasHealer && HasDamageDealer;

        public bool IsAlive => Players.Any(p => p.IsAlive);
        public bool AllMembersAlive => Players.All(p => p.IsAlive);
        public int AliveMembersCount => Players.Count(p => p.IsAlive);
        public float HealthPercentage => MaxTotalHealth > 0 ? (float)TotalHealth / MaxTotalHealth : 0f;

        public void AddPlayer(Player player)
        {
            if (Players.Count >= 4)
            {
                throw new InvalidOperationException("Team is full (maximum 4 players)");
            }

            if (Players.Any(p => p.Id == player.Id))
            {
                throw new InvalidOperationException("Player is already in this team");
            }

            Players.Add(player);
            ApplyTeamBuffs(player);
        }

        public bool RemovePlayer(string playerId)
        {
            var player = Players.FirstOrDefault(p => p.Id == playerId);
            if (player != null)
            {
                RemoveTeamBuffs(player);
                Players.Remove(player);
                return true;
            }
            return false;
        }

        public Player GetPlayer(string playerId)
        {
            return Players.FirstOrDefault(p => p.Id == playerId);
        }

        public List<Player> GetPlayersByClass(string className)
        {
            return Players.Where(p => p.Class.ToLower() == className.ToLower()).ToList();
        }

        private void ApplyTeamBuffs(Player player)
        {
            switch (Strategy)
            {
                case TeamStrategy.Aggressive:
                    ApplyAggressiveBuff(player);
                    break;
                case TeamStrategy.Defensive:
                    ApplyDefensiveBuff(player);
                    break;
                case TeamStrategy.Supportive:
                    ApplySupportiveBuff(player);
                    break;
                case TeamStrategy.Balanced:
                    ApplyBalancedBuff(player);
                    break;
            }
            ApplySynergyBonuses(player);
        }

        private void ApplyAggressiveBuff(Player player)
        {
            if (TeamBuffs.ContainsKey("AggressiveBuff"))
            {
                player.Attack += 5;
                player.Speed += 2;
            }
        }

        private void ApplyDefensiveBuff(Player player)
        {
            if (TeamBuffs.ContainsKey("DefensiveBuff"))
            {
                player.Defense += 5;
                player.MaxHealth += 20;
                player.Health += 20;
            }
        }

        private void ApplySupportiveBuff(Player player)
        {
            if (TeamBuffs.ContainsKey("SupportiveBuff"))
            {
                player.MaxMana += 10;
                player.Mana += 10;
            }
        }

        private void ApplyBalancedBuff(Player player)
        {
            if (TeamBuffs.ContainsKey("BalancedBuff"))
            {
                player.Attack += 2;
                player.Defense += 2;
                player.Speed += 1;
            }
        }

        private void ApplySynergyBonuses(Player player)
        {
            if (IsBalanced && !TeamBuffs.ContainsKey("SynergyBonus"))
            {
                TeamBuffs["SynergyBonus"] = true;
                foreach (var p in Players)
                {
                    p.Attack += 3;
                    p.Defense += 3;
                }
            }
        }

        private void RemoveTeamBuffs(Player player)
        {
            // Remove team buffs when player leaves
            switch (Strategy)
            {
                case TeamStrategy.Aggressive:
                    player.Attack -= 5;
                    player.Speed -= 2;
                    break;
                case TeamStrategy.Defensive:
                    player.Defense -= 5;
                    player.MaxHealth -= 20;
                    if (player.Health > player.MaxHealth)
                        player.Health = player.MaxHealth;
                    break;
                case TeamStrategy.Supportive:
                    player.MaxMana -= 10;
                    if (player.Mana > player.MaxMana)
                        player.Mana = player.MaxMana;
                    break;
                case TeamStrategy.Balanced:
                    player.Attack -= 2;
                    player.Defense -= 2;
                    player.Speed -= 1;
                    break;
            }
        }

        public void ChangeStrategy(TeamStrategy newStrategy)
        {
            if (Strategy == newStrategy) return;

            // Remove old buffs
            foreach (var player in Players)
            {
                RemoveTeamBuffs(player);
            }

            Strategy = newStrategy;

            // Apply new buffs
            foreach (var player in Players)
            {
                ApplyTeamBuffs(player);
            }
        }

        public TeamStats GetStats()
        {
            return new TeamStats
            {
                TeamId = Id,
                TeamName = Name,
                PlayerCount = Players.Count,
                TotalLevel = TotalLevel,
                AverageLevel = AverageLevel,
                HealthPercentage = HealthPercentage,
                IsBalanced = IsBalanced,
                Strategy = Strategy,
                IsReady = IsReady
            };
        }

        public List<Player> GetTurnOrder()
        {
            return Players.Where(p => p.IsAlive).OrderByDescending(p => p.GetModifiedSpeed()).ToList();
        }
    }

    public enum TeamStrategy
    {
        Aggressive,
        Defensive,
        Supportive,
        Balanced
    }

    [Serializable]
    public class TeamStats
    {
        public string TeamId { get; set; }
        public string TeamName { get; set; }
        public int PlayerCount { get; set; }
        public int TotalLevel { get; set; }
        public int AverageLevel { get; set; }
        public float HealthPercentage { get; set; }
        public bool IsBalanced { get; set; }
        public TeamStrategy Strategy { get; set; }
        public bool IsReady { get; set; }
    }
}