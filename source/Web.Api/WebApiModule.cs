using Autofac;
using JetBrains.Annotations;
using Microsoft.IO;
using MirrorSharp.Advanced;
using TryRoslyn.Web.Api.Integration;

namespace TryRoslyn.Web.Api {
    [UsedImplicitly]
    public class WebApiModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterInstance(new RecyclableMemoryStreamManager())
                   .AsSelf();

            builder.RegisterType<SlowUpdate>()
                   .As<ISlowUpdateExtension>()
                   .SingleInstance();

            builder.RegisterType<SetOptionsFromClient>()
                   .As<ISetOptionsFromClientExtension>()
                   .SingleInstance();
        }
    }
}