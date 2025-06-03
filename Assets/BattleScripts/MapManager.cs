using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSystem
{
    [Serializable]
    public class MapNode
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> ConnectedNodes { get; set; } = new List<string>();
        public bool HasEnemies { get; set; }
        public List<string> EnemyIds { get; set; } = new List<string>();
        public bool InBattle { get; set; }
        public string BattleId { get; set; }
        public List<string> TeamsInNode { get; set; } = new List<string>();
        public Vector2 Position { get; set; }

        public MapNode(string id, string name, Vector2 position)
        {
            Id = id;
            Name = name;
            Position = position;
            TeamsInNode = new List<string>();
            EnemyIds = new List<string>();
        }
    }

    public class MapManager
    {
        public List<MapNode> Nodes { get; private set; } = new List<MapNode>();
        public Dictionary<string, string> TeamNodePositions { get; private set; } = new Dictionary<string, string>();
        private Dictionary<string, string> nodeEnemyTemplates = new Dictionary<string, string>();
        private System.Random randomGenerator = new System.Random(Guid.NewGuid().GetHashCode());

        private const float ENEMY_SPAWN_CHANCE = 0.6f;

        public MapManager()
        {
            GenerateMap();
        }

        private void GenerateMap()
        {
            int gridSize = 5;
            
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    string nodeId = $"node_{x}_{y}";
                    string nodeName = $"Waypoint ({x},{y})";
                    Vector2 position = new Vector2(x, y);
                    
                    MapNode node = new MapNode(nodeId, nodeName, position);
                    Nodes.Add(node);
                }
            }
            
            foreach (var node in Nodes)
            {
                int x = (int)node.Position.x;
                int y = (int)node.Position.y;

                if (x > 0)
                    node.ConnectedNodes.Add($"node_{x-1}_{y}");
                if (x < gridSize - 1)
                    node.ConnectedNodes.Add($"node_{x+1}_{y}");
                if (y > 0)
                    node.ConnectedNodes.Add($"node_{x}_{y-1}");
                if (y < gridSize - 1)
                    node.ConnectedNodes.Add($"node_{x}_{y+1}");
            }
            
            Dictionary<string, Vector2> spawnPositions = new Dictionary<string, Vector2>
            {
                { "spawn_1", new Vector2(0, 0) },
                { "spawn_2", new Vector2(gridSize-1, 0) },
                { "spawn_3", new Vector2(0, gridSize-1) },
                { "spawn_4", new Vector2(gridSize-1, gridSize-1) }
            };
            
            foreach (var spawn in spawnPositions)
            {
                var node = Nodes.FirstOrDefault(n => n.Position == spawn.Value);
                if (node != null)
                {
                    node.Name = spawn.Key;
                }
            }
        }

        public void GenerateEnemies(Dictionary<string, Enemy> enemyTemplates)
        {
            foreach (var node in Nodes)
            {
                node.HasEnemies = false;
                node.EnemyIds.Clear();
            }

            if (enemyTemplates == null || enemyTemplates.Count == 0)
                return;

            var enemyTemplateList = enemyTemplates.Keys.ToList();

            foreach (var node in Nodes)
            {
                if (node.Name.StartsWith("spawn_"))
                    continue;

                if (randomGenerator.NextDouble() < ENEMY_SPAWN_CHANCE)
                {
                    node.HasEnemies = true;
                    int enemyCount = randomGenerator.Next(1, 4);
                    
                    for (int i = 0; i < enemyCount; i++)
                    {
                        string randomEnemyTemplate = enemyTemplateList[randomGenerator.Next(enemyTemplateList.Count)];
                        node.EnemyIds.Add(randomEnemyTemplate);
                    }
                }
            }
        }

        public void AssignTeamToNode(string teamId, string nodeId)
        {
            var targetNode = Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (targetNode != null)
            {
                if (TeamNodePositions.ContainsKey(teamId))
                {
                    var oldNode = Nodes.FirstOrDefault(n => n.Id == TeamNodePositions[teamId]);
                    oldNode?.TeamsInNode.Remove(teamId);
                }

                TeamNodePositions[teamId] = nodeId;
                if (!targetNode.TeamsInNode.Contains(teamId))
                {
                    targetNode.TeamsInNode.Add(teamId);
                }
            }
        }

        public List<string> GetMovementOptions(string teamId)
        {
            if (!TeamNodePositions.TryGetValue(teamId, out string currentNodeId))
                return new List<string>();

            var currentNode = Nodes.FirstOrDefault(n => n.Id == currentNodeId);
            if (currentNode == null)
                return new List<string>();

            return currentNode.ConnectedNodes.ToList();
        }

        public bool MoveTeam(string teamId, string targetNodeId)
        {
            var movementOptions = GetMovementOptions(teamId);
            if (!movementOptions.Contains(targetNodeId))
                return false;

            AssignTeamToNode(teamId, targetNodeId);
            return true;
        }

        public bool CheckForBattleStart(string nodeId, out bool isPvP, out List<string> battleTeams, out List<string> enemyTemplateIds)
        {
            isPvP = false;
            battleTeams = new List<string>();
            enemyTemplateIds = new List<string>();

            var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node == null || node.InBattle)
                return false;

            if (node.TeamsInNode.Count > 1)
            {
                isPvP = true;
                battleTeams = node.TeamsInNode.ToList();
                return true;
            }

            if (node.TeamsInNode.Count == 1 && node.HasEnemies)
            {
                isPvP = false;
                battleTeams = node.TeamsInNode.ToList();
                enemyTemplateIds = node.EnemyIds.ToList();
                return true;
            }

            return false;
        }

        public void StartBattleAtNode(string nodeId, string battleId)
        {
            var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                node.InBattle = true;
                node.BattleId = battleId;
            }
        }

        public void EndBattleAtNode(string nodeId)
        {
            var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (node != null)
            {
                node.InBattle = false;
                node.BattleId = null;
                node.HasEnemies = false;
                node.EnemyIds.Clear();
            }
        }

        public List<string> GetTeamsInNode(string nodeId)
        {
            var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
            return node?.TeamsInNode.ToList() ?? new List<string>();
        }

        public bool CanTeamInvadeBattle(string teamId, string battleNodeId)
        {
            if (!TeamNodePositions.TryGetValue(teamId, out string teamNodeId))
                return false;

            var teamNode = Nodes.FirstOrDefault(n => n.Id == teamNodeId);
            if (teamNode == null)
                return false;

            return teamNode.ConnectedNodes.Contains(battleNodeId);
        }

        public List<string> GetInvadableBattles(string teamId)
        {
            if (!TeamNodePositions.TryGetValue(teamId, out string teamNodeId))
                return new List<string>();

            var teamNode = Nodes.FirstOrDefault(n => n.Id == teamNodeId);
            if (teamNode == null)
                return new List<string>();

            var invadableBattles = new List<string>();
            foreach (string connectedNodeId in teamNode.ConnectedNodes)
            {
                var connectedNode = Nodes.FirstOrDefault(n => n.Id == connectedNodeId);
                if (connectedNode != null && connectedNode.InBattle && !string.IsNullOrEmpty(connectedNode.BattleId))
                {
                    invadableBattles.Add(connectedNode.BattleId);
                }
            }

            return invadableBattles;
        }
    }
}