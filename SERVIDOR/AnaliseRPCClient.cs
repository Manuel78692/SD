using System;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SERVIDOR
{
    public class AnaliseRPCClient
    {
        public class AnaliseRequest
        {
            public string Tipo { get; set; } = string.Empty;
            public string DataInicio { get; set; } = string.Empty;
            public string DataFim { get; set; } = string.Empty;
            public List<string> Dados { get; set; } = new();
        }
        public class AnaliseResponse
        {
            public double Media { get; set; }
            public double Max { get; set; }
            public double Min { get; set; }
        }

        public static AnaliseResponse? Analisar(string tipo, string dataInicio, string dataFim, List<string> dados)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            var queueName = "rpc_analise";
            var correlationId = Guid.NewGuid().ToString();
            var replyQueue = channel.QueueDeclare().QueueName;

            var props = channel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueue;

            var request = new AnaliseRequest { Tipo = tipo, DataInicio = dataInicio, DataFim = dataFim, Dados = dados };
            var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

            var tcs = new System.Threading.Tasks.TaskCompletionSource<string?>();
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
            channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: props, body: messageBytes);

            string? response = null;
            try
            {
                response = tcs.Task.Wait(10000) ? tcs.Task.Result : null;
            }
            catch { }
            if (string.IsNullOrEmpty(response)) return null;
            try
            {
                return JsonSerializer.Deserialize<AnaliseResponse>(response);
            }
            catch { return null; }
        }
    }
}
