using System;
using System.IO;
using System.Globalization;
using System.Threading;

public class SimuladorHumidade
{
    static Random random = new Random();

    // Define os limites de humidade para cada estação do ano com base na região.
    static void GetSeasonHumidityLimits(DateTime dt, string region, out double minHum, out double maxHum)
    {
        int month = dt.Month;
        
        if (region == "Norte")
        {
            if (month >= 6 && month <= 8)      // Verão
            {
                minHum = 60;
                maxHum = 90;
            }
            else if (month == 12 || month == 1 || month == 2)  // Inverno
            {
                minHum = 75;
                maxHum = 100;
            }
            else if (month >= 3 && month <= 5)  // Primavera
            {
                minHum = 65;
                maxHum = 95;
            }
            else // Outono (Set, Out, Nov)
            {
                minHum = 70;
                maxHum = 98;
            }
        }
        else if (region == "Centro")
        {
            if (month >= 6 && month <= 8)      // Verão
            {
                minHum = 50;
                maxHum = 85;
            }
            else if (month == 12 || month == 1 || month == 2)  // Inverno
            {
                minHum = 70;
                maxHum = 95;
            }
            else if (month >= 3 && month <= 5)  // Primavera
            {
                minHum = 60;
                maxHum = 90;
            }
            else // Outono (Set, Out, Nov)
            {
                minHum = 65;
                maxHum = 93;
            }
        }
        else if (region == "Lisboa")
        {
            if (month >= 6 && month <= 8)      // Verão
            {
                minHum = 50;
                maxHum = 80;
            }
            else if (month == 12 || month == 1 || month == 2)  // Inverno
            {
                minHum = 65;
                maxHum = 90;
            }
            else if (month >= 3 && month <= 5)  // Primavera
            {
                minHum = 55;
                maxHum = 85;
            }
            else // Outono (Set, Out, Nov)
            {
                minHum = 60;
                maxHum = 88;
            }
        }
        else if (region == "Alentejo")
        {
            if (month >= 6 && month <= 8)      // Verão
            {
                minHum = 30;
                maxHum = 70;
            }
            else if (month == 12 || month == 1 || month == 2)  // Inverno
            {
                minHum = 60;
                maxHum = 90;
            }
            else if (month >= 3 && month <= 5)  // Primavera
            {
                minHum = 45;
                maxHum = 80;
            }
            else // Outono (Set, Out, Nov)
            {
                minHum = 50;
                maxHum = 85;
            }
        }
        else if (region == "Algarve")
        {
            if (month >= 6 && month <= 8)      // Verão
            {
                minHum = 40;
                maxHum = 75;
            }
            else if (month == 12 || month == 1 || month == 2)  // Inverno
            {
                minHum = 65;
                maxHum = 95;
            }
            else if (month >= 3 && month <= 5)  // Primavera
            {
                minHum = 50;
                maxHum = 85;
            }
            else // Outono (Set, Out, Nov)
            {
                minHum = 55;
                maxHum = 90;
            }
        }
        else
        {
            // Valores padrão caso a região não seja reconhecida
            minHum = 50;
            maxHum = 90;
        }
    }

    // Calcula a humidade de forma suave para um instante e região,
    // utilizando modulação diária e adição de ruído.
    static double SmoothRandomHumidity(DateTime dt, string region)
    {
        GetSeasonHumidityLimits(dt, region, out double minHum, out double maxHum);

        // Incluímos os segundos na modulação para consistência na simulação.
        double hour = dt.Hour + dt.Minute / 60.0 + dt.Second / 3600.0;
        double angle = ((hour / 24.0) * 2 * Math.PI) - Math.PI / 2;
        double sineValue = Math.Sin(angle);
        
        double amplitude = (maxHum - minHum);
        double modulation = 0.1 * amplitude * sineValue;
        
        double baseHum = (minHum + maxHum) / 2.0 + modulation;
        
        // Adiciona um leve ruído aleatório de ±1%
        double noise = (random.NextDouble() - 0.5) * 2.0;
        double hum = baseHum + noise;
        
        return Math.Round(hum, 2);
    }

    public static async Task Start()
    {
        // Obtém a cidade e a região através do método presente no arquivo "gerarcidades"
       
        (string selectedCity, string selectedRegion) = RandomCityRegion.GetRandomCityAndRegion();
        Console.WriteLine("Região e cidade obtidas do gerarcidades: {0} - {1}", selectedRegion, selectedCity);

        // Data de início da simulação (1 de janeiro de 2025, 00:00:00)
        DateTime simulationTime = new DateTime(2025, 1, 1, 0, 0, 0);
        

        using (StreamWriter sw = new StreamWriter("Humidade.csv", true))
        {
            // Loop da simulação: a cada 5 segundos (tempo real), calcula e grava a humidade,
            // avançando o tempo simulado em 5 segundos. Ao final de um dia, inicia o novo dia.
            while (true)
            {
                double hum = SmoothRandomHumidity(simulationTime, selectedRegion);
                string timestamp = simulationTime.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
                string output = $"Wavy_ID:Status:[{hum:F2}]:last_sync({timestamp})";
                
                sw.WriteLine(output);
                sw.Flush();
                Console.WriteLine("Humidade -- " + output);
                
                Thread.Sleep(5000);
                simulationTime = simulationTime.AddSeconds(5);

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
