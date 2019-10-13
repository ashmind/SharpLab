using System;
using Autofac;
using MirrorSharp;
using SharpLab.Server;

namespace SharpLab.Tests.Internal {
    public static class TestEnvironment {
        public static IContainer Container { get; } = ((Func<IContainer>)(() => {
            var builder = new ContainerBuilder();
            new Startup().ConfigureContainer(builder);
            return builder.Build();
        }))();

        public static MirrorSharpOptions MirrorSharpOptions { get; } = Startup.CreateMirrorSharpOptions(Container);
    }
}
