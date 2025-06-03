using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleSystem
{
    [Serializable]
    public class Skill
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Damage { get; set; }
        public int Healing { get; set; }
        public int ManaCost { get; set; }
        public int Range { get; set; }
        public bool AffectsTeam { get; set; }
        public int Cooldown { get; set; }
        public int CurrentCooldown { get; set; }
        public int MaxCooldown { get; set; }
        public SkillType Type { get; set; } = SkillType.Active;
        public TargetType TargetType { get; set; } = TargetType.Single;
        public List<StatusEffect> StatusEffects { get; set; } = new List<StatusEffect>();
        public float ExecuteThreshold { get; set; }
        public int ExecuteDamageBonus { get; set; }
        public int TauntDuration { get; set; }
        public int EffectDuration { get; set; }
        public SkillElement Element { get; set; } = SkillElement.None;
        public float CriticalChance { get; set; } = 0.05f;
        public float CriticalMultiplier { get; set; } = 1.5f;
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public bool IsStackable { get; set; } = false;

        public Skill()
        {
        }

        public Skill(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        public bool CanUse(Character caster, Battle battle = null)
        {
            if (CurrentCooldown > 0) return false;
            if (caster is Player player && player.Mana < ManaCost) return false;
            if (caster.CanAct == false) return false;
            return true;
        }

        public void Use()
        {
            CurrentCooldown = MaxCooldown > 0 ? MaxCooldown : Cooldown;
        }

        public void ReduceCooldown()
        {
            if (CurrentCooldown > 0)
                CurrentCooldown--;
        }

        public List<string> GetValidTargets(Character caster, Battle battle)
        {
            if (battle == null) return new List<string>();

            switch (TargetType)
            {
                case TargetType.Self:
                    return new List<string> { caster.Id };
                case TargetType.Single:
                    return GetSingleTargets(caster, battle);
                case TargetType.AllEnemies:
                    return GetEnemyTargets(caster, battle);
                case TargetType.AllAllies:
                    return GetAllyTargets(caster, battle);
                case TargetType.Area:
                    return GetAreaTargets(caster, battle);
                case TargetType.Random:
                    return GetRandomTargets(caster, battle);
                default:
                    return new List<string>();
            }
        }

        private List<string> GetSingleTargets(Character caster, Battle battle)
        {
            var targets = new List<string>();
            bool isOffensive = Damage > 0 || StatusEffects.Any(e => IsNegativeEffect(e.Type));
            bool isSupportive = Healing > 0 || StatusEffects.Any(e => IsPositiveEffect(e.Type));

            if (isOffensive)
            {
                targets.AddRange(GetEnemyTargets(caster, battle));
            }

            if (isSupportive)
            {
                targets.AddRange(GetAllyTargets(caster, battle));
            }

            return targets.Distinct().ToList();
        }

        private List<string> GetEnemyTargets(Character caster, Battle battle)
        {
            var targets = new List<string>();

            if (caster is Player player)
            {
                var casterTeam = battle.Teams.FirstOrDefault(t => t.Players.Any(p => p.Id == player.Id));

                if (battle.IsPvP && casterTeam != null)
                {
                    foreach (var team in battle.Teams.Where(t => t.Id != casterTeam.Id))
                    {
                        targets.AddRange(team.Players.Where(p => p.IsAlive).Select(p => p.Id));
                    }
                }
                else
                {
                    targets.AddRange(battle.Enemies.Where(e => e.IsAlive).Select(e => e.Id));
                }
            }
            else if (caster is Enemy)
            {
                foreach (var team in battle.Teams)
                {
                    targets.AddRange(team.Players.Where(p => p.IsAlive).Select(p => p.Id));
                }
            }

            return targets;
        }

        private List<string> GetAllyTargets(Character caster, Battle battle)
        {
            var targets = new List<string>();

            if (caster is Player player)
            {
                var casterTeam = battle.Teams.FirstOrDefault(t => t.Players.Any(p => p.Id == player.Id));
                if (casterTeam != null)
                {
                    targets.AddRange(casterTeam.Players.Where(p => p.IsAlive).Select(p => p.Id));
                }
            }
            else if (caster is Enemy enemy)
            {
                targets.AddRange(battle.Enemies.Where(e => e.IsAlive && e.Id != enemy.Id).Select(e => e.Id));
                targets.Add(enemy.Id);
            }

            return targets;
        }

        private List<string> GetAreaTargets(Character caster, Battle battle)
        {
            return GetSingleTargets(caster, battle);
        }

        private List<string> GetRandomTargets(Character caster, Battle battle)
        {
            return GetSingleTargets(caster, battle);
        }

        private bool IsNegativeEffect(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.AttackDebuff => true,
                StatusEffectType.DefenseDebuff => true,
                StatusEffectType.SpeedDebuff => true,
                StatusEffectType.Poison => true,
                StatusEffectType.Bleed => true,
                StatusEffectType.Stun => true,
                StatusEffectType.Taunt => true,
                _ => false
            };
        }

        private bool IsPositiveEffect(StatusEffectType type)
        {
            return type switch
            {
                StatusEffectType.AttackBuff => true,
                StatusEffectType.DefenseBuff => true,
                StatusEffectType.SpeedBuff => true,
                StatusEffectType.Regeneration => true,
                StatusEffectType.Shield => true,
                StatusEffectType.Immunity => true,
                _ => false
            };
        }

        public SkillInfo GetInfo()
        {
            return new SkillInfo
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Damage = Damage,
                Healing = Healing,
                ManaCost = ManaCost,
                Range = Range,
                Cooldown = Cooldown,
                CurrentCooldown = CurrentCooldown,
                Type = Type,
                TargetType = TargetType,
                Element = Element,
                IsUsable = CurrentCooldown == 0
            };
        }
    }

    [Serializable]
    public enum SkillType
    {
        Active,
        Passive,
        Buff,
        Debuff,
        Execute,
        Taunt,
        Summon,
        AoE,
        Drain,
        Reaction,
        Toggle
    }

    [Serializable]
    public enum TargetType
    {
        Self,
        Single,
        AllEnemies,
        AllAllies,
        Area,
        Random
    }

    [Serializable]
    public enum SkillElement
    {
        None,
        Physical,
        Fire,
        Ice,
        Lightning,
        Holy,
        Dark,
        Earth,
        Wind,
        Water
    }

    [Serializable]
    public class SkillInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Damage { get; set; }
        public int Healing { get; set; }
        public int ManaCost { get; set; }
        public int Range { get; set; }
        public int Cooldown { get; set; }
        public int CurrentCooldown { get; set; }
        public SkillType Type { get; set; }
        public TargetType TargetType { get; set; }
        public SkillElement Element { get; set; }
        public bool IsUsable { get; set; }
    }
}