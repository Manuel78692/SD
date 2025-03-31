using System;
using System.IO;
using System.Net.Sockets;

public enum Estado
{
    Associada,
    Operação,
    Manutenção,
    Desativada
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

    public void Send()
    {
        string mensagemCompleta = GerarBlocoCSV();
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
    //bloco de dados csv
    private string GerarBlocoCSV()
    {
        Random rnd = new Random();

        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        double temp = 15 + rnd.NextDouble() * 10;
        double ph = 6.5 + rnd.NextDouble();
        double acelX = rnd.NextDouble();
        double acelY = rnd.NextDouble();
        double acelZ = 9.8 + rnd.NextDouble() * 0.1;
        double gyroX = rnd.NextDouble();
        double gyroY = rnd.NextDouble();
        double gyroZ = rnd.NextDouble();
        string status = EstadoWavy.ToString().ToLower(); // usa o enum como string
        string sensores = "\"temperatura,ph,acelerometro,giroscopio,gps\"";
        double lat = 41.2950 + rnd.NextDouble() * 0.005;
        double lon = -7.7440 + rnd.NextDouble() * 0.005;

        return $"{WavyID},{timestamp},{temp:F1},{ph:F2},{acelX:F2},{acelY:F2},{acelZ:F2},{gyroX:F2},{gyroY:F2},{gyroZ:F2},{status},{sensores},{lat:F6},{lon:F6}";
    }
}
class Programa
{
    static void Main()
    {
        Wavy wavy = new Wavy("127.0.0.1", 5001, "WAVY_01");
        for (int i = 0; i < 5; i++)
        {
            wavy.Send();
            Thread.Sleep(10000);
        }
    }
}