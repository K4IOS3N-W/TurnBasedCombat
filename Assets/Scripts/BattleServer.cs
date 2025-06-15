using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace BattleSystem.Server
{
    // Delegado e enumeração existentes
    public delegate void ClientDisconnectedHandler(string clientId, DisconnectReason reason);

    public enum DisconnectReason
    {
        ClientInitiated,
        ServerInitiated,
        ConnectionLost,
        Timeout,
        Error
    }

    public class BattleServer
    {
        private TcpListener tcpListener;
        private readonly int port;
        private bool isRunning;
        private readonly Dictionary<string, ClientConnection> clients = new Dictionary<string, ClientConnection>();
        private readonly Dictionary<string, Battle> battles = new Dictionary<string, Battle>();
        private readonly Dictionary<string, List<string>> battleClients = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, Enemy> enemyTemplates = new Dictionary<string, Enemy>();

        // Modified: Session-based progression per client
        private readonly Dictionary<string, Dictionary<string, ClassProgress>> sessionProgressData = new Dictionary<string, Dictionary<string, ClassProgress>>();

        // Dicionário para mapear códigos de sala aos IDs de batalha
        private readonly Dictionary<string, string> roomCodeToBattleId = new Dictionary<string, string>();

        public event Action<string> OnLog;
        public event Action<Exception> OnError;

        public BattleServer(int port = 7777)
        {
            this.port = port;
            LoadEnemyTemplates();
        }

        private void LoadEnemyTemplates()
        {
            // Aqui você pode carregar modelos de inimigos de um arquivo JSON ou criar diretamente
            // Exemplos:
            var goblin = new Enemy
            {
                Id = "enemy_goblin",
                Name = "Goblin",
                Health = 200,
                MaxHealth = 200,
                Attack = 60,
                Defense = 30,
                Speed = 70,
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = "skill_goblin_stab",
                        Name = "Estocada",
                        Description = "Ataque rápido que causa dano moderado",
                        Damage = 75,
                        Range = 1
                    }
                }
            };

            var orc = new Enemy
            {
                Id = "enemy_orc",
                Name = "Orc Guerreiro",
                Health = 400,
                MaxHealth = 400,
                Attack = 80,
                Defense = 50,
                Speed = 50,
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = "skill_orc_slam",
                        Name = "Golpe Brutal",
                        Description = "Ataque poderoso que causa dano alto",
                        Damage = 120,
                        Range = 1
                    }
                }
            };

            var necromancer = new Enemy
            {
                Id = "enemy_necromancer",
                Name = "Necromante",
                Health = 300,
                MaxHealth = 300,
                Attack = 50,
                Defense = 40,
                Speed = 60,
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = "skill_necro_drain",
                        Name = "Drenar Vida",
                        Description = "Causa dano e recupera parte como vida",
                        Damage = 70,
                        Healing = 35,
                        Range = 3
                    },
                    new Skill
                    {
                        Id = "skill_necro_summon",
                        Name = "Invocar Esqueleto",
                        Description = "Invoca um esqueleto para lutar",
                        Range = 2
                    }
                }
            };

            var dragon = new Enemy
            {
                Id = "enemy_dragon",
                Name = "Dragão Ancião",
                Health = 1000,
                MaxHealth = 1000,
                Attack = 120,
                Defense = 80,
                Speed = 40,
                Skills = new List<Skill>
                {
                    new Skill
                    {
                        Id = "skill_dragon_breath",
                        Name = "Sopro de Fogo",
                        Description = "Lança uma rajada de fogo em todos os inimigos",
                        Damage = 150,
                        Range = 3,
                        AffectsTeam = true
                    },
                    new Skill
                    {
                        Id = "skill_dragon_tail",
                        Name = "Golpe de Cauda",
                        Description = "Atinge todos os inimigos próximos",
                        Damage = 100,
                        Range = 2,
                        AffectsTeam = true
                    }
                }
            };

            enemyTemplates.Add(goblin.Id, goblin);
            enemyTemplates.Add(orc.Id, orc);
            enemyTemplates.Add(necromancer.Id, necromancer);
            enemyTemplates.Add(dragon.Id, dragon);
        }

        public async Task Start()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
                isRunning = true;

                LogMessage($"Servidor iniciado na porta {port}");

                while (isRunning)
                {
                    try
                    {
                        var tcpClient = await tcpListener.AcceptTcpClientAsync();
                        _ = HandleClientAsync(tcpClient);
                    }
                    catch (Exception ex) when (isRunning)
                    {
                        OnError?.Invoke(ex);
                        LogMessage($"Erro ao aceitar cliente: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                LogMessage($"Erro ao iniciar servidor: {ex.Message}");
            }
            finally
            {
                isRunning = false;
                tcpListener?.Stop();
            }
        }

        public void Stop()
        {
            if (!isRunning)
                return;

            isRunning = false;
            tcpListener?.Stop();

            // Desconectar todos os clientes
            foreach (var client in clients.Values.ToList())
            {
                client.Disconnect(DisconnectReason.ServerInitiated);
            }

            clients.Clear();
            LogMessage("Servidor desligado");
        }

        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            var clientId = Guid.NewGuid().ToString();
            LogMessage($"Novo cliente conectando: {clientId}");

            try
            {
                var client = new ClientConnection(tcpClient, clientId);

                // Registrar handlers de eventos
                client.OnMessageReceived += async (message) =>
                {
                    await ProcessMessageAsync(client, message);
                };

                client.OnDisconnected += HandleClientDisconnection;

                // Adicionar o cliente à coleção de clientes
                lock (clients)
                {
                    clients.Add(clientId, client);
                }

                LogMessage($"Cliente conectado: {clientId}");

                // Iniciar loop de escuta para este cliente
                await client.StartListeningAsync();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                LogMessage($"Erro ao processar cliente {clientId}: {ex.Message}");

                // Se o cliente já foi adicionado à coleção, desconecte-o
                lock (clients)
                {
                    if (clients.TryGetValue(clientId, out var client))
                    {
                        client.Disconnect(DisconnectReason.Error);
                    }
                    else
                    {
                        // Se o cliente não foi adicionado, feche a conexão diretamente
                        tcpClient.Close();
                    }
                }
            }
        }

        private void HandleClientDisconnection(string clientId, DisconnectReason reason)
        {
            lock (clients)
            {
                clients.Remove(clientId);

                // Limpar dados de progresso da sessão
                if (sessionProgressData.ContainsKey(clientId))
                {
                    LogMessage($"Limpando dados de progresso para cliente {clientId}");
                    sessionProgressData.Remove(clientId);
                }

                // Remover cliente das batalhas
                foreach (var battleId in battleClients.Keys.ToList())
                {
                    if (battleClients[battleId].Contains(clientId))
                    {
                        battleClients[battleId].Remove(clientId);

                        // Notificar outros clientes da desconexão
                        if (battles.ContainsKey(battleId))
                        {
                            var battleInfo = battles[battleId];
                            string disconnectMessage = $"Jogador desconectado da batalha: {battleId}, Razão: {reason}";
                            _ = NotifyBattleClients(battleId, disconnectMessage);

                            // Verificar se a batalha ficou vazia
                            if (battleClients[battleId].Count == 0 && battleInfo.State != BattleState.Finished)
                            {
                                battles.Remove(battleId);
                                battleClients.Remove(battleId);
                                LogMessage($"Batalha {battleId} removida por falta de jogadores");
                            }
                        }
                    }
                }
            }

            LogMessage($"Cliente desconectado: {clientId}, Razão: {reason}");
        }

        private async Task ProcessMessageAsync(ClientConnection client, string message)
        {
            try
            {
                var baseRequest = JsonConvert.DeserializeObject<BaseRequest>(message);
                LogMessage($"Requisição recebida: {baseRequest.RequestType} de {client.Id}");

                switch (baseRequest.RequestType)
                {
                    case "CreateBattle":
                        await HandleCreateBattle(client, message);
                        break;
                    case "CreateTeam":
                        await HandleCreateTeam(client, message);
                        break;
                    case "JoinBattle":
                        await HandleJoinBattle(client, message);
                        break;
                    case "StartBattle":
                        await HandleStartBattle(client, message);
                        break;
                    case "ExecuteAction":
                        await HandleExecuteAction(client, message);
                        break;
                    case "GetBattleState":
                        await HandleGetBattleState(client, message);
                        break;
                    case "SetTeamReady":
                        await HandleSetTeamReady(client, message);
                        break;
                    default:
                        LogMessage($"Tipo de requisição desconhecido: {baseRequest.RequestType}");
                        await SendErrorResponse(client, "Tipo de requisição desconhecida");
                        break;
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                LogMessage($"Erro ao processar mensagem: {ex.Message}\nDetalhe: {ex.StackTrace}");
                await SendErrorResponse(client, "Erro ao processar requisição");
            }
        }

        private async Task SendErrorResponse(ClientConnection client, string message)
        {
            var response = new BaseResponse
            {
                Success = false,
                Message = message
            };

            await client.SendMessageAsync(JsonConvert.SerializeObject(response));
        }

        #region Handlers de Requisição

        private async Task HandleCreateBattle(ClientConnection client, string message)
        {
            var request = JsonConvert.DeserializeObject<CreateBattleRequest>(message);

            // Gerar ID único para a batalha (UUID completo para uso interno)
            var battleId = Guid.NewGuid().ToString();

            // Gerar código de sala de 5 dígitos (para uso do jogador)
            var roomCode = GenerateRoomCode();

            var battle = new Battle
            {
                Id = battleId,
                RoomCode = roomCode,
                State = BattleState.Preparation,
                StartedAt = DateTime.Now,
                Teams = new List<Team>(),
                Enemies = new List<Enemy>(),
                TeamReadyStatus = new Dictionary<string, bool>()
            };

            battles.Add(battleId, battle);
            roomCodeToBattleId.Add(roomCode, battleId);
            battleClients.Add(battleId, new List<string> { client.Id });

            LogMessage($"Batalha criada: {battleId}, Código de sala: {roomCode}");

            var response = new CreateBattleResponse
            {
                Success = true,
                Message = "Batalha criada com sucesso",
                BattleId = battleId,
                RoomCode = roomCode
            };

            await client.SendMessageAsync(JsonConvert.SerializeObject(response));
        }

        // Método para gerar um código de sala único de 5 dígitos
        private string GenerateRoomCode()
        {
            Random random = new Random();
            string code;

            do
            {
                // Gerar um número de 5 dígitos (10000-99999)
                code = random.Next(10000, 100000).ToString();
            } while (roomCodeToBattleId.ContainsKey(code));

            return code;
        }

        private async Task HandleJoinBattle(ClientConnection client, string message)
        {
            var request = JsonConvert.DeserializeObject<JoinBattleRequest>(message);
            string battleId = request.BattleId;

            // Se fornecido um código de sala em vez de battleId, buscar o battleId correspondente
            if (string.IsNullOrEmpty(battleId) && !string.IsNullOrEmpty(request.RoomCode))
            {
                if (roomCodeToBattleId.TryGetValue(request.RoomCode, out string id))
                {
                    battleId = id;
                }
                else
                {
                    await SendErrorResponse(client, "Código de sala inválido");
                    return;
                }
            }

            if (!battles.TryGetValue(battleId, out var battle))
            {
                await SendErrorResponse(client, "Batalha não encontrada");
                return;
            }

            if (battle.State != BattleState.Preparation)
            {
                await SendErrorResponse(client, "A batalha já foi iniciada ou finalizada");
                return;
            }

            // Verificar número máximo de jogadores (assumindo 8 - 2 equipes de 4)
            int totalPlayers = battle.Teams.Sum(e => e.Players.Count);
            if (totalPlayers >= 8)
            {
                await SendErrorResponse(client, "Número máximo de jogadores atingido");
                return;
            }

            // Create player with session-based progression
            var playerId = new Random().Next(1000, 9999).ToString();
            var player = CreatePlayerWithSessionProgress(client.Id, playerId, request.PlayerName, request.Class);

            // Adicionar jogador a uma equipe - SISTEMA MELHORADO DE DISTRIBUIÇÃO
            Team team = null;
            string teamId = request.TeamId;

            // Apenas atribui automaticamente se o usuário não especificou equipe
            if (string.IsNullOrEmpty(teamId))
            {
                // Tentamos balancear as equipes para PvP, levando em consideração o número de jogadores e suas classes
                if (battle.Teams.Count >= 2)
                {
                    // Se já existem pelo menos duas equipes, balancear com base no número de jogadores
                    team = battle.Teams.OrderBy(t => t.Players.Count).First();
                }
                else if (battle.Teams.Count == 1)
                {
                    // Se existe apenas uma equipe, verificamos a distribuição de classes para PvP
                    var existingTeam = battle.Teams[0];

                    // Primeiro verifica se a equipe está cheia
                    if (existingTeam.Players.Count >= 4)
                    {
                        // Criar segunda equipe
                        team = new Team
                        {
                            Id = "team2",
                            Name = "Time 2",
                            Players = new List<Player>()
                        };
                        battle.Teams.Add(team);
                    }
                    else
                    {
                        // Se a primeira equipe não está cheia, verificamos as classes
                        bool hasWarrior = existingTeam.Players.Any(p => p.Class.ToLower() == "warrior");
                        bool hasMage = existingTeam.Players.Any(p => p.Class.ToLower() == "mage");
                        bool hasHealer = existingTeam.Players.Any(p => p.Class.ToLower() == "healer");

                        bool shouldCreateNewTeam = false;

                        // Para balanceamento de PvP, distribuímos as classes
                        if (request.Class.ToLower() == "warrior" && hasWarrior)
                            shouldCreateNewTeam = true;
                        else if (request.Class.ToLower() == "mage" && hasMage)
                            shouldCreateNewTeam = true;
                        else if (request.Class.ToLower() == "healer" && hasHealer)
                            shouldCreateNewTeam = true;

                        if (shouldCreateNewTeam)
                        {
                            // Criar segunda equipe para evitar duplicidade de classes no mesmo time
                            team = new Team
                            {
                                Id = "team2",
                                Name = "Time 2",
                                Players = new List<Player>()
                            };
                            battle.Teams.Add(team);
                        }
                        else
                        {
                            // Adicionar ao time existente
                            team = existingTeam;
                        }
                    }
                }
                else
                {
                    // Não há times, cria o primeiro
                    team = new Team
                    {
                        Id = "team1",
                        Name = "Time 1",
                        Players = new List<Player>()
                    };
                    battle.Teams.Add(team);
                }

                teamId = team.Id;
            }
            else
            {
                // Verificar se equipe existe
                team = battle.Teams.FirstOrDefault(e => e.Id == teamId);

                if (team == null)
                {
                    // Criar nova equipe com ID fornecido
                    team = new Team
                    {
                        Id = teamId,
                        Name = $"Time {battle.Teams.Count + 1}",
                        Players = new List<Player>()
                    };
                    battle.Teams.Add(team);
                }
                else if (team.Players.Count >= 4)
                {
                    await SendErrorResponse(client, "Equipe já está com número máximo de jogadores");
                    return;
                }
            }

            // Adicionar jogador à equipe
            try
            {
                team.AddPlayer(player);
                LogMessage($"Jogador {player.Id} ({player.Name}) adicionado à equipe {team.Name} com buffs aplicados");
            }
            catch (InvalidOperationException ex)
            {
                LogMessage($"Erro ao adicionar jogador à equipe: {ex.Message}");
                await SendErrorResponse(client, ex.Message);
                return;
            }

            // Adicionar cliente à lista de clientes da batalha
            if (!battleClients[battle.Id].Contains(client.Id))
            {
                battleClients[battle.Id].Add(client.Id);
            }

            LogMessage($"Jogador {player.Id} ({player.Name}) entrou na batalha {battle.Id}, equipe {team.Name}");

            var response = new JoinBattleResponse
            {
                Success = true,
                Message = "Entrou na batalha com sucesso",
                PlayerId = playerId,
                TeamId = teamId,
                TeamName = team.Name
            };

            await client.SendMessageAsync(JsonConvert.SerializeObject(response));

            // Notificar outros clientes
            await BroadcastBattleState(battle.Id);
        }

        private async Task HandleCreateTeam(ClientConnection client, string message)
        {
            var request = JsonConvert.DeserializeObject<CreateTeamRequest>(message);

            if (!battles.TryGetValue(request.BattleId, out var battle))
            {
                await SendErrorResponse(client, "Batalha não encontrada");
                return;
            }

            if (battle.State != BattleState.Preparation)
            {
                await SendErrorResponse(client, "A batalha já foi iniciada ou finalizada");
                return;
            }

            // Verificar limite de equipes (máximo 4 equipes)
            if (battle.Teams.Count >= 4)
            {
                await SendErrorResponse(client, "Número máximo de equipes atingido");
                return;
            }

            // Criar nova equipe
            var teamId = $"team{battle.Teams.Count + 1}";
            var team = new Team
            {
                Id = teamId,
                Name = request.TeamName,
                Players = new List<Player>()
            };

            battle.Teams.Add(team);

            LogMessage($"Nova equipe criada: {team.Name} (ID: {teamId}) na batalha {battle.Id}");

            var response = new CreateTeamResponse
            {
                Success = true,
                Message = "Equipe criada com sucesso",
                TeamId = teamId,
                TeamName = team.Name
            };

            await client.SendMessageAsync(JsonConvert.SerializeObject(response));

            // Notificar outros clientes da nova equipe
            await BroadcastBattleState(battle.Id);
        }

        private async Task HandleSetTeamReady(ClientConnection client, string message)
        {
            var request = JsonConvert.DeserializeObject<SetTeamReadyRequest>(message);

            if (!battles.TryGetValue(request.BattleId, out var battle))
            {
                await SendErrorResponse(client, "Batalha não encontrada");
                return;
            }

            if (battle.State != BattleState.Preparation)
            {
                await SendErrorResponse(client, "A batalha já foi iniciada ou finalizada");
                return;
            }

            // Verificar se o time existe
            var team = battle.Teams.FirstOrDefault(t => t.Id == request.TeamId);
            if (team == null)
            {
                await SendErrorResponse(client, "Time não encontrado");
                return;
            }

            // Atualizar status de prontidão da equipe
            battle.TeamReadyStatus[team.Id] = request.IsReady;

            var response = new BaseResponse
            {
                Success = true,
                Message = $"Status de prontidão atualizado para equipe {team.Name}"
            };

            await client.SendMessageAsync(JsonConvert.SerializeObject(response));

            // Notificar todos os clientes sobre a mudança de status
            await BroadcastBattleState(battle.Id);
        }

        private async Task HandleStartBattle(ClientConnection client, string message)
        {
            var request = JsonConvert.DeserializeObject<StartBattleRequest>(message);

            if (!battles.TryGetValue(request.BattleId, out var battle))
            {
                await SendErrorResponse(client, "Batalha não encontrada");
                return;
            }

            if (battle.State != BattleState.Preparation)
            {
                await SendErrorResponse(client, "A batalha já foi iniciada ou finalizada");
                return;
            }

            // Verificar se há pelo menos um jogador em cada equipe
            bool hasValidTeams = false;

            if (battle.Teams.Count >= 2)  // Para modo PvP precisa de pelo menos 2 equipes
            {
                // Verificar se as equipes estão prontas e têm jogadores
                bool allTeamsReady = true;
                bool allTeamsHavePlayers = true;
                int teamsWithPlayers = 0;

                foreach (var team in battle.Teams)
                {
                    // Verificar se a equipe está pronta (se o status não existe, assume falso)
                    if (!battle.TeamReadyStatus.TryGetValue(team.Id, out bool isReady) || !isReady)
                    {
                        allTeamsReady = false;
                    }

                    // Verificar se a equipe tem pelo menos um jogador
                    if (team.Players.Count == 0)
                    {
                        allTeamsHavePlayers = false;
                    }
                    else
                    {
                        teamsWithPlayers++;
                    }
                }

                // Para PvP, precisa de pelo menos 2 equipes com jogadores
                hasValidTeams = allTeamsReady && allTeamsHavePlayers &&
                    (request.IsPvP ? teamsWithPlayers >= 2 : teamsWithPlayers >= 1);
            }
            else if (battle.Teams.Count == 1 && !request.IsPvP) // Para PvE, uma equipe é suficiente
            {
                var team = battle.Teams[0];
                bool isTeamReady = battle.TeamReadyStatus.TryGetValue(team.Id, out bool ready) && ready;
                hasValidTeams = isTeamReady && team.Players.Count > 0;
            }

            if (!hasValidTeams)
            {
                await SendErrorResponse(client, "Nem todas as equipes estão prontas ou têm jogadores suficientes");
                return;
            }

            // Adicionar inimigos se especificados e não for PvP
            if (!request.IsPvP && request.EnemyIds != null && request.EnemyIds.Count > 0)
            {
                foreach (var enemyId in request.EnemyIds)
                {
                    if (enemyTemplates.TryGetValue(enemyId, out var enemyTemplate))
                    {
                        // Criar uma cópia do modelo do inimigo
                        var enemy = new Enemy
                        {
                            Id = new Random().Next(1000, 9999).ToString(), // ID numérico mais curto
                            Name = enemyTemplate.Name,
                            Health = enemyTemplate.Health,
                            MaxHealth = enemyTemplate.MaxHealth,
                            Attack = enemyTemplate.Attack,
                            Defense = enemyTemplate.Defense,
                            Speed = enemyTemplate.Speed,
                            Skills = enemyTemplate.Skills.ToList()
                        };

                        battle.Enemies.Add(enemy);
                    }
                }
            }

            // Calcular ordem dos turnos com base na velocidade
            var participants = new List<(string Id, int Speed)>();

            // Adicionar jogadores
            foreach (var team in battle.Teams)
            {
                foreach (var player in team.Players)
                {
                    participants.Add((player.Id, player.Speed));
                }
            }

            // Adicionar inimigos (apenas no modo PvE)
            if (!request.IsPvP)
            {
                foreach (var enemy in battle.Enemies)
                {
                    participants.Add((enemy.Id, enemy.Speed));
                }
            }

            // Ordenar por velocidade (maior para menor)
            var orderedParticipants = participants
                .OrderByDescending(p => p.Speed)
                .Select(p => p.Id)
                .ToList();

            battle.TurnOrder = orderedParticipants;
            battle.State = BattleState.InProgress;
            battle.CurrentTurn = 0;
            battle.IsPvP = request.IsPvP;

            LogMessage($"Batalha {battle.Id} iniciada no modo {(request.IsPvP ? "PvP" : "PvE")} com {battle.TurnOrder.Count} participantes");

            var response = new StartBattleResponse
            {
                Success = true,
                Message = $"Batalha iniciada com sucesso no modo {(request.IsPvP ? "PvP" : "PvE")}",
                TurnOrder = orderedParticipants,
                FirstPlayer = orderedParticipants.FirstOrDefault()
            };

            await client.SendMessageAsync(JsonConvert.SerializeObject(response));

            // Notificar todos os clientes da batalha
            await BroadcastStartBattle(battle, response);
        }

        // Método para notificar todos os jogadores sobre o início da batalha
        private async Task BroadcastStartBattle(Battle battle, StartBattleResponse response)
        {
            if (!battleClients.TryGetValue(battle.Id, out var clientIds))
            {
                return;
            }

            string responseJson = JsonConvert.SerializeObject(response);

            foreach (var clientId in clientIds)
            {
                if (clients.TryGetValue(clientId, out var client))
                {
                    await client.SendMessageAsync(responseJson);
                }
            }
        }

        private async Task HandleExecuteAction(ClientConnection client, string message)
        {
            var request = JsonConvert.DeserializeObject<ExecuteActionRequest>(message);

            if (!battles.TryGetValue(request.BattleId, out var battle))
            {
                await SendErrorResponse(client, "Batalha não encontrada");
                return;
            }

            if (battle.State != BattleState.InProgress)
            {
                await SendErrorResponse(client, "A batalha não está em andamento");
                return;
            }

            // Verificar se é o turno do jogador
            if (battle.CurrentParticipant != request.PlayerId)
            {
                await SendErrorResponse(client, "Não é o turno deste jogador");
                return;
            }

            // Encontrar o jogador
            Player player = null;
            foreach (var team in battle.Teams)
            {
                player = team.Players.FirstOrDefault(p => p.Id == request.PlayerId);
                if (player != null)
                    break;
            }

            if (player == null)
            {
                await SendErrorResponse(client, "Jogador não encontrado");
                return;
            }

            // Processar a ação
            var actionResults = new List<ActionResult>();

            switch (request.Action.Type)
            {
                case ActionType.Attack:
                    await ProcessAttack(battle, player, request.Action, actionResults);
                    break;
                case ActionType.Skill:
                    await ProcessSkill(battle, player, request.Action, actionResults);
                    break;
                case ActionType.Item:
                    await ProcessItem(battle, player, request.Action, actionResults);
                    break;
                case ActionType.Pass:
                    // Não faz nada, só passa o turno
                    break;
            }

            // Avançar para o próximo turno
            battle.CurrentTurn++;
            var nextPlayerId = battle.CurrentParticipant;

            // Verificar se alguma equipe foi derrotada
            CheckBattleEndCondition(battle);

            LogMessage($"Jogador {player.Name} executou ação {request.Action.Type}, próximo: {nextPlayerId}");

            var response = new ExecuteActionResponse
            {
                Success = true,
                Message = "Ação executada com sucesso",
                Results = actionResults,
                NextPlayer = nextPlayerId
            };

            await client.SendMessageAsync(JsonConvert.SerializeObject(response));

            // Notificar todos os clientes da batalha
            await BroadcastBattleState(battle.Id);
        }

        private async Task ProcessAttack(Battle battle, Player attacker, Action action, List<ActionResult> results)
        {
            // Encontrar o alvo
            var target = FindTarget(battle, action.TargetId);
            if (target == null)
            {
                return;
            }

            // Calcular dano - modificado para não usar dynamic
            int targetDefense = 0;

            if (target is Player playerTarget)
            {
                targetDefense = playerTarget.Defense;
            }
            else if (target is Enemy enemyTarget)
            {
                targetDefense = enemyTarget.Defense;
            }

            int damage = CalculateDamage(attacker.Attack, targetDefense);

            // Aplicar dano
            if (target is Player playerT)
            {
                playerT.Health = Math.Max(0, playerT.Health - damage);
            }
            else if (target is Enemy enemyT)
            {
                enemyT.Health = Math.Max(0, enemyT.Health - damage);
            }

            // Registrar resultado
            bool isDead = false;
            if (target is Player p) isDead = !p.IsAlive;
            else if (target is Enemy e) isDead = !e.IsAlive;

            var result = new ActionResult
            {
                TargetId = action.TargetId,
                DamageReceived = damage,
                HealingReceived = 0,
                IsDead = isDead
            };

            results.Add(result);
        }

        private async Task ProcessSkill(Battle battle, Player caster, Action action, List<ActionResult> results)
        {
            // Encontrar a habilidade
            var skill = caster.Skills.FirstOrDefault(h => h.Id == action.SkillId);
            if (skill == null)
            {
                return;
            }

            // Verificar custo de mana
            if (caster.Mana < skill.ManaCost)
            {
                return; // Mana insuficiente
            }

            // Consumir mana
            caster.Mana -= skill.ManaCost;
            
            // Marcar que a habilidade foi utilizada (cooldown)
            skill.Use();
            
            // Registrar a ação no histórico da batalha
            battle.ActionHistory.Add(new BattleAction
            {
                ActorId = caster.Id,
                Turn = battle.CurrentTurn,
                Timestamp = DateTime.Now,
                ActionType = ActionType.Skill,
                TargetId = action.TargetId,
                SkillId = action.SkillId
            });

            // Se a habilidade afeta a equipe inteira
            if (skill.AffectsTeam || skill.TargetType == TargetType.AllEnemies || skill.TargetType == TargetType.AllAllies)
            {
                List<Character> targets = new List<Character>();

                // Determinar alvos baseado no tipo de alvo da habilidade
                switch (skill.TargetType)
                {
                    case TargetType.AllAllies:
                        var casterTeam = battle.Teams.FirstOrDefault(t => t.Players.Any(p => p.Id == caster.Id));
                        if (casterTeam != null)
                        {
                            targets.AddRange(casterTeam.Players.Where(p => p.IsAlive));
                        }
                        break;
                        
                    case TargetType.AllEnemies:
                        if (battle.IsPvP)
                        {
                            var playerTeam = battle.Teams.FirstOrDefault(t => t.Players.Any(p => p.Id == caster.Id));
                            if (playerTeam != null)
                            {
                                foreach (var team in battle.Teams.Where(t => t.Id != playerTeam.Id))
                                {
                                    targets.AddRange(team.Players.Where(p => p.IsAlive));
                                }
                            }
                        }
                        else
                        {
                            targets.AddRange(battle.Enemies.Where(e => e.IsAlive));
                        }
                        break;
                        
                    default:
                        // Para skill.AffectsTeam quando TargetType é Single, Area, etc.
                        // Tenta encontrar um time como alvo primeiro
                        var targetTeam = battle.Teams.FirstOrDefault(t => t.Id == action.TargetId);
                        if (targetTeam != null)
                        {
                            targets.AddRange(targetTeam.Players.Where(p => p.IsAlive));
                        }
                        else
                        {
                            // Assume que são todos os inimigos
                            targets.AddRange(battle.Enemies.Where(e => e.IsAlive));
                        }
                        break;
                }

                // Aplicar efeitos da habilidade a todos os alvos
                foreach (var target in targets)
                {
                    var targetResults = skill.ApplyEffects(caster, target, battle);
                    results.AddRange(targetResults);
                }
            }
            else
            {
                // Skill com alvo único
                var target = FindTargetCharacter(battle, action.TargetId);
                if (target == null)
                {
                    return;
                }

                var targetResults = skill.ApplyEffects(caster, target, battle);
                results.AddRange(targetResults);
            }
        }

        private async Task ProcessItem(Battle battle, Player user, Action action, List<ActionResult> results)
        {
            // Implemente lógica de uso de item aqui
            // Por simplicidade, este exemplo não implementa uso de itens
            return;
        }

        private object FindTarget(Battle battle, string targetId)
        {
            // Procurar entre jogadores
            foreach (var team in battle.Teams)
            {
                var player = team.Players.FirstOrDefault(p => p.Id == targetId);
                if (player != null)
                {
                    return player;
                }
            }

            // Procurar entre inimigos
            var enemy = battle.Enemies.FirstOrDefault(e => e.Id == targetId);
            return enemy;
        }

        private Character FindTargetCharacter(Battle battle, string targetId)
        {
            // Procurar entre jogadores
            foreach (var team in battle.Teams)
            {
                var player = team.Players.FirstOrDefault(p => p.Id == targetId);
                if (player != null)
                {
                    return player;
                }
            }

            // Procurar entre inimigos
            var enemy = battle.Enemies.FirstOrDefault(e => e.Id == targetId);
            if (enemy != null)
            {
                return enemy;
            }
            
            return null;
        }

        private bool IsAlly(Player caster, object target, Battle battle)
        {
            // Se target é jogador, verificar se está na mesma equipe
            if (target is Player targetPlayer)
            {
                foreach (var team in battle.Teams)
                {
                    bool casterInTeam = team.Players.Any(p => p.Id == caster.Id);
                    bool targetInTeam = team.Players.Any(p => p.Id == targetPlayer.Id);

                    if (casterInTeam && targetInTeam)
                    {
                        return true;
                    }
                }
                return false;
            }

            // Se target é inimigo, não é aliado
            return false;
        }

        private int CalculateDamage(int attackPower, int targetDefense)
        {
            // Fórmula de dano básico
            float damageReduction = targetDefense / 100f;
            int baseDamage = attackPower;
            int finalDamage = (int)(baseDamage * (1 - Math.Min(0.75f, damageReduction)));

            // Adicionar aleatoriedade (+-10%)
            Random random = new Random();
            float variation = random.Next(-10, 11) / 100f;
            finalDamage = (int)(finalDamage * (1 + variation));

            return Math.Max(1, finalDamage); // Mínimo 1 de dano
        }

        private int CalculateSkillDamage(Player caster, Skill skill, object target)
        {
            int targetDefense = 0;

            if (target is Player playerTarget)
                targetDefense = playerTarget.Defense;
            else if (target is Enemy enemyTarget)
                targetDefense = enemyTarget.Defense;

            float damageReduction = targetDefense / 100f;
            int baseDamage = skill.Damage;
            int finalDamage = (int)(baseDamage * (1 - Math.Min(0.75f, damageReduction)));

            // Adicionar aleatoriedade (+-10%)
            Random random = new Random();
            float variation = random.Next(-10, 11) / 100f;
            finalDamage = (int)(finalDamage * (1 + variation));

            return Math.Max(1, finalDamage);
        }

        private void CheckBattleEndCondition(Battle battle)
        {
            bool battleEnded = false;

            if (battle.IsPvP)
            {
                // Em PvP, verificar se apenas um time tem jogadores vivos
                int teamsWithLivingPlayers = 0;
                Team winningTeam = null;

                foreach (var team in battle.Teams)
                {
                    if (team.Players.Any(p => p.IsAlive))
                    {
                        teamsWithLivingPlayers++;
                        winningTeam = team;
                    }
                }

                if (teamsWithLivingPlayers <= 1)
                {
                    battle.State = BattleState.Finished;
                    battle.FinishedAt = DateTime.Now;
                    battleEnded = true;

                    if (winningTeam != null)
                    {
                        battle.WinnerTeamId = winningTeam.Id;
                        LogMessage($"Batalha PvP {battle.Id} finalizada. Time vencedor: {winningTeam.Name}");
                    }
                    else
                    {
                        LogMessage($"Batalha PvP {battle.Id} finalizada. Nenhum vencedor (empate).");
                    }
                }
            }
            else
            {
                // No modo PvE, verificar se todos os jogadores ou inimigos estão derrotados
                bool allPlayersDead = battle.Teams.All(t => !t.Players.Any(p => p.IsAlive));
                bool allEnemiesDead = battle.Enemies.Count == 0 || battle.Enemies.All(e => !e.IsAlive);

                if (allPlayersDead || allEnemiesDead)
                {
                    battle.State = BattleState.Finished;
                    battle.FinishedAt = DateTime.Now;
                    battleEnded = true;

                    // Definir o time vencedor para recompensas de XP
                    if (allEnemiesDead)
                    {
                        // Todos os times de jogadores vencem contra os inimigos
                        LogMessage($"Batalha PvE {battle.Id} finalizada. Vencedor: Jogadores");
                    }
                    else
                    {
                        LogMessage($"Batalha PvE {battle.Id} finalizada. Vencedor: Inimigos");
                    }
                }
            }

            // Se a batalha terminou, recompensar os jogadores com experiência
            if (battleEnded)
            {
                _ = RewardExperienceAsync(battle);
            }
        }

        private async Task HandleGetBattleState(ClientConnection client, string message)
        {
            var request = JsonConvert.DeserializeObject<GetBattleStateRequest>(message);
            string battleId = request.BattleId;

            // Se temos um código de sala em vez de um ID de batalha
            if (string.IsNullOrEmpty(battleId) && !string.IsNullOrEmpty(request.RoomCode))
            {
                if (roomCodeToBattleId.TryGetValue(request.RoomCode, out string id))
                {
                    battleId = id;
                }
                else
                {
                    await SendErrorResponse(client, "Código de sala inválido");
                    return;
                }
            }

            if (!battles.TryGetValue(battleId, out var battle))
            {
                await SendErrorResponse(client, "Batalha não encontrada");
                return;
            }

            var response = new GetBattleStateResponse
            {
                Success = true,
                Message = "Estado da batalha obtido com sucesso",
                Battle = battle,
                BattleId = battleId  // Incluir o ID da batalha na resposta
            };

            await client.SendMessageAsync(JsonConvert.SerializeObject(response));
        }

        private async Task BroadcastBattleState(string battleId)
        {
            if (!battles.TryGetValue(battleId, out var battle))
            {
                return;
            }

            var response = new GetBattleStateResponse
            {
                Success = true,
                Message = "Atualização de estado da batalha",
                Battle = battle,
                BattleId = battleId
            };

            var message = JsonConvert.SerializeObject(response);
            await NotifyBattleClients(battleId, message);
        }

        private async Task NotifyBattleClients(string battleId, string message)
        {
            if (!battleClients.TryGetValue(battleId, out var clientIds))
            {
                return;
            }

            List<Task> sendTasks = new List<Task>();

            foreach (var clientId in clientIds)
            {
                if (clients.TryGetValue(clientId, out var client) && client.IsConnected)
                {
                    sendTasks.Add(client.SendMessageAsync(message));
                }
            }

            // Aguardar todas as mensagens serem enviadas
            await Task.WhenAll(sendTasks);
        }

        #endregion

        private void LogMessage(string message)
        {
            OnLog?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        // Método para gerenciar a experiência do jogador após uma batalha
        private async Task RewardExperienceAsync(Battle battle)
        {
            if (battle.State != BattleState.Finished)
                return;

            int baseXP = battle.IsPvP ? 50 : 30;
            double winnerMultiplier = 1.5;

            foreach (var team in battle.Teams)
            {
                bool isWinningTeam = false;

                if (battle.IsPvP)
                {
                    isWinningTeam = team.Players.Any(p => p.IsAlive);
                }
                else
                {
                    isWinningTeam = battle.Enemies.All(e => !e.IsAlive);
                }

                foreach (var player in team.Players)
                {
                    int xpAmount = baseXP;

                    if (isWinningTeam)
                        xpAmount = (int)(xpAmount * winnerMultiplier);

                    if (battle.IsPvP)
                    {
                        double avgEnemyLevel = GetAverageEnemyLevel(battle, team.Id);
                        xpAmount += (int)(avgEnemyLevel * 5);
                    }
                    else
                    {
                        xpAmount += battle.Enemies.Count * 10;
                    }

                    // Find the client that controls this player and add experience to their session
                    string clientId = FindClientIdForPlayer(battle.Id, player.Id);
                    if (!string.IsNullOrEmpty(clientId))
                    {
                        await AddSessionExperienceAsync(clientId, player.Class, xpAmount, player);
                    }
                }
            }
        }

        // Add missing method to create player with session progress
        private Player CreatePlayerWithSessionProgress(string clientId, string playerId, string playerName, string className)
        {
            // Initialize session progress for this client if it doesn't exist
            if (!sessionProgressData.ContainsKey(clientId))
            {
                sessionProgressData[clientId] = new Dictionary<string, ClassProgress>();
            }

            // Get or create class progress for this class
            if (!sessionProgressData[clientId].ContainsKey(className))
            {
                sessionProgressData[clientId][className] = new ClassProgress(className);
            }

            var classProgress = sessionProgressData[clientId][className];

            // Create player with stats based on level
            var player = new Player
            {
                Id = playerId,
                Name = playerName,
                Class = className,
                Level = classProgress.Level,
                Health = GetBaseHealth(className) + (classProgress.Level - 1) * 20,
                MaxHealth = GetBaseHealth(className) + (classProgress.Level - 1) * 20,
                Mana = GetBaseMana(className) + (classProgress.Level - 1) * 10,
                MaxMana = GetBaseMana(className) + (classProgress.Level - 1) * 10,
                Attack = GetBaseAttack(className) + (classProgress.Level - 1) * 5,
                Defense = GetBaseDefense(className) + (classProgress.Level - 1) * 3,
                Speed = GetBaseSpeed(className) + (classProgress.Level - 1) * 2,
                Skills = GetSkillsForClass(className, classProgress.Level)
            };

            return player;
        }

        private int GetBaseHealth(string className)
        {
            return className.ToLower() switch
            {
                "warrior" => 150,
                "mage" => 100,
                "healer" => 120,
                _ => 100
            };
        }

        private int GetBaseMana(string className)
        {
            return className.ToLower() switch
            {
                "warrior" => 50,
                "mage" => 120,
                "healer" => 100,
                _ => 50
            };
        }

        private int GetBaseAttack(string className)
        {
            return className.ToLower() switch
            {
                "warrior" => 70,
                "mage" => 60,
                "healer" => 40,
                _ => 50
            };
        }

        private int GetBaseDefense(string className)
        {
            return className.ToLower() switch
            {
                "warrior" => 60,
                "mage" => 30,
                "healer" => 50,
                _ => 40
            };
        }

        private int GetBaseSpeed(string className)
        {
            return className.ToLower() switch
            {
                "warrior" => 50,
                "mage" => 70,
                "healer" => 60,
                _ => 50
            };
        }

        private List<Skill> GetSkillsForClass(string className, int level)
        {
            var skills = new List<Skill>();

            switch (className.ToLower())
            {
                case "warrior":
                    skills.Add(new Skill
                    {
                        Id = "skill_warrior_slash",
                        Name = "Golpe Poderoso",
                        Description = "Um ataque físico poderoso",
                        Damage = 80 + (level * 5),
                        ManaCost = 20,
                        Range = 1,
                        CurrentCooldown = 0,
                        MaxCooldown = 2
                    });

                    if (level >= 3)
                    {
                        skills.Add(new Skill
                        {
                            Id = "skill_warrior_taunt",
                            Name = "Provocar",
                            Description = "Força o inimigo a atacar você",
                            ManaCost = 15,
                            Range = 1,
                            CurrentCooldown = 0,
                            MaxCooldown = 3
                        });
                    }
                    break;

                case "mage":
                    skills.Add(new Skill
                    {
                        Id = "skill_mage_fireball",
                        Name = "Bola de Fogo",
                        Description = "Lança uma bola de fogo mágica",
                        Damage = 90 + (level * 6),
                        ManaCost = 30,
                        Range = 3,
                        CurrentCooldown = 0,
                        MaxCooldown = 1
                    });

                    if (level >= 2)
                    {
                        skills.Add(new Skill
                        {
                            Id = "skill_mage_lightning",
                            Name = "Zoltraak",
                            Description = "Precise Demon-Killing Magic",
                            Damage = 70 + (level * 4),
                            ManaCost = 25,
                            Range = 2,
                            CurrentCooldown = 0,
                            MaxCooldown = 1
                        });
                    }
                    break;

                case "healer":
                    skills.Add(new Skill
                    {
                        Id = "skill_healer_heal",
                        Name = "Curar",
                        Description = "Restaura a vida de um aliado",
                        Healing = 80 + (level * 8),
                        ManaCost = 25,
                        Range = 3,
                        CurrentCooldown = 0,
                        MaxCooldown = 1
                    });

                    if (level >= 2)
                    {
                        skills.Add(new Skill
                        {
                            Id = "skill_healer_shield",
                            Name = "Escudo Mágico",
                            Description = "Aumenta a defesa de um aliado",
                            ManaCost = 20,
                            Range = 3,
                            CurrentCooldown = 0,
                            MaxCooldown = 2
                        });
                    }
                    break;
            }

            return skills;
        }

        private string FindClientIdForPlayer(string battleId, string playerId)
        {
            if (!battleClients.TryGetValue(battleId, out var clientIds))
                return null;

            // For this simple implementation, we'll find the client that has this player
            // In a more complex system, you'd maintain a player-to-client mapping
            foreach (var clientId in clientIds)
            {
                if (sessionProgressData.ContainsKey(clientId))
                {
                    // This is a simplified approach - in production you'd want better tracking
                    return clientId;
                }
            }

            return clientIds.FirstOrDefault();
        }

        private async Task AddSessionExperienceAsync(string clientId, string className, int xpAmount, Player player)
        {
            if (!sessionProgressData.ContainsKey(clientId) ||
                !sessionProgressData[clientId].ContainsKey(className))
            {
                LogMessage($"No session progress found for client {clientId}, class {className}");
                return;
            }

            var classProgress = sessionProgressData[clientId][className];

            classProgress.Experience += xpAmount;
            bool leveledUp = false;
            var newSkills = new List<string>();
            var statBonuses = new Dictionary<string, int>();

            // Check for level up
            int xpForNextLevel = CalculateXpForNextLevel(classProgress.Level);
            while (classProgress.Experience >= xpForNextLevel)
            {
                classProgress.Experience -= xpForNextLevel;
                classProgress.Level++;
                leveledUp = true;

                // Add stat bonuses
                statBonuses["Health"] = 20;
                statBonuses["Mana"] = 10;
                statBonuses["Attack"] = 5;
                statBonuses["Defense"] = 3;
                statBonuses["Speed"] = 2;

                // Check for new skills
                var newPlayerSkills = GetSkillsForClass(className, classProgress.Level);
                var currentSkillIds = player.Skills.Select(s => s.Id).ToHashSet();

                foreach (var skill in newPlayerSkills)
                {
                    if (!currentSkillIds.Contains(skill.Id))
                    {
                        newSkills.Add(skill.Name);
                        player.Skills.Add(skill);
                    }
                }

                xpForNextLevel = CalculateXpForNextLevel(classProgress.Level);

                LogMessage($"Client {clientId} leveled up class {className} to level {classProgress.Level}");
            }

            // Send experience update to client
            var response = new ExperienceUpdateResponse
            {
                Success = true,
                Message = $"Gained {xpAmount} XP" + (leveledUp ? " and leveled up!" : ""),
                Level = classProgress.Level,
                Experience = classProgress.Experience,
                ExperienceToNextLevel = xpForNextLevel,
                XpGained = xpAmount,
                Class = className,
                LeveledUp = leveledUp,
                NewSkillsLearned = newSkills,
                StatBonuses = statBonuses
            };

            if (clients.TryGetValue(clientId, out var client))
            {
                await client.SendMessageAsync(JsonConvert.SerializeObject(response));
            }
        }

        private double GetAverageEnemyLevel(Battle battle, string excludeTeamId)
        {
            var enemyPlayers = battle.Teams
                .Where(t => t.Id != excludeTeamId)
                .SelectMany(t => t.Players)
                .Where(p => p.IsAlive);

            return enemyPlayers.Any() ? enemyPlayers.Average(p => p.Level) : 1.0;
        }

        private int CalculateXpForNextLevel(int level)
        {
            return level * 100 + (level - 1) * 50;
        }
    }

    public class ClassProgress
    {
        public string Class { get; set; }
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;

        public ClassProgress(string className)
        {
            Class = className;
        }
    }
}