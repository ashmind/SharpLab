using Autofac;
using Autofac.Core;
using JetBrains.Annotations;
using TryRoslyn.Core.Compilation;
using TryRoslyn.Core.Compilation.Internal;
using TryRoslyn.Core.Decompilation;

namespace TryRoslyn.Core {
    [UsedImplicitly]
    public class CoreModule : Module {
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<MetadataReferenceCollector>()
                   .As<IMetadataReferenceCollector>()
                   .SingleInstance();

            builder.RegisterType<CSharpFeatureDiscovery>()
                   .Keyed<IFeatureDiscovery>(LanguageIdentifier.CSharp)
                   .SingleInstance();

            builder.RegisterType<VBNetFeatureDiscovery>()
                   .Keyed<IFeatureDiscovery>(LanguageIdentifier.VBNet)
                   .SingleInstance();

            builder.RegisterType<CSharpSetup>()
                   .As<ILanguageSetup>()
                   .WithParameter(ResolvedParameter.ForKeyed<IFeatureDiscovery>(LanguageIdentifier.CSharp))
                   .SingleInstance();

            builder.RegisterType<VBNetSetup>()
                   .As<ILanguageSetup>()
                   .WithParameter(ResolvedParameter.ForKeyed<IFeatureDiscovery>(LanguageIdentifier.VBNet))
                   .SingleInstance();

            builder.RegisterType<CSharpDecompiler>().As<IDecompiler>().SingleInstance();
            builder.RegisterType<VBNetDecompiler>().As<IDecompiler>().SingleInstance();
            builder.RegisterType<ILDecompiler>().As<IDecompiler>().SingleInstance();
        }
    }
}