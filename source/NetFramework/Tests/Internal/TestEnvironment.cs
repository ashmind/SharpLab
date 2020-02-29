using System;
using Autofac;
using MirrorSharp;
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

        public static MirrorSharpServices MirrorSharpServices { get; } = ((Func<MirrorSharpServices>)(() => {
            var services = StartupHelper.CreateMirrorSharpServices(Container);
            return new MirrorSharpServices {
                SetOptionsFromClient = services.SetOptionsFromClient,
                SlowUpdate = services.SlowUpdate,
                ExceptionLogger = services.ExceptionLogger
            };
        }))();

        public static MirrorSharpTestDriver NewDriver() => MirrorSharpTestDriver.New(MirrorSharpOptions, MirrorSharpServices);
    }
}
