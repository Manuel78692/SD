using System;

namespace ANALISERPC
{
    class AnaliseRPCServerMain
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ANALISE RPC SERVER ===");
            Console.WriteLine("Iniciando servidor RPC de análise de dados...");
            
            try
            {
                AnaliseRPCServer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao iniciar servidor RPC de análise: {ex.Message}");
                Console.WriteLine("Pressione qualquer tecla para sair...");
                Console.ReadKey();
            }
        }
    }
}
