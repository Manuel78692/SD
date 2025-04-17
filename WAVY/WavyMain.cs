using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Concurrent;

class WavyMain
{
    // Ip dos AGREGADORES
    private static string agregadorIp = "127.0.0.1";

    // Lista dos WAVYs
    private static Wavy[] wavys;

    // Para guardar os logs de cada WAVY
    private static ConcurrentDictionary<string, ConcurrentQueue<string>> _sendLogs = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
    
    // Para parar o loop de forma segura
    private static CancellationTokenSource _cts = new CancellationTokenSource();

    // Esta função simula os diferentes WAVYs
    // Cria instâncias da classe e corre todas simultaneamente
    public static void Main()
    {
        wavys = new Wavy[]
        {
            new Wavy("WAVY01", agregadorIp, 5001, new List<TipoDado> { TipoDado.GPS, TipoDado.Gyro }),
            new Wavy("WAVY02", agregadorIp, 5002, new List<TipoDado> { TipoDado.GPS }),
        };

        // Aqui, cada Wavy é inicializado e o evento OnDataBlockReady é associado a uma fila de logs
        foreach(var w in wavys)
        {
            // Inicializa a fila de logs para cada Wavy
            _sendLogs[w.id] = new ConcurrentQueue<string>();

            // Associa o evento OnDataBlockReady a uma função que adiciona os logs à fila
            w.OnDataBlockReady += block =>
            {
                // Adiciona o log à fila correspondente ao Wavy
                _sendLogs[w.id].Enqueue($"{DateTime.Now:HH:mm:ss} → {block}");
            };

            // Inicia a receção de dados
            Task.Run(() => w.ReceberDados(_cts.Token));
        }
        // Inicia a interface na consola
        RunAsync().Wait();
    }

    // Esta função gere a interface na consola
    private static async Task RunAsync()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Gestão de WAVYs ===");
            Console.WriteLine("1. Listar WAVYs");
            Console.WriteLine("2. Mostrar envio de dados");
            Console.WriteLine("3. Alterar Estado de uma WAVY");
            Console.WriteLine("4. Sair");
            Console.Write("Escolha uma opção: ");
            string opcao = Console.ReadLine();

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
                    _cts.Cancel();
                    Console.Clear();
                    return;
                default:
                    Console.WriteLine("Opção inválida. Pressione qualquer tecla para continuar...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    // Esta função lista todas as WAVYs existentes
    private static void ListarWavys()
    {
        Console.Clear();
        Console.WriteLine("=== Lista de WAVYs ===");
        foreach (var wavy in wavys)
        {
            Console.WriteLine($"ID: {wavy.id}, Estado: {wavy.estadoWavy}");
        }
        Console.WriteLine("Pressione qualquer tecla para voltar ao menu...");
        Console.ReadKey();
    }

    // Esta função mostra os logs das WAVYs
    private static async Task MostrarEnvioDados()
    {
        Console.Clear();
        Console.WriteLine("=== Enviando Dados (Pressione qualquer tecla para voltar ao menu) ===");

        while (true)
        {
            // Se o utilizador pressionou uma tecla, volta para o menu principal
            if (Console.KeyAvailable)
            {
                Console.ReadKey(true);
                break;
            }

            // Fazer print de todos os logs das WAVYs
            foreach (var w in wavys)
            {
                var queue = _sendLogs[w.id];
                while (queue.TryDequeue(out var logEntry))
                    Console.WriteLine($"[{w.id}] {logEntry}");
            }

            // Pausar para não estar a fazer looping constantemente
            await Task.Delay(200);
        }
    }

    // Esta função altera o estado da WAVY conforme o input do utilizador
    private static void AlterarEstadoWavy()
    {
        Console.Clear();
        Console.WriteLine("=== Alterar Estado de uma WAVY ===");
        Console.WriteLine("Digite o ID da WAVY:");
        string id = Console.ReadLine();

        var wavy = Array.Find(wavys, w => w.id == id);
        if (wavy == null)
        {
            Console.WriteLine("WAVY não encontrada. Pressione qualquer tecla para voltar ao menu...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"Estado atual da {wavy.id}: {wavy.estadoWavy}");
        Console.WriteLine($"Digite o novo estado ({string.Join("/", Enum.GetNames(typeof(Estado)))}):");
        string novoEstado = Console.ReadLine();

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
}
