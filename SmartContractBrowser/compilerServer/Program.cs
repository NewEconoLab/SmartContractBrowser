using System;

namespace compilerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            WebSocketSharp.Server.HttpServer server = new WebSocketSharp.Server.HttpServer(227);
            server.Start();
            server.DocumentRootPath = "abc";
            server.OnGet += Server_OnGet;
            while (true)
            {
                Console.ReadLine();
            }
        }
       
        private static async void Server_OnGet(object sender, WebSocketSharp.Server.HttpRequestEventArgs e)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes("hello everywhere.");
            e.Response.ContentEncoding = System.Text.Encoding.UTF8;
            e.Response.ContentType = "application/json";
            e.Response.ContentLength64 = buffer.Length;
            e.Response.StatusCode = 200;
            await e.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            //await e.Response.OutputStream.FlushAsync();
             e.Response.Close();
        }
    }
}
