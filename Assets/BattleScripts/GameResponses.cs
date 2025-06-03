using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BattleSystem
{
    [Serializable]
    public class CreateGameResponse : BaseResponse
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("roomCode")]
        public string RoomCode { get; set; }
    }

    [Serializable]
    public class JoinGameResponse : BaseResponse
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("teamId")]
        public string TeamId { get; set; }

        [JsonProperty("teamName")]
        public string TeamName { get; set; }

        [JsonProperty("isTeamLeader")]
        public bool IsTeamLeader { get; set; }
    }

    [Serializable]
    public class GameStartedResponse : BaseResponse
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("firstTeamId")]
        public string FirstTeamId { get; set; }
    }

    [Serializable]
    public class GetGameStateResponse : BaseResponse
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("state")]
        public GameState State { get; set; }

        [JsonProperty("teams")]
        public List<Team> Teams { get; set; }

        [JsonProperty("teamLeaders")]
        public Dictionary<string, string> TeamLeaders { get; set; }

        [JsonProperty("teamReadyStatus")]
        public Dictionary<string, bool> TeamReadyStatus { get; set; }

        [JsonProperty("currentTeamId")]
        public string CurrentTeamId { get; set; }

        [JsonProperty("map")]
        public List<MapNode> Map { get; set; }

        [JsonProperty("teamPositions")]
        public Dictionary<string, string> TeamPositions { get; set; }

        [JsonProperty("activeBattles")]
        public Dictionary<string, Battle> ActiveBattles { get; set; }
    }

    [Serializable]
    public class MoveTeamResponse : BaseResponse
    {
        [JsonProperty("newNodeId")]
        public string NewNodeId { get; set; }

        [JsonProperty("nextTeamId")]
        public string NextTeamId { get; set; }
    }

    [Serializable]
    public class InvadeBattleResponse : BaseResponse
    {
        [JsonProperty("battleId")]
        public string BattleId { get; set; }
    }

    [Serializable]
    public class BattleInvasionNotification : BaseResponse
    {
        [JsonProperty("battleId")]
        public string BattleId { get; set; }

        [JsonProperty("invaderTeamId")]
        public string InvaderTeamId { get; set; }
    }

    [Serializable]
    public class GetBattleOptionsResponse : BaseResponse
    {
        [JsonProperty("invadableBattles")]
        public List<string> InvadableBattles { get; set; }

        [JsonProperty("availableMovements")]
        public List<string> AvailableMovements { get; set; }
    }
}