using System;
using System.Collections.Generic;
using UnityEngine;
using BattleSystem.Server;
using BattleSystem.Client;

namespace BattleSystem.Server
{
    public class ServerRequest
    {
        public string Type { get; set; }
        public string RoomCode { get; set; }
        public string TeamId { get; set; }
        public string TeamName { get; set; }
        public string PlayerClass { get; set; }
        public string TargetWaypoint { get; set; }
        public bool IsReady { get; set; }
        public BattleActionData BattleAction { get; set; }
    }

    public class ServerResponse
    {
        public string Type { get; set; }
        public bool Success { get; set; } = true;
        public string Message { get; set; }
        public string RoomCode { get; set; }
        public string TeamId { get; set; }
        public string TeamName { get; set; }
        public string CurrentTurn { get; set; }
        public int PlayerCount { get; set; }
        public EncounterData Encounter { get; set; }
        public BattleActionResult BattleResult { get; set; }
        public object MapState { get; set; }
        public string NewPosition { get; set; }
        public string GameState { get; set; }
        public bool IsLeader { get; set; }
        public string LeaderId { get; set; }
        public bool IsReady { get; set; }
    }

    public class PlayerStats
    {
        public string PlayerId { get; set; }
        public string Class { get; set; }
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int Attack { get; set; } = 10;
        public int Defense { get; set; } = 5;
        public int Speed { get; set; } = 5;

        public void UpdateForClass(string playerClass)
        {
            Class = playerClass;
            switch (playerClass.ToLower())
            {
                case "warrior":
                    MaxHealth = 120;
                    Health = 120;
                    Attack = 12;
                    Defense = 8;
                    Speed = 4;
                    break;
                case "mage":
                    MaxHealth = 80;
                    Health = 80;
                    Attack = 15;
                    Defense = 3;
                    Speed = 6;
                    break;
                case "rogue":
                    MaxHealth = 90;
                    Health = 90;
                    Attack = 14;
                    Defense = 5;
                    Speed = 8;
                    break;
                case "healer":
                    MaxHealth = 100;
                    Health = 100;
                    Attack = 8;
                    Defense = 6;
                    Speed = 5;
                    break;
            }
        }
    }

    public class GameTeam
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<ClientHandler> Members { get; set; } = new List<ClientHandler>();
        public bool IsReady { get; set; }
        public string Position { get; set; } = "start";
    }

    [System.Serializable]
    public class EncounterData
    {
        public string Type { get; set; }
        public List<string> Enemies { get; set; } = new List<string>();
        public int Difficulty { get; set; }
    }

    [System.Serializable]
    public class BattleActionData
    {
        public string Type { get; set; }
        public string TargetId { get; set; }
        public string SkillId { get; set; }
    }

    public class BattleActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int Damage { get; set; }
    }

    public class MoveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public bool IsVictory { get; set; }
    }
}
