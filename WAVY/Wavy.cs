using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;

public enum Estado
{
    Ativo,
    Desativo
}
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
    private string AgregadorIP;
    private int Port;
    public string WavyID;
    public Estado EstadoWavy = Estado.Ativo;
    private List<string> bufferDados;
    private const int MaxBufferSize = 5; // Tamanho máximo do buffer

    public Wavy(string IP, int port, string ID)
    {
        AgregadorIP = IP;
        Port = port;
        WavyID = ID;
        bufferDados = new List<string>();
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
    */
    public async Task ReceberDadosIndividual(List<TipoDado> tipoDados)
    {
        // Cria uma lista de tasks para cada simulador.
        var tasks = new List<Task>();

        foreach (TipoDado tipo in tipoDados)
        {
            if (SimuladorFactory.Simulators.TryGetValue(tipo, out var simulatorFunc))
            {
                // Inicia o simulador para este tipo.
                // Note: Passamos "this", ou seja, a própria instância de Wavy.
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
            // Aqui você pode fazer qualquer processamento adicional, se necessário.
            // Por exemplo, se desejar combinar os dados de vários sensores numa única linha,
            // você pode armazenar cada sensor em um dicionário temporário e só juntar quando todos tiverem produzido um novo valor.
            // Neste exemplo, cada output é adicionado individualmente.
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
    public async Task ReceberDados(List<TipoDado> tipoDados)
    {
        Random random = new Random();
        // Create a list of enumerators – one per sensor type.
        var enumerators = new List<(TipoDado Tipo, IAsyncEnumerator<string> Enumerator)>();

        foreach (TipoDado tipo in tipoDados)
        {
            if (SimuladorFactory.Simulators.TryGetValue(tipo, out var simulatorFunc))
            {
                // Get the enumerator from the IAsyncEnumerable<string>
                IAsyncEnumerator<string> enumerator = simulatorFunc(this).GetAsyncEnumerator();
                enumerators.Add((tipo, enumerator));
            }
            else
            {
                Console.WriteLine($"Nenhum simulador encontrado para o tipo: {tipo}");
            }
        }

        // Infinite loop – adjust as needed (or add cancellation)
        while (true)
        {
            // Prepare a list to hold the current values from all enumerators.
            var sensorValues = new List<string>();

            string last_sync = string.Empty;

            // For each sensor enumerator, wait for the next value.
            foreach (var (Tipo, Enumerator) in enumerators)
            {
                // Await the next result; if one sensor ends, you can decide to break out.
                bool hasNext = await Enumerator.MoveNextAsync();
                if (!hasNext)
                {
                    Console.WriteLine($"O simulador para {Tipo} terminou.");
                    return; // or break, depending on your requirements
                }

                string dadosRecebidos = Enumerator.Current;
                string[] partes = dadosRecebidos.Split(':');
                if (partes.Length > 1)
                    last_sync = partes[1];
                else
                    last_sync = "N/A";

                sensorValues.Add(partes[0]);
            }

            // Combine the results into a single composite string.
            // For a device with GPS and Gyroscope, it would produce something like: [data_gps:data_gyro]
            string compositeOutput = "[" + string.Join(":", sensorValues) + "]" + ":" + last_sync;
            if (bufferDados.Count >= MaxBufferSize)
                    GerirLista();
            bufferDados.Add(compositeOutput);

            // Print to console (or process/store further)
            // Console.WriteLine("Composite Data: " + compositeOutput);

            // Optionally, add a delay between iterations,
            // or let the simulators pace themselves with their own delays.

            // Gera um atraso aleatório, para simular a leitura dos sensores
            // Em .Next, o primeiro parâmetro é inclusivo e o segundo é exclusivo.
            // int delay = random.Next(100, 201);
            await Task.Delay(500);
        }
    }
    

    private void GerirLista()
    {
        // Verifica se o buffer atingiu o tamanho máximo
        if (bufferDados.Count >= MaxBufferSize)
        {
            // Envia o bloco de dados para o agregador
            EnviarBloco();
            Console.WriteLine(WavyID + " List ::");
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

    private void EnviarBloco()
    {
        
        try
        {
            using (TcpClient client = new TcpClient(AgregadorIP, Port))
            {
                NetworkStream stream = client.GetStream();
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    // Envia a linha de cabeçalho com o identificador do bloco e o número de linhas
                    string header = "BLOCK " + bufferDados.Count + " STATUS " + EstadoWavy.ToString();
                    writer.WriteLine(header);
                    Console.WriteLine("Enviado header: " + header);

                    // Envia cada linha do bloco
                    foreach (string linha in bufferDados)
                    {
                        writer.WriteLine(WavyID + ":" + linha);
                        Console.WriteLine("Enviada linha: " + WavyID + ":" + linha);
                    }

                    // Aguarda o ACK do Agregador
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
}