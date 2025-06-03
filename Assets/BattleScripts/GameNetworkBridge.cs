using System.Collections;
using UnityEngine;
using BattleSystem;
using System.Collections.Generic;
using Newtonsoft.Json;

// This class bridges the NetworkManager and BattleManagerExtension
public class GameNetworkBridge : MonoBehaviour
{
    private NetworkManager networkManager;
    private BattleManagerExtension battleManagerExt;
    
    private void Start()
    {
        networkManager = NetworkManager.Instance;
        battleManagerExt = BattleManagerExtension.Instance;
        
        // Wait a frame to ensure all components are initialized
        StartCoroutine(DelayedInit());
    }
    
    private IEnumerator DelayedInit()
    {
        yield return null;
        
        // Setup message handlers for the client
        SetupMessageHandlers();
    }
    
    private void SetupMessageHandlers()
    {
        // For a real implementation, you'd need to set up handlers for:
        // - Player joined messages
        // - Team created messages
        // - Ready status updates
        // - Battle start messages
        
        // Example of how you would handle a player joined message:
        // networkManager.OnPlayerJoined += (playerJson) => {
        //     var player = JsonConvert.DeserializeObject<Player>(playerJson);
        //     battleManagerExt.RegisterPlayer(player);
        // };
    }

    public void ProcessMessage(string message)
    {
        try
        {
            // Parse the message type
            var baseMsg = JsonConvert.DeserializeObject<BaseResponse>(message);

            switch (baseMsg.ResponseType)
            {  // Changed from RequestType to ResponseType
                case "PlayerJoined":
                    HandlePlayerJoined(message);
                    break;
                case "TeamCreated":
                    HandleTeamCreated(message);
                    break;
                case "ReadyStatusUpdate":
                    HandleReadyStatusUpdate(message);
                    break;
                case "BattleStarted":
                    HandleBattleStarted(message);
                    break;
                default:
                    Debug.Log($"Unknown message type: {baseMsg.ResponseType}");
                    break;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error processing message: {ex.Message}");
        }
    }


    private void HandlePlayerJoined(string message)
    {
        try {
            // Parse the player data
            var response = JsonConvert.DeserializeObject<PlayerJoinedResponse>(message);
            
            // Create player object
            var player = Player.CreatePlayer(
                response.PlayerId, 
                response.PlayerName, 
                response.PlayerClass
            );
            
            // Register with battle manager
            battleManagerExt.RegisterPlayer(player);
        }
        catch (System.Exception ex) {
            Debug.LogError($"Error handling player joined: {ex.Message}");
        }
    }
    
    private void HandleTeamCreated(string message)
    {
        try {
            // Parse the team data
            var response = JsonConvert.DeserializeObject<TeamCreatedResponse>(message);
            
            // Create team object
            var team = new Team {
                Id = response.TeamId,
                Name = response.TeamName
            };
            
            // Register with battle manager
            battleManagerExt.RegisterTeam(team);
        }
        catch (System.Exception ex) {
            Debug.LogError($"Error handling team created: {ex.Message}");
        }
    }
    
    private void HandleReadyStatusUpdate(string message)
    {
        try {
            // Parse the ready status update
            var response = JsonConvert.DeserializeObject<ReadyStatusResponse>(message);
            
            // Update team ready status
            // For a complete implementation, you would update the team's ready status
            // and then check if all teams are ready
            
            battleManagerExt.CheckAllPlayersReady();
        }
        catch (System.Exception ex) {
            Debug.LogError($"Error handling ready status update: {ex.Message}");
        }
    }
    
    private void HandleBattleStarted(string message)
    {
        try {
            // Parse the battle started message
            var response = JsonConvert.DeserializeObject<BattleStartedResponse>(message);
            
            // Trigger battle started event
            battleManagerExt.TriggerBattleStarted(response.BattleId);
        }
        catch (System.Exception ex) {
            Debug.LogError($"Error handling battle started: {ex.Message}");
        }
    }
}

// Custom response classes for network messages
[System.Serializable]
public class PlayerJoinedResponse : BaseResponse
{
    public PlayerJoinedResponse()
    {
        ResponseType = "PlayerJoined";
    }

    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public string PlayerClass { get; set; }
}

[System.Serializable]
public class TeamCreatedResponse : BaseResponse
{
    public TeamCreatedResponse()
    {
        ResponseType = "TeamCreated";
    }

    public string TeamId { get; set; }
    public string TeamName { get; set; }
}

[System.Serializable]
public class ReadyStatusResponse : BaseResponse
{
    public ReadyStatusResponse()
    {
        ResponseType = "ReadyStatusUpdate";
    }

    public string TeamId { get; set; }
    public bool IsReady { get; set; }
}

[System.Serializable]
public class BattleStartedResponse : BaseResponse
{
    public BattleStartedResponse()
    {
        ResponseType = "BattleStarted";
    }

    public string BattleId { get; set; }
}
