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
using MirrorSharp.Advanced;
using SharpLab.Server.Common;
using SharpLab.Server.Common.Diagnostics;

namespace SharpLab.Server {    
    public class Startup {
        // Chrome would limit to 10 mins I believe
        private static readonly TimeSpan CorsPreflightMaxAge = TimeSpan.FromHours(1);

        public void ConfigureServices(IServiceCollection services) {
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
                SetOptionsFromClient = container.Resolve<ISetOptionsFromClientExtension>(),
                SlowUpdate = container.Resolve<ISlowUpdateExtension>(),
                IncludeExceptionDetails = true,
                ExceptionLogger = container.Resolve<IExceptionLogger>()
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
            app.UseMirrorSharp(CreateMirrorSharpOptions(app.ApplicationServices.GetAutofacRoot()));

            app.UseEndpoints(e => {
                var okBytes = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("OK"));
                e.MapGet("/status", context => {
                    context.Response.ContentType = "text/plain";
                    return context.Response.BodyWriter.WriteAsync(okBytes).AsTask();
                });

                e.MapControllers();
            });

            ConfigureStaticFiles(app);
        }

        protected virtual void ConfigureStaticFiles(IApplicationBuilder app) {
            app.UseStaticFiles();
        }
    }
}
