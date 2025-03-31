using System;
using System.IO;
using System.Net.Sockets;

public enum Estado
{
    Assoc, // Associado
    Ope, // Operação
    Manut, // Manutenção
    Desat // Desativado
}

public class Wavy
{
    private string AgregadorIP;
    private int Port;
    public string WavyID;
    public Estado EstadoWavy { get; set; }

    public Wavy(string IP, int port, string ID)
    {
        AgregadorIP = IP;
        Port = port;
        WavyID = ID;
    }

    public void Send(string mensagemCompleta)
    {
        try
        {
            using (TcpClient client = new TcpClient(AgregadorIP, Port))
            {
                NetworkStream stream = client.GetStream();
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    writer.WriteLine(mensagemCompleta);
                    Console.WriteLine("Mensagem enviada: " + mensagemCompleta);

                    // Aguarda o ACK do Agregador
                    string resposta = reader.ReadLine();
                    if (resposta == "ACK")
                    {
                        Console.WriteLine("ACK recebido. Conexão encerrada.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro na conexão: " + ex.Message);
        }
    }
}