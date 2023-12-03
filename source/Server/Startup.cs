using System;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.FileSystemRegistration;
using MirrorSharp;
using MirrorSharp.AspNetCore;
using SharpLab.Server.Common;
using SharpLab.Server.Common.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SharpLab.Server {
    public class Startup {
        // Chrome would limit to 10 mins I believe
        private static readonly TimeSpan CorsPreflightMaxAge = TimeSpan.FromHours(1);

        public void ConfigureServices(IServiceCollection services) {
            services.AddHttpClient();
            services.AddCors();
            services.AddControllers();
        }

        public void ConfigureContainer(ContainerBuilder builder) {
            var assembly = Assembly.GetExecutingAssembly();

            builder
                .RegisterAssemblyModulesInDirectoryOf(assembly)
                .WhereFileMatches("SharpLab.*");
        }

        public static MirrorSharpOptions CreateMirrorSharpOptions(ILifetimeScope container) {            
            var options = new MirrorSharpOptions {
                IncludeExceptionDetails = true,
                StatusTestCommands = {
                    ('O', "x-optimize=debug,x-target=C#,x-no-cache=true,language=C#"),
                    ('R', "0:0:0::using System; public class C { public void M() { } }"),
                    ('U', "")
                }
            };
            var languages = container.Resolve<ILanguageAdapter[]>();
            foreach (var language in languages) {
                language.SlowSetup(options);
            }
            PerformanceLog.Checkpoint("Startup.CreateMirrorSharpOptions.End");
            return options;
        }

        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();
            app.UseCors(p => p
                .AllowAnyHeader()
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .SetPreflightMaxAge(CorsPreflightMaxAge)
            );
            
            app.UseWebSockets();
            app.MapMirrorSharp("/mirrorsharp", CreateMirrorSharpOptions(app.ApplicationServices.GetAutofacRoot()));

            app.UseEndpoints(e => {
                MapStatus(e);
                MapBranchVersion(e, env);
                if (env.IsDevelopment())
                    MapFeatureFlags(e);
                MapOtherEndpoints(e);

                e.MapControllers();
            });
        }

        private void MapStatus(IEndpointRouteBuilder e) {
            var okBytes = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("OK"));
            e.MapGet("/status", context => {
                context.Response.ContentType = MediaTypeNames.Text.Plain;
                return WriteResponseBodyAsync(context, okBytes);
            });
        }

        private void MapFeatureFlags(IEndpointRouteBuilder e) {
            e.MapGet("/featureflags/{key:alpha}", static context => {
                var key = (string)context.GetRouteValue("key")!;
                var featureFlagClient = context.RequestServices.GetRequiredService<IFeatureFlagClient>();

                return context.Response.WriteAsync(
                    featureFlagClient.GetInt32Flag(key)?.ToString() ?? "<null>",
                    context.RequestAborted
                );
            });
        }

        // Temporary: until build is updated to something better than a json file on site itself
        protected virtual void MapBranchVersion(IEndpointRouteBuilder endpoints, IWebHostEnvironment env) {
            var file = env.WebRootFileProvider.GetFileInfo("branch-version.json");
            if (!file.Exists)
                return;

            using var stream = file.CreateReadStream();
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            endpoints.MapGet("/branch-version.json", context => {
                context.Response.ContentType = MediaTypeNames.Application.Json;
                return WriteResponseBodyAsync(context, bytes);
            });
        }

        private Task WriteResponseBodyAsync(HttpContext context, ReadOnlyMemory<byte> body) {
            var writeTask = context.Response.BodyWriter.WriteAsync(body, context.RequestAborted);
            return writeTask.IsCompletedSuccessfully
                ? Task.CompletedTask
                : writeTask.AsTask();
        }

        protected virtual void MapOtherEndpoints(IEndpointRouteBuilder endpoints) {
        }
    }
}
