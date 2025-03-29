class WavyMain
{
    private static string AgregadorIP = "127.0.0.1";
    private static int Port = 5000;
    public static void Main()
    {
        Wavy Wavy01 = new Wavy(AgregadorIP, Port, "WAVY01");
        Wavy Wavy02 = new Wavy(AgregadorIP, Port, "WAVY02");
        Wavy Wavy03 = new Wavy(AgregadorIP, Port, "WAVY03");

        Wavy01.Send();
        Wavy02.Send();
        Wavy03.Send();
    }
}
