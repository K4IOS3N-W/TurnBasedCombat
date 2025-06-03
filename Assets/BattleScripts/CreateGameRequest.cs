using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BattleSystem
{


    [Serializable]
    public class LeaveTeamRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("teamId")]
        public string TeamId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }
    }

    [Serializable]
    public class ChangeTeamStrategyRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("teamId")]
        public string TeamId { get; set; }

        [JsonProperty("strategy")]
        public TeamStrategy Strategy { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }
    }

    [Serializable]
    public class SetPlayerReadyRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("ready")]
        public bool Ready { get; set; }
    }

    [Serializable]
    public class GetAvailableGamesRequest : BaseRequest
    {
        [JsonProperty("includePrivate")]
        public bool IncludePrivate { get; set; } = false;

        [JsonProperty("maxResults")]
        public int MaxResults { get; set; } = 50;
    }

    [Serializable]
    public class GetGameStatusRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("roomCode")]
        public string RoomCode { get; set; }
    }

    [Serializable]
    public class UpdatePlayerCharacterRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("characterName")]
        public string CharacterName { get; set; }

        [JsonProperty("class")]
        public string Class { get; set; }

        [JsonProperty("attributes")]
        public Dictionary<string, int> Attributes { get; set; } = new Dictionary<string, int>();
    }

    [Serializable]
    public class KickPlayerRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("teamId")]
        public string TeamId { get; set; }

        [JsonProperty("targetPlayerId")]
        public string TargetPlayerId { get; set; }

        [JsonProperty("requestingPlayerId")]
        public string RequestingPlayerId { get; set; }
    }

    [Serializable]
    public class SendChatMessageRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("messageType")]
        public ChatMessageType MessageType { get; set; } = ChatMessageType.General;

        [JsonProperty("targetId")]
        public string TargetId { get; set; } // For team or private messages
    }

    [Serializable]
    public class GetMapInfoRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("includeEnemies")]
        public bool IncludeEnemies { get; set; } = true;

        [JsonProperty("includeTeamPositions")]
        public bool IncludeTeamPositions { get; set; } = true;
    }

    [Serializable]
    public class GetBattleInfoRequest : BaseRequest
    {
        [JsonProperty("battleId")]
        public string BattleId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }
    }

    [Serializable]
    public class UseSkillRequest : BaseRequest
    {
        [JsonProperty("battleId")]
        public string BattleId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("skillId")]
        public string SkillId { get; set; }

        [JsonProperty("targetId")]
        public string TargetId { get; set; }
    }

    [Serializable]
    public class UseItemRequest : BaseRequest
    {
        [JsonProperty("battleId")]
        public string BattleId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("itemId")]
        public string ItemId { get; set; }

        [JsonProperty("targetId")]
        public string TargetId { get; set; }
    }

    [Serializable]
    public class DefendRequest : BaseRequest
    {
        [JsonProperty("battleId")]
        public string BattleId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }
    }

    [Serializable]
    public class FleeRequest : BaseRequest
    {
        [JsonProperty("battleId")]
        public string BattleId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }
    }

    [Serializable]
    public class SurrenderRequest : BaseRequest
    {
        [JsonProperty("gameId")]
        public string GameId { get; set; }

        [JsonProperty("teamId")]
        public string TeamId { get; set; }

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }
    }

    [Serializable]
    public class GetPlayerStatsRequest : BaseRequest
    {
        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("includeSessionStats")]
        public bool IncludeSessionStats { get; set; } = true;
    }

    [Serializable]
    public class GetLeaderboardRequest : BaseRequest
    {
        [JsonProperty("leaderboardType")]
        public LeaderboardType LeaderboardType { get; set; } = LeaderboardType.Experience;

        [JsonProperty("maxResults")]
        public int MaxResults { get; set; } = 100;

        [JsonProperty("includeCurrentPlayer")]
        public bool IncludeCurrentPlayer { get; set; } = true;

        [JsonProperty("playerId")]
        public string PlayerId { get; set; }
    }

    [Serializable]
    public class PingRequest : BaseRequest
    {
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }
    }

    // Enums for requests
    [Serializable]
    public enum ChatMessageType
    {
        General,
        Team,
        Private,
        System,
        Battle
    }

    [Serializable]
    public enum LeaderboardType
    {
        Experience,
        Level,
        WinRate,
        BattlesWon,
        DamageDealt,
        HealingDone,
        SessionTime
    }
}