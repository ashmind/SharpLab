using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Cors;
using System.Web.Hosting;
using Autofac;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using MirrorSharp.Owin;
using Owin;
using SharpLab.Server.Common;
using SharpLab.Server.Monitoring;
using SharpLab.Server.Owin;

[assembly: OwinStartup(typeof(Startup), nameof(Startup.Configuration))]

namespace SharpLab.Server.Owin {
    public class Startup {
        public virtual void Configuration(IAppBuilder app) {
            DotEnv.Load();

            var corsPolicyTask = Task.FromResult(new CorsPolicy {
                AllowAnyHeader = true,
                AllowAnyMethod = true,
                AllowAnyOrigin = true,
                PreflightMaxAge = (long)StartupHelper.CorsPreflightMaxAge.TotalMilliseconds
            });
            var corsOptions = new CorsOptions {
                PolicyProvider = new CorsPolicyProvider {
                    PolicyResolver = r => corsPolicyTask
                }
            };
            app.UseCors(corsOptions);

            var container = CreateContainer();
            var mirrorSharpOptions = StartupHelper.CreateMirrorSharpOptions(container);
            app.UseMirrorSharp(mirrorSharpOptions);

            app.Map("/status", a => a.Use((c, next) => {
                c.Response.ContentType = "text/plain";
                return c.Response.WriteAsync("OK");
            }));

            var monitor = container.Resolve<IMonitor>();
            monitor.Event("Application Startup", null);
            HostingEnvironment.RegisterObject(new ShutdownMonitor(monitor));

            app.UseAutofacLifetimeScopeInjector(container);
        }

        private IContainer CreateContainer() {
            var builder = new ContainerBuilder();
            StartupHelper.ConfigureContainer(builder);
            return builder.Build();
        }

        private class ShutdownMonitor : IRegisteredObject {
            private readonly IMonitor _monitor;

            public ShutdownMonitor(IMonitor monitor) {
                _monitor = monitor;
            }

            public void Stop(bool immediate) {
                if (immediate)
                    return;
                try {
                    _monitor.Event("Application Shutdown", null, new Dictionary<string, string> {
                        { "Reason", HostingEnvironment.ShutdownReason.ToString() }
                    });
                }
                catch (Exception ex) {
                    _monitor.Exception(ex, null);
                }
            }
        }
    }
}