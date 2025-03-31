class WavyMain
{
    private static string AgregadorIP = "127.0.0.1";
    private static int Port = 5001;
    public static void Main()
    {
        Wavy Wavy01 = new Wavy(AgregadorIP, Port, "WAVY01");
        Wavy Wavy02 = new Wavy(AgregadorIP, Port, "WAVY02");
        Wavy Wavy03 = new Wavy(AgregadorIP, Port, "WAVY03");

        // Isto é em sequência, não em paralelo
        Wavy01.Send(Wavy01.WavyID + ", sensor1=25.4, sensor2=30.2, sensor3=18.9");
        Wavy02.Send(Wavy02.WavyID + ", sensor1=25.4, sensor2=30.2, sensor3=18.9");
        Wavy03.Send(Wavy03.WavyID + ", sensor1=25.4, sensor2=30.2, sensor3=18.9");

        
        // Para simular melhor, partilhar processos/threads

        // Possivelmente tornar os WAVYs modulares7

    }
}
