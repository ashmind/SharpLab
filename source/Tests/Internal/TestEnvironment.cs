using Autofac;
using MirrorSharp;
using SharpLab.Server;

namespace SharpLab.Tests.Internal {
    public static class TestEnvironment {
        public static IContainer Container { get; } = StartupHelper.CreateContainerBuilder().Build();
        public static MirrorSharpOptions MirrorSharpOptions { get; } = StartupHelper.CreateMirrorSharpOptions(Container);
    }
}
