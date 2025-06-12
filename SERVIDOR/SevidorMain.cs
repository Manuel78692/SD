using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace SERVIDOR 
{
    public class ServidorMain
    {
        private static Servidor servidor = new();
        private static ConcurrentDictionary<string, ConcurrentQueue<string>> _servidorLogs = new ConcurrentDictionary<string, ConcurrentQueue<string>>();

        public static void Init()
        {
            string servidorId = servidor.GetId();
            _servidorLogs[servidorId] = new ConcurrentQueue<string>();
            servidor.OnLogEntry += (logMessage) =>
            {
                // Check if the queue exists, to be safe, though it should.
                if (_servidorLogs.TryGetValue(servidorId, out var queue))
                {
                    queue.Enqueue($"{DateTime.Now:HH:mm:ss} → {logMessage}");
                }
            };
            servidor.Run();

            Console.WriteLine("Agregadores iniciados e logs sendo coletados."); // Main feedback
        }


        // Add GetId() to Agregador.cs: public string GetId() => id;
        public static async Task MostrarLogsServidor()
        {
            Console.Clear();
            Console.WriteLine("=== Logs do SERVIDOR (Pressione qualquer tecla para voltar) ===");

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
                    foreach (var kvp in _servidorLogs)
                    {
                        string servidorId = kvp.Key;
                        ConcurrentQueue<string> queue = kvp.Value;
                        while (queue.TryDequeue(out var logEntry))
                        {
                            Console.WriteLine($"[{servidorId}] {logEntry}");
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

        private static async Task Main()
        {
            // Só para ter alguma coisa
        }
    }
}