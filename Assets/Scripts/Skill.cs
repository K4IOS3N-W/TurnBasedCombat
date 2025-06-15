using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleSystem
{
    /// <summary>
    /// Interface para implementar efeitos de habilidades de forma modular
    /// </summary>
    public interface ISkillEffect
    {
        string EffectId { get; }
        string Name { get; }
        void Apply(Character caster, Character target, Skill skill, Battle battle, List<ActionResult> results);
    }

    [Serializable]
    public class Skill
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        // Valores básicos
        public int Damage { get; set; }
        public int Healing { get; set; }
        public int ManaCost { get; set; }
        public int Range { get; set; }
        public bool AffectsTeam { get; set; }

        // Sistema de cooldown
        public int Cooldown { get; set; }
        public int CurrentCooldown { get; set; }
        public int MaxCooldown { get; set; }

        // Tipo de habilidade
        public SkillType Type { get; set; } = SkillType.Active;
        public TargetType TargetType { get; set; } = TargetType.Single;

        // Propriedades para buffs/debuffs
        public List<StatusEffect> StatusEffects { get; set; } = new List<StatusEffect>();

        // Para habilidades de execução
        public float ExecuteThreshold { get; set; } // Porcentagem de vida abaixo da qual a execução causa dano extra
        public int ExecuteDamageBonus { get; set; } // Dano adicional da execução

        // Para provocações (taunts)
        public int TauntDuration { get; set; } // Duração da provocação em turnos

        // Para buffs/debuffs
        public int EffectDuration { get; set; } // Duração do efeito em turnos

        // Elemento da habilidade
        public SkillElement Element { get; set; } = SkillElement.None;

        // Crítico
        public float CriticalChance { get; set; } = 0.05f; // 5% chance de crítico base
        public float CriticalMultiplier { get; set; } = 1.5f; // Multiplicador de dano crítico

        // Propriedades adicionais genéricas
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        // Lista de efeitos de habilidade modulares
        [NonSerialized]
        private List<ISkillEffect> _effects;

        public List<ISkillEffect> Effects
        {
            get
            {
                if (_effects == null)
                {
                    _effects = new List<ISkillEffect>();
                    // Aqui podemos adicionar efeitos padrão baseados no tipo da habilidade
                    if (Damage > 0)
                    {
                        _effects.Add(new DamageSkillEffect());
                    }
                    if (Healing > 0)
                    {
                        _effects.Add(new HealingSkillEffect());
                    }
                    if (StatusEffects.Count > 0)
                    {
                        _effects.Add(new StatusEffectSkillEffect());
                    }
                    if (Type == SkillType.Execute)
                    {
                        _effects.Add(new ExecuteSkillEffect());
                    }
                    if (Type == SkillType.Taunt)
                    {
                        _effects.Add(new TauntSkillEffect());
                    }
                    if (Type == SkillType.Drain)
                    {
                        _effects.Add(new DrainSkillEffect());
                    }
                }
                return _effects;
            }
            set { _effects = value; }
        }

        // Construtor
        public Skill()
        {
        }

        // Construtor com parâmetros básicos
        public Skill(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }

        // Adiciona um efeito à habilidade
        public void AddEffect(ISkillEffect effect)
        {
            if (!Effects.Any(e => e.EffectId == effect.EffectId))
            {
                Effects.Add(effect);
            }
        }

        // Remove um efeito da habilidade
        public void RemoveEffect(string effectId)
        {
            Effects.RemoveAll(e => e.EffectId == effectId);
        }

        // Aplica todos os efeitos da habilidade
        public List<ActionResult> ApplyEffects(Character caster, Character target, Battle battle)
        {
            var results = new List<ActionResult>();

            foreach (var effect in Effects)
            {
                effect.Apply(caster, target, this, battle, results);
            }

            return results;
        }

        public bool CanUse(Character caster, Battle battle = null)
        {
            if (CurrentCooldown > 0) return false;

            // Verificar mana se o personagem for um jogador
            if (caster is Player player && player.Mana < ManaCost) return false;

            // Verificar se o personagem pode agir
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

        // Calcula dano considerando crítico e resistências elementais
        public int CalculateDamage(Character caster, Character target)
        {
            int baseDamage = Damage;

            // Aplicar bônus de execução se aplicável
            if (Type == SkillType.Execute && target.Health <= target.MaxHealth * ExecuteThreshold)
            {
                baseDamage += ExecuteDamageBonus;
            }

            // Verificar crítico
            bool isCritical = new Random().NextDouble() < CriticalChance;
            if (isCritical)
            {
                baseDamage = (int)(baseDamage * CriticalMultiplier);
            }

            // Aplicar resistências elementais
            float elementModifier = 1.0f;
            if (target is Enemy enemy && Element != SkillElement.None)
            {
                elementModifier = enemy.CalculateElementalModifier(Element);
            }

            // Calcular dano final
            int attackPower = caster.GetModifiedAttack();
            int defense = target.GetModifiedDefense();
            float damageReduction = defense / 100f;

            int finalDamage = (int)(baseDamage * (1 - Math.Min(0.75f, damageReduction)) * elementModifier);

            // Adicionar aleatoriedade (+-10%)
            Random random = new Random();
            float variation = random.Next(-10, 11) / 100f;
            finalDamage = (int)(finalDamage * (1 + variation));

            return Math.Max(1, finalDamage);
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

            // Se o lançador for um jogador
            if (caster is Player player)
            {
                var casterTeam = battle.Teams.FirstOrDefault(t => t.Players.Any(p => p.Id == player.Id));

                // Se a batalha for PvP
                if (battle.IsPvP && casterTeam != null)
                {
                    // Adicionar jogadores de outras equipes como alvos
                    foreach (var team in battle.Teams.Where(t => t.Id != casterTeam.Id))
                    {
                        targets.AddRange(team.Players.Where(p => p.IsAlive).Select(p => p.Id));
                    }
                }
                else
                {
                    // Em PvE, os alvos são os inimigos
                    targets.AddRange(battle.Enemies.Where(e => e.IsAlive).Select(e => e.Id));
                }
            }
            // Se o lançador for um inimigo
            else if (caster is Enemy)
            {
                // Os alvos são os jogadores
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

            // Se o lançador for um jogador
            if (caster is Player player)
            {
                var casterTeam = battle.Teams.FirstOrDefault(t => t.Players.Any(p => p.Id == player.Id));
                if (casterTeam != null)
                {
                    // Os aliados são os jogadores da mesma equipe
                    targets.AddRange(casterTeam.Players.Where(p => p.IsAlive).Select(p => p.Id));
                }
            }
            // Se o lançador for um inimigo
            else if (caster is Enemy enemy)
            {
                // Os aliados são outros inimigos
                targets.AddRange(battle.Enemies.Where(e => e.IsAlive && e.Id != enemy.Id).Select(e => e.Id));

                // Incluir o próprio inimigo como alvo
                targets.Add(enemy.Id);
            }

            return targets;
        }

        private List<string> GetAreaTargets(Character caster, Battle battle)
        {
            // Para simplicidade, vamos considerar que habilidades de área afetam
            // todos os alvos que seriam atingidos individualmente
            return GetSingleTargets(caster, battle);
        }

        private List<string> GetRandomTargets(Character caster, Battle battle)
        {
            // Para simplicidade, podemos retornar todos os alvos elegíveis
            // O sistema de batalha pode selecionar um aleatoriamente
            return GetSingleTargets(caster, battle);
        }

        private bool IsInRange(Character caster, Character target)
        {
            // Implementação simples: consideramos que todos estão em alcance
            return true;
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

    // Classes de efeitos de habilidades
    public class DamageSkillEffect : ISkillEffect
    {
        public string EffectId => "damage";
        public string Name => "Dano";

        public void Apply(Character caster, Character target, Skill skill, Battle battle, List<ActionResult> results)
        {
            if (skill.Damage <= 0) return;

            int damage = skill.CalculateDamage(caster, target);
            target.Health = Math.Max(0, target.Health - damage);

            var result = new ActionResult
            {
                TargetId = target.Id,
                AttackerId = caster.Id,
                DamageReceived = damage,
                IsDead = !target.IsAlive,
                IsCritical = damage > skill.Damage * 1.2f, // Aproximação para detectar crítico
                Message = $"{target.Name} recebeu {damage} de dano"
            };

            results.Add(result);
        }
    }

    public class HealingSkillEffect : ISkillEffect
    {
        public string EffectId => "healing";
        public string Name => "Cura";

        public void Apply(Character caster, Character target, Skill skill, Battle battle, List<ActionResult> results)
        {
            if (skill.Healing <= 0) return;

            int healing = skill.Healing;

            // Bônus baseado em atributos
            if (caster is Player player)
            {
                // Curandeiros curam mais
                if (player.Class.ToLower() == "healer")
                {
                    healing = (int)(healing * 1.2f);
                }
            }

            int healingDone = Math.Min(target.MaxHealth - target.Health, healing);
            target.Health += healingDone;

            var result = new ActionResult
            {
                TargetId = target.Id,
                AttackerId = caster.Id,
                HealingReceived = healingDone,
                Message = $"{target.Name} recuperou {healingDone} de vida"
            };

            results.Add(result);
        }
    }

    public class StatusEffectSkillEffect : ISkillEffect
    {
        public string EffectId => "status_effect";
        public string Name => "Efeito de Status";

        public void Apply(Character caster, Character target, Skill skill, Battle battle, List<ActionResult> results)
        {
            if (skill.StatusEffects.Count == 0) return;

            foreach (var effect in skill.StatusEffects)
            {
                target.AddStatusEffect(effect, caster.Id, skill.EffectDuration > 0 ? skill.EffectDuration : effect.Duration);

                var result = new ActionResult
                {
                    TargetId = target.Id,
                    AttackerId = caster.Id,
                    EffectApplied = effect.Name,
                    Message = $"{target.Name} afetado por {effect.Name}"
                };

                results.Add(result);
            }
        }
    }

    public class ExecuteSkillEffect : ISkillEffect
    {
        public string EffectId => "execute";
        public string Name => "Execução";

        public void Apply(Character caster, Character target, Skill skill, Battle battle, List<ActionResult> results)
        {
            if (skill.Type != SkillType.Execute) return;

            bool isExecutable = target.Health <= target.MaxHealth * skill.ExecuteThreshold;
            int damage = skill.CalculateDamage(caster, target);

            if (isExecutable)
            {
                damage += skill.ExecuteDamageBonus;
            }

            target.Health = Math.Max(0, target.Health - damage);

            var result = new ActionResult
            {
                TargetId = target.Id,
                AttackerId = caster.Id,
                DamageReceived = damage,
                IsDead = !target.IsAlive,
                Message = isExecutable
                    ? $"Execução! {target.Name} recebeu {damage} de dano"
                    : $"{target.Name} recebeu {damage} de dano"
            };

            results.Add(result);
        }
    }

    public class TauntSkillEffect : ISkillEffect
    {
        public string EffectId => "taunt";
        public string Name => "Provocação";

        public void Apply(Character caster, Character target, Skill skill, Battle battle, List<ActionResult> results)
        {
            if (skill.Type != SkillType.Taunt) return;

            // Criar efeito de provocação
            var tauntEffect = new StatusEffect(
                "effect_taunt_" + caster.Id,
                "Provocado",
                StatusEffectType.Taunt,
                0, // Valor não importa para Taunt
                skill.TauntDuration > 0 ? skill.TauntDuration : 2 // Duração padrão: 2 turnos
            );

            target.AddStatusEffect(tauntEffect, caster.Id);
            target.TauntedBy = caster.Id;

            var result = new ActionResult
            {
                TargetId = target.Id,
                AttackerId = caster.Id,
                EffectApplied = "Provocação",
                Message = $"{target.Name} foi provocado por {caster.Name}"
            };

            results.Add(result);
        }
    }

    public class DrainSkillEffect : ISkillEffect
    {
        public string EffectId => "drain";
        public string Name => "Drenagem";

        public void Apply(Character caster, Character target, Skill skill, Battle battle, List<ActionResult> results)
        {
            if (skill.Type != SkillType.Drain) return;

            int damage = skill.CalculateDamage(caster, target);
            target.Health = Math.Max(0, target.Health - damage);

            // Drenar como cura (geralmente 30-50% do dano)
            int drainPercent = skill.Properties.TryGetValue("DrainPercent", out object drainObj)
                ? Convert.ToInt32(drainObj)
                : 30;

            int healing = (int)(damage * drainPercent / 100f);
            caster.Health = Math.Min(caster.MaxHealth, caster.Health + healing);

            // Resultado do dano
            var damageResult = new ActionResult
            {
                TargetId = target.Id,
                AttackerId = caster.Id,
                DamageReceived = damage,
                IsDead = !target.IsAlive,
                Message = $"{target.Name} perdeu {damage} de vida"
            };

            // Resultado da cura
            var healResult = new ActionResult
            {
                TargetId = caster.Id,
                AttackerId = caster.Id,
                HealingReceived = healing,
                Message = $"{caster.Name} drenou {healing} de vida"
            };

            results.Add(damageResult);
            results.Add(healResult);
        }
    }

    [Serializable]
    public enum SkillType
    {
        Active,    // Habilidade ativa
        Passive,   // Habilidade passiva
        Buff,      // Aumenta status
        Debuff,    // Reduz status
        Execute,   // Causa dano extra em alvos com pouca vida
        Taunt,     // Força inimigos a atacar o lançador
        Summon,    // Invoca aliados
        AoE,       // Ataque em área
        Drain,     // Drena vida/mana
        Reaction,  // Ativada por eventos
        Toggle     // Pode ser ligada/desligada
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