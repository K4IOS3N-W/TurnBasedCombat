using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BattleSystem.UI
{
    public class BattleArenaManager : MonoBehaviour
    {
        [Header("Times")]
        [SerializeField] private Transform[] teamASlots = new Transform[4]; // Slots vermelhos (esquerda)
        [SerializeField] private Transform[] teamBSlots = new Transform[4]; // Slots verdes (direita)
        
        [Header("Prefabs")]
        [SerializeField] private GameObject warriorPrefab;
        [SerializeField] private GameObject magePrefab;
        [SerializeField] private GameObject healerPrefab;
        [SerializeField] private GameObject defaultEnemyPrefab;
        
        [Header("Materiais")]
        [SerializeField] private Material teamAMaterial; // Material vermelho
        [SerializeField] private Material teamBMaterial; // Material verde
        [SerializeField] private Material enemyMaterial; // Material para inimigos
        
        // Dicionário para armazenar referências aos objetos na arena
        private Dictionary<string, GameObject> characterObjects = new Dictionary<string, GameObject>();
        private Battle currentBattle;

        // Singleton para acesso fácil
        public static BattleArenaManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Configura a arena com os personagens da batalha atual
        /// </summary>
        public void SetupArena(Battle battle)
        {
            // Limpar arena antes de configurar
            ClearArena();
            
            currentBattle = battle;
            
            // Se for modo PvP
            if (battle.IsPvP)
            {
                SetupPvPArena(battle);
            }
            else // Modo PvE
            {
                SetupPvEArena(battle);
            }
        }
        
        private void SetupPvPArena(Battle battle)
        {
            if (battle.Teams.Count < 2) return;
            
            // Pegar duas equipes para mostrar na arena
            var teamA = battle.Teams[0];
            var teamB = battle.Teams[1];
            
            // Posicionar jogadores da equipe A
            PositionTeam(teamA, teamASlots, teamAMaterial);
            
            // Posicionar jogadores da equipe B
            PositionTeam(teamB, teamBSlots, teamBMaterial);
        }
        
        private void SetupPvEArena(Battle battle)
        {
            if (battle.Teams.Count == 0) return;
            
            // Posicionar jogadores (todos na mesma equipe)
            var playerTeam = battle.Teams[0];
            PositionTeam(playerTeam, teamBSlots, teamBMaterial);
            
            // Posicionar inimigos nos slots da equipe A
            PositionEnemies(battle.Enemies, teamASlots, enemyMaterial);
        }
        
        private void PositionTeam(Team team, Transform[] slots, Material teamMaterial)
        {
            int slotIndex = 0;
            
            foreach (var player in team.Players)
            {
                if (slotIndex >= slots.Length) break;
                
                // Escolher prefab baseado na classe do jogador
                GameObject prefab = GetPrefabForClass(player.Class);
                
                // Instanciar personagem no slot
                GameObject character = Instantiate(prefab, slots[slotIndex].position, slots[slotIndex].rotation);
                
                // Aplicar material da equipe
                ApplyMaterialToCharacter(character, teamMaterial);
                
                // Configurar nome do jogador
                SetCharacterName(character, player.Name);
                
                // Guardar referência
                characterObjects[player.Id] = character;
                
                slotIndex++;
            }
        }
        
        private void PositionEnemies(List<Enemy> enemies, Transform[] slots, Material enemyMaterial)
        {
            int slotIndex = 0;
            
            foreach (var enemy in enemies)
            {
                if (slotIndex >= slots.Length) break;
                
                // Instanciar inimigo no slot
                GameObject character = Instantiate(defaultEnemyPrefab, slots[slotIndex].position, slots[slotIndex].rotation);
                
                // Aplicar material de inimigo
                ApplyMaterialToCharacter(character, enemyMaterial);
                
                // Configurar nome do inimigo
                SetCharacterName(character, enemy.Name);
                
                // Guardar referência
                characterObjects[enemy.Id] = character;
                
                slotIndex++;
            }
        }
        
        private GameObject GetPrefabForClass(string className)
        {
            switch (className.ToLower())
            {
                case "warrior":
                    return warriorPrefab != null ? warriorPrefab : defaultEnemyPrefab;
                case "mage":
                    return magePrefab != null ? magePrefab : defaultEnemyPrefab;
                case "healer":
                    return healerPrefab != null ? healerPrefab : defaultEnemyPrefab;
                default:
                    return defaultEnemyPrefab;
            }
        }
        
        private void ApplyMaterialToCharacter(GameObject character, Material material)
        {
            // Aplicar material aos renderers do personagem
            var renderers = character.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = material;
            }
        }
        
        private void SetCharacterName(GameObject character, string name)
        {
            // Adicionar um TextMesh para o nome se necessário
            // Por simplicidade, não implementaremos isto agora
        }
        
        /// <summary>
        /// Limpar todos os personagens da arena
        /// </summary>
        public void ClearArena()
        {
            foreach (var obj in characterObjects.Values)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            
            characterObjects.Clear();
        }
        
        /// <summary>
        /// Destacar o personagem que está no turno atual
        /// </summary>
        public void HighlightCurrentTurn(string characterId)
        {
            // Resetar highlight em todos
            foreach (var obj in characterObjects.Values)
            {
                if (obj != null)
                {
                    // Remover efeito de highlight (implementação simplificada)
                    obj.transform.localScale = Vector3.one;
                }
            }
            
            // Aplicar highlight no personagem atual
            if (characterObjects.TryGetValue(characterId, out GameObject currentCharacter))
            {
                // Fazer personagem um pouco maior para indicar turno atual
                currentCharacter.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            }
        }
        
        /// <summary>
        /// Atualizar estado visual dos personagens (vida, etc)
        /// </summary>
        public void UpdateCharactersState(Battle battle)
        {
            // Atualizar estado dos jogadores
            foreach (var team in battle.Teams)
            {
                foreach (var player in team.Players)
                {
                    if (characterObjects.TryGetValue(player.Id, out GameObject character))
                    {
                        // Atualizar visual baseado no estado do jogador
                        UpdateCharacterVisuals(character, player.IsAlive, (float)player.Health / player.MaxHealth);
                    }
                }
            }
            
            // Atualizar estado dos inimigos
            foreach (var enemy in battle.Enemies)
            {
                if (characterObjects.TryGetValue(enemy.Id, out GameObject character))
                {
                    // Atualizar visual baseado no estado do inimigo
                    UpdateCharacterVisuals(character, enemy.IsAlive, (float)enemy.Health / enemy.MaxHealth);
                }
            }
        }
        
        private void UpdateCharacterVisuals(GameObject character, bool isAlive, float healthPercent)
        {
            if (!isAlive)
            {
                // Personagem morto - deitar na arena
                character.transform.rotation = Quaternion.Euler(90, 0, 0);
            }
            else
            {
                // Personagem vivo - ajustar escala com base na saúde
                float scaleY = Mathf.Max(0.5f, healthPercent);
                Vector3 scale = character.transform.localScale;
                character.transform.localScale = new Vector3(scale.x, scaleY, scale.z);
            }
        }
    }
}