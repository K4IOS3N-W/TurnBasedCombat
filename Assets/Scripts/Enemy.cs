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

        // Atributos relacionados a comportamento
        public bool IsElite { get; set; } = false;
        public bool IsBoss { get; set; } = false;
        public int AggroValue { get; set; } = 1;  // Quanto mais alto, mais chances do inimigo ser alvo

        // Possibilidade de adicionar um sprite/modelo para o inimigo
        public string SpriteName { get; set; }

        // Taxas de resistência a elementos
        public Dictionary<SkillElement, float> ElementalResistances { get; set; } = new Dictionary<SkillElement, float>();

        public Enemy()
        {
            // Inicializa resistências elementais para 0 (normal)
            foreach (SkillElement element in Enum.GetValues(typeof(SkillElement)))
            {
                ElementalResistances[element] = 0f;
            }
        }

        // Método para copiar um Enemy (clonar template)
        public Enemy Clone()
        {
            var clone = new Enemy
            {
                Id = Guid.NewGuid().ToString(),
                Name = this.Name,
                Type = this.Type,
                Level = this.Level,
                Health = this.Health,
                MaxHealth = this.MaxHealth,
                Attack = this.Attack,
                Defense = this.Defense,
                Speed = this.Speed,
                IsElite = this.IsElite,
                IsBoss = this.IsBoss,
                AggroValue = this.AggroValue,
                SpriteName = this.SpriteName,
                ExperienceReward = this.ExperienceReward,
                GoldReward = this.GoldReward,
                Behavior = this.Behavior
            };

            // Copiar habilidades
            foreach (var skill in this.Skills)
            {
                clone.Skills.Add(new Skill
                {
                    Id = skill.Id,
                    Name = skill.Name,
                    Description = skill.Description,
                    Damage = skill.Damage,
                    Healing = skill.Healing,
                    ManaCost = skill.ManaCost,
                    Range = skill.Range,
                    AffectsTeam = skill.AffectsTeam,
                    Cooldown = skill.Cooldown,
                    CurrentCooldown = skill.CurrentCooldown,
                    MaxCooldown = skill.MaxCooldown,
                    Type = skill.Type,
                    TargetType = skill.TargetType,
                    Element = skill.Element
                });
            }

            // Copiar drops
            clone.Drops.AddRange(this.Drops);

            // Copiar resistências elementais
            foreach (var pair in this.ElementalResistances)
            {
                clone.ElementalResistances[pair.Key] = pair.Value;
            }

            // Copiar efeitos ativos
            foreach (var effect in this.ActiveEffects)
            {
                var effectCopy = new StatusEffect(
                    effect.Effect.Id,
                    effect.Effect.Name,
                    effect.Effect.Type,
                    effect.Effect.Value,
                    effect.Effect.Duration
                );

                clone.AddStatusEffect(effectCopy, effect.SourceId, effect.RemainingDuration);
            }

            return clone;
        }

        // Método para determinar qual ação o inimigo executará
        public Action DecideAction(Battle battle)
        {
            // Implementação básica: escolher uma habilidade aleatória ou atacar
            var availableSkills = Skills.Where(s => s.CurrentCooldown == 0).ToList();

            if (availableSkills.Count > 0 && new Random().Next(100) < 70) // 70% de chance de usar habilidade
            {
                // Escolher uma habilidade aleatória
                var skill = availableSkills[new Random().Next(availableSkills.Count)];

                // Escolher um alvo válido
                var validTargets = skill.GetValidTargets(this, battle);
                if (validTargets.Count > 0)
                {
                    string targetId = validTargets[new Random().Next(validTargets.Count)];

                    // Marcar cooldown da habilidade
                    skill.Use();

                    return new Action
                    {
                        Type = ActionType.Skill,
                        TargetId = targetId,
                        SkillId = skill.Id
                    };
                }
            }

            // Ataque básico (fallback)
            // Escolher um jogador aleatório como alvo
            var players = battle.Teams.SelectMany(t => t.Players).Where(p => p.IsAlive).ToList();

            if (players.Count > 0)
            {
                // Verificar se há algum jogador provocando o inimigo
                if (!string.IsNullOrEmpty(TauntedBy))
                {
                    var tauntingPlayer = players.FirstOrDefault(p => p.Id == TauntedBy);
                    if (tauntingPlayer != null)
                    {
                        return new Action
                        {
                            Type = ActionType.Attack,
                            TargetId = tauntingPlayer.Id
                        };
                    }
                }

                // Sem provocação, escolher um alvo com base no comportamento
                switch (Behavior)
                {
                    case EnemyBehavior.Aggressive:
                        // Escolher o jogador com menos vida
                        var weakestPlayer = players.OrderBy(p => p.Health).First();
                        return new Action
                        {
                            Type = ActionType.Attack,
                            TargetId = weakestPlayer.Id
                        };

                    case EnemyBehavior.Defensive:
                        // Escolher o jogador com mais ataque
                        var strongestPlayer = players.OrderByDescending(p => p.GetModifiedAttack()).First();
                        return new Action
                        {
                            Type = ActionType.Attack,
                            TargetId = strongestPlayer.Id
                        };

                    case EnemyBehavior.Random:
                    default:
                        // Escolher um jogador aleatório
                        var randomPlayer = players[new Random().Next(players.Count)];
                        return new Action
                        {
                            Type = ActionType.Attack,
                            TargetId = randomPlayer.Id
                        };
                }
            }

            // Se não houver jogadores vivos, simplesmente passar o turno
            return new Action
            {
                Type = ActionType.Pass
            };
        }

        // Método para calcular dano recebido considerando resistências elementais
        public float CalculateElementalModifier(SkillElement element)
        {
            if (ElementalResistances.TryGetValue(element, out float resistance))
            {
                // Resistência: -1.0 (imune) a 1.0 (vulnerável)
                // 0 = dano normal, <0 = redução, >0 = aumento
                return 1.0f + resistance;
            }

            return 1.0f; // Sem modificador
        }

        // Reduzir cooldown de todas as habilidades
        public void ReduceAllCooldowns()
        {
            foreach (var skill in Skills)
            {
                skill.ReduceCooldown();
            }
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
        Aggressive,  // Ataca os alvos mais fracos
        Defensive,   // Ataca os alvos mais perigosos
        Random,      // Escolhe alvos aleatoriamente
        Smart,       // Tenta usar estratégias inteligentes
        Coward       // Tenta fugir quando está fraco
    }
}