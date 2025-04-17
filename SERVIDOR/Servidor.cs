using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Servidor
{
    // Porta para escutar as conexões dos AGREGADORes
    private static readonly int Port = 5000;
    private static readonly string dataFolder = "dados";
    private static readonly string[] tiposValidos = {
        "gps", "gyro", "humidade", "ph", "temperatura"
    };
    private static readonly Mutex wavysFileMutex = new Mutex();

    public static void Main()
    {
        if (!Directory.Exists(dataFolder))
        {
            Console.WriteLine("Erro: Pasta 'data/' não existe.");
            return;
        }

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
                Thread clientThread = new Thread(() => ProcessaCliente(client));
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
        // Verifica a existência e inicia cada ficheiro CSV
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
    private static void ProcessaCliente(TcpClient client)
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
                    string header = reader.ReadLine();
                    if (!string.IsNullOrEmpty(header))
                    {
                        Console.WriteLine("Header recebido: " + header);

                        if (header != null && header.StartsWith("BLOCK"))
                        {
                            string[] partes = header.Split(' ');
                            if (partes.Length == 4 && int.TryParse(partes[1], out int numLinhas) && partes[2] == "TYPE") 
                            {
                                string tipo = partes[3];
                                string[] bloco = new string[numLinhas];
                                for (int i = 0; i < numLinhas; i++)
                                    bloco[i] = reader.ReadLine();
                                
                                // Agora, 'bloco' contém todas as linhas enviadas pelo AGREGADOR.
                                Console.WriteLine("Bloco de dados recebido:");
                                foreach (string linha in bloco)
                                    Console.WriteLine(linha);

                                ProcessaBloco(bloco, tipo);

                                // Envia o ACK para confirmar o recebimento
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
            string path = Path.Combine(dataFolder, tipo + ".csv");
            wavysFileMutex.WaitOne();
            try
            {
                using (StreamWriter sw = new StreamWriter(path, append: true))
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
