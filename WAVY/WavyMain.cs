using System;
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

        // Run tasks in parallel
        Task task1 = Task.Run(() => Wavy01.Send("WAVY01:[humidity=10:gps=40.453243,-9.123142]"));
        Task task2 = Task.Run(() => Wavy02.Send("WAVY02:[humidity=12:gps=40.453244,-9.123143]"));
        Task task3 = Task.Run(() => Wavy03.Send("WAVY03:[humidity=15:gps=40.453245,-9.123144]"));

        await Task.WhenAll(task1, task2, task3);

        Console.WriteLine("All WAVYs have sent their data.");
    }
}
