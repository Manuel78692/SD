using System;
using System.Threading.Tasks; // ← Necessário para Task.Run
using AGREGADOR;               // ← Necessário para reconhecer a classe Agregador

namespace AGREGADOR
{
    public class AgregadorMain
    {
        public static void Main(string[] args)
        {
            var agregador1 = new Agregador("AGREGADOR01", 5001, "127.0.0.1", 6001);
            var agregador2 = new Agregador("AGREGADOR02", 5002, "127.0.0.1", 6001);

            Console.WriteLine("Inicializando agregadores...");
            _ = Task.Run(() => agregador1.Run());
            _ = Task.Run(() => agregador2.Run());

            // Mantém o processo vivo
            while (true)
            {
                Task.Delay(1000).Wait();
            }
        }
    }
}

