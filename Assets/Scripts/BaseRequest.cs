using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BattleSystem
{
    [Serializable]
    public class BaseRequest
    {
        [JsonProperty("requestType")]
        public string RequestType { get; set; }
    }

    [Serializable]
    public class CreateBattleRequest : BaseRequest
    {
        // Campos adicionais conforme necessário
    }

    [Serializable]
    public class JoinBattleRequest : BaseRequest
    {
        [JsonProperty("battleId")]
        public string BattleId { get; set; }

        [JsonProperty("roomCode")]
        public string RoomCode { get; set; }

        [JsonProperty("playerName")]
        public string PlayerName { get; set; }

        [JsonProperty("class")]
        public string Class { get; set; }

        [JsonProperty("teamId")]
        public string TeamId { get; set; }
    }

    [Serializable]
    public class SetTeamReadyRequest : BaseRequest
    {
        [JsonProperty("battleId")]
        public string BattleId { get; set; }

        [JsonProperty("teamId")]
        public string TeamId { get; set; }

        [JsonProperty("isReady")]
        public bool IsReady { get; set; }
    }

    [Serializable]
    public class StartBattleRequest : BaseRequest
    {
        [JsonProperty("battleId")]
        public string BattleId { get; set; }

        [JsonProperty("enemyIds")]
        public List<string> EnemyIds { get; set; }

        [JsonProperty("isPvP")]
        public bool IsPvP { get; set; }
    }


    [Serializable]
    
    public class CreateTeamRequest : BaseRequest
    {
        [JsonProperty("battleId")]
        public string BattleId { get; set; }

        [JsonProperty("teamName")]
        public string TeamName { get; set; }
    }


    [Serializable]
    public class ExecuteActionRequest : BaseRequest
    {
        [JsonProperty("battleId")]
        public string BattleId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("action")]
        public Action Action { get; set; }
    }

    [Serializable]
    public class GetBattleStateRequest : BaseRequest
    {

        [JsonProperty("battleId")]
        public string BattleId { get; set; }

        [JsonProperty("roomCode")]
        public string RoomCode { get; set; }
    }
}