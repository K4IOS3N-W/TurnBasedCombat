using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BattleSystem
{
    [Serializable]
    public class BaseResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("responseType")]
        public string ResponseType { get; set; }
    }

    [Serializable]
    public class GenericResponse<T> : BaseResponse
    {
        [JsonProperty("data")]
        public T Data { get; set; }
    }

    [Serializable]
    public class CreateBattleResponse : BaseResponse
    {
        [JsonProperty("battleId")]
        public string BattleId { get; set; }

        [JsonProperty("roomCode")]
        public string RoomCode { get; set; }
    }

    [Serializable]
    public class CreateTeamResponse : BaseResponse
    {
        [JsonProperty("teamId")]
        public string TeamId { get; set; }

        [JsonProperty("teamName")]
        public string TeamName { get; set; }
    }

    [Serializable]
    public class JoinBattleResponse : BaseResponse
    {
        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("teamId")]
        public string TeamId { get; set; }

        [JsonProperty("teamName")]
        public string TeamName { get; set; }
    }

    [Serializable]
    public class StartBattleResponse : BaseResponse
    {
        [JsonProperty("turnOrder")]
        public List<string> TurnOrder { get; set; }

        [JsonProperty("firstPlayer")]
        public string FirstPlayer { get; set; }
    }

    [Serializable]
    public class ExecuteActionResponse : BaseResponse
    {
        [JsonProperty("results")]
        public List<ActionResult> Results { get; set; }

        [JsonProperty("nextPlayer")]
        public string NextPlayer { get; set; }
    }

    [Serializable]
    public class GetBattleStateResponse : BaseResponse
    {
        [JsonProperty("battle")]
        public Battle Battle { get; set; }

        [JsonProperty("battleId")]
        public string BattleId { get; set; }
    }

    [Serializable]
    public class StatusEffectsResponse : BaseResponse
    {
        [JsonProperty("results")]
        public List<ActionResult> Results { get; set; }
    }

    [Serializable]
    public class ExperienceUpdateResponse : BaseResponse
    {
        [JsonProperty("level")]
        public int Level { get; set; }
        
        [JsonProperty("experience")]
        public int Experience { get; set; }
        
        [JsonProperty("experienceToNextLevel")]
        public int ExperienceToNextLevel { get; set; }
        
        [JsonProperty("xpGained")]
        public int XpGained { get; set; }
        
        [JsonProperty("class")]
        public string Class { get; set; }
        
        [JsonProperty("leveledUp")]
        public bool LeveledUp { get; set; }
        
        [JsonProperty("newSkillsLearned")]
        public List<string> NewSkillsLearned { get; set; } = new List<string>();
        
        [JsonProperty("statBonuses")]
        public Dictionary<string, int> StatBonuses { get; set; } = new Dictionary<string, int>();
    }
}