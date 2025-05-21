using System;
using System.Collections.Generic;

namespace BattleSystem
{
    [Serializable]
    public class Enemy : Character
    {
        public List<Skill> Skills { get; set; } = new List<Skill>();
    }
}