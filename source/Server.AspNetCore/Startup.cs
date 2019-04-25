using System;
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
        private IContainer _container;

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

            app.UseWebSockets();
            app.UseMirrorSharp(StartupHelper.CreateMirrorSharpOptions(_container));
        }
    }
}
