using System;
using System.Collections.Generic;
using System.Linq;

namespace BattleSystem
{
    [Serializable]
    public class Battle
    {
        public string Id { get; set; }
        public string RoomCode { get; set; }
        public List<Team> Teams { get; set; } = new List<Team>();
        public List<Enemy> Enemies { get; set; } = new List<Enemy>();
        public BattleState State { get; set; } = BattleState.Waiting;
        public int CurrentTurn { get; set; } = 0;
        public List<Character> TurnOrder { get; set; } = new List<Character>();
        public int CurrentCharacterIndex { get; set; } = 0;
        public bool IsPvP { get; set; } = false;
        public string NodeId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string WinnerTeamId { get; set; }
        public bool CanBeInvaded { get; set; } = true;
        public bool HasBeenInvaded { get; set; } = false;
        public string InvadingTeamId { get; set; }
        public Dictionary<string, object> BattleSettings { get; set; } = new Dictionary<string, object>();

        public Character CurrentCharacter => TurnOrder.Count > 0 && CurrentCharacterIndex < TurnOrder.Count 
            ? TurnOrder[CurrentCharacterIndex] : null;

        public bool IsActive => State == BattleState.InProgress;
        public TimeSpan BattleDuration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.Now - StartTime;

        public Battle()
        {
            Id = Guid.NewGuid().ToString();
            StartTime = DateTime.Now;
        }

        public Battle(string id, string roomCode) : this()
        {
            Id = id;
            RoomCode = roomCode;
        }

        public void AddTeam(Team team)
        {
            if (Teams.Count >= 4)
            {
                throw new InvalidOperationException("Battle is full (maximum 4 teams)");
            }

            Teams.Add(team);
            
            if (Teams.Count > 1)
            {
                IsPvP = true;
            }
        }

        public void AddEnemies(List<Enemy> enemies)
        {
            Enemies.AddRange(enemies);
            IsPvP = false;
        }

        public bool CanInvade()
        {
            return CanBeInvaded && !HasBeenInvaded && IsActive && Teams.Count < 3;
        }

        public bool AddInvadingTeam(Team invadingTeam)
        {
            if (!CanInvade())
                return false;

            Teams.Add(invadingTeam);
            InvadingTeamId = invadingTeam.Id;
            HasBeenInvaded = true;
            IsPvP = true;

            // Apply third party invasion buff to weakest team
            var weakestTeam = GetWeakestTeam();
            if (weakestTeam != null && weakestTeam.Id != invadingTeam.Id)
            {
                ApplyThirdPartyBuff(weakestTeam);
            }

            // Recalculate turn order
            CalculateTurnOrder();
            return true;
        }

        private Team GetWeakestTeam()
        {
            if (Teams.Count < 2) return null;
            
            var nonInvadingTeams = Teams.Where(t => t.Id != InvadingTeamId).ToList();
            return nonInvadingTeams.OrderBy(t => t.TotalHealth).FirstOrDefault();
        }

        private void ApplyThirdPartyBuff(Team team)
        {
            team.TeamBuffs["ThirdPartyDefenseBuff"] = true;
            foreach (var player in team.Players)
            {
                player.Defense += 5;
            }
        }

        public void Start()
        {
            if (State != BattleState.Waiting)
            {
                throw new InvalidOperationException("Battle has already started");
            }

            State = BattleState.InProgress;
            CalculateTurnOrder();
            CurrentTurn = 1;
            CurrentCharacterIndex = 0;
        }

        public void CalculateTurnOrder()
        {
            TurnOrder.Clear();

            // Add all alive players
            foreach (var team in Teams)
            {
                TurnOrder.AddRange(team.Players.Where(p => p.IsAlive));
            }

            // Add all alive enemies
            TurnOrder.AddRange(Enemies.Where(e => e.IsAlive));

            // Sort by speed (highest first)
            TurnOrder = TurnOrder.OrderByDescending(c => c.GetModifiedSpeed()).ToList();
        }

        public Character GetNextCharacter()
        {
            if (TurnOrder.Count == 0) return null;

            CurrentCharacterIndex++;
            if (CurrentCharacterIndex >= TurnOrder.Count)
            {
                CurrentCharacterIndex = 0;
                CurrentTurn++;
                ProcessTurnEffects();
            }

            // Skip dead characters
            while (CurrentCharacterIndex < TurnOrder.Count && !TurnOrder[CurrentCharacterIndex].IsAlive)
            {
                CurrentCharacterIndex++;
                if (CurrentCharacterIndex >= TurnOrder.Count)
                {
                    CurrentCharacterIndex = 0;
                    CurrentTurn++;
                    ProcessTurnEffects();
                }
            }

            return CurrentCharacter;
        }

        private void ProcessTurnEffects()
        {
            // Process status effects for all characters
            foreach (var character in TurnOrder)
            {
                character.ProcessStatusEffects();
            }

            // Remove dead characters from turn order
            TurnOrder.RemoveAll(c => !c.IsAlive);

            // Check for battle end
            CheckBattleEnd();
        }

        public BattleResult ProcessAction(string characterId, Action action)
        {
            var character = TurnOrder.FirstOrDefault(c => c.Id == characterId);
            if (character == null || character != CurrentCharacter)
            {
                return new BattleResult
                {
                    Success = false,
                    Message = "Not your turn or character not found"
                };
            }

            var result = ExecuteAction(character, action);
            
            if (result.Success)
            {
                GetNextCharacter();
            }

            CheckBattleEnd();
            return result;
        }

        private BattleResult ExecuteAction(Character actor, Action action)
        {
            switch (action.Type)
            {
                case ActionType.Attack:
                    return ExecuteAttack(actor, action.TargetId);
                case ActionType.Skill:
                    return ExecuteSkill(actor, action.SkillId, action.TargetId);
                case ActionType.Defend:
                    return ExecuteDefend(actor);
                case ActionType.Pass:
                    return ExecutePass(actor);
                case ActionType.Flee:
                    return ExecuteFlee(actor);
                default:
                    return new BattleResult
                    {
                        Success = false,
                        Message = "Unknown action type"
                    };
            }
        }

        private BattleResult ExecuteAttack(Character attacker, string targetId)
        {
            var target = GetCharacterById(targetId);
            if (target == null || !target.IsAlive)
            {
                return new BattleResult
                {
                    Success = false,
                    Message = "Invalid target"
                };
            }

            int damage = CalculateDamage(attacker.GetModifiedAttack(), target.GetModifiedDefense());
            target.TakeDamage(damage);

            return new BattleResult
            {
                Success = true,
                Message = $"{attacker.Name} attacks {target.Name} for {damage} damage",
                ActionResults = new List<ActionResult>
                {
                    new ActionResult
                    {
                        AttackerId = attacker.Id,
                        TargetId = target.Id,
                        DamageReceived = damage,
                        IsDead = !target.IsAlive,
                        Message = $"{attacker.Name} attacks {target.Name} for {damage} damage"
                    }
                }
            };
        }

        private BattleResult ExecuteSkill(Character caster, string skillId, string targetId)
        {
            Skill skill = null;
            
            if (caster is Player player)
            {
                skill = player.Skills.FirstOrDefault(s => s.Id == skillId);
            }
            else if (caster is Enemy enemy)
            {
                skill = enemy.Skills.FirstOrDefault(s => s.Id == skillId);
            }

            if (skill == null || !skill.CanUse(caster, this))
            {
                return new BattleResult
                {
                    Success = false,
                    Message = "Cannot use this skill"
                };
            }

            var targets = GetTargetsForSkill(skill, caster, targetId);
            if (targets.Count == 0)
            {
                return new BattleResult
                {
                    Success = false,
                    Message = "No valid targets"
                };
            }

            skill.Use();
            if (caster is Player p)
            {
                p.UseMana(skill.ManaCost);
            }

            var results = new List<ActionResult>();
            foreach (var target in targets)
            {
                if (skill.Damage > 0)
                {
                    int damage = CalculateSkillDamage(skill, caster, target);
                    target.TakeDamage(damage);
                    
                    results.Add(new ActionResult
                    {
                        AttackerId = caster.Id,
                        TargetId = target.Id,
                        DamageReceived = damage,
                        IsDead = !target.IsAlive,
                        Message = $"{caster.Name} uses {skill.Name} on {target.Name} for {damage} damage"
                    });
                }

                if (skill.Healing > 0)
                {
                    target.Heal(skill.Healing);
                    
                    results.Add(new ActionResult
                    {
                        AttackerId = caster.Id,
                        TargetId = target.Id,
                        HealingReceived = skill.Healing,
                        Message = $"{caster.Name} heals {target.Name} for {skill.Healing} health"
                    });
                }

                foreach (var effect in skill.StatusEffects)
                {
                    target.ApplyStatusEffect(effect, caster.Id);
                }
            }

            return new BattleResult
            {
                Success = true,
                Message = $"{caster.Name} uses {skill.Name}",
                ActionResults = results
            };
        }

        private BattleResult ExecuteDefend(Character character)
        {
            character.Defense += 5;
            
            return new BattleResult
            {
                Success = true,
                Message = $"{character.Name} defends",
                ActionResults = new List<ActionResult>
                {
                    new ActionResult
                    {
                        TargetId = character.Id,
                        Message = $"{character.Name} increases defense"
                    }
                }
            };
        }

        private BattleResult ExecutePass(Character character)
        {
            return new BattleResult
            {
                Success = true,
                Message = $"{character.Name} passes their turn"
            };
        }

        private BattleResult ExecuteFlee(Character character)
        {
            if (IsPvP)
            {
                return new BattleResult
                {
                    Success = false,
                    Message = "Cannot flee from PvP battle"
                };
            }

            // Remove character from battle
            TurnOrder.Remove(character);
            
            return new BattleResult
            {
                Success = true,
                Message = $"{character.Name} flees from battle"
            };
        }

        private List<Character> GetTargetsForSkill(Skill skill, Character caster, string primaryTargetId)
        {
            var targets = new List<Character>();
            
            switch (skill.TargetType)
            {
                case TargetType.Self:
                    targets.Add(caster);
                    break;

                case TargetType.Single:
                    var target = GetCharacterById(primaryTargetId);
                    if (target != null && target.IsAlive)
                        targets.Add(target);
                    break;

                case TargetType.AllEnemies:
                case TargetType.AllAllies:
                case TargetType.Area:
                    var validTargets = skill.GetValidTargets(caster, this);
                    targets.AddRange(validTargets.Select(GetCharacterById).Where(c => c != null && c.IsAlive));
                    break;

                default:
                    var singleTarget = GetCharacterById(primaryTargetId);
                    if (singleTarget != null && singleTarget.IsAlive)
                        targets.Add(singleTarget);
                    break;
            }

            return targets;
        }

        private int CalculateDamage(int attack, int defense)
        {
            return Math.Max(1, attack - defense);
        }

        private int CalculateSkillDamage(Skill skill, Character caster, Character target)
        {
            int baseDamage = skill.Damage + (caster.GetModifiedAttack() / 2);
            int finalDamage = baseDamage - target.GetModifiedDefense();
            
            if (target is Enemy enemy && enemy.ElementalResistances.ContainsKey(skill.Element))
            {
                float resistance = enemy.ElementalResistances[skill.Element];
                finalDamage = (int)(finalDamage * (1.0f - resistance));
            }

            return Math.Max(1, finalDamage);
        }

        public Character GetCharacterById(string id)
        {
            return TurnOrder.FirstOrDefault(c => c.Id == id);
        }

        private void CheckBattleEnd()
        {
            if (State != BattleState.InProgress)
                return;

            if (IsPvP)
            {
                var aliveTeams = Teams.Where(t => t.IsAlive).ToList();
                if (aliveTeams.Count <= 1)
                {
                    EndBattle(aliveTeams.FirstOrDefault()?.Id);
                }
            }
            else
            {
                bool playersAlive = Teams.Any(t => t.IsAlive);
                bool enemiesAlive = Enemies.Any(e => e.IsAlive);

                if (!playersAlive)
                {
                    EndBattle(null);
                }
                else if (!enemiesAlive)
                {
                    EndBattle(Teams.FirstOrDefault()?.Id);
                }
            }
        }

        private void EndBattle(string winnerTeamId)
        {
            State = BattleState.Finished;
            EndTime = DateTime.Now;
            WinnerTeamId = winnerTeamId;
        }

        public BattleInfo GetBattleInfo()
        {
            return new BattleInfo
            {
                Id = Id,
                RoomCode = RoomCode,
                State = State,
                IsPvP = IsPvP,
                CurrentTurn = CurrentTurn,
                TeamsCount = Teams.Count,
                EnemiesCount = Enemies.Count,
                CanBeInvaded = CanBeInvaded,
                HasBeenInvaded = HasBeenInvaded,
                Duration = BattleDuration
            };
        }
    }

    [Serializable]
    public enum BattleState
    {
        Waiting,
        InProgress,
        Finished,
        Cancelled
    }

    [Serializable]
    public class BattleResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<ActionResult> ActionResults { get; set; } = new List<ActionResult>();
    }

    [Serializable]
    public class BattleInfo
    {
        public string Id { get; set; }
        public string RoomCode { get; set; }
        public BattleState State { get; set; }
        public bool IsPvP { get; set; }
        public int CurrentTurn { get; set; }
        public int TeamsCount { get; set; }
        public int EnemiesCount { get; set; }
        public bool CanBeInvaded { get; set; }
        public bool HasBeenInvaded { get; set; }
        public TimeSpan Duration { get; set; }
    }
}