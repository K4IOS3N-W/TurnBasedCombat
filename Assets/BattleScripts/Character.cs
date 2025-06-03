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
        public Dictionary<string, int> Attributes { get; set; } = new Dictionary<string, int>();
        public bool? CanAct { get; set; } = true;
        public List<ActiveStatusEffect> ActiveEffects { get; set; } = new List<ActiveStatusEffect>();
        public string TauntedBy { get; set; }

        public bool IsAlive => Health > 0;

        public bool IsStunned()
        {
            return ActiveEffects.Any(e => e.Effect.Type == StatusEffectType.Stun);
        }

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
            return Math.Max(1, modifiedAttack);
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

        public void ApplyStatusEffect(StatusEffect effect, string sourceId)
        {
            if (!effect.IsStackable)
            {
                var existing = ActiveEffects.FirstOrDefault(e => e.Effect.Id == effect.Id);
                if (existing != null)
                {
                    existing.RemainingDuration = effect.Duration;
                    return;
                }
            }

            ActiveEffects.Add(new ActiveStatusEffect
            {
                Effect = effect,
                RemainingDuration = effect.Duration,
                SourceId = sourceId
            });
        }

        public void ProcessStatusEffects()
        {
            for (int i = ActiveEffects.Count - 1; i >= 0; i--)
            {
                var activeEffect = ActiveEffects[i];
                
                // Apply effect
                switch (activeEffect.Effect.Type)
                {
                    case StatusEffectType.Poison:
                    case StatusEffectType.Bleed:
                        TakeDamage(activeEffect.Effect.Value);
                        break;
                    case StatusEffectType.Regeneration:
                        Heal(activeEffect.Effect.Value);
                        break;
                }

                // Reduce duration
                activeEffect.RemainingDuration--;
                
                // Remove if expired
                if (activeEffect.RemainingDuration <= 0)
                {
                    ActiveEffects.RemoveAt(i);
                }
            }
        }

        public void TakeDamage(int damage)
        {
            Health = Math.Max(0, Health - damage);
        }

        public void Heal(int healing)
        {
            Health = Math.Min(MaxHealth, Health + healing);
        }

        public void RemoveStatusEffect(string effectId)
        {
            ActiveEffects.RemoveAll(e => e.Effect.Id == effectId);
        }

        public void ClearTaunt()
        {
            TauntedBy = null;
            RemoveStatusEffect("taunt");
        }
    }

    [Serializable]
    public class ActiveStatusEffect
    {
        public StatusEffect Effect { get; set; }
        public int RemainingDuration { get; set; }
        public string SourceId { get; set; }
    }

    [Serializable]
    public class StatusEffect
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public StatusEffectType Type { get; set; }
        public int Value { get; set; }
        public int Duration { get; set; }
        public bool IsStackable { get; set; } = false;

        public StatusEffect() { }

        public StatusEffect(string id, string name, StatusEffectType type, int value, int duration)
        {
            Id = id;
            Name = name;
            Type = type;
            Value = value;
            Duration = duration;
        }
    }

    [Serializable]
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