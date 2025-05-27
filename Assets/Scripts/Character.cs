using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleSystem
{
    [Serializable]
    public abstract class Character
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Speed { get; set; }

        // Propriedades adicionais
        public Dictionary<string, int> Attributes { get; set; } = new Dictionary<string, int>();
        public bool? CanAct { get; set; } = true;

        // Efeitos de status ativos no personagem
        public List<ActiveStatusEffect> ActiveEffects { get; set; } = new List<ActiveStatusEffect>();

        // ID do personagem que está provocando este personagem
        public string TauntedBy { get; set; }

        public bool IsAlive => Health > 0;

        // Métodos para manipular efeitos de status
        public void AddStatusEffect(StatusEffect effect, string sourceId, int duration = -1)
        {
            // Se a duração não foi especificada, usar a do efeito
            if (duration < 0)
                duration = effect.Duration;

            // Verificar se já existe um efeito do mesmo tipo
            var existingEffect = ActiveEffects.Find(e => e.Effect.Id == effect.Id);

            if (existingEffect != null && !effect.IsStackable)
            {
                // Atualizar duração se já existir
                existingEffect.RemainingDuration = duration;
            }
            else
            {
                // Adicionar novo efeito
                ActiveEffects.Add(new ActiveStatusEffect
                {
                    Effect = effect,
                    RemainingDuration = duration,
                    SourceId = sourceId
                });

                // Se for uma provocação, atualizar TauntedBy
                if (effect.Type == StatusEffectType.Taunt)
                {
                    TauntedBy = sourceId;
                }
            }
        }

        // Reduzir duração dos efeitos ativos e aplicar efeitos por turno (veneno, sangramento, etc.)
        public List<ActionResult> ProcessStatusEffects()
        {
            var results = new List<ActionResult>();

            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                var activeEffect = ActiveEffects[i];
                activeEffect.RemainingDuration--;

                // Aplicar efeito por turno se necessário
                var result = ApplyStatusEffectTick(activeEffect);
                if (result != null)
                    results.Add(result);

                // Remover efeitos expirados
                if (activeEffect.RemainingDuration <= 0)
                {
                    RemoveStatusEffect(activeEffect, i);
                }
            }

            return results;
        }

        private ActionResult ApplyStatusEffectTick(ActiveStatusEffect activeEffect)
        {
            var effect = activeEffect.Effect;

            // Verificar o tipo de efeito
            switch (effect.Type)
            {
                case StatusEffectType.Poison:
                case StatusEffectType.Bleed:
                    int damage = effect.Value;
                    Health = Math.Max(0, Health - damage);

                    return new ActionResult
                    {
                        TargetId = this.Id,
                        DamageReceived = damage,
                        EffectApplied = effect.Name,
                        IsDead = !IsAlive
                    };

                case StatusEffectType.Regeneration:
                    int healing = effect.Value;
                    Health = Math.Min(MaxHealth, Health + healing);

                    return new ActionResult
                    {
                        TargetId = this.Id,
                        HealingReceived = healing,
                        EffectApplied = effect.Name,
                        IsDead = false
                    };
            }

            return null;
        }

        private void RemoveStatusEffect(ActiveStatusEffect activeEffect, int index)
        {
            // Limpar referência de provocação se aplicável
            if (activeEffect.Effect.Type == StatusEffectType.Taunt &&
                TauntedBy == activeEffect.SourceId)
            {
                TauntedBy = null;
            }

            // Remover o efeito
            ActiveEffects.RemoveAt(index);
        }

        // Obter valor de atributo considerando buffs/debuffs
        public int GetModifiedAttack()
        {
            int modifiedAttack = Attack;

            foreach (var effect in ActiveEffects)
            {
                if (effect.Effect.Type == StatusEffectType.AttackBuff)
                    modifiedAttack += effect.Effect.Value;
                else if (effect.Effect.Type == StatusEffectType.AttackDebuff)
                    modifiedAttack -= effect.Effect.Value;
            }

            return Math.Max(1, modifiedAttack); // Mínimo 1 de ataque
        }

        public int GetModifiedDefense()
        {
            int modifiedDefense = Defense;

            foreach (var effect in ActiveEffects)
            {
                if (effect.Effect.Type == StatusEffectType.DefenseBuff)
                    modifiedDefense += effect.Effect.Value;
                else if (effect.Effect.Type == StatusEffectType.DefenseDebuff)
                    modifiedDefense -= effect.Effect.Value;
            }

            return Math.Max(0, modifiedDefense);
        }

        public int GetModifiedSpeed()
        {
            int modifiedSpeed = Speed;

            foreach (var effect in ActiveEffects)
            {
                if (effect.Effect.Type == StatusEffectType.SpeedBuff)
                    modifiedSpeed += effect.Effect.Value;
                else if (effect.Effect.Type == StatusEffectType.SpeedDebuff)
                    modifiedSpeed -= effect.Effect.Value;
            }

            return Math.Max(1, modifiedSpeed);
        }

        // Verificar se o personagem está atordoado
        public bool IsStunned()
        {
            return ActiveEffects.Exists(e => e.Effect.Type == StatusEffectType.Stun);
        }
    }

    [Serializable]
    public class ActiveStatusEffect
    {
        public StatusEffect Effect { get; set; }
        public int RemainingDuration { get; set; }
        public string SourceId { get; set; } // ID do personagem que aplicou o efeito
    }

    [Serializable]
    public class StatusEffect
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public StatusEffectType Type { get; set; }
        public int Value { get; set; }       // Valor do efeito (ex: +20 de ataque)
        public int Duration { get; set; }    // Duração em turnos
        public bool IsStackable { get; set; } = false; // Se o efeito pode acumular

        // Construtor para facilitar a criação
        public StatusEffect(string id, string name, StatusEffectType type, int value, int duration)
        {
            Id = id;
            Name = name;
            Type = type;
            Value = value;
            Duration = duration;
            Description = GenerateDescription();
        }

        private string GenerateDescription()
        {
            string effect = IsPositiveEffect(Type) ? "Aumenta" : "Reduz";
            string stat = "";

            switch (Type)
            {
                case StatusEffectType.AttackBuff:
                case StatusEffectType.AttackDebuff:
                    stat = "ataque";
                    break;
                case StatusEffectType.DefenseBuff:
                case StatusEffectType.DefenseDebuff:
                    stat = "defesa";
                    break;
                case StatusEffectType.SpeedBuff:
                case StatusEffectType.SpeedDebuff:
                    stat = "velocidade";
                    break;
                case StatusEffectType.Poison:
                    return $"Causa {Value} de dano por turno durante {Duration} turnos";
                case StatusEffectType.Bleed:
                    return $"Causa {Value} de dano por turno durante {Duration} turnos";
                case StatusEffectType.Stun:
                    return $"Impede ações durante {Duration} turnos";
                case StatusEffectType.Taunt:
                    return $"Força a atacar o provocador durante {Duration} turnos";
                case StatusEffectType.Regeneration:
                    return $"Recupera {Value} de vida por turno durante {Duration} turnos";
                case StatusEffectType.Shield:
                    return $"Absorve {Value} de dano durante {Duration} turnos";
                case StatusEffectType.Immunity:
                    return $"Concede imunidade a efeitos negativos durante {Duration} turnos";
            }

            return $"{effect} {stat} em {Value} durante {Duration} turnos";
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
    }

    public enum StatusEffectType
    {
        AttackBuff,
        AttackDebuff,
        DefenseBuff,
        DefenseDebuff,
        SpeedBuff,
        SpeedDebuff,
        Poison,
        Bleed,
        Regeneration,
        Stun,
        Taunt,
        Shield,
        Immunity
    }
}