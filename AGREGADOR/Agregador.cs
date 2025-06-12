using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;
using System.Globalization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;

public class Agregador
{
    // Id to AGREGADOR
    private string id { get; set; }

    // Porta do AGREGADOR para escutar as conexões das WAVYs
    private readonly int port;

    // Ip do SERVIDOR
    private readonly string servidorIp;

    // Porta do SERVIDOR
    private readonly int servidorPort;

    // Pasta onde irá guardar os dados
    private readonly string dataFolder = "dados";

    // Ficheiro CSV onde irá guardar os dados
    private string? agregadorFilePath;

    // Mutex para garantir a exclusão mútua ao escrever no arquivo CSV
    private readonly Mutex wavysFileMutex = new Mutex();

    // RabbitMQ
    private IConnection? _rabbitConnection;
    private IModel? _rabbitChannel;
    private CancellationTokenSource _agregadorCts = new CancellationTokenSource();
    private bool _isFailing = false;

    public event Action<string>? OnLogEntry;
    public Agregador(string _id, int _port, string _servidorIp, int _servidorPort)
    {
        id = _id;
        port = _port;
        servidorIp = _servidorIp;
        servidorPort = _servidorPort;
    }

    public string GetId()
    {
        return id;
    }
    private void Log(string message)
    {

        OnLogEntry?.Invoke(message);
    }

    public void Run()
    {
        if (!Directory.Exists(dataFolder))
        {
            // This is a critical startup error, might be okay to leave as Console.WriteLine or throw
            Console.WriteLine($"Agregador {id}: ERRO CRÍTICO: Pasta '{dataFolder}/' não existe. Desligando.\n");
            Log($"ERRO CRÍTICO: Pasta '{dataFolder}/' não existe. Desligando."); // Also log it
            return;
        }
        InitializeCSV(); // Initializes agregador's own status CSV

        try
        {
            var factory = new ConnectionFactory() { HostName = RabbitMqConstants.HostName, DispatchConsumersAsync = true };
            _rabbitConnection = factory.CreateConnection();
            _rabbitChannel = _rabbitConnection.CreateModel();

            Log($"Agregador {id} conectado ao RabbitMQ.");

            // Declare Topic Exchange (WAVYs publish here)
            _rabbitChannel.ExchangeDeclare(exchange: RabbitMqConstants.WavyTopicExchange, type: ExchangeType.Topic, durable: true);

            // Declare Dead Letter Exchange (for fallback)
            _rabbitChannel.ExchangeDeclare(exchange: RabbitMqConstants.AgregadorFallebackDlx, type: ExchangeType.Fanout, durable: true); // Fanout is simple for DLX

            // --- Preferred Queue Setup ---
            string preferredQueueName = $"{id}_preferred_q";
            var preferredQueueArgs = new Dictionary<string, object>
            {
                {"x-dead-letter-exchange", RabbitMqConstants.AgregadorFallebackDlx},
                // {"x-dead-letter-routing-key", RabbitMqConstants.FallbackRoutingKey}, // Not needed if DLX is fanout
                {"x-message-ttl", RabbitMqConstants.MessageTtlMilliseconds}
            };
            _rabbitChannel.QueueDeclare(queue: preferredQueueName, durable: true, exclusive: false, autoDelete: false, arguments: preferredQueueArgs);
            string preferredRoutingKey = $"wavy.data.prefer.{id}";
            _rabbitChannel.QueueBind(queue: preferredQueueName, exchange: RabbitMqConstants.WavyTopicExchange, routingKey: preferredRoutingKey);

            Log($"Agregador {id}: Preferred queue '{preferredQueueName}' declarada e ligada ao exchange '{RabbitMqConstants.WavyTopicExchange}' com RK '{preferredRoutingKey}'. TTL: {RabbitMqConstants.MessageTtlMilliseconds}ms.");

            // Use AsyncEventingBasicConsumer
            var preferredConsumer = new AsyncEventingBasicConsumer(_rabbitChannel); // <--- CORRECTED
            preferredConsumer.Received += async (sender, ea) => // Changed 'model' to 'sender' for typical event handler signature, though 'model' also works as it's the channel
            {
                if (_isFailing) return; // Don't process if failing
                // The 'sender' here will be the IModel (channel)
                await HandleReceivedMessage(ea, $"PreferredQueue ({preferredQueueName})");
            };
            _rabbitChannel.BasicConsume(queue: preferredQueueName, autoAck: false, consumer: preferredConsumer);

            // --- Fallback Queue Setup ---
            _rabbitChannel.QueueDeclare(queue: RabbitMqConstants.AgregadorGeneralFallbackQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _rabbitChannel.QueueBind(queue: RabbitMqConstants.AgregadorGeneralFallbackQueue, exchange: RabbitMqConstants.AgregadorFallebackDlx, routingKey: "");

            Log($"Agregador {id}: Fallback queue '{RabbitMqConstants.AgregadorGeneralFallbackQueue}' declarada e ligada ao DLX '{RabbitMqConstants.AgregadorFallebackDlx}'.");

            // Use AsyncEventingBasicConsumer
            var fallbackConsumer = new AsyncEventingBasicConsumer(_rabbitChannel); // <--- CORRECTED
            fallbackConsumer.Received += async (sender, ea) => // Changed 'model' to 'sender'
            {
                await HandleReceivedMessage(ea, $"FallbackQueue ({RabbitMqConstants.AgregadorGeneralFallbackQueue})");
            };
            _rabbitChannel.BasicConsume(queue: RabbitMqConstants.AgregadorGeneralFallbackQueue, autoAck: false, consumer: fallbackConsumer);

            Log($"Agregador {id} aguardando mensagens nas queues '{preferredQueueName}' e '{RabbitMqConstants.AgregadorGeneralFallbackQueue}'. Pressione [enter] para sair.");
            // Keep alive for listening, but check for cancellation token
            while (!_agregadorCts.Token.IsCancellationRequested)
            {
                // You can use Task.Delay for a non-blocking wait or keep Console.ReadLine() if user interaction on Agregador console is desired.
                // For simulation via AgregadorMain, a delay is better.
                try
                {
                    Task.Delay(1000, _agregadorCts.Token); // Check every second
                }
                catch (TaskCanceledException)
                {
                    // Expected when _agregadorCts.Cancel() is called
                    break;
                }
            }
            Log($"Agregador {id} está parando...");
        }
        catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
        {
            Log($"Agregador {id}: Não foi possível conectar ao RabbitMQ: {ex.Message}. Verifique se o RabbitMQ está em execução.");
        }
        catch (Exception ex)
        {
            if (!_isFailing) // Don't print scary errors if we initiated the failure
            {
                Log($"Agregador {id}: Erro fatal no setup do RabbitMQ ou ao executar: {ex.Message}\n{ex.StackTrace}");
            }
        }
        finally
        {
            Log($"Agregador {id}: Limpando recursos...");
            try
            {
                _rabbitChannel?.Close();
                _rabbitChannel?.Dispose();
            }
            catch (Exception ex) { Log($"Agregador {id}: Erro ao fechar canal: {ex.Message}"); }
            try
            {
                _rabbitConnection?.Close();
                _rabbitConnection?.Dispose();
            }
            catch (Exception ex) { Log($"Agregador {id}: Erro ao fechar conexão: {ex.Message}"); }
            Log($"Agregador {id}: Recursos limpos. Desligado.");
        }
    }

    public void SimulateFailure()
    {
        Log($"\n!!! AGREGADOR {id}: SIMULANDO FALHA !!!");
        _isFailing = true; // Set flag to prevent processing new messages if any arrive just before shutdown

        // Signal the Run method to stop its loop and clean up
        _agregadorCts.Cancel();

        // Note: The 'finally' block in Run() will now handle closing the channel and connection
        // as the _agregadorCts.Cancel() will cause the loop in Run() to exit.
        // Forcing close here can sometimes race with the finally block.
        // However, if Run() was stuck for some other reason, an explicit close might be desired,
        // but it's generally better to let the graceful shutdown initiated by CancellationToken do its job.
        Log($"AGREGADOR {id}: Solicitação de cancelamento enviada. Deve desligar em breve.");
    }

    // Esta função cria o ficheiro CSV do AGREGADOR, se não existir
    private void InitializeCSV()
    {
        // Verifica se o ficheiro "wavys_{id}.csv" existe na pasta "dados"
        agregadorFilePath = $"wavys_{id}.csv";
        string filePath = Path.Combine(dataFolder, agregadorFilePath);
        if (!File.Exists(filePath))
        {
            // Cria o arquivo "wavys_{id}.csv" com um cabeçalho inicial
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("WAVY_ID:status:[data_types]:last_sync");
            }
            Log($"Arquivo '{agregadorFilePath}' criado na pasta '{dataFolder}'.");
        }
    }

    private async Task HandleReceivedMessage(BasicDeliverEventArgs ea, string queueSource)
    {
        if (_isFailing)
        {
            Log($"[{id} - {queueSource}] Ignorando mensagem (DeliveryTag: {ea.DeliveryTag}) porque o agregador está em processo de falha.");
            // Optionally NACK and requeue if the failure is temporary and another instance should get it.
            // For a hard stop simulation, you might not even NACK, letting RabbitMQ requeue by timeout or if channel closes.
            // For this simulation, let's assume the channel will close, leading to redelivery.
            return;
        }
        var processingId = Guid.NewGuid().ToString("N").Substring(0, 6); // Short ID for this processing attempt
        Log($"\n[{id} - {processingId} - {queueSource}] Mensagem recebida. DeliveryTag: {ea.DeliveryTag}");
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            // The message contains the header line, then data lines.
            // We need to parse it similarly to how ProcessaWavy did with StreamReader.
            using (StringReader messageReader = new StringReader(message))
            {
                string header = messageReader.ReadLine();
                Log($"[{id} - {processingId}] Header: {header}");

                if (header != null && header.StartsWith("BLOCK"))
                {
                    string[] partesHeader = header.Split(' ');
                    if (partesHeader.Length == 4 && int.TryParse(partesHeader[1], out int numLinhas) && partesHeader[2] == "STATUS")
                    {
                        string statusWavy = partesHeader[3];
                        List<string> blocoList = new List<string>();
                        for (int i = 0; i < numLinhas; i++)
                        {
                            string? line = messageReader.ReadLine();
                            if (line != null)
                            {
                                blocoList.Add(line);
                            }
                            else
                            {
                                Log($"[{id} - {processingId}] Erro: Fim inesperado da mensagem, esperava {numLinhas} linhas no bloco mas so recebeu {i}.");
                                // Decide how to handle: nack and don't requeue?
                                _rabbitChannel?.BasicNack(ea.DeliveryTag, false, false);
                                return;
                            }
                        }
                        string[] bloco = blocoList.ToArray();

                        Log($"[{id} - {processingId}] Bloco de dados ({bloco.Length} linhas) extraído para processamento.");
                        // Debug: Print block
                        // foreach (string linha in bloco)
                        //     Log($"[{id} - {processingId}]   {linha}");

                        await ProcessaBlocoAsync(bloco, statusWavy); // Your existing core logic

                        _rabbitChannel?.BasicAck(ea.DeliveryTag, false); // Acknowledge successful processing
                        Log($"[{id} - {processingId}] Mensagem processada e ACK enviada. DeliveryTag: {ea.DeliveryTag}");
                    }
                    else
                    {
                        Log($"[{id} - {processingId}] Formato de header inválido na mensagem: {header}. NACK (não reencaminhar).");
                        _rabbitChannel?.BasicNack(ea.DeliveryTag, false, false); // Bad message, don't requeue
                    }
                }
                else
                {
                    Log($"[{id} - {processingId}] Mensagem não começa com 'BLOCK': {header?.Substring(0, Math.Min(header?.Length ?? 0, 20))}. NACK (não reencaminhar).");
                    _rabbitChannel?.BasicNack(ea.DeliveryTag, false, false); // Bad message, don't requeue
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[{id} - {processingId}] Erro ao processar mensagem (DeliveryTag: {ea.DeliveryTag}): {ex.Message}\n{ex.StackTrace}");
            // Decide on requeue strategy. For this example, NACK and don't requeue to avoid poison messages.
            // You might implement a retry mechanism with a separate delay/retry queue.
            _rabbitChannel?.BasicNack(ea.DeliveryTag, false, false);
        }
    }

    // Esta função processa os dados recebidos das WAVYs
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
            Log("Timeout à espera da resposta do serviço de pré-processamento.");
            return;
        }

        if (string.IsNullOrEmpty(response))
        {
            Log("Resposta vazia do serviço de pré-processamento.");
            return;
        }

        PreProcessamentoResultado? resultado = null;
        try
        {
            resultado = JsonSerializer.Deserialize<PreProcessamentoResultado>(response);
        }
        catch (Exception ex)
        {
            Log($"Erro ao desserializar resposta do pré-processamento: {ex.Message}");
            return;
        }

        if (resultado == null)
        {
            Log("Resultado do pré-processamento é nulo.");
            return;
        }

        // // ADICIONA AQUI O DEBUG:
        // Console.WriteLine("DEBUG - Quantidade de tipos em DadosSensor: " + resultado.DadosSensor.Count);
        // foreach (var tipo in resultado.DadosSensor.Keys)
        // {
        //     Console.WriteLine($"DEBUG - Tipo: {tipo}, Leituras: {resultado.DadosSensor[tipo].Count}");
        // }

        // Console.WriteLine("DEBUG - Resposta do RPC:");
        // Console.WriteLine(response);

        string linhaCSV = $"{resultado.WavyId}:{resultado.Status}:[{resultado.Tipos}]:{resultado.Timestamp}";
        string filePath = Path.Combine(dataFolder, agregadorFilePath ?? $"wavys_{id}.csv");

        wavysFileMutex.WaitOne();
        try
        {
            using StreamWriter writer = new StreamWriter(filePath, append: true);
            foreach (var tipo in resultado.DadosSensor.Keys)
            {
                foreach (var leitura in resultado.DadosSensor[tipo])
                {
                    writer.WriteLine(leitura); // Exemplo: WAVY01:123:2024-06-11-12-00-00
                }
            }
            Log($"Dados das WAVYs adicionados ao arquivo '{filePath}'.");
        }
        catch (Exception ex)
        {
            Log("Erro ao escrever no arquivo CSV: " + ex.Message);
        }
        finally
        {
            wavysFileMutex.ReleaseMutex();
        }

        EncaminhaParaServidor(resultado.DadosSensor);
    }

    // Esta função encaminha cada bloco de dados, separados por tipo de dados, para o servidor
    private void EncaminhaParaServidor(Dictionary<string, List<string>> dados)
    {
        // No servidor, existem ficheiros .csv para cada tipo de dados
        // O que o AGREGADOR deverá fazer é enviar os dados separados por tipo de dados
        /* 
            Formato de dados enviados para o servidor:
            BLOCK size_of_data TYPE data_type
            WAVY_ID:data:date_of_reading
            (...)
        */
        try
        {
            foreach (var entry in dados)
            {
                using (TcpClient clienteServidor = new TcpClient(servidorIp, servidorPort))
                {
                    NetworkStream stream = clienteServidor.GetStream();
                    using (StreamReader reader = new StreamReader(stream))
                    using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                    {
                        // Variável que guarda o tipo de dado atual
                        string tipoDado = entry.Key;

                        // Variável que guarda os valores dos dados associados ao tipo
                        List<string> valores = entry.Value;

                        // Envia o header com o tipo de dado
                        writer.WriteLine("BLOCK " + valores.Count + " TYPE " + tipoDado);

                        // Envia os dados
                        foreach (string valor in valores)
                            writer.WriteLine(valor);

                        // Aguarda ACK do Servidor
                        string resposta = reader.ReadLine();
                        if (resposta == "ACK")
                            Log("ACK recebido do Servidor.\n");
                        else
                            Log("Resposta inesperada: " + resposta + "\n");
                    }
                }
            }

        }
        catch (Exception ex)
        {
            Log("Erro ao encaminhar dados para o Servidor: " + ex.Message + "\n");
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