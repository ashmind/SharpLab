using System;
using System.Text;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MirrorSharp.AspNetCore;
using SharpLab.Server;

namespace Server.AspNetCore {    
    public class Startup {
        #pragma warning disable CS8618 // Non-nullable field is uninitialized.
        private IContainer _container;
        #pragma warning restore CS8618 // Non-nullable field is uninitialized.

        public IServiceProvider ConfigureServices(IServiceCollection services) {
            services.AddCors();

            var builder = StartupHelper.CreateContainerBuilder();
            builder.Populate(services);

            _container = builder.Build();
            return new AutofacServiceProvider(_container);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseCors(p => p
                .AllowAnyHeader()
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .SetPreflightMaxAge(StartupHelper.CorsPreflightMaxAge)
            );
            
            var okBytes = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("OK"));
            app.Map("/status", a => a.Use((c, next) => {
                c.Response.ContentType = "text/plain";
                return c.Response.BodyWriter.WriteAsync(okBytes).AsTask();
            }));

            app.UseWebSockets();
            app.UseMirrorSharp(StartupHelper.CreateMirrorSharpOptions(_container));
        }
    }
}
