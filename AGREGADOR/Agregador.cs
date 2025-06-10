using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AGREGADOR
{
    public class Agregador // ← Corrigido aqui!
    {
        private string id;
        private readonly int port;
        private readonly string servidorIp;
        private readonly int servidorPort;
        private readonly string dataFolder = "dados";
        private string? agregadorFilePath;
        private readonly Mutex wavysFileMutex = new Mutex();   

        public Agregador(string _id, int _port, string _servidorIp, int _servidorPort)
        {
            id = _id;
            port = _port;
            servidorIp = _servidorIp;
            servidorPort = _servidorPort;
        }

        public void Run()
        {
            if (!Directory.Exists(dataFolder))
            {
                Console.WriteLine($"Erro: Pasta '{dataFolder}/' não existe.\n");
                return;
            }

            InitializeCSV();

            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("Agregador iniciado na porta " + port + ". Aguardando conexões das WAVYs...");

            while (true)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Console.WriteLine("Conexão de uma WAVY recebida.\n");
                    Task.Run(() => ProcessaWavy(client));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro ao aceitar conexão: " + ex.Message + "\n");
                }
            }
        }

        private void InitializeCSV()
        {
            agregadorFilePath = $"wavys_{id}.csv";
            string filePath = Path.Combine(dataFolder, agregadorFilePath);
            if (!File.Exists(filePath))
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("WAVY_ID:status:[data_types]:last_sync");
                }
                Console.WriteLine($"Arquivo '{agregadorFilePath}' criado na pasta '{dataFolder}'.");
            }
        }

        private async Task ProcessaWavy(TcpClient client)
        {
            try
            {
                using (client)
                {
                    NetworkStream stream = client.GetStream();
                    using (StreamReader reader = new StreamReader(stream))
                    using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                    {
                        string? header = reader.ReadLine();
                        Console.WriteLine("Header recebido: " + header);

                        if (header != null && header.StartsWith("BLOCK"))
                        {
                            string[] partes = header.Split(' ');
                            if (partes.Length == 4 && int.TryParse(partes[1], out int numLinhas) && partes[2] == "STATUS")
                            {
                                string status = partes[3];
                                string[] bloco = new string[numLinhas];
                                for (int i = 0; i < numLinhas; i++)
                                {
                                    bloco[i] = reader.ReadLine() ?? string.Empty;
                                }

                                Console.WriteLine("Bloco de dados recebido:");
                                foreach (string linha in bloco)
                                    Console.WriteLine(linha);

                                await ProcessaBlocoAsync(bloco, status);

                                writer.WriteLine("ACK");
                                Console.WriteLine("ACK enviado à WAVY.\n");
                            }
                            else
                            {
                                Console.WriteLine("Formato de header inválido.\n");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao processar bloco: " + ex.Message + "\n");
            }
        }

        private async Task ProcessaBlocoAsync(string[] bloco, string status)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            var queueName = "rpc_preprocessamento";
            var correlationId = Guid.NewGuid().ToString();
            var replyQueue = channel.QueueDeclare().QueueName;

            var props = channel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueue;

            var request = new { Bloco = bloco, Status = status };
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

            var tcs = new TaskCompletionSource<string?>();
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    var response = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());
                    tcs.TrySetResult(response);
                }
            };
            channel.BasicConsume(consumer: consumer, queue: replyQueue, autoAck: true);

            channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: props, body: messageBytes);

            string? response = null;
            try
            {
                response = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Timeout à espera da resposta do serviço de pré-processamento.");
                return;
            }

            if (string.IsNullOrEmpty(response))
            {
                Console.WriteLine("Resposta vazia do serviço de pré-processamento.");
                return;
            }

            PreProcessamentoResultado? resultado = null;
            try
            {
                resultado = JsonSerializer.Deserialize<PreProcessamentoResultado>(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao desserializar resposta do pré-processamento: {ex.Message}");
                return;
            }

            if (resultado == null)
            {
                Console.WriteLine("Resultado do pré-processamento é nulo.");
                return;
            }

            string linhaCSV = $"{resultado.WavyId}:{resultado.Status}:[{resultado.Tipos}]:{resultado.Timestamp}";
            string filePath = Path.Combine(dataFolder, agregadorFilePath ?? $"wavys_{id}.csv");

            wavysFileMutex.WaitOne();
            try
            {
                using StreamWriter writer = new StreamWriter(filePath, append: true);
                writer.WriteLine(linhaCSV);
                Console.WriteLine($"Estado da WAVY atualizado no arquivo '{filePath}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao escrever no arquivo CSV: " + ex.Message);
            }
            finally
            {
                wavysFileMutex.ReleaseMutex();
            }

            EncaminhaParaServidor(resultado.DadosSensor);
        }

        private void EncaminhaParaServidor(Dictionary<string, List<string>> dados)
        {
            try
            {
                foreach (var entry in dados)
                {
                    using TcpClient clienteServidor = new TcpClient(servidorIp, servidorPort);
                    NetworkStream stream = clienteServidor.GetStream();
                    using StreamReader reader = new StreamReader(stream);
                    using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

                    string tipoDado = entry.Key;
                    List<string> valores = entry.Value;

                    writer.WriteLine($"BLOCK {valores.Count} TYPE {tipoDado}");
                    foreach (string valor in valores)
                        writer.WriteLine(valor);

                    string? resposta = reader.ReadLine();
                    if (resposta == "ACK")
                        Console.WriteLine("ACK recebido do Servidor.\n");
                    else
                        Console.WriteLine("Resposta inesperada: " + resposta + "\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao encaminhar dados para o Servidor: " + ex.Message + "\n");
            }
        }

        private class PreProcessamentoResultado
        {
            public Dictionary<string, List<string>> DadosSensor { get; set; } = new();
            public string WavyId { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Timestamp { get; set; } = string.Empty;
            public string Tipos { get; set; } = string.Empty;
            public string[] Bloco { get; set; } = Array.Empty<string>();
        }
    }
}
