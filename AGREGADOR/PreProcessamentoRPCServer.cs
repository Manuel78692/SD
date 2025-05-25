using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace AGREGADOR
{
    public class PreProcessamentoRPCServer
    {
        public static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "rpc_preprocessamento",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                channel.BasicQos(0, 1, false);

                Console.WriteLine(" [x] Aguardando pedidos RPC de pré-processamento...");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    string response = string.Empty;

                    var body = ea.Body.ToArray();
                    var props = ea.BasicProperties;
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;

                    try
                    {
                        // Espera-se receber um JSON com bloco e status
                        var message = Encoding.UTF8.GetString(body);
                        var request = JsonSerializer.Deserialize<PreProcessamentoRequest>(message);
                        var resultado = ProcessaBloco(request.Bloco, request.Status);
                        response = JsonSerializer.Serialize(resultado);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(" [.] " + e.Message);
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

                channel.BasicConsume(queue: "rpc_preprocessamento",
                                     autoAck: false,
                                     consumer: consumer);

                Console.WriteLine(" Pressione [enter] para sair.");
                Console.ReadLine();
            }
        }

        // Estrutura para receber o pedido
        public class PreProcessamentoRequest
        {
            public string[] Bloco { get; set; }
            public string Status { get; set; }
        }

        // Estrutura para devolver o resultado (podes adaptar conforme necessário)
        public class PreProcessamentoResultado
        {
            public Dictionary<string, List<string>> DadosSensor { get; set; }
            public string WavyId { get; set; }
            public string Status { get; set; }
            public string Timestamp { get; set; }
            public string Tipos { get; set; }
        }

        // Lógica de processamento (adaptada do ProcessaBloco)
        public static PreProcessamentoResultado ProcessaBloco(string[] bloco, string status)
        {
            Dictionary<string, List<string>> dadosSensor = new Dictionary<string, List<string>>();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
            string wavyId = string.Empty;

            foreach (string linha in bloco)
            {
                string[] partes = linha.Split('[', ']');
                string dataLeitura = partes[2].Trim(':');
                wavyId = partes[0].TrimEnd(':');
                string[] dados = partes[1].TrimEnd(']').Split(':');

                foreach (string dado in dados)
                {
                    string[] tipoDado = dado.Split('=');
                    string dataType = tipoDado[0].Trim();
                    string data = tipoDado[1].Trim();

                    if (!dadosSensor.ContainsKey(dataType))
                        dadosSensor[dataType] = new List<string>();

                    dadosSensor[dataType].Add(wavyId + ":" + data + ":" + dataLeitura);
                }
            }

            string tipos = string.Join(":", dadosSensor.Keys);

            return new PreProcessamentoResultado
            {
                DadosSensor = dadosSensor,
                WavyId = wavyId,
                Status = status,
                Timestamp = timestamp,
                Tipos = tipos
            };
        }
    }
}
