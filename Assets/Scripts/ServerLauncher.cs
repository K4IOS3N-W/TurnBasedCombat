using System;
using System.Threading.Tasks;
using BattleSystem.Server;

namespace BattleSystem
{
    public static class ServerLauncher
    {
        public static async Task Main(string[] args)
        {
            int port = 7777;
            if (args.Length > 0 && int.TryParse(args[0], out var customPort))
            {
                port = customPort;
            }

            Console.WriteLine($"Iniciando servidor na porta {port}...");

            BattleServer server = new BattleServer(port);

            server.OnLog += (message) => Console.WriteLine(message);
            server.OnError += (exception) => Console.WriteLine($"ERRO: {exception.Message}");

            Console.WriteLine("Servidor iniciado. Pressione Ctrl+C para encerrar.");

            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;
                server.Stop();
            };

            await server.Start();
        }
    }
}