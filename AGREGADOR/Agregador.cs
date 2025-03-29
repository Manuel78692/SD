using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

class Agregador
{
    private static readonly int Port = 5000; // Porta a utilizar

    public static void Main()
    {
        TcpListener server = new TcpListener(IPAddress.Any, Port);
        server.Start();
        Console.WriteLine("Agregador aguardando conexões...");

        while (true)
        {
            using (TcpClient client = server.AcceptTcpClient())
            {
                Console.WriteLine("Conexão recebida.");
                NetworkStream stream = client.GetStream();
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    // Lê a mensagem única enviada pela WAVY
                    string mensagem = reader.ReadLine();
                    Console.WriteLine("Dados recebidos: " + mensagem);
                    
                    // Aqui pode-se processar os dados (ex: salvar num ficheiro CSV)
                    ProcessarDados(mensagem);

                    // Envia o ACK para confirmar o recebimento
                    writer.WriteLine("ACK");
                    Console.WriteLine("ACK enviado, encerrando conexão.");
                }
            }
        }
    }

    private static void ProcessarDados(string dados)
    {
        // Exemplo: acrescenta os dados a um ficheiro CSV
        string filePath = "dados_agregados.csv";
        File.AppendAllText(filePath, dados + Environment.NewLine);
    }
}
