using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class SimuladorFactory
{
    // Este dicionário associa cada tipo de dado à respetiva função de simulação do sensor
    public static readonly Dictionary<TipoDado, Func<Wavy, IAsyncEnumerable<string>>> Simuladores =
        new Dictionary<TipoDado, Func<Wavy, IAsyncEnumerable<string>>>
    {
        { TipoDado.GPS, SimuladorGPS.Start },
        { TipoDado.Gyro, SimuladorGyro.Start },
        { TipoDado.Humidade, SimuladorHumidade.Start },
        { TipoDado.PH, SimuladorPH.Start },
        { TipoDado.Temperatura, SimuladorTemperatura.Start }
    };
}
