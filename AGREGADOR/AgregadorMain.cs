using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace AGREGADOR
{
    public class AgregadorMain
    {
        // Ip do SERVIDOR
        private static string servidorIp = "127.0.0.1";

        // Porta para enviar os dados para o SERVIDOR
        private static int servidorPort = 5010;

        private static List<Agregador> agregadores = new List<Agregador>();
        private static List<Task> agregadorTasks = new List<Task>();
        private static ConcurrentDictionary<string, ConcurrentQueue<string>> _agregadorLogs = new ConcurrentDictionary<string, ConcurrentQueue<string>>();        public static void Init()
        {
            Console.WriteLine("AgregadorMain.Init() - Iniciando Agregadores...");
            Agregador agregador01 = new Agregador("AGREGADOR01", 5001, servidorIp, servidorPort);
            Agregador agregador02 = new Agregador("AGREGADOR02", 5002, servidorIp, servidorPort);
            // Agregador agregador03 = new Agregador("AGREGADOR03", servidorIp, servidorPort);

            agregadores.Add(agregador01);
            agregadores.Add(agregador02);
            // agregadores.Add(agregador03);

            Console.WriteLine($"AgregadorMain.Init() - {agregadores.Count} agregadores criados. Iniciando setup de logs e tasks...");

            // Setup logging and start tasks for each agregador
            foreach (var agregador in agregadores)
            {
                string currentAgregadorId = agregador.GetId(); // Assuming GetId() exists
                _agregadorLogs[currentAgregadorId] = new ConcurrentQueue<string>();
                agregador.OnLogEntry += (logMessage) =>
                {
                    // Check if the queue exists, to be safe, though it should.
                    if (_agregadorLogs.TryGetValue(currentAgregadorId, out var queue))
                    {
                        queue.Enqueue($"{DateTime.Now:HH:mm:ss} → {logMessage}");
                    }                };
                Console.WriteLine($"AgregadorMain.Init() - Iniciando task para {currentAgregadorId}...");
                agregadorTasks.Add(Task.Run(async () => await agregador.Run()));
            }
            
            Console.WriteLine($"AgregadorMain.Init() - Agregadores iniciados e logs sendo coletados. {agregadorTasks.Count} tasks iniciadas."); // Main feedback
        }

        private static async Task Main()
        {
            Console.WriteLine("Iniciando sistema AGREGADOR...");
            Init();
            await Task.Delay(1000);

            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("=== Gestão de AGREGADORes ===");
                Console.WriteLine("1. Listar AGREGADORes (Status)");
                Console.WriteLine("2. Simular Falha de AGREGADOR");
                Console.WriteLine("3. Mostrar Logs dos AGREGADORes"); // New option
                Console.WriteLine("4. Sair do Painel de Gestão (Agregadores continuarão rodando)");
                Console.WriteLine("5. Sair e Desligar Tudo (Experimental)");
                Console.Write("Escolha uma opção: ");
                string? opcao = Console.ReadLine();

                switch (opcao)
                {
                    case "1":
                        ListarAgregadores();
                        break;
                    case "2":
                        SimularFalhaAgregador();
                        break;
                    case "3":
                        await MostrarLogsAgregadores();
                        break;
                    case "4":
                        Console.WriteLine("Painel de gestão encerrado. Agregadores continuam em execução em background.");
                        exit = true;
                        break;
                    case "5":
                        Console.WriteLine("Tentando desligar todos os agregadores...");
                        foreach (var agregador in agregadores)
                        {
                            agregador.SimulateFailure(); // Request graceful shutdown
                        }
                        // Give some time for them to shut down before exiting main
                        await Task.WhenAll(agregadorTasks.ToArray());
                        Console.WriteLine("Todos os agregadores devem ter sido desligados.");
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Opção inválida. Pressione qualquer tecla para continuar...");
                        Console.ReadKey();
                        break;
                }
                if (!exit && opcao != "3" && opcao != "4") // Don't pause if exiting
                {
                    Console.WriteLine("\nPressione qualquer tecla para voltar ao menu...");
                    Console.ReadKey();
                }
            }
        }

        public static void ListarAgregadores()
        {
            Console.Clear();
            Console.WriteLine("=== Lista de AGREGADORes ===");
            if (!agregadores.Any())
            {
                Console.WriteLine("Nenhum agregador iniciado ou registrado.");
                return;
            }
            for (int i = 0; i < agregadores.Count; i++)
            {
                // Accessing agregador.id directly. Need to ensure Agregador class exposes Id publicly.
                // If Agregador.id is private, add a public getter or make it public.
                // For this example, let's assume 'id' in Agregador is public or has a public getter.
                // String agregadorId = agregadores[i].id; // Assuming Agregador has a public 'id' field/property

                // We need a way to get the ID without direct access if it's private.
                // For simplicity, let's assume we use its index if ID is not easily accessible
                // Or better, ensure Agregador has a public Id property.
                // Let's assume `agregadores[i].id` is accessible for now.
                // If Agregador.id is private, you'd need: public string Id => id; in Agregador.cs
                Console.WriteLine($"{i + 1}. AGREGADOR ID: {agregadores[i].GetId()} - Status Task: {agregadorTasks[i].Status}");
            }
        }
        // Add GetId() to Agregador.cs: public string GetId() => id;
        public static async Task MostrarLogsAgregadores()
        {
            Console.Clear();
            Console.WriteLine("=== Logs dos AGREGADORes (Pressione qualquer tecla para voltar ao menu) ===");
            Console.WriteLine("--- Os logs mais recentes podem demorar um pouco para aparecer ---");

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => { cts.Cancel(); e.Cancel = true; }; // Allow Ctrl+C to exit this view

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(true); // Consume the key
                        break; // Exit if any key is pressed
                    }

                    bool logsDisplayed = false;
                    foreach (var kvp in _agregadorLogs)
                    {
                        string agregadorId = kvp.Key;
                        ConcurrentQueue<string> queue = kvp.Value;
                        while (queue.TryDequeue(out var logEntry))
                        {
                            Console.WriteLine($"[{agregadorId}] {logEntry}");
                            logsDisplayed = true;
                        }
                    }

                    if (!logsDisplayed)
                    {
                        // Optional: add a small delay if no logs were displayed to prevent tight loop if console is empty
                        // but typically not needed if Task.Delay is present.
                    }
                    await Task.Delay(200, cts.Token); // Check for new logs every 200ms
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when exiting via Ctrl+C or key press if cts was passed to Task.Delay
            }
            finally
            {
                Console.CancelKeyPress -= (s, e) => { cts.Cancel(); e.Cancel = true; };
            }
        }

        public static void SimularFalhaAgregador()
        {
            Console.Clear();
            ListarAgregadores();
            if (!agregadores.Any()) return;

            Console.Write("\nDigite o número do AGREGADOR para simular falha: ");
            if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= agregadores.Count)
            {
                Agregador agregadorParaFalhar = agregadores[index - 1];
                Console.WriteLine($"Simulando falha para AGREGADOR: {agregadorParaFalhar.GetId()}...");
                agregadorParaFalhar.SimulateFailure();
                Console.WriteLine($"Comando de falha enviado. O Agregador {agregadorParaFalhar.GetId()} deve parar de consumir e desconectar do RabbitMQ.");
                Console.WriteLine("As mensagens que estavam destinadas a ele (preferred) devem expirar (TTL) e ir para a fila de fallback.");
            }
            else
            {
                Console.WriteLine("Seleção inválida.");
            }
        }
    }   
}