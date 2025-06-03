using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleSystem
{
    public class EnemyAI
    {
        private Random random = new Random();

        public Action DecideAction(Enemy enemy, Battle battle)
        {
            if (enemy.IsStunned() || enemy.CanAct == false)
            {
                return new Action { Type = ActionType.Pass };
            }

            if (!string.IsNullOrEmpty(enemy.TauntedBy))
            {
                var tauntingPlayer = battle.Teams
                    .SelectMany(t => t.Players)
                    .FirstOrDefault(p => p.Id == enemy.TauntedBy);

                if (tauntingPlayer != null && tauntingPlayer.IsAlive)
                {
                    return DecideActionAgainstTarget(enemy, battle, tauntingPlayer.Id);
                }
            }

            switch (enemy.Behavior)
            {
                case EnemyBehavior.Aggressive:
                    return DecideAggressiveAction(enemy, battle);

                case EnemyBehavior.Defensive:
                    return DecideDefensiveAction(enemy, battle);

                case EnemyBehavior.Smart:
                    return DecideSmartAction(enemy, battle);

                case EnemyBehavior.Coward:
                    return DecideCowardAction(enemy, battle);

                case EnemyBehavior.Random:
                default:
                    return DecideRandomAction(enemy, battle);
            }
        }

        private Action DecideActionAgainstTarget(Enemy enemy, Battle battle, string targetId)
        {
            var availableSkills = enemy.Skills.Where(s => s.CanUse(enemy, battle)).ToList();

            if (availableSkills.Count > 0 && random.NextDouble() < 0.7)
            {
                var skill = availableSkills[random.Next(availableSkills.Count)];
                var validTargets = skill.GetValidTargets(enemy, battle);

                if (validTargets.Contains(targetId))
                {
                    return new Action
                    {
                        Type = ActionType.Skill,
                        SkillId = skill.Id,
                        TargetId = targetId
                    };
                }
            }

            return new Action
            {
                Type = ActionType.Attack,
                TargetId = targetId
            };
        }

        private Action DecideAggressiveAction(Enemy enemy, Battle battle)
        {
            var players = battle.Teams.SelectMany(t => t.Players).Where(p => p.IsAlive).ToList();
            if (players.Count == 0)
                return new Action { Type = ActionType.Pass };

            var lowHealthPlayers = players.Where(p => p.Health < p.MaxHealth * 0.3f).ToList();

            if (lowHealthPlayers.Count > 0)
            {
                var executeSkills = enemy.Skills.Where(s => s.Type == SkillType.Execute && s.CanUse(enemy, battle)).ToList();
                if (executeSkills.Count > 0)
                {
                    var weakestTarget = lowHealthPlayers.OrderBy(p => p.Health).FirstOrDefault() ?? players.OrderBy(p => p.Health).First();
                    return new Action
                    {
                        Type = ActionType.Skill,
                        SkillId = executeSkills.First().Id,
                        TargetId = weakestTarget.Id
                    };
                }
            }

            // Atacar jogador com menor vida, ou qualquer jogador se todos estiverem saud�veis
            var target = lowHealthPlayers.Count > 0 
                ? lowHealthPlayers.OrderBy(p => p.Health).First() 
                : players.OrderBy(p => p.Health).First();

            // Tenta usar uma habilidade ofensiva se dispon�vel
            var damageSkills = enemy.Skills.Where(s => s.Damage > 0 && s.CanUse(enemy, battle)).ToList();
            if (damageSkills.Count > 0 && random.NextDouble() < 0.7)
            {
                var skill = damageSkills[random.Next(damageSkills.Count)];
                return new Action
                {
                    Type = ActionType.Skill,
                    SkillId = skill.Id,
                    TargetId = target.Id
                };
            }

            // Retorna um ataque b�sico como fallback
            return new Action
            {
                Type = ActionType.Attack,
                TargetId = target.Id
            };
        }

        private Action DecideDefensiveAction(Enemy enemy, Battle battle)
        {
            if (enemy.Health < enemy.MaxHealth * 0.5f)
            {
                var healingSkills = enemy.Skills.Where(s => s.Healing > 0 && s.CanUse(enemy, battle)).ToList();
                if (healingSkills.Count > 0)
                {
                    return new Action
                    {
                        Type = ActionType.Skill,
                        SkillId = healingSkills.First().Id,
                        TargetId = enemy.Id
                    };
                }

                if (random.NextDouble() < 0.4)
                {
                    return new Action { Type = ActionType.Defend };
                }
            }

            return DecideAggressiveAction(enemy, battle);
        }

        private Action DecideSmartAction(Enemy enemy, Battle battle)
        {
            var players = battle.Teams.SelectMany(t => t.Players).Where(p => p.IsAlive).ToList();
            if (players.Count == 0)
                return new Action { Type = ActionType.Pass };

            // Prioritize healers
            var healers = players.Where(p => p.Class.ToLower() == "healer").ToList();
            if (healers.Count > 0)
            {
                var target = healers.OrderBy(p => p.Health).First();
                var bestSkill = enemy.Skills
                    .Where(s => s.Damage > 0 && s.CanUse(enemy, battle))
                    .OrderByDescending(s => s.Damage)
                    .FirstOrDefault();

                if (bestSkill != null)
                {
                    return new Action
                    {
                        Type = ActionType.Skill,
                        SkillId = bestSkill.Id,
                        TargetId = target.Id
                    };
                }
            }

            // Then prioritize low health targets
            var lowHealthTargets = players.Where(p => p.Health < p.MaxHealth * 0.4f).ToList();
            if (lowHealthTargets.Count > 0)
            {
                var target = lowHealthTargets.OrderBy(p => p.Health).First();
                return new Action
                {
                    Type = ActionType.Attack,
                    TargetId = target.Id
                };
            }

            return DecideAggressiveAction(enemy, battle);
        }

        private Action DecideCowardAction(Enemy enemy, Battle battle)
        {
            if (enemy.Health < enemy.MaxHealth * 0.3f)
            {
                if (random.NextDouble() < 0.6)
                {
                    return new Action { Type = ActionType.Flee };
                }
            }

            if (enemy.Health < enemy.MaxHealth * 0.7f)
            {
                if (random.NextDouble() < 0.4)
                {
                    return new Action { Type = ActionType.Defend };
                }
            }

            return DecideAggressiveAction(enemy, battle);
        }

        private Action DecideRandomAction(Enemy enemy, Battle battle)
        {
            var actions = new List<ActionType> { ActionType.Attack };

            if (enemy.Skills.Any(s => s.CanUse(enemy, battle)))
                actions.Add(ActionType.Skill);

            if (random.NextDouble() < 0.1)
                actions.Add(ActionType.Defend);

            if (random.NextDouble() < 0.05)
                actions.Add(ActionType.Pass);

            var chosenAction = actions[random.Next(actions.Count)];

            switch (chosenAction)
            {
                case ActionType.Skill:
                    var availableSkills = enemy.Skills.Where(s => s.CanUse(enemy, battle)).ToList();
                    if (availableSkills.Count > 0)
                    {
                        var skill = availableSkills[random.Next(availableSkills.Count)];
                        var validTargets = skill.GetValidTargets(enemy, battle);
                        if (validTargets.Count > 0)
                        {
                            return new Action
                            {
                                Type = ActionType.Skill,
                                SkillId = skill.Id,
                                TargetId = validTargets[random.Next(validTargets.Count)]
                            };
                        }
                    }
                    goto case ActionType.Attack;

                case ActionType.Attack:
                    var players = battle.Teams.SelectMany(t => t.Players).Where(p => p.IsAlive).ToList();
                    if (players.Count > 0)
                    {
                        var target = players[random.Next(players.Count)];
                        return new Action
                        {
                            Type = ActionType.Attack,
                            TargetId = target.Id
                        };
                    }
                    return new Action { Type = ActionType.Pass };

                default:
                    return new Action { Type = chosenAction };
            }
        }
    }
}