using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Servidor
{
    // Porta para escutar as conexões dos AGREGADORes
    private static readonly int Port = 5000;

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
                Thread clientThread = new Thread(() => ProcessaCliente(client));
                clientThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao aceitar conexão: " + ex.Message);
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
                                string[] bloco = new string[numLinhas];
                                for (int i = 0; i < numLinhas; i++)
                                    bloco[i] = reader.ReadLine();
                                
                                // Agora, 'bloco' contém todas as linhas enviadas pelo AGREGADOR.
                                Console.WriteLine("Bloco de dados recebido:");
                                foreach (string linha in bloco)
                                    Console.WriteLine(linha);
                            }
                            
                            // Envia o ACK para confirmar o recebimento
                            writer.WriteLine("ACK");
                            Console.WriteLine("ACK enviado. Encerrando conexão.");
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
}
