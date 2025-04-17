using System;
using System.Net;
using System.Threading.Tasks;

class WavyMain
{
    // Ip dos AGREGADORES
    private static string agregadorIp = "127.0.0.1";
    public static void Main()
    {
        Task.Run(async () => await RunAsync()).Wait();
    }
    // Esta função simula os diferentes WAVYs
    // Cria instâncias da classe e corre todas simultaneamente
    private static async Task RunAsync()
    {
        Wavy Wavy01 = new Wavy("WAVY01", agregadorIp, 5001);
        Wavy Wavy02 = new Wavy("WAVY02", agregadorIp, 5002);
        // Wavy Wavy03 = new Wavy(AgregadorIP, Port, "WAVY03");
        // Wavy Wavy04 = new Wavy(AgregadorIP, Port, "WAVY04");

        Task task1 = Task.Run(() => Wavy01.ReceberDados(new List<TipoDado> { TipoDado.GPS, TipoDado.Gyro }));
        Task task2 = Task.Run(() => Wavy02.ReceberDados(new List<TipoDado> { TipoDado.GPS }));
        // Task task3 = Task.Run(() => Wavy03.ReceberDados(new List<TipoDado> { TipoDado.PH, TipoDado.Temperatura }));
        // Task task4 = Task.Run(() => Wavy04.ReceberDados(new List<TipoDado> { TipoDado.GPS, TipoDado.Gyro, TipoDado.PH, TipoDado.Temperatura }));

        await Task.WhenAll(task1, task2);
    }
}
