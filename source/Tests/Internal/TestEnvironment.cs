using Autofac;
using MirrorSharp;
using SharpLab.Server;

namespace SharpLab.Tests.Internal {
    public static class TestEnvironment {
        public static IContainer Container { get; } = Startup.CreateContainer();
        public static MirrorSharpOptions MirrorSharpOptions { get; } = Startup.CreateMirrorSharpOptions(Container);
    }
}
