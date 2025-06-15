using System.Text.Json;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ANALISERPC.Models;

namespace SERVIDOR.Services
{
    /// <summary>
    /// Client for communicating with the ANALISERPC service
    /// </summary>
    public class AnaliseRPCClient
    {
        private const string RPC_QUEUE_NAME = "rpc_analise";
        private const int TIMEOUT_MS = 30000; // 30 seconds timeout

        /// <summary>
        /// Sends analysis request to ANALISERPC service
        /// </summary>
        public static async Task<AnaliseResponse?> SolicitarAnaliseAsync(AnaliseRequest request)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            
            try
            {
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                var correlationId = Guid.NewGuid().ToString();
                var replyQueue = channel.QueueDeclare().QueueName;

                var props = channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = replyQueue;

                var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

                var tcs = new TaskCompletionSource<string?>();
                var consumer = new EventingBasicConsumer(channel);
                
                consumer.Received += (model, ea) =>
                {
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        var response = Encoding.UTF8.GetString(ea.Body.ToArray());
                        tcs.TrySetResult(response);
                    }
                };

                channel.BasicConsume(consumer: consumer, queue: replyQueue, autoAck: true);
                channel.BasicPublish(exchange: "", routingKey: RPC_QUEUE_NAME, basicProperties: props, body: messageBytes);

                // Wait for response with timeout
                using var cts = new CancellationTokenSource(TIMEOUT_MS);
                var timeoutTask = Task.Delay(TIMEOUT_MS, cts.Token);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("[!] Timeout aguardando resposta do servidor de análise");
                    return null;
                }

                cts.Cancel(); // Cancel the timeout task
                var responseJson = await tcs.Task;
                
                if (string.IsNullOrEmpty(responseJson))
                    return null;

                return JsonSerializer.Deserialize<AnaliseResponse>(responseJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Erro na comunicação com servidor de análise: {ex.Message}");
                return null;
            }
        }
    }
}
