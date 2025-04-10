using System;
using System.Collections.Generic;
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
    // Ip do AGREGADOR
    private string AgregadorIP;
    // Porta para enviar os dados para o AGREGADOR
    private int Port;
    // Identificador da WAVY
    private string WavyID;
    // Estado atual da WAVY
    public Estado EstadoWavy { get; set; }
    // Buffer de dados para armazenar as linhas lidas do CSV, para depois enviar para o AGREGADOR
    private List<string> bufferDados;
    // Tamanho máximo do buffer
    private const int MaxBufferSize = 5;

    public Wavy(string IP, int port, string ID)
    {
        AgregadorIP = IP;
        Port = port;
        WavyID = ID;
        bufferDados = new List<string>();
    }

    public void ReceberDados(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine("Arquivo CSV não encontrado.");
            return;
        }
        using (StreamReader reader = new StreamReader(filePath))
        {
            while (!reader.EndOfStream)
            {
                // Lê a linha do arquivo CSV
                string linha = reader.ReadLine();
                bufferDados.Add(WavyID + ":" + linha);
                // Quando tivermos MaxBufferSize linhas, enviamos o bloco
                if (bufferDados.Count >= MaxBufferSize)
                {
                    EnviarBloco();
                    bufferDados.Clear(); // Limpa o buffer após envio
                }
            }
        }
    }
    private void EnviarBloco()
    {
        // Create the Random instance once.
        Random random = new Random();
        while(true)
        {
            try
            {
                using (TcpClient client = new TcpClient(AgregadorIP, Port))
                {
                    NetworkStream stream = client.GetStream();
                    using (StreamReader reader = new StreamReader(stream))
                    using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                    {
                        // Envia a linha de cabeçalho com o identificador do bloco e o número de linhas
                        string header = "BLOCK " + bufferDados.Count;
                        writer.WriteLine(header);
                        Console.WriteLine("Enviado header: " + header);

                        // Envia cada linha do bloco
                        foreach (string linha in bufferDados)
                        {
                            writer.WriteLine(linha);
                            Console.WriteLine("Enviada linha: " + linha);
                        }

                        // Aguarda o ACK do Agregador
                        string resposta = reader.ReadLine();
                        if (resposta == "ACK")
                        {
                            Console.WriteLine("ACK recebido. Bloco enviado com sucesso.");
                        }
                        else
                        {
                            Console.WriteLine("Resposta inesperada: " + resposta);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao enviar bloco: " + ex.Message);
            }
            // Gera um atraso aleatório, para simular a leitura dos sensores
            // Em .Next, o primeiro parâmetro é inclusivo e o segundo é exclusivo.
            int delay = random.Next(750, 1001);
            Console.WriteLine("Aguardando " + delay + " ms antes do próximo envio.");
            Thread.Sleep(delay);
        }
    }
}