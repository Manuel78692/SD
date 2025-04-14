using System;
using System.Globalization;
using System.Threading;
using System.IO;

public class PHSimulator
{
    static Random random = new Random();
    
    // Retorna o ajuste (delta) de pH para cada região
    static double GetRegionPHAdjustment(string region)
    {
        switch (region)
        {
            case "Norte":    return -0.03;
            case "Centro":   return -0.02;
            case "Lisboa":   return  0.00;
            case "Alentejo": return  0.05;
            case "Algarve":  return  0.08;
            default:         return  0.0;
        }
    }
    
    // Define os limites de pH para cada estação do ano com base em um intervalo base e aplica o ajuste da região
    static void GetSeasonPHLimits(DateTime dt, string region, out double minPH, out double maxPH)
    {
        double adjustment = GetRegionPHAdjustment(region);
        int month = dt.Month;
        if (month == 12 || month == 1 || month == 2) // Inverno
        {
            minPH = 8.15 + adjustment;
            maxPH = 8.25 + adjustment;
        }
        else if (month >= 3 && month <= 5) // Primavera
        {
            minPH = 8.05 + adjustment;
            maxPH = 8.15 + adjustment;
        }
        else if (month >= 6 && month <= 8) // Verão
        {
            minPH = 7.95 + adjustment;
            maxPH = 8.05 + adjustment;
        }
        else // Outono (Set, Out, Nov)
        {
            minPH = 8.05 + adjustment;
            maxPH = 8.15 + adjustment;
        }
    }
    
    // Calcula o pH de forma suave para um determinado instante e região.
    // Utiliza uma modulação diária (função senoidal com amplitude pequena) e adiciona um leve ruído.
    static double SmoothRandomPH(DateTime dt, string region)
    {
        GetSeasonPHLimits(dt, region, out double minPH, out double maxPH);
        
        // Para simular variações diárias leves, usamos uma função senoidal baseada na hora do dia.
        double hour = dt.Hour + dt.Minute / 60.0;
        double angle = ((hour / 24.0) * 2 * Math.PI) - Math.PI / 2;
        double sineValue = Math.Sin(angle);
        
        double range = maxPH - minPH;
        // A amplitude de variação diária é 10% do range da faixa
        double modulation = 0.1 * range * sineValue;
        double basePH = (minPH + maxPH) / 2.0 + modulation;
        
        // Adiciona um leve ruído aleatório (±0.01)
        double noise = (random.NextDouble() - 0.5) * 0.02;
        double ph = basePH + noise;
        
        return Math.Round(ph, 3);
    }
    
    public static void Main(string[] args)
    {
        // Obtém a cidade e a região através do método definido no arquivo "gerarcidades".
        
        (string selectedCity, string selectedRegion) = RandomCityRegion.GetRandomCityAndRegion();
        Console.WriteLine("Região e cidade obtidas do gerarcidades: {0} - {1}", selectedRegion, selectedCity);
        
        // Data de início da simulação (1 de janeiro de 2025, 00:00:00)
        DateTime simulationTime = new DateTime(2025, 1, 1, 0, 0, 0);
        
        // Abre o arquivo PH.csv em modo append (supondo que ele já exista)
        using (StreamWriter sw = new StreamWriter("PH.csv", true))
        {
            // Loop da simulação: a cada 5 segundos (tempo real), calcula e grava o pH,
            // avançando 5 segundos no tempo simulado. Ao final de um dia, inicia o novo dia.
            while (true)
            {
                // Calcula o pH para o instante atual na região selecionada
                double ph = SmoothRandomPH(simulationTime, selectedRegion);
                
                // Formata o timestamp conforme "YYYY-MM-DD-HH-mm-ss"
                string timestamp = simulationTime.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
                
      
                string output = $"Wavy_ID:Status:[{ph:F3}]:last_sync({timestamp})";
                
                // Grava a mensagem no CSV
                sw.WriteLine(output);
                sw.Flush();
                
                // Exibe também no console para acompanhamento
                Console.WriteLine(output);
                
                // Aguarda 5 segundos em tempo real
                Thread.Sleep(5000);
                
                // Avança o tempo simulado em 5 segundos
                simulationTime = simulationTime.AddSeconds(5);
                
                // Se tiver completado um dia (86400 segundos), inicia o próximo dia (o .NET faz a transição de dia, mês e ano)
                if (simulationTime.TimeOfDay.TotalSeconds >= 86400)
                {
                    simulationTime = simulationTime.Date.AddDays(1);
                    string newDayTimestamp = simulationTime.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
                    
                    string newDayMessage = "Fim do dia. Iniciando o dia: " + newDayTimestamp;
                    sw.WriteLine(newDayMessage);
                    sw.Flush();
                    
                    Console.WriteLine(newDayMessage);
                }
            }
        }
    }
}
