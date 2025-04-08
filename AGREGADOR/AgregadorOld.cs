using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class AgregadorOld
{
    // Porta para escutar as conexões das WAVYs
    private static readonly int PortWavy = 5001;
    // Endereço e porta do Servidor (ao qual o Agregador encaminhará os dados)
    private static readonly string ServidorIP = "127.0.0.1";
    private static readonly int PortServidor = 5000;
    private static Mutex mutexWavys = new Mutex();

    public static void MainOld()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, PortWavy);
        listener.Start();
        Console.WriteLine("Agregador iniciado na porta " + PortWavy + ". Aguardando conexões das WAVYs...");

        while (true)
        {
            try
            {
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Conexão de uma WAVY recebida.");
                Thread clientThread = new Thread(() => ProcessaWavy(client));
                clientThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao aceitar conexão de WAVY: " + ex.Message);
            }
        }
    }

    private static void ProcessaWavy(TcpClient client)
    {
        try
        {
            
            string dadosWavy;
            using (client)
            {
                NetworkStream stream = client.GetStream();
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    // Lê a mensagem única enviada pela WAVY
                    dadosWavy = reader.ReadToEnd();
                    Console.WriteLine("Dados da WAVY: " + dadosWavy);

                    // Aqui poderás processar ou agregar os dados (por exemplo, salvar num ficheiro)
                    // ...

                    // Envia ACK para a WAVY
                    writer.WriteLine("ACK");
                    Console.WriteLine("ACK enviado à WAVY.");
                }
            }

            // Após receber os dados da WAVY, o Agregador encaminha-os para o Servidor
            EncaminhaParaServidor(dadosWavy);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao processar dados da WAVY: " + ex.Message);
        }
    }

    private static void EncaminhaParaServidor(string dados)
    {
        try
        {
            using (TcpClient clienteServidor = new TcpClient(ServidorIP, PortServidor))
            {
                NetworkStream stream = clienteServidor.GetStream();
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    // Envia os dados para o Servidor
                    writer.WriteLine(dados);
                    Console.WriteLine("Dados encaminhados ao Servidor: " + dados);

                    // Aguarda ACK do Servidor
                    string resposta = reader.ReadLine();
                    if (resposta == "ACK")
                    {
                        Console.WriteLine("ACK recebido do Servidor.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao encaminhar dados para o Servidor: " + ex.Message);
        }
    }
}
