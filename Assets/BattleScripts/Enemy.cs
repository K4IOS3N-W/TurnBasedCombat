using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleSystem
{
    [Serializable]
    public class Enemy : Character
    {
        public EnemyType Type { get; set; } = EnemyType.Normal;
        public int Level { get; set; } = 1;
        public List<Skill> Skills { get; set; } = new List<Skill>();
        public List<string> Drops { get; set; } = new List<string>();
        public int ExperienceReward { get; set; } = 10;
        public int GoldReward { get; set; } = 5;
        public EnemyBehavior Behavior { get; set; } = EnemyBehavior.Aggressive;

        // Attributes related to behavior
        public bool IsElite { get; set; } = false;
        public bool IsBoss { get; set; } = false;
        public int AggroValue { get; set; } = 1;

        // Visual representation
        public string SpriteName { get; set; }

        // Elemental resistances
        public Dictionary<SkillElement, float> ElementalResistances { get; set; } = new Dictionary<SkillElement, float>();

        public Enemy()
        {
            ElementalResistances = new Dictionary<SkillElement, float>
            {
                { SkillElement.Physical, 0.0f },
                { SkillElement.Fire, 0.0f },
                { SkillElement.Ice, 0.0f },
                { SkillElement.Lightning, 0.0f },
                { SkillElement.Holy, 0.0f },
                { SkillElement.Dark, 0.0f },
                { SkillElement.Earth, 0.0f },
                { SkillElement.Wind, 0.0f },
                { SkillElement.Water, 0.0f }
            };
        }

        // Method to copy an Enemy (clone template)
        public Enemy Clone()
        {
            return new Enemy
            {
                Id = Guid.NewGuid().ToString(),
                Name = Name,
                Health = Health,
                MaxHealth = MaxHealth,
                Attack = Attack,
                Defense = Defense,
                Speed = Speed,
                Type = Type,
                Level = Level,
                Skills = Skills.Select(s => CloneSkill(s)).ToList(),
                Drops = new List<string>(Drops),
                ExperienceReward = ExperienceReward,
                GoldReward = GoldReward,
                Behavior = Behavior,
                IsElite = IsElite,
                IsBoss = IsBoss,
                AggroValue = AggroValue,
                SpriteName = SpriteName,
                ElementalResistances = new Dictionary<SkillElement, float>(ElementalResistances),
                Attributes = new Dictionary<string, int>(Attributes),
                ActiveEffects = new List<ActiveStatusEffect>(),
                CanAct = true
            };
        }

        private Skill CloneSkill(Skill original)
        {
            return new Skill(original.Id, original.Name, original.Description)
            {
                Damage = original.Damage,
                Healing = original.Healing,
                ManaCost = original.ManaCost,
                Range = original.Range,
                AffectsTeam = original.AffectsTeam,
                Cooldown = original.Cooldown,
                CurrentCooldown = 0,
                MaxCooldown = original.MaxCooldown,
                Type = original.Type,
                TargetType = original.TargetType,
                StatusEffects = original.StatusEffects.ToList(),
                ExecuteThreshold = original.ExecuteThreshold,
                ExecuteDamageBonus = original.ExecuteDamageBonus,
                TauntDuration = original.TauntDuration,
                EffectDuration = original.EffectDuration,
                Element = original.Element,
                CriticalChance = original.CriticalChance,
                CriticalMultiplier = original.CriticalMultiplier,
                Properties = new Dictionary<string, object>(original.Properties),
                IsStackable = original.IsStackable
            };
        }

        public static Dictionary<string, Enemy> CreateEnemyTemplates()
        {
            var templates = new Dictionary<string, Enemy>();

            // Goblin template
            var goblin = new Enemy
            {
                Id = "goblin_template",
                Name = "Goblin",
                Health = 50,
                MaxHealth = 50,
                Attack = 15,
                Defense = 8,
                Speed = 12,
                Type = EnemyType.Normal,
                Level = 1,
                ExperienceReward = 25,
                GoldReward = 10,
                Behavior = EnemyBehavior.Aggressive,
                SpriteName = "goblin_sprite"
            };
            goblin.Skills.Add(new Skill("goblin_slash", "Crude Slash", "Basic goblin attack")
            {
                Damage = 20,
                Type = SkillType.Active,
                TargetType = TargetType.Single,
                Element = SkillElement.Physical
            });
            templates["goblin"] = goblin;

            // Orc template
            var orc = new Enemy
            {
                Id = "orc_template",
                Name = "Orc Warrior",
                Health = 100,
                MaxHealth = 100,
                Attack = 25,
                Defense = 15,
                Speed = 8,
                Type = EnemyType.Elite,
                Level = 3,
                ExperienceReward = 50,
                GoldReward = 20,
                Behavior = EnemyBehavior.Aggressive,
                IsElite = true,
                SpriteName = "orc_sprite"
            };
            orc.Skills.Add(new Skill("orc_charge", "Charge", "Powerful charging attack")
            {
                Damage = 35,
                Type = SkillType.Active,
                TargetType = TargetType.Single,
                Element = SkillElement.Physical
            });
            templates["orc"] = orc;

            // Dragon template (Boss)
            var dragon = new Enemy
            {
                Id = "dragon_template",
                Name = "Ancient Dragon",
                Health = 300,
                MaxHealth = 300,
                Attack = 50,
                Defense = 30,
                Speed = 15,
                Type = EnemyType.Boss,
                Level = 10,
                ExperienceReward = 200,
                GoldReward = 100,
                Behavior = EnemyBehavior.Smart,
                IsElite = true,
                IsBoss = true,
                SpriteName = "dragon_sprite"
            };
            dragon.Skills.Add(new Skill("dragon_flame", "Dragon Flame", "Devastating fire breath")
            {
                Damage = 60,
                Type = SkillType.AoE,
                TargetType = TargetType.AllEnemies,
                Element = SkillElement.Fire
            });
            dragon.ElementalResistances[SkillElement.Fire] = 0.5f;
            dragon.ElementalResistances[SkillElement.Ice] = -0.5f;
            templates["dragon"] = dragon;

            return templates;
        }
    }

    [Serializable]
    public enum EnemyType
    {
        Normal,
        Elite,
        Boss,
        Minion,
        Swarm
    }

    [Serializable]
    public enum EnemyBehavior
    {
        Aggressive,
        Defensive,
        Random,
        Smart,
        Coward
    }
}