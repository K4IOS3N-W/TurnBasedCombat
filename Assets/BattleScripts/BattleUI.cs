using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using BattleSystem;

public class BattleUI : MonoBehaviour
{
    [Header("Battle UI Elements")]
    public GameObject battlePanel;
    public TextMeshProUGUI battleStatusText;
    public TextMeshProUGUI currentTurnText;
    public TextMeshProUGUI battleLogText;
    
    [Header("Action Buttons")]
    public Button attackButton;
    public Button defendButton;
    public Button passButton;
    public TMP_Dropdown skillDropdown;
    public TMP_Dropdown targetDropdown;
    public Button useSkillButton;
    
    [Header("Character Info")]
    public Transform playersContainer;
    public Transform enemiesContainer;
    public GameObject characterInfoPrefab;
    
    private string currentBattleId;
    private Battle currentBattle;
    private BattleManager battleManager;
    private string localPlayerId;
    
    void Start()
    {
        battleManager = FindObjectOfType<BattleManager>();
        SetupButtons();
        battlePanel?.SetActive(false);
    }
    
    private void SetupButtons()
    {
        attackButton?.onClick.AddListener(() => ExecuteAction(ActionType.Attack));
        defendButton?.onClick.AddListener(() => ExecuteAction(ActionType.Defend));
        passButton?.onClick.AddListener(() => ExecuteAction(ActionType.Pass));
        useSkillButton?.onClick.AddListener(() => ExecuteSkillAction());
    }
    
    public void StartBattle(string battleId)
    {
        currentBattleId = battleId;
        currentBattle = battleManager.GetBattle(battleId);
        
        if (currentBattle == null)
        {
            Debug.LogError($"Battle {battleId} not found!");
            return;
        }
        
        // Get local player ID from SimpleGameManager
        var gameManager = FindObjectOfType<SimpleGameManager>();
        var defaultTeam = gameManager?.GetDefaultTeam();
        localPlayerId = defaultTeam?.Players.FirstOrDefault()?.Id;
        
        battlePanel?.SetActive(true);
        UpdateBattleUI();
        UpdateSkillDropdown();
        UpdateTargetDropdown();
    }
    
    private void UpdateBattleUI()
    {
        if (currentBattle == null) return;
        
        // Update battle status
        if (battleStatusText != null)
        {
            string status = $"Battle State: {currentBattle.State}\nTurn: {currentBattle.CurrentTurn}";
            if (currentBattle.IsPvP)
                status += "\nPvP Battle";
            else
                status += $"\nEnemies: {currentBattle.Enemies.Count(e => e.IsAlive)}";
            
            battleStatusText.text = status;
        }
        
        // Update current turn
        if (currentTurnText != null && currentBattle.CurrentCharacter != null)
        {
            currentTurnText.text = $"Current Turn: {currentBattle.CurrentCharacter.Name}";
            
            // Enable/disable buttons based on if it's player's turn
            bool isPlayerTurn = currentBattle.CurrentCharacter.Id == localPlayerId;
            attackButton?.gameObject.SetActive(isPlayerTurn);
            defendButton?.gameObject.SetActive(isPlayerTurn);
            passButton?.gameObject.SetActive(isPlayerTurn);
            useSkillButton?.gameObject.SetActive(isPlayerTurn);
            skillDropdown?.gameObject.SetActive(isPlayerTurn);
            targetDropdown?.gameObject.SetActive(isPlayerTurn);
        }
        
        UpdateCharacterDisplays();
    }
    
    private void UpdateCharacterDisplays()
    {
        // Clear existing displays
        foreach (Transform child in playersContainer)
            DestroyImmediate(child.gameObject);
        foreach (Transform child in enemiesContainer)
            DestroyImmediate(child.gameObject);
        
        // Display players
        foreach (var team in currentBattle.Teams)
        {
            foreach (var player in team.Players)
            {
                CreateCharacterDisplay(player, playersContainer);
            }
        }
        
        // Display enemies
        foreach (var enemy in currentBattle.Enemies)
        {
            CreateCharacterDisplay(enemy, enemiesContainer);
        }
    }
    
    private void CreateCharacterDisplay(Character character, Transform parent)
    {
        if (characterInfoPrefab == null) return;
        
        GameObject display = Instantiate(characterInfoPrefab, parent);
        
        // Update character info (assuming the prefab has these components)
        var nameText = display.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText != null)
        {
            string info = $"{character.Name}\nHP: {character.Health}/{character.MaxHealth}";
            if (character is Player player)
                info += $"\nMP: {player.Mana}/{player.MaxMana}";
            
            nameText.text = info;
        }
        
        // Color coding
        var image = display.GetComponent<Image>();
        if (image != null)
        {
            if (!character.IsAlive)
                image.color = Color.red;
            else if (character.Id == localPlayerId)
                image.color = Color.green;
            else if (character is Player)
                image.color = Color.blue;
            else
                image.color = Color.gray;
        }
    }
    
    private void UpdateSkillDropdown()
    {
        if (skillDropdown == null) return;
        
        skillDropdown.options.Clear();
        
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer != null)
        {
            foreach (var skill in currentPlayer.Skills)
            {
                if (skill.CanUse(currentPlayer, currentBattle))
                {
                    skillDropdown.options.Add(new TMP_Dropdown.OptionData($"{skill.Name} (MP: {skill.ManaCost})"));
                }
            }
        }
        
        skillDropdown.RefreshShownValue();
    }
    
    private void UpdateTargetDropdown()
    {
        if (targetDropdown == null) return;
        
        targetDropdown.options.Clear();
        
        // Add valid targets based on battle type
        if (currentBattle.IsPvP)
        {
            // In PvP, can target other teams
            foreach (var team in currentBattle.Teams)
            {
                foreach (var player in team.Players.Where(p => p.IsAlive))
                {
                    targetDropdown.options.Add(new TMP_Dropdown.OptionData(player.Name));
                }
            }
        }
        else
        {
            // In PvE, can target enemies and allies
            foreach (var enemy in currentBattle.Enemies.Where(e => e.IsAlive))
            {
                targetDropdown.options.Add(new TMP_Dropdown.OptionData(enemy.Name));
            }
            
            foreach (var team in currentBattle.Teams)
            {
                foreach (var player in team.Players.Where(p => p.IsAlive))
                {
                    targetDropdown.options.Add(new TMP_Dropdown.OptionData(player.Name));
                }
            }
        }
        
        targetDropdown.RefreshShownValue();
    }
    
    private Player GetCurrentPlayer()
    {
        if (currentBattle?.CurrentCharacter is Player player)
            return player;
        return null;
    }
    
    private void ExecuteAction(ActionType actionType)
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null || currentPlayer.Id != localPlayerId) return;
        
        var action = new BattleSystem.Action { Type = actionType };
        
        if (actionType == ActionType.Attack && targetDropdown != null)
        {
            string targetName = targetDropdown.options[targetDropdown.value].text;
            var target = GetCharacterByName(targetName);
            if (target != null)
                action.TargetId = target.Id;
        }
        
        ProcessAction(action);
    }
    
    private void ExecuteSkillAction()
    {
        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer == null || currentPlayer.Id != localPlayerId) return;
        
        if (skillDropdown == null || targetDropdown == null) return;
        
        string skillName = skillDropdown.options[skillDropdown.value].text.Split('(')[0].Trim();
        var skill = currentPlayer.Skills.FirstOrDefault(s => s.Name == skillName);
        
        string targetName = targetDropdown.options[targetDropdown.value].text;
        var target = GetCharacterByName(targetName);
        
        if (skill != null && target != null)
        {
            var action = new BattleSystem.Action 
            { 
                Type = ActionType.Skill,
                SkillId = skill.Id,
                TargetId = target.Id
            };
            
            ProcessAction(action);
        }
    }
    
    private Character GetCharacterByName(string name)
    {
        // Search in all teams
        foreach (var team in currentBattle.Teams)
        {
            var player = team.Players.FirstOrDefault(p => p.Name == name);
            if (player != null) return player;
        }
        
        // Search in enemies
        return currentBattle.Enemies.FirstOrDefault(e => e.Name == name);
    }
    
    private void ProcessAction(BattleSystem.Action action)
    {
        var result = currentBattle.ProcessAction(localPlayerId, action);
        
        if (result.Success)
        {
            LogBattleAction(result.Message);
            UpdateBattleUI();
            UpdateSkillDropdown();
            UpdateTargetDropdown();
            
            // Process enemy turns if it's PvE
            if (!currentBattle.IsPvP)
            {
                ProcessEnemyTurns();
            }
            
            // Check if battle ended
            if (currentBattle.State == BattleState.Finished)
            {
                EndBattle();
            }
        }
        else
        {
            LogBattleAction($"Action failed: {result.Message}");
        }
    }
    
    private void ProcessEnemyTurns()
    {
        var enemyAI = new EnemyAI();
        
        while (currentBattle.CurrentCharacter is Enemy enemy && currentBattle.IsActive)
        {
            var action = enemyAI.DecideAction(enemy, currentBattle);
            var result = currentBattle.ProcessAction(enemy.Id, action);
            
            if (result.Success)
            {
                LogBattleAction($"{enemy.Name}: {result.Message}");
            }
            
            UpdateBattleUI();
            
            if (currentBattle.State == BattleState.Finished)
            {
                EndBattle();
                break;
            }
        }
    }
    
    private void LogBattleAction(string message)
    {
        if (battleLogText != null)
        {
            battleLogText.text += message + "\n";
        }
        Debug.Log($"[Battle] {message}");
    }
    
    private void EndBattle()
    {
        LogBattleAction($"Battle ended! Winner: {currentBattle.WinnerTeamId ?? "None"}");
        
        // Award experience
        if (!string.IsNullOrEmpty(currentBattle.WinnerTeamId))
        {
            var winnerTeam = currentBattle.Teams.FirstOrDefault(t => t.Id == currentBattle.WinnerTeamId);
            if (winnerTeam != null)
            {
                foreach (var player in winnerTeam.Players)
                {
                    int expGain = currentBattle.IsPvP ? 100 : 50;
                    if (player.GainExperience(expGain))
                    {
                        LogBattleAction($"{player.Name} leveled up!");
                    }
                }
            }
        }
        
        // Notify battle manager
        battleManager.EndBattle(currentBattleId, currentBattle.WinnerTeamId);
        
        // Hide battle UI after delay
        Invoke(nameof(HideBattleUI), 3f);
    }
    
    private void HideBattleUI()
    {
        battlePanel?.SetActive(false);
        currentBattle = null;
        currentBattleId = null;
    }
}