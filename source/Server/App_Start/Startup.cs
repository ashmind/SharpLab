using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Cors;
using Autofac;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.Owin;
using Owin;
using SharpLab.Server;
using SharpLab.Server.MirrorSharp;

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
        }

        public static MirrorSharpOptions CreateMirrorSharpOptions() {
            var container = CreateContainer();
            var options = new MirrorSharpOptions {
                SetOptionsFromClient = container.Resolve<ISetOptionsFromClientExtension>(),
                SlowUpdate = container.Resolve<ISlowUpdateExtension>(),
                IncludeExceptionDetails = true
            };
            var setups = container.Resolve<IMirrorSharpSetup[]>();
            foreach (var setup in setups) {
                setup.ApplyTo(options);
            }
            return options;
        }

        private static IContainer CreateContainer() {
            var builder = new ContainerBuilder();
            var assembly = Assembly.GetExecutingAssembly();

            builder.RegisterAssemblyModules(assembly);

            return builder.Build();
        }
    }
}