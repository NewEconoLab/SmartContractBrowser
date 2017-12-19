using System;

namespace remotebuilderCore
{
    public class Program
    {
        public static void Main(string[] arg1)
        {
            httplib2.RpcServer server = new httplib2.RpcServer();
            server.Start(System.Net.IPAddress.Any, 8080);
            Compiler compiler = new Compiler();
           
            server.AddParser("/_api/help", compiler.onHelp);
            server.AddParser("/_api/parse", compiler.onCompile);

            ShowWelcome();

            while (true)
            {
                Console.Write("cmd>");
                var line = Console.ReadLine();
                line = line.Replace(" ", "").ToLower();
                switch (line)
                {
                    case "":
                        break;
                    case "help":
                        ShowHelp();
                        break;
                    default:
                        Console.WriteLine("wrong cmd.");
                        ShowHelp();
                        break;
                }
            }
        }
        static void ShowHelp()
        {

        }
        static void ShowWelcome()
        {
            Console.WriteLine("Neo smartContract remote compile servie.");
            Console.WriteLine("v0.01");
        }
    }
}
