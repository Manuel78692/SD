using System;
using System.Net;
using System.Threading.Tasks;

class WavyMain
{
    // IP do AGREGADOR
    private static string AgregadorIP = "127.0.0.1";
    // Porta para enviar os dados para o AGREGADOR
    private static int Port = 5001;
    public static void Main()
    {
        // Run async Main logic inside a Task and wait for it synchronously
        Task.Run(async () => await RunAsync()).Wait();
    }

    private static async Task RunAsync()
    {
        Wavy Wavy01 = new Wavy(AgregadorIP, Port, "WAVY01");
        Wavy Wavy02 = new Wavy(AgregadorIP, Port, "WAVY02");
        Wavy Wavy03 = new Wavy(AgregadorIP, Port, "WAVY03");
        Wavy Wavy04 = new Wavy(AgregadorIP, Port, "WAVY04");
        Wavy Wavy05 = new Wavy(AgregadorIP, Port, "WAVY05");
        Wavy Wavy06 = new Wavy(AgregadorIP, Port, "WAVY06");
        Wavy Wavy07 = new Wavy(AgregadorIP, Port, "WAVY07");
        Wavy Wavy08 = new Wavy(AgregadorIP, Port, "WAVY08");
        Wavy Wavy09 = new Wavy(AgregadorIP, Port, "WAVY09");

        // Run tasks in parallel
        Task task1 = Task.Run(() => Wavy01.ReceberDados(new List<TipoDado> { TipoDado.GPS, TipoDado.Gyro }));
        // Task task2 = Task.Run(() => Wavy02.ReceberDados("./dados/dados2.csv"));
        // Task task3 = Task.Run(() => Wavy03.ReceberDados("./dados/dados3.csv"));

        await Task.WhenAll(task1);

        Console.WriteLine("All WAVYs have sent their data.");
    }
}
