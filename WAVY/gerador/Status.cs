using System;
using System.Globalization;

public class WavyStateManager
{
    public static void Start()
    {
        // Exibe a mensagem de solicitação ao administrador.
        Console.WriteLine("Deseja que o wavy fique ativo ou desativado?");
        Console.WriteLine("Digite A para ativar (Online) ou D para desativar (Offline):");
        
        // Lê e trata a resposta do administrador.
        string input = Console.ReadLine().Trim().ToUpper();
        string status;
        if (input == "A")
        {
            status = "Online";
        }
        else if (input == "D")
        {
            status = "Offline";
        }
        else
        {
            Console.WriteLine("Opção inválida. Por padrão, o wavy ficará Offline.");
            status = "Offline";
        }
        
        // Obtém a data/hora atual no formato "YYYY-MM-DD-HH-mm-ss"
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture);
        
        // Exibe a mensagem formatada conforme o padrão solicitado
        Console.WriteLine($"Wavy_ID:Status:[{status}]:last_sync({timestamp})");
    }
}
