using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class SimuladorFactory
{
    // This dictionary maps each TipoDado to a simulator function.
    // Each simulator function receives a Wavy and returns IAsyncEnumerable<string>.
    public static readonly Dictionary<TipoDado, Func<Wavy, IAsyncEnumerable<string>>> Simulators =
        new Dictionary<TipoDado, Func<Wavy, IAsyncEnumerable<string>>>
    {
        { TipoDado.GPS, SimuladorGPS.Start },
        { TipoDado.Gyro, SimuladorGyro.Start },
        { TipoDado.Humidade, SimuladorHumidade.Start },
        { TipoDado.PH, SimuladorPH.Start },
        { TipoDado.Temperatura, SimuladorTemperatura.Start }
    };
}
