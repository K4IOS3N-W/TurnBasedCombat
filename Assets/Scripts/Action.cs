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
    }

    public enum ActionType
    {
        Attack,
        Skill,
        Item,
        Pass
    }
}