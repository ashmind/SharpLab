using System;
using System.Reflection;
using Autofac;
using Autofac.Extras.FileSystemRegistration;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.Advanced.EarlyAccess;
using MirrorSharp.Owin;
using SharpLab.Server.Common;

namespace SharpLab.Server {
    public static class StartupHelper {
        // Chrome would limit to 10 mins I believe
        public static readonly TimeSpan CorsPreflightMaxAge = TimeSpan.FromHours(1);
        
        public static void ConfigureContainer(ContainerBuilder builder) {
            var assembly = Assembly.GetExecutingAssembly();

            builder
                .RegisterAssemblyModulesInDirectoryOf(assembly)
                .WhereFileMatches("SharpLab.*");
        }

        public static MirrorSharpOptions CreateMirrorSharpOptions(ILifetimeScope container) {
            var options = new MirrorSharpOptions {
                IncludeExceptionDetails = true,
                StatusTestCommands = {
                    ('O', "x-optimize=debug,x-target=C#,x-no-cache=true,language=C#"),
                    ('R', "0:0:0::using System; public class C { public void M() { } }"),
                    ('U', "")
                }
            };
            var languages = container.Resolve<ILanguageAdapter[]>();
            foreach (var language in languages) {
                language.SlowSetup(options);
            }
            return options;
        }

        public static MirrorSharpServices CreateMirrorSharpServices(ILifetimeScope container) {
            return new MirrorSharpServices {
                SetOptionsFromClient = container.Resolve<ISetOptionsFromClientExtension>(),
                SlowUpdate = container.Resolve<ISlowUpdateExtension>(),
                RoslynSourceTextGuard = container.Resolve<IRoslynSourceTextGuard>(),
                RoslynCompilationGuard = container.Resolve<IRoslynCompilationGuard>(),
                ExceptionLogger = container.Resolve<IExceptionLogger>()
            };
        }
    }
}
