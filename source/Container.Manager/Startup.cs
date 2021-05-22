using System;
using System.Diagnostics;
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

namespace SharpLab.Container.Manager {
    public class Startup {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // TODO: proper DI, e.g. Autofac
            services.AddSingleton<DockerClientConfiguration>();

            services.AddSingleton<ContainerNameFormat>();
            services.AddSingleton<ContainerPool>();

            services.AddHostedService<ContainerAllocationWorker>();
            services.AddSingleton<ContainerCleanupWorker>();
            services.AddHostedService(c => c.GetRequiredService<ContainerCleanupWorker>());

            services.AddSingleton<ExecutionHandler>();
            services.AddSingleton<StdinWriter>();
            services.AddSingleton<StdoutReader>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();

            // TODO: DI
            var expectedAuthorization = "Bearer " + (
                Environment.GetEnvironmentVariable("SHARPLAB_CONTAINER_HOST_ACCESS_TOKEN")
                ?? throw new Exception("Required environment variable SHARPLAB_CONTAINER_HOST_ACCESS_TOKEN was not provided.")
            );

            var handler = app.ApplicationServices.GetRequiredService<ExecutionHandler>();
            app.UseEndpoints(endpoints => {
                endpoints.MapPost("/", async context => {
                    var stopwatch = Stopwatch.StartNew();
                    // TODO: Proper structure
                    var authorization = context.Request.Headers["Authorization"][0];
                    if (authorization != expectedAuthorization) {
                        context.Response.StatusCode = 401;
                        return;
                    }

                    var sessionId = context.Request.Headers["Sl-Session-Id"][0]!;
                    var memoryStream = new MemoryStream();
                    await context.Request.Body.CopyToAsync(memoryStream);

                    context.Response.StatusCode = 200;
                    using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
                    timeoutSource.CancelAfter(5000);
                    try {
                        var result = await handler.ExecuteAsync(sessionId, memoryStream.ToArray(), timeoutSource.Token);
                        var bytes = new byte[Encoding.UTF8.GetByteCount(result.Span)];
                        Encoding.UTF8.GetBytes(result.Span, bytes);
                        await context.Response.BodyWriter.WriteAsync(bytes, context.RequestAborted);
                        await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes($"\n  CONTAINER MANAGER: {stopwatch.ElapsedMilliseconds,8}ms"), context.RequestAborted);
                    }
                    catch (Exception ex) {
                        await context.Response.WriteAsync(ex.ToString(), context.RequestAborted);
                    }
                });
            });
        }
    }
}
