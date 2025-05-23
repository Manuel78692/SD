using System;
using System.IO;
using System.Globalization;
using System.Threading;

public class SimuladorGyro
{
    static Random random = new Random();
    
    // Define os limites de ondulação (swell) conforme a estação do ano.
    static void GetSeasonSwellLimits(DateTime dt, out double minSwell, out double maxSwell)
    {
        int month = dt.Month;
        if (month >= 6 && month <= 8)             // Verão: ondas mais calmas
        {
            minSwell = 0.5;
            maxSwell = 1.5;
        }
        else if (month == 12 || month == 1 || month == 2)  // Inverno: ondas mais agitadas
        {
            minSwell = 2.5;
            maxSwell = 4.0;
        }
        else if (month >= 3 && month <= 5)         // Primavera: intermediários
        {
            minSwell = 1.0;
            maxSwell = 2.5;
        }
        else                                      // Outono: intermediários
        {
            minSwell = 1.5;
            maxSwell = 3.0;
        }
    }
    
    // Calcula a ondulação de forma suave para um instante dado, aplicando uma modulação diária (função senoidal) e adicionando um leve ruído.
    static double SmoothRandomSwell(DateTime dt)
    {
        GetSeasonSwellLimits(dt, out double minSwell, out double maxSwell);
        double hour = dt.Hour + dt.Minute / 60.0 + dt.Second / 3600.0;
        double angle = ((hour / 24.0) * 2 * Math.PI) - Math.PI / 2;
        double sineValue = Math.Sin(angle);
        double amplitude = maxSwell - minSwell;
        double modulation = 0.1 * amplitude * sineValue;
        double baseSwell = ((minSwell + maxSwell) / 2.0) + modulation;
        double noise = (random.NextDouble() - 0.5) * 0.2 * amplitude;
        double swell = baseSwell + noise;
        return Math.Round(swell, 2);
    }
    
    // Classifica o estado da ondulação com base no valor (em metros).
    public static string ClassifySwell(double swell)
    {
        if (swell < 1.0)
            return "Calmo";
        else if (swell < 2.0)
            return "Moderado";
        else if (swell < 3.0)
            return "Agitado";
        else
            return "Muito Agitado";
    }
    
    public static async IAsyncEnumerable<string> Start(Wavy wavy)
    {
        // Obtém a cidade e a região através do método definido no arquivo "RandomCityRegion".
        (string selectedCity, string selectedRegion) = RandomCityRegion.GetRandomCityAndRegion();       
        // Console.WriteLine("Região e cidade obtidas do gerarcidades: {0} - {1}", selectedRegion, selectedCity);
        
        // Data de início da simulação
        DateTime simulationTime = new DateTime(2025, 1, 1, 0, 0, 0);
        
        // Loop de simulação
        while (true)
        {
            double swell = SmoothRandomSwell(simulationTime);
            string classification = ClassifySwell(swell);
            string timestamp = simulationTime.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
            
            string output = string.Format(CultureInfo.InvariantCulture, "gyro={0:F2}:{1}", swell, timestamp);
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
