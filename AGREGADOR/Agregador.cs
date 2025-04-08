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
    // Dados do Servidor para encaminhamento (se necessário)
    private static readonly string ServidorIP = "127.0.0.1";
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
                        string[] partesHeader = header.Split(' ');
                        if (partesHeader.Length == 2 && int.TryParse(partesHeader[1], out int numLinhas))
                        {
                            string[] bloco = new string[numLinhas];
                            for (int i = 0; i < numLinhas; i++)
                            {
                                bloco[i] = reader.ReadLine();
                            }
                            
                            // Agora, 'bloco' contém todas as linhas enviadas pela WAVY.
                            Console.WriteLine("Bloco de dados recebido:");
                            foreach (string linha in bloco)
                            {
                                Console.WriteLine(linha);
                            }
                            
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
            // Envia dados para o servidor
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao processar dados da WAVY: " + ex.Message);
        }
    }

    private static void ProcessaBloco(string[] bloco)
    {
        // Cada linha do bloco tem o formato:
        // "WAVY_ID:STATUS:[Tipo1=Timestamp1, Tipo2=Timestamp2, ...]"
        foreach (string linha in bloco)
        {
            try
            {
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
            }
        }
    }

    // Exemplo de método para encaminhar dados para o Servidor (caso seja necessário)
    private static void EncaminhaParaServidor(string dados)
    {
        // No servidor, existem .csv para cada tipo de dados
        // O que o AGREGADOR deverá fazer é enviar os dados separados por tipo de dados
        try
        {
            using (TcpClient clienteServidor = new TcpClient(ServidorIP, PortServidor))
            {
                NetworkStream stream = clienteServidor.GetStream();
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    // Envia os dados para o Servidor
                    writer.WriteLine(dados);
                    Console.WriteLine("Dados encaminhados ao Servidor: " + dados);

                    // Aguarda ACK do Servidor
                    string resposta = reader.ReadLine();
                    if (resposta == "ACK")
                    {
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
