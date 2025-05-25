using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;
using System.Globalization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

class Agregador
{
    // Id to AGREGADOR
    private string id;

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
    
    public Agregador(string _id, int _port, string _servidorIp, int _servidorPort)
    {
        id = _id;
        port = _port;
        servidorIp = _servidorIp;
        servidorPort = _servidorPort;
    }

    public void Run()
    {
        // Verifica se a pasta "dados" existe
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
                Thread clientThread = new Thread(() => ProcessaWavy(client));
                clientThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao aceitar conexão: " + ex.Message + "\n");
            }
        }
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
            Console.WriteLine($"Arquivo '{agregadorFilePath}' criado na pasta '{dataFolder}'.");
        }
    }

    // Esta função processa os dados recebidos das WAVYs
    private void ProcessaWavy(TcpClient client)
    {
        try
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    // Lê a linha de header que indica o início do bloco e quantas linhas seguirão
                    string header = reader.ReadLine();
                    Console.WriteLine("Header recebido: " + header);

                    if (header != null && header.StartsWith("BLOCK"))
                    {
                        // Extrai o número de linhas a serem lidas
                        string[] partes = header.Split(' ');
                        if (partes.Length == 4 && int.TryParse(partes[1], out int numLinhas) && partes[2] == "STATUS")
                        {
                            // Variável que guarda o estado atual da WAVY
                            string status = partes[3];

                            // Variável que irá guardar o bloco de mensagens enviadas pela WAVY
                            string[] bloco = new string[numLinhas];
                            for (int i = 0; i < numLinhas; i++)
                                bloco[i] = reader.ReadLine();
                            
                            // Agora, 'bloco' contém todas as linhas enviadas pela WAVY.
                            // Debug : Faz print do bloco de dados recebido
                            Console.WriteLine("Bloco de dados recebido:");
                            foreach (string linha in bloco)
                                Console.WriteLine(linha);
                            
                            // Processa o bloco conforme necessário
                            ProcessaBloco(bloco, status);

                            // Envia ACK para confirmar o recebimento do bloco
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
    private void ProcessaBloco(string[] bloco, string status)
    {
        // Envia o bloco e o status para o servidor RPC de pré-processamento
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            var queueName = "rpc_preprocessamento";
            var correlationId = Guid.NewGuid().ToString();
            var replyQueue = channel.QueueDeclare().QueueName;

            var props = channel.CreateBasicProperties();
            props.CorrelationId = correlationId;
            props.ReplyTo = replyQueue;

            // Prepara o pedido em JSON
            var request = new
            {
                Bloco = bloco,
                Status = status
            };
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

            channel.BasicPublish(exchange: "",
                                 routingKey: queueName,
                                 basicProperties: props,
                                 body: messageBytes);

            // Espera pela resposta
            var consumer = new EventingBasicConsumer(channel);
            string response = null;
            consumer.Received += (model, ea) =>
            {
                if (ea.BasicProperties.CorrelationId == correlationId)
                {
                    response = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());
                }
            };
            channel.BasicConsume(consumer: consumer, queue: replyQueue, autoAck: true);

            // Espera até receber a resposta
            while (response == null)
            {
                System.Threading.Thread.Sleep(10);
            }

            // Deserializa o resultado devolvido pelo servidor RPC
            var resultado = JsonSerializer.Deserialize<PreProcessamentoResultado>(response);

            // Atualiza o ficheiro CSV com o estado e tipos de dados
            string linhaCSV = $"{resultado.WavyId}:{resultado.Status}:[{resultado.Tipos}]:{resultado.Timestamp}";
            string filePath = System.IO.Path.Combine(dataFolder, agregadorFilePath);

            wavysFileMutex.WaitOne();
            try
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filePath, append: true))
                {
                    writer.WriteLine(linhaCSV);
                }
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

            // Encaminha os dados processados para o servidor
            EncaminhaParaServidor(resultado.DadosSensor);
        }
    }

    // Estrutura para deserializar o resultado do RPC
    private class PreProcessamentoResultado
    {
        public Dictionary<string, List<string>> DadosSensor { get; set; }
        public string WavyId { get; set; }
        public string Status { get; set; }
        public string Timestamp { get; set; }
        public string Tipos { get; set; }
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
                            Console.WriteLine("ACK recebido do Servidor.\n");
                        else
                            Console.WriteLine("Resposta inesperada: " + resposta + "\n");
                    }
                }
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao encaminhar dados para o Servidor: " + ex.Message + "\n");
        }
    }
}
