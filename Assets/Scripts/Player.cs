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
        
        // Experiência necessária para subir de nível
        public int ExperienceToNextLevel => Level * 100;
        
        // Método para adicionar experiência e verificar level up
        public bool AddExperience(int amount)
        {
            Experience += amount;
            bool leveledUp = false;
            
            // Verifica se subiu de nível
            while (Experience >= ExperienceToNextLevel)
            {
                LevelUp();
                leveledUp = true;
            }
            
            return leveledUp;
        }
        
        // Método para subir de nível com bônus específicos por classe
        private void LevelUp()
        {
            Level++;
            
            // Bônus específicos por classe
            switch (Class.ToLower())
            {
                case "warrior":
                    // Warrior ganha mais HP e Ataque por nível
                    MaxHealth += 30;
                    Health += 30;
                    Attack += 5;
                    Defense += 3;
                    MaxMana += 5;
                    Mana += 5;
                    
                    // Adiciona novas habilidades em níveis específicos
                    if (Level == 3 && !HasSkill("skill_warrior_slam"))
                    {
                        Skills.Add(new Skill
                        {
                            Id = "skill_warrior_slam",
                            Name = "Golpe Esmagador",
                            Description = "Ataca vários inimigos próximos",
                            Damage = 80,
                            ManaCost = 25,
                            Range = 1,
                            AffectsTeam = true
                        });
                    }
                    if (Level == 5 && !HasSkill("skill_warrior_berserk"))
                    {
                        Skills.Add(new Skill
                        {
                            Id = "skill_warrior_berserk",
                            Name = "Fúria Guerreira",
                            Description = "Aumenta o ataque em troca de defesa",
                            Damage = 0,
                            ManaCost = 30,
                            Range = 0
                        });
                    }
                    break;
                    
                case "mage":
                    // Mage ganha mais Mana e poder mágico (dano) por nível
                    MaxHealth += 15;
                    Health += 15;
                    Attack += 2;
                    Defense += 1;
                    MaxMana += 20;
                    Mana += 20;
                    
                    // Aumenta o dano de todas as habilidades existentes
                    foreach (var skill in Skills)
                    {
                        if (skill.Damage > 0)
                        {
                            skill.Damage += 5;
                        }
                    }
                    
                    // Adiciona novas habilidades em níveis específicos
                    if (Level == 3 && !HasSkill("skill_mage_frostbolt"))
                    {
                        Skills.Add(new Skill
                        {
                            Id = "skill_mage_frostbolt",
                            Name = "Raio de Gelo",
                            Description = "Dispara um raio congelante",
                            Damage = 90,
                            ManaCost = 25,
                            Range = 3
                        });
                    }
                    if (Level == 5 && !HasSkill("skill_mage_meteor"))
                    {
                        Skills.Add(new Skill
                        {
                            Id = "skill_mage_meteor",
                            Name = "Chuva de Meteoros",
                            Description = "Invoca meteoros que atingem todos os inimigos",
                            Damage = 150,
                            ManaCost = 50,
                            Range = 4,
                            AffectsTeam = true
                        });
                    }
                    break;
                    
                case "healer":
                    // Healer ganha equilíbrio entre HP/MP e poder de cura
                    MaxHealth += 20;
                    Health += 20;
                    Attack += 1;
                    Defense += 2;
                    MaxMana += 15;
                    Mana += 15;
                    
                    // Aumenta a cura de todas as habilidades existentes
                    foreach (var skill in Skills)
                    {
                        if (skill.Healing > 0)
                        {
                            skill.Healing += 8;
                        }
                    }
                    
                    // Adiciona novas habilidades em níveis específicos
                    if (Level == 3 && !HasSkill("skill_healer_mass_heal"))
                    {
                        Skills.Add(new Skill
                        {
                            Id = "skill_healer_mass_heal",
                            Name = "Cura em Massa",
                            Description = "Cura todos os aliados próximos",
                            Healing = 70,
                            ManaCost = 35,
                            Range = 3,
                            AffectsTeam = true
                        });
                    }
                    if (Level == 5 && !HasSkill("skill_healer_resurrection"))
                    {
                        Skills.Add(new Skill
                        {
                            Id = "skill_healer_resurrection",
                            Name = "Ressurreição",
                            Description = "Revive um aliado caído com parte da vida",
                            Healing = 150,
                            ManaCost = 60,
                            Range = 2
                        });
                    }
                    break;
                    
                default:
                    // Classe padrão - crescimento balanceado
                    MaxHealth += 20;
                    Health += 20;
                    Attack += 3;
                    Defense += 2;
                    MaxMana += 10;
                    Mana += 10;
                    break;
            }
        }
        
        // Verifica se o jogador já possui determinada habilidade
        private bool HasSkill(string skillId)
        {
            return Skills.Exists(s => s.Id == skillId);
        }
        
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