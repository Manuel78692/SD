using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace AGREGADOR {
    public class AgregadorMain
    {
        // Ip do SERVIDOR
        private static string servidorIp = "127.0.0.1";

        // Porta para enviar os dados para o SERVIDOR
        private static int servidorPort = 5000;

        private static List<Agregador> agregadores = new List<Agregador>();
        private static List<Task> agregadorTasks = new List<Task>();
        private static ConcurrentDictionary<string, ConcurrentQueue<string>> _agregadorLogs = new ConcurrentDictionary<string, ConcurrentQueue<string>>();

        // public static void Main()
        // {
        //     // Start Agregadores in the background
        //     Task.Run(async () => await RunAsyncInternal()); // Fire and forget Agregador startups

        // }
        public static void Init()
        {
            Console.WriteLine("Iniciando Agregadores...");
            Agregador agregador01 = new Agregador("AGREGADOR01", 5001, servidorIp, servidorPort);
            Agregador agregador02 = new Agregador("AGREGADOR02", 5002, servidorIp, servidorPort);
            // Agregador agregador03 = new Agregador("AGREGADOR03", servidorIp, servidorPort);


            agregadores.Add(agregador01);
            agregadores.Add(agregador02);
            // agregadores.Add(agregador03);

            //agregadorTasks.Add(Task.Run(() => agregador01.Run()));
            //agregadorTasks.Add(Task.Run(() => agregador02.Run()));
            // agregadorTasks.Add(Task.Run(() => agregador03.Run()));

            // This will now run the menu concurrently with the agregadores
            // await Task.WhenAll(agregadorTasks); // We won't wait for all to finish here anymore
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
                    }
                };
                agregadorTasks.Add(Task.Run(() => agregador.Run()));
            }
            Console.WriteLine("Agregadores iniciados e logs sendo coletados."); // Main feedback
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
            Console.WriteLine("=== Logs dos AGREGADORes (Pressione qualquer tecla para voltar) ===");

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
    }
}