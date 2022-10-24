using System;
using System.Runtime.CompilerServices;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.Advanced.EarlyAccess;
using MirrorSharp.Testing;
using SharpLab.Server;
using SharpLab.Server.Common;

namespace SharpLab.Tests.Internal {
    public static class TestEnvironment {
        public static ILifetimeScope Container { get; } = ((Func<ILifetimeScope>)(() => {
            Environment.SetEnvironmentVariable("SHARPLAB_CONTAINER_HOST_URL", "http://localhost/test");
            Environment.SetEnvironmentVariable("SHARPLAB_LOCAL_SECRETS_ContainerHostAuthorizationToken", "_");
            Environment.SetEnvironmentVariable("SHARPLAB_WEBAPP_NAME", "sl-test");
            Environment.SetEnvironmentVariable("SHARPLAB_CACHE_PATH_PREFIX", "test");

            var host = Program.CreateHostBuilder(new string[0]).Build();
            return host.Services.GetAutofacRoot();
        }))();

        public static MirrorSharpOptions MirrorSharpOptions { get; } = Startup.CreateMirrorSharpOptions(Container);

        public static MirrorSharpServices MirrorSharpServices { get; } = new MirrorSharpServices {
            SetOptionsFromClient = Container.ResolveOptional<ISetOptionsFromClientExtension>(),
            SlowUpdate = Container.ResolveOptional<ISlowUpdateExtension>(),
            RoslynSourceTextGuard = Container.ResolveOptional<IRoslynSourceTextGuard>(),
            RoslynCompilationGuard = Container.ResolveOptional<IRoslynCompilationGuard>(),
            ConnectionSendViewer = Container.ResolveOptional<IConnectionSendViewer>(),
            ExceptionLogger = Container.ResolveOptional<IExceptionLogger>()
        };

        [ModuleInitializer]
        public static void Initialize() => DotEnv.Load();

        public static MirrorSharpTestDriver NewDriver() => MirrorSharpTestDriver.New(MirrorSharpOptions, MirrorSharpServices);
    }
}
