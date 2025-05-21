using System;
using System.Collections.Generic;

namespace BattleSystem
{
    [Serializable]
    public class Player : Character
    {
        public string Class { get; set; }
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int Mana { get; set; }
        public int MaxMana { get; set; }
        public List<Skill> Skills { get; set; } = new List<Skill>();
        
        public static Player CreatePlayer(string id, string name, string characterClass)
        {
            var player = new Player
            {
                Id = id,
                Name = name,
                Class = characterClass
            };
            
            // Configurações iniciais baseadas na classe
            switch (characterClass.ToLower())
            {
                case "warrior":
                    player.Health = 500;
                    player.MaxHealth = 500;
                    player.Attack = 80;
                    player.Defense = 70;
                    player.Speed = 50;
                    player.Mana = 100;
                    player.MaxMana = 100;
                    player.Skills.Add(new Skill 
                    { 
                        Id = "skill_warrior_strike", 
                        Name = "Golpe Poderoso", 
                        Description = "Um ataque forte que causa dano elevado",
                        Damage = 100,
                        ManaCost = 20,
                        Range = 1
                    });
                    break;
                    
                case "mage":
                    player.Health = 300;
                    player.MaxHealth = 300;
                    player.Attack = 40;
                    player.Defense = 30;
                    player.Speed = 60;
                    player.Mana = 300;
                    player.MaxMana = 300;
                    player.Skills.Add(new Skill 
                    { 
                        Id = "skill_mage_fireball", 
                        Name = "Bola de Fogo", 
                        Description = "Lança uma poderosa bola de fogo",
                        Damage = 120,
                        ManaCost = 30,
                        Range = 3
                    });
                    break;
                    
                case "healer":
                    player.Health = 350;
                    player.MaxHealth = 350;
                    player.Attack = 30;
                    player.Defense = 40;
                    player.Speed = 55;
                    player.Mana = 250;
                    player.MaxMana = 250;
                    player.Skills.Add(new Skill 
                    { 
                        Id = "skill_healer_heal", 
                        Name = "Cura", 
                        Description = "Restaura a vida de um aliado",
                        Healing = 100,
                        ManaCost = 25,
                        Range = 2
                    });
                    break;
                    
                default:
                    // Classe padrão - Aventureiro
                    player.Health = 400;
                    player.MaxHealth = 400;
                    player.Attack = 60;
                    player.Defense = 50;
                    player.Speed = 60;
                    player.Mana = 150;
                    player.MaxMana = 150;
                    player.Skills.Add(new Skill 
                    { 
                        Id = "skill_adventurer_slash", 
                        Name = "Corte Rápido", 
                        Description = "Um ataque rápido que causa dano moderado",
                        Damage = 70,
                        ManaCost = 15,
                        Range = 1
                    });
                    break;
            }
            
            return player;
        }
    }
}