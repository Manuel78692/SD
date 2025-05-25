using System;
using System.Globalization;
using System.Threading;
using System.IO;

public class SimuladorTemperatura
{
    static Random random = new Random();

    // Retorna o ajuste de temperatura para cada região
    static double GetRegionAdjustment(string region)
    {
        switch (region)
        {
            case "Norte":    return -2;
            case "Centro":   return -1;
            case "Lisboa":   return 0;
            case "Alentejo": return 1;
            case "Algarve":  return 2;
            default:         return 0;
        }
    }

    // Define os limites de temperatura para cada estação do ano, aplicando o ajuste da região
    static void GetSeasonTemperatureLimits(DateTime dt, string region, out double minTemp, out double maxTemp)
    {
        double adjustment = GetRegionAdjustment(region);
        int month = dt.Month;
        
        if (month == 12 || month == 1 || month == 2) // Inverno
        {
            minTemp = 5 + adjustment;
            maxTemp = 15 + adjustment;
        }
        else if (month >= 3 && month <= 5) // Primavera
        {
            minTemp = 10 + adjustment;
            maxTemp = 22 + adjustment;
        }
        else if (month >= 6 && month <= 8) // Verão
        {
            minTemp = 16 + adjustment;
            maxTemp = 27 + adjustment;
        }
        else // Outono
        {
            minTemp = 12 + adjustment;
            maxTemp = 24 + adjustment;
        }
    }

    // Calcula a temperatura suave para um determinado instante e região
    static double SmoothRandomTemperature(DateTime dt, string region)
    {
        GetSeasonTemperatureLimits(dt, region, out double minTemp, out double maxTemp);
        
        // Modulação pelo horário: função senoidal com pico próximo de 12h e mínimos por volta da meia-noite.
        double hour = dt.Hour + dt.Minute / 60.0 + dt.Second / 3600.0;
        double angle = ((hour / 24.0) * 2 * Math.PI) - Math.PI / 2;
        double sineValue = Math.Sin(angle);
        double baseTemp = minTemp + ((sineValue + 1) / 2.0) * (maxTemp - minTemp);
        
        // Adiciona um pequeno ruído aleatório (±0.5°C)
        double noise = (random.NextDouble() - 0.5);
        double temp = baseTemp + noise;
        
        return Math.Round(temp, 2);
    }
    
    public static async IAsyncEnumerable<string> Start(Wavy wavy)
    {
        // Obtém a cidade e a região através do método presente no arquivo "RandomCityRegion"
        (string selectedCity, string selectedRegion) = RandomCityRegion.GetRandomCityAndRegion();
        Console.WriteLine("Região e cidade obtidas do gerarcidades: {0} - {1}", selectedRegion, selectedCity);

        // Data de início da simulação 
        DateTime simulationTime = new DateTime(2025, 1, 1, 0, 0, 0);

        // Loop da simulação
        while (true)
        {
            double temp = SmoothRandomTemperature(simulationTime, selectedRegion);
            string timestamp = simulationTime.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);

            string output = string.Format(CultureInfo.InvariantCulture, "temperatura={0:F2}:{1}", temp, timestamp);
            yield return output;

            simulationTime = simulationTime.AddSeconds(5);

            // Ao final de um dia (86400 segundos simulados), inicia o dia seguinte)
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
