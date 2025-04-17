using System;
using System.Collections.Generic;

public class RandomCityRegion
{
    public static (string,string) GetRandomCityAndRegion()
    {
        // Define o dicionário que mapeia as regiões das cidades.
        Dictionary<string, List<string>> regionCities = new Dictionary<string, List<string>>
        {
            { "Norte", new List<string> { "Viana do Castelo", "Braga", "Porto" } },
            { "Centro", new List<string> { "Aveiro", "Coimbra", "Leiria" } },
            { "Lisboa", new List<string> { "Lisboa", "Setúbal Norte" } },
            { "Alentejo", new List<string> { "Setúbal Sul", "Beja" } },
            { "Algarve", new List<string> { "Faro" } }
        };

        // Cria um dicionário auxiliar que mapeia cada cidade à sua região.
        Dictionary<string, string> cityToRegion = new Dictionary<string, string>();
        foreach (var kv in regionCities)
        {
            string region = kv.Key;
            foreach (string city in kv.Value)
            {
                cityToRegion[city] = region;
            }
        }

        // Cria uma lista com todas as cidades disponíveis (as chaves do dicionário cityToRegion)
        List<string> citiesList = new List<string>(cityToRegion.Keys);

        // Seleciona aleatoriamente uma cidade da lista
        Random random = new Random();
        string selectedCity = citiesList[random.Next(citiesList.Count)];

        // Obtém a região correspondente à cidade selecionada
        string regionForCity = cityToRegion[selectedCity];

        // Exibe o resultado
        // Console.WriteLine("Cidade escolhida: {0}", selectedCity);
        // Console.WriteLine("Pertence à região: {0}", regionForCity);

        return (selectedCity, regionForCity);
    }
}
