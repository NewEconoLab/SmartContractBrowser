using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;

namespace httplib2
{
    public delegate Task<MyJson.IJsonNode> onHttp(FormData data);
    public class RpcServer
    {
        System.Collections.Concurrent.ConcurrentDictionary<string, onHttp> mapParser = new System.Collections.Concurrent.ConcurrentDictionary<string, onHttp>();
        async Task ProcessAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;
            var form = await FormData.FromRequest(context.Request);

            onHttp onHttp = null;
            mapParser.TryGetValue(path.ToLower(), out onHttp);
            if (onHttp != null)
            {
                var jsonback = await onHttp(form);
                var res = context.Response;
                res.StatusCode = 200;
                res.Headers["Access-Control-Allow-Origin"] = "*";
                res.Headers["Content-Type"] = "text/plain; charset=UTF-8";
                await context.Response.WriteAsync(jsonback.ToString());
            }
            else
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("not found:" + path);
            }
        }
        public void AddParser(string path, onHttp onhttp)
        {
            mapParser[path.ToLower()] = onhttp;
        }
        IWebHost server;
        public void Start(IPAddress adress, int port)
        {
            this.server = new WebHostBuilder().UseKestrel
                (options => options.Listen(adress, port, listenOptions =>
            {
                //if (!string.IsNullOrEmpty(sslCert))
                //    listenOptions.UseHttps(sslCert, password);
            }))
            .Configure(app => app.Run(ProcessAsync))
            .Build();
            server.Start();
        }
        public void Stop()
        {
            server.StopAsync();
        }

    }
}
