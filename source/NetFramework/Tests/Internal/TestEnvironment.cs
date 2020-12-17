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
            StartupHelper.ConfigureContainer(builder);
            return builder.Build();
        }))();

        public static MirrorSharpOptions MirrorSharpOptions { get; } = StartupHelper.CreateMirrorSharpOptions(Container);

        public static MirrorSharpServices MirrorSharpServices { get; } = new MirrorSharpServices {
            SetOptionsFromClient = Container.ResolveOptional<ISetOptionsFromClientExtension>(),
            SlowUpdate = Container.ResolveOptional<ISlowUpdateExtension>(),
            RoslynGuard = Container.ResolveOptional<IRoslynGuard>(),
            ExceptionLogger = Container.ResolveOptional<IExceptionLogger>()
        };

        public static MirrorSharpTestDriver NewDriver() => MirrorSharpTestDriver.New(MirrorSharpOptions, MirrorSharpServices);
    }
}
