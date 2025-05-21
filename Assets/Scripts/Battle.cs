using System;
using System.Collections.Generic;

namespace BattleSystem
{
    [Serializable]
    public class Battle
    {
        public string Id { get; set; }
        public string RoomCode { get; set; } // C�digo de sala de 5 d�gitos
        public BattleState State { get; set; }
        public List<Team> Teams { get; set; } = new List<Team>();
        public List<Enemy> Enemies { get; set; } = new List<Enemy>();
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public List<string> TurnOrder { get; set; } = new List<string>();
        public int CurrentTurn { get; set; }
        public bool IsPvP { get; set; }
        public string WinnerTeamId { get; set; }

        // Status de prontid�o das equipes
        public Dictionary<string, bool> TeamReadyStatus { get; set; } = new Dictionary<string, bool>();

        public string CurrentParticipant =>
            TurnOrder.Count > 0 ? TurnOrder[CurrentTurn % TurnOrder.Count] : null;
    }

    public enum BattleState
    {
        Preparation,
        InProgress,
        Finished
    }
}