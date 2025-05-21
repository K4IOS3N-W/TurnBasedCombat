using System;

namespace BattleSystem
{
    [Serializable]
    public class ActionResult
    {
        public string TargetId { get; set; }
        public int DamageReceived { get; set; }
        public int HealingReceived { get; set; }
        public bool IsDead { get; set; }
    }
}