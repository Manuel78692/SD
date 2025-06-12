using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading.Tasks;

namespace AGREGADOR
{
    public class PreProcessamentoRPCServer
    {
        public static void Start()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

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
                var props = ea.BasicProperties;
                var replyProps = channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;

                try
                {
                    Console.WriteLine(" [>] Pedido RPC recebido para pré-processamento.");
                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var request = JsonSerializer.Deserialize<PreProcessamentoRequest>(message);
                    var resultado = ProcessaBloco(request?.Bloco ?? Array.Empty<string>(), request?.Status ?? "");
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

        public class PreProcessamentoRequest
        {
            public string[]? Bloco { get; set; }
            public string? Status { get; set; }
        }

        public class PreProcessamentoResultado
        {
            public Dictionary<string, List<string>> DadosSensor { get; set; } = new();
            public string WavyId { get; set; } = "";
            public string Status { get; set; } = "";
            public string Timestamp { get; set; } = "";
            public string Tipos { get; set; } = "";
        }

        public static PreProcessamentoResultado ProcessaBloco(string[] bloco, string status)
        {
            var dadosSensor = new Dictionary<string, List<string>>();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
            string wavyId = string.Empty;

            foreach (string linha in bloco)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(linha))
                        continue;

                    var partes = linha.Split('[', ']');
                    if (partes.Length < 3)
                    {
                        Console.WriteLine($"[Aviso] Linha malformada ignorada: '{linha}'");
                        continue;
                    }

                    wavyId = partes[0].TrimEnd(':');
                    var dadosStr = partes[1].TrimEnd(']');
                    var dataLeitura = partes[2].Trim(':');

                    if (string.IsNullOrWhiteSpace(wavyId) || string.IsNullOrWhiteSpace(dadosStr) || string.IsNullOrWhiteSpace(dataLeitura))
                    {
                        Console.WriteLine($"[Aviso] Campos obrigatórios em falta na linha: '{linha}'");
                        continue;
                    }

                    var dados = dadosStr.Split(':');
                    foreach (var dado in dados)
                    {
                        var tipoDado = dado.Split('=');
                        if (tipoDado.Length != 2)
                        {
                            Console.WriteLine($"[Aviso] Dado malformado ignorado: '{dado}' na linha '{linha}'");
                            continue;
                        }
                        var dataType = tipoDado[0].Trim();
                        var data = tipoDado[1].Trim();
                        if (string.IsNullOrWhiteSpace(dataType) || string.IsNullOrWhiteSpace(data))
                        {
                            Console.WriteLine($"[Aviso] Tipo ou valor vazio ignorado: '{dado}' na linha '{linha}'");
                            continue;
                        }
                        if (!dadosSensor.ContainsKey(dataType))
                            dadosSensor[dataType] = new List<string>();
                        dadosSensor[dataType].Add($"{wavyId}:{data}:{dataLeitura}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Erro] Exceção ao processar linha '{linha}': {ex.Message}");
                    continue;
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