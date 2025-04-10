using System;
using System.Threading.Tasks;

class WavyMain
{
    // IP do AGREGADOR
    private static string AgregadorIP = "127.0.0.1";
    // Porta para enviar os dados para o AGREGADOR
    private static int Port = 5001;
    public static void Main()
    {
        // Corre a lógica do Main assincronamente e espera por ela de forma síncrona
        Task.Run(async () => await RunAsync()).Wait();
    }

    private static async Task RunAsync()
    {
        Wavy Wavy01 = new Wavy(AgregadorIP, Port, "WAVY01");
        Wavy Wavy02 = new Wavy(AgregadorIP, Port, "WAVY02");
        Wavy Wavy03 = new Wavy(AgregadorIP, Port, "WAVY03");

        // Corre as tarefas em paralelo
        Task task1 = Task.Run(() => Wavy01.ReceberDados("./dados/dados1.csv"));
        Task task2 = Task.Run(() => Wavy02.ReceberDados("./dados/dados2.csv"));
        Task task3 = Task.Run(() => Wavy03.ReceberDados("./dados/dados3.csv"));

        // Espera que todas as tarefas terminem
        await Task.WhenAll(task1, task2, task3);

        Console.WriteLine("All WAVYs have sent their data.");
    }
}
