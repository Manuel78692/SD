using System;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class WavyMain
{
    private static string AgregadorIP = "127.0.0.1";
    private static int Port = 5001;

    public static void Main()
    {
        Task.Run(async () => await RunAsync()).Wait();
    }

    private static async Task RunAsync()
    {
        Wavy[] wavys = new Wavy[]
        {
            new Wavy(AgregadorIP, Port, "WAVY01"),
            new Wavy(AgregadorIP, Port, "WAVY02"),
            new Wavy(AgregadorIP, Port, "WAVY03"),
            new Wavy(AgregadorIP, Port, "WAVY04"),
    
        };

        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Gerenciamento de WAVYs ===");
            Console.WriteLine("1. Listar WAVYs");
            Console.WriteLine("2. Enviar Dados");
            Console.WriteLine("3. Alterar Estado de uma WAVY");
            Console.WriteLine("4. Sair");
            Console.Write("Escolha uma opção: ");
            string opcao = Console.ReadLine();

            switch (opcao)
            {
                case "1":
                    ListarWavys(wavys);
                    break;
                case "2":
                    await EnviarDados(wavys);
                    break;
                case "3":
                    AlterarEstadoWavy(wavys);
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("Opção inválida. Pressione qualquer tecla para continuar...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private static void ListarWavys(Wavy[] wavys)
    {
        Console.Clear();
        Console.WriteLine("=== Lista de WAVYs ===");
        foreach (var wavy in wavys)
        {
            Console.WriteLine($"ID: {wavy.WavyID}, Estado: {wavy.EstadoWavy}");
        }
        Console.WriteLine("Pressione qualquer tecla para voltar ao menu...");
        Console.ReadKey();
    }

    private static async Task EnviarDados(Wavy[] wavys)
    {
    Console.Clear();
    Console.WriteLine("=== Enviando Dados ===");
    Console.WriteLine("Pressione qualquer tecla para parar o envio de dados.");

    // Cria um CancellationTokenSource para cancelar as tarefas


        try
        {
            // Lista de tarefas para envio de dados
            List<Task> tasks = new List<Task>();

            foreach (var wavy in wavys)
            {
                if (wavy.EstadoWavy == Estado.Ativo) // Apenas WAVYs ativas enviam dados
                {
                    // Define os tipos de dados para cada WAVY
                    List<TipoDado> tiposDeDados = wavy.WavyID switch
                    {
                        "WAVY01" => new List<TipoDado> { TipoDado.GPS, TipoDado.Gyro },
                        "WAVY02" => new List<TipoDado> { TipoDado.Humidade },
                        "WAVY03" => new List<TipoDado> { TipoDado.PH, TipoDado.Temperatura },
                        "WAVY04" => new List<TipoDado> { TipoDado.GPS, TipoDado.Gyro, TipoDado.PH, TipoDado.Temperatura },
                        _ => new List<TipoDado>() // Caso padrão para WAVYs adicionais
                    };

                    // Adiciona a tarefa para envio de dados
                    tasks.Add(Task.Run(() => wavy.ReceberDados(tiposDeDados)));
                }
            }

            await Task.WhenAll(tasks); // Aguarda todas as tarefas serem concluídas
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Envio de dados interrompido pelo usuário.");
        }
      

        Console.WriteLine("Pressione qualquer tecla para voltar ao menu...");
        Console.ReadKey();
    }

    private static void AlterarEstadoWavy(Wavy[] wavys)
    {
        Console.Clear();
        Console.WriteLine("=== Alterar Estado de uma WAVY ===");
        Console.WriteLine("Digite o ID da WAVY:");
        string id = Console.ReadLine();

        var wavy = Array.Find(wavys, w => w.WavyID == id);
        if (wavy == null)
        {
            Console.WriteLine("WAVY não encontrada. Pressione qualquer tecla para voltar ao menu...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"Estado atual da {wavy.WavyID}: {wavy.EstadoWavy}");
        Console.WriteLine("Digite o novo estado (Ativo/Desativado):");
        string novoEstado = Console.ReadLine();

        if (Enum.TryParse(novoEstado, true, out Estado estado))
        {
            wavy.EstadoWavy = estado;
            Console.WriteLine($"Estado da {wavy.WavyID} alterado para {wavy.EstadoWavy}.");
        }
        else
        {
            Console.WriteLine("Estado inválido.");
        }

        Console.WriteLine("Pressione qualquer tecla para voltar ao menu...");
        Console.ReadKey();
    }
}
