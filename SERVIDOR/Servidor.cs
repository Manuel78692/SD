using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Servidor
{
    // Porta do SERVIDOR para escutar as conexões dos AGREGADORes
    private static readonly int port = 5000;

    // Pasta onde irá guardar os dados
    private static readonly string dataFolder = "dados";

    // Tipos de dados válidos
    private static readonly string[] tiposValidos = {
        "gps", "gyro", "humidade", "ph", "temperatura"
    };

    // Mutex para garantir a exclusão mútua ao escrever no arquivo CSV
    private static readonly Mutex wavysFileMutex = new Mutex();

    public static void Main()
    {
        // Verifica se a pasta "dados" existe
        if (!Directory.Exists(dataFolder))
        {
            Console.WriteLine($"Erro: Pasta '{dataFolder}/' não existe.");
            return;
        }

        InitializeCSVs();

        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine("Servidor iniciado. Aguardando conexões...");

        while (true)
        {
            try
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Conexão recebida.");
                Thread clientThread = new Thread(() => ProcessaAgregador(client));
                clientThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao aceitar conexão: " + ex.Message);
            }
        }
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
    private static void ProcessaAgregador(TcpClient client)
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
                        Console.WriteLine("Header recebido: " + header);

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
                                Console.WriteLine("Bloco de dados recebido:");
                                foreach (string linha in bloco)
                                    Console.WriteLine(linha);

                                // Processa o bloco conforme necessário
                                ProcessaBloco(bloco, tipo);

                                // Envia o ACK para confirmar o recebimento do bloco
                                writer.WriteLine("ACK");
                                Console.WriteLine("ACK enviado. Encerrando conexão.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Formato de header inválido.");
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
    private static void ProcessaBloco(string[] bloco, string tipo)
    {
        /*
            Cada linha do AGREGADOR vem no formato seguinte: "WAVY_ID:data:date_of_reading"
            Já que a mensagem vem com um HEADER do tipo "BLOCK [size] TYPE [data_type]", o tipo de dados já está declarado

            O que o SERVIDOR faz é separar os dados e guardá-los no respectivo ficheiro CSV
            O formato do CSV é "WAVY_ID:Dado:Timestamp"

            O SERVIDOR deve verificar se o tipo de dado é válido, caso contrário, não guarda nada
        */
        if (Array.Exists(tiposValidos, t => t == tipo))
        {
            string filePath = Path.Combine(dataFolder, tipo + ".csv");
            wavysFileMutex.WaitOne();
            try
            {
                using (StreamWriter sw = new StreamWriter(filePath, append: true))
                {
                    foreach (string linha in bloco)
                        sw.WriteLine(linha);
                }
                Console.WriteLine($"Bloco de dados do tipo '{tipo}' salvo com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar bloco de dados do tipo '{tipo}': {ex.Message}");
            }
            finally
            {
                wavysFileMutex.ReleaseMutex();
            }
        }
        else
        {
            Console.WriteLine($"Tipo de dado '{tipo}' inválido. Bloco descartado.");
        }
    }
}
