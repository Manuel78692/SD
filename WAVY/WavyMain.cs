using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace WAVY
{
    public class WavyMain
    {
        private static string agregadorIp = "127.0.0.1";
        private static Wavy[]? wavys;
        private static ConcurrentDictionary<string, ConcurrentQueue<string>> _sendLogs = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
        private static CancellationTokenSource _cts = new CancellationTokenSource();

        public static void Init()
        {
            wavys = new Wavy[]
            {
                new Wavy("WAVY01", agregadorIp, 5001, new List<TipoDado> { TipoDado.GPS, TipoDado.Gyro }),
                new Wavy("WAVY02", agregadorIp, 5002, new List<TipoDado> { TipoDado.GPS }),
            };

            foreach (var w in wavys)
            {
                _sendLogs[w.id] = new ConcurrentQueue<string>();

                w.OnDataBlockReady += block =>
                {
                    _sendLogs[w.id].Enqueue($"{DateTime.Now:HH:mm:ss} → {block}");
                };

                Task.Run(() => w.ReceberDados(_cts.Token));
            }
        }

        public static void ListarWavys()
        {
            Console.WriteLine("=== Lista de WAVYs ===");
            if (wavys == null)
            {
                Console.WriteLine("Nenhuma WAVY inicializada.");
                return;
            }
            foreach (var wavy in wavys)
            {
                Console.WriteLine($"ID: {wavy.id}, Estado: {wavy.estadoWavy}");
            }
        }

        public static async Task MostrarEnvioDados()
        {
            Console.WriteLine("=== Enviando Dados (Pressione qualquer tecla para voltar ao menu) ===");
            if (wavys == null)
            {
                Console.WriteLine("Nenhuma WAVY inicializada.");
                return;
            }
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    break;
                }
                foreach (var w in wavys)
                {
                    var queue = _sendLogs[w.id];
                    while (queue.TryDequeue(out var logEntry))
                        Console.WriteLine($"[{w.id}] {logEntry}");
                }
                await Task.Delay(200);
            }
        }

        public static void AlterarEstadoWavy()
        {
            Console.WriteLine("=== Alterar Estado de uma WAVY ===");
            if (wavys == null)
            {
                Console.WriteLine("Nenhuma WAVY inicializada.");
                return;
            }
            Console.WriteLine("Digite o ID da WAVY:");
            string? id = Console.ReadLine();
            var wavy = wavys != null ? Array.Find(wavys, w => w.id == id) : null;
            if (wavy == null)
            {
                Console.WriteLine("WAVY não encontrada. Pressione qualquer tecla para voltar ao menu...");
                Console.ReadKey();
                return;
            }
            Console.WriteLine($"Estado atual da {wavy.id}: {wavy.estadoWavy}");
            Console.WriteLine($"Digite o novo estado ({string.Join("/", Enum.GetNames(typeof(Estado)))}):");
            string? novoEstado = Console.ReadLine();
            if (Enum.TryParse(novoEstado, true, out Estado estado))
            {
                wavy.estadoWavy = estado;
                Console.WriteLine($"Estado da {wavy.id} alterado para {wavy.estadoWavy}.");
            }
            else
            {
                Console.WriteLine("Estado inválido.");
            }
            Console.WriteLine("Pressione qualquer tecla para voltar ao menu...");
            Console.ReadKey();
        }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Iniciando sistema WAVY...");
            Init();
            await Task.Delay(1000);

            while (true)
            {
                Console.WriteLine("\n=== Menu Principal ===");
                Console.WriteLine("1. Listar WAVYs");
                Console.WriteLine("2. Mostrar Envio de Dados");
                Console.WriteLine("3. Alterar Estado de uma WAVY");
                Console.WriteLine("4. Sair");
                Console.Write("Escolha uma opção: ");

                var opcao = Console.ReadLine();

                switch (opcao)
                {
                    case "1":
                        ListarWavys();
                        break;
                    case "2":
                        await MostrarEnvioDados();
                        break;
                    case "3":
                        AlterarEstadoWavy();
                        break;
                    case "4":
                        Console.WriteLine("Saindo...");
                        _cts.Cancel();
                        return;
                    default:
                        Console.WriteLine("Opção inválida. Tente novamente.");
                        break;
                }
            }
        }
    }
}
