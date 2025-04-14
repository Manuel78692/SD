using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

public enum Estado
{
    Ativo,
    Desativado
}

public class Wavy
{
    private string AgregadorIP;
    private int Port;
    private string WavyID;
    public Estado EstadoWavy { get; set; }
    private List<string> bufferDados;
    private const int MaxBufferSize = 5; // Tamanho máximo do buffer

    public Wavy(string IP, int port, string ID)
    {
        AgregadorIP = IP;
        Port = port;
        WavyID = ID;
        bufferDados = new List<string>();
    }

    public void ReceberDados(string filePath)
    {
        // for (int i = 0; i < 10; i++)
        // {
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
                    bufferDados.Add(linha);
                    // Quando tivermos MaxBufferSize linhas, enviamos o bloco
                    if (bufferDados.Count >= MaxBufferSize)
                    {
                        EnviarBloco();
                        bufferDados.Clear(); // Limpa o buffer após envio
                    }
                }
            }
        // }
    }
    private void EnviarBloco()
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
    }
}