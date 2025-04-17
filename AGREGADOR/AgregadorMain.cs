using System;
using System.Net;
using System.Threading.Tasks;

class AgregadorMain
{
    // Ip do SERVIDOR
    private static string servidorIp = "127.0.0.1";
    
    // Porta para enviar os dados para o SERVIDOR
    private static int servidorPort = 5000;

    public static void Main()
    {
        Task.Run(async () => await RunAsync()).Wait();
    }
    private static async Task RunAsync()
    {
        Agregador agregador01 = new Agregador("AGREGADOR01", 5001, servidorIp, servidorPort);
        Agregador agregador02 = new Agregador("AGREGADOR02", 5002, servidorIp, servidorPort);

        Task task1 = Task.Run(() => agregador01.Run());
        Task task2 = Task.Run(() => agregador02.Run());

        await Task.WhenAll(task1, task2);
    }
}