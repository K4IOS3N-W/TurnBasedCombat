using System;

namespace BattleSystem
{
    [Serializable]
    public class Skill
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Damage { get; set; }
        public int Healing { get; set; }
        public int ManaCost { get; set; }
        public int Range { get; set; }
        public bool AffectsTeam { get; set; }
    }
}