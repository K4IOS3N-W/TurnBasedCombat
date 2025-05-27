using System;
using System.Collections.Generic;

namespace BattleSystem
{
    [Serializable]
    public class Battle
    {
        public string Id { get; set; }
        public string RoomCode { get; set; } // Código de sala de 5 dígitos
        public BattleState State { get; set; }
        public List<Team> Teams { get; set; } = new List<Team>();
        public List<Enemy> Enemies { get; set; } = new List<Enemy>();
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public List<string> TurnOrder { get; set; } = new List<string>();
        public int CurrentTurn { get; set; }
        public bool IsPvP { get; set; }
        public string WinnerTeamId { get; set; }

        // Status de prontidão das equipes
        public Dictionary<string, bool> TeamReadyStatus { get; set; } = new Dictionary<string, bool>();
        
        // Lista de invocações (summons)
        public List<Summon> Summons { get; set; } = new List<Summon>();
        
        // Histórico de ações
        public List<BattleAction> ActionHistory { get; set; } = new List<BattleAction>();

        public string CurrentParticipant =>
            TurnOrder.Count > 0 ? TurnOrder[CurrentTurn % TurnOrder.Count] : null;
    }
    
    [Serializable]
    public enum BattleState
    {
        Preparation,
        InProgress,
        Finished
    }
    
    [Serializable]
    public class Summon
    {
        public string CreatureId { get; set; }  // ID da criatura invocada
        public string OwnerId { get; set; }     // ID do jogador que a invocou
        public int Duration { get; set; }       // Duração em turnos
    }
    
    [Serializable]
    public class BattleAction
    {
        public string ActorId { get; set; }
        public int Turn { get; set; }
        public DateTime Timestamp { get; set; }
        public ActionType ActionType { get; set; }
        public string TargetId { get; set; }
        public string SkillId { get; set; }
        public string ItemId { get; set; }
        public List<ActionResult> Results { get; set; } = new List<ActionResult>();
    }
}