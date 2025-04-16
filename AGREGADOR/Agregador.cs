using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Globalization;

class Agregador
{
    // Porta para escutar as conexões das WAVYs
    private static readonly int PortWavy = 5001;
<<<<<<< HEAD
    // Dados do Servidor para encaminhamento (se necessário)
=======
    // Ip do SERVIDOR
>>>>>>> AL78555_v2
    private static readonly string ServidorIP = "127.0.0.1";
    // Porta para enviar os dados para o SERVIDOR
    private static readonly int PortServidor = 5000;
    
    // Pasta onde estão os CSV's (deve existir)
    private static readonly string dataFolder = "data";
    // Tipos de dados válidos
    private static readonly string[] tiposValidos = {
        "Humidade", "Temperatura", "PH", "Acelerometro", "Gyroscopio", "GPS", "Timestamp"
    };
    
    // Objeto de lock único para escrita nos ficheiros
    private static readonly object fileLock = new object();

    public static void Main()
    {
        // Verifica se a pasta "data" existe
        if (!Directory.Exists(dataFolder))
        {
            Console.WriteLine("Erro: Pasta 'data/' não existe.");
            return;
        }

        // Inicializa os ficheiros CSV (só cria se não existirem)
        InitializeCSVs();

        // Inicia o listener para conexões das WAVYs
        TcpListener listener = new TcpListener(IPAddress.Any, PortWavy);
        listener.Start();
        Console.WriteLine("Agregador iniciado na porta " + PortWavy + ". Aguardando conexões das WAVYs...");

        while (true)
        {
            try
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Conexão de uma WAVY recebida.");
                Thread clientThread = new Thread(() => ProcessaWavy(client));
                clientThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao aceitar conexão: " + ex.Message);
            }
        }
    }

    private static void InitializeCSVs()
    {
        lock (fileLock)
        {
            // Para cada tipo válido, assegura que o ficheiro CSV existe com o cabeçalho no formato definido.
            foreach (string tipo in tiposValidos)
            {
                string path = Path.Combine(dataFolder, tipo + ".csv");
                if (!File.Exists(path))
                {
                    using (StreamWriter sw = new StreamWriter(path))
                    {
                        // Cabeçalho: Wavy_ID:Status:[Data_type]:last_sync
                        sw.WriteLine($"Wavy_ID:Status:[{tipo}]:last_sync");
                    }
                }
            }
        }
    }

    private static void ProcessaWavy(TcpClient client)
    {
        try
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    // Exemplo de header: "BLOCK 1", indicando que haverá um bloco com 1 linha.
                    string header = reader.ReadLine();
                    Console.WriteLine("Header recebido: " + header);

                    if (header != null && header.StartsWith("BLOCK"))
                    {
<<<<<<< HEAD
                        string[] partesHeader = header.Split(' ');
                        if (partesHeader.Length == 2 && int.TryParse(partesHeader[1], out int numLinhas))
=======
                        // Extrai o número de linhas a serem lidas
                        string[] partes = header.Split(' ');
                        if (partes.Length == 2 && int.TryParse(partes[1], out int numLinhas))
>>>>>>> AL78555_v2
                        {
                            string[] bloco = new string[numLinhas];
                            for (int i = 0; i < numLinhas; i++)
                                bloco[i] = reader.ReadLine();
                            
                            // Agora, 'bloco' contém todas as linhas enviadas pela WAVY.
                            Console.WriteLine("Bloco de dados recebido:");
                            foreach (string linha in bloco)
                                Console.WriteLine(linha);
                            
                            // Processa o bloco conforme necessário
                            ProcessaBloco(bloco);

                            // Envia ACK para confirmar o recebimento do bloco
                            writer.WriteLine("ACK");
                            Console.WriteLine("ACK enviado à WAVY.");
                        }
                        else
                        {
                            Console.WriteLine("Formato de header inválido.");
                            writer.WriteLine("ERRO: Formato de header inválido.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Header não identificado.");
                        writer.WriteLine("ERRO: Header não identificado.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao processar dados da WAVY: " + ex.Message);
        }
    }

    private static void ProcessaBloco(string[] bloco)
    {
<<<<<<< HEAD
        // Cada linha do bloco tem o formato:
        // "WAVY_ID:STATUS:[Tipo1=Timestamp1, Tipo2=Timestamp2, ...]"
=======
        /*
            Cada linha do WAVY vem no formato seguinte: "WAVY_ID:[data_type=data]"

            O que o AGREGADOR faz é separar os dados e encaminhá-los para o Servidor
            Depois também deverá fazer algum tipo de pré-processamento dos dados, se necessário

            O suposto é o AGREGADOR verificar a partir de um ficheiro de configuração em .csv 
            o que deverá fazer para cada tipo de dado em cada WAVY

            Cada linha desse ficheiro segue o seguinte formato: 
            "WAVY_ID:pré_processamento:volume_dados_enviar:servidor_associado"
            Como neste momento apenas existe um servidor, não há pré-processamento e o volume_dados_enviar é redundante, 
            não lê o ficheiro

            Antes de enviar para o servidor, convém guardar o estado e o 'last sync' do WAVY conectado
            No ficheiro "wavys.csv", cada linha corresponde a "WAVY_ID:status:[data_types]:last_sync"

            O AGREGADOR também deve atualizar o estado da WAVY. Os estados são Associada, Operação, Manutenção, Desativada
        */

        // Dicionário para armazenar listas para cada tipo de dado
        Dictionary<string, List<string>> sensorData = new Dictionary<string, List<string>>();

>>>>>>> AL78555_v2
        foreach (string linha in bloco)
        {
            try
            {
<<<<<<< HEAD
                // Separa a parte inicial ("WAVY_ID:STATUS") dos dados.
                string[] partes = linha.Split(new char[] {':'}, 3);
                if (partes.Length < 3)
                {
                    Console.WriteLine("Linha com formato inesperado: " + linha);
                    continue;
                }

                string wavyId = partes[0].Trim();
                string status = partes[1].Trim();
                // A parte restante contém os dados entre parenteses retos
                string dadosComPR = partes[2].Trim();
                if (dadosComPR.StartsWith("[") && dadosComPR.EndsWith("]"))
                {
                    string dadosConteudo = dadosComPR.Substring(1, dadosComPR.Length - 2);
                    // Divide os pares "Tipo=Timestamp" usando a vírgula
                    string[] pares = dadosConteudo.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string par in pares)
                    {
                        string[] info = par.Split('=');
                        if (info.Length != 2)
                        {
                            Console.WriteLine("Formato de par inválido: " + par);
                            continue;
                        }
                        
                        string tipo = info[0].Trim();
                        string timestampRecebido = info[1].Trim();

                        if (!Array.Exists(tiposValidos, t => t.Equals(tipo, StringComparison.OrdinalIgnoreCase)))
                        {
                            Console.WriteLine("Tipo de dado '" + tipo + "' não é válido. Ignorando.");
                            continue;
                        }

                        // Tenta converter o timestamp para DateTime usando o formato "yyyy-MM-dd HH:mm:ss".
                        DateTime timestamp;
                        if (!DateTime.TryParseExact(timestampRecebido, "yyyy-MM-dd HH:mm:ss", 
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp))
                        {
                            Console.WriteLine("Timestamp inválido para " + tipo + ". Usando a hora atual.");
                            timestamp = DateTime.Now;
                        }

                        // Formata o timestamp para "YYYY-MM-DD-HH-mm-ss"
                        string timestampFormatado = timestamp.ToString("yyyy-MM-dd-HH-mm-ss");

                        // Linha do CSV no formato: Wavy_ID:Status:[Data_type]:last_sync
                        string linhaCSV = $"{wavyId}:{status}:[{tipo}]:{timestampFormatado}";
                        string caminhoCSV = Path.Combine(dataFolder, tipo + ".csv");

                        lock (fileLock)
                        {
                            using (StreamWriter sw = new StreamWriter(caminhoCSV, true))
                            {
                                sw.WriteLine(linhaCSV);
                            }
                        }
                        Console.WriteLine($"Dados para '{tipo}' atualizados com sucesso no CSV.");
                    }
                }
                else
                {
                    Console.WriteLine("Dados do bloco sem formatação de Parenteses retos " + dadosComPR);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao processar a linha do bloco: " + ex.Message);
=======
                // Aqui, a linha é do tipo "data_type=data"
                string[] tipoDado = dado.Split('=');
                string dataType = tipoDado[0].Trim(); // Tipo de dado
                string data = tipoDado[1].Trim(); // Valor do dado

                // Se a lista para este tipo de dado não existir, cria-a
                if (!sensorData.ContainsKey(dataType))
                    sensorData[dataType] = new List<string>();

                // Adiciona o dado à lista apropriada
                sensorData[dataType].Add(data);
>>>>>>> AL78555_v2
            }
        }
        // Debug: Faz print dos dados separados por tipo
        foreach (var entry in sensorData)
        {
            Console.WriteLine($"Tipo de dado: {entry.Key}");
            foreach (var data in entry.Value)
                Console.WriteLine($"  Dado: {data}");
        }
        EncaminhaParaServidor(sensorData);
    }
<<<<<<< HEAD

    // Exemplo de método para encaminhar dados para o Servidor (caso seja necessário)
    private static void EncaminhaParaServidor(string dados)
=======
    private static void EncaminhaParaServidor(Dictionary<string, List<string>> dados)
>>>>>>> AL78555_v2
    {
        // No servidor, existem ficheiros .csv para cada tipo de dados
        // O que o AGREGADOR deverá fazer é enviar os dados separados por tipo de dados
        /* 
            Formato de dados enviados para o servidor:
            BLOCK size_of_data TYPE data_type
            WAVY_ID:data
            (...)
        */
        try
        {
            foreach (var entry in dados)
            {
                using (TcpClient clienteServidor = new TcpClient(ServidorIP, PortServidor))
                {
                    NetworkStream stream = clienteServidor.GetStream();
                    using (StreamReader reader = new StreamReader(stream))
                    using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                    {
                        // Envia os dados para o Servidor
                        string tipoDado = entry.Key;
                        List<string> valores = entry.Value;

                        // Envia o header com o tipo de dado
                        writer.WriteLine("BLOCK " + valores.Count + " " + "TYPE " + tipoDado);

                        // Envia os dados da WAVY
                        foreach (string valor in valores)
                            writer.WriteLine(valor);

                        // Aguarda ACK do Servidor
                        string resposta = reader.ReadLine();
                        if (resposta == "ACK")
                            Console.WriteLine("ACK recebido do Servidor.");
                    }
                }
            }
            
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao encaminhar dados para o Servidor: " + ex.Message);
        }
    }
}

