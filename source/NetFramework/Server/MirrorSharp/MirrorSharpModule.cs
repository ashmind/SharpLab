using Autofac;
using JetBrains.Annotations;
using MirrorSharp.Advanced;

namespace SharpLab.Server.MirrorSharp {
    [UsedImplicitly]
    public class MirrorSharpModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<SlowUpdate>()
                   .As<ISlowUpdateExtension>()
                   .SingleInstance();

            builder.RegisterType<SetOptionsFromClient>()
                   .As<ISetOptionsFromClientExtension>()
                   .SingleInstance();
        }
    }
}