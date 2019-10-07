using System;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using MirrorSharp.AspNetCore;

namespace SharpLab.Server {    
    public class Startup {
        public void ConfigureServices(IServiceCollection services) {
            services.AddCors();
            services.AddControllers();
        }

        public void ConfigureContainer(ContainerBuilder builder) {
            StartupHelper.ConfigureContainer(builder);
        }

        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();
            app.UseCors(p => p
                .AllowAnyHeader()
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .SetPreflightMaxAge(StartupHelper.CorsPreflightMaxAge)
            );
            
            app.UseWebSockets();
            app.UseMirrorSharp(StartupHelper.CreateMirrorSharpOptions(app.ApplicationServices.GetAutofacRoot()));

            app.UseEndpoints(e => {
                var okBytes = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("OK"));
                e.MapGet("/status", context => {
                    context.Response.ContentType = "text/plain";
                    return context.Response.BodyWriter.WriteAsync(okBytes).AsTask();
                });

                e.MapControllers();
            });
        }
    }
}
