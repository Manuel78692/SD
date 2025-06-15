using System.Text.Json;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ANALISERPC.Models;
using ANALISERPC.Services;

namespace ANALISERPC
{
    /// <summary>
    /// RPC Server for sensor data analysis
    /// Receives data from SERVIDOR and performs various types of analysis
    /// </summary>
    public class AnaliseRPCServer
    {
        private const string RPC_QUEUE_NAME = "rpc_analise";

        public static void Start()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            
            try
            {
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                // Declare the RPC queue
                channel.QueueDeclare(queue: RPC_QUEUE_NAME,
                                   durable: false,
                                   exclusive: false,
                                   autoDelete: false,
                                   arguments: null);

                // Set QoS to process one message at a time
                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                
                Console.WriteLine($"[x] Servidor RPC de análise iniciado na fila '{RPC_QUEUE_NAME}'");
                Console.WriteLine("[x] Aguardando pedidos de análise...");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    string response = string.Empty;
                    var props = ea.BasicProperties;
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;

                    try
                    {
                        var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                        Console.WriteLine($"[.] Recebido pedido de análise (ID: {props.CorrelationId})");
                        
                        var request = JsonSerializer.Deserialize<AnaliseRequest>(message);
                        if (request == null)
                        {
                            throw new ArgumentException("Pedido de análise inválido");
                        }

                        Console.WriteLine($"[.] Processando análise: Tipo={request.TipoSensor}, Análise={request.TipoAnalise}, Dados={request.Dados.Count}");
                        
                        var resultado = AnalysisService.ProcessarAnalise(request);
                        response = JsonSerializer.Serialize(resultado);
                        
                        Console.WriteLine($"[.] Análise concluída: {resultado.Mensagem}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[!] Erro ao processar análise: {e.Message}");
                        
                        var errorResponse = new AnaliseResponse
                        {
                            Sucesso = false,
                            Mensagem = $"Erro no servidor de análise: {e.Message}"
                        };
                        response = JsonSerializer.Serialize(errorResponse);
                    }
                    finally
                    {
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        
                        channel.BasicPublish(exchange: "",
                                           routingKey: props.ReplyTo,
                                           basicProperties: replyProps,
                                           body: responseBytes);
                        
                        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    }
                };

                channel.BasicConsume(queue: RPC_QUEUE_NAME,
                                   autoAck: false,
                                   consumer: consumer);

                Console.WriteLine("[x] Servidor RPC de análise pronto. Pressione CTRL+C para sair.");
                
                // Keep the server running
                var cancellationTokenSource = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) =>
                {
                    e.Cancel = true;
                    cancellationTokenSource.Cancel();
                    Console.WriteLine("\n[x] Encerrando servidor RPC de análise...");
                };

                try
                {
                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Erro fatal no servidor RPC de análise: {ex.Message}");
                throw;
            }
        }
    }
}
