using System;
using System.Reflection;
using Autofac;
using Autofac.Extras.FileSystemRegistration;
using MirrorSharp;
using MirrorSharp.Advanced;
using SharpLab.Server.Common;

namespace SharpLab.Server {
    public static class StartupHelper {
        // Chrome would limit to 10 mins I believe
        public static readonly TimeSpan CorsPreflightMaxAge = TimeSpan.FromHours(1);
        
        public static ContainerBuilder CreateContainerBuilder() {
            var builder = new ContainerBuilder();
            var assembly = Assembly.GetExecutingAssembly();

            builder
                .RegisterAssemblyModulesInDirectoryOf(assembly)
                .WhereFileMatches("SharpLab.*");

            return builder;
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
    }
}
