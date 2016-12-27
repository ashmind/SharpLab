using System;
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
using TryRoslyn.Core.Compilation;
using TryRoslyn.Core.Decompilation;
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

            var languageSetups = container.Resolve<ILanguageSetup[]>();
            var parseOptions = languageSetups.ToDictionary(s => s.LanguageName, s => s.GetParseOptions(SourceCodeKind.Regular));
            var compilationOptions = languageSetups.ToDictionary(s => s.LanguageName, s => s.GetCompilationOptions());
            var metadataReferences = languageSetups.ToDictionary(s => s.LanguageName, s => s.GetMetadataReferences());

            app.UseMirrorSharp(new MirrorSharpOptions {
                GetDefaultParseOptionsByLanguageName = name => parseOptions[name],
                GetDefaultCompilationOptionsByLanguageName = name => compilationOptions[name],
                GetDefaultMetadataReferencesByLanguageName = name => metadataReferences[name],
                SetOptionsFromClient = container.Resolve<ISetOptionsFromClientExtension>(),
                SlowUpdate = container.Resolve<ISlowUpdateExtension>(),
                IncludeExceptionDetails = true
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