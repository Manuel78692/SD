using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SERVIDOR.Services;

class Servidor
{
    private string id;  
    // Porta do SERVIDOR para escutar as conexões dos AGREGADORes
    private static readonly int port = 5000;

    // Pasta onde irá guardar os dados
    private static readonly string dataFolder = "dados";

    // Tipos de dados válidos
    private static readonly string[] tiposValidos = {
        "gps", "gyro", "humidade", "ph", "temperatura"
    };    // Mutex para garantir a exclusão mútua ao escrever no arquivo CSV
    private static readonly Mutex wavysFileMutex = new Mutex();
    public event Action<string>? OnLogEntry;

    public Servidor() { id = "servidor"; }

    public string GetId() { return id; }

    public void Log(string msg) { OnLogEntry?.Invoke(msg); }

    // Database service for sensor data operations
    private static readonly SensorDataService sensorDataService = new SensorDataService();

    public void Run()
    {
        // Verifica se a pasta "dados" existe
        if (!Directory.Exists(dataFolder))
        {
            Log($"Erro: Pasta '{dataFolder}/' não existe.\n");
            return;
        }

        InitializeCSVs();

        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Log("Servidor iniciado. Aguardando conexões...\n");

        Task.Run(() =>
        {
            while (true)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Log("Conexão recebida.");
                    Thread clientThread = new Thread(() => ProcessaAgregador(client));
                    clientThread.Start();
                }
                catch (Exception ex)
                {
                    Log("Erro ao aceitar conexão: " + ex.Message + "\n");
                }
            }
        });
    }

    // Esta função cria os ficheiros CSV dos tipos de dados, se não existir
    private static void InitializeCSVs()
    {
        foreach (string tipo in tiposValidos)
        {
            string path = Path.Combine(dataFolder, tipo + ".csv");
            if (!File.Exists(path))
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.WriteLine("WAVY_ID:Dado:Timestamp");
                }
            }
        }
    }

    // Esta função processa os dados recebidos dos AGREGADORes
    private  void ProcessaAgregador(TcpClient client)
    {
        try
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    // Lê a linha de header que indica o início do bloco e quantas linhas seguirão e qual o tipo de dados
                    string header = reader.ReadLine();
                    if (!string.IsNullOrEmpty(header))
                    {
                        Log("Header recebido: " + header);

                        if (header != null && header.StartsWith("BLOCK"))
                        {
                            // Extrai o número de linhas a serem lidas
                            string[] partes = header.Split(' ');
                            if (partes.Length == 4 && int.TryParse(partes[1], out int numLinhas) && partes[2] == "TYPE")
                            {
                                // Variável que guarda o tipo de dados recebidos
                                string tipo = partes[3];

                                // Variável que irá guardar o bloco de mensagens enviadas pelo AGREGADOR
                                string[] bloco = new string[numLinhas];
                                for (int i = 0; i < numLinhas; i++)
                                    bloco[i] = reader.ReadLine();

                                // Agora, 'bloco' contém todas as linhas enviadas pelo AGREGADOR.
                                // Debug : Faz print do bloco de dados recebido
                                Log("Bloco de dados recebido:");
                                foreach (string linha in bloco)
                                    Log(linha);

                                // Processa o bloco conforme necessário
                                ProcessaBloco(bloco, tipo);

                                // Envia o ACK para confirmar o recebimento do bloco
                                writer.WriteLine("ACK");
                                Log("ACK enviado ao AGREGADOR.\n");
                            }
                        }
                        else
                        {
                            Log("Formato de header inválido.\n");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log("Erro ao processar conexão: " + ex.Message + "\n");
        }
    }
    private  void ProcessaBloco(string[] bloco, string tipo)
    {
        /*
            Cada linha do AGREGADOR vem no formato seguinte: "WAVY_ID:data:date_of_reading"
            Já que a mensagem vem com um HEADER do tipo "BLOCK [size] TYPE [data_type]", o tipo de dados já está declarado

            O SERVIDOR agora salva os dados tanto na base de dados SQLite quanto nos ficheiros CSV
            O formato do CSV continua: "WAVY_ID:Dado:Timestamp"

            O SERVIDOR deve verificar se o tipo de dado é válido, caso contrário, não guarda nada
        */
        if (Array.Exists(tiposValidos, t => t == tipo))
        {
            // First, try to save to database
            try
            {
                var task = sensorDataService.SaveSensorDataAsync(bloco, tipo);
                task.Wait(); // Since this is a console app, we'll wait synchronously
                Log($"Bloco de dados do tipo '{tipo}' salvo na base de dados com sucesso.");
            }
            catch (Exception ex)
            {
                Log($"Erro ao salvar dados do tipo '{tipo}' na base de dados: {ex.Message}");
                Log("Continuando com salvamento em CSV como fallback...");
            }

            // Also save to CSV file (for backup/redundancy)
            string filePath = Path.Combine(dataFolder, tipo + ".csv");
            wavysFileMutex.WaitOne();
            try
            {
                using (StreamWriter sw = new StreamWriter(filePath, append: true))
                {
                    foreach (string linha in bloco)
                        sw.WriteLine(linha);
                }
                Log($"Bloco de dados do tipo '{tipo}' também salvo em CSV.");
            }
            catch (Exception ex)
            {
                Log($"Erro ao salvar bloco de dados do tipo '{tipo}' em CSV: {ex.Message}");
            }
            finally
            {
                wavysFileMutex.ReleaseMutex();
            }
        }
        else
        {
            Log($"Tipo de dado '{tipo}' inválido. Bloco descartado.");
        }
    }
}
