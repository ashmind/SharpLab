using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Cors;
using System.Web.Hosting;
using Autofac;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.Owin;
using Owin;
using SharpLab.Server;
using SharpLab.Server.MirrorSharp.Internal;

[assembly: OwinStartup(typeof(Startup), nameof(Startup.Configuration))]

namespace SharpLab.Server {
    public class Startup {
        public void Configuration(IAppBuilder app) {
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

            var mirrorSharpOptions = CreateMirrorSharpOptions();
            app.UseMirrorSharp(mirrorSharpOptions);

            app.Map("/status", a => a.Use((c, next) => {
                c.Response.ContentType = "text/plain";
                return c.Response.WriteAsync("OK");
            }));

            Trace.TraceInformation("Application started.");
            HostingEnvironment.RegisterObject(new ShutdownTracer());
        }

        public static MirrorSharpOptions CreateMirrorSharpOptions() {
            var container = CreateContainer();
            var options = new MirrorSharpOptions {
                SetOptionsFromClient = container.Resolve<ISetOptionsFromClientExtension>(),
                SlowUpdate = container.Resolve<ISlowUpdateExtension>(),
                IncludeExceptionDetails = true
            };
            var languages = container.Resolve<ILanguageIntegration[]>();
            foreach (var language in languages) {
                language.SlowSetup(options);
            }
            return options;
        }

        private static IContainer CreateContainer() {
            var builder = new ContainerBuilder();
            var assembly = Assembly.GetExecutingAssembly();

            builder.RegisterAssemblyModules(assembly);

            return builder.Build();
        }

        private class ShutdownTracer : IRegisteredObject {
            public void Stop(bool immediate) {
                if (immediate)
                    return;
                try {
                    Trace.TraceInformation("Application shutdown: {0}.", HostingEnvironment.ShutdownReason);
                }
                catch (Exception ex) {
                    Trace.TraceError(ex.ToString());
                }
            }
        }
    }
}