using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleSystem
{
    [Serializable]
    public class Player : Character
    {
        public string Class { get; set; }
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int Mana { get; set; }
        public int MaxMana { get; set; }
        public List<Skill> Skills { get; set; } = new List<Skill>();
        public PlayerStats BaseStats { get; set; } = new PlayerStats();

        // Session statistics
        public DateTime SessionStartTime { get; set; } = DateTime.Now;
        public int SessionBattlesWon { get; set; } = 0;
        public int SessionBattlesLost { get; set; } = 0;
        public int SessionDamageDealt { get; set; } = 0;
        public int SessionHealingDone { get; set; } = 0;

        public int ExperienceToNextLevel => CalculateExpToNextLevel(Level);
        public TimeSpan SessionDuration => DateTime.Now - SessionStartTime;

        // Calculate experience required for next level with progressive scaling
        private int CalculateExpToNextLevel(int level)
        {
            return 50 + (level * 50);
        }

        public ClassMasteryInfo GetClassMastery()
        {
            return new ClassMasteryInfo
            {
                Class = Class,
                Level = Level,
                Experience = Experience,
                ExperienceToNextLevel = ExperienceToNextLevel,
                SessionDuration = SessionDuration,
                BattlesWon = SessionBattlesWon,
                BattlesLost = SessionBattlesLost,
                WinRate = SessionBattlesWon + SessionBattlesLost > 0 ?
                    (float)SessionBattlesWon / (SessionBattlesWon + SessionBattlesLost) * 100 : 0,
                TotalDamageDealt = SessionDamageDealt,
                TotalHealingDone = SessionHealingDone
            };
        }

        public static Player CreatePlayer(string id, string name, string characterClass)
        {
            var player = new Player
            {
                Id = id,
                Name = name,
                Class = characterClass,
                Level = 1,
                Experience = 0,
                SessionStartTime = DateTime.Now
            };

            // Inicializar estatísticas com base na classe
            switch (characterClass.ToLower())
            {
                case "warrior":
                    player.MaxHealth = 150;
                    player.Health = 150;
                    player.MaxMana = 50;
                    player.Mana = 50;
                    player.Attack = 70;
                    player.Defense = 60;
                    player.Speed = 50;
                    break;

                case "mage":
                    player.MaxHealth = 100;
                    player.Health = 100;
                    player.MaxMana = 120;
                    player.Mana = 120;
                    player.Attack = 60;
                    player.Defense = 30;
                    player.Speed = 70;
                    break;

                case "healer":
                    player.MaxHealth = 120;
                    player.Health = 120;
                    player.MaxMana = 100;
                    player.Mana = 100;
                    player.Attack = 40;
                    player.Defense = 50;
                    player.Speed = 60;
                    break;

                default:
                    player.MaxHealth = 100;
                    player.Health = 100;
                    player.MaxMana = 100;
                    player.Mana = 100;
                    player.Attack = 50;
                    player.Defense = 50;
                    player.Speed = 50;
                    break;
            }

            return player;
        }

        public void RecordBattleResult(bool won, int damageDealt = 0, int healingDone = 0)
        {
            if (won)
                SessionBattlesWon++;
            else
                SessionBattlesLost++;

            SessionDamageDealt += damageDealt;
            SessionHealingDone += healingDone;
        }

        public void ResetSession()
        {
            SessionStartTime = DateTime.Now;
            SessionBattlesWon = 0;
            SessionBattlesLost = 0;
            SessionDamageDealt = 0;
            SessionHealingDone = 0;
        }

        // Método para regenerar mana por turno
        public void RegenerateMana(int amount)
        {
            Mana = Math.Min(MaxMana, Mana + amount);
        }

        // Método para consumir mana ao usar habilidades
        public bool UseMana(int amount)
        {
            if (Mana < amount)
                return false;

            Mana -= amount;
            return true;
        }

        // Método para ganhar experiência
        public bool GainExperience(int amount)
        {
            Experience += amount;
            bool leveledUp = false;

            // Verificar se subiu de nível
            int xpForNextLevel = CalculateExpToNextLevel(Level);
            while (Experience >= xpForNextLevel)
            {
                Experience -= xpForNextLevel;
                Level++;
                leveledUp = true;

                // Atualizar xp necessária para o próximo nível
                xpForNextLevel = CalculateExpToNextLevel(Level);
            }

            return leveledUp;
        }
    }

    [Serializable]
    public class ClassMasteryInfo
    {
        public string Class { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public int ExperienceToNextLevel { get; set; }
        public TimeSpan SessionDuration { get; set; }
        public int BattlesWon { get; set; }
        public int BattlesLost { get; set; }
        public float WinRate { get; set; }
        public int TotalDamageDealt { get; set; }
        public int TotalHealingDone { get; set; }
    }

    [Serializable]
    public class PlayerStats
    {
        public int Strength { get; set; }
        public int Intelligence { get; set; }
        public int Wisdom { get; set; }
        public int Constitution { get; set; }
        public int Agility { get; set; }
        public int Luck { get; set; }
    }

    [Serializable]
    public class ClassConfiguration
    {
        public string Name { get; set; }
        public int BaseHealth { get; set; }
        public int BaseMana { get; set; }
        public int BaseAttack { get; set; }
        public int BaseDefense { get; set; }
        public int BaseSpeed { get; set; }
        public int HealthPerLevel { get; set; }
        public int ManaPerLevel { get; set; }
        public int AttackPerLevel { get; set; }
        public int DefensePerLevel { get; set; }
        public int SpeedPerLevel { get; set; }
        public Dictionary<string, int> StartingAttributes { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> AttributeGains { get; set; } = new Dictionary<string, int>();
        public Dictionary<int, List<Skill>> SkillsByLevel { get; set; } = new Dictionary<int, List<Skill>>();
    }
}