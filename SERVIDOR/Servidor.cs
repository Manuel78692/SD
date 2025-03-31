using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Servidor
{
    private static readonly int Port = 5000; // Mesma porta utilizada pelo Agregador

    public static void Main()
    {
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
                        
                        // Aqui você pode processar a mensagem conforme necessário,
                        // por exemplo, gravar num ficheiro ou exibir no console.
                        
                        // Envia o ACK para confirmar o recebimento
                        writer.WriteLine("ACK");
                        Console.WriteLine("ACK enviado. Encerrando conexão.");
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
