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
        Wavy Wavy04 = new Wavy(AgregadorIP, Port, "WAVY04");
        Wavy Wavy05 = new Wavy(AgregadorIP, Port, "WAVY05");
        Wavy Wavy06 = new Wavy(AgregadorIP, Port, "WAVY06");
        Wavy Wavy07 = new Wavy(AgregadorIP, Port, "WAVY07");
        Wavy Wavy08 = new Wavy(AgregadorIP, Port, "WAVY08");
        Wavy Wavy09 = new Wavy(AgregadorIP, Port, "WAVY09");

        // Corre as tarefas em paralelo
        Task task1 = Task.Run(() => Wavy01.ReceberDados("./dados/dados1.csv"));
        Task task2 = Task.Run(() => Wavy02.ReceberDados("./dados/dados2.csv"));
        Task task3 = Task.Run(() => Wavy03.ReceberDados("./dados/dados3.csv"));
        Task task4 = Task.Run(() => Wavy04.ReceberDados("./dados/dados1.csv"));
        Task task5 = Task.Run(() => Wavy05.ReceberDados("./dados/dados2.csv"));
        Task task6 = Task.Run(() => Wavy06.ReceberDados("./dados/dados3.csv"));
        Task task7 = Task.Run(() => Wavy07.ReceberDados("./dados/dados1.csv"));
        Task task8 = Task.Run(() => Wavy08.ReceberDados("./dados/dados2.csv"));
        Task task9 = Task.Run(() => Wavy09.ReceberDados("./dados/dados3.csv"));

        Task[] tasks = new Task[] { task1, task2, task3, task4, task5, task6, task7, task8, task9 };

        // Espera que todas as tarefas terminem
        await Task.WhenAll(tasks);

        Console.WriteLine("All WAVYs have sent their data.");
    }
}
