using System;
using System.Threading.Tasks;
using System.IO;
using SERVIDOR;
using WAVY;
using AGREGADOR;
using RabbitMQ.Client;


class SDMain
{
    private static Task? _tarefaEnvioDadosWavy;
    private static Task? _tarefaAgregador;
    private static Task? _tarefaServidor;
    // static List<string> logsServidor = new List<string>();
    // static Servidor servidor = new Servidor();

    public static async Task Main(string[] args)
    {
        // Inicializar Mains
        WavyMain.Init();
        AgregadorMain.Init();
        ServidorMain.Init();
        
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Menu Principal ===");
            Console.WriteLine("1. Wavy");
            Console.WriteLine("2. Agregador");
            Console.WriteLine("3. Servidor");
            Console.WriteLine("4. Sair");
            Console.Write("Escolha uma opção: ");
            var opcao = Console.ReadLine();
            switch (opcao)
            {
                case "1":
                    await MenuWavy();
                    break;
                case "2":
                    await MenuAgregador();
                    break;
                case "3":
                    await MenuServidor();
                    break;
                case "4":
                    Console.WriteLine("A sair...");
                    return;
                default:
                    Console.WriteLine("Opção inválida. Pressione qualquer tecla para continuar...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private static async Task MenuWavy()
    {
        await Task.Yield();
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Menu WAVY ===");
            Console.WriteLine("1. Listar WAVYs");
            Console.WriteLine("2. Mostrar Logs");
            Console.WriteLine("3. Alterar Estado de uma WAVY");
            Console.WriteLine("4. Voltar");
            Console.Write("Escolha uma opção: ");
            var opcao = Console.ReadLine();
            switch (opcao)
            {
                case "1":
                    WavyMain.ListarWavys();
                    break;
                case "2":
                    if (_tarefaEnvioDadosWavy == null || _tarefaEnvioDadosWavy.IsCompleted)
                    {
                        // Inicia a tarefa em background
                        _tarefaEnvioDadosWavy = Task.Run(() => WavyMain.MostrarEnvioDados());
                    }
                    // Aguarda ou mostra progresso
                    await _tarefaEnvioDadosWavy;
                    break;
                case "3":
                    WavyMain.AlterarEstadoWavy();
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

    private static async Task MenuAgregador()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Menu Agregador ===");
            Console.WriteLine("1. Mostrar Logs");
            Console.WriteLine("2. Mostrar de dados processados");
            Console.WriteLine("3. Voltar");
            Console.Write("Escolha uma opção: ");
            var opcao = Console.ReadLine();

            switch (opcao)
            {
                case "1":
                    if (_tarefaAgregador == null || _tarefaAgregador.IsCompleted)
                    {
                        // Inicia a tarefa em background
                        _tarefaAgregador = Task.Run(() => AgregadorMain.MostrarLogsAgregadores());
                    }
                    // Aguarda ou mostra progresso
                    await _tarefaAgregador;
                    break;
                case "2":
                    Console.WriteLine("=== Informações Pós-Processadas pelo PREPROCESSAMENTORRPC ===");
                    string[] agregadores = { "AGREGADOR01", "AGREGADOR02" };
                    foreach (var ag in agregadores)
                    {
                        string filePath = $"dados/wavys_{ag}.csv"; 
                        if (File.Exists(filePath))
                        {
                            Console.WriteLine($"\n--- {ag} ---");
                            var linhas = File.ReadAllLines(filePath);
                            foreach (var linha in linhas)
                                Console.WriteLine(linha);
                        }
                        else
                        {
                            Console.WriteLine($"\n--- {ag} ---");
                            Console.WriteLine("Nenhum dado encontrado.");
                        }
                    }
                    Console.WriteLine("\nPressione qualquer tecla para voltar...");
                    Console.ReadKey();
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("Opção inválida. Pressione qualquer tecla para continuar...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    private static async Task MenuServidor()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Menu Servidor ===");
            Console.WriteLine("1. Mostrar dados");
            Console.WriteLine("2. Analisar dados");
            Console.WriteLine("3. Mostrar Logs");
            Console.WriteLine("4. Voltar");
            Console.Write("Escolha uma opção: ");
            var opcao = Console.ReadLine();
            switch (opcao)
            {
                case "1":
                    string[] tipos = { "gps", "gyro", "humidade", "ph", "temperatura" };
                    foreach (var tipo in tipos)
                    {
                        string filePath = $"SERVIDOR/dados/{tipo}.csv";
                        if (File.Exists(filePath))
                        {
                            Console.WriteLine($"\n--- {tipo.ToUpper()} ---");
                            var linhas = File.ReadAllLines(filePath);
                            foreach (var linha in linhas)
                                Console.WriteLine(linha);
                        }
                        else
                        {
                            Console.WriteLine($"\n--- {tipo.ToUpper()} ---");
                            Console.WriteLine("Nenhum dado encontrado.");
                        }
                    }
                    Console.WriteLine("\nPressione qualquer tecla para voltar...");
                    Console.ReadKey();
                    break;
                case "2":
                    Console.Write("Digite o tipo de dado (gps/gyro/humidade/ph/temperatura): ");
                    string? tipoAnalise = Console.ReadLine();
                    Console.Write("Digite a data/hora inicial (yyyy-MM-dd-HH-mm-ss): ");
                    string? inicio = Console.ReadLine();
                    Console.Write("Digite a data/hora final   (yyyy-MM-dd-HH-mm-ss): ");
                    string? fim = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(tipoAnalise) || string.IsNullOrWhiteSpace(inicio) || string.IsNullOrWhiteSpace(fim))
                    {
                        Console.WriteLine("Tipo, data/hora inicial e final são obrigatórios.");
                        Console.WriteLine("Pressione qualquer tecla para voltar...");
                        Console.ReadKey();
                        break;
                    }

                    string filePathAnalise = $"SERVIDOR/dados/{tipoAnalise}.csv";
                    var dados = new List<string>();
                    if (File.Exists(filePathAnalise))
                    {
                        var linhas = File.ReadAllLines(filePathAnalise);
                        foreach (var linha in linhas)
                        {
                            if (linha.StartsWith("WAVY_ID")) continue;
                            var partes = linha.Split(':');
                            if (partes.Length >= 3)
                            {
                                string timestamp = partes[2];
                                if (string.Compare(timestamp, inicio) >= 0 && string.Compare(timestamp, fim) <= 0)
                                    dados.Add(partes[1]);
                            }
                        }
                    }

                    if (dados.Count == 0)
                    {
                        Console.WriteLine("Nenhum dado encontrado no intervalo selecionado.");
                    }
                    else
                    {
                        var resultado = AnaliseRPCClient.Analisar(tipoAnalise, inicio, fim, dados);
                        if (resultado != null)
                        {
                            Console.WriteLine($"Média: {resultado.Media}");
                            Console.WriteLine($"Máximo: {resultado.Max}");
                            Console.WriteLine($"Mínimo: {resultado.Min}");
                        }
                        else
                        {
                            Console.WriteLine("Erro ao obter resposta do ANALISARPCSERVIDOR.");
                        }
                    }

                    Console.WriteLine("Pressione qualquer tecla para voltar...");
                    Console.ReadKey();
                    break;
                case "3":
                    if (_tarefaServidor == null || _tarefaServidor.IsCompleted)
                    {
                        // Inicia a tarefa em background
                        _tarefaServidor = Task.Run(() => ServidorMain.MostrarLogsServidor());
                    }
                    // Aguarda ou mostra progresso
                    await _tarefaServidor;
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
}
