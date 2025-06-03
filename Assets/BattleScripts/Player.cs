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

        private int CalculateExpToNextLevel(int level)
        {
            return 50 + (level * 50);
        }

   

        public static Player CreatePlayer(string id, string name, string characterClass)
        {
            switch (characterClass.ToLower())
            {
                case "warrior":
                    return CreateWarrior(id, name);
                case "mage":
                    return CreateMage(id, name);
                case "healer":
                    return CreateHealer(id, name);
                default:
                    return CreateWarrior(id, name);
            }
        }

        private static Player CreateWarrior(string id, string name)
        {
            var player = new Player
            {
                Id = id,
                Name = name,
                Class = "Warrior",
                Level = 1,
                Health = 120,
                MaxHealth = 120,
                Mana = 30,
                MaxMana = 30,
                Attack = 25,
                Defense = 20,
                Speed = 10
            };

            player.Skills.Add(new Skill("warrior_slash", "Slash", "Basic sword attack")
            {
                Damage = 35,
                ManaCost = 5,
                Type = SkillType.Active,
                TargetType = TargetType.Single,
                Element = SkillElement.Physical
            });

            player.Skills.Add(new Skill("warrior_taunt", "Taunt", "Forces enemy to attack you")
            {
                ManaCost = 10,
                Type = SkillType.Taunt,
                TargetType = TargetType.Single,
                TauntDuration = 2,
                StatusEffects = new List<StatusEffect>
                {
                    new StatusEffect("taunt", "Taunt", StatusEffectType.Taunt, 0, 2)
                }
            });

            return player;
        }

        private static Player CreateMage(string id, string name)
        {
            var player = new Player
            {
                Id = id,
                Name = name,
                Class = "Mage",
                Level = 1,
                Health = 80,
                MaxHealth = 80,
                Mana = 100,
                MaxMana = 100,
                Attack = 15,
                Defense = 10,
                Speed = 15
            };

            player.Skills.Add(new Skill("mage_fireball", "Fireball", "Fire spell that deals damage")
            {
                Damage = 45,
                ManaCost = 20,
                Type = SkillType.Active,
                TargetType = TargetType.Single,
                Element = SkillElement.Fire
            });

            player.Skills.Add(new Skill("mage_lightning", "Lightning Bolt", "Lightning attack")
            {
                Damage = 55,
                ManaCost = 30,
                Type = SkillType.Active,
                TargetType = TargetType.Single,
                Element = SkillElement.Lightning
            });

            return player;
        }

        private static Player CreateHealer(string id, string name)
        {
            var player = new Player
            {
                Id = id,
                Name = name,
                Class = "Healer",
                Level = 1,
                Health = 100,
                MaxHealth = 100,
                Mana = 80,
                MaxMana = 80,
                Attack = 12,
                Defense = 15,
                Speed = 12
            };

            player.Skills.Add(new Skill("healer_heal", "Heal", "Restores health to ally")
            {
                Healing = 40,
                ManaCost = 15,
                Type = SkillType.Active,
                TargetType = TargetType.Single,
                Element = SkillElement.Holy
            });

            player.Skills.Add(new Skill("healer_blessing", "Blessing", "Increases ally stats")
            {
                ManaCost = 20,
                Type = SkillType.Buff,
                TargetType = TargetType.Single,
                StatusEffects = new List<StatusEffect>
                {
                    new StatusEffect("blessing", "Blessing", StatusEffectType.AttackBuff, 10, 3)
                }
            });

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

        public void RegenerateMana(int amount)
        {
            Mana = Math.Min(Mana + amount, MaxMana);
        }

        public bool UseMana(int amount)
        {
            if (Mana < amount)
                return false;

            Mana -= amount;
            return true;
        }

        public bool GainExperience(int amount)
        {
            Experience += amount;
            bool leveledUp = false;

            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                Level++;
                leveledUp = true;
                LevelUp();
            }

            return leveledUp;
        }

        private void LevelUp()
        {
            // Increase stats on level up
            MaxHealth += 10;
            Health += 10;
            MaxMana += 5;
            Mana += 5;
            Attack += 2;
            Defense += 2;
            Speed += 1;
        }
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