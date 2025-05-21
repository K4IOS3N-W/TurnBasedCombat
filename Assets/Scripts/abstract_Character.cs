using System;

namespace BattleSystem
{
    [Serializable]
    public abstract class Character
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Speed { get; set; }
        
        public bool IsAlive => Health > 0;
    }
}