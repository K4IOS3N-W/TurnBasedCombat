using System;

namespace BattleSystem
{
    [Serializable]
    public class Action
    {
        public ActionType Type { get; set; }
        public string TargetId { get; set; }
        public string SkillId { get; set; }
        public string ItemId { get; set; }

        // Parâmetros adicionais (opcionais)
        public object Parameters { get; set; }
    }

    [Serializable]
    public enum ActionType
    {
        Attack,     // Ataque básico
        Skill,      // Usar habilidade
        Item,       // Usar item
        Pass,       // Passar turno
        Defend,     // Defender (aumenta defesa temporariamente)
        Flee        // Tentar fugir da batalha
    }
}