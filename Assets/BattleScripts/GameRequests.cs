using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BattleSystem
{
    [Serializable]
    public class CreateGameRequest : BaseRequest
    {
        // Campos adicionais conforme necessário
    }

    [Serializable]
    public class JoinGameRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

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
    public class SetTeamLeaderRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("teamId")]
        public string TeamId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }
    }

    [Serializable]
    public class SetGameTeamReadyRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("teamId")]
        public string TeamId { get; set; }

        [JsonProperty("isReady")]
        public bool IsReady { get; set; }
    }

    [Serializable]
    public class GetGameStateRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("roomCode")]
        public string RoomCode { get; set; }
    }

    [Serializable]
    public class MoveTeamRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("teamId")]
        public string TeamId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("targetNodeId")]
        public string TargetNodeId { get; set; }
    }

    [Serializable]
    public class InvadeBattleRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("teamId")]
        public string TeamId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("battleId")]
        public string BattleId { get; set; }
    }

    [Serializable]
    public class GetBattleOptionsRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("teamId")]
        public string TeamId { get; set; }
    }
}