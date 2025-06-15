using System;
using System.Threading.Tasks;
using System.IO;
using SERVIDOR;
using WAVY;
using AGREGADOR;

/// <summary>
/// Main UI class for the distributed sensor data system.
/// Provides a clean console interface while delegating all business logic to existing components.
/// </summary>
class MainUI
{
    private static bool _systemInitialized = false;

    public static async Task Main(string[] args)
    {
        Console.Title = "Sistema Distribuído de Sensores";
        
        try
        {
            await InitializeSystemAsync();
            await RunMainMenuAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Erro fatal do sistema: {ex.Message}");
        }
        finally
        {
            ShowInfo("Sistema encerrado.");
        }
    }

    #region System Initialization
    
    private static async Task InitializeSystemAsync()
    {
        ShowHeader("Inicializando Sistema Distribuído de Sensores");
        
        try
        {
            // Step 1: Initialize Agregadores first (RabbitMQ setup)
            ShowStep("1/4", "Inicializando Agregadores...");
            AgregadorMain.Init();
            await Task.Delay(2000); // Allow RabbitMQ setup
            
            // Step 2: Initialize Servidor
            ShowStep("2/4", "Inicializando Servidor...");
            ServidorMain.Init();
            await Task.Delay(1000);
            
            // Step 3: Initialize WAVYs (after message queues are ready)
            ShowStep("3/4", "Inicializando WAVYs...");
            WavyMain.Init();
            await Task.Delay(1000);
            
            // Step 4: System stabilization
            ShowStep("4/4", "Finalizando inicialização...");
            await Task.Delay(1000);
            
            _systemInitialized = true;
            ShowSuccess("✅ Sistema inicializado com sucesso!");
            await Task.Delay(1500);
        }
        catch (Exception ex)
        {
            ShowError($"❌ Falha na inicialização: {ex.Message}");
            throw;
        }
    }
    
    #endregion

    #region Main Menu System
    
    private static async Task RunMainMenuAsync()
    {
        while (true)
        {
            try
            {
                Console.Clear();
                ShowMainMenu();
                
                var choice = GetUserChoice();
                
                switch (choice)
                {
                    case "1":
                        await HandleWavyMenuAsync();
                        break;
                    case "2":
                        await HandleAgregadorMenuAsync();
                        break;
                    case "3":
                        await HandleServidorMenuAsync();
                        break;
                    case "4":
                        ShowInfo("Encerrando sistema...");
                        return;
                    default:
                        ShowError("Opção inválida!");
                        PauseForUser();
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Erro no menu: {ex.Message}");
                PauseForUser();
            }
        }
    }
    
    private static void ShowMainMenu()
    {
        ShowHeader("Menu Principal");
        
        if (!_systemInitialized)
        {
            ShowWarning("⚠️  Sistema não inicializado corretamente");
            Console.WriteLine();
        }
        
        Console.WriteLine("┌─────────────────────────────────────┐");
        Console.WriteLine("│  1. 📡 WAVYs (Sensores)            │");
        Console.WriteLine("│  2. 🔄 Agregadores                 │");
        Console.WriteLine("│  3. 🖥️  Servidor                   │");
        Console.WriteLine("│  4. 🚪 Sair                        │");
        Console.WriteLine("└─────────────────────────────────────┘");
        Console.WriteLine();
        Console.Write("Escolha uma opção (1-4): ");
    }
    
    #endregion

    #region Menu Handlers
    
    private static async Task HandleWavyMenuAsync()
    {
        while (true)
        {
            Console.Clear();
            ShowHeader("Menu WAVYs");
            
            Console.WriteLine("┌─────────────────────────────────────┐");
            Console.WriteLine("│  1. 📋 Listar WAVYs                │");
            Console.WriteLine("│  2. 📄 Mostrar Logs em Tempo Real  │");
            Console.WriteLine("│  3. ⚙️  Alterar Estado de WAVY     │");
            Console.WriteLine("│  4. ⬅️  Voltar                     │");
            Console.WriteLine("└─────────────────────────────────────┘");
            Console.WriteLine();
            Console.Write("Escolha uma opção (1-4): ");
            
            var choice = GetUserChoice();
            
            switch (choice)
            {
                case "1":
                    ExecuteSafely(() => WavyMain.ListarWavys(), "listar WAVYs");
                    break;
                case "2":
                    await ExecuteSafelyAsync(() => WavyMain.MostrarEnvioDados(), "mostrar logs das WAVYs");
                    break;
                case "3":
                    ExecuteSafely(() => WavyMain.AlterarEstadoWavy(), "alterar estado da WAVY");
                    break;
                case "4":
                    return;
                default:
                    ShowError("Opção inválida!");
                    PauseForUser();
                    break;
            }
        }
    }
    
    private static async Task HandleAgregadorMenuAsync()
    {
        while (true)
        {
            Console.Clear();
            ShowHeader("Menu Agregadores");
            
            Console.WriteLine("┌─────────────────────────────────────┐");
            Console.WriteLine("│  1. 📄 Mostrar Logs                │");
            Console.WriteLine("│  2. 📊 Dados Processados           │");
            Console.WriteLine("│  3. ⬅️  Voltar                     │");
            Console.WriteLine("└─────────────────────────────────────┘");
            Console.WriteLine();
            Console.Write("Escolha uma opção (1-3): ");
            
            var choice = GetUserChoice();
            
            switch (choice)
            {
                case "1":
                    await ExecuteSafelyAsync(() => AgregadorMain.MostrarLogsAgregadores(), "mostrar logs dos agregadores");
                    break;
                case "2":
                    ShowProcessedData();
                    break;
                case "3":
                    return;
                default:
                    ShowError("Opção inválida!");
                    PauseForUser();
                    break;
            }
        }
    }
    
    private static async Task HandleServidorMenuAsync()
    {
        while (true)
        {
            Console.Clear();
            ShowHeader("Menu Servidor");
            
            Console.WriteLine("┌─────────────────────────────────────┐");
            Console.WriteLine("│  1. 📊 Mostrar Dados               │");
            Console.WriteLine("│  2. 🔍 Analisar Dados              │");
            Console.WriteLine("│  3. 📄 Mostrar Logs                │");
            Console.WriteLine("│  4. ⬅️  Voltar                     │");
            Console.WriteLine("└─────────────────────────────────────┘");
            Console.WriteLine();
            Console.Write("Escolha uma opção (1-4): ");
            
            var choice = GetUserChoice();
            
            switch (choice)
            {
                case "1":
                    ShowServerData();
                    break;
                case "2":
                    await AnalyzeDataAsync(); // Changed to await async version
                    break;
                case "3":
                    await ExecuteSafelyAsync(() => ServidorMain.MostrarLogsServidor(), "mostrar logs do servidor");
                    break;
                case "4":
                    return;
                default:
                    ShowError("Opção inválida!");
                    PauseForUser();
                    break;
            }
        }
    }
    
    #endregion

    #region Data Display Methods
    
    private static void ShowProcessedData()
    {
        try
        {
            Console.Clear();
            ShowHeader("Dados Processados pelos Agregadores");
            
            string[] agregadores = { "AGREGADOR01", "AGREGADOR02" };
            
            foreach (var agregador in agregadores)
            {
                string filePath = $"dados/wavys_{agregador}.csv";
                
                Console.WriteLine($"\n┌─── {agregador} ─────────────────────────────────────┐");
                
                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath);
                    if (lines.Length > 0)
                    {
                        foreach (var line in lines)
                        {
                            Console.WriteLine($"│ {line,-45} │");
                        }
                    }
                    else
                    {
                        Console.WriteLine("│ Nenhum dado disponível                           │");
                    }
                }
                else
                {
                    Console.WriteLine("│ Arquivo não encontrado                            │");
                }
                
                Console.WriteLine("└───────────────────────────────────────────────────┘");
            }
            
            PauseForUser();
        }
        catch (Exception ex)
        {
            ShowError($"Erro ao mostrar dados processados: {ex.Message}");
            PauseForUser();
        }
    }
    
    private static void ShowServerData()
    {
        try
        {
            Console.Clear();
            ShowHeader("Dados do Servidor");
            
            string[] sensorTypes = { "gps", "gyro", "humidade", "ph", "temperatura" };
            
            foreach (var sensorType in sensorTypes)
            {
                string filePath = $"SERVIDOR/dados/{sensorType}.csv";
                
                Console.WriteLine($"\n┌─── {sensorType.ToUpper()} ─────────────────────────────────────┐");
                
                if (File.Exists(filePath))
                {
                    var lines = File.ReadAllLines(filePath);
                    if (lines.Length > 0)
                    {
                        // Show only last 5 lines to avoid cluttering
                        var recentLines = lines.Skip(Math.Max(0, lines.Length - 5));
                        foreach (var line in recentLines)
                        {
                            Console.WriteLine($"│ {line,-55} │");
                        }
                        if (lines.Length > 5)
                        {
                            Console.WriteLine($"│ ... e mais {lines.Length - 5} entradas anteriores");
                        }
                    }
                    else
                    {
                        Console.WriteLine("│ Nenhum dado disponível                               │");
                    }
                }
                else
                {
                    Console.WriteLine("│ Arquivo não encontrado                                │");
                }
                
                Console.WriteLine("└─────────────────────────────────────────────────────┘");
            }
            
            PauseForUser();
        }
        catch (Exception ex)
        {
            ShowError($"Erro ao mostrar dados do servidor: {ex.Message}");
            PauseForUser();
        }
    }
    
    private static async Task AnalyzeDataAsync() 
    {
        try
        {
            Console.Clear();
            ShowHeader("Análise de Dados");
            
            Console.Write("Digite o tipo de sensor (gps/gyro/humidade/ph/temperatura): ");
            string? sensorType = Console.ReadLine()?.Trim().ToLowerInvariant();
            
            if (string.IsNullOrWhiteSpace(sensorType) || !IsValidSensorType(sensorType))
            {
                ShowError("Tipo de sensor inválido ou não fornecido!");
                PauseForUser();
                return;
            }

            Console.Write("Digite o tipo de análise (basica/tendencia/anomalia/completa): ");
            string? analysisType = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(analysisType) || !IsValidAnalysisType(analysisType))
            {
                ShowError("Tipo de análise inválido ou não fornecido! Use: basica, tendencia, anomalia, completa.");
                PauseForUser();
                return;
            }
            
            Console.Write("Digite a data/hora inicial (yyyy-MM-dd-HH-mm-ss): ");
            string? startTimeStr = Console.ReadLine()?.Trim();
            
            Console.Write("Digite a data/hora final (yyyy-MM-dd-HH-mm-ss): ");
            string? endTimeStr = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrWhiteSpace(startTimeStr) || string.IsNullOrWhiteSpace(endTimeStr))
            {
                ShowError("Datas de início e fim são obrigatórias!");
                PauseForUser();
                return;
            }

            ShowInfo($"Solicitando análise {analysisType} para {sensorType} de {startTimeStr} a {endTimeStr}...");

            var analysisResult = await ServidorMain.RealizarAnaliseAsync(sensorType, analysisType, startTimeStr, endTimeStr);
            
            Console.WriteLine("\n┌─── Resultado da Análise Detalhada ───────────────┐");
            
            if (analysisResult == null || !analysisResult.Sucesso)
            {
                ShowError($"│ Falha ao obter resultados: {analysisResult?.Mensagem ?? "Erro desconhecido."}");
            }
            else
            {
                // Display Basic Statistics
                if (analysisResult.EstatisticasBasicas != null)
                {
                    Console.WriteLine("│ --- Estatísticas Básicas ---                   │");
                    Console.WriteLine($"│ Média: {analysisResult.EstatisticasBasicas.Media,-38} │");
                    Console.WriteLine($"│ Mediana: {analysisResult.EstatisticasBasicas.Mediana,-36} │");
                    Console.WriteLine($"│ Máximo: {analysisResult.EstatisticasBasicas.Max,-37} │");
                    Console.WriteLine($"│ Mínimo: {analysisResult.EstatisticasBasicas.Min,-37} │");
                    Console.WriteLine($"│ Desvio Padrão: {analysisResult.EstatisticasBasicas.DesvioPadrao,-28} │");
                    Console.WriteLine($"│ Contagem: {analysisResult.EstatisticasBasicas.TotalLeituras,-34} │");
                }
                else
                {
                    Console.WriteLine("│ Estatísticas básicas não disponíveis.          │");
                }

                // Display Trend Analysis
                if (analysisResult.Tendencia != null)
                {
                    Console.WriteLine("│ --- Análise de Tendência ---                   │");
                    Console.WriteLine($"│ Direção: {analysisResult.Tendencia.Direcao,-35} │");
                    Console.WriteLine($"│ Inclinação: {analysisResult.Tendencia.Inclinacao,-31} │");
                    Console.WriteLine($"│ Correlação Temporal: {analysisResult.Tendencia.CorrelacaoTemporal,-20} │");
                }
                else
                {
                    Console.WriteLine("│ Análise de tendência não disponível.           │");
                }

                // Display Anomaly Detection
                if (analysisResult.Anomalias != null && analysisResult.Anomalias.LeiturasSuspeitas.Count > 0)
                {
                    Console.WriteLine("│ --- Detecção de Anomalias ---                  │");
                    Console.WriteLine($"│ Total de Anomalias: {analysisResult.Anomalias.TotalAnomalias,-25} │");
                    Console.WriteLine($"│ Limite Inferior: {analysisResult.Anomalias.LimiteInferior,-28} │");
                    Console.WriteLine($"│ Limite Superior: {analysisResult.Anomalias.LimiteSuperior,-28} │");
                    foreach (var anomaly in analysisResult.Anomalias.LeiturasSuspeitas)
                    {
                        Console.WriteLine($"│ Anomalia: {anomaly.Timestamp:yyyy-MM-dd HH:mm:ss} - V: {anomaly.Value} (WAVY: {anomaly.WavyId}) │");
                    }
                }
                else
                {
                    Console.WriteLine("│ Nenhuma anomalia detectada ou não disponível.  │");
                }
            }
            
            Console.WriteLine("└───────────────────────────────────────────────────┘");
            PauseForUser();
        }
        catch (Exception ex)
        {
            ShowError($"Erro na análise de dados: {ex.Message}");
            PauseForUser();
        }
    }

    private static bool IsValidSensorType(string sensorType)
    {
        string[] validTypes = { "gps", "gyro", "humidade", "ph", "temperatura" };
        return validTypes.Contains(sensorType.ToLowerInvariant());
    }

    private static bool IsValidAnalysisType(string analysisType)
    {
        string[] validTypes = { "basica", "tendencia", "anomalia", "completa" };
        return validTypes.Contains(analysisType.ToLowerInvariant());
    }
    
    #endregion

    #region Utility Methods
    
    private static string GetUserChoice()
    {
        return Console.ReadLine()?.Trim() ?? "";
    }
    
    private static void ExecuteSafely(Action action, string actionName)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            ShowError($"Erro ao {actionName}: {ex.Message}");
            PauseForUser();
        }
    }
    
    private static async Task ExecuteSafelyAsync(Func<Task> action, string actionName)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ShowError($"Erro ao {actionName}: {ex.Message}");
            PauseForUser();
        }
    }
    
    private static void PauseForUser()
    {
        Console.WriteLine("\nPressione qualquer tecla para continuar...");
        Console.ReadKey(true);
    }
    
    #endregion

    #region Display Helpers
    
    private static void ShowHeader(string title)
    {
        Console.WriteLine($"\n╔═══════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║  {title,-59} ║");
        Console.WriteLine($"╚═══════════════════════════════════════════════════════════════╝\n");
    }
    
    private static void ShowStep(string step, string message)
    {
        Console.WriteLine($"[{step}] {message}");
    }
    
    private static void ShowSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    
    private static void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ {message}");
        Console.ResetColor();
    }
    
    private static void ShowWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }
    
    private static void ShowInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"ℹ️  {message}");
        Console.ResetColor();
    }
    
    #endregion
}
