using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.Advanced.EarlyAccess;
using MirrorSharp.Testing;
using SharpLab.Server;

namespace SharpLab.Tests.Internal {
    public static class TestEnvironment {
        public static ILifetimeScope Container { get; } = ((Func<ILifetimeScope>)(() => {
            Environment.SetEnvironmentVariable("SHARPLAB_CONTAINER_HOST_URL", "http://localhost/test");
            Environment.SetEnvironmentVariable("SHARPLAB_LOCAL_SECRETS_ContainerHostAuthorizationToken", "_");

            var host = Program.CreateHostBuilder(new string[0]).Build();
            return host.Services.GetAutofacRoot();
        }))();

        public static MirrorSharpOptions MirrorSharpOptions { get; } = Startup.CreateMirrorSharpOptions(Container);

        public static MirrorSharpServices MirrorSharpServices { get; } = new MirrorSharpServices {
            SetOptionsFromClient = Container.ResolveOptional<ISetOptionsFromClientExtension>(),
            SlowUpdate = Container.ResolveOptional<ISlowUpdateExtension>(),
            RoslynSourceTextGuard = Container.ResolveOptional<IRoslynSourceTextGuard>(),
            RoslynCompilationGuard = Container.ResolveOptional<IRoslynCompilationGuard>(),
            ExceptionLogger = Container.ResolveOptional<IExceptionLogger>()
        };

        public static MirrorSharpTestDriver NewDriver() => MirrorSharpTestDriver.New(MirrorSharpOptions, MirrorSharpServices);
    }
}
