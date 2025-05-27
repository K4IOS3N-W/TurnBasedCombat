using System;
using System.Collections.Generic;

namespace BattleSystem
{
    [Serializable]
    public class ActionResult
    {
        public string TargetId { get; set; }
        public string AttackerId { get; set; }
        public int DamageReceived { get; set; }
        public int HealingReceived { get; set; }
        public bool IsDead { get; set; }
        public bool IsCritical { get; set; }
        public string EffectApplied { get; set; }

        // Mensagem para comunicar o resultado ao cliente
        public string Message { get; set; }

        // Dados adicionais
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }
}