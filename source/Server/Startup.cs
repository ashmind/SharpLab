using System;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using MirrorSharp.AspNetCore;
using System.Reflection;
using Autofac.Extras.FileSystemRegistration;
using MirrorSharp;
using SharpLab.Server.Common;
using SharpLab.Server.Common.Diagnostics;
using Microsoft.AspNetCore.Routing;

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
            var options = new MirrorSharpOptions { IncludeExceptionDetails = true };
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
                var okBytes = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("OK"));
                e.MapGet("/status", context => {
                    context.Response.ContentType = "text/plain";
                    return context.Response.BodyWriter.WriteAsync(okBytes).AsTask();
                });

                MapOtherEndpoints(e);

                e.MapControllers();
            });
        }

        protected virtual void MapOtherEndpoints(IEndpointRouteBuilder endpoints) {
        }
    }
}
