using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharpLab.WebApp.Server.Assets;

namespace SharpLab.WebApp.Server {
    public class Startup : SharpLab.Server.Startup {
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (!env.IsDevelopment())
                app.UseHttpsRedirection();

            base.Configure(app, env);
        }

        protected override void MapOtherEndpoints(IEndpointRouteBuilder endpoints) {
            var indexHtmlEndpoints = endpoints.ServiceProvider.GetRequiredService<IndexHtmlEndpoints>();
            // https://github.com/dotnet/aspnetcore/issues/5897
            indexHtmlEndpoints.StartAsync().GetAwaiter().GetResult();
            var reloadToken = Environment.GetEnvironmentVariable("SHARPLAB_ASSETS_RELOAD_TOKEN");

            endpoints.MapGet("/", indexHtmlEndpoints.GetRootAsync);
            endpoints.MapPost("/assets/reload", indexHtmlEndpoints.PostReloadAsync);
        }
    }
}
