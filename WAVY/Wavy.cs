using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;

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

    // IP do AGREGADOR associado
    private string agregadorIp;

    // Port do AGREGADOR associado
    private int agregadorPort;

    // Estado da WAVY
    public Estado estadoWavy = Estado.Ativo;

    // Buffer de dados a enviar para o AGREGADOR
    private List<string> bufferDados;

    // Tamanho máximo do buffer
    private const int MaxBufferSize = 5; 

    // Construtor da WAVY
    public Wavy(string _id, string _agregadorIp, int _agregadorPort)
    {
        id = _id;
        agregadorIp = _agregadorIp;
        agregadorPort = _agregadorPort;
        bufferDados = new List<string>();
    }

    //Esta função recebe dados dos sensores.
    //Ou seja, utiliza os simuladores de sensores (na pasta geradores) para simular leitura de dados em tempo real.
    public async Task ReceberDados(List<TipoDado> tipoDados)
    {
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
            }
            else
            {
                Console.WriteLine($"Nenhum simulador encontrado para o tipo: {tipo}");
            }
        }

        // Inicia o loop da simulação dos sensores
        while (true)
        {
            // Lista que contém todos os valores do enumeradores
            var valoresSensor = new List<string>();

            // Variável para guardar a data na qual o sensor foi lido
            string last_sync = string.Empty;

            foreach (var (Tipo, Enumerador) in enumeradores)
            {
                bool hasNext = await Enumerador.MoveNextAsync();
                if (!hasNext)
                {
                    Console.WriteLine($"O simulador para {Tipo} terminou.");
                    return;
                }

                string dadosRecebidos = Enumerador.Current;
                string[] partes = dadosRecebidos.Split(':');

                // Verifica se existe ou não data na qual o sensor foi lido. Se não existir, fica "N/A"
                if (partes.Length > 1)
                    last_sync = partes[1];
                else
                    last_sync = "N/A";

                valoresSensor.Add(partes[0]);
            }

            // Combina todos os dados recebidos dos possíveis diferentes tipos de sensores e combina-os numa linha da lista
            string compositeOutput = "[" + string.Join(":", valoresSensor) + "]" + ":" + last_sync;

            // Se a lista ultrapassar o tamanho máximo, chama a função GerirLista
            if (bufferDados.Count >= MaxBufferSize)
                    GerirLista();
            
            // Adiciona a lista ao bufferDados
            bufferDados.Add(compositeOutput);

            // Gera um atraso aleatório, para simular a leitura dos sensores
            // Em .Next, o primeiro parâmetro é inclusivo e o segundo é exclusivo.
            // int delay = random.Next(100, 201);
            await Task.Delay(500);
        }
    }
    
    // Esta função chama a função EnviarBloco e limpa o bufferDados
    private void GerirLista()
    {
        // Verifica se o buffer atingiu o tamanho máximo
        if (bufferDados.Count >= MaxBufferSize)
        {
            // Envia o bloco de dados para o AGREGADOR
            EnviarBloco();

            // Debug : Faz print da lista
            Console.WriteLine(id + " List :");
            foreach (string element in bufferDados)
            {
                Console.WriteLine("| List - " + element);
            }

            // Limpa o buffer após enviar
            lock (bufferDados)
            {
                bufferDados.Clear();
            }
        }
    }

    // Esta função envia o bloco de dados (bufferDados) para o AGREGADOR associado
    private void EnviarBloco()
    {
        
        try
        {
            using (TcpClient client = new TcpClient(agregadorIp, agregadorPort))
            {
                NetworkStream stream = client.GetStream();
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    // Envia a linha de cabeçalho com o identificador do bloco e o número de linhas
                    string header = "BLOCK " + bufferDados.Count + " STATUS " + estadoWavy.ToString();

                    // Debug : Faz print do header
                    writer.WriteLine(header);
                    Console.WriteLine("Enviado header: " + header);

                    // Envia cada linha do bloco
                    foreach (string linha in bufferDados)
                    {
                        writer.WriteLine(id + ":" + linha);
                    }

                    // Aguarda o ACK do AGREGADOR
                    string resposta = reader.ReadLine();
                    if (resposta == "ACK")
                    {
                        Console.WriteLine("ACK recebido. Bloco enviado com sucesso.");
                    }
                    else
                    {
                        Console.WriteLine("Resposta inesperada: " + resposta);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao enviar bloco: " + ex.Message);
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
                Console.WriteLine($"Nenhum simulador encontrado para o tipo: {tipo}");
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
            // Console.WriteLine($"[{tipo}] Data added to list: {output}");
        }
    }
    // ###################################################################################################################### //
}