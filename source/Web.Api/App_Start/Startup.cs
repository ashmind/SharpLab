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
using TryRoslyn.Core;
using TryRoslyn.Web.Api;

[assembly: OwinStartup(typeof(Startup), nameof(Startup.Configuration))]

namespace TryRoslyn.Web.Api {
    public class Startup {
        public void Configuration(IAppBuilder app) {
            var container = CreateContainer();
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

            app.UseMirrorSharp(new MirrorSharpOptions {
                SlowUpdate = container.Resolve<ISlowUpdateExtension>()
            });
        }

        private static IContainer CreateContainer() {
            var builder = new ContainerBuilder();
            var apiAssembly = Assembly.GetExecutingAssembly();
            
            builder.RegisterAssemblyModules(typeof(ICodeProcessor).Assembly);
            builder.RegisterAssemblyModules(apiAssembly);

            return builder.Build();
        }
    }
}