using System;
using System.Globalization;
using System.Threading;

public class TemperatureYearSimulator
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
        else // Outono (meses 9, 10, 11)
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
    
    public static void Start()
    {
        // Aqui não reescrevemos a lógica de seleção de cidade/região.
        // Obtém a cidade e a região através do método presente no arquivo "gerarcidades".
        
        (string selectedCity, string selectedRegion) = RandomCityRegion.GetRandomCityAndRegion();
        Console.WriteLine("Região e cidade obtidas do gerarcidades: {0} - {1}", selectedRegion, selectedCity);

        // Data de início da simulação (1 de janeiro de 2025)
        DateTime simulationTime = new DateTime(2025, 1, 1, 0, 0, 0);

        // Loop da simulação: a cada 5 segundos (tempo real), o sistema envia a temperatura,
        // avançando 5 segundos na simulação. Ao final de um dia (86400 segundos), o contador é reiniciado para o dia seguinte.
        while (true)
        {
            // Calcula a temperatura para o instante atual na região selecionada
            double temp = SmoothRandomTemperature(simulationTime, selectedRegion);
            
            // Formata o timestamp conforme "YYYY-MM-DD-HH-MM-SS"
            string timestamp = simulationTime.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
            
            /
            string output = $"Wavy_ID:{status:}]:[{temp:F2}]:last_sync({timestamp})";
            Console.WriteLine(output);

            // Aguarda 5 segundos em tempo real
            Thread.Sleep(5000);

            // Avança o tempo simulado em 5 segundos
            simulationTime = simulationTime.AddSeconds(5);

            // Se for o final do dia (86400 segundos), inicia um novo dia
            if (simulationTime.TimeOfDay.TotalSeconds >= 86400)
            {
                simulationTime = simulationTime.Date.AddDays(1);
                Console.WriteLine("Fim do dia. Iniciando o dia: " + simulationTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }
        }
    }
}
