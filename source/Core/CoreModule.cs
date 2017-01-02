using Autofac;
using Autofac.Core;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
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
                   .Keyed<IFeatureDiscovery>(LanguageNames.CSharp)
                   .SingleInstance();

            builder.RegisterType<VisualBasicFeatureDiscovery>()
                   .Keyed<IFeatureDiscovery>(LanguageNames.VisualBasic)
                   .SingleInstance();

            builder.RegisterType<CSharpSetup>()
                   .As<ILanguageSetup>()
                   .WithParameter(ResolvedParameter.ForKeyed<IFeatureDiscovery>(LanguageNames.CSharp))
                   .SingleInstance();

            builder.RegisterType<VisualBasicSetup>()
                   .As<ILanguageSetup>()
                   .WithParameter(ResolvedParameter.ForKeyed<IFeatureDiscovery>(LanguageNames.VisualBasic))
                   .SingleInstance();

            builder.RegisterType<CSharpDecompiler>().As<IDecompiler>().SingleInstance();
            builder.RegisterType<VisualBasicDecompiler>().As<IDecompiler>().SingleInstance();
            builder.RegisterType<ILDecompiler>().As<IDecompiler>().SingleInstance();
        }
    }
}