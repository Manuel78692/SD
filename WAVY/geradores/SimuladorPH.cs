using System;
using System.Globalization;
using System.Threading;
using System.IO;

public class SimuladorPH
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
    
    // Define os limites de pH para cada estação do ano com base num intervalo base e aplica o ajuste da região
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
    
    public static async IAsyncEnumerable<string> Start(Wavy wavy)
    {
        // Obtém a cidade e a região através do método definido no arquivo "RandomCityRegion".
        (string selectedCity, string selectedRegion) = RandomCityRegion.GetRandomCityAndRegion();
        Console.WriteLine("Região e cidade obtidas do gerarcidades: {0} - {1}", selectedRegion, selectedCity);
        
        // Data de início da simulação
        DateTime simulationTime = new DateTime(2025, 1, 1, 0, 0, 0);

        // Loop da simulação
        while (true)
        {
            double ph = SmoothRandomPH(simulationTime, selectedRegion);
            string timestamp = simulationTime.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);

            string output = string.Format(CultureInfo.InvariantCulture, "ph={0:F2}:{1}", ph, timestamp);
            yield return output;

            simulationTime = simulationTime.AddSeconds(5);
            
            // Ao final de um dia (86400 segundos simulados), inicia o dia seguinte
            if (simulationTime.TimeOfDay.TotalSeconds >= 86400)
            {
                simulationTime = simulationTime.Date.AddDays(1);
                string newDayTimestamp = simulationTime.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
                string newDayMessage = "Fim do dia. Iniciando o dia: " + newDayTimestamp;
                Console.WriteLine(newDayMessage);
            }
        }
    }
}
