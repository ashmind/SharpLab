using System;
using System.IO;
using System.Text;
using System.Threading;
using Docker.DotNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharpLab.Container.Manager.Internal;

namespace SharpLab.Container.Manager
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();

            // TODO: DI
            var manager = new DockerManager(new StdinWriter(), new StdoutReader(), new DockerClientConfiguration());
            app.UseEndpoints(endpoints => {
                endpoints.MapPost("/", async context => {
                    var sessionId = context.Request.Headers["Sl-Session-Id"][0]!;
                    var memoryStream = new MemoryStream();
                    await context.Request.Body.CopyToAsync(memoryStream);

                    context.Response.StatusCode = 200;
                    using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
                    timeoutSource.CancelAfter(10000);
                    try {
                        var result = await manager.ExecuteAsync(sessionId, memoryStream.ToArray(), timeoutSource.Token);
                        var bytes = new byte[Encoding.UTF8.GetByteCount(result.Span)];
                        Encoding.UTF8.GetBytes(result.Span, bytes);
                        await context.Response.BodyWriter.WriteAsync(bytes, context.RequestAborted);
                    }
                    catch (Exception ex) {
                        await context.Response.WriteAsync(ex.ToString(), context.RequestAborted);
                    }
                });
            });
        }
    }
}
