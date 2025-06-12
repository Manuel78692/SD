using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Text;
using RabbitMQ.Client;

// Os estados que a WAVY pode ter
public enum Estado
{
    Ativo,
    Desativo
}
// Os tipos de dados que a WAVY pode ter
public enum TipoDado
{
    GPS,
    Gyro,
    Humidade,
    PH,
    Temperatura
}
public class Wavy
{
    // Id da WAVY
    public string id;

    // Preferred AGREGADOR ID
    private string preferredAgregatorId; // New

    // Lista de sensores que o WAVY tem
    private List<TipoDado> tipoDados = new List<TipoDado>();

    // Estado da WAVY
    public Estado estadoWavy = Estado.Ativo;

    // Buffer de dados a enviar para o AGREGADOR
    private List<string> bufferDados;

    // Tamanho máximo do buffer
    private const int MaxBufferSize = 5;

    // Para mandar os logs ao WavyMain
    // Invés Console.Log, usa-se Log
    public event Action<string>? OnDataBlockReady;

    // RabbitMQ connection objects - could be shared or per send
    private IConnection? _rabbitConnection; // New
    private IModel? _rabbitChannel; // New    // Construtor da WAVY
    public Wavy(string _id, string _preferredAgregatorId, List<TipoDado> _tipoDados)
    {
        id = _id;
        preferredAgregatorId = _preferredAgregatorId; // Changed
        tipoDados = _tipoDados;
        bufferDados = new List<string>();
        Console.WriteLine($"Wavy {_id} criada com tipos de dados: {string.Join(", ", _tipoDados)} -> Agregador preferido: {_preferredAgregatorId}");
        // InitializeRabbitMq(); // Consider initializing connection here or on first send
    }

    public void Log(string msg) { OnDataBlockReady?.Invoke(msg); }    private void EnsureRabbitMqConnection()
    {
        Log($"[{id}] EnsureRabbitMqConnection() - Verificando conexão...");
        if (_rabbitChannel == null || _rabbitChannel.IsClosed)
        {
            Log($"[{id}] Canal RabbitMQ não disponível ou fechado. Criando nova conexão...");
            try
            {
                _rabbitConnection?.Close(); // Close previous if any
                Log($"[{id}] Criando ConnectionFactory para {RabbitMqConstants.HostName}...");
                var factory = new ConnectionFactory() 
                { 
                    HostName = RabbitMqConstants.HostName, 
                    DispatchConsumersAsync = true,
                    RequestedConnectionTimeout = 5000, // 5 seconds in milliseconds
                    RequestedHeartbeat = 10 // 10 seconds
                };
                
                Log($"[{id}] Estabelecendo conexão...");
                _rabbitConnection = factory.CreateConnection();
                
                Log($"[{id}] Criando canal...");
                _rabbitChannel = _rabbitConnection.CreateModel();

                Log($"[{id}] Declarando exchange '{RabbitMqConstants.WavyTopicExchange}'...");
                // Declare the topic exchange - idempotent
                _rabbitChannel.ExchangeDeclare(exchange: RabbitMqConstants.WavyTopicExchange, type: ExchangeType.Topic, durable: true);
                Log($"[{id}] ✓ RabbitMQ connection and topic exchange '{RabbitMqConstants.WavyTopicExchange}' ensured successfully.");
            }
            catch (Exception ex)
            {
                Log($"[{id}] ✗ ERRO ao inicializar RabbitMQ: {ex.Message}");
                Log($"[{id}] Stack trace: {ex.StackTrace}");
                _rabbitChannel = null; // Prevent further use if setup failed
            }
        }
        else
        {
            Log($"[{id}] Conexão RabbitMQ já está ativa.");
        }
    }//Esta função recebe dados dos sensores.
    //Ou seja, utiliza os simuladores de sensores (na pasta geradores) para simular leitura de dados em tempo real.
    public async Task ReceberDados(CancellationToken token = default)
    {
        Log($"{id} - ReceberDados() iniciado com {tipoDados.Count} tipos de dados.");
        Random random = new Random();
        // Cria uma lista de enumeradores, uma por tipo de sensor
        var enumeradores = new List<(TipoDado Tipo, IAsyncEnumerator<string> Enumerador)>();

        foreach (TipoDado tipo in tipoDados)
        {
            // Verifica no SimuladorFactory a função associada ao tipo de dado
            // A função associada é o simulador do respetivo sensor
            if (SimuladorFactory.Simuladores.TryGetValue(tipo, out var simulatorFunc))
            {
                // Guarda a função no enumerador
                IAsyncEnumerator<string> enumerador = simulatorFunc(this).GetAsyncEnumerator();
                enumeradores.Add((tipo, enumerador));
                Log($"{id} - Simulador para {tipo} configurado.");
            }
            else
            {
                Log($"Nenhum simulador encontrado para o tipo: {tipo}");
            }
        }        Log($"{id} - Iniciando loop de simulação com {enumeradores.Count} enumeradores...");
        int iterationCount = 0;
        // Inicia o loop da simulação dos sensores
        while (!token.IsCancellationRequested)
        {
            iterationCount++;
            Log($"{id} - Loop iteração #{iterationCount}, Estado: {estadoWavy}");
            
            if (estadoWavy != Estado.Ativo)
            {
                Log($"{id} não ativada - aguardando...");
                await Task.Delay(500, token);
                continue;
            }
            
            Log($"{id} - Processando dados dos sensores...");
            // Lista que contém todos os valores do enumeradores
            var valoresSensor = new List<string>();

            // Variável para guardar a data na qual o sensor foi lido
            string last_sync = string.Empty;

            foreach (var (Tipo, Enumerador) in enumeradores)
            {
                Log($"{id} - Obtendo dados do sensor {Tipo}...");
                bool hasNext = await Enumerador.MoveNextAsync();
                if (!hasNext)
                {
                    Log($"O simulador para {Tipo} terminou.");
                    return;
                }

                string dadosRecebidos = Enumerador.Current;
                Log($"{id} - Dados recebidos de {Tipo}: {dadosRecebidos}");
                string[] partes = dadosRecebidos.Split(':');

                // Verifica se existe ou não data na qual o sensor foi lido. Se não existir, fica "N/A"
                if (partes.Length > 1)
                    last_sync = partes[1];
                else
                    last_sync = "N/A";

                valoresSensor.Add(partes[0]);
            }            // Combina todos os dados recebidos dos possíveis diferentes tipos de sensores e combina-os numa linha da lista
            string compositeOutput = "[" + string.Join(":", valoresSensor) + "]" + ":" + last_sync;
            Log($"{id} - Dados compostos gerados: {compositeOutput}");

            // Adiciona a lista ao bufferDados
            bufferDados.Add(compositeOutput);
            Log($"{id} - Dados adicionados ao buffer. Tamanho atual: {bufferDados.Count}/{MaxBufferSize}");

            // Se a lista ultrapassar o tamanho máximo, chama a função GerirLista
            if (bufferDados.Count >= MaxBufferSize)
            {
                Log($"{id} - Buffer cheio ({bufferDados.Count}/{MaxBufferSize}), enviando dados...");
                GerirLista();
            }

            // Gera um atraso aleatório, para simular a leitura dos sensores
            // Em .Next, o primeiro parâmetro é inclusivo e o segundo é exclusivo.
            // int delay = random.Next(100, 201);
            await Task.Delay(500, token);
        }
    }    // Esta função chama a função EnviarBloco e limpa o bufferDados
    private void GerirLista()
    {
        Log($"{id} - GerirLista chamada. Buffer tem {bufferDados.Count} itens.");
        // Verifica se o buffer atingiu o tamanho máximo
        if (bufferDados.Count >= MaxBufferSize)
        {
            Log($"{id} - Buffer cheio! Enviando bloco...");
            // Envia o bloco de dados para o AGREGADOR
            EnviarBloco();

            // Debug : Faz print da lista
            Log(id + " List :");
            foreach (string element in bufferDados)
            {
                Log("| List - " + element);
            }

            // Limpa o buffer após enviar
            lock (bufferDados)
            {
                bufferDados.Clear();
            }
            Log($"{id} - Buffer limpo após envio.");
        }
    }    // Esta função envia o bloco de dados (bufferDados) para o AGREGADOR associado
    private void EnviarBloco()
    {
        Log($"[{id}] EnviarBloco() iniciado. Tentando estabelecer conexão RabbitMQ...");
        EnsureRabbitMqConnection();
        if (_rabbitChannel == null)
        {
            Log($"[{id}] ERRO: Cannot send block - RabbitMQ channel not available.");
            // Optionally, re-buffer or handle this error
            return;
        }

        Log($"[{id}] Conexão RabbitMQ estabelecida. Preparando mensagem...");

        try
        {
            // Construct the message payload
            // The payload includes the original header and all data lines
            StringBuilder messageBuilder = new StringBuilder();
            string header = $"BLOCK {bufferDados.Count} STATUS {estadoWavy.ToString()}";
            messageBuilder.AppendLine(header);

            foreach (string linha in bufferDados)
            {
                messageBuilder.AppendLine($"{id}:{linha}");
            }

            string messageBody = messageBuilder.ToString();
            var bodyBytes = Encoding.UTF8.GetBytes(messageBody);

            // Define the routing key for the preferred agregador
            string routingKey = $"wavy.data.prefer.{preferredAgregatorId}";

            Log($"[{id}] Mensagem preparada. Tamanho: {bodyBytes.Length} bytes. Routing key: {routingKey}");

            var properties = _rabbitChannel.CreateBasicProperties();
            properties.Persistent = true; // Make messages persistent

            Log($"[{id}] Enviando mensagem para exchange '{RabbitMqConstants.WavyTopicExchange}'...");

            _rabbitChannel.BasicPublish(
                exchange: RabbitMqConstants.WavyTopicExchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: bodyBytes);

            Log($"[{id}] ✓ SUCESSO! Bloco enviado com header '{header}' para exchange '{RabbitMqConstants.WavyTopicExchange}' com RK '{routingKey}'.");
        }
        catch (Exception ex)
        {
            Log($"[{id}] ✗ ERRO ao enviar bloco via RabbitMQ: {ex.Message}");
            Log($"[{id}] Stack trace: {ex.StackTrace}");
            // Consider closing/re-establishing channel on certain errors
            try
            {
                _rabbitChannel?.Close(); // May force re-init on next send
            }
            catch (Exception closeEx)
            {
                Log($"[{id}] Erro ao fechar canal: {closeEx.Message}");
            }
            _rabbitChannel = null;
        }
    }
    // Call this when Wavy is disposed or application shuts down
    public void CloseRabbitMq()
    {
        try
        {
            _rabbitChannel?.Close();
            _rabbitConnection?.Close();
        }
        catch (Exception ex)
        {
            Log($"Error closing RabbitMQ for {id}: {ex.Message}");
        }
    }



    // ###################################################################################################################### //
    // --- Nestas funções, os dados são recebidos individualmente, ou seja, cada linha do bufferDados vai ser de cada tipo de dado individualmente
    /*
        Exemplo de um bufferDados completo, se usadas estas funções:
        25.2        [Temperatura]
        37.020244   [GPS]
        25.5        [Temperatura]
        37.020106   [GPS]
        25.8        [Temperatura]

        Funções foram mantidas aqui caso seja necessário o uso
    */
    public async Task ReceberDadosIndividual(List<TipoDado> tipoDados)
    {
        // Cria uma lista de tasks para cada simulador.
        var tasks = new List<Task>();

        foreach (TipoDado tipo in tipoDados)
        {
            if (SimuladorFactory.Simuladores.TryGetValue(tipo, out var simulatorFunc))
            {
                // Inicia o simulador para este tipo.
                // Nota: Passamos "this", ou seja, a própria instância de Wavy.
                var simulatorStream = simulatorFunc(this);

                // Cria uma task para processar os dados desse simulador.
                Task task = ProcessSimulatorStream(tipo, simulatorStream);
                tasks.Add(task);
            }
            else
            {
                Log($"Nenhum simulador encontrado para o tipo: {tipo}");
            }
        }

        // Aguarda que todas as tasks concluam.
        await Task.WhenAll(tasks);
    }
    private async Task ProcessSimulatorStream(TipoDado tipo, IAsyncEnumerable<string> simulatorStream)
    {
        await foreach (string output in simulatorStream)
        {
            lock (bufferDados)
            {
                if (bufferDados.Count >= MaxBufferSize)
                    GerirLista();

                bufferDados.Add(output);
            }
            // Log($"[{tipo}] Data added to list: {output}");
        }
    }
    // ###################################################################################################################### //
}