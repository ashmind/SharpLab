using System;
using Autofac;
using MirrorSharp;
using MirrorSharp.Advanced;
using MirrorSharp.Advanced.EarlyAccess;
using MirrorSharp.Testing;
using SharpLab.Server;

namespace SharpLab.Tests.Internal {
    public static class TestEnvironment {
        public static IContainer Container { get; } = ((Func<IContainer>)(() => {
            var builder = new ContainerBuilder();
            new Startup().ConfigureContainer(builder);
            return builder.Build();
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
