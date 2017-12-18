using System;
using System.Collections.Generic;

namespace compilerServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var path1 = typeof(Program).Assembly.Location;
            System.IO.Directory.SetCurrentDirectory(System.IO.Path.GetDirectoryName(path1));
            var path2 = typeof(Action).Assembly.Location;

            compileService.testc.Init();
            Console.WriteLine("Init Server Fine!");
            List<string> errorinfo = new List<string>();
            //var b =compileService.testc.Build(errorinfo);
            //if (b)
            //{
            //    List<string> errorinfo2 = new List<string>();
            //    //var b2 = compileService.testc.BuildNeon(errorinfo2);
            //    //if(b2)
            //    //{
            //    //    Console.WriteLine("GenCode Succ");

            //    //}
            //}
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
