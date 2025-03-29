using System;
using System.IO;
using System.Net.Sockets;

public class Wavy
{
    private string AgregadorIP;
    private int Port;
    private string WavyID;

    public Wavy(string IP, int port, string ID)
    {
        AgregadorIP = IP;
        Port = port;
        WavyID = ID;
    }

    public void Send()
    {
        try
        {
            using (TcpClient client = new TcpClient(AgregadorIP, Port))
            {
                NetworkStream stream = client.GetStream();
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    // Prepara a mensagem única que contém todas as informações necessárias
                    // Exemplo: "WAVY01, sensor1=25.4, sensor2=30.2, sensor3=18.9"
                    string mensagemCompleta = WavyID + ", sensor1=25.4, sensor2=30.2, sensor3=18.9";
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