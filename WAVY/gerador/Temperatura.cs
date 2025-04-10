using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

public class TemperatureYearSimulator
{
    static Random random = new Random();

    public TemperatureYearSimulator(){

    }
    // Dicionário que mapeia a região (grupo) para suas respectivas cidades
    static Dictionary<string, List<string>> regionCities = new Dictionary<string, List<string>>
    {
        { "Norte", new List<string> { "Viana do Castelo", "Braga", "Porto" } },
        { "Centro", new List<string> { "Aveiro", "Coimbra", "Leiria" } },
        { "Lisboa", new List<string> { "Lisboa", "Setúbal Norte" } },
        { "Alentejo", new List<string> { "Setúbal Sul", "Beja" } },
        { "Algarve", new List<string> { "Faro" } }
    };

    // Retorna o ajuste de temperatura para cada região
    static double GetRegionAdjustment(string region)
    {
        switch (region)
        {
            case "Norte": return -2;
            case "Centro": return -1;
            case "Lisboa": return 0;
            case "Alentejo": return 1;
            case "Algarve": return 2;
            default: return 0;
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
        else // Outono (meses 9,10,11)
        {
            minTemp = 12 + adjustment;
            maxTemp = 24 + adjustment;
        }
    }

    // Calcula a temperatura suave para um determinado instante e região
    static double SmoothRandomTemperature(DateTime dt, string region)
    {
        GetSeasonTemperatureLimits(dt, region, out double minTemp, out double maxTemp);
        
        // Modulação pelo horário:
        // Função senoidal que gera o pico próximo de 12h e mínimos por volta da meia-noite.

        double hour = dt.Hour + dt.Minute / 60.0;
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
        // Primeiro, cria um dicionário que mapeia cada cidade à sua região.
        // Isso facilita saber, para uma cidade escolhida aleatoriamente, a qual região ela pertence.
        Dictionary<string, string> cityToRegion = new Dictionary<string, string>();
        foreach (var kv in regionCities)
        {
            string region = kv.Key;
            foreach (string city in kv.Value)
            {
                cityToRegion[city] = region;
            }
        }

        // Cria uma lista contendo todas as cidades disponíveis
        List<string> citiesList = new List<string>(cityToRegion.Keys);


        // Caminho para o CSV de saída
        string csvPath = "Temperatura.csv";
        using (StreamWriter sw = new StreamWriter(csvPath))
        {
            // Cabeçalho do CSV
            sw.WriteLine("DataHora,Regiao,Cidade,Temperatura");
            
            
            DateTime start = new DateTime(2025, 1, 1, 0, 0, 0);
            DateTime end = start.AddDays(1);
            DateTime current = start;

            while (current < end)
            {
                // Seleciona aleatoriamente uma cidade
                string city = citiesList[random.Next(citiesList.Count)];
                // Obtém a região correspondente usando o dicionário cityToRegion
                string region = cityToRegion[city];
                
                // Calcula a temperatura para o instante atual e para a região escolhida
                double temp = SmoothRandomTemperature(current, region);
                
                // Cria a linha do CSV no formato "YYYY-MM-DD HH:mm:ss,Regiao,Cidade,Temperatura"
                string line = $"{current.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)},{region},{city},{temp}";
                sw.WriteLine(line);
                
                // Avança uma hora
                current = current.AddHours(1);
            }
        }
        Console.WriteLine("CSV gerado com sucesso: " + csvPath);
    }
}