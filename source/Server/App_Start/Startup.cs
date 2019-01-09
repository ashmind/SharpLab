using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Cors;
using System.Web.Hosting;
using Autofac;
using Autofac.Extras.FileSystemRegistration;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.Owin;
using Owin;
using SharpLab.Server;
using SharpLab.Server.Common;
using SharpLab.Server.Monitoring;

[assembly: OwinStartup(typeof(Startup), nameof(Startup.Configuration))]

namespace SharpLab.Server {
    public class Startup {
        public virtual void Configuration(IAppBuilder app) {
            DotEnv.Load();

            var corsPolicyTask = Task.FromResult(new CorsPolicy {
                AllowAnyHeader = true,
                AllowAnyMethod = true,
                AllowAnyOrigin = true,
                PreflightMaxAge = 60 * 60 * 1000 // 1 hour, though Chrome would limit to 10 mins I believe
            });
            var corsOptions = new CorsOptions {
                PolicyProvider = new CorsPolicyProvider {
                    PolicyResolver = r => corsPolicyTask
                }
            };
            app.UseCors(corsOptions);

            var container = CreateContainer();
            var mirrorSharpOptions = CreateMirrorSharpOptions(container);
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

        public static MirrorSharpOptions CreateMirrorSharpOptions(IContainer container) {
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
            return options;
        }

        public static IContainer CreateContainer() {
            var builder = new ContainerBuilder();
            var assembly = Assembly.GetExecutingAssembly();

            builder.RegisterAssemblyModulesInDirectoryOf(assembly);

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