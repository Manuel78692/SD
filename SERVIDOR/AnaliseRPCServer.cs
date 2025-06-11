using System;
using System.Text.Json;
using System.Text;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class AnaliseRPCServer
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

    public static void Start()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "rpc_analise",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);
        channel.BasicQos(0, 1, false);
        Console.WriteLine("[x] Aguardando pedidos RPC de análise...");
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
                var request = JsonSerializer.Deserialize<AnaliseRequest>(message);
                var resultado = ProcessarAnalise(request);
                response = JsonSerializer.Serialize(resultado);
            }
            catch (Exception e)
            {
                Console.WriteLine("[.] " + e.Message);
                response = string.Empty;
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
        channel.BasicConsume(queue: "rpc_analise",
                             autoAck: false,
                             consumer: consumer);
        Console.WriteLine("[x] Servidor de análise RPC pronto.");
        Console.ReadLine();
    }

    private static AnaliseResponse ProcessarAnalise(AnaliseRequest? req)
    {
        var response = new AnaliseResponse();
        if (req == null || req.Dados == null || req.Dados.Count == 0)
            return response;
        var valores = new List<double>();
        foreach (var dado in req.Dados)
        {
            if (double.TryParse(dado.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                valores.Add(v);
        }
        if (valores.Count == 0) return response;
        response.Media = valores.Count > 0 ? Math.Round(valores.Average(), 3) : 0;
        response.Max = valores.Count > 0 ? valores.Max() : 0;
        response.Min = valores.Count > 0 ? valores.Min() : 0;
        return response;
    }
}
