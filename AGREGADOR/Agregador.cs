using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;

class Agregador
{
    // Porta para escutar as conexões das WAVYs
    private static readonly int PortWavy = 5001;
    private static readonly string ServidorIP = "127.0.0.1";
    private static readonly int PortServidor = 5000;

    public static void Main()
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
                Console.WriteLine("Erro ao aceitar conexão: " + ex.Message);
            }
        }
    }

    private static void ProcessaWavy(TcpClient client)
    {
        try
        {
            using (client)
            {
                NetworkStream stream = client.GetStream();
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    // Lê a linha de header que indica o início do bloco e quantas linhas seguirão
                    string header = reader.ReadLine();
                    Console.WriteLine("Header recebido: " + header);

                    if (header != null && header.StartsWith("BLOCK"))
                    {
                        // Antes de tuodo, deve guardar o sync do WAVY para um ficheiro .csv
                        // Cada linha do ficheiro segue o seguinte formato: "WAVY_ID:status:[data_types]:last_sync"

                        // Extrai o número de linhas a serem lidas
                        string[] partes = header.Split(' ');
                        if (partes.Length == 2 && int.TryParse(partes[1], out int numLinhas))
                        {
                            string[] bloco = new string[numLinhas];
                            for (int i = 0; i < numLinhas; i++)
                            {
                                bloco[i] = reader.ReadLine();
                            }
                            
                            // Agora, 'bloco' contém todas as linhas enviadas pela WAVY.
                            Console.WriteLine("Bloco de dados recebido:");
                            foreach (string linha in bloco)
                            {
                                Console.WriteLine(linha);
                            }
                            
                            // Processa o bloco conforme necessário
                            ProcessaBloco(bloco);

                            // Envia ACK para confirmar o recebimento do bloco
                            writer.WriteLine("ACK");
                            Console.WriteLine("ACK enviado à WAVY.");
                        }
                        else
                        {
                            Console.WriteLine("Formato de header inválido.");
                        }
                    }
                }
            }
            // Envia dados para o servidor
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro ao processar bloco: " + ex.Message);
        }
    }
    private static void ProcessaBloco(string[] bloco)
    {
        // Cada linha do WAVY vem no formato seguinte: "WAVY_ID:[data_type=data]"
        // O que o AGREGADOR faz é separar os dados e encaminhá-los para o Servidor
        // Depois também deverá fazer algum tipo de pré-processamento dos dados, se necessário
        // O suposto é o AGREGADOR verificar a partir de um ficheiro de configuração em .csv o que deverá fazer para cada tipo de dado em cada WAVY
        // Cada linha desse ficheiro segue o seguinte formato: "WAVY_ID:pré_processamento:volume_dados_enviar:servidor_associado"
        // Como neste momento apenas existe um servidor, não há pré-processamento e o volume_dados_enviar é redundante, não lê o ficheiro
        
        // string[] partes;
        foreach (string linha in bloco)
        {
            // Extrai o ID da WAVY e os dados
            string[] partes = linha.Split('[');
            string wavyId = partes[0].TrimEnd(':'); // ID da WAVY
            string[] dados = partes[1].TrimEnd(']').Split(':'); // Dados da WAVY
            
            foreach (string dado in dados)
            {
                // Aqui, o dado é do tipo "data_type=data"
                string[] tipoDado = dado.Split('=');
                string dataType = tipoDado[0].Trim(); // Tipo de dado
                string data = tipoDado[1].Trim(); // Valor do dado
            }
        }
    }
    private static void EncaminhaParaServidor(string dados)
    {
        // No servidor, existem .csv para cada tipo de dados
        // O que o AGREGADOR deverá fazer é enviar os dados separados por tipo de dados
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
