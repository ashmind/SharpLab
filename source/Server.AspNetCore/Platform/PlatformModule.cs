using Autofac;
using JetBrains.Annotations;
using SharpLab.Server.Common;
using SharpLab.Server.Common.Internal;

namespace SharpLab.Server.Owin.Platform {
    [UsedImplicitly]
    public class PlatformModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<ConfigurationAdapter>()
                   .As<IConfigurationAdapter>()
                   .SingleInstance();

            builder.RegisterType<LocalAssemblyDocumentationResolver>()
                   .As<IAssemblyDocumentationResolver>()
                   .SingleInstance();
        }
    }
}
