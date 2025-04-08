using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Globalization;

class Servidor
{
    private static readonly int Port = 5000;
    private static readonly object fileLock = new object();
    private static readonly string dataFolder = "data";
    private static readonly string[] tiposValidos = {
        "Humidade", "Temperatura", "PH", "Acelerometro", "Gyroscopio", "GPS", "Timestamp"
    };

    public static void Main()
    {
        // Verifica se a pasta "data" existe. Se não existir, exibe mensagem e encerra.
        if (!Directory.Exists(dataFolder))
        {
            Console.WriteLine("Erro: Pasta 'data/' não existe.");
            return;
        }

        // Inicializa os ficheiros CSV se ainda não estiverem criados
        InitializeCSVs();

        TcpListener listener = new TcpListener(IPAddress.Any, Port);
        listener.Start();
        Console.WriteLine("Servidor iniciado. Aguardando conexões...");

        while (true)
        {
            try
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Conexão recebida.");

                // Inicia uma nova thread para tratar a conexão
                Thread clientThread = new Thread(() => ProcessClient(client));
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
            // Verifica a existência e inicia cada ficheiro CSV
            foreach (string tipo in tiposValidos)
            {
                string path = Path.Combine(dataFolder, tipo + ".csv");
                if (!File.Exists(path))
                {
                    using (StreamWriter sw = new StreamWriter(path))
                    {
                        sw.WriteLine("WAVY_ID,Dado,TimestampRecebido,TimestampFormatado");
                    }
                }
            }
        }
    }

    private static void ProcessClient(TcpClient client)
    {
        try
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    // Lê a mensagem única enviada pelo cliente
                    string mensagemRecebida = reader.ReadLine();
                    if (!string.IsNullOrEmpty(mensagemRecebida))
                    {
                        Console.WriteLine("Mensagem recebida: " + mensagemRecebida);

                        // Espera-se o formato: WAVY_ID:TIPO:DADO:TIMESTAMP
                        string[] partes = mensagemRecebida.Split(':');
                        if (partes.Length == 4)
                        {
                            string wavyId = partes[0].Trim();
                            string tipo = partes[1].Trim();
                            string dado = partes[2].Trim();
                            string timestampOriginal = partes[3].Trim();

                            if (Array.Exists(tiposValidos, t => t.Equals(tipo, StringComparison.OrdinalIgnoreCase)))
                            {
                                DateTime timestamp;
                                if (!DateTime.TryParse(timestampOriginal, CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp))
                                {
                                    Console.WriteLine("Timestamp inválido. Usando a hora atual.");
                                    timestamp = DateTime.Now;
                                }

                                string timestampFormatado = timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                                string linhaCSV = $"{wavyId},{dado},{timestampOriginal},{timestampFormatado}";
                                string caminhoCSV = Path.Combine(dataFolder, tipo + ".csv");

                                lock (fileLock)
                                {
                                    using (StreamWriter sw = new StreamWriter(caminhoCSV, true))
                                    {
                                        sw.WriteLine(linhaCSV);
                                    }
                                }

                                writer.WriteLine("ACK");
                                Console.WriteLine($"Dado '{tipo}' gravado com sucesso em {tipo}.csv");
                            }
                            else
                            {
                                Console.WriteLine("Tipo de dado inválido.");
                                writer.WriteLine("ERRO: Tipo inválido.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Formato de mensagem incorreto.");
                            writer.WriteLine("ERRO: Formato inválido.");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao processar conexão: " + ex.Message);
        }
    }
}
