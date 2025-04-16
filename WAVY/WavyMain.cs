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
        //Task.Run(async () => await RunAsync()).Wait();

        Thread gpsThread = new Thread(async () => await SimuladorGPS.Start());
        Thread gyroThread = new Thread(async () => await SimuladorGyro.Start());
        Thread humidadeThread = new Thread(async () => await SimuladorHumidade.Start());
        Thread phThread = new Thread(async () => await SimuladorPH.Start());
        Thread temperaturaThread = new Thread(async () => await SimuladorTemperatura.Start());
        
        gpsThread.Start();
        gyroThread.Start();
        humidadeThread.Start();
        phThread.Start();
        temperaturaThread.Start();
    }

    private static async Task RunAsync()
    {
        Wavy Wavy01 = new Wavy(AgregadorIP, Port, "WAVY01");
        Wavy Wavy02 = new Wavy(AgregadorIP, Port, "WAVY02");
        Wavy Wavy03 = new Wavy(AgregadorIP, Port, "WAVY03");

        // Run tasks in parallel
        Task task1 = Task.Run(() => Wavy01.ReceberDados("./dados/dados1.csv"));
        Task task2 = Task.Run(() => Wavy02.ReceberDados("./dados/dados2.csv"));
        Task task3 = Task.Run(() => Wavy03.ReceberDados("./dados/dados3.csv"));

        await Task.WhenAll(task1, task2, task3);

        Console.WriteLine("All WAVYs have sent their data.");
    }
}
