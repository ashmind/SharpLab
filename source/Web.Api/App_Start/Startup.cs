using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Cors;
using Autofac;
using Microsoft.CodeAnalysis;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.Owin;
using Owin;
using TryRoslyn.Core.Decompilation;
using TryRoslyn.Core.Processing.Languages;
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

            var parseOptions = container.Resolve<ILanguageSetup[]>().ToDictionary(
                s => s.LanguageName, s => s.GetParseOptions(SourceCodeKind.Regular)
            );
            app.UseMirrorSharp(new MirrorSharpOptions {
                GetDefaultParseOptionsByLanguageName = name => parseOptions[name],
                SlowUpdate = container.Resolve<ISlowUpdateExtension>()
            });
        }

        private static IContainer CreateContainer() {
            var builder = new ContainerBuilder();
            var apiAssembly = Assembly.GetExecutingAssembly();

            builder.RegisterAssemblyModules(typeof(IDecompiler).Assembly);
            builder.RegisterAssemblyModules(apiAssembly);

            return builder.Build();
        }
    }
}