using System;
using System.Net;
using System.Threading.Tasks;

class WavyMain
{
    private static string AgregadorIP = "127.0.0.1";
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

        // Run tasks in parallel
        Task task1 = Task.Run(() => Wavy01.ReceberDados(new List<TipoDado> { TipoDado.GPS, TipoDado.Gyro }));
        Task task2 = Task.Run(() => Wavy02.ReceberDados(new List<TipoDado> { TipoDado.Humidade }));
        Task task3 = Task.Run(() => Wavy03.ReceberDados(new List<TipoDado> { TipoDado.PH, TipoDado.Temperatura }));
        Task task4 = Task.Run(() => Wavy04.ReceberDados(new List<TipoDado> { TipoDado.GPS, TipoDado.Gyro, TipoDado.PH, TipoDado.Temperatura }));

        await Task.WhenAll(task1, task2, task3, task4);

        Console.WriteLine("All WAVYs have sent their data.");
    }
}
