using System;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Collections.Generic;

public class CoastalAreaGPSSimulator
{
    static Random random = new Random();
    
    // Dicionário que mapeia os nomes das cidades (conforme retornado por RandomCityRegion) 
    // para as coordenadas (latitude, longitude) da sua área costal.
    // Se a cidade não possuir registro, usaremos o default da região.
    static Dictionary<string, (double lat, double lon)> coastalCoordinates = new Dictionary<string, (double, double)>
    {
        { "Viana do Castelo", (41.70, -8.83) },
        { "Porto",             (41.15, -8.61) },
        { "Aveiro",            (40.64, -8.65) },
        { "Leiria",            (39.74, -8.81) },
        { "Lisboa",            (38.72, -9.14) },
        { "Setúbal Norte",      (38.53, -8.89) },
        { "Setúbal Sul",       (38.53, -8.89) },
        { "Faro",              (37.02, -7.93) }
    };
    
    // Coordenadas padrão de área costal por região, caso a cidade não tenha mapeamento específico.
    static Dictionary<string, (double lat, double lon)> defaultCoastalByRegion = new Dictionary<string, (double, double)>
    {
        { "Norte",   (41.15, -8.61) },
        { "Centro",  (40.64, -8.65) },
        { "Lisboa",  (38.72, -9.14) },
        { "Alentejo",(38.52, -8.89) },
        { "Algarve", (37.02, -7.93) }
    };
    
    // Função que, a partir da cidade e região obtidas do "gerarcidades",
    // retorna as coordenadas da área costal (possivelmente com pequena variação para simular o movimento da onda).
    static (double lat, double lon) GetCoastalGPS(string city, string region)
    {
        (double baseLat, double baseLon) target;
        if (coastalCoordinates.ContainsKey(city))
        {
            target = coastalCoordinates[city];
        }
        else if (defaultCoastalByRegion.ContainsKey(region))
        {
            target = defaultCoastalByRegion[region];
        }
        else
        {
            // Se por algum motivo nem a região tiver default, usa (0,0)
            target = (0.0, 0.0);
        }
        
        // Adiciona uma leve variação (ruído) para simular a dinâmica da área "wavy"
        double noiseLat = (random.NextDouble() - 0.5) * 0.001; // variação em torno de ±0.0005
        double noiseLon = (random.NextDouble() - 0.5) * 0.001;
        
        double finalLat = Math.Round(target.lat + noiseLat, 6);
        double finalLon = Math.Round(target.lon + noiseLon, 6);
        return (finalLat, finalLon);
    }
    
    public static void Start()
    {
        // Obtém a cidade e a região através do método definido no arquivo "gerarcidades"
        
        (string selectedCity, string selectedRegion) = RandomCityRegion.GetRandomCityAndRegion();
        Console.WriteLine("Região e cidade obtidas do gerarcidades: {0} - {1}", selectedRegion, selectedCity);
        
        // Define a data de início da simulação: 1 de janeiro de 2025, 00:00:00
        DateTime simulationTime = new DateTime(2025, 1, 1, 0, 0, 0);
        
        // Caminho para o CSV de saída (arquivo já existente, modo append)
        string csvPath = "GPS.csv";
        using (StreamWriter sw = new StreamWriter(csvPath, true))
        {
            // Loop de simulação: a cada 5 segundos (tempo real) atualiza e grava a localização GPS
            while (true)
            {
                (double lat, double lon) = GetCoastalGPS(selectedCity, selectedRegion);
                string timestamp = simulationTime.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
                
                
                string output = $"Wavy_ID:Status:[{lat:F6}, {lon:F6}]:last_sync({timestamp})";
                sw.WriteLine(output);
                sw.Flush();
                Console.WriteLine(output);
                
                Thread.Sleep(5000);
                simulationTime = simulationTime.AddSeconds(5);
                
                // Ao final de um dia (86400 segundos simulados), inicia o novo dia
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
